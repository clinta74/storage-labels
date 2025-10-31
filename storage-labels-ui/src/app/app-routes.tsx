import React from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { useAuth0 } from '@auth0/auth0-react';
import { Box, CircularProgress, Typography } from '@mui/material';

import { Welcome } from './components/welcome';
import { ApiProvider } from '../api';
import { LegalRoutes } from './components/legal/legal-routes';
import { NewUserRoutes } from './components/new-user/new-user-routes';
import { LocationRoutes } from './components/locations/locations-routes';
import { ImagesRoutes } from './components/images/images-routes';
import { CommonLocationsRoutes } from './components/common-locations/common-locations-routes';
import { Preferences } from './components/user/preferences';
import { UserProvider } from './providers/user-provider';
import { AppThemeProvider } from './providers/theme-provider';

export const AppRoutes: React.FunctionComponent = () => {
    const { isAuthenticated, isLoading } = useAuth0();

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
    else if (isAuthenticated) {
        return (
            <Box>
                <ApiProvider>
                    <UserProvider>
                        <AppThemeProvider>
                            <Routes>
                                <Route path="/legal/*" element={<LegalRoutes />} />
                                <Route path="/new-user/*" element={<NewUserRoutes />} />
                                <Route path="/locations/*" element={<LocationRoutes />} />
                                <Route path="/images/*" element={<ImagesRoutes />} />
                                <Route path="/common-locations/*" element={<CommonLocationsRoutes />} />
                                <Route path="/preferences" element={<Preferences />} />
                                <Route index element={<Navigate to="/locations" />} />
                                <Route path="*" element={<Navigate to="/" replace />} />
                            </Routes>
                        </AppThemeProvider>
                    </UserProvider>
                </ApiProvider>
            </Box>
        );
    }

    return <Welcome />
}
