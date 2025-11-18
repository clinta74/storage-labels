import React, { useState } from 'react';
import { useNavigate } from 'react-router';
import { Box, Button, TextField, Typography, Paper, Alert } from '@mui/material';
import { useAuth } from '../../../auth/auth-provider';

export const Register: React.FC = () => {
    const [email, setEmail] = useState('');
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const { register } = useAuth();
    const navigate = useNavigate();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (password !== confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        setIsLoading(true);

        try {
            await register(email, username, password, firstName, lastName);
            navigate('/', { replace: true });
        } catch (err: any) {
            setError(err.response?.data?.message || 'Registration failed. Please try again.');
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
            <Paper elevation={3} sx={{ p: 4, maxWidth: 500, width: '100%' }}>
                <Typography variant="h4" component="h1" gutterBottom>
                    Register
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                <form onSubmit={handleSubmit}>
                    <TextField
                        fullWidth
                        label="Email"
                        type="email"
                        value={email}
                        onChange={(e) => setEmail(e.target.value)}
                        margin="normal"
                        required
                        autoFocus
                        disabled={isLoading}
                    />

                    <TextField
                        fullWidth
                        label="Username"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <TextField
                        fullWidth
                        label="First Name"
                        value={firstName}
                        onChange={(e) => setFirstName(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <TextField
                        fullWidth
                        label="Last Name"
                        value={lastName}
                        onChange={(e) => setLastName(e.target.value)}
                        margin="normal"
                        required
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

                    <TextField
                        fullWidth
                        label="Confirm Password"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <Button
                        fullWidth
                        type="submit"
                        variant="contained"
                        color="primary"
                        sx={{ mt: 3, mb: 2 }}
                        disabled={isLoading}
                    >
                        {isLoading ? 'Registering...' : 'Register'}
                    </Button>

                    <Button
                        fullWidth
                        variant="text"
                        onClick={() => navigate('/login')}
                        disabled={isLoading}
                    >
                        Already have an account? Login
                    </Button>
                </form>
            </Paper>
        </Box>
    );
};
