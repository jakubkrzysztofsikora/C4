import { NavLink, Outlet } from 'react-router-dom';
import { CommandPalette } from './CommandPalette';
import { useTheme } from '../theme/ThemeProvider';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
  const { mode, toggleMode } = useTheme();
  const { user, logout } = useAuth();

  return (
    <main className="app-shell">
      <header className="app-header">
        <nav className="nav-links">
          <NavLink
            to="/"
            end
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            Dashboard
          </NavLink>
          <NavLink
            to="/organizations"
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            Organizations
          </NavLink>
          <NavLink
            to="/subscriptions"
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            Subscriptions
          </NavLink>
          <NavLink
            to="/diagram"
            className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
          >
            Diagram
          </NavLink>
        </nav>
        <div className="header-right">
          {user !== null && (
            <span className="header-user">{user.email}</span>
          )}
          <button className="btn btn-sm" onClick={toggleMode} type="button">
            {mode === 'dark' ? 'Light' : 'Dark'}
          </button>
          <button className="btn btn-sm btn-ghost btn-danger" onClick={logout} type="button">
            Sign out
          </button>
        </div>
      </header>
      <section style={{ marginTop: 12 }}>
        <CommandPalette />
      </section>
      <section style={{ marginTop: 16 }}>
        <Outlet />
      </section>
    </main>
  );
}
