const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';
const TOKEN_STORAGE_KEY = 'c4_token';

type ApiErrorMetadata = {
  code: string | undefined;
  title: string | undefined;
  userActionHint: string | undefined;
  rawBody: string | undefined;
  payload: unknown;
};

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string | undefined;
  public readonly title: string | undefined;
  public readonly userActionHint: string | undefined;
  public readonly rawBody: string | undefined;
  public readonly payload: unknown;

  constructor(status: number, message: string, metadata?: Partial<ApiErrorMetadata>) {
    super(message);
    this.status = status;
    this.code = metadata?.code;
    this.title = metadata?.title;
    this.userActionHint = metadata?.userActionHint;
    this.rawBody = metadata?.rawBody;
    this.payload = metadata?.payload;
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

function readString(obj: Record<string, unknown>, key: string): string | undefined {
  const value = obj[key];
  if (typeof value !== 'string') return undefined;
  const normalized = value.trim();
  return normalized.length > 0 ? normalized : undefined;
}

function truncate(value: string, maxLength: number): string {
  if (value.length <= maxLength) return value;
  return `${value.slice(0, Math.max(0, maxLength - 1))}\u2026`;
}

function stripNestedJson(value: string): string {
  const jsonStart = value.indexOf('{');
  if (jsonStart <= 0) return value;

  const candidate = value.slice(0, jsonStart).trim();
  if (candidate.length === 0) return value;
  return candidate.replace(/[.:;\s]+$/u, '').trim();
}

function normalizeMessage(value: string): string {
  const compact = value.replace(/\s+/gu, ' ').trim();
  if (compact.length === 0) return 'Request failed.';
  return truncate(stripNestedJson(compact), 280);
}

function mapKnownApiError(code: string | undefined): string | undefined {
  if (code === undefined) return undefined;

  return {
    'discovery.auth.permission': 'Azure authorization failed. Reconnect your Azure subscription and retry discovery.',
    'discovery.connector.unavailable': 'Discovery connector is unavailable. Retry shortly, then reconnect subscription if this persists.',
    'discovery.invalid_sources': 'Discovery request contains invalid source values.',
    'graph.not_found': 'No graph exists for the selected project yet.',
  }[code];
}

function parseApiError(status: number, rawBody: string): {
  message: string;
  code: string | undefined;
  title: string | undefined;
  userActionHint: string | undefined;
  rawBody: string;
  payload: unknown;
} {
  const fallback = `Request failed with status ${status}.`;
  const trimmedBody = rawBody.trim();
  if (trimmedBody.length === 0) {
    return { message: fallback, code: undefined, title: undefined, userActionHint: undefined, rawBody: rawBody, payload: undefined };
  }

  try {
    const payload: unknown = JSON.parse(trimmedBody);
    if (typeof payload !== 'object' || payload === null || Array.isArray(payload)) {
      return {
        message: `${fallback} ${normalizeMessage(trimmedBody)}`,
        code: undefined,
        title: undefined,
        userActionHint: undefined,
        rawBody: rawBody,
        payload,
      };
    }

    const record = payload as Record<string, unknown>;
    const code = readString(record, 'errorCode') ?? readString(record, 'code');
    const title = readString(record, 'title');
    const userActionHint = readString(record, 'userActionHint');
    const backendMessage = readString(record, 'errorMessage') ?? readString(record, 'message') ?? title;

    const known = mapKnownApiError(code);
    if (known !== undefined) {
      const withHint = userActionHint !== undefined && userActionHint.length > 0 && userActionHint !== backendMessage
        ? `${known} ${normalizeMessage(userActionHint)}`
        : known;
      return { message: withHint, code, title, userActionHint, rawBody: rawBody, payload };
    }

    const normalizedBackendMessage = backendMessage !== undefined
      ? normalizeMessage(backendMessage)
      : `${fallback} ${normalizeMessage(trimmedBody)}`;
    return {
      message: normalizedBackendMessage,
      code,
      title,
      userActionHint,
      rawBody: rawBody,
      payload,
    };
  } catch {
    return {
      message: `${fallback} ${normalizeMessage(trimmedBody)}`,
      code: undefined,
      title: undefined,
      userActionHint: undefined,
      rawBody: rawBody,
      payload: undefined,
    };
  }
}

async function throwApiError(response: Response): Promise<never> {
  handleUnauthorized(response.status);
  const body = await response.text().catch(() => '');
  const parsed = parseApiError(response.status, body);
  throw new ApiError(response.status, parsed.message, parsed);
}

async function handleResponse<TResponse>(response: Response): Promise<TResponse> {
  if (!response.ok) {
    await throwApiError(response);
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
    await throwApiError(response);
  }

  const text = await response.text();
  if (text.length === 0) {
    return undefined as TResponse;
  }

  return JSON.parse(text) as TResponse;
}

export async function getJsonOrNull<TResponse>(path: string): Promise<TResponse | null> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { ...getAuthHeaders() },
  });
  if (response.status === 204) return null;
  if (!response.ok) {
    handleUnauthorized(response.status);
    return null;
  }
  return (await response.json()) as TResponse;
}

export async function fetchBlob(path: string): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: { ...getAuthHeaders() },
  });
  if (!response.ok) {
    await throwApiError(response);
  }
  return response.blob();
}

export async function postBlob<TRequest>(path: string, body: TRequest): Promise<Blob> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeaders() },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    await throwApiError(response);
  }

  return response.blob();
}
