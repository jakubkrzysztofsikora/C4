import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { postJson } from '../api/client';

const TOKEN_STORAGE_KEY = 'c4_token';

type AuthUser = {
  userId: string;
  email: string;
};

type LoginResponse = {
  token: string;
};

type RegisterResponse = {
  token: string;
};

type AuthState = {
  token: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string) => Promise<void>;
  logout: () => void;
};

function parseJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;
  const base64 = parts[1]!.replace(/-/g, '+').replace(/_/g, '/');
  try {
    const json = atob(base64);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function extractUser(token: string): AuthUser | null {
  const payload = parseJwtPayload(token);
  if (payload === null) return null;
  const userId = typeof payload['sub'] === 'string' ? payload['sub'] : null;
  const email = typeof payload['email'] === 'string' ? payload['email'] : null;
  if (userId === null || email === null) return null;
  return { userId, email };
}

function isTokenExpired(token: string): boolean {
  const payload = parseJwtPayload(token);
  if (payload === null) return true;
  const exp = typeof payload['exp'] === 'number' ? payload['exp'] : null;
  if (exp === null) return false;
  return Date.now() / 1000 > exp;
}

function loadStoredToken(): string | null {
  const stored = localStorage.getItem(TOKEN_STORAGE_KEY);
  if (stored === null) return null;
  if (isTokenExpired(stored)) {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    return null;
  }
  return stored;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(loadStoredToken);

  const user = useMemo(() => (token !== null ? extractUser(token) : null), [token]);

  const applyToken = useCallback((newToken: string) => {
    localStorage.setItem(TOKEN_STORAGE_KEY, newToken);
    setToken(newToken);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await postJson<{ email: string; password: string }, LoginResponse>(
      '/api/auth/login',
      { email, password }
    );
    applyToken(response.token);
  }, [applyToken]);

  const register = useCallback(async (email: string, password: string) => {
    const response = await postJson<{ email: string; password: string }, RegisterResponse>(
      '/api/auth/register',
      { email, password }
    );
    applyToken(response.token);
  }, [applyToken]);

  const logout = useCallback(() => {
    localStorage.removeItem(TOKEN_STORAGE_KEY);
    setToken(null);
  }, []);

  const value = useMemo<AuthState>(
    () => ({
      token,
      user,
      isAuthenticated: token !== null,
      login,
      register,
      logout,
    }),
    [token, user, login, register, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
