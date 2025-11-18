import React, { useState } from 'react';
import { Box, Button, TextField, Typography, Paper, Alert } from '@mui/material';
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
        <Box>
            <Typography variant="h5" component="h2" gutterBottom>
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

            <Paper elevation={1} sx={{ p: 3, maxWidth: 500 }}>
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

                    <Button
                        fullWidth
                        type="submit"
                        variant="contained"
                        color="primary"
                        sx={{ mt: 3 }}
                        disabled={isLoading}
                    >
                        {isLoading ? 'Changing Password...' : 'Change Password'}
                    </Button>
                </form>
            </Paper>
        </Box>
    );
};
