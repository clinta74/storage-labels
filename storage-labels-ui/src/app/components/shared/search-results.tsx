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
} from '@mui/material';
import InventoryIcon from '@mui/icons-material/Inventory';
import LabelIcon from '@mui/icons-material/Label';

interface SearchResultsProps {
    results: SearchResultResponse[];
    onResultClick: (result: SearchResultResponse) => void;
    loading?: boolean;
}

export const SearchResults: React.FC<SearchResultsProps> = ({ results, onResultClick, loading }) => {
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
                maxHeight: '400px',
                overflow: 'auto',
            }}
        >
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
        </Paper>
    );
};
