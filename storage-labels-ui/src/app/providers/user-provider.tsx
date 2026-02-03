import React, { createContext, PropsWithChildren, useEffect, useState } from 'react';
import { useApi } from '../../api';
import { useAuth } from '../../auth/auth-provider';
import { useAlertMessage } from './alert-provider';

interface UserContext {
    user: UserResponse | undefined;
    updateUser: (options?: { silent?: boolean }) => Promise<void>;
}

export const UserContext = createContext<UserContext | null>(null);

export const UserProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [user, setUser] = useState<UserResponse>();
    const [loading, setLoading] = useState(false);
    const alert = useAlertMessage();
    const { isAuthenticated, authMode } = useAuth();
    const { Api } = useApi();

    const fetchUser = React.useCallback(async (silent = false) => {
        if (!isAuthenticated || authMode === 'None') {
            setUser(undefined);
            setLoading(false);
            return;
        }

        setLoading(true);
        try {
            const { data } = await Api.User.getUser();
            setUser(data);
        } catch (error) {
            if (!silent) {
                alert.addError(error);
            } else {
                console.warn('Failed to load user data:', error);
            }
        } finally {
            setLoading(false);
        }
    }, [Api, alert, authMode, isAuthenticated]);

    const updateUser = React.useCallback(async (options?: { silent?: boolean }) => {
        await fetchUser(options?.silent ?? false);
    }, [fetchUser]);

    useEffect(() => {
        if (authMode === 'None') {
            return;
        }

        if (!isAuthenticated) {
            if (user) {
                setUser(undefined);
            }
            setLoading(false);
            return;
        }

        if (user || loading) {
            return;
        }

        updateUser({ silent: true }).catch(() => { /* handled in updateUser */ });
    }, [isAuthenticated, authMode, user, loading, updateUser]);

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