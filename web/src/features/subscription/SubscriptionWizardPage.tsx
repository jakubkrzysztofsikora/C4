import { MdCloud, MdCheckCircle } from 'react-icons/md';
import { useSubscriptions } from './useSubscriptions';

export function SubscriptionWizardPage() {
  const { connectedSubscription, loading, error, startAzureAuth } = useSubscriptions();

  if (loading) {
    return (
      <section className="fade-in">
        <h1 style={{ marginTop: 0, marginBottom: 4 }}>Azure Subscription</h1>
        <div className="card">
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div className="skeleton" style={{ height: 20, width: '40%' }} />
            <div className="skeleton" style={{ height: 14, width: '60%' }} />
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="fade-in">
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Azure Subscription</h1>
      <p className="subtle" style={{ marginTop: 0, marginBottom: 20 }}>
        Connect your Azure subscription to enable architecture discovery.
      </p>

      {error !== undefined && (
        <div className="card" style={{ borderColor: 'var(--error)', marginBottom: 16 }}>
          <p style={{ color: 'var(--error)', margin: 0 }}>{error}</p>
        </div>
      )}

      {connectedSubscription !== undefined ? (
        <div className="card fade-in">
          <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 12 }}>
            <MdCheckCircle size={24} style={{ color: 'var(--success)', flexShrink: 0 }} />
            <div>
              <div style={{ fontSize: 13, color: 'var(--muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.5px' }}>
                Connected Subscription
              </div>
              <strong style={{ fontSize: 16 }}>{connectedSubscription.displayName}</strong>
            </div>
          </div>
          <div
            style={{
              padding: '10px 14px',
              background: 'var(--panel-2)',
              border: '1px solid var(--border)',
              borderRadius: 8,
              display: 'flex',
              alignItems: 'center',
              gap: 8,
            }}
          >
            <MdCloud size={16} style={{ color: 'var(--muted)', flexShrink: 0 }} />
            <span style={{ fontSize: 13, color: 'var(--muted)', fontFamily: 'monospace' }}>
              {connectedSubscription.externalSubscriptionId}
            </span>
          </div>
        </div>
      ) : (
        <div className="card">
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16, padding: '24px 0' }}>
            <MdCloud size={48} style={{ color: '#0078D4' }} />
            <div style={{ textAlign: 'center' }}>
              <div style={{ fontWeight: 600, fontSize: 18, marginBottom: 4 }}>Connect with Azure</div>
              <div style={{ fontSize: 14, color: 'var(--muted)', maxWidth: 360 }}>
                Sign in with your Microsoft account to discover and connect your Azure subscriptions automatically.
              </div>
            </div>
            <button
              className="btn btn-primary"
              type="button"
              onClick={() => void startAzureAuth()}
              style={{ gap: 8, padding: '10px 24px', fontSize: 15 }}
            >
              <MdCloud size={16} />
              Sign in with Microsoft
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
