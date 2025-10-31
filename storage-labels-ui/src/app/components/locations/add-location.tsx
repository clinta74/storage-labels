import { Autocomplete, Box, Button, FormControl, Paper, Stack, TextField, Typography } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { ErrorMessage } from '../error-message';
import { validateAll } from '../../../utils/validate';
import { validationTests } from './validation-test';
import { useApi } from '../../../api';
import { AxiosError } from 'axios';
// import { useAlertMessage } from '../../providers/alert-provider';

export const AddLocation: React.FC = () => {
    const navigate = useNavigate();
    // const alert = useAlertMessage();
    const [location, setLocation] = useState<StorageLocation>({
        locationId: 0,
        name: '',
        created: '',
        updated: '',
        accessLevel: 'None'
    });
    const { Api } = useApi();
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [postingJob, setPostingJob] = useState(false);
    const [commonLocations, setCommonLocations] = useState<CommonLocation[]>([]);

    useEffect(() => {
        Api.CommonLocation.getCommonLocations()
            .then(({ data }) => {
                setCommonLocations(data);
            })
            .catch((error) => console.error('Error loading common locations:', error));
    }, []);

    const addLocation: React.MouseEventHandler<HTMLButtonElement> = () => {
        const [valid] = validateAll(validationTests, location);
        setIsSubmitted(true);
        if (!postingJob && valid) {
            setPostingJob(true);
            Api.Location.createLocation(location)
                .then(() => {
                    navigate(`..`);
                })
                .catch((error: AxiosError) => alert(error.message))
                .finally(() => setPostingJob(false));
        }
    }


    const [isValid, results] = validateAll(validationTests, location);
    const showErrors = isSubmitted && !isValid;

    const hasErrors = (inputName: keyof StorageLocation) => results
        .filter(({ name }) => inputName === name)
        .length > 0;

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant='h4'>Add Location</Typography>
                    </Box>
                    <Box margin={2}>
                        <FormControl fullWidth>
                            <Autocomplete
                                freeSolo
                                options={commonLocations.map((cl) => cl.name)}
                                value={location.name}
                                onChange={(event, newValue) => {
                                    setLocation({
                                        ...location,
                                        name: newValue || ''
                                    });
                                }}
                                onInputChange={(event, newInputValue) => {
                                    setLocation({
                                        ...location,
                                        name: newInputValue
                                    });
                                }}
                                renderInput={(params) => (
                                    <TextField
                                        {...params}
                                        variant="standard"
                                        label="Name"
                                        error={showErrors && hasErrors('name')}
                                        disabled={postingJob}
                                        required
                                    />
                                )}
                            />
                            <ErrorMessage isSubmitted={isSubmitted} inputName="name" results={results} />
                        </FormControl>
                    </Box>
                    <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                        <Button color="primary" onClick={addLocation} loading={postingJob}>Add</Button>
                        <Button color="secondary" component={Link} to="..">Cancel</Button>
                    </Stack>
                </Paper>
            </Box>
        </React.Fragment>
    )
}