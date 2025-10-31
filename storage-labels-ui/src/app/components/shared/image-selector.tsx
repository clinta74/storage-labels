import React, { useState, useEffect } from 'react';
import {
    Box,
    Typography,
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Grid,
    Card,
    CardActionArea,
    RadioGroup,
    FormControlLabel,
    Radio,
    CircularProgress,
    Stack,
    Chip,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { AuthenticatedImage } from './authenticated-image';
import { ImageCapture } from './image-capture';

interface ImageSelectorProps {
    currentImageUrl?: string;
    onImageSelected: (imageUrl: string, imageId: string) => void;
    onCancel: () => void;
}

export const ImageSelector: React.FC<ImageSelectorProps> = ({
    currentImageUrl,
    onImageSelected,
    onCancel,
}) => {
    const { Api } = useApi();
    const alert = useAlertMessage();
    const [images, setImages] = useState<ImageMetadataResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [selectedImageUrl, setSelectedImageUrl] = useState<string>(currentImageUrl || '');
    const [selectedImageId, setSelectedImageId] = useState<string>('');
    const [mode, setMode] = useState<'select' | 'upload'>('select');
    const [captureDialogOpen, setCaptureDialogOpen] = useState(false);

    useEffect(() => {
        loadImages();
    }, []);

    const loadImages = () => {
        setLoading(true);
        Api.Image.getUserImages()
            .then(({ data }) => {
                setImages(data);
            })
            .catch((error) => alert.addError(error))
            .finally(() => setLoading(false));
    };

    const handleImageCapture = async (file: File) => {
        setUploading(true);
        try {
            const { data: imageUrl } = await Api.Image.uploadImage(file);
            // Reload images to get the new one with metadata
            const { data: allImages } = await Api.Image.getUserImages();
            setImages(allImages);
            
            // Find the newly uploaded image and automatically save it
            const newImage = allImages.find(img => img.url === imageUrl);
            if (newImage) {
                onImageSelected(newImage.url, newImage.imageId);
            } else {
                // If not found by exact URL match, try to find the most recently uploaded image
                const sortedImages = [...allImages].sort((a, b) => 
                    new Date(b.uploadedAt).getTime() - new Date(a.uploadedAt).getTime()
                );
                if (sortedImages.length > 0) {
                    onImageSelected(sortedImages[0].url, sortedImages[0].imageId);
                }
            }
        } catch (error) {
            alert.addMessage(error);
        } finally {
            setUploading(false);
        }
    };

    const handleConfirm = () => {
        if (selectedImageUrl && selectedImageId) {
            onImageSelected(selectedImageUrl, selectedImageId);
        }
    };

    return (
        <Dialog open={true} onClose={onCancel} maxWidth="md" fullWidth>
            <DialogTitle>Select or Upload Image</DialogTitle>
            <DialogContent>
                <Box mb={2}>
                    <RadioGroup
                        row
                        value={mode}
                        onChange={(e) => setMode(e.target.value as 'select' | 'upload')}
                    >
                        <FormControlLabel value="select" control={<Radio />} label="Select Existing" />
                        <FormControlLabel value="upload" control={<Radio />} label="Upload New" />
                    </RadioGroup>
                </Box>

                {mode === 'upload' ? (
                    <Box py={4} textAlign="center">
                        <Button
                            variant="contained"
                            onClick={() => setCaptureDialogOpen(true)}
                            disabled={uploading}
                            startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <CloudUploadIcon />}
                        >
                            {uploading ? 'Uploading...' : 'Capture or Upload Image'}
                        </Button>
                    </Box>
                ) : loading ? (
                    <Box textAlign="center" py={4}>
                        <CircularProgress />
                    </Box>
                ) : images.length === 0 ? (
                    <Typography variant="body2" color="text.secondary" textAlign="center" py={4}>
                        No images available. Upload one to get started.
                    </Typography>
                ) : (
                    <Grid container spacing={2}>
                        {images.map((image) => (
                            <Grid size={{ xs: 6, sm: 4, md: 3 }} key={image.imageId}>
                                <Card
                                    sx={{
                                        border: selectedImageUrl === image.url ? 2 : 0,
                                        borderColor: 'primary.main',
                                    }}
                                >
                                    <CardActionArea onClick={() => {
                                    setSelectedImageUrl(image.url);
                                    setSelectedImageId(image.imageId);
                                }}>
                                        <Box height="140" display="flex" alignItems="center" justifyContent="center">
                                            <AuthenticatedImage
                                                src={image.url}
                                                alt={image.fileName}
                                                style={{ width: '100%', height: '140px', objectFit: 'cover' }}
                                            />
                                        </Box>
                                        <Box p={1}>
                                            <Typography variant="caption" noWrap display="block">
                                                {image.fileName}
                                            </Typography>
                                            <Stack direction="row" spacing={0.5} mt={0.5}>
                                                {image.boxReferenceCount > 0 && (
                                                    <Chip label={`${image.boxReferenceCount} boxes`} size="small" />
                                                )}
                                                {image.itemReferenceCount > 0 && (
                                                    <Chip label={`${image.itemReferenceCount} items`} size="small" />
                                                )}
                                            </Stack>
                                        </Box>
                                    </CardActionArea>
                                </Card>
                            </Grid>
                        ))}
                    </Grid>
                )}
            </DialogContent>
            <DialogActions>
                <Button onClick={onCancel}>Cancel</Button>
                <Button
                    onClick={() => onImageSelected('', '')}
                    color="warning"
                >
                    Clear
                </Button>
                <Button
                    onClick={handleConfirm}
                    variant="contained"
                    disabled={!selectedImageUrl || !selectedImageId || mode === 'upload'}
                >
                    Select
                </Button>
            </DialogActions>

            {/* Image Capture Dialog */}
            <ImageCapture
                open={captureDialogOpen}
                onClose={() => setCaptureDialogOpen(false)}
                onCapture={handleImageCapture}
                uploading={uploading}
            />
        </Dialog>
    );
};
