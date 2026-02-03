import React, { useMemo, useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router';
import { Box, Button, TextField, Typography, Paper, Alert } from '@mui/material';
import { useAuth } from '../../../auth/auth-provider';
import { useUser } from '../../providers/user-provider';

export const Login: React.FC = () => {
    const [usernameOrEmail, setUsernameOrEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const { login } = useAuth();
    const { updateUser } = useUser();
    const navigate = useNavigate();
    const location = useLocation();
    const sessionExpired = useMemo(() => {
        const params = new URLSearchParams(location.search);
        return params.get('notice') === 'session-expired';
    }, [location.search]);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setIsLoading(true);

        try {
            await login(usernameOrEmail, password);
            await updateUser();
            const from = (location.state as any)?.from?.pathname || '/';
            navigate(from, { replace: true });
        } catch (err: any) {
            setError(err.message || 'Login failed. Please check your credentials.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Box
            display="flex"
            justifyContent="center"
            alignItems="center"
            minHeight="60vh"
        >
            <Paper elevation={3} sx={{ p: 4, maxWidth: 400, width: '100%' }}>
                <Typography variant="h4" component="h1" gutterBottom>
                    Login
                </Typography>

                {sessionExpired && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        Your session expired. Please sign in again to continue.
                    </Alert>
                )}

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                <form onSubmit={handleSubmit}>
                    <TextField
                        fullWidth
                        label="Username or Email"
                        value={usernameOrEmail}
                        onChange={(e) => setUsernameOrEmail(e.target.value)}
                        margin="normal"
                        required
                        autoFocus
                        disabled={isLoading}
                    />

                    <TextField
                        fullWidth
                        label="Password"
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <Button
                        fullWidth
                        type="submit"
                        color="primary"
                        sx={{ mt: 3, mb: 2 }}
                        disabled={isLoading}
                    >
                        {isLoading ? 'Logging in...' : 'Login'}
                    </Button>

                    <Button
                        fullWidth
                        variant="text"
                        component={Link}
                        to="/register"
                        disabled={isLoading}
                    >
                        Don&apos;t have an account? Register
                    </Button>
                </form>
            </Paper>
        </Box>
    );
};
