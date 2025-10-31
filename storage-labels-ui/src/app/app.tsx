import React from 'react';
import { Box, Container, ThemeProvider } from '@mui/material';
import { BrowserRouter as Router } from 'react-router';
import { ConfirmProvider } from 'material-ui-confirm';
import { Auth0ProviderWithHistory } from '../auth/auth0-provider-with-history';
import { AppRoutes } from './app-routes';
import { AlertMessage, AlertProvider } from './providers/alert-provider';
import { UserPermissionProvider } from './providers/user-permission-provider';
import { NavigationBar } from './components/navigation-bar';
import { SnackbarProvider } from './providers/snackbar-provider';
import { theme } from './theme';

export const App: React.FC = () => {
    return (
        <React.Fragment>
            <ThemeProvider theme={theme}>
                <ConfirmProvider>
                    <Box position="relative" minHeight="100vh" zIndex={1}>
                        <Box position="relative" zIndex={2}>
                            <AlertProvider>
                                <SnackbarProvider>
                                    <AlertMessage />
                                    <Router>
                                        <Auth0ProviderWithHistory>
                                            <UserPermissionProvider>
                                                <NavigationBar />
                                                <Container maxWidth="lg" style={{ paddingBottom: 8 }}>
                                                    <AppRoutes />
                                                </Container>
                                            </UserPermissionProvider>
                                        </Auth0ProviderWithHistory>
                                    </Router>
                                </SnackbarProvider>
                            </AlertProvider>
                        </Box>
                    </Box>
                </ConfirmProvider>
            </ThemeProvider>
        </React.Fragment >
    );
}
