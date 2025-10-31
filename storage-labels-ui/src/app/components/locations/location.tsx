import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSearch } from '../../providers/search-provider';
import { useLocation } from '../../providers/location-provider';
import { useApi } from '../../../api';
import { 
    Avatar, 
    Badge,
    Box, 
    Button,
    Checkbox,
    Dialog,
    DialogActions,
    DialogContent,
    DialogContentText,
    DialogTitle,
    Fab, 
    FormControlLabel,
    IconButton,
    Menu,
    MenuItem,
    List, 
    ListItem, 
    ListItemAvatar, 
    ListItemButton, 
    ListItemText, 
    Paper, 
    Typography,
    useTheme
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import EditIcon from '@mui/icons-material/Edit';
import PeopleIcon from '@mui/icons-material/People';
import InventoryIcon from '@mui/icons-material/Inventory';
import { SearchBar, SearchResults, Breadcrumbs, EmptyState } from '../shared';

export const Location: React.FC = () => {
    const params = useParams();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const { clearSearch } = useSearch();
    const { location } = useLocation();
    const [boxes, setBoxes] = useState<Box[]>([]);
    const [boxItemCounts, setBoxItemCounts] = useState<Record<string, number>>({});
    const [boxToDelete, setBoxToDelete] = useState<Box | null>(null);
    const [boxItemCount, setBoxItemCount] = useState<number>(0);
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const [openDeleteLocationDialog, setOpenDeleteLocationDialog] = useState(false);
    const [searchResults, setSearchResults] = useState<SearchResultResponse[]>([]);
    const [searching, setSearching] = useState(false);
    const [settingsMenuAnchor, setSettingsMenuAnchor] = useState<null | HTMLElement>(null);
    const [forceDelete, setForceDelete] = useState(false);

    const theme = useTheme();

    useEffect(() => {
        const locationId = Number(params.locationId);

        if (locationId) {
            Api.Box.getBoxes(locationId)
                .then(({ data }) => {
                    setBoxes(data);
                    
                    // Fetch item counts for all boxes
                    data.forEach(box => {
                        Api.Item.getItemsByBoxId(box.boxId)
                            .then(({ data: items }) => {
                                setBoxItemCounts(prev => ({
                                    ...prev,
                                    [box.boxId]: items.length
                                }));
                            })
                            .catch(error => console.error('Error fetching item count:', error));
                    });
                });
        }
    }, [params]);

    
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

    const handleDeleteLocationClick = () => {
        setSettingsMenuAnchor(null);
        setForceDelete(false); // Reset checkbox
        setOpenDeleteLocationDialog(true);
    };

    const handleCloseDeleteLocationDialog = () => {
        setOpenDeleteLocationDialog(false);
        setForceDelete(false); // Reset checkbox
    };

    const handleConfirmDeleteLocation = () => {
        if (location) {
            Api.Location.deleteLocation(location.locationId, forceDelete)
                .then(() => {
                    // Navigate back to locations list
                    navigate('/locations');
                })
                .catch((error) => alert.addMessage(error.message));
        }
    };

    const handleSearch = (query: string) => {
        // Clear results if query is empty
        if (!query || !query.trim()) {
            setSearchResults([]);
            return;
        }
        
        setSearching(true);
        // Search globally, not just in this location
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
                // Navigate directly to the box (could be in any location)
                if (data.boxId) {
                    navigate(`/locations/${data.locationId}/box/${data.boxId}`);
                }
            })
            .catch((_error) => {
                alert.addMessage(`No box found with code: ${code}`);
            });
    };

    const handleSearchResultClick = (result: SearchResultResponse) => {
        setSearchResults([]); // Clear results
        clearSearch(); // Clear search box
        
        if (result.type === 'box' && result.boxId) {
            // Navigate to the found box (could be in any location)
            navigate(`/locations/${result.locationId}/box/${result.boxId}`);
        } else if (result.type === 'item' && result.boxId) {
            // Navigate to the box containing the item (could be in any location)
            navigate(`/locations/${result.locationId}/box/${result.boxId}`);
        }
    };

    return (
        <React.Fragment>
            <Box margin={2} mb={2} position="relative">
                {location && (
                    <Breadcrumbs 
                        items={[
                            { label: location.name }
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
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <Fab color="primary" title="Add a Box" aria-label="add" component={Link} to={`box/add`}>
                            <AddIcon />
                        </Fab>
                    </Box>
                    <Box 
                        margin={1} 
                        textAlign="center" 
                        pb={2}
                        position="relative"
                        sx={{
                            px: { xs: 8, sm: 2 }, // Extra horizontal padding on mobile to avoid FAB overlap
                            pt: { xs: 1.5, sm: 1 } // Slightly more top padding on mobile
                        }}
                    >
                        <IconButton
                            aria-label="location settings"
                            title="Location Settings"
                            onClick={(e) => setSettingsMenuAnchor(e.currentTarget)}
                            sx={{
                                position: 'absolute',
                                left: theme.spacing(1),
                                top: theme.spacing(1)
                            }}
                        >
                            <MoreVertIcon />
                        </IconButton>
                        <Typography variant='h4' sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2.125rem' } // Slightly smaller on mobile
                        }}>
                            {location?.name}
                        </Typography>
                    </Box>
                    <Box margin={2}>
                        {boxes.length === 0 ? (
                            <EmptyState
                                icon={InventoryIcon}
                                title="No boxes in this location"
                                message="Add your first box to start tracking items in this location. Each box can contain multiple items and has a unique QR code for quick access."
                                actionLabel="Add Box"
                                onAction={() => navigate('box/add')}
                            />
                        ) : (
                            <List>
                                {
                                    boxes.map(box =>
                                        <ListItem key={box.boxId}>
                                            <ListItemButton component={Link} to={`box/${box.boxId}`}>
                                                <ListItemAvatar>
                                                    <Badge 
                                                        badgeContent={boxItemCounts[box.boxId] || 0} 
                                                        color="primary"
                                                        max={999}
                                                    >
                                                        <Avatar>
                                                            <InventoryIcon />
                                                        </Avatar>
                                                    </Badge>
                                                </ListItemAvatar>
                                                <ListItemText primary={box.name} secondary={box.description} />
                                            </ListItemButton>
                                        </ListItem>
                                    )
                                }
                            </List>
                        )}
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
                        Are you sure you want to delete &ldquo;{boxToDelete?.name}&rdquo;? 
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

            {/* Settings Menu */}
            <Menu
                anchorEl={settingsMenuAnchor}
                open={Boolean(settingsMenuAnchor)}
                onClose={() => setSettingsMenuAnchor(null)}
            >
                <MenuItem 
                    component={Link} 
                    to="edit"
                    onClick={() => setSettingsMenuAnchor(null)}
                >
                    <EditIcon sx={{ mr: 1 }} fontSize="small" />
                    Edit Name
                </MenuItem>
                <MenuItem 
                    component={Link} 
                    to="users"
                    onClick={() => setSettingsMenuAnchor(null)}
                >
                    <PeopleIcon sx={{ mr: 1 }} fontSize="small" />
                    Manage Users
                </MenuItem>
                <MenuItem 
                    onClick={handleDeleteLocationClick}
                >
                    <DeleteIcon sx={{ mr: 1 }} fontSize="small" />
                    Delete Location
                </MenuItem>
            </Menu>

            {/* Delete Location Confirmation Dialog */}
            <Dialog
                open={openDeleteLocationDialog}
                onClose={handleCloseDeleteLocationDialog}
                aria-labelledby="delete-location-dialog-title"
                aria-describedby="delete-location-dialog-description"
            >
                <DialogTitle id="delete-location-dialog-title">
                    Delete Location
                </DialogTitle>
                <DialogContent>
                    <DialogContentText id="delete-location-dialog-description">
                        {boxes.length > 0 ? (
                            <>
                                This location contains {boxes.length} box{boxes.length !== 1 ? 'es' : ''}.
                                <br /><br />
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={forceDelete}
                                            onChange={(e) => setForceDelete(e.target.checked)}
                                            color="primary"
                                        />
                                    }
                                    label={`Delete all ${boxes.length} box${boxes.length !== 1 ? 'es' : ''} and their items`}
                                />
                                <br /><br />
                                {forceDelete ? (
                                    <>
                                        Are you sure you want to permanently delete &ldquo;{location?.name}&rdquo; and all {boxes.length} box{boxes.length !== 1 ? 'es' : ''} with their items? 
                                        This action cannot be undone.
                                    </>
                                ) : (
                                    <>
                                        Please delete all boxes from this location before deleting the location itself, or check the box above to force delete.
                                    </>
                                )}
                            </>
                        ) : (
                            <>
                                Are you sure you want to delete &ldquo;{location?.name}&rdquo;? This action cannot be undone.
                            </>
                        )}
                    </DialogContentText>
                </DialogContent>
                <DialogActions>
                    {boxes.length > 0 && !forceDelete ? (
                        <Button onClick={handleCloseDeleteLocationDialog} color="primary" autoFocus>
                            OK
                        </Button>
                    ) : (
                        <>
                            <Button onClick={handleConfirmDeleteLocation} color="primary" autoFocus>
                                Delete
                            </Button>
                            <Button onClick={handleCloseDeleteLocationDialog} color="secondary">
                                Cancel
                            </Button>
                        </>
                    )}
                </DialogActions>
            </Dialog>
        </React.Fragment>
    );
}