import { createTheme } from '@mui/material';

export const theme = createTheme({
    components: {
        MuiContainer: {
            styleOverrides: {
                root: {
                    paddingTop: 8,
                    paddingBottom: 8,
                },
            },
        },
    },
    spacing: (factor: number) => {
        // Default spacing is 8px
        // On small screens, we can reduce it slightly for better space usage
        return `${8 * factor}px`;
    },
});
