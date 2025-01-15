import { AxiosInstance } from 'axios';

export type UserEndpoints = ReturnType<typeof getUserEndpoints>;

export const getUserEndpoints = (client: AxiosInstance) => ({
    getUser: () =>
        client.get<User>(`user`),

    getUserExists: () =>
        client.get<boolean>(`user/exists`),

    updateUser: (user: User) =>
        client.patch(`user`, user),

    createUser: (user: NewUser) =>
        client.post<User>(`user`, user),
});