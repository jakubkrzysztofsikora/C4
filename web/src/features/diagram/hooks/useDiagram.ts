import { useCallback, useEffect, useMemo, useState } from 'react';
import { getJson, ApiError } from '../../../shared/api/client';
import { DiagramData, DiagramNode, DiagramEdge, ServiceType } from '../types';
import { useSignalR } from './useSignalR';

type GraphNodeDto = {
  id: string;
  name: string;
  externalResourceId: string;
  level: string;
  health?: string;
  healthScore?: number;
  parentNodeId?: string;
  drift?: boolean;
  environment?: string;
  serviceType?: string;
  resourceGroup?: string;
};

type GraphEdgeDto = {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  traffic?: number;
};

type GraphDto = {
  projectId: string;
  nodes: ReadonlyArray<GraphNodeDto>;
  edges: ReadonlyArray<GraphEdgeDto>;
};

type NodeHealthEntry = {
  nodeId: string;
  health: DiagramNode['health'];
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

const VALID_SERVICE_TYPES: ReadonlySet<string> = new Set<ServiceType>(['app', 'api', 'database', 'queue', 'cache', 'storage', 'monitoring', 'external', 'boundary']);

function isValidServiceType(value: unknown): value is ServiceType {
  return typeof value === 'string' && VALID_SERVICE_TYPES.has(value);
}

function resolveHealth(value: string | undefined): DiagramNode['health'] {
  if (isValidHealth(value)) return value;
  return 'green';
}

function mapGraphDtoToDiagramData(dto: GraphDto): DiagramData {
  const nodes: DiagramNode[] = (dto.nodes ?? []).map((node) => ({
    id: node.id,
    label: node.name,
    level: mapLevel(node.level),
    health: resolveHealth(node.health),
    serviceType: isValidServiceType(node.serviceType) ? node.serviceType : inferServiceType(node.name),
    environment: node.environment ?? 'unknown',
    ...(node.resourceGroup ? { resourceGroup: node.resourceGroup } : {}),
    ...(node.parentNodeId !== undefined && { parentId: node.parentNodeId }),
    ...(node.drift === true && { drift: true }),
  }));

  const edges: DiagramEdge[] = (dto.edges ?? []).map((edge) => ({
    id: edge.id,
    from: edge.sourceNodeId,
    to: edge.targetNodeId,
    traffic: edge.traffic ?? 1,
  }));

  return { nodes, edges };
}

const seed: DiagramData = {
  nodes: [
    { id: 'n1', label: 'Frontend SPA', level: 'Container', health: 'green', serviceType: 'app', environment: 'production' },
    { id: 'n2', label: 'Identity API', level: 'Container', health: 'green', serviceType: 'api', environment: 'production' },
    { id: 'n3', label: 'Discovery Worker', level: 'Component', health: 'yellow', serviceType: 'queue', parentId: 'n4', environment: 'production' },
    { id: 'n4', label: 'Graph Service', level: 'Container', health: 'green', serviceType: 'api', environment: 'production' },
    { id: 'n5', label: 'PostgreSQL', level: 'Container', health: 'green', serviceType: 'database', environment: 'production' },
    { id: 'n6', label: 'Redis Cache', level: 'Container', health: 'yellow', drift: true, serviceType: 'cache', environment: 'production' },
    { id: 'n7', label: 'Azure Resource Graph', level: 'Context', health: 'green', serviceType: 'external', environment: 'production' },
  ],
  edges: [
    { id: 'e1', from: 'n1', to: 'n2', traffic: 0.9 },
    { id: 'e2', from: 'n2', to: 'n4', traffic: 0.76 },
    { id: 'e3', from: 'n4', to: 'n5', traffic: 0.63 },
    { id: 'e4', from: 'n3', to: 'n7', traffic: 0.52 },
    { id: 'e5', from: 'n4', to: 'n6', traffic: 0.45 },
    { id: 'e6', from: 'n4', to: 'n3', traffic: 0.7 },
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

function isValidHealth(value: unknown): value is DiagramNode['health'] {
  return value === 'green' || value === 'yellow' || value === 'red';
}

function parseHealthOverlay(healthJson: string): NodeHealthEntry[] {
  try {
    const parsed: unknown = JSON.parse(healthJson);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter(
      (item): item is NodeHealthEntry =>
        typeof item === 'object' &&
        item !== null &&
        typeof (item as Record<string, unknown>)['nodeId'] === 'string' &&
        isValidHealth((item as Record<string, unknown>)['health']),
    );
  } catch {
    return [];
  }
}

function applyHealthOverlay(nodes: DiagramNode[], overlay: NodeHealthEntry[]): DiagramNode[] {
  if (overlay.length === 0) return nodes;
  const healthMap = new Map(overlay.map((e) => [e.nodeId, e.health]));
  return nodes.map((node) => {
    const updatedHealth = healthMap.get(node.id);
    return updatedHealth !== undefined ? { ...node, health: updatedHealth } : node;
  });
}

export function useDiagram(projectId?: string) {
  const [level, setLevel] = useState<'Context' | 'Container' | 'Component'>('Container');
  const [search, setSearch] = useState('');
  const [timeline, setTimeline] = useState(100);
  const [environment, setEnvironment] = useState('production');
  const [hideOrphans, setHideOrphans] = useState(true);
  const [apiData, setApiData] = useState<DiagramData | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);

  const fetchGraph = useCallback(async (id: string) => {
    setLoading(true);
    setError(undefined);
    try {
      const dto = await getJson<GraphDto>(`/api/projects/${id}/graph`);
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
      void fetchGraph(projectId);
    }
  }, [projectId, fetchGraph]);

  const handleHealthOverlayChanged = useCallback((_receivedProjectId: string, healthJson: string) => {
    setApiData((current) => {
      if (current === undefined) return current;
      const overlay = parseHealthOverlay(healthJson);
      const updatedNodes = applyHealthOverlay(current.nodes, overlay);
      return { ...current, nodes: updatedNodes };
    });
  }, []);

  const handleDiagramUpdated = useCallback(() => {
    if (projectId !== undefined) {
      void fetchGraph(projectId);
    }
  }, [projectId, fetchGraph]);

  useSignalR(projectId, {
    onHealthOverlayChanged: handleHealthOverlayChanged,
    onDiagramUpdated: handleDiagramUpdated,
  });

  const sourceData = apiData ?? (error !== undefined ? { nodes: [], edges: [] } : seed);

  const environments = useMemo(() => {
    const envSet = new Set(sourceData.nodes.map((n) => n.environment ?? 'unknown'));
    return Array.from(envSet).sort();
  }, [sourceData]);

  const data = useMemo(() => {
    const levelFiltered = sourceData.nodes.filter((n) =>
      level === 'Container' ? n.level !== 'Context' : n.level === level,
    );
    const envFiltered = environment === 'all'
      ? levelFiltered
      : levelFiltered.filter((n) => n.environment === environment);
    const searchFiltered = envFiltered.filter((n) =>
      n.label.toLowerCase().includes(search.toLowerCase()),
    );
    const visibleNodeIds = new Set(searchFiltered.map((n) => n.id));
    const edges = sourceData.edges.filter(
      (e) => visibleNodeIds.has(e.from) && visibleNodeIds.has(e.to) && e.traffic <= timeline / 100,
    );

    if (!hideOrphans) {
      return { nodes: searchFiltered, edges };
    }

    const connectedNodeIds = new Set<string>();
    for (const e of edges) {
      connectedNodeIds.add(e.from);
      connectedNodeIds.add(e.to);
    }

    return {
      nodes: searchFiltered.filter((n) => connectedNodeIds.has(n.id)),
      edges,
    };
  }, [sourceData, level, search, timeline, environment, hideOrphans]);

  return { data, level, setLevel, search, setSearch, timeline, setTimeline, environment, setEnvironment, environments, hideOrphans, setHideOrphans, loading, error, refetch: fetchGraph };
}
