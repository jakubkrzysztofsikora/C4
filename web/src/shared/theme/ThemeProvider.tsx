import { createContext, useContext, useMemo, useState, type ReactNode } from 'react';
import { tokens } from './tokens';

type ThemeMode = 'light' | 'dark';

type ThemeContextValue = {
  mode: ThemeMode;
  toggleMode: () => void;
};

const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setMode] = useState<ThemeMode>('dark');

  const value = useMemo(
    () => ({
      mode,
      toggleMode: () => setMode(current => (current === 'dark' ? 'light' : 'dark'))
    }),
    [mode]
  );

  const palette = tokens.colors[mode];

  return (
    <ThemeContext.Provider value={value}>
      <div style={{ backgroundColor: palette.background, color: palette.foreground, minHeight: '100vh' }}>{children}</div>
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
