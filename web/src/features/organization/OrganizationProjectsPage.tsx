import { useState } from 'react';

type Project = { id: string; name: string };

export function OrganizationProjectsPage() {
  const [organization, setOrganization] = useState('Acme');
  const [projects, setProjects] = useState<Project[]>([{ id: crypto.randomUUID(), name: 'Platform' }]);
  const [name, setName] = useState('');

  return (
    <section>
      <h2>Organization & Projects</h2>
      <label>
        Organization
        <input value={organization} onChange={e => setOrganization(e.target.value)} />
      </label>
      <div>
        <input placeholder="New project" value={name} onChange={e => setName(e.target.value)} />
        <button onClick={() => name && (setProjects([...projects, { id: crypto.randomUUID(), name }]), setName(''))}>Create Project</button>
      </div>
      <ul>
        {projects.map(project => (
          <li key={project.id}>{project.name}</li>
        ))}
      </ul>
    </section>
  );
}
