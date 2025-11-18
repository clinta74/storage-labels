import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { useApi } from '../../api';
import { useAuth } from '../../auth/auth-provider';
import { useAlertMessage } from './alert-provider';

interface UserContext {
    user: UserResponse | undefined,
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
            // In NoAuth mode, skip user checks - render immediately
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

    // Always render children - the context value will have undefined user if not loaded yet
    // Components that need user data should check if user is defined
    return (
        <UserContext.Provider value={{ user, updateUser }}>
            {children}
        </UserContext.Provider>
    );
}

export const useUser = () => {
    const context = React.useContext(UserContext)
    if (context === null) throw new Error('useUser must be used within a UserProvider');
    return context;
}