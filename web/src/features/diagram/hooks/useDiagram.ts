import { useCallback, useEffect, useMemo, useState } from 'react';
import { getJson, ApiError } from '../../../shared/api/client';
import { DiagramData, DiagramNode, DiagramEdge } from '../types';

type GraphNodeDto = {
  id: string;
  name: string;
  externalResourceId: string;
  level: string;
};

type GraphEdgeDto = {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
};

type GraphDto = {
  projectId: string;
  nodes: ReadonlyArray<GraphNodeDto>;
  edges: ReadonlyArray<GraphEdgeDto>;
};

function mapLevel(level: string): DiagramNode['level'] {
  const normalized = level.toLowerCase();
  if (normalized === 'context') return 'Context';
  if (normalized === 'component') return 'Component';
  return 'Container';
}

function inferServiceType(name: string): DiagramNode['serviceType'] {
  const lower = name.toLowerCase();
  if (lower.includes('database') || lower.includes('sql') || lower.includes('postgres')) return 'database';
  if (lower.includes('cache') || lower.includes('redis')) return 'cache';
  if (lower.includes('queue') || lower.includes('bus') || lower.includes('worker')) return 'queue';
  if (lower.includes('api') || lower.includes('service')) return 'api';
  if (lower.includes('external') || lower.includes('azure') || lower.includes('cloud')) return 'external';
  return 'app';
}

function mapGraphDtoToDiagramData(dto: GraphDto): DiagramData {
  const nodes: DiagramNode[] = dto.nodes.map((node) => ({
    id: node.id,
    label: node.name,
    level: mapLevel(node.level),
    health: 'green' as const,
    serviceType: inferServiceType(node.name),
  }));

  const edges: DiagramEdge[] = dto.edges.map((edge) => ({
    id: edge.id,
    from: edge.sourceNodeId,
    to: edge.targetNodeId,
    traffic: 1,
  }));

  return { nodes, edges };
}

const seed: DiagramData = {
  nodes: [
    { id: 'n1', label: 'Frontend SPA', level: 'Container', health: 'green', serviceType: 'app' },
    { id: 'n2', label: 'Identity API', level: 'Container', health: 'green', serviceType: 'api' },
    { id: 'n3', label: 'Discovery Worker', level: 'Component', health: 'yellow', serviceType: 'queue' },
    { id: 'n4', label: 'Graph Service', level: 'Container', health: 'green', serviceType: 'api' },
    { id: 'n5', label: 'PostgreSQL', level: 'Container', health: 'green', serviceType: 'database' },
    { id: 'n6', label: 'Redis Cache', level: 'Container', health: 'yellow', drift: true, serviceType: 'cache' },
    { id: 'n7', label: 'Azure Resource Graph', level: 'Context', health: 'green', serviceType: 'external' },
  ],
  edges: [
    { id: 'e1', from: 'n1', to: 'n2', traffic: 0.9 },
    { id: 'e2', from: 'n2', to: 'n4', traffic: 0.76 },
    { id: 'e3', from: 'n4', to: 'n5', traffic: 0.63 },
    { id: 'e4', from: 'n3', to: 'n7', traffic: 0.52 },
    { id: 'e5', from: 'n4', to: 'n6', traffic: 0.45 },
  ],
};

function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}

function extractErrorMessage(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return 'An unexpected error occurred';
}

export function useDiagram(projectId?: string) {
  const [level, setLevel] = useState<'Context' | 'Container' | 'Component'>('Container');
  const [search, setSearch] = useState('');
  const [timeline, setTimeline] = useState(100);
  const [apiData, setApiData] = useState<DiagramData | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);

  const fetchGraph = useCallback(async (id: string, graphLevel?: string) => {
    setLoading(true);
    setError(undefined);
    try {
      const levelParam = graphLevel !== undefined ? `?level=${graphLevel}` : '';
      const dto = await getJson<GraphDto>(`/api/projects/${id}/graph${levelParam}`);
      const mapped = mapGraphDtoToDiagramData(dto);
      setApiData(mapped);
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setError(message);
      setApiData(undefined);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (projectId !== undefined) {
      void fetchGraph(projectId, level);
    }
  }, [projectId, level, fetchGraph]);

  const sourceData = apiData ?? seed;

  const data = useMemo(() => {
    const levelFiltered = sourceData.nodes.filter((n) =>
      level === 'Container' ? n.level !== 'Context' : n.level === level,
    );
    const searchFiltered = levelFiltered.filter((n) =>
      n.label.toLowerCase().includes(search.toLowerCase()),
    );
    const visibleNodeIds = new Set(searchFiltered.map((n) => n.id));

    return {
      nodes: searchFiltered,
      edges: sourceData.edges.filter(
        (e) => visibleNodeIds.has(e.from) && visibleNodeIds.has(e.to) && e.traffic <= timeline / 100,
      ),
    };
  }, [sourceData, level, search, timeline]);

  return { data, level, setLevel, search, setSearch, timeline, setTimeline, loading, error, refetch: fetchGraph };
}
