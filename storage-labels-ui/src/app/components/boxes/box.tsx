import React, { useEffect, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router';
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
    Grid,
    IconButton,
    List,
    ListItem,
    ListItemAvatar,
    ListItemButton,
    ListItemText,
    Modal,
    Paper,
    Stack,
    Typography,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import NavigateBeforeIcon from '@mui/icons-material/NavigateBefore';
import DeleteIcon from '@mui/icons-material/Delete';
import InventoryIcon from '@mui/icons-material/Inventory';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { AuthenticatedImage } from '../shared';

type Params = Record<'boxId', string>;

export const BoxComponent: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [box, setBox] = useState<Box | null>(null);
    const [items, setItems] = useState<ItemResponse[]>([]);
    const [selectedItem, setSelectedItem] = useState<ItemResponse | null>(null);
    const [itemToDelete, setItemToDelete] = useState<ItemResponse | null>(null);
    const [openModal, setOpenModal] = useState(false);
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const theme = createTheme();

    useEffect(() => {
        const boxId = params.boxId;
        if (boxId) {
            Api.Box.getBox(boxId)
                .then(({ data }) => {
                    setBox(data);
                })
                .catch((error) => alert.addMessage(error));

            Api.Item.getItemsByBoxId(boxId)
                .then(({ data }) => {
                    setItems(data);
                })
                .catch((error) => alert.addMessage(error));
        }
    }, [params]);

    const handleItemClick = (item: ItemResponse) => {
        setSelectedItem(item);
        setOpenModal(true);
    };

    const handleCloseModal = () => {
        setOpenModal(false);
        setSelectedItem(null);
    };

    const handleDeleteClick = (item: ItemResponse) => {
        setItemToDelete(item);
        setOpenDeleteDialog(true);
    };

    const handleCloseDeleteDialog = () => {
        setOpenDeleteDialog(false);
        setItemToDelete(null);
    };

    const handleConfirmDelete = () => {
        if (itemToDelete && params.boxId) {
            Api.Item.deleteItem(itemToDelete.itemId)
                .then(() => {
                    // Refresh the items list
                    Api.Item.getItemsByBoxId(params.boxId!)
                        .then(({ data }) => {
                            setItems(data);
                        })
                        .catch((error) => alert.addMessage(error));
                    handleCloseDeleteDialog();
                })
                .catch((error) => alert.addMessage(error.message));
        }
    };

    if (!box) {
        return null;
    }

    const modalStyle = {
        position: 'absolute' as 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: { xs: '90%', sm: 600 },
        maxHeight: '90vh',
        overflow: 'auto',
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
    };

    return (
        <React.Fragment>
            <Paper>
                <Box position="relative">
                    <Box position="absolute" left={theme.spacing(1)} top={theme.spacing(1)}>
                        <IconButton edge="end" aria-label="back" component={Link} to={`/locations/${box.locationId}`}>
                            <NavigateBeforeIcon />
                        </IconButton>
                    </Box>
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)}>
                        <IconButton aria-label="edit" component={Link} to="edit">
                            <EditIcon />
                        </IconButton>
                    </Box>
                    <Box margin={1} textAlign="center">
                        <Typography variant="h4">{box.name}</Typography>
                    </Box>
                    <Box margin={2} pb={2}>
                        <Grid container spacing={2}>
                            <Grid size={{ xs: 12, md: 6 }}>
                                <Typography variant="h6" gutterBottom>
                                    Code: <Typography component="span" variant="h6" fontWeight="bold">{box.code}</Typography>
                                </Typography>
                                <Typography variant="body1">{box.description || 'No description provided.'}</Typography>
                            </Grid>
                            {box.imageUrl && (
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Box display="flex" justifyContent="center">
                                        <AuthenticatedImage
                                            src={box.imageUrl}
                                            alt={box.name}
                                            style={{ maxWidth: '100%', maxHeight: 300, objectFit: 'contain' }}
                                        />
                                    </Box>
                                </Grid>
                            )}
                        </Grid>
                    </Box>
                </Box>
            </Paper>

            <Paper sx={{ mt: 2 }}>
                <Box position="relative">
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)}>
                        <Fab color="primary" title="Add an Item" aria-label="add" component={Link} to="item/add">
                            <AddIcon />
                        </Fab>
                    </Box>
                    <Box margin={1} textAlign="center" pb={2}>
                        <Typography variant="h4">Items</Typography>
                    </Box>
                    <Box margin={2}>
                        {items.length === 0 ? (
                            <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                                No items in this box yet.
                            </Typography>
                        ) : (
                            <List>
                                {items.map((item) => (
                                    <ListItem
                                        key={item.itemId}
                                        secondaryAction={
                                            <Stack direction="row" spacing={1}>
                                                <IconButton 
                                                    edge="end" 
                                                    aria-label="edit"
                                                    component={Link}
                                                    to={`item/${item.itemId}/edit`}
                                                >
                                                    <EditIcon />
                                                </IconButton>
                                                <IconButton 
                                                    edge="end" 
                                                    aria-label="delete"
                                                    onClick={() => handleDeleteClick(item)}
                                                >
                                                    <DeleteIcon />
                                                </IconButton>
                                            </Stack>
                                        }
                                        disablePadding
                                    >
                                        <ListItemButton onClick={() => handleItemClick(item)}>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <InventoryIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText
                                                primary={item.name}
                                                secondary={item.description}
                                                slotProps={{
                                                    primary: { noWrap: true }
                                                }}
                                                sx={{ pr: 2, overflow: 'hidden' }}
                                            />
                                        </ListItemButton>
                                    </ListItem>
                                ))}
                            </List>
                        )}
                    </Box>
                </Box>
            </Paper>

            {/* Item Detail Modal */}
            <Modal
                open={openModal}
                onClose={handleCloseModal}
                aria-labelledby="item-modal-title"
            >
                <Box sx={modalStyle}>
                    {selectedItem && (
                        <Stack spacing={2}>
                            <Typography id="item-modal-title" variant="h5" component="h2">
                                {selectedItem.name}
                            </Typography>
                            {selectedItem.description && (
                                <Typography variant="body1">
                                    {selectedItem.description}
                                </Typography>
                            )}
                            {selectedItem.imageUrl ? (
                                <Box display="flex" justifyContent="center">
                                    <AuthenticatedImage
                                        src={selectedItem.imageUrl}
                                        alt={selectedItem.name}
                                        style={{ maxWidth: '100%', maxHeight: 400, objectFit: 'contain' }}
                                    />
                                </Box>
                            ) : (
                                <Box display="flex" justifyContent="center" py={4}>
                                    <Typography variant="body2" color="text.secondary">
                                        No image available
                                    </Typography>
                                </Box>
                            )}
                            <Box display="flex" justifyContent="flex-end">
                                <Button onClick={handleCloseModal} color="secondary">
                                    Close
                                </Button>
                            </Box>
                        </Stack>
                    )}
                </Box>
            </Modal>

            {/* Delete Confirmation Dialog */}
            <Dialog
                open={openDeleteDialog}
                onClose={handleCloseDeleteDialog}
                aria-labelledby="delete-dialog-title"
                aria-describedby="delete-dialog-description"
            >
                <DialogTitle id="delete-dialog-title">
                    Delete Item
                </DialogTitle>
                <DialogContent>
                    <DialogContentText id="delete-dialog-description">
                        Are you sure you want to delete "{itemToDelete?.name}"? This action cannot be undone.
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
};
