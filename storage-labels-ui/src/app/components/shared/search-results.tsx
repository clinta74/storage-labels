import React, { useRef, useEffect } from 'react';
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
    Button,
    Pagination,
    Stack,
    Rating,
    CircularProgress,
} from '@mui/material';
import InventoryIcon from '@mui/icons-material/Inventory';
import LabelIcon from '@mui/icons-material/Label';

interface SearchResultsProps {
    results: SearchResultResponse[];
    onResultClick: (result: SearchResultResponse) => void;
    loading?: boolean;
    loadingMore?: boolean;
    onLoadMore?: () => void;
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
    loadingMore = false,
    onLoadMore,
    currentPage = 1,
    totalPages = 1,
    totalResults = 0,
    onPageChange,
    showRelevance = true
}) => {
    const listRef = useRef<HTMLDivElement>(null);
    const observerRef = useRef<IntersectionObserver | null>(null);
    const loadMoreTriggerRef = useRef<HTMLDivElement>(null);

    // Calculate hasMoreResults based on current props
    const hasMoreResults = currentPage < totalPages;

    // Set up IntersectionObserver for infinite scroll
    useEffect(() => {
        if (!onLoadMore || !hasMoreResults || loadingMore) {
            return;
        }

        // Wait for the listRef to be available
        if (!listRef.current) {
            return;
        }

        const options = {
            root: listRef.current, // Use the Paper element as the scroll container
            rootMargin: '0px',
            threshold: 0.1
        };

        observerRef.current = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    onLoadMore();
                }
            });
        }, options);

        const trigger = loadMoreTriggerRef.current;
        if (trigger) {
            observerRef.current.observe(trigger);
        }

        return () => {
            if (observerRef.current) {
                observerRef.current.disconnect();
            }
        };
    }, [onLoadMore, hasMoreResults, loadingMore, results.length]);

    if (loading && results.length === 0) {
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
            ref={listRef}
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
                display: 'flex',
                flexDirection: 'column',
            }}
        >
            {/* Sticky header with result count */}
            {totalResults > 0 && (
                <Box 
                    sx={{ 
                        p: 2, 
                        pb: 1, 
                        borderBottom: 1, 
                        borderColor: 'divider',
                        position: 'sticky',
                        top: 0,
                        bgcolor: 'background.paper',
                        zIndex: 1,
                    }}
                >
                    <Typography variant="caption" color="text.secondary">
                        {totalResults} result{totalResults !== 1 ? 's' : ''} found
                        {results.length < totalResults && ` • Showing ${results.length}`}
                    </Typography>
                </Box>
            )}
            
            <List sx={{ flexGrow: 1, overflow: 'visible' }}>
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
            
            {/* Load More button and loading indicator */}
            {hasMoreResults && (
                <Box sx={{ p: 2, borderTop: 1, borderColor: 'divider', textAlign: 'center', bgcolor: 'background.paper' }}>
                    {loadingMore ? (
                        <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 1 }}>
                            <CircularProgress size={24} />
                            <Typography variant="caption" color="text.secondary">
                                Loading more results...
                            </Typography>
                        </Box>
                    ) : (
                        <>
                            <Button 
                                variant="outlined" 
                                size="small" 
                                onClick={onLoadMore}
                                fullWidth
                            >
                                Load More Results ({results.length} of {totalResults})
                            </Button>
                            {/* Invisible trigger for IntersectionObserver */}
                            <Box ref={loadMoreTriggerRef} sx={{ height: '1px', width: '100%', visibility: 'hidden' }} />
                        </>
                    )}
                </Box>
            )}
        </Paper>
    );
};
