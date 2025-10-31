import { AxiosInstance } from 'axios';

export type NewUserEndpoints = ReturnType<typeof getNewUserEndpoints>;

export const getNewUserEndpoints = (client: AxiosInstance) => ({
    getNewUser: () =>
        client.get<NewUserResponse>(`new-user`),
});