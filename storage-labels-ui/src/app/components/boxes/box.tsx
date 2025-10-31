import React, { useEffect, useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router';
import {
    Avatar,
    Box,
    Button,
    Checkbox,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Fab,
    FormControl,
    FormControlLabel,
    FormLabel,
    Grid,
    IconButton,
    List,
    ListItem,
    ListItemAvatar,
    ListItemButton,
    ListItemText,
    Menu,
    MenuItem,
    Modal,
    Paper,
    Select,
    Stack,
    Typography,
    useTheme,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import DriveFileMoveIcon from '@mui/icons-material/DriveFileMove';
import LabelIcon from '@mui/icons-material/Label';
import WarehouseIcon from '@mui/icons-material/Warehouse';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSearch } from '../../providers/search-provider';
import { useLocation } from '../../providers/location-provider';
import { AuthenticatedImage, SearchBar, SearchResults, Breadcrumbs, EmptyState, FormattedCode } from '../shared';

type Params = Record<'boxId', string>;

export const BoxComponent: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const { clearSearch } = useSearch();
    const { location } = useLocation();
    const [box, setBox] = useState<Box | null>(null);
    const [items, setItems] = useState<ItemResponse[]>([]);
    const [selectedItem, setSelectedItem] = useState<ItemResponse | null>(null);
    const [itemMenuAnchor, setItemMenuAnchor] = useState<null | HTMLElement>(null);
    const [itemMenuContext, setItemMenuContext] = useState<ItemResponse | null>(null);
    const [itemToDelete, setItemToDelete] = useState<ItemResponse | null>(null);
    const [openModal, setOpenModal] = useState(false);
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const [openDeleteBoxDialog, setOpenDeleteBoxDialog] = useState(false);
    const [openMoveBoxDialog, setOpenMoveBoxDialog] = useState(false);
    const [searchResults, setSearchResults] = useState<SearchResultResponse[]>([]);
    const [searching, setSearching] = useState(false);
    const [boxMenuAnchor, setBoxMenuAnchor] = useState<null | HTMLElement>(null);
    const [forceDelete, setForceDelete] = useState(false);
    const [availableLocations, setAvailableLocations] = useState<StorageLocation[]>([]);
    const [selectedLocationId, setSelectedLocationId] = useState<number | null>(null);
    const theme = useTheme();

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

    const handleDeleteBoxClick = () => {
        setBoxMenuAnchor(null);
        setForceDelete(false); // Reset checkbox
        setOpenDeleteBoxDialog(true);
    };

    const handleCloseDeleteBoxDialog = () => {
        setOpenDeleteBoxDialog(false);
        setForceDelete(false); // Reset checkbox
    };

    const handleConfirmDeleteBox = () => {
        if (box && location) {
            Api.Box.deleteBox(box.boxId, forceDelete)
                .then(() => {
                    // Navigate back to the location
                    navigate(`/locations/${location.locationId}`);
                })
                .catch((error) => alert.addMessage(error.message));
        }
    };

    const handleMoveBoxClick = () => {
        setBoxMenuAnchor(null);
        // Load available locations
        Api.Location.getLocaions()
            .then(({ data }) => {
                // Filter out the current location
                const otherLocations = data.filter(l => l.locationId !== location?.locationId);
                setAvailableLocations(otherLocations);
                setSelectedLocationId(otherLocations.length > 0 ? otherLocations[0].locationId : null);
                setOpenMoveBoxDialog(true);
            })
            .catch((error) => alert.addMessage(error));
    };

    const handleCloseMoveBoxDialog = () => {
        setOpenMoveBoxDialog(false);
        setSelectedLocationId(null);
    };

    const handleConfirmMoveBox = () => {
        if (box && selectedLocationId) {
            Api.Box.moveBox(box.boxId, selectedLocationId)
                .then(() => {
                    // Navigate to the box in its new location
                    navigate(`/locations/${selectedLocationId}/box/${box.boxId}`);
                    handleCloseMoveBoxDialog();
                })
                .catch((error) => {
                    if (error.response?.status === 400) {
                        alert.addMessage("You don't have permission to move boxes to the selected location. You need edit access.");
                    } else {
                        alert.addMessage(error.message);
                    }
                });
        }
    };

    const handleSearch = (query: string) => {
        // Clear results if query is empty
        if (!query || !query.trim()) {
            setSearchResults([]);
            return;
        }
        
        setSearching(true);
        // Search globally across all locations and boxes
        Api.Search.searchBoxesAndItems(query)
            .then(({ data }) => {
                setSearchResults(data.results);
            })
            .catch((error) => alert.addMessage(error))
            .finally(() => setSearching(false));
    };

    const handleQrCodeScan = (code: string) => {
        Api.Search.searchByQrCode(code)
            .then(({ data }) => {
                // Navigate to the box containing the scanned item/box
                if (data.boxId && data.type === 'box') {
                    // Navigating to the found box (could be in any location)
                    navigate(`/locations/${data.locationId}/box/${data.boxId}`);
                } else if (data.boxId && data.type === 'item') {
                    // Navigate to the box containing the item
                    if (data.boxId === params.boxId) {
                        // Item is in current box - find and show it
                        const item = items.find(i => i.itemId === data.itemId);
                        if (item) {
                            handleItemClick(item);
                        }
                    } else {
                        // Navigate to the box containing the item (could be in any location)
                        navigate(`/locations/${data.locationId}/box/${data.boxId}`);
                    }
                }
            })
            .catch((_error) => {
                alert.addMessage(`No box or item found with code: ${code}`);
            });
    };

    const handleSearchResultClick = (result: SearchResultResponse) => {
        setSearchResults([]); // Clear results
        clearSearch(); // Clear search box
        
        if (result.type === 'box' && result.boxId) {
            // Navigate to the found box (could be in any location)
            navigate(`/locations/${result.locationId}/box/${result.boxId}`);
        } else if (result.type === 'item') {
            // Check if item is in current box
            const item = items.find(i => i.itemId === result.itemId);
            if (item) {
                // Item is in current box - show details modal
                handleItemClick(item);
            } else if (result.boxId) {
                // Item is in a different box - navigate to that box
                navigate(`/locations/${result.locationId}/box/${result.boxId}`);
            }
        }
    };

    if (!box) {
        return null;
    }

    const modalStyle = {
        position: 'absolute' as const,
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
            <Box margin={2} mb={2} position="relative">
                {location && box && (
                    <Breadcrumbs 
                        items={[
                            { label: location.name, path: `/locations/${box.locationId}` },
                            { label: box.name }
                        ]}
                    />
                )}
                <SearchBar
                    placeholder="Search all boxes and items..."
                    onSearch={handleSearch}
                    onQrCodeScan={handleQrCodeScan}
                />
                <SearchResults
                    results={searchResults}
                    onResultClick={handleSearchResultClick}
                    loading={searching}
                />
            </Box>

            <Paper>
                <Box position="relative">
                    <Box 
                        margin={1} 
                        textAlign="center"
                        position="relative"
                        sx={{
                            px: { xs: 6, sm: 2 }, // Extra horizontal padding on mobile to avoid menu button overlap
                            pt: { xs: 1.5, sm: 1 } // Slightly more top padding on mobile
                        }}
                    >
                        <IconButton
                            aria-label="box settings"
                            title="Box Settings"
                            onClick={(e) => setBoxMenuAnchor(e.currentTarget)}
                            sx={{
                                position: 'absolute',
                                left: theme.spacing(1),
                                top: theme.spacing(1)
                            }}
                        >
                            <MoreVertIcon />
                        </IconButton>
                        <Typography variant="h4" sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2.125rem' } // Slightly smaller on mobile
                        }}>
                            {box.name}
                        </Typography>
                        {location && (
                            <Box display="flex" alignItems="center" justifyContent="center" gap={1} mt={1}>
                                <WarehouseIcon color="action" />
                                <Typography variant="body1" color="text.secondary">
                                    {location.name}
                                </Typography>
                            </Box>
                        )}
                    </Box>
                    <Box margin={2} pb={2}>
                        <Grid container spacing={2}>
                            <Grid size={{ xs: 12, md: 6 }}>
                                <Typography variant="h6" gutterBottom component="div">
                                    Code: <FormattedCode code={box.code} variant="h6" />
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
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <Fab color="primary" title="Add an Item" aria-label="add" component={Link} to="item/add">
                            <AddIcon />
                        </Fab>
                    </Box>
                    <Box 
                        margin={1} 
                        textAlign="center" 
                        pb={2}
                        sx={{
                            px: { xs: 8, sm: 2 }, // Extra horizontal padding on mobile to avoid FAB overlap
                            pt: { xs: 1.5, sm: 1 } // Slightly more top padding on mobile
                        }}
                    >
                        <Typography variant="h4" sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2.125rem' } // Slightly smaller on mobile
                        }}>
                            Items
                        </Typography>
                    </Box>
                    <Box margin={2}>
                        {items.length === 0 ? (
                            <EmptyState
                                icon={LabelIcon}
                                title="No items in this box"
                                message="Add items to track what's stored in this box. You can add descriptions, images, and other details to help you find things later."
                                actionLabel="Add Item"
                                onAction={() => navigate('item/add')}
                            />
                        ) : (
                            <List>
                                {items.map((item) => (
                                    <ListItem
                                        key={item.itemId}
                                        secondaryAction={
                                            <IconButton 
                                                edge="end" 
                                                aria-label="item menu"
                                                title="Item options"
                                                onClick={(e) => {
                                                    setItemMenuAnchor(e.currentTarget);
                                                    setItemMenuContext(item);
                                                }}
                                            >
                                                <MoreVertIcon />
                                            </IconButton>
                                        }
                                        disablePadding
                                    >
                                        <ListItemButton onClick={() => handleItemClick(item)}>
                                            <ListItemAvatar sx={{ display: { xs: 'none', sm: 'flex' } }}>
                                                <Avatar>
                                                    <LabelIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText
                                                primary={item.name}
                                                secondary={item.description}
                                                slotProps={{
                                                    primary: { noWrap: true }
                                                }}
                                                sx={{ pr: 3, overflow: 'hidden' }}
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
                        Are you sure you want to delete &ldquo;{itemToDelete?.name}&rdquo;? This action cannot be undone.
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

            {/* Item Menu */}
            <Menu
                anchorEl={itemMenuAnchor}
                open={Boolean(itemMenuAnchor)}
                onClose={() => {
                    setItemMenuAnchor(null);
                    setItemMenuContext(null);
                }}
            >
                <MenuItem 
                    component={Link} 
                    to={`item/${itemMenuContext?.itemId}/edit`}
                    onClick={() => {
                        setItemMenuAnchor(null);
                        setItemMenuContext(null);
                    }}
                >
                    <EditIcon sx={{ mr: 1 }} fontSize="small" />
                    Edit
                </MenuItem>
                <MenuItem 
                    onClick={() => {
                        if (itemMenuContext) {
                            handleDeleteClick(itemMenuContext);
                        }
                        setItemMenuAnchor(null);
                        setItemMenuContext(null);
                    }}
                >
                    <DeleteIcon sx={{ mr: 1 }} fontSize="small" />
                    Delete
                </MenuItem>
            </Menu>

            {/* Box Settings Menu */}
            <Menu
                anchorEl={boxMenuAnchor}
                open={Boolean(boxMenuAnchor)}
                onClose={() => setBoxMenuAnchor(null)}
            >
                <MenuItem 
                    component={Link} 
                    to="edit"
                    onClick={() => setBoxMenuAnchor(null)}
                >
                    <EditIcon sx={{ mr: 1 }} fontSize="small" />
                    Edit
                </MenuItem>
                <MenuItem 
                    onClick={handleMoveBoxClick}
                >
                    <DriveFileMoveIcon sx={{ mr: 1 }} fontSize="small" />
                    Move to Location
                </MenuItem>
                <MenuItem 
                    onClick={handleDeleteBoxClick}
                >
                    <DeleteIcon sx={{ mr: 1 }} fontSize="small" />
                    Delete
                </MenuItem>
            </Menu>

            {/* Move Box Dialog */}
            <Dialog
                open={openMoveBoxDialog}
                onClose={handleCloseMoveBoxDialog}
                aria-labelledby="move-box-dialog-title"
            >
                <DialogTitle id="move-box-dialog-title">
                    Move Box to Another Location
                </DialogTitle>
                <DialogContent>
                    <DialogContentText sx={{ mb: 2 }}>
                        Select the location where you want to move &ldquo;{box?.name}&rdquo;.
                    </DialogContentText>
                    {availableLocations.length === 0 ? (
                        <Typography variant="body2" color="text.secondary">
                            No other locations available. Please create another location first.
                        </Typography>
                    ) : (
                        <FormControl fullWidth>
                            <FormLabel>Destination Location</FormLabel>
                            <Select
                                value={selectedLocationId || ''}
                                onChange={(e) => setSelectedLocationId(Number(e.target.value))}
                                variant="standard"
                            >
                                {availableLocations.map((loc) => (
                                    <MenuItem key={loc.locationId} value={loc.locationId}>
                                        {loc.name}
                                    </MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                    )}
                </DialogContent>
                <DialogActions>
                    {availableLocations.length > 0 && (
                        <Button onClick={handleConfirmMoveBox} color="primary" autoFocus>
                            Move
                        </Button>
                    )}
                    <Button onClick={handleCloseMoveBoxDialog} color="secondary">
                        Cancel
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Delete Box Confirmation Dialog */}
            <Dialog
                open={openDeleteBoxDialog}
                onClose={handleCloseDeleteBoxDialog}
                aria-labelledby="delete-box-dialog-title"
                aria-describedby="delete-box-dialog-description"
            >
                <DialogTitle id="delete-box-dialog-title">
                    Delete Box
                </DialogTitle>
                <DialogContent>
                    <DialogContentText id="delete-box-dialog-description">
                        {items.length > 0 ? (
                            <>
                                This box contains {items.length} item{items.length !== 1 ? 's' : ''}.
                                <br /><br />
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={forceDelete}
                                            onChange={(e) => setForceDelete(e.target.checked)}
                                            color="primary"
                                        />
                                    }
                                    label={`Delete all ${items.length} item${items.length !== 1 ? 's' : ''} and the box`}
                                />
                                <br /><br />
                                {forceDelete ? (
                                    <>
                                        Are you sure you want to permanently delete &ldquo;{box?.name}&rdquo; and all {items.length} item{items.length !== 1 ? 's' : ''} inside it? 
                                        This action cannot be undone.
                                    </>
                                ) : (
                                    <>
                                        Please delete all items from this box before deleting the box itself, or check the box above to force delete.
                                    </>
                                )}
                            </>
                        ) : (
                            <>
                                Are you sure you want to delete &ldquo;{box?.name}&rdquo;? This action cannot be undone.
                            </>
                        )}
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    {items.length > 0 && !forceDelete ? (
                        <Button onClick={handleCloseDeleteBoxDialog} color="primary" autoFocus>
                            OK
                        </Button>
                    ) : (
                        <>
                            <Button onClick={handleConfirmDeleteBox} color="primary" autoFocus>
                                Delete
                            </Button>
                            <Button onClick={handleCloseDeleteBoxDialog} color="secondary">
                                Cancel
                            </Button>
                        </>
                    )}
                </DialogActions>
            </Dialog>
        </React.Fragment>
    );
};
