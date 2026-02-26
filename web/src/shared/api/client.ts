const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';
const TOKEN_STORAGE_KEY = 'c4_token';

export class ApiError extends Error {
  public readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

function getAuthHeaders(): Record<string, string> {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (token === null) return {};
  return { Authorization: `Bearer ${token}` };
}

function handleUnauthorized(status: number): void {
  if (status === 401) {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    window.location.href = '/login';
  }
}

async function handleResponse<TResponse>(response: Response): Promise<TResponse> {
  if (!response.ok) {
    handleUnauthorized(response.status);
    const body = await response.text().catch(() => 'Unknown error');
    throw new ApiError(response.status, `Request failed with status ${response.status}: ${body}`);
  }

  return (await response.json()) as TResponse;
}

export async function getJson<TResponse>(path: string): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { ...getAuthHeaders() },
  });
  return handleResponse<TResponse>(response);
}

export async function postJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
    body: JSON.stringify(body),
  });
  return handleResponse<TResponse>(response);
}

export async function putJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
    body: JSON.stringify(body),
  });
  return handleResponse<TResponse>(response);
}

export async function deleteJson<TResponse = void>(path: string): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'DELETE',
    headers: { ...getAuthHeaders() },
  });

  if (!response.ok) {
    handleUnauthorized(response.status);
    const body = await response.text().catch(() => 'Unknown error');
    throw new ApiError(response.status, `Request failed with status ${response.status}: ${body}`);
  }

  const text = await response.text();
  if (text.length === 0) {
    return undefined as TResponse;
  }

  return JSON.parse(text) as TResponse;
}

export async function fetchBlob(path: string): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { ...getAuthHeaders() },
  });
  if (!response.ok) {
    handleUnauthorized(response.status);
    const body = await response.text().catch(() => 'Unknown error');
    throw new ApiError(response.status, `Request failed with status ${response.status}: ${body}`);
  }
  return response.blob();
}
