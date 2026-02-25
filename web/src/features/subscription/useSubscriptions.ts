import { useCallback, useState } from 'react';
import { postJson, ApiError } from '../../shared/api/client';

type ConnectSubscriptionRequest = {
  externalSubscriptionId: string;
  displayName: string;
};

type ConnectSubscriptionResponse = {
  subscriptionId: string;
  externalSubscriptionId: string;
  displayName: string;
};

type SubscriptionState = {
  connectedSubscription: ConnectSubscriptionResponse | undefined;
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
    loading: false,
    error: undefined,
  });

  const connectSubscription = useCallback(async (externalSubscriptionId: string, displayName: string) => {
    setState((prev) => ({ ...prev, loading: true, error: undefined }));
    try {
      const response = await postJson<ConnectSubscriptionRequest, ConnectSubscriptionResponse>(
        '/api/discovery/subscriptions',
        { externalSubscriptionId, displayName },
      );
      setState({
        connectedSubscription: response,
        loading: false,
        error: undefined,
      });
      return response;
    } catch (err: unknown) {
      const message = extractErrorMessage(err);
      setState((prev) => ({ ...prev, loading: false, error: message }));
      return undefined;
    }
  }, []);

  return {
    connectedSubscription: state.connectedSubscription,
    loading: state.loading,
    error: state.error,
    connectSubscription,
  } as const;
}
