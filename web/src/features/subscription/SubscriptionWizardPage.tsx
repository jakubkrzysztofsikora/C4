import { useState } from 'react';
import { MdCloud, MdCheckCircle, MdLink } from 'react-icons/md';
import { useSubscriptions } from './useSubscriptions';

export function SubscriptionWizardPage() {
  const { connectedSubscription, loading, error, connectSubscription } = useSubscriptions();

  const [subscriptionId, setSubscriptionId] = useState('');
  const [displayName, setDisplayName] = useState('');

  async function handleConnect() {
    if (!subscriptionId || !displayName) return;
    await connectSubscription(subscriptionId, displayName);
  }

  return (
    <section className="fade-in">
      <h1 style={{ marginTop: 0, marginBottom: 4 }}>Azure Subscription Wizard</h1>
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
              {connectedSubscription.subscriptionId}
            </span>
          </div>
        </div>
      ) : (
        <div className="card">
          <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 20 }}>
            <MdCloud size={20} style={{ color: 'var(--accent)' }} />
            <div>
              <div style={{ fontWeight: 600 }}>Connect Azure Subscription</div>
              <div style={{ fontSize: 13, color: 'var(--muted)' }}>Pending connection</div>
            </div>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
            <div className="form-group">
              <label className="form-label" htmlFor="subscription-id-input">Subscription ID</label>
              <input
                className="input"
                id="subscription-id-input"
                placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                value={subscriptionId}
                onChange={(e) => setSubscriptionId(e.target.value)}
                disabled={loading}
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="display-name-input">Display Name</label>
              <input
                className="input"
                id="display-name-input"
                placeholder="Production"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                disabled={loading}
              />
            </div>

            <button
              className="btn btn-primary"
              style={{ alignSelf: 'flex-start' }}
              type="button"
              onClick={() => void handleConnect()}
              disabled={loading || !subscriptionId || !displayName}
            >
              {loading ? (
                <>
                  <span className="spinner spinner-sm" />
                  Connecting...
                </>
              ) : (
                <>
                  <MdLink size={16} />
                  Connect
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
