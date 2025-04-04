import { Avatar, Box, createTheme, Fab, IconButton, List, ListItem, ListItemAvatar, ListItemButton, ListItemText, Paper, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { Link } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useApi } from '../../../api';
import AddIcon from '@mui/icons-material/Add';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import WarehouseIcon from '@mui/icons-material/Warehouse';

export const Locations: React.FC = () => {
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [locations, setLocations] = useState<StorageLocation[]>([]);

    const theme = createTheme();

    useEffect(() => {
        Api.Location.getLocaions()
            .then(({ data }) => {
                setLocations(data);
            });
    }, []);

    return (
        <React.Fragment>
            <Box position="relative">
                <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)}>
                    <Fab color="primary" title="Add a Location" aria-label="add" component={Link} to={`add`}>
                        <AddIcon />
                    </Fab>
                </Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant='h4'>Your Locations</Typography>
                    </Box>
                    <Box margin={2}>
                        <List>
                            {
                                locations.map(location =>
                                    <ListItem key={location.locationId}
                                        secondaryAction={
                                            <IconButton edge="end" aria-label="delete">
                                                <NavigateNextIcon />
                                            </IconButton>
                                        }
                                    >
                                        <ListItemButton component={Link} to={`${location.locationId}`}>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <WarehouseIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText primary={location.name} />
                                        </ListItemButton>
                                    </ListItem>
                                )
                            }
                        </List>
                    </Box>
                </Paper>
            </Box>
        </React.Fragment>
    );
}