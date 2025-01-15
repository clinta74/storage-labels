import { AxiosInstance } from 'axios';

export type UserEndpoints = ReturnType<typeof getUserEndpoints>;

export const getUserEndpoints = (client: AxiosInstance) => ({
    getUser: () =>
        client.get<CurrentUser>(`user`),

    getUserExists: () =>
        client.get<boolean>(`user/exists`),

    updateUser: (user: CurrentUser) =>
        client.patch(`user`, user),
});