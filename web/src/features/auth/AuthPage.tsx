import { FormEvent, useState } from 'react';

export function AuthPage({ onLogin }: { onLogin: (token: string) => void }) {
  const [email, setEmail] = useState('');

  const submit = (e: FormEvent) => {
    e.preventDefault();
    onLogin(`token:${email || 'demo@c4.local'}`);
  };

  return (
    <form onSubmit={submit} style={{ display: 'grid', gap: 8, maxWidth: 320 }}>
      <h2>Sign in</h2>
      <input aria-label="email" placeholder="email" value={email} onChange={e => setEmail(e.target.value)} />
      <button type="submit">Login</button>
    </form>
  );
}
