import { DiagramCanvas } from './components/DiagramCanvas';
import { useDiagram } from './hooks/useDiagram';
import { useDiagramExport } from './hooks/useDiagramExport';
import { useGraphLayout } from './hooks/useGraphLayout';
import { usePanZoom } from './hooks/usePanZoom';
import { useProject } from '../../shared/project/ProjectContext';
import './diagram.css';

export function DiagramPage() {
  const { activeProject, loading: projectLoading } = useProject();
  const projectId = activeProject?.id;
  const { data, level, setLevel, search, setSearch, timeline, setTimeline, environment, setEnvironment, environments, loading, error } = useDiagram(projectId);
  const layouted = useGraphLayout(data);
  const { zoom, setZoom } = usePanZoom();
  const { exportAs } = useDiagramExport(layouted, projectId);

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

  return (
    <section className="diagram-grid">
      <aside className="diagram-sidebar">
        <h2 style={{ marginTop: 0 }}>Architecture Diagram</h2>
        <p className="subtle">Professional C4-style view with service icons, health overlays, and drift highlights.</p>

        {loading && (
          <div className="loading-state">
            <span className="spinner" />
            Loading graph data...
          </div>
        )}

        {error !== undefined && (
          <p style={{ color: 'var(--error)' }}>{error}</p>
        )}

        <div className="toolbox">
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
            Filter
            <input placeholder="Search service" value={search} onChange={(e) => setSearch(e.target.value)} />
          </label>
          <label>
            Timeline
            <input type="range" min={0} max={100} value={timeline} onChange={(e) => setTimeline(Number(e.target.value))} />
          </label>
          <label>
            Zoom
            <input type="range" min={50} max={150} value={zoom * 100} onChange={(e) => setZoom(Number(e.target.value) / 100)} />
          </label>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn" onClick={() => void exportAs('svg')}>Export SVG</button>
            <button className="btn" onClick={() => void exportAs('pdf')}>Export PNG</button>
          </div>
        </div>
        <div className="legend">
          <span className="badge green">Green: healthy</span>
          <span className="badge yellow">Yellow: degraded</span>
          <span className="badge red">Red: critical</span>
          <span className="badge drift">Drift detected</span>
        </div>
      </aside>
      <DiagramCanvas data={layouted} />
    </section>
  );
}
