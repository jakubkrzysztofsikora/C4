import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { DiagramCanvas } from './components/DiagramCanvas';
import { useDiagram } from './hooks/useDiagram';
import { useDiagramExport } from './hooks/useDiagramExport';
import { useElkLayout } from './hooks/useElkLayout';
import { usePanZoom } from './hooks/usePanZoom';
import { useProject } from '../../shared/project/ProjectContext';
import { useToast } from '../../shared/hooks/useToast';
import { ToastContainer } from '../../shared/components/Toast';
import { getJson, getJsonOrNull, postJson } from '../../shared/api/client';
import './diagram.css';

const DEFAULTS = {
  level: 'Container',
  environment: 'all',
  scope: 'all',
  groupBy: 'domain',
  includeInfrastructure: 'false',
  hideOrphans: false,
  serviceType: 'all',
  technology: 'all',
  domain: 'all',
  risk: 'all',
  tag: '',
  driftOnly: false,
} as const;

type CurrentSubscriptionResponse = {
  subscriptionId: string;
  externalSubscriptionId: string;
  displayName: string;
};

type DiscoverResponse = {
  resourcesCount: number;
};

type DetectDriftResponse = {
  driftedCount: number;
  inSyncCount: number;
};

type DetectDriftRequest = {
  iacContent?: string | null;
  format?: string | null;
  useRepositories: boolean;
  environment?: string | null;
};

type DriftOverviewResponse = {
  projectId: string;
  driftedCount: number;
  driftedNodes: ReadonlyArray<{ nodeId: string; name: string; externalResourceId: string; level: string }>;
};

type SyncTelemetryResponse = {
  metricsIngested: number;
  healthMetricsIngested: number;
  dependencyMetricsIngested: number;
};

type CollapsibleSectionProps = {
  title: string;
  badge?: number;
  defaultOpen?: boolean;
  children: React.ReactNode;
};

function CollapsibleSection({ title, badge, defaultOpen, children }: CollapsibleSectionProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen ?? false);
  return (
    <div className="collapsible-section">
      <button
        className="collapsible-header"
        type="button"
        onClick={() => setIsOpen(prev => !prev)}
        aria-expanded={isOpen}
      >
        <span className="collapsible-title">{title}</span>
        {badge !== undefined && badge > 0 && (
          <span className="collapsible-badge">{badge}</span>
        )}
        <span className={`collapsible-chevron ${isOpen ? 'open' : ''}`}>&#9662;</span>
      </button>
      {isOpen && <div className="collapsible-body">{children}</div>}
    </div>
  );
}

