import React from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { useAuth0 } from '@auth0/auth0-react';
import { Box, CircularProgress, Typography } from '@mui/material';

import { Welcome } from './components/welcome';
import { ApiProvider } from '../api';
import { LegalRoutes } from './components/legal/legal-routes';
import { NewUserRoutes } from './components/new-user/new-user-routes';
import { LocationRoutes } from './components/locations/locations-routes';

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
                    <Routes>
                        <Route path="/legal/*" element={<LegalRoutes />} />
                        <Route path="/new-user/*" element={<NewUserRoutes />} />
                        <Route path="/location/*" element={<LocationRoutes />} />
                        <Route index element={<Navigate to="/location" />} />
                    </Routes>
                </ApiProvider>
            </Box>
        );
    }

    return <Welcome />
}
