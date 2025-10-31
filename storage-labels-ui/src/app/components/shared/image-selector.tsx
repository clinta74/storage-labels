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

    useEffect(() => {
        loadImages();
    }, []);

    const loadImages = () => {
        setLoading(true);
        Api.Image.getUserImages()
            .then(({ data }) => {
                setImages(data);
            })
            .catch((error) => alert.addMessage(error))
            .finally(() => setLoading(false));
    };

    const handleFileUpload = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        setUploading(true);
        try {
            const { data: imageId } = await Api.Image.uploadImage(file);
            // Reload images to get the new one
            const { data: updatedImages } = await Api.Image.getUserImages();
            setImages(updatedImages);
            
            // Find the newly uploaded image and auto-select it
            const newImage = updatedImages.find(img => img.imageId === imageId);
            if (newImage) {
                // Automatically use the uploaded image and save
                onImageSelected(newImage.url, newImage.imageId);
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
                    <Box textAlign="center" py={4}>
                        <input
                            accept="image/*"
                            style={{ display: 'none' }}
                            id="image-upload-input"
                            type="file"
                            onChange={handleFileUpload}
                            disabled={uploading}
                        />
                        <label htmlFor="image-upload-input">
                            <Button
                                variant="contained"
                                component="span"
                                startIcon={uploading ? <CircularProgress size={20} /> : <CloudUploadIcon />}
                                disabled={uploading}
                            >
                                {uploading ? 'Uploading...' : 'Choose Image to Upload'}
                            </Button>
                        </label>
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
        </Dialog>
    );
};