export function DiagramPage() {
  const navigate = useNavigate();
  const { activeProject, loading: projectLoading } = useProject();
  const projectId = activeProject?.id;
  const {
    data,
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
    graphNotFound,
    signalR,
    isStale,
    lastRefreshAt,
    overlayMode,
    setOverlayMode,
    threatView,
    setThreatView,
    overlaySummary,
    captureSnapshot,
    refetch,
  } = useDiagram(projectId);
  const { layoutedData, groupNodes, isLayouting } = useElkLayout(data);
  const { zoom, setZoom } = usePanZoom();
  const { exportAs } = useDiagramExport(layoutedData, projectId);
  const { toasts, addToast, removeToast } = useToast();
  const [discoveringFromEmptyState, setDiscoveringFromEmptyState] = useState(false);
  const [driftIacContent, setDriftIacContent] = useState('');
  const [driftFormat, setDriftFormat] = useState<'bicep' | 'terraform'>('bicep');
  const [driftInputMode, setDriftInputMode] = useState<'repositories' | 'manual'>('repositories');
  const [runningDrift, setRunningDrift] = useState(false);
  const [runningTelemetrySync, setRunningTelemetrySync] = useState(false);
  const [latestDrift, setLatestDrift] = useState<DriftOverviewResponse | undefined>(undefined);
  const [loadingLatestDrift, setLoadingLatestDrift] = useState(false);

  const activeFilterChips = useMemo(() => {
    const chips: string[] = [];
    if (level !== DEFAULTS.level) chips.push(`Level: ${level}`);
    if (environment !== DEFAULTS.environment) chips.push(`Env: ${environment}`);
    if (scope !== DEFAULTS.scope) chips.push(`Scope: ${scope}`);
    if (groupBy !== DEFAULTS.groupBy) chips.push(`Group: ${groupBy}`);
    if (includeInfrastructure !== DEFAULTS.includeInfrastructure) chips.push(`Infra: ${includeInfrastructure}`);
    if (hideOrphans !== DEFAULTS.hideOrphans) chips.push('Hide unconnected');
    if (serviceTypeFilter !== DEFAULTS.serviceType) chips.push(`Type: ${serviceTypeFilter}`);
    if (technologyFilter !== DEFAULTS.technology) chips.push(`Tech: ${technologyFilter}`);
    if (domainFilter !== DEFAULTS.domain) chips.push(`Team: ${domainFilter}`);
    if (riskFilter !== DEFAULTS.risk) chips.push(`Risk: ${riskFilter}`);
    if (tagFilter !== DEFAULTS.tag) chips.push(`Tag: ${tagFilter}`);
    if (driftOnly !== DEFAULTS.driftOnly) chips.push('Drift only');
    if (search.length > 0) chips.push(`Search: ${search}`);
    return chips;
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
    tagFilter,
    driftOnly,
    search,
  ]);

  const advancedFilterCount = useMemo(() => {
    let count = 0;
    if (scope !== DEFAULTS.scope) count++;
    if (groupBy !== DEFAULTS.groupBy) count++;
    if (includeInfrastructure !== DEFAULTS.includeInfrastructure) count++;
    if (serviceTypeFilter !== DEFAULTS.serviceType) count++;
    if (technologyFilter !== DEFAULTS.technology) count++;
    if (domainFilter !== DEFAULTS.domain) count++;
    if (riskFilter !== DEFAULTS.risk) count++;
    if (hideOrphans !== DEFAULTS.hideOrphans) count++;
    if (driftOnly !== DEFAULTS.driftOnly) count++;
    if (tagFilter !== DEFAULTS.tag) count++;
    return count;
  }, [scope, groupBy, includeInfrastructure, serviceTypeFilter, technologyFilter, domainFilter, riskFilter, hideOrphans, driftOnly, tagFilter]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      if (event.key === '1') setLevel('Context');
      if (event.key === '2') setLevel('Container');
      if (event.key === '3') setLevel('Component');
      if (event.key === '4') setLevel('Code');
      if (event.key.toLowerCase() === 'r') resetToFullMap();
    }

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [setLevel]);

  function resetToFullMap() {
    setLevel('Container');
    setEnvironment('all');
    setScope('all');
    setGroupBy('domain');
    setIncludeInfrastructure('false');
    setHideOrphans(false);
    setServiceTypeFilter('all');
    setTechnologyFilter('all');
    setDomainFilter('all');
    setRiskFilter('all');
    setTagFilter('');
    setSearch('');
    setDriftOnly(false);
    setTimelineIndex(-1);
    setDiffEnabled(false);
    setOverlayMode('none');
    setThreatView('general');
  }

  async function handleDiscoverFromEmptyState() {
    if (projectId === undefined) return;

    setDiscoveringFromEmptyState(true);
    try {
      const subscription = await getJsonOrNull<CurrentSubscriptionResponse>('/api/discovery/subscriptions/current');
      if (subscription === null) {
        addToast('No Azure subscription connected. Connect one first.', 'error');
        navigate('/subscriptions');
        return;
      }

      const result = await postJson<{
        externalSubscriptionId: string;
        projectId: string;
        organizationId: string | null;
        sources: ReadonlyArray<'AzureSubscription'>;
      }, DiscoverResponse>(
        `/api/discovery/subscriptions/${subscription.subscriptionId}/discover`,
        {
          externalSubscriptionId: subscription.externalSubscriptionId,
          projectId,
          organizationId: null,
          sources: ['AzureSubscription'],
        },
      );

      addToast(`Discovery complete (${result.resourcesCount} resources).`, 'success');
      await refetch(projectId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Discovery failed', 'error');
    } finally {
      setDiscoveringFromEmptyState(false);
    }
  }

  async function handleExport(format: 'svg' | 'png' | 'pdf' | 'graphml') {
    try {
      await exportAs(format);
      addToast(`Exported ${format.toUpperCase()} diagram`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Export failed', 'error');
    }
  }

  async function handleCaptureSnapshot() {
    try {
      await captureSnapshot('manual');
      addToast('Snapshot captured', 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Snapshot capture failed', 'error');
    }
  }

  const refreshLatestDrift = useCallback(async () => {
    if (projectId === undefined) {
      setLatestDrift(undefined);
      return;
    }

    setLoadingLatestDrift(true);
    try {
      const latest = await getJson<DriftOverviewResponse>(`/api/projects/${projectId}/drift/runs/latest`);
      setLatestDrift(latest);
    } catch {
      setLatestDrift(undefined);
    } finally {
      setLoadingLatestDrift(false);
    }
  }, [projectId]);

  useEffect(() => {
    if (projectId === undefined || graphNotFound || metrics.totalNodes === 0) {
      setLatestDrift(undefined);
      return;
    }

    void refreshLatestDrift();
  }, [projectId, graphNotFound, metrics.totalNodes, refreshLatestDrift]);

  async function handleSyncTelemetry() {
    if (projectId === undefined) return;

    setRunningTelemetrySync(true);
    try {
      const result = await postJson<{ lookbackMinutes: number }, SyncTelemetryResponse>(
        `/api/projects/${projectId}/telemetry/sync`,
        { lookbackMinutes: 240 },
      );
      addToast(
        `Telemetry sync complete (${result.metricsIngested} metrics: ${result.healthMetricsIngested} health, ${result.dependencyMetricsIngested} dependencies).`,
        'success',
      );
      await refetch(projectId);
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Telemetry sync failed', 'error');
    } finally {
      setRunningTelemetrySync(false);
    }
  }

  async function handleRunDriftDetection() {
    if (projectId === undefined) return;
    if (driftInputMode === 'manual' && driftIacContent.trim().length === 0) {
      addToast('Paste IaC content before running drift detection.', 'error');
      return;
    }

    setRunningDrift(true);
    try {
      const subscription = await getJsonOrNull<CurrentSubscriptionResponse>('/api/discovery/subscriptions/current');
      if (subscription === null) {
        addToast('No Azure subscription connected.', 'error');
        navigate('/subscriptions');
        return;
      }

      const request: DetectDriftRequest = driftInputMode === 'manual'
        ? {
            iacContent: driftIacContent,
            format: driftFormat,
            useRepositories: false,
            environment: environment !== 'all' ? environment : null,
          }
        : {
            iacContent: null,
            format: null,
            useRepositories: true,
            environment: environment !== 'all' ? environment : null,
          };

      const result = await postJson<DetectDriftRequest, DetectDriftResponse>(
        `/api/discovery/subscriptions/${subscription.subscriptionId}/drift/run`,
        request,
      );
      addToast(`Drift detection complete (${result.driftedCount} drifted, ${result.inSyncCount} in sync).`, 'success');
      await refetch(projectId);
      await refreshLatestDrift();
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Drift detection failed', 'error');
    } finally {
      setRunningDrift(false);
    }
  }

  if (projectLoading) {
    return (
      <section className="diagram-grid">
        <div className="loading-state">
          <span className="spinner" />
          Loading project...
        </div>
      </section>
    );
  }

  if (activeProject === undefined) {
    return (
      <section className="diagram-grid">
        <div className="loading-state">
          No project selected. Create a project on the Organizations page.
        </div>
      </section>
    );
  }

  const selectedSnapshot = snapshots[timelineIndex];

  return (
    <section className="diagram-grid">
      <aside className="diagram-sidebar">
        <h2 style={{ marginTop: 0 }}>Architecture Diagram</h2>
        <p className="subtle">Truthful C4 architecture map with health, drift, risk, cost and security overlays.</p>

        {(loading || isLayouting) && (
          <div className="loading-state">
            <span className="spinner" />
            {isLayouting ? 'Computing layout...' : 'Loading graph data...'}
          </div>
        )}

        {graphNotFound && !loading && (
          <div className="card" style={{ marginBottom: 10 }}>
            <strong>No graph for the selected project.</strong>
            <p className="subtle" style={{ marginTop: 6 }}>
              Run discovery now to build the runtime map for this project.
            </p>
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              <button
                className="btn"
                type="button"
                disabled={discoveringFromEmptyState}
                onClick={() => void handleDiscoverFromEmptyState()}
              >
                {discoveringFromEmptyState ? 'Discovering...' : 'Discover resources'}
              </button>
              <button className="btn btn-sm" type="button" onClick={() => navigate('/subscriptions')}>
                Configure subscription
              </button>
            </div>
          </div>
        )}

        {error !== undefined && !graphNotFound && (
          <p style={{ color: 'var(--error)' }}>{error}</p>
        )}

        {isStale && (
          <div className="stale-banner" role="status">
            <strong>Data may be stale.</strong> Live connection is {signalR.status}.
            {lastRefreshAt !== undefined && (
              <span> Last refresh: {new Date(lastRefreshAt).toLocaleString()}.</span>
            )}
          </div>
        )}

        <div className="metrics-row">
          <span>Nodes: <strong>{metrics.renderedNodes}</strong> / {metrics.totalNodes}</span>
          <span>Edges: <strong>{metrics.renderedEdges}</strong> / {metrics.totalEdges}</span>
          <span>Filtered: {metrics.hiddenByFilters}</span>
          <span>Orphans hidden: {metrics.hiddenAsOrphans}</span>
          <span>Fallback classes: {metrics.fallbackClassificationCount}</span>
          <span>Unknown env: {metrics.unknownEnvironmentCount}</span>
          <span>Non-runtime: {metrics.nonRuntimeNodeCount}</span>
          <span>Raw IaC labels: {metrics.rawDeclarationLabelCount}</span>
        </div>

        {!telemetryMetrics.hasAnyTelemetry && (
          <div className="stale-banner" role="status">
            No live telemetry found for this view. Health/traffic overlays are shown as <strong>unknown</strong> until telemetry arrives.
            <div style={{ marginTop: 8 }}>
              <button
                className="btn btn-sm"
                type="button"
                disabled={runningTelemetrySync}
                onClick={() => void handleSyncTelemetry()}
              >
                {runningTelemetrySync ? 'Syncing telemetry...' : 'Sync telemetry now'}
              </button>
            </div>
          </div>
        )}

        {activeFilterChips.length > 0 && (
          <div className="filter-chips" aria-label="Active filters">
            {activeFilterChips.map((chip) => (
              <span key={chip} className="filter-chip">{chip}</span>
            ))}
            <button className="btn btn-sm" type="button" onClick={resetToFullMap}>
              Reset to full map <kbd>R</kbd>
            </button>
          </div>
        )}

        <div className="toolbox" role="group" aria-label="Diagram controls">
          <div className="level-selector">
            <label>C4 Level</label>
            <div className="level-options">
              {(['Context', 'Container', 'Component', 'Code'] as const).map((lvl, i) => (
                <button
                  key={lvl}
                  className={`level-btn ${level === lvl ? 'active' : ''}`}
                  type="button"
                  onClick={() => setLevel(lvl)}
                >
                  {lvl}
                  <kbd>{i + 1}</kbd>
                </button>
              ))}
            </div>
          </div>

          <label>
            Environment
            <select value={environment} onChange={(e) => setEnvironment(e.target.value)}>
              <option value="all">All environments</option>
              {environments.map((env) => (
                <option key={env} value={env}>{env}</option>
              ))}
            </select>
          </label>

          <label>
            Search
            <input placeholder="Search service" value={search} onChange={(e) => setSearch(e.target.value)} />
          </label>

          <CollapsibleSection title="Advanced Filters" badge={advancedFilterCount}>
            <label>
              Scope
              <select value={scope} onChange={(e) => setScope(e.target.value as 'all' | 'coreHub')}>
                <option value="all">All Nodes</option>
                <option value="coreHub">Core Hub</option>
              </select>
            </label>
            <label>
              Group By
              <select value={groupBy} onChange={(e) => setGroupBy(e.target.value as 'domain' | 'resourceGroup' | 'none')}>
                <option value="domain">Service Domain</option>
                <option value="resourceGroup">Resource Group</option>
                <option value="none">None</option>
              </select>
            </label>
            <label>
              Infrastructure
              <select value={includeInfrastructure} onChange={(e) => setIncludeInfrastructure(e.target.value as 'auto' | 'true' | 'false')}>
                <option value="false">Hide</option>
                <option value="auto">Auto (Component only)</option>
                <option value="true">Show</option>
              </select>
            </label>
            <label>
              Resource Type
              <select value={serviceTypeFilter} onChange={(e) => setServiceTypeFilter(e.target.value)}>
                <option value="all">All types</option>
                {serviceTypes.map((serviceType) => (
                  <option key={serviceType} value={serviceType}>{serviceType}</option>
                ))}
              </select>
            </label>
            <label>
              Technology
              <select value={technologyFilter} onChange={(e) => setTechnologyFilter(e.target.value)}>
                <option value="all">All technologies</option>
                {technologies.map((technology) => (
                  <option key={technology} value={technology}>{technology}</option>
                ))}
              </select>
            </label>
            <label>
              Team / Domain
              <select value={domainFilter} onChange={(e) => setDomainFilter(e.target.value)}>
                <option value="all">All domains</option>
                {domains.map((domain) => (
                  <option key={domain} value={domain}>{domain}</option>
                ))}
              </select>
            </label>
            <label>
              Risk
              <select value={riskFilter} onChange={(e) => setRiskFilter(e.target.value)}>
                <option value="all">All risk levels</option>
                {riskLevels.map((riskLevel) => (
                  <option key={riskLevel} value={riskLevel}>{riskLevel}</option>
                ))}
              </select>
            </label>
            <label className="checkbox-label">
              <input type="checkbox" checked={hideOrphans} onChange={(e) => setHideOrphans(e.target.checked)} />
              Hide unconnected
            </label>
            <label className="checkbox-label">
              <input type="checkbox" checked={driftOnly} onChange={(e) => setDriftOnly(e.target.checked)} />
              Drift only
            </label>
            <label>
              Tag / Metadata Filter
              <input list="diagram-tag-options" placeholder="Filter by tag or metadata" value={tagFilter} onChange={(e) => setTagFilter(e.target.value)} />
              <datalist id="diagram-tag-options">
                {tags.map((tag) => (
                  <option key={tag} value={tag} />
                ))}
              </datalist>
            </label>
          </CollapsibleSection>

          <CollapsibleSection title="Timeline & Diff">
            <label>
              Snapshot Timeline
              <input
                type="range"
                min={-1}
                max={Math.max(0, snapshots.length - 1)}
                value={timelineIndex < 0 ? -1 : Math.min(timelineIndex, Math.max(0, snapshots.length - 1))}
                onChange={(e) => setTimelineIndex(Number(e.target.value))}
                disabled={snapshots.length === 0}
              />
              <small className="subtle">
                {snapshots.length === 0
                  ? 'No snapshots available'
                  : selectedSnapshot !== undefined
                    ? `${new Date(selectedSnapshot.createdAtUtc).toLocaleString()} (${selectedSnapshot.source})`
                    : 'Live data (current graph)'}
              </small>
            </label>
            <div>
              <button className="btn btn-sm" type="button" onClick={() => void handleCaptureSnapshot()}>
                Capture Snapshot
              </button>
            </div>
            <label className="checkbox-label">
              <input type="checkbox" checked={diffEnabled} onChange={(e) => setDiffEnabled(e.target.checked)} />
              Diff mode
            </label>
            {diffEnabled && (
              <>
                <label>
                  Diff from
                  <select value={diffFromSnapshotId} onChange={(e) => setDiffFromSnapshotId(e.target.value)}>
                    {snapshots.map((snapshot) => (
                      <option key={snapshot.snapshotId} value={snapshot.snapshotId}>
                        {new Date(snapshot.createdAtUtc).toLocaleString()}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Diff to
                  <select value={diffToSnapshotId} onChange={(e) => setDiffToSnapshotId(e.target.value)}>
                    {snapshots.map((snapshot) => (
                      <option key={snapshot.snapshotId} value={snapshot.snapshotId}>
                        {new Date(snapshot.createdAtUtc).toLocaleString()}
                      </option>
                    ))}
                  </select>
                </label>
                <div className="metrics-row" aria-live="polite">
                  <span>Added nodes: <strong>{diffMetrics.addedNodes}</strong></span>
                  <span>Removed nodes: <strong>{diffMetrics.removedNodes}</strong></span>
                  <span>Added edges: <strong>{diffMetrics.addedEdges}</strong></span>
                  <span>Removed edges: <strong>{diffMetrics.removedEdges}</strong></span>
                </div>
                {diffMetrics.addedNodes === 0 && diffMetrics.removedNodes === 0 && diffMetrics.addedEdges === 0 && diffMetrics.removedEdges === 0 && (
                  <small className="subtle">No structural change between selected snapshots.</small>
                )}
              </>
            )}
          </CollapsibleSection>

          <CollapsibleSection title="Overlays">
            <label>
              Overlay
              <select value={overlayMode} onChange={(e) => setOverlayMode(e.target.value as 'none' | 'threat' | 'cost' | 'security')}>
                <option value="none">None</option>
                <option value="threat">Threat</option>
                <option value="security">Security</option>
                <option value="cost">Cost</option>
              </select>
            </label>
            {overlayMode === 'threat' && (
              <label>
                Threat View
                <select
                  value={threatView}
                  onChange={(e) => setThreatView(e.target.value as 'general' | 'api-attack-surface' | 'exit-points' | 'data-exposure' | 'blast-radius')}
                >
                  <option value="general">General</option>
                  <option value="api-attack-surface">API attack surface</option>
                  <option value="exit-points">Exit points</option>
                  <option value="data-exposure">Data exposure</option>
                  <option value="blast-radius">Blast radius</option>
                </select>
              </label>
            )}
          </CollapsibleSection>

          <CollapsibleSection title="Export">
            <label>
              Zoom
              <input type="range" min={50} max={150} value={zoom * 100} onChange={(e) => setZoom(Number(e.target.value) / 100)} />
            </label>
            <div className="export-row">
              <button className="btn" onClick={() => void handleExport('svg')} type="button">Export SVG</button>
              <button className="btn" onClick={() => void handleExport('png')} type="button">Export PNG</button>
              <button className="btn" onClick={() => void handleExport('pdf')} type="button">Export PDF</button>
              <button className="btn" onClick={() => void handleExport('graphml')} type="button">Export GraphML</button>
            </div>
          </CollapsibleSection>
        </div>

        {overlaySummary !== undefined && overlayMode !== 'none' && (
          <div className="overlay-summary">
            <strong>{overlaySummary.title}</strong>
            <ul>
              {overlaySummary.lines.slice(0, 10).map((line) => (
                <li key={line}>{line}</li>
              ))}
            </ul>
          </div>
        )}

        <div className="legend">
          <span className="badge green">Green: healthy/normal</span>
          <span className="badge yellow">Yellow: degraded</span>
          <span className="badge red">Red: critical</span>
          <span className="badge unknown">Gray: telemetry unknown</span>
          <span className="badge drift">Drift detected</span>
          <span className="badge added">Diff: added</span>
          <span className="badge removed">Diff: removed</span>
          <span className="subtle">Traffic thresholds: red if error ≥5% or p95 ≥2000ms; yellow if error ≥1% or p95 ≥800ms.</span>
        </div>

        <div className="card" style={{ marginTop: 10 }}>
          <strong>Run Drift Detection</strong>
          <p className="subtle" style={{ marginTop: 6, marginBottom: 10 }}>
            Run drift detection from configured repositories (recommended) or from pasted IaC content.
          </p>
          <div className="subtle" style={{ fontSize: 13, marginBottom: 10 }}>
            {loadingLatestDrift
              ? 'Loading latest drift status...'
              : latestDrift !== undefined
                ? `Latest drifted resources: ${latestDrift.driftedCount}`
                : 'Latest drift status is unavailable.'}
          </div>
          {latestDrift !== undefined && latestDrift.driftedNodes.length > 0 && (
            <div style={{ marginBottom: 10, maxHeight: 120, overflowY: 'auto' }}>
              {latestDrift.driftedNodes.slice(0, 6).map((node) => (
                <div key={node.nodeId} className="subtle" style={{ fontSize: 12 }}>
                  {node.name} ({node.level})
                </div>
              ))}
            </div>
          )}
          <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
            <label style={{ display: 'flex', flexDirection: 'column', gap: 4, minWidth: 220 }}>
              Source
              <select value={driftInputMode} onChange={(e) => setDriftInputMode(e.target.value as 'repositories' | 'manual')}>
                <option value="repositories">Configured repositories</option>
                <option value="manual">Manual IaC paste</option>
              </select>
            </label>
          </div>
          {driftInputMode === 'repositories' && (
            <p className="subtle" style={{ fontSize: 12, marginTop: 0, marginBottom: 10 }}>
              Uses all configured repository entries. You can specify multiple repos in Subscription settings (one per line as
              {' '}
              <code>url|branch|rootPath</code>
              ).
            </p>
          )}
          <div style={{ display: 'flex', gap: 8, marginBottom: 8 }}>
            {driftInputMode === 'manual' && (
              <select value={driftFormat} onChange={(e) => setDriftFormat(e.target.value as 'bicep' | 'terraform')}>
                <option value="bicep">Bicep</option>
                <option value="terraform">Terraform</option>
              </select>
            )}
            <button className="btn btn-sm" type="button" disabled={runningDrift} onClick={() => void handleRunDriftDetection()}>
              {runningDrift ? 'Running...' : 'Run Drift Detection'}
            </button>
          </div>
          {driftInputMode === 'manual' && (
            <textarea
              value={driftIacContent}
              onChange={(e) => setDriftIacContent(e.target.value)}
              placeholder="Paste IaC content here"
              style={{
                width: '100%',
                minHeight: 120,
                background: 'var(--input-bg)',
                color: 'var(--text)',
                border: '1px solid var(--border)',
                borderRadius: 8,
                padding: 10,
                fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace',
                fontSize: 12,
              }}
            />
          )}
        </div>

        {level === 'Code' && !loading && !graphNotFound && data.nodes.length === 0 && (
          <div className="card" style={{ marginTop: 10 }}>
            <strong>No code artifacts available</strong>
            <p className="subtle" style={{ marginTop: 6, marginBottom: 0 }}>
              Code-level view requires repository-derived code metadata. Configure repository discovery and rerun discovery.
            </p>
          </div>
        )}
      </aside>
      <DiagramCanvas data={layoutedData} groupNodes={groupNodes} overlayMode={overlayMode} />
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </section>
  );
}
