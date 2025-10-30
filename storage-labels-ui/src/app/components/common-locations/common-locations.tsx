import React, { useEffect, useState } from 'react';
import {
    Avatar,
    Box,
    Fab,
    IconButton,
    List,
    ListItem,
    ListItemAvatar,
    ListItemButton,
    ListItemText,
    Paper,
    Typography,
    useTheme,
} from '@mui/material';
import { Link } from 'react-router';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import { useUserPermission } from '../../providers/user-permission-provider';
import { Permissions } from '../../constants/permissions';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';

export const CommonLocations: React.FC = () => {
    const { hasPermission } = useUserPermission();
    const { Api } = useApi();
    const alert = useAlertMessage();
    const [locations, setLocations] = useState<CommonLocation[]>([]);
    const theme = useTheme();

    const canWrite = hasPermission(Permissions.Write_CommonLocations);

    useEffect(() => {
        Api.CommonLocation.getCommonLocations()
            .then(({ data }) => {
                setLocations(data);
            })
            .catch(error => alert.addMessage(error));
    }, []);

    const handleDelete = (locationId: number) => {
        if (canWrite) {
            Api.CommonLocation.deleteCommonLocation(locationId)
                .then(() => {
                    setLocations(locations.filter(loc => loc.commonLocationId !== locationId));
                })
                .catch(error => alert.addMessage(error));
        }
    };

    return (
        <React.Fragment>
            <Box position="relative">
                {canWrite && (
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <Fab color="primary" title="Add a Common Location" aria-label="add" component={Link} to="add">
                            <AddIcon />
                        </Fab>
                    </Box>
                )}
                <Paper>
                    <Box 
                        margin={1} 
                        textAlign="center"
                        sx={{
                            px: { xs: 8, sm: 2 }, // Extra horizontal padding on mobile to avoid FAB overlap
                            pt: { xs: 1.5, sm: 1 } // Slightly more top padding on mobile
                        }}
                    >
                        <Typography variant="h4" sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2.125rem' } // Slightly smaller on mobile
                        }}>
                            Common Locations
                        </Typography>
                    </Box>
                    <Box margin={2}>
                        {locations.length === 0 ? (
                            <Typography variant="body1" color="text.secondary" textAlign="center" py={4}>
                                No common locations available.
                                {canWrite && ' Click the + button to add one.'}
                            </Typography>
                        ) : (
                            <List>
                                {locations.map((location) => (
                                    <ListItem
                                        key={location.commonLocationId}
                                        secondaryAction={
                                            canWrite && (
                                                <IconButton
                                                    edge="end"
                                                    aria-label="delete"
                                                    onClick={() => handleDelete(location.commonLocationId)}
                                                >
                                                    <DeleteIcon />
                                                </IconButton>
                                            )
                                        }
                                    >
                                        <ListItemButton>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <LocationOnIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText primary={location.name} />
                                        </ListItemButton>
                                    </ListItem>
                                ))}
                            </List>
                        )}
                    </Box>
                </Paper>
                {!canWrite && (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 2, textAlign: 'center' }}>
                        You have read-only access. Contact an administrator for write permissions.
                    </Typography>
                )}
            </Box>
        </React.Fragment>
    );
};
