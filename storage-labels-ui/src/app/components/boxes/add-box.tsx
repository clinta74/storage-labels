import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router';
import {
    Box,
    Button,
    FormControl,
    IconButton,
    Paper,
    Stack,
    TextField,
    Typography,
} from '@mui/material';
import QrCodeScannerIcon from '@mui/icons-material/QrCodeScanner';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { ImageSelector, AuthenticatedImage } from '../shared';
import { Scanner } from '@yudiel/react-qr-scanner';

type Params = Record<'locationId', string>;

export const AddBox: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { Api } = useApi();
    const [code, setCode] = useState('');
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [imageUrl, setImageUrl] = useState('');
    const [imageMetadataId, setImageMetadataId] = useState<string | undefined>(undefined);
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [saving, setSaving] = useState(false);
    const [showImageSelector, setShowImageSelector] = useState(false);
    const [showQrScanner, setShowQrScanner] = useState(false);

    const hasError = (field: string, value: string) => {
        return isSubmitted && value.trim().length === 0;
    };

    const handleQrScan = (data: unknown) => {
        if (data && Array.isArray(data) && data.length > 0) {
            const result = data[0];
            if (result && typeof result === 'object' && 'rawValue' in result) {
                setCode(String(result.rawValue));
                setShowQrScanner(false);
            }
        }
    };

    const handleQrError = (err: unknown) => {
        console.error('QR Scanner error:', err);
    };

    const handleSave = () => {
        setIsSubmitted(true);
        const isValid = code.trim().length > 0 && name.trim().length > 0;

        if (!saving && isValid && params.locationId) {
            setSaving(true);
            const newBox: BoxRequest = {
                code,
                name,
                description,
                locationId: parseInt(params.locationId),
                imageUrl,
                imageMetadataId,
            };

            Api.Box.createBox(newBox)
                .then(() => {
                    navigate(`/locations/${params.locationId}`);
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

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant="h4">Add Box</Typography>
                    </Box>
                    <Box margin={2}>
                        <Stack spacing={2}>
                            <FormControl fullWidth>
                                <TextField
                                    variant="standard"
                                    label="Code"
                                    value={code}
                                    onChange={(e) => setCode(e.target.value)}
                                    disabled={saving}
                                    required
                                    error={hasError('code', code)}
                                    helperText={hasError('code', code) ? 'Code is required' : ''}
                                    slotProps={{
                                        input: {
                                            endAdornment: (
                                                <IconButton
                                                    onClick={() => setShowQrScanner(!showQrScanner)}
                                                    edge="end"
                                                    disabled={saving}
                                                    aria-label="scan QR code"
                                                    title="Scan QR code"
                                                >
                                                    <QrCodeScannerIcon />
                                                </IconButton>
                                            ),
                                        },
                                    }}
                                />
                            </FormControl>

                            {showQrScanner && (
                                <Box>
                                    <Scanner
                                        onScan={handleQrScan}
                                        onError={handleQrError}
                                        constraints={{
                                            facingMode: 'environment'
                                        }}
                                        styles={{
                                            container: { width: '100%' }
                                        }}
                                    />
                                    <Button
                                        onClick={() => setShowQrScanner(false)}
                                        color="secondary"
                                        fullWidth
                                        sx={{ mt: 1 }}
                                    >
                                        Cancel Scan
                                    </Button>
                                </Box>
                            )}

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
                                            alt="Box image"
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
                        <Button color="secondary" component={Link} to="..">
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
