import { useCallback, useState } from 'react';
import { postJson, ApiError } from '../../shared/api/client';

type RegisterOrganizationRequest = { name: string };
type RegisterOrganizationResponse = { organizationId: string; name: string };

type CreateProjectRequest = { name: string };
type CreateProjectResponse = { projectId: string; organizationId: string; name: string };

type Project = { id: string; name: string };

type OrganizationState = {
  organizationId: string | undefined;
  organizationName: string;
  projects: ReadonlyArray<Project>;
  loading: boolean;
  error: string | undefined;
};

function isApiError(value: unknown): value is ApiError {
  return value instanceof ApiError;
}

function extractErrorMessage(err: unknown): string {
  if (isApiError(err)) {
    return err.message;
  }
  if (err instanceof Error) {
    return err.message;
  }
  return 'An unexpected error occurred';
}

export function useOrganizations() {
  const [state, setState] = useState<OrganizationState>({
    organizationId: undefined,
    organizationName: '',
    projects: [],
    loading: false,
    error: undefined,
  });

  const registerOrganization = useCallback(async (name: string) => {
    setState((prev) => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<RegisterOrganizationRequest, RegisterOrganizationResponse>(
        '/api/organizations',
        { name },
      );
      setState((prev) => ({
        ...prev,
        organizationId: response.organizationId,
        organizationName: response.name,
        loading: false,
      }));
      return response;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState((prev) => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  const createProject = useCallback(async (organizationId: string, name: string) => {
    setState((prev) => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<CreateProjectRequest, CreateProjectResponse>(
        `/api/organizations/${organizationId}/projects`,
        { name },
      );
      const newProject: Project = { id: response.projectId, name: response.name };
      setState((prev) => ({
        ...prev,
        projects: [...prev.projects, newProject],
        loading: false,
      }));
      return response;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState((prev) => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  return {
    organizationId: state.organizationId,
    organizationName: state.organizationName,
    projects: state.projects,
    loading: state.loading,
    error: state.error,
    registerOrganization,
    createProject,
  } as const;
}
