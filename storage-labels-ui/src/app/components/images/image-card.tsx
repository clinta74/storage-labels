import React from 'react';
import {
    Card,
    CardContent,
    CardActions,
    Box,
    Typography,
    Button,
    Chip,
    Stack,
} from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { AuthenticatedImage } from '../shared';

interface ImageCardProps {
    image: ImageMetadataResponse;
    onDelete: (image: ImageMetadataResponse) => void;
}

export const ImageCard: React.FC<ImageCardProps> = ({ image, onDelete }) => {
    const formatFileSize = (bytes: number): string => {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    };

    const formatDate = (dateString: string): string => {
        return new Date(dateString).toLocaleDateString();
    };

    return (
        <Card>
            <Box 
                height={200} 
                display="flex" 
                alignItems="center" 
                justifyContent="center" 
                sx={{ bgcolor: 'action.hover' }}
            >
                <AuthenticatedImage
                    src={image.url}
                    alt={image.fileName}
                    style={{ width: '100%', height: '200px', objectFit: 'contain' }}
                />
            </Box>
            <CardContent>
                <Typography variant="body2" noWrap title={image.fileName}>
                    {image.fileName}
                </Typography>
                <Typography variant="caption" color="text.secondary" display="block">
                    {formatFileSize(image.sizeInBytes)} • {formatDate(image.uploadedAt)}
                </Typography>
                <Stack direction="row" spacing={0.5} mt={1} flexWrap="wrap" gap={0.5}>
                    {image.boxReferenceCount > 0 && (
                        <Chip
                            label={`${image.boxReferenceCount} box${image.boxReferenceCount > 1 ? 'es' : ''}`}
                            size="small"
                            color="primary"
                            variant="outlined"
                        />
                    )}
                    {image.itemReferenceCount > 0 && (
                        <Chip
                            label={`${image.itemReferenceCount} item${image.itemReferenceCount > 1 ? 's' : ''}`}
                            size="small"
                            color="secondary"
                            variant="outlined"
                        />
                    )}
                </Stack>
            </CardContent>
            <CardActions>
                <Button
                    size="small"
                    color="error"
                    startIcon={<DeleteIcon />}
                    onClick={() => onDelete(image)}
                >
                    Delete
                </Button>
            </CardActions>
        </Card>
    );
};
