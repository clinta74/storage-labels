import { Box, Button, CircularProgress, FormControl, Grid, Paper, TextField, Typography } from '@mui/material';
import { AxiosError } from 'axios';
import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useApi } from '../../../api';
import { validateAll, ValidationTest } from '../../../utils/validate';
import { useAlertMessage } from '../../providers/alert-provider';
import { ErrorMessage } from '../error-message';

const validationTests: ValidationTest<NewUser>[] =
    [
        {
            passCondition: ({ firstName }) => firstName.trim().length > 0,
            result: {
                message: 'A first name is required.',
                name: 'firstName',
            }
        },
        {
            passCondition: ({ lastName }) => lastName.trim().length > 0,
            result: {
                message: 'A last name is required.',
                name: 'lastName',
            }
        },
    ];

export const NewUser: React.FC = () => {
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const { Api } = useApi();

    const [newUser, setNewUser] = useState<NewUser>({
        firstName: '',
        lastName: '',
    });
    const [emailAddress, setEmailAddress] = useState<string>('');
    const [isSubmitted, setIsSubmitted] = useState(false);
    const [postingNewUser, setPostingNewUser] = useState(false);

    useEffect(() => {
        Api.NewUser.getNewUser()
            .then(({ data }) => {
                setNewUser({
                    firstName: data.firstName || '',
                    lastName: data.lastName || '',
                });
                setEmailAddress(data.emailAddress || '');
            })
            .catch(error => alert.addMessage(error));
    }, []);

    const onChangeStringField: React.ChangeEventHandler<HTMLInputElement> = event => {
        const { name, value } = event.target;

        setNewUser(user => ({
            ...user,
            [name]: value
        }));
    }

    const createNewUser = () => {
        const [valid] = validateAll(validationTests, newUser);
        setIsSubmitted(true);
        if (!postingNewUser && valid) {
            setPostingNewUser(true);
            Api.User.createUser(newUser)
                .then(() => {
                    navigate(`/`);
                })
                .catch((error: AxiosError) => {
                    alert.addMessage(error.message)
                    setPostingNewUser(false);
                });
        }
    }

    const [isValid, results] = validateAll(validationTests, newUser);
    const showErrors = isSubmitted && !isValid;

    const hasErrors = (inputName: keyof NewUser) => results
        .filter(({ name }) => inputName === name)
        .length > 0;

    return (
        <Grid container justifyContent="center">
            <Grid size={{ xs: 12, md: 10, xl: 8 }}>
                <Paper elevation={4}>
                    <Box padding={2} mt={4}>
                        <Box mb={2}>
                            <Typography variant="h4">Create User</Typography>
                            <p>To be able to access your daily tracking you must first register.</p>
                        </Box>
                        <form noValidate autoComplete="off">
                            <Grid container justifyContent="center" alignItems="stretch" spacing={2}>
                                <Grid size={{ xs: 6 }}>
                                    <FormControl fullWidth>
                                        <TextField error={showErrors && hasErrors('firstName')} label="First Name" id="firstName" name="firstName" value={newUser.firstName} onChange={onChangeStringField} disabled={postingNewUser} required />
                                        <ErrorMessage isSubmitted={isSubmitted} inputName="firstName" results={results} />
                                    </FormControl>
                                </Grid>
                                <Grid size={{ xs: 6 }}>
                                    <FormControl fullWidth>
                                        <TextField error={showErrors && hasErrors('lastName')} label="Last Name" id="lastName" name="lastName" value={newUser.lastName} onChange={onChangeStringField} disabled={postingNewUser} required />
                                        <ErrorMessage isSubmitted={isSubmitted} inputName="lastName" results={results} />
                                    </FormControl>
                                </Grid>

                                <Grid size={{ xs: 12 }}>
                                    <FormControl fullWidth>
                                        <TextField 
                                            label="Email Address" 
                                            id="emailAddress" 
                                            name="emailAddress" 
                                            value={emailAddress} 
                                            disabled={true}
                                            slotProps={{
                                                input: {
                                                    readOnly: true,
                                                }
                                            }}
                                        />
                                    </FormControl>
                                </Grid>

                                <Grid size={{ xs: 12 }}>
                                    <em>* Required fields.</em>
                                </Grid>
                            </Grid>
                        </form>

                        <Box display="flex" justifyContent="flex-end" mt={2}>
                            <Box display="flex" alignItems="center">
                                <Box mr={1}>
                                    <Button color="primary" onClick={createNewUser} disabled={postingNewUser}>Create</Button>
                                    {postingNewUser && <CircularProgress size={24}></CircularProgress>}
                                </Box>
                                <Link to="/locations">
                                    <Button color="secondary" disabled={postingNewUser}>Cancel</Button>
                                </Link>
                            </Box>
                        </Box>
                    </Box>
                </Paper>
            </Grid>
        </Grid >
    );
}