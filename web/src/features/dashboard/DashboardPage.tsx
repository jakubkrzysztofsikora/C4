import { useState } from 'react';
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
    <section>
      <h1>Dynamic Architecture Dashboard</h1>
      <p>Connect Azure and start discovering your C4 architecture graph.</p>

      <div style={{ marginTop: 16 }}>
        <label>
          Project ID
          <input
            placeholder="Enter project ID to load graph"
            value={projectIdInput}
            onChange={(e) => setProjectIdInput(e.target.value)}
            disabled={loading}
            style={{ marginLeft: 8 }}
          />
        </label>
        <button onClick={handleLoadProject} disabled={loading || !projectIdInput} style={{ marginLeft: 8 }}>
          {loading ? 'Loading...' : 'Load Project'}
        </button>
      </div>

      {error !== undefined && <p style={{ color: '#e74c3c', marginTop: 12 }}>{error}</p>}

      {graph !== undefined && (
        <div style={{ marginTop: 16 }}>
          <h2>Project: {graph.projectId}</h2>
          <p>{graph.nodes.length} nodes, {graph.edges.length} edges</p>
          <h3>Resources</h3>
          <ul>
            {graph.nodes.map((node) => (
              <li key={node.id}>
                <strong>{node.name}</strong> ({node.level}) - {node.externalResourceId}
              </li>
            ))}
          </ul>
        </div>
      )}

      {graph === undefined && activeProjectId === undefined && !loading && (
        <p style={{ marginTop: 16, color: '#8896b3' }}>Enter a project ID above to load architecture data from the API.</p>
      )}
    </section>
  );
}
