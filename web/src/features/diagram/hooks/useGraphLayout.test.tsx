import { describe, expect, it } from 'vitest';
import { renderToString } from 'react-dom/server';
import { useGraphLayout } from './useGraphLayout';
import { DiagramData } from '../types';

const SAMPLE_DATA: DiagramData = {
  nodes: [
    { id: 'a', label: 'Service A', level: 'Container', health: 'green', serviceType: 'api' },
    { id: 'b', label: 'Service B', level: 'Container', health: 'yellow', serviceType: 'database' },
    { id: 'c', label: 'Service C', level: 'Container', health: 'green', serviceType: 'cache' },
  ],
  edges: [
    { id: 'e1', from: 'a', to: 'b', traffic: 0.8 },
    { id: 'e2', from: 'b', to: 'c', traffic: 0.6 },
  ],
};

const EMPTY_DATA: DiagramData = { nodes: [], edges: [] };

const SINGLE_NODE_DATA: DiagramData = {
  nodes: [
    { id: 'solo', label: 'Solo Service', level: 'Container', health: 'green', serviceType: 'app' },
  ],
  edges: [],
};

function decodeHtmlEntities(html: string): string {
  return html
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&apos;/g, "'");
}

function LayoutHarness({ data }: { data: DiagramData }) {
  const layouted = useGraphLayout(data);
  return <output>{JSON.stringify(layouted)}</output>;
}

function parseLayoutData(html: string): DiagramData {
  const decoded = decodeHtmlEntities(html);
  const match = decoded.match(/>(\{.*\})</);
  if (!match?.[1]) throw new Error('Could not parse layout data from rendered output');
  return JSON.parse(match[1]) as DiagramData;
}

describe('useGraphLayout', () => {
  it('assigns position to every node', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    for (const node of result.nodes) {
      expect(node.position).toBeDefined();
      expect(typeof node.position?.x).toBe('number');
      expect(typeof node.position?.y).toBe('number');
    }
  });

  it('returns empty data unchanged', () => {
    const rendered = renderToString(<LayoutHarness data={EMPTY_DATA} />);
    const result = parseLayoutData(rendered);

    expect(result.nodes).toHaveLength(0);
    expect(result.edges).toHaveLength(0);
  });

  it('produces distinct positions for different nodes', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    const positions = result.nodes.map((n) => `${n.position?.x},${n.position?.y}`);
    const uniquePositions = new Set(positions);

    expect(uniquePositions.size).toBe(result.nodes.length);
  });

  it('preserves all original node properties', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    for (const original of SAMPLE_DATA.nodes) {
      const layouted = result.nodes.find((n) => n.id === original.id);
      expect(layouted).toBeDefined();
      expect(layouted?.label).toBe(original.label);
      expect(layouted?.level).toBe(original.level);
      expect(layouted?.health).toBe(original.health);
      expect(layouted?.serviceType).toBe(original.serviceType);
    }
  });

  it('preserves edges unchanged', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    expect(result.edges).toEqual(SAMPLE_DATA.edges);
  });

  it('handles a single node', () => {
    const rendered = renderToString(<LayoutHarness data={SINGLE_NODE_DATA} />);
    const result = parseLayoutData(rendered);

    expect(result.nodes).toHaveLength(1);
    expect(result.nodes[0]?.position).toBeDefined();
    expect(typeof result.nodes[0]?.position?.x).toBe('number');
    expect(typeof result.nodes[0]?.position?.y).toBe('number');
  });

  it('preserves node count', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    expect(result.nodes).toHaveLength(SAMPLE_DATA.nodes.length);
  });

  it('uses left-to-right layout direction', () => {
    const rendered = renderToString(<LayoutHarness data={SAMPLE_DATA} />);
    const result = parseLayoutData(rendered);

    const nodeA = result.nodes.find((n) => n.id === 'a');
    const nodeC = result.nodes.find((n) => n.id === 'c');

    expect(nodeA?.position).toBeDefined();
    expect(nodeC?.position).toBeDefined();
    expect(nodeC!.position!.x).toBeGreaterThan(nodeA!.position!.x);
  });
});
