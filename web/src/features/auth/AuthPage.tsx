import { type FormEvent, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../shared/auth/AuthContext';

type AuthTab = 'signin' | 'register';

export function AuthPage() {
  const { login, register } = useAuth();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<AuthTab>('signin');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | undefined>(undefined);

  function switchTab(tab: AuthTab) {
    setActiveTab(tab);
    setErrorMessage(undefined);
    setEmail('');
    setPassword('');
    setDisplayName('');
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setErrorMessage(undefined);
    setIsLoading(true);

    try {
      if (activeTab === 'signin') {
        await login(email, password);
      } else {
        if (displayName.trim().length === 0) {
          setErrorMessage('Display name is required.');
          return;
        }
        if (password.length < 8) {
          setErrorMessage('Password must be at least 8 characters.');
          return;
        }
        await register(email, password, displayName.trim());
      }
      navigate('/', { replace: true });
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : 'An unexpected error occurred.';
      setErrorMessage(message);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <div className="auth-logo">
          <div className="auth-logo-mark">C4</div>
          <span className="auth-logo-text">C4 Platform</span>
        </div>

        <div className="auth-tabs">
          <button
            className={`auth-tab ${activeTab === 'signin' ? 'active' : ''}`}
            onClick={() => switchTab('signin')}
            type="button"
          >
            Sign In
          </button>
          <button
            className={`auth-tab ${activeTab === 'register' ? 'active' : ''}`}
            onClick={() => switchTab('register')}
            type="button"
          >
            Create Account
          </button>
        </div>

        <form className="auth-form" onSubmit={(e) => void handleSubmit(e)}>
          <div className="form-group">
            <label className="form-label" htmlFor="auth-email">Email</label>
            <input
              className="input"
              id="auth-email"
              type="email"
              placeholder="you@example.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              disabled={isLoading}
              required
              autoComplete="email"
            />
          </div>

          {activeTab === 'register' && (
            <div className="form-group">
              <label className="form-label" htmlFor="auth-display-name">Display Name</label>
              <input
                className="input"
                id="auth-display-name"
                type="text"
                placeholder="How should we call you?"
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                disabled={isLoading}
                required
                autoComplete="name"
              />
            </div>
          )}

          <div className="form-group">
            <label className="form-label" htmlFor="auth-password">Password</label>
            <input
              className="input"
              id="auth-password"
              type="password"
              placeholder={activeTab === 'register' ? 'Min. 8 characters' : 'Enter your password'}
              value={password}
              onChange={e => setPassword(e.target.value)}
              disabled={isLoading}
              required
              autoComplete={activeTab === 'signin' ? 'current-password' : 'new-password'}
            />
          </div>

          {activeTab === 'register' && (
            <div className="auth-hint">
              <span className="auth-hint-title">Password requirements</span>
              <span>Minimum 8 characters</span>
              <span>Use a mix of letters, numbers, and symbols</span>
            </div>
          )}

          {errorMessage !== undefined && (
            <p className="form-error" role="alert">{errorMessage}</p>
          )}

          <button
            className="btn btn-primary auth-submit"
            type="submit"
            disabled={isLoading || email.length === 0 || password.length === 0 || (activeTab === 'register' && displayName.trim().length === 0)}
          >
            {isLoading ? (
              <>
                <span className="spinner spinner-sm" />
                {activeTab === 'signin' ? 'Signing in...' : 'Creating account...'}
              </>
            ) : (
              activeTab === 'signin' ? 'Sign In' : 'Create Account'
            )}
          </button>
        </form>
      </div>
    </div>
  );
}
