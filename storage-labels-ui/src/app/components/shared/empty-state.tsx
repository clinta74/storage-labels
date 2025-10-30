import React from 'react';
import { Box, Typography, Button } from '@mui/material';
import { SvgIconComponent } from '@mui/icons-material';

interface EmptyStateProps {
    icon: SvgIconComponent;
    title: string;
    message: string;
    actionLabel?: string;
    onAction?: () => void;
}

export const EmptyState: React.FC<EmptyStateProps> = ({ 
    icon: Icon, 
    title, 
    message, 
    actionLabel, 
    onAction 
}) => {
    return (
        <Box 
            sx={{ 
                textAlign: 'center', 
                py: 6,
                px: 2,
            }}
        >
            <Icon 
                sx={{ 
                    fontSize: 80, 
                    color: 'text.disabled',
                    mb: 2,
                    opacity: 0.5,
                }} 
            />
            <Typography 
                variant="h6" 
                color="text.secondary"
                gutterBottom
                sx={{ fontWeight: 500 }}
            >
                {title}
            </Typography>
            <Typography 
                variant="body2" 
                color="text.secondary" 
                sx={{ mb: 3, maxWidth: 400, mx: 'auto' }}
            >
                {message}
            </Typography>
            {actionLabel && onAction && (
                <Button 
                    variant="contained" 
                    onClick={onAction}
                    size="large"
                >
                    {actionLabel}
                </Button>
            )}
        </Box>
    );
};
