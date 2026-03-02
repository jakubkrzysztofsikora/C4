import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { act, createElement } from 'react';
import { createRoot } from 'react-dom/client';
import { renderToString } from 'react-dom/server';
import { DiagramData, DiagramNode } from '../types';
import { useDiagram } from './useDiagram';

declare global {
  var IS_REACT_ACT_ENVIRONMENT: boolean;
}

globalThis.IS_REACT_ACT_ENVIRONMENT = true;

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
    expect(allLevels.length).toBe(0);
  });

  it('includes Container and Component nodes at Container level', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const levels = new Set(data.nodes.map((n) => n.level));
    expect(levels.has('Container')).toBe(false);
    expect(levels.has('Component')).toBe(false);
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

  it('returns no seed node when projectId is omitted', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const discoveryWorker = data.nodes.find((n) => n.label === 'Discovery Worker');
    expect(discoveryWorker).toBeUndefined();
  });

  it('seed nodes without parentId have parentId undefined', () => {
    const rendered = renderToString(<DiagramHarness />);
    const data = parseData(rendered);

    const nodesWithoutParent = data.nodes.filter((n) => n.label !== 'Discovery Worker');
    for (const node of nodesWithoutParent) {
      expect(node.parentId).toBeUndefined();
    }
  });
});

vi.mock('../../../shared/api/client', () => ({
  getJson: vi.fn(),
  ApiError: class ApiError extends Error {
    public readonly status: number;
    constructor(status: number, message: string) {
      super(message);
      this.status = status;
      this.name = 'ApiError';
    }
  },
}));

vi.mock('./useSignalR', () => ({
  useSignalR: vi.fn(),
}));

import { getJson } from '../../../shared/api/client';
import { useSignalR } from './useSignalR';

const DISCONNECTED_SIGNALR_STATE = {
  status: 'disconnected' as const,
  lastError: undefined,
};

beforeEach(() => {
  vi.mocked(getJson).mockReset();
  vi.mocked(useSignalR).mockReturnValue(DISCONNECTED_SIGNALR_STATE);
  window.history.replaceState({}, '', '/');
});

async function flushEffects() {
  await act(async () => {
    await Promise.resolve();
  });
}

function mockGraphResponses(projectId: string, graphResponse: unknown) {
  const mockGetJson = vi.mocked(getJson);
  mockGetJson.mockImplementation((url: string) => {
    if (url.includes('/graph/snapshots')) {
      return Promise.resolve({ projectId, snapshots: [] });
    }
    if (url.includes('/graph?')) {
      return Promise.resolve(graphResponse);
    }
    return Promise.reject(new Error(`Unexpected URL: ${url}`));
  });
}

type CapturedNodes = { nodes: DiagramNode[] };

function ApiHarness({ projectId }: { projectId: string }) {
  const diagram = useDiagram(projectId);
  return <output>{JSON.stringify({ nodes: diagram.data.nodes })}</output>;
}

function decodeEntities(html: string): string {
  return html
    .replace(/&quot;/g, '"')
    .replace(/&amp;/g, '&')
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&apos;/g, "'");
}

function parseNodes(html: string): DiagramNode[] {
  const decoded = decodeEntities(html);
  const match = decoded.match(/>(\{"nodes":.*?\})</);
  if (!match?.[1]) throw new Error('Could not parse nodes from rendered output');
  return (JSON.parse(match[1]) as CapturedNodes).nodes;
}

