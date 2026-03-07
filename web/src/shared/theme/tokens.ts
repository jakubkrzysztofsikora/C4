export const tokens = {
  spacing: {
    xs: 4,
    sm: 8,
    md: 16,
    lg: 24,
    xl: 32,
    '2xl': 48,
    '3xl': 64
  },
  radius: {
    sm: '6px',
    md: '10px',
    lg: '14px',
    xl: '20px',
    '2xl': '24px'
  },
  shadow: {
    sm: '0 1px 3px rgba(0,0,0,0.08)',
    md: '0 4px 12px rgba(0,0,0,0.1)',
    lg: '0 8px 24px rgba(0,0,0,0.12)',
    xl: '0 16px 48px rgba(0,0,0,0.15)'
  },
  motion: {
    fast: '0.12s',
    normal: '0.2s',
    slow: '0.35s'
  },
  typography: {
    fontFamily: 'Inter, system-ui, sans-serif',
    scale: {
      xs: '11px',
      sm: '13px',
      base: '14px',
      md: '15px',
      lg: '18px',
      xl: '22px',
      '2xl': '28px',
      '3xl': '36px',
      '4xl': '48px'
    },
    weight: {
      regular: 400,
      medium: 500,
      semibold: 600,
      bold: 700,
      extrabold: 800
    }
  },
  colors: {
    light: {
      background: '#f8fafc',
      foreground: '#0f172a',
      border: '#cbd5e1'
    },
    dark: {
      background: '#0f172a',
      foreground: '#e2e8f0',
      border: '#334155'
    }
  }
} as const;
