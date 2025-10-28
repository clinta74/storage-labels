import React, { useState, useEffect } from 'react';
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

interface SearchBarProps {
    placeholder?: string;
    onSearch: (query: string) => void;
    onQrCodeScan: (code: string) => void;
}

export const SearchBar: React.FC<SearchBarProps> = ({ 
    placeholder = "Search boxes and items...", 
    onSearch, 
    onQrCodeScan
}) => {
    const { searchQuery, setSearchQuery } = useSearch();
    const [scannerOpen, setScannerOpen] = useState(false);

    const handleSearchChange = (event: React.ChangeEvent<HTMLInputElement>) => {
        const value = event.target.value;
        setSearchQuery(value);
        
        // Trigger search as user types, or clear results if empty
        if (value.trim()) {
            onSearch(value.trim());
        } else {
            // Clear results when search box is empty
            onSearch('');
        }
    };

    const handleQrScan = (result: string) => {
        setScannerOpen(false);
        onQrCodeScan(result);
    };

    const handleClearSearch = () => {
        setSearchQuery('');
        // Clear results when user clicks clear button
        onSearch('');
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
                                        >
                                            <CloseIcon />
                                        </IconButton>
                                    )}
                                    <IconButton
                                        color="primary"
                                        onClick={() => setScannerOpen(true)}
                                        edge="end"
                                    >
                                        <QrCodeScannerIcon />
                                    </IconButton>
                                </InputAdornment>
                            ),
                        },
                    }}
                />
            </Paper>

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
