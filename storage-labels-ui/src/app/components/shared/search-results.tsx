import React from 'react';
import {
    List,
    ListItem,
    ListItemButton,
    ListItemText,
    ListItemAvatar,
    Avatar,
    Paper,
    Typography,
    Chip,
    Box,
    Pagination,
    Stack,
    Rating,
} from '@mui/material';
import InventoryIcon from '@mui/icons-material/Inventory';
import LabelIcon from '@mui/icons-material/Label';

interface SearchResultsProps {
    results: SearchResultV2[];
    onResultClick: (result: SearchResultV2) => void;
    loading?: boolean;
    // v2 Pagination props
    currentPage?: number;
    totalPages?: number;
    totalResults?: number;
    onPageChange?: (page: number) => void;
    showRelevance?: boolean;
}

export const SearchResults: React.FC<SearchResultsProps> = ({ 
    results, 
    onResultClick, 
    loading,
    currentPage = 1,
    totalPages = 1,
    totalResults = 0,
    onPageChange,
    showRelevance = true
}) => {
    if (loading) {
        return (
            <Paper elevation={2} sx={{ p: 2 }}>
                <Typography variant="body2" color="text.secondary">
                    Searching...
                </Typography>
            </Paper>
        );
    }

    if (results.length === 0) {
        return null;
    }

    // Normalize rank to 0-5 scale for visual display (typical rank values are 0-1)
    const normalizeRank = (rank: number): number => {
        return Math.min(5, Math.max(0, rank * 5));
    };

    return (
        <Paper 
            elevation={8} 
            sx={{ 
                position: 'absolute',
                top: '100%',
                left: 0,
                right: 0,
                mt: 1,
                zIndex: 1200,
                maxHeight: '500px',
                overflow: 'auto',
            }}
        >
            {totalResults > 0 && (
                <Box sx={{ p: 2, pb: 1, borderBottom: 1, borderColor: 'divider' }}>
                    <Typography variant="caption" color="text.secondary">
                        {totalResults} result{totalResults !== 1 ? 's' : ''} found
                        {totalPages > 1 && ` • Page ${currentPage} of ${totalPages}`}
                    </Typography>
                </Box>
            )}
            
            <List>
                {results.map((result, index) => (
                    <ListItem
                        key={`${result.type}-${result.boxId || result.itemId}-${index}`}
                        disablePadding
                        divider={index < results.length - 1}
                    >
                        <ListItemButton onClick={() => onResultClick(result)}>
                            <ListItemAvatar>
                                <Avatar>
                                    {result.type === 'box' ? <InventoryIcon /> : <LabelIcon />}
                                </Avatar>
                            </ListItemAvatar>
                            <ListItemText
                                primary={
                                    <Box display="flex" alignItems="center" gap={1}>
                                        {result.type === 'box' ? result.boxName : result.itemName}
                                        <Chip
                                            label={result.type}
                                            size="small"
                                            color={result.type === 'box' ? 'primary' : 'secondary'}
                                        />
                                        {showRelevance && result.rank > 0 && (
                                            <Box sx={{ ml: 'auto' }}>
                                                <Rating 
                                                    value={normalizeRank(result.rank)} 
                                                    precision={0.5} 
                                                    size="small" 
                                                    readOnly 
                                                    max={5}
                                                />
                                            </Box>
                                        )}
                                    </Box>
                                }
                                secondary={
                                    <>
                                        {result.type === 'box' && result.boxCode && (
                                            <Typography variant="caption" component="span" display="block">
                                                Code: {result.boxCode}
                                            </Typography>
                                        )}
                                        {result.type === 'item' && (
                                            <Typography variant="caption" component="span" display="block">
                                                In box: {result.boxName} ({result.boxCode})
                                            </Typography>
                                        )}
                                        <Typography variant="caption" component="span" color="text.secondary">
                                            Location: {result.locationName}
                                        </Typography>
                                    </>
                                }
                                slotProps={{
                                    primary: { noWrap: true },
                                }}
                                sx={{ pr: 2 }}
                            />
                        </ListItemButton>
                    </ListItem>
                ))}
            </List>

            {totalPages > 1 && onPageChange && (
                <Box sx={{ p: 2, pt: 1, display: 'flex', justifyContent: 'center', borderTop: 1, borderColor: 'divider' }}>
                    <Stack spacing={2}>
                        <Pagination 
                            count={totalPages} 
                            page={currentPage} 
                            onChange={(_, page) => onPageChange(page)}
                            color="primary"
                            size="small"
                            showFirstButton
                            showLastButton
                        />
                    </Stack>
                </Box>
            )}
        </Paper>
    );
};
