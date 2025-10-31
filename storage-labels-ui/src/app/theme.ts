import { createTheme, PaletteMode } from '@mui/material';

export const getTheme = (mode: PaletteMode) => createTheme({
    palette: {
        mode,
        primary: {
            main: '#8B4513', // Brown to match storage box theme
            light: '#A0522D',
            dark: '#654321',
        },
        secondary: {
            main: mode === 'dark' ? '#9E9E9E' : '#757575', // Lighter gray for dark mode
            light: '#BDBDBD',
            dark: '#616161',
        },
        background: mode === 'dark' ? {
            default: '#1a1a1a',
            paper: '#2d2d2d',
        } : {
            default: '#F5F5F0', // Off-white background
            paper: '#FFFFFF',
        },
        text: mode === 'dark' ? {
            primary: '#E0E0E0',
            secondary: '#B0B0B0',
        } : {
            primary: '#1a1a1a',
            secondary: '#666666',
        },
    },
    typography: {
        fontFamily: '"Roboto", "Helvetica", "Arial", sans-serif',
        h4: {
            fontWeight: 600,
        },
        h6: {
            fontWeight: 500,
        },
    },
    shape: {
        borderRadius: 8,
    },
    components: {
        MuiContainer: {
            styleOverrides: {
                root: {
                    paddingTop: 8,
                    paddingBottom: 8,
                },
            },
        },
        MuiButton: {
            styleOverrides: {
                root: {
                    textTransform: 'none', // Remove uppercase transformation
                    fontWeight: 500,
                },
            },
        },
        MuiPaper: {
            styleOverrides: {
                root: {
                    backgroundImage: 'none', // Remove default elevation gradient
                },
            },
        },
        MuiAppBar: {
            styleOverrides: {
                root: {
                    backgroundImage: 'none',
                },
            },
        },
    },
    spacing: (factor: number) => `${8 * factor}px`,
});

// Default light theme for backward compatibility
export const theme = getTheme('light');
