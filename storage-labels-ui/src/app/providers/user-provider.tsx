import { useAuth0 } from '@auth0/auth0-react';
import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApi } from '../../api';
import { useAlertMessage } from './alert-provider';

interface UserContext {
    user: UserResponse,
    updateUser: () => void;
}

export const UserContext = createContext<UserContext | null>(null);

export const UserProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [user, setUser] = useState<UserResponse>();
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const { isAuthenticated } = useAuth0();
    const { Api } = useApi();

    useEffect(() => {
        if (isAuthenticated) {
            Api.User.getUserExists()
                .then(({ data }) => {
                    if (data) {
                        Api.User.getUser()
                            .then(({ data }) => {
                                setUser(data);
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

    const updateUser = () => {
        Api.User.getUserExists()
            .then(({ data }) => {
                if (data) {
                    Api.User.getUser()
                        .then(({ data }) => {
                            setUser(data);
                        })
                        .catch(error => alert.addMessage(error));
                }
                else {
                    navigate('/new-user');
                }
            })
            .catch(error => alert.addMessage(error));
    }

    return (
        <React.Fragment>
            {
                user && <UserContext.Provider value={{ user, updateUser }}>{children}</UserContext.Provider>
            }
        </React.Fragment>
    );
}

export const useUser = () => {
    const context = React.useContext(UserContext)
    if (context === null) throw new Error('useUser must be used within a UserProvider');
    return context;
}