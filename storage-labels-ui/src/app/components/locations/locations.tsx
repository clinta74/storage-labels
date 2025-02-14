import { Avatar, Box, IconButton, List, ListItem, ListItemAvatar, ListItemText, Paper, Typography } from '@mui/material';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import WarehouseIcon from '@mui/icons-material/Warehouse';
import React, { useEffect, useState } from 'react';
import { useAlertMessage } from '../../providers/alert-provider';
import { useApi } from '../../../api';

export const Locations: React.FC = () => {
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [locations, setLocations] = useState<Location[]>([]);

    useEffect(() => {
        Api.Location.getLocaions()
            .then(({ data }) => {
                setLocations(data);
            });
    }, []);

    return (
        <React.Fragment>
            <Paper>
                <Box margin={1} textAlign="center">
                    <Typography variant='h4'>Your Locations</Typography>
                </Box>
                <Box margin={2}>
                    <List>
                        {
                            locations.map(location =>
                                <ListItem
                                    secondaryAction={
                                        <IconButton edge="end" aria-label="delete">
                                            <NavigateNextIcon />
                                        </IconButton>
                                    }
                                >
                                    <ListItemAvatar>
                                        <Avatar>
                                            <WarehouseIcon />
                                        </Avatar>
                                    </ListItemAvatar>
                                    <ListItemText primary={location.name} />
                                </ListItem>
                            )
                        }
                    </List>
                </Box>
            </Paper>
        </React.Fragment>
    );
}