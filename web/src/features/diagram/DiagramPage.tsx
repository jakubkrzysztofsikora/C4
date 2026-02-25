import { useParams } from 'react-router-dom';
import { DiagramCanvas } from './components/DiagramCanvas';
import { useDiagram } from './hooks/useDiagram';
import { useDiagramExport } from './hooks/useDiagramExport';
import { useGraphLayout } from './hooks/useGraphLayout';
import { usePanZoom } from './hooks/usePanZoom';
import './diagram.css';

export function DiagramPage() {
  const { projectId } = useParams<{ projectId: string }>();
  const { data, level, setLevel, search, setSearch, timeline, setTimeline, loading, error } = useDiagram(projectId);
  const layouted = useGraphLayout(data);
  const { zoom, setZoom } = usePanZoom();
  const { exportAs } = useDiagramExport(layouted, projectId);

  return (
    <section className="diagram-grid">
      <aside className="diagram-sidebar">
        <h2 style={{ marginTop: 0 }}>Architecture Diagram</h2>
        <p className="subtle">Professional C4-style view with service icons, health overlays, and drift highlights.</p>

        {loading && <p>Loading graph data...</p>}
        {error !== undefined && <p style={{ color: '#e74c3c' }}>{error}</p>}

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
            <button className="btn" onClick={() => void exportAs('pdf')}>Export PDF</button>
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
