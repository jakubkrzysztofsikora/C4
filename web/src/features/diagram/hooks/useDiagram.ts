import { useCallback, useEffect, useMemo, useState } from 'react';
import { getJson, postJson, ApiError } from '../../../shared/api/client';
import { DiagramData, DiagramNode, DiagramEdge, ServiceType, RiskLevel } from '../types';
import { useSignalR } from './useSignalR';

type GraphNodeDto = {
  id: string;
  name: string;
  externalResourceId: string;
  level: string;
  health?: string;
  healthScore?: number;
  telemetryStatus?: string;
  requestRate?: number;
  errorRate?: number;
  p95LatencyMs?: number;
  riskLevel?: string;
  hourlyCostUsd?: number;
  parentNodeId?: string;
  drift?: boolean;
  environment?: string;
  serviceType?: string;
  technology?: string;
  resourceGroup?: string;
  domain?: string;
  isInfrastructure?: boolean;
  classificationSource?: string;
  classificationConfidence?: number;
  groupKey?: string;
  tags?: ReadonlyArray<string>;
};

type GraphEdgeDto = {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
  traffic?: number;
  trafficState?: string;
  requestRate?: number;
  errorRate?: number;
  p95LatencyMs?: number;
  protocol?: string;
};

type GraphDto = {
  projectId: string;
  nodes: ReadonlyArray<GraphNodeDto>;
  edges: ReadonlyArray<GraphEdgeDto>;
};

type GraphSnapshotsResponse = {
  projectId: string;
  snapshots: ReadonlyArray<{ snapshotId: string; createdAtUtc: string; source: string }>;
};

type GraphDiffResponse = {
  addedNodes: ReadonlyArray<string>;
  removedNodes: ReadonlyArray<string>;
  addedEdges: ReadonlyArray<string>;
  removedEdges: ReadonlyArray<string>;
};

type CreateGraphSnapshotResponse = {
  snapshotId: string;
  createdAtUtc: string;
  source: string;
};

type NodeHealthEntry = {
  nodeId: string;
  health: DiagramNode['health'];
};

type OverlayMode = 'none' | 'threat' | 'cost' | 'security';
type ThreatView = 'general' | 'api-attack-surface' | 'exit-points' | 'data-exposure' | 'blast-radius';

type OverlaySummary = {
  title: string;
  lines: string[];
};

function getInitialParam(name: string, fallback: string): string {
  try {
    const url = new URL(window.location.href);
    const value = url.searchParams.get(name);
    return value ?? fallback;
  } catch {
    return fallback;
  }
}

function getInitialBoolParam(name: string, fallback: boolean): boolean {
  const value = getInitialParam(name, fallback ? 'true' : 'false').toLowerCase();
  return value === 'true' || value === '1' || value === 'yes';
}

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
const VALID_RISK_LEVELS: ReadonlySet<string> = new Set<RiskLevel>(['low', 'medium', 'high', 'critical']);

function isValidServiceType(value: unknown): value is ServiceType {
  return typeof value === 'string' && VALID_SERVICE_TYPES.has(value);
}

function isValidRiskLevel(value: unknown): value is RiskLevel {
  return typeof value === 'string' && VALID_RISK_LEVELS.has(value);
}

function resolveHealth(value: string | undefined): DiagramNode['health'] {
  if (value === 'green' || value === 'yellow' || value === 'red' || value === 'unknown') return value;
  return 'unknown';
}

function isOverlayMode(value: string): value is OverlayMode {
  return value === 'none' || value === 'threat' || value === 'cost' || value === 'security';
}

