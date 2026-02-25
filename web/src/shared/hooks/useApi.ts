import { useCallback } from 'react';
import { getJson, postJson, putJson, deleteJson, fetchBlob } from '../api/client';

export function useApi() {
  const get = useCallback(<TResponse,>(path: string) => getJson<TResponse>(path), []);
  const post = useCallback(<TRequest, TResponse>(path: string, body: TRequest) => postJson<TRequest, TResponse>(path, body), []);
  const put = useCallback(<TRequest, TResponse>(path: string, body: TRequest) => putJson<TRequest, TResponse>(path, body), []);
  const del = useCallback(<TResponse = void,>(path: string) => deleteJson<TResponse>(path), []);
  const blob = useCallback((path: string) => fetchBlob(path), []);

  return { get, post, put, del, blob };
}
