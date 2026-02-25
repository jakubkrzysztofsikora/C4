const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

export class ApiError extends Error {
  public readonly status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

async function handleResponse<TResponse>(response: Response): Promise<TResponse> {
  if (!response.ok) {
    const body = await response.text().catch(() => 'Unknown error');
    throw new ApiError(response.status, `Request failed with status ${response.status}: ${body}`);
  }

  return (await response.json()) as TResponse;
}

export async function getJson<TResponse>(path: string): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`);
  return handleResponse<TResponse>(response);
}

export async function postJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return handleResponse<TResponse>(response);
}

export async function putJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return handleResponse<TResponse>(response);
}

export async function deleteJson<TResponse = void>(path: string): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'DELETE',
  });

  if (!response.ok) {
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
  const response = await fetch(`${API_BASE_URL}${path}`);
  if (!response.ok) {
    const body = await response.text().catch(() => 'Unknown error');
    throw new ApiError(response.status, `Request failed with status ${response.status}: ${body}`);
  }
  return response.blob();
}
