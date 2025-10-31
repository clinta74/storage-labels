import React, { PropsWithChildren, useMemo } from 'react';
import { ThemeProvider as MuiThemeProvider, CssBaseline } from '@mui/material';
import { getTheme } from '../theme';
import { useUser } from './user-provider';

export const AppThemeProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const { user } = useUser();

    const theme = useMemo(() => {
        const mode = user?.preferences?.theme === 'dark' ? 'dark' : 'light';
        return getTheme(mode);
    }, [user?.preferences?.theme]);

    return (
        <MuiThemeProvider theme={theme}>
            <CssBaseline />
            {children}
        </MuiThemeProvider>
    );
};
