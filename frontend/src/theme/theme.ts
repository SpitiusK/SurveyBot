import { createTheme, type ThemeOptions } from '@mui/material/styles';

// Extended color palette types for Material Design color scales
declare module '@mui/material/styles' {
  interface PaletteColor {
    50?: string;
    100?: string;
    200?: string;
  }
  interface SimplePaletteColorOptions {
    50?: string;
    100?: string;
    200?: string;
  }
}

// Shared theme options
const getThemeOptions = (mode: 'light' | 'dark'): ThemeOptions => ({
  palette: {
    mode,
    primary: {
      main: '#1976d2',
      light: '#42a5f5',
      dark: '#1565c0',
      contrastText: '#fff',
      // Extended palette for light/dark mode compatibility
      50: mode === 'light' ? '#e3f2fd' : '#0d47a1',
      100: mode === 'light' ? '#bbdefb' : '#1565c0',
      200: mode === 'light' ? '#90caf9' : '#1976d2',
    },
    secondary: {
      main: '#dc004e',
      light: '#f33371',
      dark: '#9a0036',
      contrastText: '#fff',
    },
    success: {
      main: '#2e7d32',
      light: '#4caf50',
      dark: '#1b5e20',
      // Extended palette for light/dark mode compatibility
      50: mode === 'light' ? '#e8f5e9' : '#1b5e20',
      100: mode === 'light' ? '#c8e6c9' : '#2e7d32',
      200: mode === 'light' ? '#a5d6a7' : '#388e3c',
    },
    error: {
      main: '#d32f2f',
      light: '#ef5350',
      dark: '#c62828',
      // Extended palette for light/dark mode compatibility
      50: mode === 'light' ? '#ffebee' : '#b71c1c',
      100: mode === 'light' ? '#ffcdd2' : '#c62828',
      200: mode === 'light' ? '#ef9a9a' : '#d32f2f',
    },
    warning: {
      main: '#ed6c02',
      light: '#ff9800',
      dark: '#e65100',
      // Extended palette for light/dark mode compatibility
      50: mode === 'light' ? '#fff3e0' : '#e65100',
      100: mode === 'light' ? '#ffe0b2' : '#ef6c00',
      200: mode === 'light' ? '#ffcc80' : '#f57c00',
    },
    info: {
      main: '#0288d1',
      light: '#03a9f4',
      dark: '#01579b',
      // Extended palette for light/dark mode compatibility
      50: mode === 'light' ? '#e1f5fe' : '#01579b',
      100: mode === 'light' ? '#b3e5fc' : '#0277bd',
      200: mode === 'light' ? '#81d4fa' : '#0288d1',
    },
    grey: {
      50: mode === 'light' ? '#fafafa' : '#2c2c2c',
      100: mode === 'light' ? '#f5f5f5' : '#3a3a3a',
      200: mode === 'light' ? '#eeeeee' : '#4a4a4a',
      300: mode === 'light' ? '#e0e0e0' : '#5a5a5a',
      400: mode === 'light' ? '#bdbdbd' : '#6a6a6a',
      500: mode === 'light' ? '#9e9e9e' : '#7a7a7a',
      600: mode === 'light' ? '#757575' : '#8a8a8a',
      700: mode === 'light' ? '#616161' : '#9a9a9a',
      800: mode === 'light' ? '#424242' : '#aaaaaa',
      900: mode === 'light' ? '#212121' : '#bdbdbd',
      A100: '#f5f5f5',
      A200: '#eeeeee',
      A400: '#bdbdbd',
      A700: '#616161',
    },
    ...(mode === 'light'
      ? {
          background: {
            default: '#f5f5f5',
            paper: '#ffffff',
          },
          text: {
            primary: '#212121',
            secondary: '#757575',
          },
        }
      : {
          background: {
            default: '#121212',
            paper: '#1e1e1e',
          },
          text: {
            primary: '#ffffff',
            secondary: '#b0b0b0',
          },
        }),
  },
  typography: {
    fontFamily: [
      '-apple-system',
      'BlinkMacSystemFont',
      '"Segoe UI"',
      'Roboto',
      '"Helvetica Neue"',
      'Arial',
      'sans-serif',
    ].join(','),
    h1: {
      fontSize: '2.5rem',
      fontWeight: 600,
      lineHeight: 1.2,
    },
    h2: {
      fontSize: '2rem',
      fontWeight: 600,
      lineHeight: 1.3,
    },
    h3: {
      fontSize: '1.75rem',
      fontWeight: 600,
      lineHeight: 1.4,
    },
    h4: {
      fontSize: '1.5rem',
      fontWeight: 600,
      lineHeight: 1.4,
    },
    h5: {
      fontSize: '1.25rem',
      fontWeight: 600,
      lineHeight: 1.5,
    },
    h6: {
      fontSize: '1rem',
      fontWeight: 600,
      lineHeight: 1.6,
    },
    body1: {
      fontSize: '1rem',
      lineHeight: 1.5,
    },
    body2: {
      fontSize: '0.875rem',
      lineHeight: 1.43,
    },
    button: {
      textTransform: 'none',
      fontWeight: 500,
    },
  },
  shape: {
    borderRadius: 8,
  },
  spacing: 8,
  breakpoints: {
    values: {
      xs: 0,
      sm: 600,
      md: 960,
      lg: 1280,
      xl: 1920,
    },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: 8,
          padding: '8px 16px',
          fontSize: '0.875rem',
          fontWeight: 500,
        },
        contained: {
          boxShadow: 'none',
          '&:hover': {
            boxShadow: '0 2px 8px rgba(0,0,0,0.15)',
          },
        },
      },
    },
    MuiCard: {
      styleOverrides: {
        root: {
          borderRadius: 12,
          boxShadow: mode === 'light'
            ? '0 2px 8px rgba(0,0,0,0.1)'
            : '0 2px 8px rgba(0,0,0,0.3)',
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        root: {
          borderRadius: 12,
        },
        elevation1: {
          boxShadow: mode === 'light'
            ? '0 1px 4px rgba(0,0,0,0.08)'
            : '0 1px 4px rgba(0,0,0,0.4)',
        },
      },
    },
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: 8,
          },
        },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: 8,
        },
      },
    },
    MuiDrawer: {
      styleOverrides: {
        paper: {
          borderRadius: 0,
        },
      },
    },
    MuiDialog: {
      defaultProps: {
        // Use disableScrollLock to prevent body scroll manipulation
        // This also prevents aria-hidden from being added to the root
        disableScrollLock: true,
      },
    },
  },
});

// Create light theme
export const lightTheme = createTheme(getThemeOptions('light'));

// Create dark theme
export const darkTheme = createTheme(getThemeOptions('dark'));

// Default export is light theme
export default lightTheme;
