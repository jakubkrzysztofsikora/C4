import { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { getJsonOrNull, postJson } from '../../shared/api/client';
import { useSubscriptions } from './useSubscriptions';

type AzureSubscription = {
  subscriptionId: string;
  displayName: string;
  state: string;
};

type OrgResponse = {
  organizationId: string;
  name: string;
  projects: ReadonlyArray<{ projectId: string; name: string }>;
};

type DiscoverRequest = {
  externalSubscriptionId: string;
  projectId: string;
  organizationId: string | null;
  sources: null;
};

type DiscoverResponse = {
  subscriptionId: string;
  resourcesCount: number;
  status: string;
  escalationLevel: string;
  userActionHint: string;
  dataQualityFailures: number;
};

type CallbackStatus = 'exchanging' | 'selecting' | 'connecting' | 'discovering' | 'discover-done' | 'discover-error' | 'error';

export function AzureCallbackPage() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { exchangeAzureCode, connectSubscription, error } = useSubscriptions();
  const [status, setStatus] = useState<CallbackStatus>('exchanging');
  const [subscriptions, setSubscriptions] = useState<ReadonlyArray<AzureSubscription>>([]);
  const [discoverResult, setDiscoverResult] = useState<DiscoverResponse | undefined>(undefined);
  const [discoverError, setDiscoverError] = useState<string | undefined>(undefined);

  useEffect(() => {
    const code = searchParams.get('code');
    const state = searchParams.get('state');
    const savedState = sessionStorage.getItem('azure_auth_state');

    if (!code || !state || state !== savedState) {
      setStatus('error');
      return;
    }

    sessionStorage.removeItem('azure_auth_state');
    const redirectUri = `${window.location.origin}/azure/callback`;

    async function exchange() {
      const result = await exchangeAzureCode(code!, redirectUri);
      if (result !== undefined && result.length > 0) {
        setSubscriptions(result);
        setStatus('selecting');
      } else {
        setStatus('error');
      }
    }

    void exchange();
  }, [searchParams, exchangeAzureCode]);

  async function handleSelect(externalSubscriptionId: string, displayName: string) {
    setStatus('connecting');
    const result = await connectSubscription(externalSubscriptionId, displayName);
    if (result === undefined) {
      setStatus('error');
      return;
    }

    setStatus('discovering');
    const org = await getJsonOrNull<OrgResponse>('/api/organizations/current');
    const projectId = org?.projects[0]?.projectId;

    if (projectId !== undefined) {
      try {
        const discoverResp = await postJson<DiscoverRequest, DiscoverResponse>(
          `/api/discovery/subscriptions/${result.subscriptionId}/discover`,
          { externalSubscriptionId: result.externalSubscriptionId, projectId, organizationId: null, sources: null },
        );
        setDiscoverResult(discoverResp);
        setStatus('discover-done');
      } catch (err: unknown) {
        setDiscoverError(err instanceof Error ? err.message : 'Discovery failed');
        setStatus('discover-error');
      }
    } else {
      navigate('/', { replace: true });
    }
  }

  if (status === 'exchanging') {
    return (
      <div className="auth-page">
        <div className="auth-card" style={{ textAlign: 'center' }}>
          <span className="spinner" style={{ margin: '0 auto 16px' }} />
          <p>Authenticating with Azure...</p>
        </div>
      </div>
    );
  }

  if (status === 'connecting') {
    return (
      <div className="auth-page">
        <div className="auth-card" style={{ textAlign: 'center' }}>
          <span className="spinner" style={{ margin: '0 auto 16px' }} />
          <p>Connecting subscription...</p>
        </div>
      </div>
    );
  }

  if (status === 'discovering') {
    return (
      <div className="auth-page">
        <div className="auth-card" style={{ textAlign: 'center' }}>
          <span className="spinner" style={{ margin: '0 auto 16px' }} />
          <p style={{ fontWeight: 600 }}>Scanning Azure subscription...</p>
          <p style={{ fontSize: 13, color: 'var(--muted)' }}>
            Querying Azure Resource Graph and classifying resources. This may take a moment.
          </p>
        </div>
      </div>
    );
  }

  if (status === 'discover-done' && discoverResult !== undefined) {
    const hasWarnings = discoverResult.dataQualityFailures > 0;
    return (
      <div className="auth-page">
        <div className="auth-card">
          <div style={{ textAlign: 'center', marginBottom: 16 }}>
            <div style={{
              width: 48,
              height: 48,
              borderRadius: '50%',
              background: hasWarnings ? 'rgba(230,167,0,0.1)' : 'rgba(46,143,94,0.1)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 12px',
            }}>
              {hasWarnings ? '!' : '\u2713'}
            </div>
            <h2 style={{ margin: '0 0 4px 0' }}>Discovery Complete</h2>
            <p style={{ fontSize: 14, color: 'var(--muted)', margin: 0 }}>
              <strong>{discoverResult.resourcesCount}</strong> resource{discoverResult.resourcesCount !== 1 ? 's' : ''} discovered
            </p>
          </div>
          {hasWarnings && (
            <div style={{
              padding: '10px 14px',
              background: 'rgba(230,167,0,0.06)',
              border: '1px solid rgba(230,167,0,0.3)',
              borderRadius: 8,
              fontSize: 13,
              marginBottom: 16,
            }}>
              {discoverResult.dataQualityFailures} resource{discoverResult.dataQualityFailures !== 1 ? 's' : ''} could not be classified.
              {discoverResult.userActionHint.length > 0 && ` ${discoverResult.userActionHint}`}
            </div>
          )}
          <button
            className="btn btn-primary"
            style={{ width: '100%' }}
            onClick={() => navigate('/', { replace: true })}
            type="button"
          >
            Go to Dashboard
          </button>
        </div>
      </div>
    );
  }

  if (status === 'discover-error') {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <h2 style={{ marginTop: 0 }}>Discovery Failed</h2>
          <p style={{ color: 'var(--error)', fontSize: 14 }}>
            {discoverError ?? 'Could not discover resources from your Azure subscription.'}
          </p>
          <p style={{ fontSize: 13, color: 'var(--muted)' }}>
            Your subscription was connected successfully. You can try rediscovering from the Dashboard.
          </p>
          <button
            className="btn btn-primary"
            onClick={() => navigate('/', { replace: true })}
            type="button"
          >
            Go to Dashboard
          </button>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <h2 style={{ marginTop: 0 }}>Authentication Failed</h2>
          <p style={{ color: 'var(--error)' }}>{error ?? 'Azure authentication failed. Please try again.'}</p>
          <button
            className="btn btn-primary"
            onClick={() => navigate('/subscriptions', { replace: true })}
            type="button"
          >
            Back to Subscriptions
          </button>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 600, margin: '0 auto', padding: '40px 20px' }}>
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Select Azure Subscription</h1>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 24 }}>
        Choose a subscription to connect for architecture discovery.
      </p>
      <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
        {subscriptions.map(sub => (
          <button
            key={sub.subscriptionId}
            className="card"
            type="button"
            onClick={() => void handleSelect(sub.subscriptionId, sub.displayName)}
            style={{
              cursor: 'pointer',
              textAlign: 'left',
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
            }}
          >
            <div>
              <strong>{sub.displayName}</strong>
              <div style={{ fontSize: 13, color: 'var(--muted)', fontFamily: 'monospace', marginTop: 4 }}>
                {sub.subscriptionId}
              </div>
            </div>
            <span className={`badge ${sub.state === 'Enabled' ? 'green' : 'yellow'}`}>
              {sub.state}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
