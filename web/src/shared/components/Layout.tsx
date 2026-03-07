import { useEffect, useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import { MdLightMode, MdDarkMode, MdContrast } from 'react-icons/md';
import { CommandPalette } from './CommandPalette';
import { KeyboardShortcutsDialog } from './KeyboardShortcutsDialog';
import { useTheme } from '../theme/ThemeProvider';
import { useAuth } from '../auth/AuthContext';
import { useProject } from '../project/ProjectContext';

export function Layout() {
  const { mode, toggleMode } = useTheme();
  const { user, logout } = useAuth();
  const { activeProject, projects, setActiveProject } = useProject();
  const [commandPaletteOpen, setCommandPaletteOpen] = useState(false);
  const [shortcutsOpen, setShortcutsOpen] = useState(false);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      const isMac = navigator.platform.toUpperCase().includes('MAC');
      const modKey = isMac ? e.metaKey : e.ctrlKey;

      if (modKey && e.key === 'k') {
        e.preventDefault();
        setCommandPaletteOpen((prev) => !prev);
        return;
      }

      if (e.key === '?' && !e.ctrlKey && !e.metaKey && !e.altKey) {
        const target = e.target as HTMLElement;
        const isTyping = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable;
        if (!isTyping) {
          setShortcutsOpen((prev) => !prev);
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, []);

  const themeAriaLabel = mode.includes('dark') ? 'Switch to light mode' : 'Switch to dark mode';

  return (
    <>
      <a href="#main-content" className="skip-to-content">Skip to content</a>
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
            {projects.length > 0 && (
              <select
                className="project-switcher"
                value={activeProject?.id ?? ''}
                onChange={(e) => setActiveProject(e.target.value)}
              >
                {projects.map((p) => (
                  <option key={p.id} value={p.id}>{p.name}</option>
                ))}
              </select>
            )}
            {user !== null && (
              <span className="header-user">{user.email}</span>
            )}
            <button
              className="btn btn-sm btn-ghost theme-toggle"
              onClick={toggleMode}
              type="button"
              aria-label={themeAriaLabel}
            >
              {mode === 'dark' && <MdDarkMode size={18} />}
              {mode === 'light' && <MdLightMode size={18} />}
              {mode.includes('hc') && <MdContrast size={18} />}
            </button>
            <button className="btn btn-sm btn-ghost btn-danger" onClick={logout} type="button">
              Sign out
            </button>
          </div>
        </header>
        <section style={{ marginTop: 12 }}>
          <CommandPalette
            isOpen={commandPaletteOpen}
            onOpen={() => setCommandPaletteOpen(true)}
            onClose={() => setCommandPaletteOpen(false)}
          />
        </section>
        <section id="main-content" style={{ marginTop: 16 }}>
          <Outlet />
        </section>
      </main>
      <KeyboardShortcutsDialog
        isOpen={shortcutsOpen}
        onClose={() => setShortcutsOpen(false)}
      />
    </>
  );
}
