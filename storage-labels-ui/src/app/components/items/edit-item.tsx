import React, { useState, useEffect } from 'react';
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

type Params = Record<'itemId', string>;

export const EditItem: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [item, setItem] = useState<ItemResponse | null>(null);
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [imageUrl, setImageUrl] = useState('');
    const [imageMetadataId, setImageMetadataId] = useState<string | undefined>(undefined);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [saving, setSaving] = useState(false);
    const [showImageSelector, setShowImageSelector] = useState(false);

    useEffect(() => {
        const itemId = params.itemId;
        if (itemId) {
            Api.Item.getItemById(itemId)
                .then(({ data }) => {
                    setItem(data);
                    setName(data.name);
                    setDescription(data.description || '');
                    setImageUrl(data.imageUrl || '');
                    setImageMetadataId(data.imageMetadataId);
                })
                .catch((error) => alert.addError(error));
        }
    }, [params]);

    const handleSave = () => {
        setIsSubmitted(true);
        const isValid = name.trim().length > 0;

        if (!saving && isValid && item) {
            setSaving(true);
            const updatedItem: ItemRequest = {
                boxId: item.boxId,
                name,
                description,
                imageUrl,
                imageMetadataId,
            };

            Api.Item.updateItem(item.itemId, updatedItem)
                .then(() => {
                    navigate('../..');
                })
                .catch((error) => alert.addError(error))
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

    if (!item) {
        return null;
    }

    const hasError = (field: string, value: string) =>
        isSubmitted && value.trim().length === 0;

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant="h4">Edit Item</Typography>
                    </Box>
                    <Box margin={2}>
                        <Stack spacing={2}>
                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    error={hasError('name', name)}
                                    label="Name"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    disabled={saving}
                                    required
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
                            Save
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
