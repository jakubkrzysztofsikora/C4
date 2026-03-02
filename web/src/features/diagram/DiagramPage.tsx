import { useEffect, useMemo } from 'react';
import { DiagramCanvas } from './components/DiagramCanvas';
import { useDiagram } from './hooks/useDiagram';
import { useDiagramExport } from './hooks/useDiagramExport';
import { useElkLayout } from './hooks/useElkLayout';
import { usePanZoom } from './hooks/usePanZoom';
import { useProject } from '../../shared/project/ProjectContext';
import { useToast } from '../../shared/hooks/useToast';
import { ToastContainer } from '../../shared/components/Toast';
import './diagram.css';

const DEFAULTS = {
  level: 'Container',
  environment: 'all',
  scope: 'all',
  groupBy: 'domain',
  includeInfrastructure: 'false',
  hideOrphans: false,
  serviceType: 'all',
  domain: 'all',
  risk: 'all',
  tag: '',
  driftOnly: false,
} as const;

export function DiagramPage() {
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
    metrics,
    loading,
    error,
    signalR,
    isStale,
    lastRefreshAt,
    overlayMode,
    setOverlayMode,
    overlaySummary,
  } = useDiagram(projectId);
  const { layoutedData, groupNodes, isLayouting } = useElkLayout(data);
  const { zoom, setZoom } = usePanZoom();
  const { exportAs } = useDiagramExport(layoutedData, projectId);
  const { toasts, addToast, removeToast } = useToast();

  const activeFilterChips = useMemo(() => {
    const chips: string[] = [];
    if (level !== DEFAULTS.level) chips.push(`Level: ${level}`);
    if (environment !== DEFAULTS.environment) chips.push(`Env: ${environment}`);
    if (scope !== DEFAULTS.scope) chips.push(`Scope: ${scope}`);
    if (groupBy !== DEFAULTS.groupBy) chips.push(`Group: ${groupBy}`);
    if (includeInfrastructure !== DEFAULTS.includeInfrastructure) chips.push(`Infra: ${includeInfrastructure}`);
    if (hideOrphans !== DEFAULTS.hideOrphans) chips.push('Hide unconnected');
    if (serviceTypeFilter !== DEFAULTS.serviceType) chips.push(`Type: ${serviceTypeFilter}`);
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
    domainFilter,
    riskFilter,
    tagFilter,
    driftOnly,
    search,
  ]);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.target instanceof HTMLInputElement || event.target instanceof HTMLTextAreaElement) {
        return;
      }

      if (event.key === '1') setLevel('Context');
      if (event.key === '2') setLevel('Container');
      if (event.key === '3') setLevel('Component');
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
    setDomainFilter('all');
    setRiskFilter('all');
    setTagFilter('');
    setSearch('');
    setDriftOnly(false);
    setDiffEnabled(false);
  }

  async function handleExport(format: 'svg' | 'png' | 'pdf' | 'graphml') {
    try {
      await exportAs(format);
      addToast(`Exported ${format.toUpperCase()} diagram`, 'success');
    } catch (err) {
      addToast(err instanceof Error ? err.message : 'Export failed', 'error');
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

        {error !== undefined && (
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
        </div>

        {activeFilterChips.length > 0 && (
          <div className="filter-chips" aria-label="Active filters">
            {activeFilterChips.map((chip) => (
              <span key={chip} className="filter-chip">{chip}</span>
            ))}
            <button className="btn btn-sm" type="button" onClick={resetToFullMap}>Reset to full map</button>
          </div>
        )}

        <div className="toolbox" role="group" aria-label="Diagram controls">
          <label>
            C4 Level
            <select value={level} onChange={(e) => setLevel(e.target.value as 'Context' | 'Container' | 'Component')}>
              <option>Context</option>
              <option>Container</option>
              <option>Component</option>
            </select>
          </label>
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
            Search
            <input placeholder="Search service" value={search} onChange={(e) => setSearch(e.target.value)} />
          </label>
          <label>
            Tag / Metadata Filter
            <input placeholder="Filter by tag or metadata" value={tagFilter} onChange={(e) => setTagFilter(e.target.value)} />
          </label>

          <label>
            Snapshot Timeline
            <input
              type="range"
              min={0}
              max={Math.max(0, snapshots.length - 1)}
              value={Math.min(timelineIndex, Math.max(0, snapshots.length - 1))}
              onChange={(e) => setTimelineIndex(Number(e.target.value))}
              disabled={snapshots.length <= 1}
            />
            <small className="subtle">
              {selectedSnapshot !== undefined
                ? `${new Date(selectedSnapshot.createdAtUtc).toLocaleString()} (${selectedSnapshot.source})`
                : 'No snapshots available'}
            </small>
          </label>

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
            </>
          )}

          <label>
            Overlay
            <select value={overlayMode} onChange={(e) => setOverlayMode(e.target.value as 'none' | 'threat' | 'cost' | 'security')}>
              <option value="none">None</option>
              <option value="threat">Threat</option>
              <option value="security">Security</option>
              <option value="cost">Cost</option>
            </select>
          </label>

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
        </div>
      </aside>
      <DiagramCanvas data={layoutedData} groupNodes={groupNodes} overlayMode={overlayMode} />
      <ToastContainer toasts={toasts} onRemove={removeToast} />
    </section>
  );
}
