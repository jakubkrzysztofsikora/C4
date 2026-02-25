import { useState } from 'react';

export function SubscriptionWizardPage() {
  const [subscriptionId, setSubscriptionId] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [connected, setConnected] = useState(false);

  return (
    <section>
      <h2>Azure Subscription Wizard</h2>
      <label htmlFor="subscription-id-input">Subscription Id</label>
      <input id="subscription-id-input" placeholder="Subscription Id" value={subscriptionId} onChange={e => setSubscriptionId(e.target.value)} />
      <label htmlFor="display-name-input">Display Name</label>
      <input id="display-name-input" placeholder="Display Name" value={displayName} onChange={e => setDisplayName(e.target.value)} />
      <button type="button" onClick={() => setConnected(Boolean(subscriptionId && displayName))}>Connect</button>
      {connected ? <p>Connected: {displayName}</p> : <p>Pending connection</p>}
    </section>
  );
}
