import { Link, Outlet } from 'react-router-dom';
import { CommandPalette } from './CommandPalette';
import { useTheme } from '../theme/ThemeProvider';

export function Layout() {
  const { mode, toggleMode } = useTheme();

  return (
    <main className="app-shell">
      <header className="app-header">
        <nav className="nav-links">
          <Link to="/">Dashboard</Link>
          <Link to="/organizations">Organizations</Link>
          <Link to="/subscriptions">Subscriptions</Link>
          <Link to="/diagram">Diagram</Link>
        </nav>
        <button className="btn" onClick={toggleMode} type="button">Theme: {mode}</button>
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
