import React, { useState } from 'react';
import { Box, Button, TextField, Typography, Paper, Alert, Container, Stack } from '@mui/material';
import axios from 'axios';
import { CONFIG } from '../../../config';
import { useAuth } from '../../../auth/auth-provider';

export const ChangePassword: React.FC = () => {
    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState(false);
    const [isLoading, setIsLoading] = useState(false);
    const { getAccessToken } = useAuth();

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(false);

        if (newPassword !== confirmPassword) {
            setError('New passwords do not match');
            return;
        }

        if (newPassword.length < 8) {
            setError('New password must be at least 8 characters');
            return;
        }

        setIsLoading(true);

        try {
            const token = await getAccessToken();
            await axios.post(
                `${CONFIG.API_URL}/api/auth/change-password`,
                {
                    currentPassword,
                    newPassword
                },
                {
                    headers: { Authorization: `Bearer ${token}` }
                }
            );

            setSuccess(true);
            setCurrentPassword('');
            setNewPassword('');
            setConfirmPassword('');
        } catch (err: any) {
            setError(err.response?.data?.error || 'Failed to change password. Please check your current password.');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Container maxWidth="md">
            <Typography variant="h4" gutterBottom sx={{ mt: 2 }}>
                Change Password
            </Typography>

            {success && (
                <Alert severity="success" sx={{ mb: 2 }}>
                    Password changed successfully!
                </Alert>
            )}

            {error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                    {error}
                </Alert>
            )}

            <Paper sx={{ p: 3 }}>
                <form onSubmit={handleSubmit}>
                    <TextField
                        fullWidth
                        label="Current Password"
                        type="password"
                        value={currentPassword}
                        onChange={(e) => setCurrentPassword(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <TextField
                        fullWidth
                        label="New Password"
                        type="password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                        helperText="Minimum 8 characters"
                    />

                    <TextField
                        fullWidth
                        label="Confirm New Password"
                        type="password"
                        value={confirmPassword}
                        onChange={(e) => setConfirmPassword(e.target.value)}
                        margin="normal"
                        required
                        disabled={isLoading}
                    />

                    <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                        <Button
                            type="submit"
                            color="primary"
                            loading={isLoading}
                        >
                            Change Password
                        </Button>
                    </Stack>
                </form>
            </Paper>
        </Container>
    );
};
