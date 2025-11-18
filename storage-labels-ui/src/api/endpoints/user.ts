import { AxiosInstance } from 'axios';

export type UserEndpoints = ReturnType<typeof getUserEndpoints>;

export const getUserEndpoints = (client: AxiosInstance) => ({
    getUser: () =>
        client.get<UserResponse>(`user`),

    updateUser: (user: UserResponse) =>
        client.patch(`user`, user),

    getUserPreferences: () =>
        client.get<UserPreferences>(`user/preferences`),

    updateUserPreferences: (preferences: UserPreferences) =>
        client.put<UserPreferences>(`user/preferences`, preferences),

    getAllUsers: () =>
        client.get<UserWithRoles[]>(`user/all`),

    updateUserRole: (userId: string, role: string) =>
        client.put(`user/${userId}/role`, { role }),

    adminResetPassword: (userId: string, newPassword: string) =>
        client.post(`auth/admin/reset-password`, { userId, newPassword }),

    deleteUser: (userId: string) =>
        client.delete(`user/${userId}`),
});