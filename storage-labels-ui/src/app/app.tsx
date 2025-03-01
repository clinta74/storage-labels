import React from 'react';
import { Box, Container, createTheme, CssBaseline, Theme } from '@mui/material';
import { BrowserRouter as Router } from 'react-router';
import { ConfirmProvider } from 'material-ui-confirm';
import { Auth0ProviderWithHistory } from '../auth/auth0-provider-with-history';
import { AppRoutes } from './app-routes';
import { AlertMessage, AlertProvider } from './providers/alert-provider';
import { UserPermissionProvider } from './providers/user-permission-provider';

export const App: React.FC = () => {
    // const theme = createTheme();

    return (
        <React.Fragment>
            <CssBaseline />
            <ConfirmProvider>
                <Box position="relative" minHeight="100vh" zIndex={1}>
                    <Box position="relative" zIndex={2}>
                        <AlertProvider>
                            <AlertMessage />
                            <Router>
                                <Auth0ProviderWithHistory>
                                    <UserPermissionProvider>
                                        <Container maxWidth="lg" style={{ paddingBottom: 8 }}>
                                            <AppRoutes />
                                        </Container>
                                    </UserPermissionProvider>
                                </Auth0ProviderWithHistory>
                            </Router>
                        </AlertProvider>
                    </Box>
                </Box>
            </ConfirmProvider>
        </React.Fragment >
    );
}
