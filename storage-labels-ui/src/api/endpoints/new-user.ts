import { AxiosInstance } from 'axios';

export type NewUserEndpoints = ReturnType<typeof getNewUserEndpoints>;

export const getNewUserEndpoints = (client: AxiosInstance) => ({
    createUser: (user: User) =>
        client.post<UserId>(`new-user/create`, user),

    addNewUser: (newUser: NewUser) =>
        client.post<UserId>(`new-user`, newUser),

    getNewUser: () =>
        client.get<NewUser>(`new-user`),
});