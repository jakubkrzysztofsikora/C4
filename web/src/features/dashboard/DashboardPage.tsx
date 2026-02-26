import { useState } from 'react';
import { MdHub, MdSearch } from 'react-icons/md';
import { useDashboard } from './useDashboard';

export function DashboardPage() {
  const [projectIdInput, setProjectIdInput] = useState('');
  const [activeProjectId, setActiveProjectId] = useState<string | undefined>(undefined);
  const { graph, loading, error } = useDashboard(activeProjectId);

  function handleLoadProject() {
    if (!projectIdInput) return;
    setActiveProjectId(projectIdInput);
  }

  return (
    <section className="fade-in">
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Dynamic Architecture Dashboard</h1>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 20 }}>
        Connect Azure and start discovering your C4 architecture graph.
      </p>

      <div className="card" style={{ marginBottom: 16 }}>
        <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', flexWrap: 'wrap' }}>
          <div className="form-group" style={{ flex: 1, minWidth: 200 }}>
            <label className="form-label" htmlFor="project-id-input">Project ID</label>
            <input
              className="input"
              id="project-id-input"
              placeholder="Enter project ID to load graph"
              value={projectIdInput}
              onChange={(e) => setProjectIdInput(e.target.value)}
              disabled={loading}
              onKeyDown={(e) => e.key === 'Enter' && handleLoadProject()}
            />
          </div>
          <button
            className="btn btn-primary"
            onClick={handleLoadProject}
            disabled={loading || !projectIdInput}
            style={{ alignSelf: 'flex-end' }}
            type="button"
          >
            {loading ? (
              <>
                <span className="spinner spinner-sm" />
                Loading...
              </>
            ) : (
              <>
                <MdSearch size={16} />
                Load Project
              </>
            )}
          </button>
        </div>
      </div>

      {error !== undefined && (
        <div className="card" style={{ borderColor: 'var(--error)', marginBottom: 16 }}>
          <p style={{ color: 'var(--error)', margin: 0 }}>{error}</p>
        </div>
      )}

      {loading && activeProjectId !== undefined && (
        <div className="card">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="skeleton" style={{ height: 20, width: '40%' }} />
            <div className="skeleton" style={{ height: 14, width: '25%' }} />
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginTop: 8 }}>
              {[1, 2, 3].map(i => (
                <div key={i} className="skeleton" style={{ height: 40, borderRadius: 8 }} />
              ))}
            </div>
          </div>
        </div>
      )}

      {graph !== undefined && !loading && (
        <div className="card fade-in">
          <h2 style={{ marginTop: 0, marginBottom: 4 }}>Project: {graph.projectId}</h2>
          <p className="subtle" style={{ marginTop: 0, marginBottom: 16 }}>
            {graph.nodes.length} node{graph.nodes.length !== 1 ? 's' : ''} &bull; {graph.edges.length} edge{graph.edges.length !== 1 ? 's' : ''}
          </p>
          <h3 style={{ marginTop: 0, marginBottom: 10 }}>Resources</h3>
          <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: 8 }}>
            {graph.nodes.map((node) => (
              <li
                key={node.id}
                style={{
                  padding: '10px 14px',
                  background: 'var(--panel-2)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  gap: 8,
                  flexWrap: 'wrap',
                }}
              >
                <strong>{node.name}</strong>
                <span className="subtle" style={{ fontSize: 13 }}>
                  {node.level} &bull; {node.externalResourceId}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      {graph === undefined && activeProjectId === undefined && !loading && (
        <div className="card">
          <div className="empty-state">
            <MdHub className="empty-state-icon" />
            <p className="empty-state-title">No project loaded</p>
            <p className="empty-state-description">
              Enter a project ID above to load architecture data and explore your C4 architecture graph.
            </p>
          </div>
        </div>
      )}
    </section>
  );
}
