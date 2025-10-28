import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useApi } from '../../../api';
import { 
    Avatar, 
    Box, 
    Button,
    createTheme, 
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Fab, 
    IconButton, 
    List, 
    ListItem, 
    ListItemAvatar, 
    ListItemButton, 
    ListItemText, 
    Paper, 
    Typography 
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import NavigateBeforeIcon from '@mui/icons-material/NavigateBefore';
import DeleteIcon from '@mui/icons-material/Delete';
import InventoryIcon from '@mui/icons-material/Inventory';

type Params = Record<'locationId', string>

export const Location: React.FC = () => {
    const params = useParams<Params>();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [boxes, setBoxes] = useState<Box[]>([]);
    const [location, setLocation] = useState<StorageLocation>();
    const [boxToDelete, setBoxToDelete] = useState<Box | null>(null);
    const [boxItemCount, setBoxItemCount] = useState<number>(0);
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);

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

    const handleDeleteClick = (box: Box) => {
        // Fetch the number of items in the box
        Api.Item.getItemsByBoxId(box.boxId)
            .then(({ data }) => {
                setBoxItemCount(data.length);
                setBoxToDelete(box);
                setOpenDeleteDialog(true);
            })
            .catch((error) => alert.addMessage(error));
    };

    const handleCloseDeleteDialog = () => {
        setOpenDeleteDialog(false);
        setBoxToDelete(null);
        setBoxItemCount(0);
    };

    const handleConfirmDelete = () => {
        if (boxToDelete && params.locationId) {
            Api.Box.deleteBox(boxToDelete.boxId)
                .then(() => {
                    // Refresh the boxes list
                    Api.Box.getBoxes(Number(params.locationId))
                        .then(({ data }) => {
                            setBoxes(data);
                        })
                        .catch((error) => alert.addMessage(error));
                    handleCloseDeleteDialog();
                })
                .catch((error) => alert.addMessage(error.message));
        }
    };

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
                        <Fab color="primary" title="Add a Box" aria-label="add" component={Link} to={`box/add`}>
                            <AddIcon />
                        </Fab>
                    </Box>
                    <Box margin={1} textAlign="center" pb={2}>
                        <Typography variant='h4'>{location?.name}</Typography>
                    </Box>
                    <Box margin={2}>
                        <List>
                            {
                                boxes.map(box =>
                                    <ListItem key={box.boxId}
                                        secondaryAction={
                                            <IconButton 
                                                edge="end" 
                                                aria-label="delete"
                                                onClick={() => handleDeleteClick(box)}
                                            >
                                                <DeleteIcon />
                                            </IconButton>
                                        }
                                        disablePadding
                                    >
                                        <ListItemButton component={Link} to={`box/${box.boxId}`}>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <InventoryIcon />
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

            {/* Delete Confirmation Dialog */}
            <Dialog
                open={openDeleteDialog}
                onClose={handleCloseDeleteDialog}
                aria-labelledby="delete-dialog-title"
                aria-describedby="delete-dialog-description"
            >
                <DialogTitle id="delete-dialog-title">
                    Delete Box
                </DialogTitle>
                <DialogContent>
                    <DialogContentText id="delete-dialog-description">
                        Are you sure you want to delete "{boxToDelete?.name}"? 
                        {boxItemCount > 0 && (
                            <> This box contains {boxItemCount} item{boxItemCount !== 1 ? 's' : ''}.</>
                        )}
                        {' '}This action cannot be undone.
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleConfirmDelete} color="primary" autoFocus>
                        Delete
                    </Button>
                    <Button onClick={handleCloseDeleteDialog} color="secondary">
                        Cancel
                    </Button>
                </DialogActions>
            </Dialog>
        </React.Fragment>
    );
}