import React, { useState } from 'react';
import {
    Box,
    IconButton,
    InputAdornment,
    Paper,
    TextField,
    Dialog,
    DialogContent,
    DialogTitle,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import CloseIcon from '@mui/icons-material/Close';
import { Scanner } from '@yudiel/react-qr-scanner';
import { useSearch } from '../../providers/search-provider';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { SearchResults } from './search-results';
import { useNavigate } from 'react-router';

interface SearchBarProps {
    placeholder?: string;
    onQrCodeScan?: (code: string) => void;
}

export const SearchBar: React.FC<SearchBarProps> = ({ 
    placeholder = "Search boxes and items...",
    onQrCodeScan
}) => {
    const { 
        searchQuery, 
        setSearchQuery,
        currentPage,
        pageSize,
        setCurrentPage,
        setPaginationInfo,
        totalPages,
        totalResults,
        accumulatedResults,
        appendResults,
        resetAccumulatedResults,
        clearSearch
    } = useSearch();
    const { Api } = useApi();
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const [scannerOpen, setScannerOpen] = useState(false);
    const [searching, setSearching] = useState(false);
    const [loadingMore, setLoadingMore] = useState(false);

    const handleSearch = (query: string, page: number = 1) => {
        // Clear accumulated results if it's a new search (page 1)
        if (page === 1) {
            resetAccumulatedResults();
            setSearching(true);
        } else {
            setLoadingMore(true);
        }
        
        // Clear results if query is empty
        if (!query || !query.trim()) {
            resetAccumulatedResults();
            setPaginationInfo(0, 0);
            setSearching(false);
            setLoadingMore(false);
            return;
        }
        
        // Search globally across all locations and boxes
        Api.Search.searchBoxesAndItems(query, undefined, undefined, page, pageSize)
            .then(({ data, totalCount, totalPages }) => {
                if (page === 1) {
                    // First page: reset accumulated results
                    resetAccumulatedResults();
                    appendResults(data);
                } else {
                    // Subsequent pages: append to accumulated results
                    appendResults(data);
                }
                setPaginationInfo(totalCount, totalPages);
                setCurrentPage(page);
            })
            .catch((error) => alert.addError(error))
            .finally(() => {
                setSearching(false);
                setLoadingMore(false);
            });
    };

    const handleLoadMore = () => {
        if (loadingMore || searching || currentPage >= totalPages) {
            return;
        }
        const nextPage = currentPage + 1;
        handleSearch(searchQuery, nextPage);
    };

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const value = event.target.value;
        setSearchQuery(value);
        
        // Trigger search as user types, or clear results if empty
        if (value.trim()) {
            handleSearch(value.trim());
        } else {
            // Clear results when search box is empty
            handleSearch('');
        }
    };

    const handleSearchResultClick = (result: SearchResultResponse) => {
        resetAccumulatedResults(); // Clear results
        clearSearch(); // Clear search box
        
        if (result.type === 'box' && result.boxId) {
            navigate(`/locations/${result.locationId}/box/${result.boxId}`);
        } else if (result.type === 'item' && result.boxId) {
            navigate(`/locations/${result.locationId}/box/${result.boxId}`);
        }
    };

    const handleQrScan = (result: string) => {
        setScannerOpen(false);
        if (onQrCodeScan) {
            onQrCodeScan(result);
        }
    };

    const handleClearSearch = () => {
        setSearchQuery('');
        resetAccumulatedResults();
        setPaginationInfo(0, 0);
    };

    return (
        <Box mb={2} position="relative">
            <Paper elevation={1}>
                <TextField
                    fullWidth
                    placeholder={placeholder}
                    value={searchQuery}
                    onChange={handleSearchChange}
                    slotProps={{
                        input: {
                            startAdornment: (
                                <InputAdornment position="start">
                                    <SearchIcon />
                                </InputAdornment>
                            ),
                            endAdornment: (
                                <InputAdornment position="end">
                                    {searchQuery && (
                                        <IconButton
                                            size="small"
                                            onClick={handleClearSearch}
                                            edge="end"
                                            sx={{ mr: 0.5 }}
                                            aria-label="clear search"
                                            title="Clear search"
                                        >
                                            <CloseIcon />
                                        </IconButton>
                                    )}
                                    <IconButton
                                        color="primary"
                                        onClick={() => setScannerOpen(true)}
                                        edge="end"
                                        aria-label="scan QR code"
                                        title="Scan QR code"
                                    >
                                        <QrCodeScannerIcon />
                                    </IconButton>
                                </InputAdornment>
                            ),
                        },
                    }}
                />
            </Paper>

            <SearchResults
                results={accumulatedResults}
                onResultClick={handleSearchResultClick}
                loading={searching}
                loadingMore={loadingMore}
                onLoadMore={handleLoadMore}
                currentPage={currentPage}
                totalPages={totalPages}
                totalResults={totalResults}
                showRelevance={true}
            />

            <Dialog
                open={scannerOpen}
                onClose={() => setScannerOpen(false)}
                maxWidth="sm"
                fullWidth
            >
                <DialogTitle>
                    Scan QR Code
                    <IconButton
                        onClick={() => setScannerOpen(false)}
                        sx={{
                            position: 'absolute',
                            right: 8,
                            top: 8,
                        }}
                        aria-label="close scanner"
                        title="Close scanner"
                    >
                        <CloseIcon />
                    </IconButton>
                </DialogTitle>
                <DialogContent>
                    <Scanner
                        onScan={(detectedCodes) => {
                            const code = detectedCodes[0]?.rawValue;
                            if (code) {
                                handleQrScan(code);
                            }
                        }}
                        constraints={{
                            facingMode: 'environment'
                        }}
                    />
                </DialogContent>
            </Dialog>
        </Box>
    );
};
