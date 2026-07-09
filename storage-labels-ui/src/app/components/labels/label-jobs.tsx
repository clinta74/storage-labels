import React, { useEffect, useState } from 'react';
import {
    Avatar,
    Box,
    Fab,
    List,
    ListItem,
    ListItemAvatar,
    ListItemButton,
    ListItemText,
    Paper,
    Typography,
    useTheme,
} from '@mui/material';
import AddIcon from '@mui/icons-material/Add';
import LabelIcon from '@mui/icons-material/LocalOffer';
import { Link, useNavigate } from 'react-router';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { Breadcrumbs, EmptyState } from '../shared';

export const LabelJobsPage: React.FC = () => {
    const navigate = useNavigate();
    const { Api } = useApi();
    const alert = useAlertMessage();
    const theme = useTheme();
    const [jobs, setJobs] = useState<LabelPrintJobResponse[]>([]);

    const loadJobs = () => {
        Api.Label.getLabelJobs()
            .then(({ data }) => setJobs(data))
            .catch(() => alert.addMessage('Failed to load label jobs.'));
    };

    useEffect(() => {
        loadJobs();
    }, []);

    return (
        <React.Fragment>
            <Box sx={{ margin: 2, mb: 2 }}>
                <Breadcrumbs homeLabel="Labels" homePath="/labels" items={[]} />
            </Box>
            <Box sx={{ position: 'relative' }}>
                <Box
                    sx={{
                        position: 'absolute',
                        right: theme.spacing(1),
                        top: theme.spacing(1),
                        zIndex: 1,
                    }}
                >
                    <Fab
                        color="primary"
                        aria-label="Create label job"
                        component={Link}
                        to="create"
                    >
                        <AddIcon />
                    </Fab>
                </Box>
                <Paper>
                    <Box
                        sx={{
                            margin: 1,
                            textAlign: 'center',
                            px: { xs: 8, sm: 2 },
                            pt: { xs: 1.5, sm: 1 },
                        }}
                    >
                        <Typography variant="h4" sx={{ fontSize: { xs: '1.75rem', sm: '2.125rem' } }}>
                            Label Print Jobs
                        </Typography>
                    </Box>
                    <Box sx={{ margin: 2 }}>
                        {jobs.length === 0 ? (
                            <EmptyState
                                icon={LabelIcon}
                                title="No label jobs yet"
                                message="Create a label job to start generating and printing labels for your storage boxes."
                                actionLabel="Create Label Job"
                                onAction={() => navigate('create')}
                            />
                        ) : (
                            <List>
                                {jobs.map(job => (
                                    <ListItem key={job.id}>
                                        <ListItemButton component={Link} to={job.id}>
                                            <ListItemAvatar>
                                                <Avatar>
                                                    <LabelIcon />
                                                </Avatar>
                                            </ListItemAvatar>
                                            <ListItemText
                                                primary={job.name}
                                                secondary={[
                                                    job.labelFormat,
                                                    job.incrementAlgorithm,
                                                    job.algorithmPrefix,
                                                    `${job.totalLabelsGenerated} generated`,
                                                ].filter(Boolean).join(' · ')}
                                            />
                                        </ListItemButton>
                                    </ListItem>
                                ))}
                            </List>
                        )}
                    </Box>
                </Paper>
            </Box>
        </React.Fragment>
    );
};
