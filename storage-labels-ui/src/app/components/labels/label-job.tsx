import React, { useEffect, useState } from 'react';
import {
    Box,
    Button,
    Card,
    CardContent,
    Chip,
    Divider,
    Grid,
    IconButton,
    Menu,
    MenuItem,
    Paper,
    Typography,
    useTheme,
} from '@mui/material';
import PrintIcon from '@mui/icons-material/Print';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import { Link, useNavigate, useParams } from 'react-router';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { useConfirm } from 'material-ui-confirm';
import { Breadcrumbs } from '../shared';

export const LabelJobPage: React.FC = () => {
    const { jobId } = useParams<{ jobId: string }>();
    const { Api } = useApi();
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const [job, setJob] = useState<LabelPrintJobResponse | null>(null);
    const [loading, setLoading] = useState(false);
    const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
    const theme = useTheme();
    const confirm = useConfirm();

    useEffect(() => {
        if (!jobId) return;
        Api.Label.getLabelJobById(jobId)
            .then(({ data }) => setJob(data))
            .catch(() => alert.addMessage('Failed to load label job.'));
    }, [jobId]);

    const handlePrintNextPage = () => {
        if (!jobId) return;
        setLoading(true);
        Api.Label.getNextPage(jobId)
            .then(({ data }) => {
                navigate(`/labels/${jobId}/print`, { state: { page: data } });
            })
            .catch(() => alert.addMessage('Failed to generate next page.'))
            .finally(() => setLoading(false));
    };

    const handleDelete = async () => {
        setMenuAnchor(null);
        try {
            await confirm({ description: `Delete label job "${job!.name}"? This cannot be undone.` });
        } catch {
            return;
        }
        Api.Label.deleteLabelJob(jobId!)
            .then(() => {
                alert.addMessage(`Deleted "${job!.name}".`);
                navigate('/labels');
            })
            .catch(() => alert.addMessage('Failed to delete label job.'));
    };

    if (!job) {
        return <Box sx={{ margin: 2 }}><Typography>Loading...</Typography></Box>;
    }

    return (
        <React.Fragment>
            <Box sx={{ margin: 2, mb: 2 }}>
                <Breadcrumbs homeLabel="Labels" homePath="/labels" items={[
                    { label: job.name },
                ]} />
            </Box>
            <Box sx={{ position: 'relative' }}>
                <Paper>
                    <Box sx={{ position: 'relative' }}>
                        <Box sx={{
                            margin: 1,
                            textAlign: 'center',
                            pb: 2,
                            px: { xs: 8, sm: 2 },
                            pt: { xs: 1.5, sm: 1 },
                        }}>
                            <IconButton
                                aria-label="job settings"
                                title="Job Settings"
                                onClick={(e) => setMenuAnchor(e.currentTarget)}
                                sx={{
                                    position: 'absolute',
                                    left: theme.spacing(1),
                                    top: theme.spacing(1),
                                }}
                            >
                                <MoreVertIcon />
                            </IconButton>
                            <Typography variant="h4" sx={{ fontSize: { xs: '1.75rem', sm: '2.125rem' } }}>
                                {job.name}
                            </Typography>
                        </Box>
                        <Box sx={{ margin: 2, pb: 2 }}>
                            <Grid container spacing={2}>
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>Configuration</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Label Format</Typography>
                                                    <Chip label={job.labelFormat} size="small" />
                                                </Box>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Algorithm</Typography>
                                                    <Typography variant="body2">{job.incrementAlgorithm}</Typography>
                                                </Box>
                                                {job.algorithmPrefix && (
                                                    <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                        <Typography variant="body2" color="text.secondary">Prefix</Typography>
                                                        <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>{job.algorithmPrefix}</Typography>
                                                    </Box>
                                                )}
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Suffix Length</Typography>
                                                    <Typography variant="body2">{job.algorithmSuffixLength}</Typography>
                                                </Box>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Next Index</Typography>
                                                    <Typography variant="body2">{job.lastGeneratedIndex}</Typography>
                                                </Box>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Total Generated</Typography>
                                                    <Typography variant="body2">{job.totalLabelsGenerated}</Typography>
                                                </Box>
                                                <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
                                                    <Typography variant="body2" color="text.secondary">Color Pattern</Typography>
                                                    <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>{job.codeColorPattern || '—'}</Typography>
                                                </Box>
                                            </Box>
                                        </CardContent>
                                    </Card>
                                </Grid>
                                <Grid size={{ xs: 12, md: 6 }}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant="h6" gutterBottom>Print</Typography>
                                            <Divider sx={{ mb: 2 }} />
                                            <Typography variant="body2" color="text.secondary" gutterBottom>
                                                Generate and print the next page of 12 labels ({job.labelFormat}).
                                                The job index will advance automatically.
                                            </Typography>
                                            <Box sx={{ mt: 2 }}>
                                                <Button
                                                    variant="contained"
                                                    startIcon={<PrintIcon />}
                                                    onClick={handlePrintNextPage}
                                                    disabled={loading}
                                                    size="large"
                                                >
                                                    {loading ? 'Generating...' : 'Print Next Page'}
                                                </Button>
                                            </Box>
                                        </CardContent>
                                    </Card>
                                </Grid>
                            </Grid>
                        </Box>
                    </Box>
                </Paper>
            </Box>
            <Menu
                anchorEl={menuAnchor}
                open={Boolean(menuAnchor)}
                onClose={() => setMenuAnchor(null)}
            >
                <MenuItem
                    component={Link}
                    to="edit"
                    onClick={() => setMenuAnchor(null)}
                >
                    <EditIcon sx={{ mr: 1 }} fontSize="small" />
                    Edit
                </MenuItem>
                <MenuItem onClick={handleDelete}>
                    <DeleteIcon sx={{ mr: 1 }} fontSize="small" />
                    Delete
                </MenuItem>
            </Menu>
        </React.Fragment>
    );
};
