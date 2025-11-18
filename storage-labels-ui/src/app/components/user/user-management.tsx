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
} from '@mui/material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';

export const UserManagement: React.FC = () => {
    const { Api } = useApi();
    const alert = useAlertMessage();
    const [users, setUsers] = useState<UserWithRoles[]>([]);
    const [loading, setLoading] = useState(true);
    const [updating, setUpdating] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

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
            
            // Show success via snackbar
            console.log(`User role updated to ${newRole}`);
        } catch (err: any) {
            const errorMessage = err.response?.data?.message || 'Failed to update user role';
            alert.addError(errorMessage);
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
            </Box>
        </Container>
    );
};
