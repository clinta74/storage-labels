import { AppState, useAuth0 } from '@auth0/auth0-react';
import { Box, Button, Typography } from '@mui/material';
import React from 'react';

export const Welcome: React.FC = () => {
    const { loginWithRedirect } = useAuth0();

    const login = () => {
        const appState: AppState = {
            returnTo: window.location.pathname,
        }
        loginWithRedirect({ appState });
    }
    return (
        <Box display="flex" justifyContent="center" p={4}>
            <Box textAlign="center">
                <Typography variant="h3">Storage Labels</Typography>
                <Typography variant="body1">Please sign in to take full advantage of the features that are offered here.</Typography>
                <Box mt={4}>
                    <Button variant="contained" size="large" color="primary" onClick={login}>Sign In</Button>
                </Box>
            </Box>
        </Box>
    );
}