function mapGraphDtoToDiagramData(dto: GraphDto): DiagramData {
  const nodes: DiagramNode[] = (dto.nodes ?? []).map((node) => ({
    id: node.id,
    label: node.name,
    externalResourceId: node.externalResourceId,
    level: mapLevel(node.level),
    health: resolveHealth(node.health),
    telemetryStatus: node.telemetryStatus === 'known' ? 'known' : 'unknown',
    ...(typeof node.requestRate === 'number' ? { requestRate: node.requestRate } : {}),
    ...(typeof node.errorRate === 'number' ? { errorRate: node.errorRate } : {}),
    ...(typeof node.p95LatencyMs === 'number' ? { p95LatencyMs: node.p95LatencyMs } : {}),
    ...(isValidRiskLevel(node.riskLevel) ? { riskLevel: node.riskLevel } : {}),
    ...(typeof node.hourlyCostUsd === 'number' ? { hourlyCostUsd: node.hourlyCostUsd } : {}),
    serviceType: isValidServiceType(node.serviceType) ? node.serviceType : inferServiceType(node.name),
    ...(node.technology ? { technology: node.technology } : {}),
    environment: node.environment ?? 'unknown',
    ...(node.resourceGroup ? { resourceGroup: node.resourceGroup } : {}),
    ...(node.domain ? { domain: node.domain } : {}),
    ...(node.groupKey ? { groupKey: node.groupKey } : {}),
    ...(Array.isArray(node.tags) && node.tags.length > 0 ? { tags: node.tags.filter((tag) => typeof tag === 'string' && tag.length > 0) } : {}),
    ...(typeof node.isInfrastructure === 'boolean' ? { isInfrastructure: node.isInfrastructure } : {}),
    ...(node.classificationSource ? { classificationSource: node.classificationSource } : {}),
    ...(typeof node.classificationConfidence === 'number' ? { classificationConfidence: node.classificationConfidence } : {}),
    ...(node.parentNodeId !== undefined && { parentId: node.parentNodeId }),
    ...(node.drift === true && { drift: true }),
  }));

  const edges: DiagramEdge[] = (dto.edges ?? []).map((edge) => ({
    id: edge.id,
    from: edge.sourceNodeId,
    to: edge.targetNodeId,
    traffic: edge.traffic ?? 0,
    ...(edge.trafficState === 'green' || edge.trafficState === 'yellow' || edge.trafficState === 'red' || edge.trafficState === 'unknown'
      ? { trafficState: edge.trafficState }
      : {}),
    ...(typeof edge.requestRate === 'number' ? { requestRate: edge.requestRate } : {}),
    ...(typeof edge.errorRate === 'number' ? { errorRate: edge.errorRate } : {}),
    ...(typeof edge.p95LatencyMs === 'number' ? { p95LatencyMs: edge.p95LatencyMs } : {}),
    ...(edge.protocol ? { protocol: edge.protocol } : {}),
  }));

  return { nodes, edges };
}

function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}

function extractErrorMessage(err: unknown): string {
  if (isApiError(err)) return err.message;
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
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
        ((item as Record<string, unknown>)['health'] === 'green'
          || (item as Record<string, unknown>)['health'] === 'yellow'
          || (item as Record<string, unknown>)['health'] === 'red'),
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
    return updatedHealth !== undefined
      ? { ...node, health: updatedHealth, telemetryStatus: 'known' }
      : node;
  });
}

function isVisibleAtLevel(node: DiagramNode, level: 'Context' | 'Container' | 'Component'): boolean {
  if (level === 'Context') return node.level === 'Context';
  if (level === 'Container') return node.level === 'Container' || node.level === 'Component';
  return node.level === 'Component';
}

const EMPTY_DIAGRAM_DATA: DiagramData = { nodes: [], edges: [] };

