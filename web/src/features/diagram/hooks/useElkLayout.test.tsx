import { describe, expect, it } from 'vitest';
import { renderToString } from 'react-dom/server';
import { useElkLayout } from './useElkLayout';
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
  const result = useElkLayout(data);
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
