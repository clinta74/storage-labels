import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSearch } from '../../providers/search-provider';
import { useLocation } from '../../providers/location-provider';
import { useApi } from '../../../api';
import { 
    Avatar, 
    Box, 
    Button,
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
    Typography,
    useTheme
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import NavigateBeforeIcon from '@mui/icons-material/NavigateBefore';
import DeleteIcon from '@mui/icons-material/Delete';
import InventoryIcon from '@mui/icons-material/Inventory';
import { SearchBar, SearchResults } from '../shared';

export const Location: React.FC = () => {
    const params = useParams();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const { clearSearch } = useSearch();
    const { location } = useLocation();
    const [boxes, setBoxes] = useState<Box[]>([]);
    const [boxToDelete, setBoxToDelete] = useState<Box | null>(null);
    const [boxItemCount, setBoxItemCount] = useState<number>(0);
    const [openDeleteDialog, setOpenDeleteDialog] = useState(false);
    const [searchResults, setSearchResults] = useState<SearchResultResponse[]>([]);
    const [searching, setSearching] = useState(false);

    const theme = useTheme();

    useEffect(() => {
        const locationId = Number(params.locationId);

        if (locationId) {
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
            .catch((error) => {
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
                    <Box position="absolute" left={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <IconButton edge="end" aria-label="back" component={Link} to={`../`}>
                            <NavigateBeforeIcon />
                        </IconButton>
                    </Box>
                    <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <Fab color="primary" title="Add a Box" aria-label="add" component={Link} to={`box/add`}>
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
                        <Typography variant='h4' sx={{ 
                            fontSize: { xs: '1.75rem', sm: '2.125rem' } // Slightly smaller on mobile
                        }}>
                            {location?.name}
                        </Typography>
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