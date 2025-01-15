import React, { PropsWithChildren } from 'react';
import { useNavigate } from 'react-router';
import { AppState, Auth0Provider } from '@auth0/auth0-react';
import { AUTH0 } from '../config';

export const Auth0ProviderWithHistory: React.FC<PropsWithChildren> = ({ children }) => {
    const navigate = useNavigate();

    const onRedirectCallback = async (appState?: AppState) => {
        navigate(appState?.returnTo || window.location.pathname);
    };

    return (
        <Auth0Provider
            redirectUri={window.location.origin}
            onRedirectCallback={onRedirectCallback}
            {...AUTH0}
        >
            {children}
        </Auth0Provider>
    );
};