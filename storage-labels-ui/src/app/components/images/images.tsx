import React, { useState, useEffect } from 'react';
import {
    Box,
    Typography,
    Button,
    Grid,
    CircularProgress,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    FormControlLabel,
    Checkbox,
    Paper,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSnackbar } from '../../providers/snackbar-provider';
import { ImageCapture } from '../shared/image-capture';
import { ImageCard } from './image-card';

export const Images: React.FC = () => {
    const { Api } = useApi();
    const alert = useAlertMessage();
    const snackbar = useSnackbar();
    
    const [images, setImages] = useState<ImageMetadataResponse[]>([]);
    const [loading, setLoading] = useState(false);
    const [uploading, setUploading] = useState(false);
    const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
    const [selectedImage, setSelectedImage] = useState<ImageMetadataResponse | null>(null);
    const [forceDelete, setForceDelete] = useState(false);
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
            await Api.Image.uploadImage(file);
            snackbar.showSuccess('Image uploaded successfully');
            loadImages();
        } catch (error) {
            alert.addError(error);
        } finally {
            setUploading(false);
        }
    };

    const handleDeleteClick = (image: ImageMetadataResponse) => {
        setSelectedImage(image);
        setForceDelete(false);
        setDeleteDialogOpen(true);
    };

    const handleDeleteConfirm = async () => {
        if (!selectedImage) return;

        const hasReferences = selectedImage.boxReferenceCount > 0 || selectedImage.itemReferenceCount > 0;

        if (hasReferences && !forceDelete) {
            alert.addMessage('Please check the force delete option to delete this image');
            return;
        }

        try {
            await Api.Image.deleteImage(selectedImage.imageId, forceDelete);
            snackbar.showSuccess('Image deleted successfully');
            setDeleteDialogOpen(false);
            setSelectedImage(null);
            setForceDelete(false);
            loadImages();
        } catch (error) {
            alert.addMessage(error);
        }
    };

    const handleDeleteCancel = () => {
        setDeleteDialogOpen(false);
        setSelectedImage(null);
        setForceDelete(false);
    };

    const hasReferences = selectedImage ? (selectedImage.boxReferenceCount > 0 || selectedImage.itemReferenceCount > 0) : false;

    return (
        <Box>
            {/* Upload Section */}
            <Paper elevation={2} sx={{ p: 3, mb: 4 }}>
                <Typography variant="h5" gutterBottom>
                    Upload New Image
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom>
                    Add photos of your storage boxes and items
                </Typography>

                <Button
                    variant="contained"
                    onClick={() => setCaptureDialogOpen(true)}
                    disabled={uploading}
                    startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <CloudUploadIcon />}
                    sx={{ mt: 2 }}
                >
                    {uploading ? 'Uploading...' : 'Upload Image'}
                </Button>
            </Paper>

            {/* Images Gallery Section */}
            <Box>
                <Typography variant="h5" gutterBottom>
                    Your Uploaded Images
                </Typography>
                <Typography variant="body2" color="text.secondary" gutterBottom mb={2}>
                    Select from these images when adding or editing boxes and items
                </Typography>

                {loading ? (
                    <Box textAlign="center" py={4}>
                        <CircularProgress />
                    </Box>
                ) : images.length === 0 ? (
                    <Typography variant="body1" color="text.secondary" textAlign="center" py={4}>
                        No images uploaded yet. Upload your first image to get started.
                    </Typography>
                ) : (
                    <Grid container spacing={2}>
                        {images.map((image) => (
                            <Grid size={{ xs: 12, sm: 6, md: 4, lg: 3 }} key={image.imageId}>
                                <ImageCard image={image} onDelete={handleDeleteClick} />
                            </Grid>
                        ))}
                    </Grid>
                )}
            </Box>

            {/* Delete Confirmation Dialog */}
            <Dialog open={deleteDialogOpen} onClose={handleDeleteCancel}>
                <DialogTitle>Delete Image</DialogTitle>
                <DialogContent>
                    {selectedImage && (
                        <>
                            <DialogContentText>
                                Are you sure you want to delete <strong>{selectedImage.fileName}</strong>?
                            </DialogContentText>
                            {hasReferences && (
                                <>
                                    <Box mt={2} p={2} bgcolor="warning.light" borderRadius={1}>
                                        <Typography variant="body2" color="text.primary">
                                            <strong>Warning:</strong> This image is currently being used by:
                                        </Typography>
                                        <Box component="ul" mt={1} mb={0}>
                                            {selectedImage.boxReferenceCount > 0 && (
                                                <li>
                                                    <Typography variant="body2">
                                                        {selectedImage.boxReferenceCount} box{selectedImage.boxReferenceCount > 1 ? 'es' : ''}
                                                    </Typography>
                                                </li>
                                            )}
                                            {selectedImage.itemReferenceCount > 0 && (
                                                <li>
                                                    <Typography variant="body2">
                                                        {selectedImage.itemReferenceCount} item{selectedImage.itemReferenceCount > 1 ? 's' : ''}
                                                    </Typography>
                                                </li>
                                            )}
                                        </Box>
                                        <Typography variant="body2" color="text.primary" mt={1}>
                                            To delete this image, you must check the &ldquo;Force Delete&rdquo; option below. This will remove the image from all boxes and items.
                                        </Typography>
                                    </Box>
                                    <Box mt={2}>
                                        <FormControlLabel
                                            control={
                                                <Checkbox
                                                    checked={forceDelete}
                                                    onChange={(e) => setForceDelete(e.target.checked)}
                                                    color="error"
                                                />
                                            }
                                            label="Force delete - remove image from all boxes and items"
                                        />
                                    </Box>
                                </>
                            )}
                        </>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button
                        onClick={handleDeleteConfirm}
                        color="primary"
                        disabled={hasReferences && !forceDelete}
                        autoFocus
                    >
                        Delete
                    </Button>
                    <Button onClick={handleDeleteCancel} color="secondary">
                        Cancel
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Image Capture Dialog */}
            <ImageCapture
                open={captureDialogOpen}
                onClose={() => setCaptureDialogOpen(false)}
                onCapture={handleImageCapture}
                uploading={uploading}
            />
        </Box>
    );
};

