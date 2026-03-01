import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { getJsonOrNull } from '../api/client';
import { useAuth } from '../auth/AuthContext';

const STORAGE_KEY = 'c4_active_project';

type Project = {
  id: string;
  name: string;
};

type GetOrganizationResponse = {
  organizationId: string;
  name: string;
  projects: ReadonlyArray<{ projectId: string; name: string }>;
};

type ProjectState = {
  activeProject: Project | undefined;
  projects: ReadonlyArray<Project>;
  setActiveProject: (projectId: string) => void;
  loading: boolean;
};

const ProjectContext = createContext<ProjectState | undefined>(undefined);

function loadSavedProjectId(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
}

function saveProdjectId(projectId: string): void {
  try {
    localStorage.setItem(STORAGE_KEY, projectId);
  } catch {
    // localStorage unavailable
  }
}

export function ProjectProvider({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useAuth();
  const [projects, setProjects] = useState<ReadonlyArray<Project>>([]);
  const [activeId, setActiveId] = useState<string | null>(loadSavedProjectId);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!isAuthenticated) {
      setProjects([]);
      setActiveId(null);
      setLoading(false);
      return;
    }

    let cancelled = false;
    async function load() {
      const data = await getJsonOrNull<GetOrganizationResponse>('/api/organizations/current');
      if (cancelled) return;
      if (data === null) {
        setLoading(false);
        return;
      }
      const mapped = data.projects.map((p) => ({ id: p.projectId, name: p.name }));
      setProjects(mapped);

      const savedId = loadSavedProjectId();
      const savedExists = mapped.some((p) => p.id === savedId);
      if (!savedExists && mapped.length > 0) {
        setActiveId(mapped[0]!.id);
        saveProdjectId(mapped[0]!.id);
      }
      setLoading(false);
    }
    void load();
    return () => { cancelled = true; };
  }, [isAuthenticated]);

  const setActiveProject = useCallback((projectId: string) => {
    setActiveId(projectId);
    saveProdjectId(projectId);
  }, []);

  const activeProject = useMemo(
    () => projects.find((p) => p.id === activeId),
    [projects, activeId],
  );

  const value = useMemo<ProjectState>(
    () => ({ activeProject, projects, setActiveProject, loading }),
    [activeProject, projects, setActiveProject, loading],
  );

  return <ProjectContext.Provider value={value}>{children}</ProjectContext.Provider>;
}

export function useProject(): ProjectState {
  const context = useContext(ProjectContext);
  if (context === undefined) {
    throw new Error('useProject must be used within ProjectProvider');
  }
  return context;
}
