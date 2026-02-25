import { useState } from 'react';
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
    <section>
      <h2>Organization & Projects</h2>

      {error !== undefined && <p style={{ color: '#e74c3c' }}>{error}</p>}

      {organizationId === undefined ? (
        <div>
          <label>
            Organization Name
            <input
              value={orgNameInput}
              onChange={(e) => setOrgNameInput(e.target.value)}
              disabled={loading}
            />
          </label>
          <button onClick={handleRegisterOrganization} disabled={loading || !orgNameInput}>
            {loading ? 'Registering...' : 'Register Organization'}
          </button>
        </div>
      ) : (
        <>
          <p>Organization: <strong>{organizationName}</strong></p>
          <div>
            <input
              placeholder="New project"
              value={projectNameInput}
              onChange={(e) => setProjectNameInput(e.target.value)}
              disabled={loading}
            />
            <button onClick={handleCreateProject} disabled={loading || !projectNameInput}>
              {loading ? 'Creating...' : 'Create Project'}
            </button>
          </div>
          <ul>
            {projects.map((project) => (
              <li key={project.id}>{project.name}</li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}
