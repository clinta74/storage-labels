import { AxiosInstance } from 'axios';

export type UserEndpoints = ReturnType<typeof getUserEndpoints>;

export const getUserEndpoints = (client: AxiosInstance) => ({
    getUser: () =>
        client.get<UserResponse>(`user`),

    getUserExists: () =>
        client.get<boolean>(`user/exists`),

    updateUser: (user: UserResponse) =>
        client.patch(`user`, user),

    createUser: (user: NewUser) =>
        client.post<UserResponse>(`user`, user),
});