import React, { useEffect, useState } from 'react';
import {
    Container,
    Paper,
    Typography,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Select,
    MenuItem,
    FormControl,
    Box,
    CircularProgress,
    Alert,
    Chip,
    IconButton,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    TextField,
} from '@mui/material';
import { Delete as DeleteIcon, LockReset as LockResetIcon } from '@mui/icons-material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSnackbar } from '../../providers/snackbar-provider';
import { useConfirm } from 'material-ui-confirm';
import { useUser } from '../../providers/user-provider';

export const UserManagement: React.FC = () => {
    const { Api } = useApi();
    const alert = useAlertMessage();
    const { showSuccess } = useSnackbar();
    const confirm = useConfirm();
    const { user: currentUser } = useUser();
    const [users, setUsers] = useState<UserWithRoles[]>([]);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [resetPasswordDialog, setResetPasswordDialog] = useState<{ open: boolean; userId: string | null; username: string | null }>({
        open: false,
        userId: null,
        username: null,
    });
    const [newPassword, setNewPassword] = useState('');

    const loadUsers = async () => {
        try {
            setLoading(true);
            setError(null);
            const { data } = await Api.User.getAllUsers();
            setUsers(data);
        } catch (err: any) {
            const errorMessage = err.response?.data?.message || 'Failed to load users';
            setError(errorMessage);
            alert.addError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadUsers();
    }, []);

    const handleRoleChange = async (userId: string, newRole: string) => {
        try {
            setUpdating(userId);
            await Api.User.updateUserRole(userId, newRole);
            
            // Update local state
            setUsers(prevUsers =>
                prevUsers.map(user =>
                    user.userId === userId
                        ? { ...user, roles: [newRole] }
                        : user
                )
            );
            
            showSuccess(`User role updated to ${newRole}`);
        } catch (err: any) {
            const errorMessage = err.response?.data?.message || 'Failed to update user role';
            alert.addError(errorMessage);
        } finally {
            setUpdating(null);
        }
    };

    const handleOpenResetPassword = (userId: string, username: string) => {
        setResetPasswordDialog({ open: true, userId, username });
        setNewPassword('');
    };

    const handleCloseResetPassword = () => {
        setResetPasswordDialog({ open: false, userId: null, username: null });
        setNewPassword('');
    };

    const handleResetPassword = async () => {
        if (!resetPasswordDialog.userId || !newPassword) {
            alert.addError('Please enter a new password');
            return;
        }

        if (newPassword.length < 6) {
            alert.addError('Password must be at least 6 characters');
            return;
        }

        try {
            await Api.User.adminResetPassword(resetPasswordDialog.userId, newPassword);
            showSuccess('Password reset successfully');
            handleCloseResetPassword();
        } catch (err: any) {
            const errorMessage = err.response?.data?.message || 'Failed to reset password';
            alert.addError(errorMessage);
        }
    };

    const handleDeleteUser = async (userId: string, username: string) => {
        // Prevent deleting yourself
        if (currentUser?.userId === userId) {
            alert.addError('You cannot delete your own account');
            return;
        }

        try {
            await confirm({
                title: 'Delete User',
                description: `Are you sure you want to delete user "${username}"? This action cannot be undone.`,
                confirmationText: 'Delete',
                cancellationText: 'Cancel',
                confirmationButtonProps: { color: 'error' },
            });

            setUpdating(userId);
            await Api.User.deleteUser(userId);
            
            // Remove from local state
            setUsers(prevUsers => prevUsers.filter(user => user.userId !== userId));
            
            showSuccess('User deleted successfully');
        } catch (err: any) {
            // Check if error is from cancellation or actual API error
            if (err?.message) {
                const errorMessage = err.response?.data?.message || 'Failed to delete user';
                alert.addError(errorMessage);
            }
        } finally {
            setUpdating(null);
        }
    };

    const getRoleColor = (role: string): "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning" => {
        switch (role) {
            case 'Admin':
                return 'error';
            case 'Auditor':
                return 'info';
            case 'User':
                return 'default';
            default:
                return 'default';
        }
    };

    if (loading) {
        return (
            <Container maxWidth="lg">
                <Box display="flex" justifyContent="center" alignItems="center" minHeight="50vh">
                    <CircularProgress />
                </Box>
            </Container>
        );
    }

    return (
        <Container maxWidth="lg">
            <Box my={4}>
                <Typography variant="h4" component="h1" gutterBottom>
                    User Management
                </Typography>

                {error && (
                    <Alert severity="error" sx={{ mb: 2 }}>
                        {error}
                    </Alert>
                )}

                <Paper>
                    <TableContainer>
                        <Table>
                            <TableHead>
                                <TableRow>
                                    <TableCell>Name</TableCell>
                                    <TableCell>Email</TableCell>
                                    <TableCell>Username</TableCell>
                                    <TableCell>Current Role</TableCell>
                                    <TableCell>Change Role</TableCell>
                                    <TableCell>Status</TableCell>
                                    <TableCell>Created</TableCell>
                                    <TableCell>Actions</TableCell>
                                </TableRow>
                            </TableHead>
                            <TableBody>
                                {users.map((user) => (
                                    <TableRow key={user.userId}>
                                        <TableCell>{user.fullName}</TableCell>
                                        <TableCell>{user.email}</TableCell>
                                        <TableCell>{user.username || '-'}</TableCell>
                                        <TableCell>
                                            {user.roles.map(role => (
                                                <Chip
                                                    key={role}
                                                    label={role}
                                                    color={getRoleColor(role)}
                                                    size="small"
                                                    sx={{ mr: 0.5 }}
                                                />
                                            ))}
                                        </TableCell>
                                        <TableCell>
                                            <FormControl size="small" sx={{ minWidth: 120 }}>
                                                <Select
                                                    value={user.roles[0] || 'User'}
                                                    onChange={(e) => handleRoleChange(user.userId, e.target.value)}
                                                    disabled={updating === user.userId}
                                                >
                                                    <MenuItem value="Admin">Admin</MenuItem>
                                                    <MenuItem value="Auditor">Auditor</MenuItem>
                                                    <MenuItem value="User">User</MenuItem>
                                                </Select>
                                            </FormControl>
                                            {updating === user.userId && (
                                                <CircularProgress size={20} sx={{ ml: 1 }} />
                                            )}
                                        </TableCell>
                                        <TableCell>
                                            <Chip
                                                label={user.isActive ? 'Active' : 'Inactive'}
                                                color={user.isActive ? 'success' : 'default'}
                                                size="small"
                                            />
                                        </TableCell>
                                        <TableCell>
                                            {new Date(user.created).toLocaleDateString()}
                                        </TableCell>
                                        <TableCell>
                                            <Box sx={{ display: 'flex', gap: 1 }}>
                                                <IconButton
                                                    size="small"
                                                    color="primary"
                                                    onClick={() => handleOpenResetPassword(user.userId, user.username || user.email)}
                                                    disabled={updating === user.userId}
                                                    title="Reset Password"
                                                >
                                                    <LockResetIcon />
                                                </IconButton>
                                                <IconButton
                                                    size="small"
                                                    color="error"
                                                    onClick={() => handleDeleteUser(user.userId, user.username || user.email)}
                                                    disabled={updating === user.userId || currentUser?.userId === user.userId}
                                                    title={currentUser?.userId === user.userId ? "Cannot delete yourself" : "Delete User"}
                                                >
                                                    <DeleteIcon />
                                                </IconButton>
                                            </Box>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    </TableContainer>

                    {users.length === 0 && (
                        <Box p={4} textAlign="center">
                            <Typography variant="body1" color="textSecondary">
                                No users found
                            </Typography>
                        </Box>
                    )}
                </Paper>

                {/* Reset Password Dialog */}
                <Dialog open={resetPasswordDialog.open} onClose={handleCloseResetPassword} maxWidth="sm" fullWidth>
                    <DialogTitle>Reset Password for {resetPasswordDialog.username}</DialogTitle>
                    <DialogContent>
                        <TextField
                            autoFocus
                            margin="dense"
                            label="New Password"
                            type="password"
                            fullWidth
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            helperText="Minimum 6 characters"
                        />
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={handleResetPassword} color="primary">
                            Reset Password
                        </Button>
                        <Button onClick={handleCloseResetPassword} color="secondary">
                            Cancel
                        </Button>
                    </DialogActions>
                </Dialog>
            </Box>
        </Container>
    );
};
