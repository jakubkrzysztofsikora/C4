import { useCallback, useEffect, useState } from 'react';
import { getJson, ApiError } from '../../shared/api/client';

type GraphNodeDto = {
  id: string;
  name: string;
  externalResourceId: string;
  level: string;
};

type GraphEdgeDto = {
  id: string;
  sourceNodeId: string;
  targetNodeId: string;
};

type GraphDto = {
  projectId: string;
  nodes: ReadonlyArray<GraphNodeDto>;
  edges: ReadonlyArray<GraphEdgeDto>;
};

type DashboardState = {
  graph: GraphDto | undefined;
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

export function useDashboard(projectId?: string) {
  const [state, setState] = useState<DashboardState>({
    graph: undefined,
    loading: false,
    error: undefined,
  });

  const fetchDashboardData = useCallback(async (id: string) => {
    setState((prev) => ({ ...prev, loading: true, error: undefined }));
    try {
      const graph = await getJson<GraphDto>(`/api/projects/${id}/graph`);
      setState({ graph, loading: false, error: undefined });
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState({ graph: undefined, loading: false, error: message });
    }
  }, []);

  useEffect(() => {
    if (projectId !== undefined) {
      void fetchDashboardData(projectId);
    }
  }, [projectId, fetchDashboardData]);

  return {
    graph: state.graph,
    loading: state.loading,
    error: state.error,
    refetch: fetchDashboardData,
  } as const;
}
