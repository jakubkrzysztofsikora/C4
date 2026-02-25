import { useCallback } from 'react';
import { getJson } from '../api/client';

export function useApi() {
  const get = useCallback(<TResponse,>(path: string) => getJson<TResponse>(path), []);

  return { get };
}
