import { describe, expect, it } from 'vitest';
import { renderToString } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from './shared/auth/AuthContext';
import { AuthPage } from './features/auth/AuthPage';

function AuthPageHarness() {
  return (
    <MemoryRouter>
      <AuthProvider>
        <AuthPage />
      </AuthProvider>
    </MemoryRouter>
  );
}

describe('AuthPage', () => {
  it('renders sign in tab by default', () => {
    const rendered = renderToString(<AuthPageHarness />);
    expect(rendered).toContain('Sign In');
  });

  it('renders create account tab', () => {
    const rendered = renderToString(<AuthPageHarness />);
    expect(rendered).toContain('Create Account');
  });

  it('renders email and password fields', () => {
    const rendered = renderToString(<AuthPageHarness />);
    expect(rendered).toContain('auth-email');
    expect(rendered).toContain('auth-password');
  });

  it('renders the C4 brand mark', () => {
    const rendered = renderToString(<AuthPageHarness />);
    expect(rendered).toContain('C4 Platform');
  });

  it('renders display name field on register tab', () => {
    const rendered = renderToString(
      <MemoryRouter>
        <AuthProvider>
          <AuthPage />
        </AuthProvider>
      </MemoryRouter>
    );
    expect(rendered).not.toContain('auth-display-name');
  });
});
