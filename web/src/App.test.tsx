import { describe, expect, it } from 'vitest';
import { renderToString } from 'react-dom/server';
import { AuthPage } from './features/auth/AuthPage';

describe('AuthPage', () => {
  it('renders sign in form', () => {
    const rendered = renderToString(<AuthPage onLogin={() => undefined} />);
    expect(rendered).toContain('Sign in');
    expect(rendered).toContain('Login');
  });
});
