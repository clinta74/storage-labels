import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useApi } from '../../../api';
import { Avatar, Box, createTheme, Fab, IconButton, List, ListItem, ListItemAvatar, ListItemButton, ListItemText, Paper, Typography } from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import NavigateBeforeIcon from '@mui/icons-material/NavigateBefore';
import WarehouseIcon from '@mui/icons-material/Warehouse';

type Params = Record<'locationId', string>

export const Location: React.FC = () => {
    const params = useParams<Params>();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [boxes, setBoxes] = useState<Box[]>([]);
    const [location, setLocation] = useState<StorageLocation>();

    const theme = createTheme();

    useEffect(() => {
        const locationId = Number(params.locationId);

        if (locationId) {
            Api.Location.getLocation(locationId)
                .then(({ data }) => {
                    setLocation(data);
                });
            Api.Box.getBoxes(locationId)
                .then(({ data }) => {
                    setBoxes(data);
                });
        }
    }, [params]);

    return (
        <React.Fragment>
            <Paper>
                <Box position="relative">
                    <Box position="absolute" left={theme.spacing(1)} top={theme.spacing(1)}>
                        <IconButton edge="end" aria-label="back" component={Link} to={`../`}>
                            <NavigateBeforeIcon />
                        </IconButton>
                    </Box>
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)}>
                        <Fab color="primary" title="Add a Box" aria-label="add" component={Link} to={`add`}>
                            <AddIcon />
                        </Fab>
                    </Box>
                    <Box margin={1} textAlign="center">
                        <Typography variant='h4'>{location?.name}</Typography>
                    </Box>
                    <Box margin={2}>
                        <List>
                            {
                                boxes.map(box =>
                                    <ListItem key={box.boxId}
                                        secondaryAction={
                                            <IconButton edge="end" aria-label="delete">
                                                <NavigateNextIcon />
                                            </IconButton>
                                        }
                                    >
                                        <ListItemButton component={Link} to={`box/${box.boxId}`}>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <WarehouseIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText primary={box.name} secondary={box.description} />
                                        </ListItemButton>
                                    </ListItem>
                                )
                            }
                        </List>
                    </Box>
                </Box>
            </Paper>
        </React.Fragment>
    );
}