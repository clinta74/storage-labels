import { 
    Box, 
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Divider,
    FormControl,
    FormLabel,
    IconButton,
    List,
    ListItem,
    ListItemText,
    MenuItem,
    Paper, 
    Select,
    Stack,
    TextField,
    Typography,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSnackbar } from '../../providers/snackbar-provider';
import { Breadcrumbs } from '../shared';

type Params = Record<'locationId', string>;

export const ManageLocationUsers: React.FC = () => {
    const params = useParams<Params>();
    const alert = useAlertMessage();
    const snackbar = useSnackbar();
    const { Api } = useApi();
    const [location, setLocation] = useState<StorageLocation | null>(null);
    
    // User access management state
    const [users, setUsers] = useState<UserLocationResponse[]>([]);
    const [newUserEmail, setNewUserEmail] = useState('');
    const [newUserAccessLevel, setNewUserAccessLevel] = useState<AccessLevels>('Edit');
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [userToDelete, setUserToDelete] = useState<UserLocationResponse | null>(null);
    const [emailError, setEmailError] = useState('');
    const [userNotFoundDialogOpen, setUserNotFoundDialogOpen] = useState(false);

    useEffect(() => {
        const locationId = Number(params.locationId);
        if (locationId) {
            Api.Location.getLocation(locationId)
                .then(({ data }) => {
                    setLocation(data);
                })
                .catch((error) => alert.addError(error));
            
            loadLocationUsers(locationId);
        }
    }, [params]);

    const loadLocationUsers = (locationId: number) => {
        Api.Location.getLocationUsers(locationId)
            .then(({ data }) => {
                setUsers(data);
            })
            .catch((error) => alert.addError(error));
    };

    const handleAddUser = () => {
        if (!newUserEmail || !location) return;

        // Validate email format
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(newUserEmail)) {
            setEmailError('Please enter a valid email address');
            return;
        }
        setEmailError('');

        Api.Location.addUserToLocation(location.locationId, {
            emailAddress: newUserEmail,
            accessLevel: newUserAccessLevel,
        })
            .then(() => {
                snackbar.showSuccess('User added successfully');
                setNewUserEmail('');
                setNewUserAccessLevel('Edit');
                loadLocationUsers(location.locationId);
            })
            .catch((error: unknown) => {
                if (error && typeof error === 'object' && 'response' in error && 
                    error.response && typeof error.response === 'object' && 
                    'status' in error.response && error.response.status === 404) {
                    setUserNotFoundDialogOpen(true);
                } else {
                    alert.addError(error);
                }
            });
    };

    const handleAccessLevelChange = (userId: string, newAccessLevel: AccessLevels) => {
        if (!location) return;

        Api.Location.updateUserLocationAccess(location.locationId, userId, {
            accessLevel: newAccessLevel,
        })
            .then(() => {
                snackbar.showSuccess('Access level updated');
                loadLocationUsers(location.locationId);
            })
            .catch((error) => alert.addError(error));
    };

    const handleDeleteClick = (user: UserLocationResponse) => {
        setUserToDelete(user);
        setDeleteDialogOpen(true);
    };

    const handleConfirmDelete = () => {
        if (!userToDelete || !location) return;

        Api.Location.removeUserFromLocation(location.locationId, userToDelete.userId)
            .then(() => {
                snackbar.showSuccess('User access removed');
                setDeleteDialogOpen(false);
                setUserToDelete(null);
                loadLocationUsers(location.locationId);
            })
            .catch((error) => alert.addError(error));
    };

    if (!location) {
        return null;
    }

    return (
        <React.Fragment>
            <Box margin={2} mb={2}>
                <Breadcrumbs 
                    items={[
                        { label: location.name, path: `/locations/${location.locationId}` },
                        { label: 'Manage Users' }
                    ]}
                />
            </Box>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant='h4'>Manage Users</Typography>
                    </Box>
                    <Box margin={2} pb={2}>
                        {/* Add User Form */}
                        <Box mb={2}>
                            <Typography variant='h6' gutterBottom>Add User</Typography>
                            <Stack direction="row" spacing={2} alignItems="flex-end">
                                <Box flexGrow={1}>
                                    <TextField
                                        label="Email Address"
                                        variant="standard"
                                        fullWidth
                                        value={newUserEmail}
                                        onChange={(e) => {
                                            setNewUserEmail(e.target.value);
                                            setEmailError('');
                                        }}
                                        type="email"
                                        error={!!emailError}
                                        helperText={emailError}
                                    />
                                </Box>
                                <FormControl variant="standard" sx={{ minWidth: 120 }}>
                                    <FormLabel>Access Level</FormLabel>
                                    <Select
                                        value={newUserAccessLevel}
                                        onChange={(e) => setNewUserAccessLevel(e.target.value as AccessLevels)}
                                    >
                                        <MenuItem value="View">View</MenuItem>
                                        <MenuItem value="Edit">Edit</MenuItem>
                                    </Select>
                                </FormControl>
                                <Button
                                    color="primary"
                                    onClick={handleAddUser}
                                    disabled={!newUserEmail}
                                >
                                    Add
                                </Button>
                            </Stack>
                        </Box>

                        <Divider sx={{ my: 2 }} />

                        {/* Users List */}
                        <Typography variant='h6' gutterBottom>Current Users</Typography>
                        {users.length === 0 ? (
                            <Typography variant="body2" color="text.secondary">
                                No users have access to this location.
                            </Typography>
                        ) : (
                            <List>
                                {users.map((user) => (
                                    <ListItem
                                        key={user.userId}
                                        secondaryAction={
                                            user.accessLevel === 'Owner' ? (
                                                <Typography variant="body2" color="text.secondary" sx={{ pr: 2 }}>
                                                    Owner
                                                </Typography>
                                            ) : (
                                                <Stack direction="row" spacing={1} alignItems="center">
                                                    <Select
                                                        value={user.accessLevel}
                                                        onChange={(e) => handleAccessLevelChange(user.userId, e.target.value as AccessLevels)}
                                                        variant="standard"
                                                        size="small"
                                                    >
                                                        <MenuItem value="View">View</MenuItem>
                                                        <MenuItem value="Edit">Edit</MenuItem>
                                                    </Select>
                                                    <IconButton
                                                        edge="end"
                                                        aria-label="delete"
                                                        title="Remove user access"
                                                        onClick={() => handleDeleteClick(user)}
                                                    >
                                                        <DeleteIcon />
                                                    </IconButton>
                                                </Stack>
                                            )
                                        }
                                        disablePadding
                                    >
                                        <ListItemText
                                            primary={`${user.firstName} ${user.lastName}`}
                                            secondary={user.emailAddress}
                                            sx={{ pr: 2 }}
                                        />
                                    </ListItem>
                                ))}
                            </List>
                        )}
                    </Box>
                </Paper>
            </Box>

            {/* Delete Confirmation Dialog */}
            <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
                <DialogTitle>Remove User Access</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        {userToDelete && (
                            <>
                                Are you sure you want to remove access for <strong>{userToDelete.firstName} {userToDelete.lastName}</strong> ({userToDelete.emailAddress})?
                            </>
                        )}
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleConfirmDelete} color="primary" autoFocus>
                        Remove
                    </Button>
                    <Button onClick={() => setDeleteDialogOpen(false)} color="secondary">
                        Cancel
                    </Button>
                </DialogActions>
            </Dialog>

            {/* User Not Found Dialog */}
            <Dialog open={userNotFoundDialogOpen} onClose={() => setUserNotFoundDialogOpen(false)}>
                <DialogTitle>User Not Found</DialogTitle>
                <DialogContent>
                    <DialogContentText>
                        No user with the email address <strong>{newUserEmail}</strong> was found in the system.
                    </DialogContentText>
                    <Typography variant="body2" sx={{ mt: 2 }}>
                        The user must create an account before they can be granted access to this location.
                        Please ask them to sign up first.
                    </Typography>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setUserNotFoundDialogOpen(false)} color="primary" autoFocus>
                        OK
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    );
};
