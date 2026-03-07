import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

type ThemeMode = 'light' | 'dark' | 'light-hc' | 'dark-hc';

type ThemeContextValue = {
  mode: ThemeMode;
  toggleMode: () => void;
};

const THEME_STORAGE_KEY = 'c4_theme';

const THEME_CYCLE: ThemeMode[] = ['dark', 'light', 'dark-hc', 'light-hc'];

function loadStoredTheme(): ThemeMode {
  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (stored === 'light' || stored === 'dark' || stored === 'light-hc' || stored === 'dark-hc') {
      return stored;
    }
  } catch {
    // localStorage unavailable (privacy mode, storage disabled)
  }
  return 'dark';
}

function saveTheme(mode: ThemeMode): void {
  try {
    localStorage.setItem(THEME_STORAGE_KEY, mode);
  } catch {
    // localStorage unavailable
  }
}

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<ThemeMode>(loadStoredTheme);

  useEffect(() => {
    document.documentElement.setAttribute('data-theme', mode);
    saveTheme(mode);
  }, [mode]);

  const value = useMemo(
    () => ({
      mode,
      toggleMode: () =>
        setMode(current => {
          const index = THEME_CYCLE.indexOf(current);
          return THEME_CYCLE[(index + 1) % THEME_CYCLE.length] ?? 'dark';
        }),
    }),
    [mode]
  );

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme() {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within ThemeProvider');
  }

  return context;
}
