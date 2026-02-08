import axios, { AxiosHeaders, AxiosInstance, AxiosRequestHeaders, InternalAxiosRequestConfig } from 'axios';
import React, { PropsWithChildren } from 'react';
import { CONFIG } from '../config';
import { useAuth } from '../auth/auth-provider';
import { UserEndpoints, getUserEndpoints } from './endpoints/user';
import { getLocationEndpoints, LocationEndpoints } from './endpoints/location';
import { BoxEndpoints, getBoxEndpoints } from './endpoints/box';
import { ItemEndpoints, getItemEndpoints } from './endpoints/item';
import { ImageEndpoints, getImageEndpoints } from './endpoints/image';
import { CommonLocationEndpoints, getCommonLocationEndpoints } from './endpoints/common-location';
import { SearchEndpoints, getSearchEndpoints } from './endpoints/search';
import { EncryptionKeyEndpoints, getEncryptionKeyEndpoints } from './endpoints/encryption-key';

const createAxiosInstance = (): AxiosInstance => {
    return axios.create({
        baseURL: `${CONFIG.API_URL}/api/v2/`,
        timeout: 240 * 1000,
        responseType: 'json',
        withCredentials: true,
        headers: {
            Accept: 'application/json',
            'Content-Type': 'application/json'
        }
    });
};

const applyAuthorizationHeader = (headers: AxiosRequestHeaders | undefined, token: string): AxiosRequestHeaders => {
    let nextHeaders = headers ?? new AxiosHeaders();

    if (nextHeaders instanceof AxiosHeaders) {
        nextHeaders.set('Authorization', `Bearer ${token}`);
        return nextHeaders;
    }

    (nextHeaders as Record<string, string>)['Authorization'] = `Bearer ${token}`;
    return nextHeaders;
};

type RetriableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean };

interface IApiContext {
    Location: LocationEndpoints;
    User: UserEndpoints;
    Box: BoxEndpoints;
    Item: ItemEndpoints;
    Image: ImageEndpoints;
    CommonLocation: CommonLocationEndpoints;
    Search: SearchEndpoints;
    EncryptionKey: EncryptionKeyEndpoints;
    getAccessToken: () => Promise<string>;
}

const ApiContext = React.createContext<{ Api: IApiContext } | null>(null);

export const ApiProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const { getAccessToken, authMode, refreshAccessToken, handleSessionExpired } = useAuth();
    const [Api, setApi] = React.useState<IApiContext>();
    const refreshPromiseRef = React.useRef<Promise<string> | null>(null);
    const authModeRef = React.useRef(authMode);
    const getAccessTokenRef = React.useRef(getAccessToken);
    const refreshAccessTokenRef = React.useRef(refreshAccessToken);
    const handleSessionExpiredRef = React.useRef(handleSessionExpired);

    React.useEffect(() => {
        authModeRef.current = authMode;
    }, [authMode]);

    React.useEffect(() => {
        getAccessTokenRef.current = getAccessToken;
    }, [getAccessToken]);

    React.useEffect(() => {
        refreshAccessTokenRef.current = refreshAccessToken;
    }, [refreshAccessToken]);

    React.useEffect(() => {
        handleSessionExpiredRef.current = handleSessionExpired;
    }, [handleSessionExpired]);

    React.useEffect(() => {
        const client = createAxiosInstance();
        
        const requestInterceptor = client.interceptors.request.use(async config => {
            // Only add token for Local mode
            if (authModeRef.current === 'Local') {
                try {
                    const token = await getAccessTokenRef.current();
                    if (token) {
                        config.headers = applyAuthorizationHeader(config.headers, token);
                    }
                } catch (error) {
                    console.warn('Failed to get access token:', error);
                }
            }
            // NoAuth mode: no token needed
            return config;
        });

        const responseInterceptor = client.interceptors.response.use(
            response => response,
            async error => {
                const status = error?.response?.status;
                const originalRequest: RetriableRequestConfig = (error?.config ?? {}) as RetriableRequestConfig;

                const isAuthRequest = typeof originalRequest?.url === 'string'
                    && (/\/auth\//.test(originalRequest.url));

                if (
                    authModeRef.current !== 'Local'
                    || status !== 401
                    || isAuthRequest
                    || originalRequest._retry
                ) {
                    return Promise.reject(error);
                }

                originalRequest._retry = true;

                try {
                    if (!refreshPromiseRef.current) {
                        refreshPromiseRef.current = refreshAccessTokenRef.current();
                    }

                    await refreshPromiseRef.current;
                    refreshPromiseRef.current = null;

                    const token = await getAccessTokenRef.current();
                    if (token) {
                        originalRequest.headers = applyAuthorizationHeader(originalRequest.headers, token);
                    }

                    return client(originalRequest);
                }
                catch (refreshError) {
                    refreshPromiseRef.current = null;
                    await handleSessionExpiredRef.current();
                    return Promise.reject(refreshError);
                }
            }
        );

        const api: IApiContext = {
            Location: getLocationEndpoints(client),
            User: getUserEndpoints(client),
            Box: getBoxEndpoints(client),
            Item: getItemEndpoints(client),
            Image: getImageEndpoints(client),
            CommonLocation: getCommonLocationEndpoints(client),
            Search: getSearchEndpoints(client),
            EncryptionKey: getEncryptionKeyEndpoints(client),
            getAccessToken,
        }

        setApi(api);

        return () => {
            client.interceptors.request.eject(requestInterceptor);
            client.interceptors.response.eject(responseInterceptor);
        };
    }, []);

    return (
        <React.Fragment>
            {
                Api && <ApiContext.Provider value={{ Api }}>{children}</ApiContext.Provider>
            }
        </React.Fragment>
    );
}

export const useApi = () => {
    const context = React.useContext(ApiContext)
    if (context === null) throw new Error('useApi must be used within a ApiProvider');
    return context;
}