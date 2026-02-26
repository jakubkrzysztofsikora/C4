import { useState } from 'react';
import { MdBusiness, MdAdd, MdFolder } from 'react-icons/md';
import { useOrganizations } from './useOrganizations';

export function OrganizationProjectsPage() {
  const {
    organizationId,
    organizationName,
    projects,
    loading,
    error,
    registerOrganization,
    createProject,
  } = useOrganizations();

  const [orgNameInput, setOrgNameInput] = useState('');
  const [projectNameInput, setProjectNameInput] = useState('');

  async function handleRegisterOrganization() {
    if (!orgNameInput) return;
    await registerOrganization(orgNameInput);
    setOrgNameInput('');
  }

  async function handleCreateProject() {
    if (!projectNameInput || organizationId === undefined) return;
    await createProject(organizationId, projectNameInput);
    setProjectNameInput('');
  }

  return (
    <section className="fade-in">
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Organization &amp; Projects</h1>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 20 }}>
        Manage your organization and its architecture projects.
      </p>

      {error !== undefined && (
        <div className="card" style={{ borderColor: 'var(--error)', marginBottom: 16 }}>
          <p style={{ color: 'var(--error)', margin: 0 }}>{error}</p>
        </div>
      )}

      {organizationId === undefined ? (
        <div className="card">
          <div className="empty-state">
            <MdBusiness className="empty-state-icon" />
            <p className="empty-state-title">Set up your organization</p>
            <p className="empty-state-description">
              Register your organization to start managing projects and connecting Azure resources.
            </p>
            <div style={{ display: 'flex', gap: 10, width: '100%', maxWidth: 360 }}>
              <div className="form-group" style={{ flex: 1 }}>
                <label className="form-label" htmlFor="org-name-input">Organization Name</label>
                <input
                  className="input"
                  id="org-name-input"
                  placeholder="Acme Corp"
                  value={orgNameInput}
                  onChange={(e) => setOrgNameInput(e.target.value)}
                  disabled={loading}
                  onKeyDown={(e) => e.key === 'Enter' && void handleRegisterOrganization()}
                />
              </div>
              <button
                className="btn btn-primary"
                onClick={() => void handleRegisterOrganization()}
                disabled={loading || !orgNameInput}
                style={{ alignSelf: 'flex-end' }}
                type="button"
              >
                {loading ? (
                  <>
                    <span className="spinner spinner-sm" />
                    Registering...
                  </>
                ) : (
                  'Register'
                )}
              </button>
            </div>
          </div>
        </div>
      ) : (
        <>
          <div className="card" style={{ marginBottom: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
              <MdBusiness size={20} style={{ color: 'var(--accent)' }} />
              <div>
                <div style={{ fontSize: 13, color: 'var(--muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>Organization</div>
                <strong style={{ fontSize: 16 }}>{organizationName}</strong>
              </div>
            </div>

            <div style={{ display: 'flex', gap: 10, alignItems: 'flex-end', flexWrap: 'wrap' }}>
              <div className="form-group" style={{ flex: 1, minWidth: 200 }}>
                <label className="form-label" htmlFor="project-name-input">New Project</label>
                <input
                  className="input"
                  id="project-name-input"
                  placeholder="My architecture project"
                  value={projectNameInput}
                  onChange={(e) => setProjectNameInput(e.target.value)}
                  disabled={loading}
                  onKeyDown={(e) => e.key === 'Enter' && void handleCreateProject()}
                />
              </div>
              <button
                className="btn btn-primary"
                onClick={() => void handleCreateProject()}
                disabled={loading || !projectNameInput}
                style={{ alignSelf: 'flex-end' }}
                type="button"
              >
                {loading ? (
                  <>
                    <span className="spinner spinner-sm" />
                    Creating...
                  </>
                ) : (
                  <>
                    <MdAdd size={16} />
                    Create Project
                  </>
                )}
              </button>
            </div>
          </div>

          {projects.length === 0 && !loading ? (
            <div className="card">
              <div className="empty-state">
                <MdFolder className="empty-state-icon" />
                <p className="empty-state-title">No projects yet</p>
                <p className="empty-state-description">
                  Create your first project to start mapping your architecture and connecting Azure resources.
                </p>
              </div>
            </div>
          ) : (
            <div className="card">
              <h3 style={{ margin: '0 0 12px 0', fontSize: 14, color: 'var(--muted)', textTransform: 'uppercase', letterSpacing: '0.5px', fontWeight: 600 }}>
                Projects ({projects.length})
              </h3>
              <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'flex', flexDirection: 'column', gap: 8 }}>
                {projects.map((project) => (
                  <li
                    key={project.id}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: 10,
                      padding: '10px 14px',
                      background: 'var(--panel-2)',
                      border: '1px solid var(--border)',
                      borderRadius: 8,
                    }}
                  >
                    <MdFolder size={16} style={{ color: 'var(--accent)', flexShrink: 0 }} />
                    <span style={{ fontWeight: 500 }}>{project.name}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </>
      )}
    </section>
  );
}
