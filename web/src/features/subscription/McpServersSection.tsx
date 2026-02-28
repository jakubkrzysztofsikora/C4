import { useState } from 'react';
import { MdAdd, MdDelete, MdDns } from 'react-icons/md';
import { useMcpServers } from './useMcpServers';

type McpServersSectionProps = {
  projectId: string | undefined;
};

export function McpServersSection({ projectId }: McpServersSectionProps) {
  const { servers, loading, error, addServer, removeServer } = useMcpServers(projectId);
  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [endpoint, setEndpoint] = useState('');

  async function handleAdd() {
    if (name.trim().length === 0 || endpoint.trim().length === 0) return;
    await addServer(name.trim(), endpoint.trim());
    setName('');
    setEndpoint('');
    setShowForm(false);
  }

  if (projectId === undefined) {
    return null;
  }

  return (
    <div style={{ marginTop: 32 }}>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 12 }}>
        <h2 style={{ margin: 0 }}>MCP Servers</h2>
        {!showForm && (
          <button
            className="btn btn-sm"
            type="button"
            onClick={() => setShowForm(true)}
            style={{ gap: 4 }}
          >
            <MdAdd size={16} />
            Add Server
          </button>
        )}
      </div>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 16 }}>
        Connect MCP servers to discover additional resources via tool calls.
      </p>

      {error !== undefined && (
        <div className="card" style={{ borderColor: 'var(--error)', marginBottom: 12 }}>
          <p style={{ color: 'var(--error)', margin: 0, fontSize: 13 }}>{error}</p>
        </div>
      )}

      {showForm && (
        <div className="card" style={{ marginBottom: 12 }}>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div>
              <label htmlFor="mcp-name" style={{ display: 'block', fontSize: 13, fontWeight: 600, marginBottom: 4 }}>
                Server Name
              </label>
              <input
                id="mcp-name"
                type="text"
                className="input"
                placeholder="My MCP Server"
                value={name}
                onChange={e => setName(e.target.value)}
                style={{ width: '100%' }}
              />
            </div>
            <div>
              <label htmlFor="mcp-endpoint" style={{ display: 'block', fontSize: 13, fontWeight: 600, marginBottom: 4 }}>
                Endpoint URL
              </label>
              <input
                id="mcp-endpoint"
                type="url"
                className="input"
                placeholder="https://my-mcp-server.example.com"
                value={endpoint}
                onChange={e => setEndpoint(e.target.value)}
                style={{ width: '100%' }}
              />
            </div>
            <div style={{ display: 'flex', gap: 8 }}>
              <button
                className="btn btn-primary btn-sm"
                type="button"
                disabled={loading || name.trim().length === 0 || endpoint.trim().length === 0}
                onClick={() => void handleAdd()}
              >
                {loading ? <span className="spinner spinner-sm" /> : 'Add'}
              </button>
              <button
                className="btn btn-sm"
                type="button"
                onClick={() => { setShowForm(false); setName(''); setEndpoint(''); }}
              >
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      {servers.length === 0 && !showForm && (
        <div className="card" style={{ textAlign: 'center', padding: '24px 16px' }}>
          <MdDns size={32} style={{ color: 'var(--muted)', marginBottom: 8 }} />
          <p style={{ color: 'var(--muted)', margin: 0, fontSize: 14 }}>
            No MCP servers configured yet.
          </p>
        </div>
      )}

      {servers.length > 0 && (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {servers.map(server => (
            <div
              key={server.id}
              className="card"
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                padding: '12px 16px',
              }}
            >
              <div>
                <strong style={{ fontSize: 14 }}>{server.name}</strong>
                <div style={{ fontSize: 12, color: 'var(--muted)', fontFamily: 'monospace', marginTop: 2 }}>
                  {server.endpoint}
                </div>
              </div>
              <button
                className="btn btn-sm"
                type="button"
                onClick={() => void removeServer(server.id)}
                style={{ color: 'var(--error)', borderColor: 'var(--error)', padding: '4px 8px' }}
                title="Remove server"
              >
                <MdDelete size={16} />
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
