import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router';
import {
    Box,
    Button,
    FormControl,
    Paper,
    Stack,
    TextField,
    Typography,
} from '@mui/material';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { ImageSelector, AuthenticatedImage } from '../shared';

type Params = Record<'boxId', string>;

export const AddItem: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [imageUrl, setImageUrl] = useState('');
    const [imageMetadataId, setImageMetadataId] = useState<string | undefined>(undefined);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [saving, setSaving] = useState(false);
    const [showImageSelector, setShowImageSelector] = useState(false);

    const hasError = (field: string, value: string) => {
        return isSubmitted && value.trim().length === 0;
    };

    const handleSave = () => {
        setIsSubmitted(true);
        const isValid = name.trim().length > 0;

        if (!saving && isValid && params.boxId) {
            setSaving(true);
            const newItem: ItemRequest = {
                boxId: params.boxId,
                name,
                description,
                imageUrl,
                imageMetadataId,
            };

            Api.Item.createItem(newItem)
                .then(() => {
                    navigate('../..');
                })
                .catch((error) => alert.addMessage(error.message))
                .finally(() => setSaving(false));
        }
    };

    const handleImageSelected = (url: string, imageId: string) => {
        if (url === '' && imageId === '') {
            // Clear the image
            setImageUrl('');
            setImageMetadataId(undefined);
        } else {
            setImageUrl(url);
            setImageMetadataId(imageId);
        }
        setShowImageSelector(false);
    };

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant="h4">Add Item</Typography>
                    </Box>
                    <Box margin={2}>
                        <Stack spacing={2}>
                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    label="Name"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    disabled={saving}
                                    required
                                    error={hasError('name', name)}
                                    helperText={hasError('name', name) ? 'Name is required' : ''}
                                />
                            </FormControl>

                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    label="Description"
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    disabled={saving}
                                    multiline
                                    rows={3}
                                />
                            </FormControl>

                            <Box>
                                <Typography variant="subtitle2" gutterBottom>
                                    Image
                                </Typography>
                                {imageUrl ? (
                                    <Box mb={2} display="flex" justifyContent="center">
                                        <AuthenticatedImage
                                            src={imageUrl}
                                            alt="Item image"
                                            style={{ maxWidth: 300, maxHeight: 200, objectFit: 'contain' }}
                                        />
                                    </Box>
                                ) : (
                                    <Box mb={2} display="flex" justifyContent="center" py={4}>
                                        <Typography variant="body2" color="text.secondary">
                                            No image set
                                        </Typography>
                                    </Box>
                                )}
                                <Button
                                    color="primary"
                                    onClick={() => setShowImageSelector(true)}
                                    disabled={saving}
                                >
                                    {imageUrl ? 'Change Image' : 'Select Image'}
                                </Button>
                            </Box>
                        </Stack>
                    </Box>
                    <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                        <Button color="primary" onClick={handleSave} disabled={saving}>
                            Add
                        </Button>
                        <Button color="secondary" component={Link} to="../..">
                            Cancel
                        </Button>
                    </Stack>
                </Paper>
            </Box>

            {showImageSelector && (
                <ImageSelector
                    currentImageUrl={imageUrl}
                    onImageSelected={handleImageSelected}
                    onCancel={() => setShowImageSelector(false)}
                />
            )}
        </React.Fragment>
    );
};
