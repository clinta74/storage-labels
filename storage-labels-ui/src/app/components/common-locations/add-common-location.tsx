import React, { useState, useEffect } from 'react';
import { Box, Button, FormControl, Paper, Stack, TextField, Typography } from '@mui/material';
import { Link, useNavigate } from 'react-router';
import { useUserPermission } from '../../providers/user-permission-provider';
import { Permissions } from '../../constants/permissions';
import { useApi } from '../../../api';
import { AxiosError } from 'axios';
import { useAlertMessage } from '../../providers/alert-provider';

export const AddCommonLocation: React.FC = () => {
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const { hasPermission } = useUserPermission();
    const { Api } = useApi();
    const [name, setName] = useState('');
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [postingJob, setPostingJob] = useState(false);

    const canWrite = hasPermission(Permissions.Write_CommonLocations);

    useEffect(() => {
        if (!canWrite) {
            navigate('/', { replace: true });
        }
    }, [canWrite, navigate]);

    const addCommonLocation: React.MouseEventHandler<HTMLButtonElement> = () => {
        setIsSubmitted(true);
        const isValid = name.trim().length > 0;
        
        if (!postingJob && isValid && canWrite) {
            setPostingJob(true);
            Api.CommonLocation.createCommonLocation({ name })
                .then(() => {
                    navigate('..');
                })
                .catch((error: AxiosError) => alert.addMessage(error.message))
                .finally(() => setPostingJob(false));
        }
    };

    if (!canWrite) {
        return null;
    }

    const hasError = isSubmitted && name.trim().length === 0;

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant="h4">Add Common Location</Typography>
                    </Box>
                    <Box margin={2}>
                        <FormControl fullWidth>
                            <TextField
                                variant="standard"
                                error={hasError}
                                label="Name"
                                id="name"
                                name="name"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                disabled={postingJob}
                                required
                                helperText={hasError ? 'Name is required' : ''}
                            />
                        </FormControl>
                    </Box>
                    <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                        <Button color="primary" onClick={addCommonLocation} disabled={postingJob}>
                            Add
                        </Button>
                        <Button color="secondary" component={Link} to="..">
                            Cancel
                        </Button>
                    </Stack>
                </Paper>
            </Box>
        </React.Fragment>
    );
};
