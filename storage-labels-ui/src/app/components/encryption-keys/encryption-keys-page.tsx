import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
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
    IconButton,
    Paper,
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
    Add as AddIcon,
    CheckCircle as CheckCircleIcon,
    Block as BlockIcon,
    Info as InfoIcon,
    History as HistoryIcon,
    LockOpen as LockOpenIcon,
} from '@mui/icons-material';
import { Fab } from '@mui/material';
import { useApi } from '../../../api';
import { Authorized } from '../../providers/user-permission-provider';
import { Breadcrumbs } from '../shared';

const getStatusColor = (status: EncryptionKeyStatus): 'success' | 'primary' | 'warning' | 'default' => {
    switch (status) {
        case 'Active': return 'success';
        case 'Created': return 'primary';
        case 'Deprecated': return 'warning';
        case 'Retired': return 'default';
        default: return 'default';
    }
};

const formatDate = (dateString: string | undefined): string => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleString();
};

export const EncryptionKeysPage: React.FC = () => {
    const { Api } = useApi();
    const navigate = useNavigate();
    const [keys, setKeys] = useState<EncryptionKey[]>([]);
    const [loading, setLoading] = useState(true);
    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [newKeyDescription, setNewKeyDescription] = useState('');
    const [selectedKey, setSelectedKey] = useState<number | null>(null);
    const [keyStats, setKeyStats] = useState<Record<number, EncryptionKeyStats>>({});
    const [migrateDialogOpen, setMigrateDialogOpen] = useState(false);
    const [migrateBatchSize, setMigrateBatchSize] = useState<number>(100);

    const loadKeys = async () => {
        try {
            setLoading(true);
            const keysData = await Api.EncryptionKey.getEncryptionKeys();
            setKeys(keysData);

            // Load stats for each key
            const stats: Record<number, EncryptionKeyStats> = {};
            for (const key of keysData) {
                try {
                    const stat = await Api.EncryptionKey.getEncryptionKeyStats(key.kid);
                    stats[key.kid] = stat;
                } catch (err) {
                    console.error(`Failed to load stats for key ${key.kid}`, err);
                }
            }
            setKeyStats(stats);
        } catch (error) {
            console.error('Failed to load encryption keys', error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadKeys();
    }, []);

    const handleCreateKey = async () => {
        try {
            await Api.EncryptionKey.createEncryptionKey({
                description: newKeyDescription || undefined,
            });
            setCreateDialogOpen(false);
            setNewKeyDescription('');
            loadKeys();
        } catch (error) {
            console.error('Failed to create encryption key', error);
        }
    };

    const handleActivateKey = async (kid: number, autoRotate: boolean) => {
        try {
            await Api.EncryptionKey.activateEncryptionKey(kid, autoRotate);
            loadKeys();
        } catch (error) {
            console.error('Failed to activate encryption key', error);
        }
    };

    const handleRetireKey = async (kid: number) => {
        try {
            await Api.EncryptionKey.retireEncryptionKey(kid);
            loadKeys();
        } catch (error) {
            console.error('Failed to retire encryption key', error);
        }
    };

    const handleMigrateUnencrypted = async () => {
        try {
            const activeKey = keys.find(k => k.status === 'Active');
            if (!activeKey) {
                console.error('No active key found');
                return;
            }

            // Start migration using fromKeyId=null to indicate unencrypted images
            const rotationId = await Api.EncryptionKey.startKeyRotation({
                fromKeyId: null,
                toKeyId: activeKey.kid,
                batchSize: migrateBatchSize,
            });

            setMigrateDialogOpen(false);
            
            // Navigate to rotations page to see progress
            navigate(`/encryption-keys/rotations`);
        } catch (error) {
            console.error('Failed to start migration', error);
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
            <Breadcrumbs items={[{ label: 'Encryption Keys' }]} />
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Typography variant="h4">Encryption Key Management</Typography>
                <Stack direction="row" spacing={1}>
                    <Authorized permissions="read:encryption-keys">
                        <Tooltip title="View Rotations">
                            <IconButton
                                onClick={() => navigate('/encryption-keys/rotations')}
                                color="primary"
                            >
                                <HistoryIcon />
                            </IconButton>
                        </Tooltip>
                    </Authorized>
                    <Authorized permissions="write:encryption-keys">
                        <Tooltip title="Migrate Unencrypted Images">
                            <IconButton
                                onClick={() => setMigrateDialogOpen(true)}
                                color="primary"
                            >
                                <LockOpenIcon />
                            </IconButton>
                        </Tooltip>
                    </Authorized>
                </Stack>
            </Box>

            <Box position="relative">
                <Authorized permissions="write:encryption-keys">
                    <Box position="absolute" right={(theme) => theme.spacing(1)} top={(theme) => theme.spacing(1)} sx={{ zIndex: 1 }}>
                        <Tooltip title="Create New Key" placement="left">
                            <Fab
                                color="primary"
                                aria-label="create new key"
                                onClick={() => setCreateDialogOpen(true)}
                            >
                                <AddIcon />
                            </Fab>
                        </Tooltip>
                    </Box>
                </Authorized>

            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Kid</TableCell>
                            <TableCell>Version</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>Description</TableCell>
                            <TableCell>Images</TableCell>
                            <TableCell>Created</TableCell>
                            <TableCell>Activated</TableCell>
                            <TableCell align="right">Actions</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {keys.map((key) => (
                            <TableRow key={key.kid}>
                                <TableCell>{key.kid}</TableCell>
                                <TableCell>v{key.version}</TableCell>
                                <TableCell>
                                    <Chip
                                        label={key.status}
                                        color={getStatusColor(key.status)}
                                        size="small"
                                    />
                                </TableCell>
                                <TableCell>{key.description || '-'}</TableCell>
                                <TableCell>
                                    {keyStats[key.kid]?.imageCount ?? <CircularProgress size={16} />}
                                </TableCell>
                                <TableCell>{formatDate(key.createdAt)}</TableCell>
                                <TableCell>{formatDate(key.activatedAt)}</TableCell>
                                <TableCell align="right">
                                    <Authorized permissions="write:encryption-keys">
                                        {key.status === 'Created' && (
                                            <>
                                                <Tooltip title="Activate (with auto-rotation)">
                                                    <IconButton
                                                        size="small"
                                                        color="success"
                                                        onClick={() => handleActivateKey(key.kid, true)}
                                                    >
                                                        <CheckCircleIcon />
                                                    </IconButton>
                                                </Tooltip>
                                                <Tooltip title="Activate (without rotation)">
                                                    <IconButton
                                                        size="small"
                                                        color="primary"
                                                        onClick={() => handleActivateKey(key.kid, false)}
                                                    >
                                                        <CheckCircleIcon />
                                                    </IconButton>
                                                </Tooltip>
                                            </>
                                        )}
                                        {(key.status === 'Active' || key.status === 'Deprecated') && (
                                            <Tooltip title="Retire Key">
                                                <IconButton
                                                    size="small"
                                                    color="error"
                                                    onClick={() => handleRetireKey(key.kid)}
                                                >
                                                    <BlockIcon />
                                                </IconButton>
                                            </Tooltip>
                                        )}
                                    </Authorized>
                                    <Authorized permissions="read:encryption-keys">
                                        <Tooltip title="View Stats">
                                            <IconButton
                                                size="small"
                                                onClick={() => setSelectedKey(key.kid)}
                                            >
                                                <InfoIcon />
                                            </IconButton>
                                        </Tooltip>
                                    </Authorized>
                                </TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
            </Box>

            {/* Create Key Dialog */}
            <Dialog open={createDialogOpen} onClose={() => setCreateDialogOpen(false)}>
                <DialogTitle>Create New Encryption Key</DialogTitle>
                <DialogContent>
                    <TextField
                        autoFocus
                        margin="dense"
                        label="Description (optional)"
                        fullWidth
                        value={newKeyDescription}
                        onChange={(e) => setNewKeyDescription(e.target.value)}
                        placeholder="e.g., Production encryption key 2025-Q1"
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setCreateDialogOpen(false)}>Cancel</Button>
                    <Button onClick={handleCreateKey} variant="contained">
                        Create
                    </Button>
                </DialogActions>
            </Dialog>

            {/* Key Stats Dialog */}
            {selectedKey && keyStats[selectedKey] && (
                <Dialog open={true} onClose={() => setSelectedKey(null)} maxWidth="sm" fullWidth>
                    <DialogTitle>Encryption Key Statistics</DialogTitle>
                    <DialogContent>
                        <Stack spacing={2} sx={{ mt: 1 }}>
                            <Box display="flex" gap={2}>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Kid
                                    </Typography>
                                    <Typography variant="h6">{keyStats[selectedKey].kid}</Typography>
                                </Box>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Version
                                    </Typography>
                                    <Typography variant="h6">v{keyStats[selectedKey].version}</Typography>
                                </Box>
                            </Box>
                            <Box display="flex" gap={2}>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Status
                                    </Typography>
                                    <Chip
                                        label={keyStats[selectedKey].status}
                                        color={getStatusColor(keyStats[selectedKey].status)}
                                    />
                                </Box>
                                <Box flex={1}>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Images Encrypted
                                    </Typography>
                                    <Typography variant="h6">{keyStats[selectedKey].imageCount}</Typography>
                                </Box>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary">
                                    Total Size
                                </Typography>
                                <Typography variant="h6">
                                    {(keyStats[selectedKey].totalSizeBytes / 1024 / 1024).toFixed(2)} MB
                                </Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="textSecondary">
                                    Created At
                                </Typography>
                                <Typography>{formatDate(keyStats[selectedKey].createdAt)}</Typography>
                            </Box>
                            {keyStats[selectedKey].activatedAt && (
                                <Box>
                                    <Typography variant="subtitle2" color="textSecondary">
                                        Activated At
                                    </Typography>
                                    <Typography>{formatDate(keyStats[selectedKey].activatedAt)}</Typography>
                                </Box>
                            )}
                        </Stack>
                    </DialogContent>
                    <DialogActions>
                        <Button onClick={() => setSelectedKey(null)}>Close</Button>
                    </DialogActions>
                </Dialog>
            )}

            {/* Migrate Unencrypted Images Dialog */}
            <Dialog open={migrateDialogOpen} onClose={() => setMigrateDialogOpen(false)}>
                <DialogTitle>Migrate Unencrypted Images</DialogTitle>
                <DialogContent>
                    <Stack spacing={2} sx={{ mt: 1, minWidth: 400 }}>
                        <Typography variant="body2" color="textSecondary">
                            This will encrypt all unencrypted images in the database using the currently active encryption key.
                            The process will run in the background and you can monitor progress on the Rotations page.
                        </Typography>
                        <TextField
                            label="Batch Size"
                            type="number"
                            fullWidth
                            value={migrateBatchSize}
                            onChange={(e) => setMigrateBatchSize(parseInt(e.target.value))}
                            helperText="Number of images to process per batch"
                        />
                        {keys.find(k => k.status === 'Active') && (
                            <Box sx={{ bgcolor: 'info.light', p: 2, borderRadius: 1 }}>
                                <Typography variant="subtitle2">
                                    Target Key: kid {keys.find(k => k.status === 'Active')!.kid} - v{keys.find(k => k.status === 'Active')!.version}
                                </Typography>
                            </Box>
                        )}
                        {!keys.find(k => k.status === 'Active') && (
                            <Box sx={{ bgcolor: 'warning.light', p: 2, borderRadius: 1 }}>
                                <Typography variant="subtitle2" color="warning.dark">
                                    No active encryption key found. Please activate a key first.
                                </Typography>
                            </Box>
                        )}
                    </Stack>
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setMigrateDialogOpen(false)}>Cancel</Button>
                    <Button 
                        onClick={handleMigrateUnencrypted} 
                        variant="contained"
                        disabled={!keys.find(k => k.status === 'Active')}
                    >
                        Start Migration
                    </Button>
                </DialogActions>
            </Dialog>
        </Box>
    );
};

