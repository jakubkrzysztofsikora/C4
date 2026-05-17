import { describe, expect, it, vi } from 'vitest';
import { renderToString } from 'react-dom/server';

vi.mock('elkjs/lib/elk-api.js', () => ({
  default: class MockElk {
    layout(): Promise<{ children: never[]; edges: never[] }> {
      return Promise.resolve({ children: [], edges: [] });
    }
  },
}));

import { useElkLayout, buildElkGraph } from './useElkLayout';
import type { DiagramData, DiagramNode } from '../types';

function makeNode(overrides: Partial<DiagramNode> & { id: string; label: string }): DiagramNode {
  return {
    level: 'Container',
    health: 'green',
    serviceType: 'app',
    ...overrides,
  };
}

function Harness({ data }: { data: DiagramData }) {
  const result = useElkLayout(data, new Set<string>());
  return (
    <output>
      {JSON.stringify({
        nodeCount: result.layoutedData.nodes.length,
        edgeCount: result.layoutedData.edges.length,
        groupCount: result.groupNodes.length,
        isLayouting: result.isLayouting,
      })}
    </output>
  );
}

function decodeEntities(html: string): string {
  return html
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&apos;/g, "'");
}

function parseResult(html: string): { nodeCount: number; edgeCount: number; groupCount: number; isLayouting: boolean } {
  const decoded = decodeEntities(html);
  const match = decoded.match(/>(\{.*?\})</);
  if (!match?.[1]) throw new Error('Could not parse result from rendered output');
  return JSON.parse(match[1]);
}

describe('useElkLayout', () => {
  it('handles empty data without errors', () => {
    const data: DiagramData = { nodes: [], edges: [] };
    const result = parseResult(renderToString(<Harness data={data} />));

    expect(result.nodeCount).toBe(0);
    expect(result.edgeCount).toBe(0);
    expect(result.groupCount).toBe(0);
    expect(result.isLayouting).toBe(false);
  });

  it('preserves all nodes through layout', () => {
    const data: DiagramData = {
      nodes: [
        makeNode({ id: 'n1', label: 'API' }),
        makeNode({ id: 'n2', label: 'DB', serviceType: 'database' }),
      ],
      edges: [{ id: 'e1', from: 'n1', to: 'n2', traffic: 0.9 }],
    };
    const result = parseResult(renderToString(<Harness data={data} />));

    expect(result.nodeCount).toBe(2);
    expect(result.edgeCount).toBe(1);
  });

  it('returns initial state before layout effect runs', () => {
    const data: DiagramData = {
      nodes: [makeNode({ id: 'n1', label: 'Service' })],
      edges: [],
    };
    const result = parseResult(renderToString(<Harness data={data} />));

    expect(result.nodeCount).toBe(1);
    expect(result.isLayouting).toBe(false);
  });

  it('does not create groups for nodes without resourceGroup', () => {
    const data: DiagramData = {
      nodes: [
        makeNode({ id: 'n1', label: 'API' }),
        makeNode({ id: 'n2', label: 'DB' }),
      ],
      edges: [],
    };
    const result = parseResult(renderToString(<Harness data={data} />));

    expect(result.groupCount).toBe(0);
  });

  it('does not create groups for a single node in a resource group', () => {
    const data: DiagramData = {
      nodes: [
        makeNode({ id: 'n1', label: 'Lonely', resourceGroup: 'rg-solo' }),
      ],
      edges: [],
    };
    const result = parseResult(renderToString(<Harness data={data} />));

    expect(result.groupCount).toBe(0);
  });
});

describe('buildElkGraph', () => {
  it('collapsed group produces a single top-level child node without children', () => {
    const nodes: DiagramNode[] = [
      makeNode({ id: 'n1', label: 'API', resourceGroup: 'rg-prod' }),
      makeNode({ id: 'n2', label: 'DB', resourceGroup: 'rg-prod' }),
    ];
    const collapsedGroups = new Set(['group-rg-prod']);

    const graph = buildElkGraph(nodes, [], collapsedGroups);

    const groupChild = graph.children?.find((c) => c.id === 'group-rg-prod');
    expect(groupChild).toBeDefined();
    expect(groupChild?.children).toBeUndefined();
  });

  it('edges to children of a collapsed group are filtered out', () => {
    const nodes: DiagramNode[] = [
      makeNode({ id: 'n1', label: 'API', resourceGroup: 'rg-prod' }),
      makeNode({ id: 'n2', label: 'DB', resourceGroup: 'rg-prod' }),
      makeNode({ id: 'n3', label: 'Gateway' }),
    ];
    const edges: DiagramData['edges'] = [
      { id: 'e1', from: 'n3', to: 'n1', traffic: 1 },
      { id: 'e2', from: 'n3', to: 'n2', traffic: 1 },
    ];
    const collapsedGroups = new Set(['group-rg-prod']);

    const graph = buildElkGraph(nodes, edges, collapsedGroups);

    expect(graph.edges).toHaveLength(0);
  });

  it('edges between non-collapsed nodes are retained', () => {
    const nodes: DiagramNode[] = [
      makeNode({ id: 'n1', label: 'API' }),
      makeNode({ id: 'n2', label: 'DB' }),
      makeNode({ id: 'n3', label: 'Cache', resourceGroup: 'rg-infra' }),
      makeNode({ id: 'n4', label: 'Queue', resourceGroup: 'rg-infra' }),
    ];
    const edges: DiagramData['edges'] = [
      { id: 'e1', from: 'n1', to: 'n2', traffic: 1 },
    ];
    const collapsedGroups = new Set<string>();

    const graph = buildElkGraph(nodes, edges, collapsedGroups);

    expect(graph.edges).toHaveLength(1);
    expect(graph.edges?.[0]?.id).toBe('e1');
  });

  it('uses POLYLINE edge routing when node count exceeds 500', () => {
    const nodes: DiagramNode[] = Array.from({ length: 501 }, (_, i) =>
      makeNode({ id: `n${i}`, label: `Node ${i}` }),
    );

    const graph = buildElkGraph(nodes, [], new Set());

    expect(graph.layoutOptions?.['elk.edgeRouting']).toBe('POLYLINE');
  });

  it('uses ORTHOGONAL edge routing when node count is at most 500', () => {
    const nodes: DiagramNode[] = Array.from({ length: 500 }, (_, i) =>
      makeNode({ id: `n${i}`, label: `Node ${i}` }),
    );

    const graph = buildElkGraph(nodes, [], new Set());

    expect(graph.layoutOptions?.['elk.edgeRouting']).toBe('ORTHOGONAL');
  });
});