export function useDiagram(projectId?: string) {
  const [level, setLevel] = useState<'Context' | 'Container' | 'Component'>(() => {
    const initial = getInitialParam('level', 'Container');
    return initial === 'Context' || initial === 'Component' ? initial : 'Container';
  });
  const [search, setSearch] = useState(() => getInitialParam('search', ''));
  const [environment, setEnvironment] = useState(() => getInitialParam('environment', 'all'));
  const [scope, setScope] = useState<'all' | 'coreHub'>(() => (getInitialParam('scope', 'all') === 'coreHub' ? 'coreHub' : 'all'));
  const [groupBy, setGroupBy] = useState<'domain' | 'resourceGroup' | 'none'>(() => {
    const initial = getInitialParam('groupBy', 'domain');
    return initial === 'resourceGroup' || initial === 'none' ? initial : 'domain';
  });
  const [includeInfrastructure, setIncludeInfrastructure] = useState<'auto' | 'true' | 'false'>(() => {
    const initial = getInitialParam('includeInfrastructure', 'false');
    return initial === 'true' || initial === 'auto' ? initial : 'false';
  });
  const [hideOrphans, setHideOrphans] = useState(() => getInitialBoolParam('hideOrphans', false));
  const [serviceTypeFilter, setServiceTypeFilter] = useState(() => getInitialParam('serviceType', 'all'));
  const [technologyFilter, setTechnologyFilter] = useState(() => getInitialParam('technology', 'all'));
  const [domainFilter, setDomainFilter] = useState(() => getInitialParam('domain', 'all'));
  const [riskFilter, setRiskFilter] = useState(() => getInitialParam('risk', 'all'));
  const [tagFilter, setTagFilter] = useState(() => getInitialParam('tag', ''));
  const [driftOnly, setDriftOnly] = useState(() => getInitialBoolParam('driftOnly', false));

  const [snapshots, setSnapshots] = useState<ReadonlyArray<{ snapshotId: string; createdAtUtc: string; source: string }>>([]);
  const [selectedSnapshotId, setSelectedSnapshotId] = useState<string | undefined>(() => {
    const initialSnapshotId = getInitialParam('snapshotId', '').trim();
    return initialSnapshotId.length > 0 ? initialSnapshotId : undefined;
  });
  const [diffEnabled, setDiffEnabled] = useState(() => getInitialBoolParam('diff', false));
  const [diffFromSnapshotId, setDiffFromSnapshotId] = useState<string>(() => getInitialParam('diffFrom', '').trim());
  const [diffToSnapshotId, setDiffToSnapshotId] = useState<string>(() => getInitialParam('diffTo', '').trim());
  const [diffResult, setDiffResult] = useState<GraphDiffResponse | undefined>(undefined);

  const [overlayMode, setOverlayMode] = useState<OverlayMode>(() => {
    const initial = getInitialParam('overlay', 'none');
    return isOverlayMode(initial) ? initial : 'none';
  });
  const [threatView, setThreatView] = useState<ThreatView>(() => {
    const initial = getInitialParam('threatView', 'general');
    if (initial === 'api-attack-surface' || initial === 'exit-points' || initial === 'data-exposure' || initial === 'blast-radius') {
      return initial;
    }
    return 'general';
  });
  const [overlaySummary, setOverlaySummary] = useState<OverlaySummary | undefined>(undefined);

  const [apiData, setApiData] = useState<DiagramData | undefined>(undefined);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | undefined>(undefined);
  const [lastRefreshAt, setLastRefreshAt] = useState<number | undefined>(undefined);

  const timelineIndex = useMemo(() => {
    if (selectedSnapshotId === undefined) return -1;
    return snapshots.findIndex((snapshot) => snapshot.snapshotId === selectedSnapshotId);
  }, [snapshots, selectedSnapshotId]);

  const setTimelineIndex = useCallback((nextIndex: number) => {
    if (!Number.isFinite(nextIndex) || nextIndex < 0 || snapshots.length === 0) {
      setSelectedSnapshotId(undefined);
      return;
    }

    const clampedIndex = Math.min(Math.max(Math.trunc(nextIndex), 0), snapshots.length - 1);
    setSelectedSnapshotId(snapshots[clampedIndex]?.snapshotId);
  }, [snapshots]);

  const fetchGraph = useCallback(async (id: string, snapshotId?: string) => {
    setLoading(true);
    setError(undefined);
    try {
      const params = new URLSearchParams({
        level,
        scope,
        groupBy,
        includeInfrastructure,
        environment,
      });
      if (snapshotId !== undefined && snapshotId.length > 0) {
        params.set('snapshotId', snapshotId);
      }

      const dto = await getJson<GraphDto>(`/api/projects/${id}/graph?${params.toString()}`);
      const mapped = mapGraphDtoToDiagramData(dto);
      setApiData(mapped);
      setLastRefreshAt(Date.now());
    } catch (err: unknown) {
      setError(extractErrorMessage(err));
      setApiData(undefined);
    } finally {
      setLoading(false);
    }
  }, [level, scope, groupBy, includeInfrastructure, environment]);

  const fetchSnapshots = useCallback(async (id: string) => {
    try {
      const response = await getJson<GraphSnapshotsResponse>(`/api/projects/${id}/graph/snapshots`);
      const ordered = [...(response.snapshots ?? [])].sort((a, b) =>
        new Date(a.createdAtUtc).getTime() - new Date(b.createdAtUtc).getTime(),
      );
      setSnapshots(ordered);
      const snapshotIds = new Set(ordered.map((snapshot) => snapshot.snapshotId));

      setSelectedSnapshotId((current) => {
        if (current === undefined) return current;
        return snapshotIds.has(current) ? current : undefined;
      });

      if (ordered.length > 0) {
        const defaultFrom = ordered[Math.max(0, ordered.length - 2)]?.snapshotId ?? ordered[0]!.snapshotId;
        const defaultTo = ordered[ordered.length - 1]!.snapshotId;

        setDiffFromSnapshotId((current) => (snapshotIds.has(current) ? current : defaultFrom));
        setDiffToSnapshotId((current) => (snapshotIds.has(current) ? current : defaultTo));
      } else {
        setDiffFromSnapshotId('');
        setDiffToSnapshotId('');
        setDiffResult(undefined);
      }
    } catch {
      setSnapshots([]);
      setSelectedSnapshotId(undefined);
    }
  }, []);

  const fetchDiff = useCallback(async (id: string, fromSnapshotId: string, toSnapshotId: string) => {
    if (fromSnapshotId.length === 0 || toSnapshotId.length === 0 || fromSnapshotId === toSnapshotId) {
      setDiffResult(undefined);
      return;
    }

    try {
      const diff = await getJson<GraphDiffResponse>(
        `/api/projects/${id}/graph/diff?fromSnapshotId=${encodeURIComponent(fromSnapshotId)}&toSnapshotId=${encodeURIComponent(toSnapshotId)}`,
      );
      setDiffResult(diff);
    } catch {
      setDiffResult(undefined);
    }
  }, []);

  useEffect(() => {
    if (projectId === undefined) return;
    void fetchSnapshots(projectId);
  }, [projectId, fetchSnapshots]);

  useEffect(() => {
    if (projectId === undefined) return;
    void fetchGraph(projectId, selectedSnapshotId);
  }, [projectId, selectedSnapshotId, fetchGraph]);

  useEffect(() => {
    if (projectId === undefined || !diffEnabled) {
      setDiffResult(undefined);
      return;
    }

    void fetchDiff(projectId, diffFromSnapshotId, diffToSnapshotId);
  }, [projectId, diffEnabled, diffFromSnapshotId, diffToSnapshotId, fetchDiff]);

  const handleHealthOverlayChanged = useCallback((_receivedProjectId: string, healthJson: string) => {
    setApiData((current) => {
      if (current === undefined) return current;
      const overlay = parseHealthOverlay(healthJson);
      return { ...current, nodes: applyHealthOverlay(current.nodes, overlay) };
    });
  }, []);

  const handleDiagramUpdated = useCallback(() => {
    if (projectId !== undefined) {
      void fetchGraph(projectId, selectedSnapshotId);
    }
  }, [projectId, selectedSnapshotId, fetchGraph]);

  const signalR = useSignalR(projectId, {
    onHealthOverlayChanged: handleHealthOverlayChanged,
    onDiagramUpdated: handleDiagramUpdated,
  });

  useEffect(() => {
    if (projectId === undefined) return;
    if (signalR.status !== 'connected') return;
    void fetchGraph(projectId, selectedSnapshotId);
  }, [projectId, signalR.status, signalR.lastConnectedAt, selectedSnapshotId, fetchGraph]);

  useEffect(() => {
    if (projectId === undefined) return;
    if (signalR.status === 'connected') return;

    const id = window.setInterval(() => {
      void fetchGraph(projectId, selectedSnapshotId);
    }, 60_000);

    return () => {
      window.clearInterval(id);
    };
  }, [projectId, signalR.status, selectedSnapshotId, fetchGraph]);

  const captureSnapshot = useCallback(async (source = 'manual'): Promise<void> => {
    if (projectId === undefined) return;

    const created = await postJson<{ source: string }, CreateGraphSnapshotResponse>(
      `/api/projects/${projectId}/graph/snapshots`,
      { source },
    );
    await fetchSnapshots(projectId);
    setSelectedSnapshotId(created.snapshotId);
  }, [projectId, fetchSnapshots]);

  useEffect(() => {
    try {
      const params = new URLSearchParams(window.location.search);
      params.set('level', level);
      params.set('environment', environment);
      params.set('scope', scope);
      params.set('groupBy', groupBy);
      params.set('includeInfrastructure', includeInfrastructure);
      params.set('hideOrphans', hideOrphans ? 'true' : 'false');
      params.set('serviceType', serviceTypeFilter);
      params.set('technology', technologyFilter);
      params.set('domain', domainFilter);
      params.set('risk', riskFilter);
      params.set('driftOnly', driftOnly ? 'true' : 'false');
      if (search.length > 0) params.set('search', search); else params.delete('search');
      if (tagFilter.length > 0) params.set('tag', tagFilter); else params.delete('tag');
      if (selectedSnapshotId !== undefined) params.set('snapshotId', selectedSnapshotId); else params.delete('snapshotId');
      params.set('overlay', overlayMode);
      params.set('diff', diffEnabled ? 'true' : 'false');
      if (diffFromSnapshotId.length > 0) params.set('diffFrom', diffFromSnapshotId); else params.delete('diffFrom');
      if (diffToSnapshotId.length > 0) params.set('diffTo', diffToSnapshotId); else params.delete('diffTo');
      params.set('threatView', threatView);
      const next = `${window.location.pathname}?${params.toString()}`;
      window.history.replaceState(null, '', next);
    } catch {
      // no-op
    }
  }, [
    level,
    environment,
    scope,
    groupBy,
    includeInfrastructure,
    hideOrphans,
    serviceTypeFilter,
    technologyFilter,
    domainFilter,
    riskFilter,
    driftOnly,
    search,
    tagFilter,
    selectedSnapshotId,
    overlayMode,
    diffEnabled,
    diffFromSnapshotId,
    diffToSnapshotId,
    threatView,
  ]);

  const sourceData = apiData ?? EMPTY_DIAGRAM_DATA;

  const environments = useMemo(() => {
    const envSet = new Set(sourceData.nodes.map((n) => n.environment ?? 'unknown'));
    return Array.from(envSet).sort();
  }, [sourceData]);

  const serviceTypes = useMemo(() => {
    const set = new Set(sourceData.nodes.map((n) => n.serviceType));
    return Array.from(set).sort();
  }, [sourceData]);

  const domains = useMemo(() => {
    const set = new Set(sourceData.nodes.map((n) => n.domain ?? 'General'));
    return Array.from(set).sort();
  }, [sourceData]);

  const technologies = useMemo(() => {
    const set = new Set(sourceData.nodes.map((n) => n.technology).filter((value): value is string => value !== undefined && value.length > 0));
    return Array.from(set).sort();
  }, [sourceData]);

  const tags = useMemo(() => {
    const set = new Set<string>();
    for (const node of sourceData.nodes) {
      for (const tag of node.tags ?? []) {
        if (tag.length > 0) set.add(tag);
      }
    }
    return Array.from(set).sort();
  }, [sourceData]);

  const riskLevels = useMemo(() => {
    const set = new Set(sourceData.nodes.map((n) => n.riskLevel).filter((v): v is RiskLevel => v !== undefined));
    return Array.from(set).sort();
  }, [sourceData]);

  const data = useMemo(() => {
    const lowerSearch = search.toLowerCase();
    const lowerTag = tagFilter.toLowerCase();

    const levelFiltered = sourceData.nodes.filter((n) => isVisibleAtLevel(n, level));

    const filtered = levelFiltered.filter((n) => {
      if (serviceTypeFilter !== 'all' && n.serviceType !== serviceTypeFilter) return false;
      if (technologyFilter !== 'all' && (n.technology ?? 'unknown') !== technologyFilter) return false;
      if (domainFilter !== 'all' && (n.domain ?? 'General') !== domainFilter) return false;
      if (riskFilter !== 'all' && n.riskLevel !== riskFilter) return false;
      if (driftOnly && n.drift !== true) return false;

      if (lowerSearch.length > 0) {
        const matchesSearch =
          n.label.toLowerCase().includes(lowerSearch)
          || (n.externalResourceId ?? '').toLowerCase().includes(lowerSearch)
          || (n.domain ?? '').toLowerCase().includes(lowerSearch);
        if (!matchesSearch) return false;
      }

      if (lowerTag.length > 0) {
        const matchesExplicitTag = (n.tags ?? []).some((tag) => tag.toLowerCase().includes(lowerTag));
        const matchesTag =
          matchesExplicitTag
          || (n.technology ?? '').toLowerCase().includes(lowerTag)
          || (n.domain ?? '').toLowerCase().includes(lowerTag)
          || n.label.toLowerCase().includes(lowerTag)
          || (n.externalResourceId ?? '').toLowerCase().includes(lowerTag)
          || (n.resourceGroup ?? '').toLowerCase().includes(lowerTag)
          || (n.classificationSource ?? '').toLowerCase().includes(lowerTag)
          || (n.groupKey ?? '').toLowerCase().includes(lowerTag);
        if (!matchesTag) return false;
      }

      return true;
    });

    const visibleNodeIds = new Set(filtered.map((n) => n.id));
    const connectedEdges = sourceData.edges.filter(
      (e) => visibleNodeIds.has(e.from) && visibleNodeIds.has(e.to),
    );

    let nodes = filtered;
    let edges = connectedEdges;

    if (hideOrphans) {
      const connectedNodeIds = new Set<string>();
      for (const edge of connectedEdges) {
        connectedNodeIds.add(edge.from);
        connectedNodeIds.add(edge.to);
      }
      nodes = filtered.filter((n) => connectedNodeIds.has(n.id));
      const nodeIds = new Set(nodes.map((n) => n.id));
      edges = connectedEdges.filter((e) => nodeIds.has(e.from) && nodeIds.has(e.to));
    }

    if (diffEnabled && diffResult !== undefined) {
      const addedNodes = new Set(diffResult.addedNodes ?? []);
      const removedNodes = new Set(diffResult.removedNodes ?? []);
      const addedEdges = new Set(diffResult.addedEdges ?? []);
      const removedEdges = new Set(diffResult.removedEdges ?? []);

      nodes = nodes.map((node) => {
        const key = node.externalResourceId ?? node.id;
        if (addedNodes.has(key)) return { ...node, diffStatus: 'added' };
        if (removedNodes.has(key)) return { ...node, diffStatus: 'removed' };
        return { ...node, diffStatus: 'unchanged' };
      });

      edges = edges.map((edge) => {
        const key = `${edge.from}->${edge.to}`;
        if (addedEdges.has(key)) return { ...edge, diffStatus: 'added' };
        if (removedEdges.has(key)) return { ...edge, diffStatus: 'removed' };
        return { ...edge, diffStatus: 'unchanged' };
      });
    }

    return { nodes, edges, preOrphanCount: filtered.length };
  }, [
    sourceData,
    level,
    search,
    serviceTypeFilter,
    technologyFilter,
    domainFilter,
    riskFilter,
    tagFilter,
    driftOnly,
    hideOrphans,
    diffEnabled,
    diffResult,
  ]);

  const metrics = useMemo(() => {
    const totalNodes = sourceData.nodes.length;
    const totalEdges = sourceData.edges.length;
    const renderedNodes = data.nodes.length;
    const renderedEdges = data.edges.length;
    const hiddenByFilters = Math.max(0, totalNodes - data.preOrphanCount);
    const hiddenAsOrphans = Math.max(0, data.preOrphanCount - renderedNodes);

    return {
      totalNodes,
      totalEdges,
      renderedNodes,
      renderedEdges,
      hiddenByFilters,
      hiddenAsOrphans,
    };
  }, [sourceData, data]);

  const diffMetrics = useMemo(() => ({
    addedNodes: diffResult?.addedNodes?.length ?? 0,
    removedNodes: diffResult?.removedNodes?.length ?? 0,
    addedEdges: diffResult?.addedEdges?.length ?? 0,
    removedEdges: diffResult?.removedEdges?.length ?? 0,
  }), [diffResult]);

  const telemetryMetrics = useMemo(() => {
    const knownNodes = data.nodes.filter((node) => node.telemetryStatus === 'known').length;
    const knownEdges = data.edges.filter((edge) => edge.trafficState !== undefined && edge.trafficState !== 'unknown').length;
    return {
      knownNodes,
      knownEdges,
      hasAnyTelemetry: knownNodes > 0 || knownEdges > 0,
    };
  }, [data.nodes, data.edges]);

  const visibleDiagramData = useMemo<DiagramData>(() => ({
    nodes: data.nodes,
    edges: data.edges,
  }), [data.nodes, data.edges]);

  useEffect(() => {
    if (projectId === undefined || overlayMode === 'none') {
      setOverlaySummary(undefined);
      return;
    }

    const load = async () => {
      try {
        if (overlayMode === 'threat') {
          const threat = await getJson<{ riskLevel: string; threats: Array<{ component: string; threatType: string; severity: string; mitigation: string }> }>(
            `/api/projects/${projectId}/threats?view=${encodeURIComponent(threatView)}`,
          );
          setOverlaySummary({
            title: `Threat Overlay (${threat.riskLevel})`,
            lines: (threat.threats ?? []).slice(0, 20).map((t) => `${t.component}: ${t.threatType} [${t.severity}]`),
          });
          return;
        }

        if (overlayMode === 'cost') {
          const cost = await getJson<{ totalHourlyCostUsd: number; topCostNodes: Array<{ name: string; hourlyCostUsd: number }> }>(`/api/projects/${projectId}/cost`);
          setOverlaySummary({
            title: `Cost Overlay ($${cost.totalHourlyCostUsd.toFixed(2)}/hr)`,
            lines: (cost.topCostNodes ?? []).slice(0, 20).map((n) => `${n.name}: $${n.hourlyCostUsd.toFixed(2)}/hr`),
          });
          return;
        }

        const security = await getJson<{ totalFindings: number; findings: Array<{ severity: string; nodeName: string; message: string }> }>(`/api/projects/${projectId}/security-findings`);
        setOverlaySummary({
          title: `Security Overlay (${security.totalFindings} findings)`,
          lines: (security.findings ?? []).slice(0, 20).map((f) => `[${f.severity}] ${f.nodeName}: ${f.message}`),
        });
      } catch {
        setOverlaySummary({
          title: 'Overlay unavailable',
          lines: ['No data available for the selected overlay.'],
        });
      }
    };

    void load();
  }, [projectId, overlayMode, threatView]);

  const isStale = useMemo(() => {
    if (signalR.status === 'connected') return false;
    return lastRefreshAt !== undefined;
  }, [signalR.status, lastRefreshAt]);

  return {
    data: visibleDiagramData,
    level,
    setLevel,
    search,
    setSearch,
    environment,
    setEnvironment,
    environments,
    scope,
    setScope,
    groupBy,
    setGroupBy,
    includeInfrastructure,
    setIncludeInfrastructure,
    hideOrphans,
    setHideOrphans,
    serviceTypeFilter,
    setServiceTypeFilter,
    serviceTypes,
    technologyFilter,
    setTechnologyFilter,
    technologies,
    tags,
    domainFilter,
    setDomainFilter,
    domains,
    riskFilter,
    setRiskFilter,
    riskLevels,
    tagFilter,
    setTagFilter,
    driftOnly,
    setDriftOnly,
    snapshots,
    timelineIndex,
    setTimelineIndex,
    diffEnabled,
    setDiffEnabled,
    diffFromSnapshotId,
    setDiffFromSnapshotId,
    diffToSnapshotId,
    setDiffToSnapshotId,
    diffMetrics,
    telemetryMetrics,
    metrics,
    loading,
    error,
    signalR,
    isStale,
    lastRefreshAt,
    overlayMode,
    setOverlayMode,
    threatView,
    setThreatView,
    overlaySummary,
    captureSnapshot,
    refetch: fetchGraph,
  };
}
