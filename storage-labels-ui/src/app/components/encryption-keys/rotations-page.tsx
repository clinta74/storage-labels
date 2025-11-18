import React, { useEffect, useState } from 'react';
import { useAuth } from '../../../auth/auth-provider';
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    FormControl,
    IconButton,
    InputLabel,
    LinearProgress,
    MenuItem,
    Paper,
    Select,
    Stack,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    TextField,
    Tooltip,
    Typography,
} from '@mui/material';
import {
    Refresh as RefreshIcon,
    Cancel as CancelIcon,
    Info as InfoIcon,
    PlayArrow as PlayArrowIcon,
} from '@mui/icons-material';
import { useApi } from '../../../api';
import { Authorized } from '../../providers/user-permission-provider';
import { Breadcrumbs } from '../shared';

const getStatusColor = (status: RotationStatus): 'info' | 'success' | 'error' | 'default' => {
    switch (status) {
        case 'InProgress': return 'info';
        case 'Completed': return 'success';
        case 'Failed': return 'error';
        case 'Cancelled': return 'default';
        default: return 'default';
    }
};

const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString();
};

export const RotationsPage: React.FC = () => {
    const { Api } = useApi();
    const { getToken } = useAuth();
    const [rotations, setRotations] = useState<EncryptionKeyRotation[]>([]);
    const [loading, setLoading] = useState(true);
    const [selectedRotation, setSelectedRotation] = useState<string | null>(null);
    const [rotationProgress, setRotationProgress] = useState<RotationProgress | null>(null);
    const [manualRotationOpen, setManualRotationOpen] = useState(false);
    const [fromKeyId, setFromKeyId] = useState<number>(0);
    const [toKeyId, setToKeyId] = useState<number>(0);
    const [batchSize, setBatchSize] = useState<number>(100);
    const [keys, setKeys] = useState<EncryptionKey[]>([]);

    const activeKeys = keys.filter(key => key.status === 'Active');
    const defaultActiveKey = activeKeys.length > 0 
        ? activeKeys.reduce((min, key) => key.kid < min.kid ? key : min, activeKeys[0])
        : null;

    const loadRotations = async () => {
        try {
            setLoading(true);
            const rotationsData = await Api.EncryptionKey.getRotations();
            setRotations(rotationsData);
        } catch (error) {
            console.error('Failed to load rotations', error);
        } finally {
            setLoading(false);
        }
    };

    const loadKeys = async () => {
        try {
            const keysData = await Api.EncryptionKey.getEncryptionKeys();
            setKeys(keysData);
            
            // Set default values for the form
            const activeKeysData = keysData.filter(key => key.status === 'Active');
            if (activeKeysData.length > 0) {
                const defaultActive = activeKeysData.reduce((min, key) => key.kid < min.kid ? key : min, activeKeysData[0]);
                setToKeyId(defaultActive.kid);
                
                // Set fromKeyId to the first non-active key if available, otherwise the first key
                const nonActiveKey = keysData.find(key => key.status !== 'Active');
                setFromKeyId(nonActiveKey ? nonActiveKey.kid : keysData[0]?.kid || 0);
            }
        } catch (error) {
            console.error('Failed to load encryption keys', error);
        }
    };

    const loadRotationProgress = async (rotationId: string) => {
        try {
            const progress = await Api.EncryptionKey.getRotationProgress(rotationId);
            setRotationProgress(progress);
        } catch (error) {
            console.error('Failed to load rotation progress', error);
        }
    };

    useEffect(() => {
        loadRotations();
        loadKeys();
    }, []);

    // Set up SSE streams for all in-progress rotations in the table
    useEffect(() => {
        const inProgressRotations = rotations.filter(r => r.status === 'InProgress');
        const cleanupFunctions: (() => void)[] = [];

        inProgressRotations.forEach(rotation => {
            Api.EncryptionKey.streamRotationProgress(
                rotation.id,
                (progress) => {
                    // Update the rotation in the list
                    setRotations(prev => prev.map(r => 
                        r.id === rotation.id 
                            ? { ...r, status: progress.status, processedImages: progress.processedImages, failedImages: progress.failedImages }
                            : r
                    ));

                    // If viewing this rotation's details, update the progress
                    if (selectedRotation === rotation.id) {
                        setRotationProgress(progress);
                    }

                    // Refresh the full list when rotation completes
                    if (progress.status !== 'InProgress') {
                        loadRotations();
                    }
                },
                getToken
            ).then(cleanup => {
                cleanupFunctions.push(cleanup);
            }).catch(error => {
                console.error(`Failed to start SSE stream for rotation ${rotation.id}:`, error);
            });
        });

        return () => {
            cleanupFunctions.forEach(cleanup => cleanup());
        };
    }, [rotations.length]); // Only re-run when the number of rotations changes

    useEffect(() => {
        if (selectedRotation) {
            // Load initial progress
            loadRotationProgress(selectedRotation);

            // Set up SSE stream for real-time updates
            let cleanup: (() => void) | undefined;

            Api.EncryptionKey.streamRotationProgress(
                selectedRotation,
                (progress) => {
                    setRotationProgress(progress);
                    
                    // Refresh rotations list when rotation completes
                    if (progress.status !== 'InProgress') {
                        loadRotations();
                    }
                },
                getToken
            ).then(cleanupFn => {
                cleanup = cleanupFn;
            }).catch(error => {
                console.error('Failed to start SSE stream:', error);
            });

            return () => {
                if (cleanup) {
                    cleanup();
                }
            };
        }
    }, [selectedRotation]);

    const handleStartManualRotation = async () => {
        try {
            await Api.EncryptionKey.startKeyRotation({
                fromKeyId,
                toKeyId,
                batchSize,
            });
            setManualRotationOpen(false);
            loadRotations();
        } catch (error) {
            console.error('Failed to start manual rotation', error);
        }
    };

    const handleCancelRotation = async (rotationId: string) => {
        try {
            await Api.EncryptionKey.cancelRotation(rotationId);
            loadRotations();
        } catch (error) {
            console.error('Failed to cancel rotation', error);
        }
    };

    if (loading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box p={3}>
            <Breadcrumbs 
                items={[
                    { label: 'Rotations' }
                ]} 
                homeLabel="Encryption Keys"
                homePath="/encryption-keys"
            />
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Typography variant="h4">Key Rotation History</Typography>
                <Box>
                    <Tooltip title="Refresh">
                        <IconButton onClick={loadRotations} sx={{ mr: 1 }}>
                            <RefreshIcon />
                        </IconButton>
                    </Tooltip>
                    <Authorized permissions="write:encryption-keys">
                        <Button
                            variant="contained"
                            startIcon={<PlayArrowIcon />}
                            onClick={() => setManualRotationOpen(true)}
                        >
                            Start Manual Rotation
                        </Button>
                    </Authorized>
                </Box>
            </Box>

            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Rotation ID</TableCell>
                            <TableCell>From Key</TableCell>
                            <TableCell>To Key</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>Progress</TableCell>
                            <TableCell>Type</TableCell>
                            <TableCell>Started</TableCell>
                            <TableCell>Completed</TableCell>
                            <TableCell align="right">Actions</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {rotations.map((rotation) => {
                            const percentComplete = rotation.totalImages > 0
                                ? Math.round((rotation.processedImages / rotation.totalImages) * 100)
                                : 0;

                            return (
                                <TableRow key={rotation.id}>
                                    <TableCell>
                                        <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                            {rotation.id.substring(0, 8)}...
                                        </Typography>
                                    </TableCell>
                                    <TableCell>
                                        {rotation.fromKeyId === null || rotation.fromKeyId === 0 ? 'Unencrypted' : `kid ${rotation.fromKeyId}`}
                                    </TableCell>
                                    <TableCell>kid {rotation.toKeyId}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={rotation.status}
                                            color={getStatusColor(rotation.status)}
                                            size="small"
                                        />
                                    </TableCell>
                                    <TableCell>
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                            <Box sx={{ flex: 1, minWidth: 100 }}>
                                                <LinearProgress
                                                    variant="determinate"
                                                    value={percentComplete}
                                                    color={rotation.failedImages > 0 ? 'error' : 'primary'}
                                                />
                                            </Box>
                                            <Typography variant="body2" sx={{ minWidth: 80 }}>
                                                {rotation.processedImages}/{rotation.totalImages}
                                                {rotation.failedImages > 0 && (
                                                    <Typography component="span" color="error" variant="body2">
                                                        {' '}({rotation.failedImages} failed)
                                                    </Typography>
                                                )}
                                            </Typography>
                                        </Box>
                                    </TableCell>
                                    <TableCell>
                                        <Chip
                                            label={rotation.isAutomatic ? 'Auto' : 'Manual'}
                                            size="small"
                                            variant="outlined"
                                        />
                                    </TableCell>
                                    <TableCell>{formatDate(rotation.startedAt)}</TableCell>
                                    <TableCell>{formatDate(rotation.completedAt)}</TableCell>
                                    <TableCell align="right">
                                        <Tooltip title="View Details">
                                            <IconButton
                                                size="small"
                                                onClick={() => setSelectedRotation(rotation.id)}
                                            >
                                                <InfoIcon />
                                            </IconButton>
                                        </Tooltip>
                                        <Authorized permissions="write:encryption-keys">
                                            {rotation.status === 'InProgress' && (
                                                <Tooltip title="Cancel Rotation">
                                                    <IconButton
                                                        size="small"
                                                        color="error"
                                                        onClick={() => handleCancelRotation(rotation.id)}
                                                    >
                                                        <CancelIcon />
                                                    </IconButton>
                                                </Tooltip>
                                            )}
                                        </Authorized>
                                    </TableCell>
                                </TableRow>
                            );
                        })}
                        {rotations.length === 0 && (
                            <TableRow>
                                <TableCell colSpan={9} align="center">
                                    <Typography variant="body2" color="textSecondary" sx={{ py: 4 }}>
                                        No rotation history found
                                    </Typography>
                                </TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                </Table>
            </TableContainer>

            {/* Manual Rotation Dialog */}
            <Dialog open={manualRotationOpen} onClose={() => setManualRotationOpen(false)}>
                <DialogTitle>Start Manual Key Rotation</DialogTitle>
                <DialogContent>
                    <Stack spacing={2} sx={{ mt: 1, minWidth: 400 }}>
                        <FormControl fullWidth>
                            <InputLabel>From Key ID</InputLabel>
                            <Select
                                value={fromKeyId}
                                label="From Key ID"
                                onChange={(e) => setFromKeyId(e.target.value as number)}
                            >
                                {keys.map(key => (
                                    <MenuItem key={key.kid} value={key.kid}>
                                        kid {key.kid} - v{key.version} ({key.status})
                                    </MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        <FormControl fullWidth>
                            <InputLabel>To Key ID (Active Keys Only)</InputLabel>
                            <Select
                                value={toKeyId}
                                label="To Key ID (Active Keys Only)"
                                onChange={(e) => setToKeyId(e.target.value as number)}
                            >
                                {activeKeys.map(key => (
                                    <MenuItem key={key.kid} value={key.kid}>
                                        kid {key.kid} - v{key.version}
                                    </MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        <TextField
                            label="Batch Size"
                            type="number"
                            fullWidth
                            value={batchSize}
                            onChange={(e) => setBatchSize(parseInt(e.target.value))}
                            helperText="Number of images to process per batch"
                        />
                    </Stack>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setManualRotationOpen(false)}>Cancel</Button>
                    <Button 
                        onClick={handleStartManualRotation} 
                        variant="contained"
                        disabled={!toKeyId || !fromKeyId || activeKeys.length === 0}
                    >
                        Start Rotation
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Rotation Progress Dialog */}
            {selectedRotation && rotationProgress && (
                <Dialog open={true} onClose={() => setSelectedRotation(null)} maxWidth="sm" fullWidth>
                    <DialogTitle>Rotation Progress</DialogTitle>
                    <DialogContent>
                        <Stack spacing={2} sx={{ mt: 1 }}>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary">
                                    Rotation ID
                                </Typography>
                                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                                    {rotationProgress.rotationId}
                                </Typography>
                            </Box>
                            <Box display="flex" gap={2}>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        From Key
                                    </Typography>
                                    <Typography variant="h6">
                                        {rotationProgress.fromKeyId === null || rotationProgress.fromKeyId === 0 ? 'Unencrypted' : `kid ${rotationProgress.fromKeyId}`}
                                    </Typography>
                                </Box>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        To Key
                                    </Typography>
                                    <Typography variant="h6">kid {rotationProgress.toKeyId}</Typography>
                                </Box>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary">
                                    Status
                                </Typography>
                                <Chip
                                    label={rotationProgress.status}
                                    color={getStatusColor(rotationProgress.status)}
                                />
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary" gutterBottom>
                                    Progress
                                </Typography>
                                <LinearProgress
                                    variant="determinate"
                                    value={rotationProgress.percentComplete}
                                    sx={{ height: 10, borderRadius: 1 }}
                                    color={rotationProgress.failedImages > 0 ? 'error' : 'primary'}
                                />
                                <Box sx={{ display: 'flex', justifyContent: 'space-between', mt: 1 }}>
                                    <Typography variant="body2">
                                        {rotationProgress.processedImages} / {rotationProgress.totalImages} images
                                    </Typography>
                                    <Typography variant="body2" fontWeight="bold">
                                        {rotationProgress.percentComplete}%
                                    </Typography>
                                </Box>
                                {rotationProgress.failedImages > 0 && (
                                    <Typography variant="body2" color="error" sx={{ mt: 1 }}>
                                        {rotationProgress.failedImages} images failed
                                    </Typography>
                                )}
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary">
                                    Started At
                                </Typography>
                                <Typography>{formatDate(rotationProgress.startedAt)}</Typography>
                            </Box>
                            {rotationProgress.completedAt && (
                                <Box>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Completed At
                                    </Typography>
                                    <Typography>{formatDate(rotationProgress.completedAt)}</Typography>
                                </Box>
                            )}
                            {rotationProgress.errorMessage && (
                                <Box>
                                    <Typography variant="subtitle2" color="error">
                                        Error Message
                                    </Typography>
                                    <Typography variant="body2" color="error">
                                        {rotationProgress.errorMessage}
                                    </Typography>
                                </Box>
                            )}
                        </Stack>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setSelectedRotation(null)}>Close</Button>
                        {rotationProgress.status === 'InProgress' && (
                            <Button onClick={() => loadRotationProgress(selectedRotation)} variant="outlined">
                                Refresh
                            </Button>
                        )}
                    </DialogActions>
                </Dialog>
            )}
        </Box>
    );
};
