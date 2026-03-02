import { useCallback, useEffect, useState } from 'react';
import { postJson, getJson, getJsonOrNull, deleteJson, ApiError } from '../../shared/api/client';

type ConnectSubscriptionRequest = {
  externalSubscriptionId: string;
  displayName: string;
  gitRepoUrl?: string | undefined;
  gitPatToken?: string | undefined;
};

type ConnectSubscriptionResponse = {
  subscriptionId: string;
  externalSubscriptionId: string;
  displayName: string;
};

type GetSubscriptionResponse = {
  subscriptionId: string;
  externalSubscriptionId: string;
  displayName: string;
  gitRepoUrl?: string;
  gitBranch?: string;
  gitRootPath?: string;
  hasGitPatToken?: boolean;
};

type AzureAuthResponse = {
  authUrl: string;
  state: string;
};

type AzureSubscriptionDto = {
  subscriptionId: string;
  displayName: string;
  state: string;
};

type ExchangeCodeResponse = {
  subscriptions: ReadonlyArray<AzureSubscriptionDto>;
};

type IacConfig = {
  gitRepoUrl: string | undefined;
  gitBranch: string | undefined;
  gitRootPath: string | undefined;
  hasGitPatToken: boolean;
};

type ConfigureIacRequest = {
  gitRepoUrl?: string | undefined;
  gitPatToken?: string | undefined;
  gitBranch?: string | undefined;
  gitRootPath?: string | undefined;
};

type ConfigureIacResponse = {
  subscriptionId: string;
  gitRepoUrl?: string;
  gitBranch?: string;
  gitRootPath?: string;
  hasGitPatToken: boolean;
};

type SubscriptionState = {
  connectedSubscription: ConnectSubscriptionResponse | undefined;
  azureSubscriptions: ReadonlyArray<AzureSubscriptionDto>;
  iacConfig: IacConfig | undefined;
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

export function useSubscriptions() {
  const [state, setState] = useState<SubscriptionState>({
    connectedSubscription: undefined,
    azureSubscriptions: [],
    iacConfig: undefined,
    loading: true,
    error: undefined,
  });

  useEffect(() => {
    let cancelled = false;
    async function load() {
      const data = await getJsonOrNull<GetSubscriptionResponse>('/api/discovery/subscriptions/current');
      if (cancelled) return;
      if (data === null) {
        setState(prev => ({ ...prev, loading: false }));
        return;
      }
      setState(prev => ({
        ...prev,
        connectedSubscription: data,
        iacConfig: {
          gitRepoUrl: data.gitRepoUrl,
          gitBranch: data.gitBranch,
          gitRootPath: data.gitRootPath,
          hasGitPatToken: data.hasGitPatToken ?? false,
        },
        loading: false,
      }));
    }
    void load();
    return () => { cancelled = true; };
  }, []);

  const startAzureAuth = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      const callbackUrl = `${window.location.origin}/azure/callback`;
      const response = await getJson<AzureAuthResponse>(`/api/azure/auth?redirectUri=${encodeURIComponent(callbackUrl)}`);
      sessionStorage.setItem('azure_auth_state', response.state);
      window.location.href = response.authUrl;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState(prev => ({ ...prev, loading: false, error: message }));
    }
  }, []);

  const exchangeAzureCode = useCallback(async (code: string, redirectUri: string, state: string) => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<{ code: string; redirectUri: string; state: string }, ExchangeCodeResponse>(
        '/api/azure/auth/callback',
        { code, redirectUri, state },
      );
      setState(prev => ({
        ...prev,
        azureSubscriptions: response.subscriptions,
        loading: false,
      }));
      return response.subscriptions;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState(prev => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  const connectSubscription = useCallback(async (externalSubscriptionId: string, displayName: string, gitRepoUrl?: string, gitPatToken?: string) => {
    setState((prev) => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<ConnectSubscriptionRequest, ConnectSubscriptionResponse>(
        '/api/discovery/subscriptions',
        { externalSubscriptionId, displayName, gitRepoUrl, gitPatToken },
      );
      setState(prev => ({
        ...prev,
        connectedSubscription: response,
        iacConfig: {
          gitRepoUrl,
          gitBranch: undefined,
          gitRootPath: undefined,
          hasGitPatToken: (gitPatToken ?? '').trim().length > 0,
        },
        loading: false,
        error: undefined,
      }));
      return response;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState((prev) => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  const configureIacRepository = useCallback(async (
    subscriptionId: string,
    gitRepoUrl?: string,
    gitPatToken?: string,
    gitBranch?: string,
    gitRootPath?: string,
  ) => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<ConfigureIacRequest, ConfigureIacResponse>(
        `/api/discovery/subscriptions/${subscriptionId}/iac-config`,
        {
          gitRepoUrl,
          gitPatToken,
          gitBranch,
          gitRootPath,
        },
      );

      setState(prev => ({
        ...prev,
        iacConfig: {
          gitRepoUrl: response.gitRepoUrl,
          gitBranch: response.gitBranch,
          gitRootPath: response.gitRootPath,
          hasGitPatToken: response.hasGitPatToken,
        },
        loading: false,
      }));
      return response;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState(prev => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  const disconnectSubscription = useCallback(async () => {
    setState(prev => ({ ...prev, loading: true, error: undefined }));
    try {
      await deleteJson('/api/discovery/subscriptions/current');
      setState(prev => ({ ...prev, connectedSubscription: undefined, iacConfig: undefined, loading: false }));
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState(prev => ({ ...prev, loading: false, error: message }));
    }
  }, []);

  return {
    connectedSubscription: state.connectedSubscription,
    azureSubscriptions: state.azureSubscriptions,
    iacConfig: state.iacConfig,
    loading: state.loading,
    error: state.error,
    startAzureAuth,
    exchangeAzureCode,
    connectSubscription,
    configureIacRepository,
    disconnectSubscription,
  } as const;
}
