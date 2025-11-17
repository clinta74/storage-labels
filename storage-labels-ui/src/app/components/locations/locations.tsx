import { Avatar, Box, Fab, IconButton, List, ListItem, ListItemAvatar, ListItemButton, ListItemText, Menu, MenuItem, Paper, Typography, useTheme } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSearch } from '../../providers/search-provider';
import { useApi } from '../../../api';
import AddIcon from '@mui/icons-material/Add';
import WarehouseIcon from '@mui/icons-material/Warehouse';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import LocationOnIcon from '@mui/icons-material/LocationOn';
import SettingsIcon from '@mui/icons-material/Settings';
import { SearchBar, SearchResults, EmptyState, Breadcrumbs } from '../shared';

export const Locations: React.FC = () => {
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const { Api } = useApi();
    const { clearSearch } = useSearch();
    const [locations, setLocations] = useState<StorageLocation[]>([]);
    const [searchResults, setSearchResults] = useState<SearchResultResponse[]>([]);
    const [searching, setSearching] = useState(false);
    const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);

    const theme = useTheme();

    useEffect(() => {
        Api.Location.getLocaions()
            .then(({ data }) => {
                setLocations(data);
            });
    }, []);

    const handleSearch = (query: string) => {
        // Clear results if query is empty
        if (!query || !query.trim()) {
            setSearchResults([]);
            return;
        }
        
        setSearching(true);
        Api.Search.searchBoxesAndItems(query)
            .then(({ data }) => {
                setSearchResults(data.results);
            })
            .catch((error) => alert.addError(error))
            .finally(() => setSearching(false));
    };

    const handleQrCodeScan = (code: string) => {
        Api.Search.searchByQrCode(code)
            .then(({ data }) => {
                // Navigate directly to the box
                if (data.boxId) {
                    navigate(`${data.locationId}/box/${data.boxId}`);
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
            navigate(`${result.locationId}/box/${result.boxId}`);
        } else if (result.type === 'item' && result.boxId) {
            navigate(`${result.locationId}/box/${result.boxId}`);
        }
    };

    return (
        <React.Fragment>
            <Box margin={2} mb={2}>
                <Breadcrumbs items={[]} />
            </Box>

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

            <Box position="relative" margin={2}>
                <Box position="absolute" right={theme.spacing(1)} top={theme.spacing(1)} sx={{ zIndex: 1 }}>
                    <Fab color="primary" title="Add a Location" aria-label="add" component={Link} to={`add`}>
                        <AddIcon />
                    </Fab>
                </Box>
                <Paper>
                    <Box position="relative">
                        <Box 
                            margin={1} 
                            textAlign="center"
                            position="relative"
                            pb={2}
                            sx={{
                                px: { xs: 6, sm: 2 }, // Extra horizontal padding on mobile to avoid menu button overlap
                                pt: { xs: 1.5, sm: 1 } // Slightly more top padding on mobile
                            }}
                        >
                            <IconButton
                                aria-label="locations settings"
                                title="Locations Settings"
                                onClick={(e) => setMenuAnchor(e.currentTarget)}
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
                                Your Locations
                            </Typography>
                        </Box>

                        <Menu
                            anchorEl={menuAnchor}
                            open={Boolean(menuAnchor)}
                            onClose={() => setMenuAnchor(null)}
                        >
                            <MenuItem onClick={() => {
                                setMenuAnchor(null);
                                navigate('/common-locations');
                            }}>
                                <LocationOnIcon sx={{ mr: 1 }} fontSize="small" />
                                Common Locations
                            </MenuItem>
                            <MenuItem onClick={() => {
                                setMenuAnchor(null);
                                navigate('/preferences');
                            }}>
                                <SettingsIcon sx={{ mr: 1 }} fontSize="small" />
                                Preferences
                            </MenuItem>
                        </Menu>
                    </Box>

                    <Box margin={2}>
                        {locations.length === 0 ? (
                            <EmptyState
                                icon={WarehouseIcon}
                                title="No locations yet"
                                message="Create your first storage location to start organizing your items. Locations can be rooms, buildings, or any physical space where you store boxes."
                                actionLabel="Add Location"
                                onAction={() => navigate('add')}
                            />
                        ) : (
                            <List>
                                {
                                    locations.map(location =>
                                        <ListItem 
                                            key={location.locationId}
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
                        )}
                    </Box>
                </Paper>
            </Box>
        </React.Fragment>
    );
}