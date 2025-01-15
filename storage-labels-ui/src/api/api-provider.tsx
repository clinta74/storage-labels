import { useAuth0 } from '@auth0/auth0-react';
import axios, { AxiosInstance } from 'axios';
import React, { PropsWithChildren } from 'react';
import { CONFIG } from '../config';
import { UserEndpoints, getUserEndpoints } from './endpoints/user';
import { getNewUserEndpoints, NewUserEndpoints } from './endpoints/new-user';

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
    NewUser: NewUserEndpoints;
    User: UserEndpoints;
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
            NewUser: getNewUserEndpoints(client),
            User: getUserEndpoints(client),
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