import { useState } from 'react';
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
    <section>
      <h2>Azure Subscription Wizard</h2>

      {error !== undefined && <p style={{ color: '#e74c3c' }}>{error}</p>}

      <label htmlFor="subscription-id-input">Subscription Id</label>
      <input
        id="subscription-id-input"
        placeholder="Subscription Id"
        value={subscriptionId}
        onChange={(e) => setSubscriptionId(e.target.value)}
        disabled={loading}
      />

      <label htmlFor="display-name-input">Display Name</label>
      <input
        id="display-name-input"
        placeholder="Display Name"
        value={displayName}
        onChange={(e) => setDisplayName(e.target.value)}
        disabled={loading}
      />

      <button
        type="button"
        onClick={handleConnect}
        disabled={loading || !subscriptionId || !displayName}
      >
        {loading ? 'Connecting...' : 'Connect'}
      </button>

      {connectedSubscription !== undefined ? (
        <p>Connected: {connectedSubscription.displayName} ({connectedSubscription.subscriptionId})</p>
      ) : (
        <p>Pending connection</p>
      )}
    </section>
  );
}
