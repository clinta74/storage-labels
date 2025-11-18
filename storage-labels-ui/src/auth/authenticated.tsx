import React, { PropsWithChildren } from 'react';
import { useAuth } from './auth-provider';

interface AuthenticatedProps extends PropsWithChildren {
    invert?: boolean;
}

export const Authenticated: React.FunctionComponent<AuthenticatedProps> = ({ children, invert }) => {
    const { isAuthenticated, isLoading, authMode } = useAuth();
    
    // In NoAuth mode, always show content
    if (authMode === 'None') {
        return <React.Fragment>{children}</React.Fragment>;
    }

    const show: boolean = invert ? !(isAuthenticated && !isLoading) : (isAuthenticated && !isLoading);

    if (show) {
        return <React.Fragment>{children}</React.Fragment>
    }
    else {
        return null;
    }
}