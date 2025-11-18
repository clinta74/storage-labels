import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApi } from '../../api';
import { useAuth } from '../../auth/auth-provider';
import { useAlertMessage } from './alert-provider';

interface UserContext {
    user: UserResponse,
    updateUser: () => void;
}

export const UserContext = createContext<UserContext | null>(null);

export const UserProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [user, setUser] = useState<UserResponse>();
    const [loading, setLoading] = useState(false);
    const alert = useAlertMessage();
    const navigate = useNavigate();
    const { isAuthenticated, authMode } = useAuth();
    const { Api } = useApi();

    useEffect(() => {
        if (authMode === 'None') {
            // In NoAuth mode, skip user checks
            return;
        }

        if (isAuthenticated && !user && !loading) {
            setLoading(true);
            Api.User.getUser()
                .then(({ data }) => {
                    setUser(data);
                })
                .catch(error => {
                    console.warn('Failed to load user data:', error);
                    // Don't show error for initial load - might be timing issue
                })
                .finally(() => {
                    setLoading(false);
                });
        }
    }, [isAuthenticated, authMode, user, loading]);

    const updateUser = () => {
        Api.User.getUser()
            .then(({ data }) => {
                setUser(data);
            })
            .catch(error => alert.addError(error));
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