import { useState } from 'react';

export function SubscriptionWizardPage() {
  const [subscriptionId, setSubscriptionId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [connected, setConnected] = useState(false);

  return (
    <section>
      <h2>Azure Subscription Wizard</h2>
      <input placeholder="Subscription Id" value={subscriptionId} onChange={e => setSubscriptionId(e.target.value)} />
      <input placeholder="Display Name" value={displayName} onChange={e => setDisplayName(e.target.value)} />
      <button onClick={() => setConnected(Boolean(subscriptionId && displayName))}>Connect</button>
      {connected ? <p>Connected: {displayName}</p> : <p>Pending connection</p>}
    </section>
  );
}
