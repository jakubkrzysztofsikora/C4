import { describe, expect, it } from 'vitest';
import { renderToString } from 'react-dom/server';
import { DiagramData } from '../types';
import { useDiagram } from './useDiagram';

function DiagramHarness() {
  const diagram = useDiagram();

  return (
    <output data-testid="harness">
      {JSON.stringify(diagram.data)}
    </output>
  );
}

function StateHarness() {
  const diagram = useDiagram();

  return (
    <output>
      <span
        data-level={diagram.level}
        data-loading={String(diagram.loading)}
        data-has-error={String(diagram.error !== undefined)}
      >
        {JSON.stringify(diagram.data)}
      </span>
    </output>
  );
}

function decodeHtmlEntities(html: string): string {
  return html
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&apos;/g, "'");
}

function parseData(html: string): DiagramData {
  const decoded = decodeHtmlEntities(html);
  const match = decoded.match(/>(\{"nodes":.*?\})\s*</);
  if (!match?.[1]) throw new Error('Could not parse diagram data from rendered output');
  return JSON.parse(match[1]) as DiagramData;
}

describe('useDiagram', () => {
  it('returns nodes filtered to Container level by default', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const allLevels = data.nodes.map((n) => n.level);
    expect(allLevels).not.toContain('Context');
    expect(allLevels.length).toBeGreaterThan(0);
  });

  it('includes Container and Component nodes at Container level', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const levels = new Set(data.nodes.map((n) => n.level));
    expect(levels.has('Container')).toBe(true);
    expect(levels.has('Component')).toBe(true);
    expect(levels.has('Context')).toBe(false);
  });

  it('returns edges only between visible nodes', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const nodeIds = new Set(data.nodes.map((n) => n.id));
    for (const edge of data.edges) {
      expect(nodeIds.has(edge.from)).toBe(true);
      expect(nodeIds.has(edge.to)).toBe(true);
    }
  });

  it('returns data with correct structure', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    expect(data).toHaveProperty('nodes');
    expect(data).toHaveProperty('edges');
    expect(Array.isArray(data.nodes)).toBe(true);
    expect(Array.isArray(data.edges)).toBe(true);
  });

  it('nodes have required properties', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    for (const node of data.nodes) {
      expect(node).toHaveProperty('id');
      expect(node).toHaveProperty('label');
      expect(node).toHaveProperty('level');
      expect(node).toHaveProperty('health');
      expect(node).toHaveProperty('serviceType');
    }
  });

  it('edges have required properties', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    for (const edge of data.edges) {
      expect(edge).toHaveProperty('id');
      expect(edge).toHaveProperty('from');
      expect(edge).toHaveProperty('to');
      expect(edge).toHaveProperty('traffic');
      expect(edge.traffic).toBeGreaterThanOrEqual(0);
      expect(edge.traffic).toBeLessThanOrEqual(1);
    }
  });

  it('defaults to Container level with no loading state', () => {
    const rendered = renderToString(<StateHarness />);
    expect(rendered).toContain('data-level="Container"');
    expect(rendered).toContain('data-loading="false"');
    expect(rendered).toContain('data-has-error="false"');
  });
});
