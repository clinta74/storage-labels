import axios, { AxiosInstance } from 'axios';
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
        baseURL: `${CONFIG.API_URL}/api/`,
        timeout: 240 * 1000,
        responseType: 'json',
        headers: {
            Accept: 'application/json',
            'Content-Type': 'application/json'
        }
    });
};

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
    const { getAccessToken, isAuthenticated, authMode } = useAuth();
    const [Api, setApi] = React.useState<IApiContext>();

    React.useEffect(() => {
        const client = createAxiosInstance();
        
        client.interceptors.request.use(async config => {
            // Only add token for Local mode
            if (authMode === 'Local') {
                try {
                    const token = await getAccessToken();
                    if (token) {
                        config.headers['Authorization'] = `Bearer ${token}`;
                    }
                } catch (error) {
                    console.error('Failed to get access token:', error);
                }
            }
            // NoAuth mode: no token needed
            return config;
        });

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
    }, [isAuthenticated, authMode]);

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