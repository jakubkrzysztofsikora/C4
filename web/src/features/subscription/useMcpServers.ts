import { useCallback, useEffect, useState } from 'react';
import { getJson, postJson, deleteJson } from '../../shared/api/client';

type McpServerItem = {
  id: string;
  name: string;
  endpoint: string;
  authMode: string;
};

type McpServerListResponse = {
  servers: ReadonlyArray<McpServerItem>;
};

type AddMcpServerRequest = {
  name: string;
  endpoint: string;
  authMode: string;
};

type McpServersState = {
  servers: ReadonlyArray<McpServerItem>;
  loading: boolean;
  error: string | undefined;
};

export function useMcpServers(projectId: string | undefined) {
  const [state, setState] = useState<McpServersState>({
    servers: [],
    loading: false,
    error: undefined,
  });

  useEffect(() => {
    if (projectId === undefined) return;
    let cancelled = false;

    async function load() {
      setState(prev => ({ ...prev, loading: true }));
      try {
        const data = await getJson<McpServerListResponse>(`/api/projects/${projectId}/mcp-servers`);
        if (!cancelled) {
          setState({ servers: data.servers, loading: false, error: undefined });
        }
      } catch {
        if (!cancelled) {
          setState(prev => ({ ...prev, loading: false }));
        }
      }
    }

    void load();
    return () => { cancelled = true; };
  }, [projectId]);

  const addServer = useCallback(async (name: string, endpoint: string) => {
    if (projectId === undefined) return;
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      const server = await postJson<AddMcpServerRequest, McpServerItem>(
        `/api/projects/${projectId}/mcp-servers`,
        { name, endpoint, authMode: 'None' },
      );
      setState(prev => ({
        ...prev,
        servers: [...prev.servers, server],
        loading: false,
      }));
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to add MCP server';
      setState(prev => ({ ...prev, loading: false, error: message }));
    }
  }, [projectId]);

  const removeServer = useCallback(async (id: string) => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      await deleteJson(`/api/mcp-servers/${id}`);
      setState(prev => ({
        ...prev,
        servers: prev.servers.filter(s => s.id !== id),
        loading: false,
      }));
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'Failed to remove MCP server';
      setState(prev => ({ ...prev, loading: false, error: message }));
    }
  }, []);

  return {
    servers: state.servers,
    loading: state.loading,
    error: state.error,
    addServer,
    removeServer,
  } as const;
}
