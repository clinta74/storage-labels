import { 
    Autocomplete, 
    Box, 
    Button, 
    FormControl, 
    Paper, 
    Stack, 
    TextField, 
    Typography,
} from '@mui/material';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';
import { ErrorMessage } from '../error-message';
import { validateAll } from '../../../utils/validate';
import { validationTests } from './validation-test';
import { useApi } from '../../../api';
import { useAlertMessage } from '../../providers/alert-provider';
import { AxiosError } from 'axios';

type Params = Record<'locationId', string>;

export const EditLocation: React.FC = () => {
    const params = useParams<Params>();
    const navigate = useNavigate();
    const alert = useAlertMessage();
    const [location, setLocation] = useState<StorageLocation | null>(null);
    const [name, setName] = useState('');
    const { Api } = useApi();
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [saving, setSaving] = useState(false);
    const [commonLocations, setCommonLocations] = useState<CommonLocation[]>([]);

    useEffect(() => {
        Api.CommonLocation.getCommonLocations()
            .then(({ data }) => {
                setCommonLocations(data);
            })
            .catch((error) => console.error('Error loading common locations:', error));
    }, []);

    useEffect(() => {
        const locationId = Number(params.locationId);
        if (locationId) {
            Api.Location.getLocation(locationId)
                .then(({ data }) => {
                    setLocation(data);
                    setName(data.name);
                })
                .catch((error) => alert.addMessage(error));
        }
    }, [params]);

    const handleSave: React.MouseEventHandler<HTMLButtonElement> = () => {
        setIsSubmitted(true);
        const updatedLocation: StorageLocation = {
            ...location!,
            name,
        };
        const [valid] = validateAll(validationTests, updatedLocation);
        
        if (!saving && valid && location) {
            setSaving(true);
            Api.Location.updateLocation(location.locationId, updatedLocation as any)
                .then(() => {
                    navigate(`..`);
                })
                .catch((error: AxiosError) => alert.addMessage(error.message))
                .finally(() => setSaving(false));
        }
    };

    if (!location) {
        return null;
    }

    const [isValid, results] = validateAll(validationTests, { ...location, name });
    const showErrors = isSubmitted && !isValid;

    const hasErrors = (inputName: keyof StorageLocation) => results
        .filter(({ name }) => inputName === name)
        .length > 0;

    return (
        <React.Fragment>
            <Box>
                <Paper>
                    <Box margin={1} textAlign="center">
                        <Typography variant='h4'>Edit Location</Typography>
                    </Box>
                    <Box margin={2}>
                        <FormControl fullWidth>
                            <Autocomplete
                                freeSolo
                                options={commonLocations.map((cl) => cl.name)}
                                value={name}
                                onChange={(event, newValue) => {
                                    setName(newValue || '');
                                }}
                                onInputChange={(event, newInputValue) => {
                                    setName(newInputValue);
                                }}
                                renderInput={(params) => (
                                    <TextField
                                        {...params}
                                        variant="standard"
                                        label="Name"
                                        error={showErrors && hasErrors('name')}
                                        disabled={saving}
                                        required
                                    />
                                )}
                            />
                            <ErrorMessage isSubmitted={isSubmitted} inputName="name" results={results} />
                        </FormControl>
                    </Box>
                    <Stack direction="row" spacing={2} padding={2} justifyContent="right">
                        <Button color="primary" onClick={handleSave} disabled={saving}>Save</Button>
                        <Button color="secondary" component={Link} to="..">Cancel</Button>
                    </Stack>
                </Paper>
            </Box>
        </React.Fragment>
    );
};
