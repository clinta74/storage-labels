import { useAuth0 } from '@auth0/auth0-react';
import axios, { AxiosInstance } from 'axios';
import React, { PropsWithChildren } from 'react';
import { CONFIG } from '../config';
import { UserEndpoints, getUserEndpoints } from './endpoints/user';
import { getNewUserEndpoints, NewUserEndpoints } from './endpoints/new-user';
import { getLocationEndpoints, LocationEndpoints } from './endpoints/location';
import { BoxEndpoints, getBoxEndpoints } from './endpoints/box';

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
    NewUser: NewUserEndpoints;
    User: UserEndpoints;
    Box: BoxEndpoints;
}

const ApiContext = React.createContext<{ Api: IApiContext } | null>(null);

export const ApiProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const { getAccessTokenSilently, isAuthenticated } = useAuth0();
    const [Api, setApi] = React.useState<IApiContext>();

    React.useEffect(() => {
        const client = createAxiosInstance();
        client.interceptors.request
            .use(async config => {
                const token = await getAccessTokenSilently();
                console.log(token);
                config.headers['Authorization'] = `Bearer ${token}`;
                return config;
            });

        const api: IApiContext = {
            Location: getLocationEndpoints(client),
            NewUser: getNewUserEndpoints(client),
            User: getUserEndpoints(client),
            Box: getBoxEndpoints(client),
        }

        setApi(api);
    }, [isAuthenticated]);

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