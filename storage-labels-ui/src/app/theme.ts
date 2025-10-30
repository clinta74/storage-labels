import { createTheme } from '@mui/material';

export const theme = createTheme({
    palette: {
        primary: {
            main: '#8B4513', // Brown to match storage box theme
            light: '#A0522D',
            dark: '#654321',
        },
        secondary: {
            main: '#757575', // Medium gray for better visibility
            light: '#9E9E9E',
            dark: '#616161',
        },
        background: {
            default: '#F5F5F0', // Off-white background
            paper: '#FFFFFF',
        },
        text: {
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
    },
    spacing: (factor: number) => `${8 * factor}px`,
});
