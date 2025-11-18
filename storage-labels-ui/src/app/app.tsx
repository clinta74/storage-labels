import React from 'react';
import { Box, Container, ThemeProvider } from '@mui/material';
import { BrowserRouter as Router } from 'react-router';
import { ConfirmProvider } from 'material-ui-confirm';
import { AuthProvider } from '../auth/auth-provider';
import { AppRoutes } from './app-routes';
import { AlertMessage, AlertProvider } from './providers/alert-provider';
import { UserPermissionProvider } from './providers/user-permission-provider';
import { NavigationBar } from './components/navigation-bar';
import { SnackbarProvider } from './providers/snackbar-provider';
import { Footer } from './components/shared/footer';
import { theme } from './theme';

export const App: React.FC = () => {
    return (
        <React.Fragment>
            <ThemeProvider theme={theme}>
                <ConfirmProvider>
                    <Box display="flex" flexDirection="column" minHeight="100vh">
                        <Box position="relative" zIndex={2} flex="1">
                            <AlertProvider>
                                <SnackbarProvider>
                                    <AlertMessage />
                                    <Router>
                                        <AuthProvider>
                                            <UserPermissionProvider>
                                                <NavigationBar />
                                                <Container maxWidth="lg" style={{ paddingBottom: 8 }}>
                                                    <AppRoutes />
                                                </Container>
                                            </UserPermissionProvider>
                                        </AuthProvider>
                                    </Router>
                                </SnackbarProvider>
                            </AlertProvider>
                        </Box>
                        <Footer />
                    </Box>
                </ConfirmProvider>
            </ThemeProvider>
        </React.Fragment >
    );
}
