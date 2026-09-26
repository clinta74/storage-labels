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
    // A ref rather than state: it only guards against duplicate fetches, and as an effect dependency
    // it made a failed fetch retry immediately, over and over
    const fetchingRef = React.useRef(false);
    const alert = useAlertMessage();
    const { isAuthenticated, authMode } = useAuth();
    const { Api } = useApi();

    const signedIn = isAuthenticated && authMode !== 'None';

    // Forget the user on sign-out. Adjusted during render rather than in an effect, per
    // https://react.dev/learn/you-might-not-need-an-effect#adjusting-some-state-when-a-prop-changes
    const [wasSignedIn, setWasSignedIn] = useState(signedIn);
    if (signedIn !== wasSignedIn) {
        setWasSignedIn(signedIn);
        if (!signedIn) {
            setUser(undefined);
        }
    }

    const fetchUser = React.useCallback((silent = false) => {
        if (!signedIn) {
            return Promise.resolve();
        }

        fetchingRef.current = true;
        return Api.User.getUser()
            .then(({ data }) => setUser(data))
            .catch(error => {
                if (!silent) {
                    alert.addError(error);
                } else {
                    console.warn('Failed to load user data:', error);
                }
            })
            .finally(() => {
                fetchingRef.current = false;
            });
    }, [Api, alert, signedIn]);

    const updateUser = React.useCallback(async (options?: { silent?: boolean }) => {
        await fetchUser(options?.silent ?? false);
    }, [fetchUser]);

    useEffect(() => {
        if (!signedIn || user || fetchingRef.current) {
            return;
        }

        updateUser({ silent: true }).catch(() => { /* handled in updateUser */ });
    }, [signedIn, user, updateUser]);

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