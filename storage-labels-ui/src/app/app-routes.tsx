import React from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { Box, CircularProgress, Typography } from '@mui/material';

import { useAuth } from '../auth/auth-provider';
import { Login } from './components/auth/login';
import { Register } from './components/auth/register';
import { ApiProvider } from '../api';
import { LegalRoutes } from './components/legal/legal-routes';
import { LocationRoutes } from './components/locations/locations-routes';
import { ImagesRoutes } from './components/images/images-routes';
import { CommonLocationsRoutes } from './components/common-locations/common-locations-routes';
import { EncryptionKeysRoutes } from './components/encryption-keys/encryption-keys-routes';
import { Preferences } from './components/user/preferences';
import { ChangePassword } from './components/user/change-password';
import { UserManagement } from './components/user/user-management';
import { UserProvider } from './providers/user-provider';
import { AppThemeProvider } from './providers/theme-provider';

export const AppRoutes: React.FunctionComponent = () => {
    const { isAuthenticated, isLoading, authMode } = useAuth();

    // Legal routes and auth routes are public
    return (
        <Routes>
            <Route path="/legal/*" element={<LegalRoutes />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />
            <Route path="/*" element={<AuthenticatedRoutes isLoading={isLoading} isAuthenticated={isAuthenticated} authMode={authMode} />} />
        </Routes>
    );
}

const AuthenticatedRoutes: React.FC<{ isLoading: boolean; isAuthenticated: boolean; authMode: 'Local' | 'None' | null }> = ({ isLoading, isAuthenticated, authMode }) => {
    if (isLoading) {
        return (
            <Box display="flex" flexDirection="column" justifyContent="center" alignItems="center" height="50vh">
                <CircularProgress size='5rem' />
                <Box mt={4}>
                    <Typography variant="h5">Putting some things away...</Typography>
                </Box>
            </Box>
        );
    }
    else if (isAuthenticated || authMode === 'None') {
        return (
            <Box>
                <ApiProvider>
                    <Routes>
                        <Route path="/*" element={
                            <UserProvider>
                                <AppThemeProvider>
                                    <Routes>
                                        <Route path="/locations/*" element={<LocationRoutes />} />
                                        <Route path="/images/*" element={<ImagesRoutes />} />
                                        <Route path="/common-locations/*" element={<CommonLocationsRoutes />} />
                                        <Route path="/encryption-keys/*" element={<EncryptionKeysRoutes />} />
                                        <Route path="/preferences" element={<Preferences />} />
                                        <Route path="/change-password" element={<ChangePassword />} />
                                        <Route path="/users" element={<UserManagement />} />
                                        <Route index element={<Navigate to="/locations" />} />
                                        <Route path="*" element={<Navigate to="/" replace />} />
                                    </Routes>
                                </AppThemeProvider>
                            </UserProvider>
                        } />
                    </Routes>
                </ApiProvider>
            </Box>
        );
    }

    return <Navigate to="/login" replace />
}
