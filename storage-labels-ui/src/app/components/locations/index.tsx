import { Box } from '@mui/material';
import React, { useEffect, useState } from 'react';
import { useApi } from '../../../api';
import { useAuth0 } from '@auth0/auth0-react';
import { useAlertMessage } from '../../providers/alert-provider';
import { useNavigate } from 'react-router';

export const Locations: React.FC = () => {
    const { Api } = useApi();
    const { isAuthenticated } = useAuth0();
    const alert = useAlertMessage();
    const navigate = useNavigate();

    const [currentUser, setCurrentUser] = useState<CurrentUser>();

    useEffect(() => {
        if (isAuthenticated) {
            Api.User.getUserExists()
                .then(({ data }) => {
                    if (data) {
                        Api.User.getUser()
                            .then(({ data }) => {
                                setCurrentUser(data);
                            })
                            .catch(error => alert.addMessage(error));
                    }
                    else {
                        navigate('/new-user');
                    }
                })
                .catch(error => alert.addMessage(error));
        }
    }, [isAuthenticated]);

    return (
        <Box>Showing Locations for {currentUser?.firstName}</Box>
    );
}