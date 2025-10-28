import React, { useState, useEffect } from 'react';
import {
    Box,
    Typography,
    Button,
    Grid,
    Card,
    CardMedia,
    CardContent,
    CardActions,
    CircularProgress,
    Chip,
    Stack,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogContentText,
    DialogActions,
    FormControlLabel,
    Checkbox,
} from '@mui/material';
import CloudUploadIcon from '@mui/icons-material/CloudUpload';
import DeleteIcon from '@mui/icons-material/Delete';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useSnackbar } from '../../providers/snackbar-provider';
import { AuthenticatedImage } from '../shared';

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
            await Api.Image.uploadImage(file);
            snackbar.showSuccess('Image uploaded successfully');
            loadImages();
        } catch (error) {
            alert.addMessage(error);
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

    const formatFileSize = (bytes: number): string => {
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
    };

    const formatDate = (dateString: string): string => {
        return new Date(dateString).toLocaleDateString();
    };

    const hasReferences = selectedImage ? (selectedImage.boxReferenceCount > 0 || selectedImage.itemReferenceCount > 0) : false;

    return (
        <Box>
            <Box display="flex" justifyContent="space-between" alignItems="center" pb={2}>
                <Typography variant="h4">Images</Typography>
                <input
                    accept="image/*"
                    capture="environment"
                    style={{ display: 'none' }}
                    id="image-upload-button"
                    type="file"
                    onChange={handleFileUpload}
                    disabled={uploading}
                />
                <label htmlFor="image-upload-button">
                    <Button
                        variant="contained"
                        component="span"
                        startIcon={uploading ? <CircularProgress size={20} color="inherit" /> : <CloudUploadIcon />}
                        disabled={uploading}
                    >
                        {uploading ? 'Uploading...' : 'Upload Image'}
                    </Button>
                </label>
            </Box>

            {loading ? (
                <Box textAlign="center" py={4}>
                    <CircularProgress />
                </Box>
            ) : images.length === 0 ? (
                <Typography variant="body1" color="text.secondary" textAlign="center" py={4}>
                    No images uploaded yet. Click "Upload Image" to get started.
                </Typography>
            ) : (
                <Grid container spacing={2}>
                    {images.map((image) => (
                        <Grid size={{ xs: 12, sm: 6, md: 4, lg: 3 }} key={image.imageId}>
                            <Card>
                                <Box height={200} display="flex" alignItems="center" justifyContent="center" bgcolor="grey.100">
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
                                        onClick={() => handleDeleteClick(image)}
                                    >
                                        Delete
                                    </Button>
                                </CardActions>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            )}

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
                                            To delete this image, you must check the "Force Delete" option below. This will remove the image from all boxes and items.
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
        </Box>
    );
};