describe('useDiagram parentId mapping from API', () => {
  let container: HTMLDivElement;
  let root: ReturnType<typeof createRoot>;

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
  });

  afterEach(() => {
    document.body.removeChild(container);
  });

  it('maps parentNodeId from API response to parentId on node', async () => {
    mockGraphResponses('proj-1', {
      projectId: 'proj-1',
      nodes: [
        { id: 'parent-1', name: 'Graph Service', externalResourceId: 'r1', level: 'Container', environment: 'production' },
        { id: 'child-1', name: 'Discovery Worker', externalResourceId: 'r2', level: 'Component', parentNodeId: 'parent-1', environment: 'production' },
      ],
      edges: [
        { id: 'e1', sourceNodeId: 'parent-1', targetNodeId: 'child-1', traffic: 1 },
      ],
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-1' }));
    });
    await flushEffects();

    const nodes = parseNodes(container.innerHTML);
    const child = nodes.find((n) => n.id === 'child-1');
    expect(child).toBeDefined();
    expect(child?.parentId).toBe('parent-1');
  });

  it('defaults to live graph fetch when snapshots exist', async () => {
    const mockGetJson = vi.mocked(getJson);
    mockGetJson.mockImplementation((url: string) => {
      if (url.includes('/graph/snapshots')) {
        return Promise.resolve({
          projectId: 'proj-live',
          snapshots: [{ snapshotId: 'snap-1', createdAtUtc: '2026-03-02T08:34:52Z', source: 'discovery' }],
        });
      }
      if (url.includes('/graph?')) {
        return Promise.resolve({ projectId: 'proj-live', nodes: [], edges: [] });
      }
      return Promise.reject(new Error(`Unexpected URL: ${url}`));
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-live' }));
    });
    await flushEffects();
    await flushEffects();

    const graphCalls = mockGetJson.mock.calls
      .map(([url]) => String(url))
      .filter((url) => url.includes('/graph?'));

    expect(graphCalls).toHaveLength(1);
    expect(graphCalls[0]).not.toContain('snapshotId=');
  });

  it('honors snapshotId from URL when provided', async () => {
    window.history.replaceState({}, '', '/diagram?snapshotId=snap-1');
    const mockGetJson = vi.mocked(getJson);
    mockGetJson.mockImplementation((url: string) => {
      if (url.includes('/graph/snapshots')) {
        return Promise.resolve({
          projectId: 'proj-snap',
          snapshots: [{ snapshotId: 'snap-1', createdAtUtc: '2026-03-02T08:34:52Z', source: 'discovery' }],
        });
      }
      if (url.includes('/graph?')) {
        return Promise.resolve({ projectId: 'proj-snap', nodes: [], edges: [] });
      }
      return Promise.reject(new Error(`Unexpected URL: ${url}`));
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-snap' }));
    });
    await flushEffects();

    const graphCalls = mockGetJson.mock.calls
      .map(([url]) => String(url))
      .filter((url) => url.includes('/graph?'));

    expect(graphCalls.length).toBeGreaterThan(0);
    expect(graphCalls[0]).toContain('snapshotId=snap-1');
  });

  it('uses serviceType from API response when available', async () => {
    mockGraphResponses('proj-st', {
      projectId: 'proj-st',
      nodes: [
        { id: 'db-1', name: 'my-storage-account', externalResourceId: 'r5', level: 'Container', serviceType: 'storage', environment: 'production' },
        { id: 'db-2', name: 'my-app', externalResourceId: 'r6', level: 'Container', serviceType: 'app', environment: 'production' },
      ],
      edges: [
        { id: 'e1', sourceNodeId: 'db-2', targetNodeId: 'db-1', traffic: 1 },
      ],
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-st' }));
    });
    await flushEffects();

    const nodes = parseNodes(container.innerHTML);
    const storageNode = nodes.find((n) => n.id === 'db-1');
    expect(storageNode?.serviceType).toBe('storage');
    const appNode = nodes.find((n) => n.id === 'db-2');
    expect(appNode?.serviceType).toBe('app');
  });

  it('falls back to name inference when serviceType is not provided', async () => {
    mockGraphResponses('proj-fb', {
      projectId: 'proj-fb',
      nodes: [
        { id: 'n1', name: 'postgres-db', externalResourceId: 'r7', level: 'Container', environment: 'production' },
        { id: 'n2', name: 'web-api', externalResourceId: 'r8', level: 'Container', environment: 'production' },
      ],
      edges: [
        { id: 'e1', sourceNodeId: 'n2', targetNodeId: 'n1', traffic: 1 },
      ],
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-fb' }));
    });
    await flushEffects();

    const nodes = parseNodes(container.innerHTML);
    const dbNode = nodes.find((n) => n.id === 'n1');
    expect(dbNode?.serviceType).toBe('database');
  });

  it('maps missing parentNodeId to undefined parentId on node', async () => {
    mockGraphResponses('proj-2', {
      projectId: 'proj-2',
      nodes: [
        { id: 'standalone-1', name: 'Identity API', externalResourceId: 'r3', level: 'Container', environment: 'production' },
        { id: 'standalone-2', name: 'PostgreSQL', externalResourceId: 'r4', level: 'Container', environment: 'production' },
      ],
      edges: [
        { id: 'e1', sourceNodeId: 'standalone-1', targetNodeId: 'standalone-2', traffic: 1 },
      ],
    });

    await act(async () => {
      root.render(createElement(ApiHarness, { projectId: 'proj-2' }));
    });
    await flushEffects();

    const nodes = parseNodes(container.innerHTML);
    const node = nodes.find((n) => n.id === 'standalone-1');
    expect(node).toBeDefined();
    expect(node?.parentId).toBeUndefined();
  });
});
