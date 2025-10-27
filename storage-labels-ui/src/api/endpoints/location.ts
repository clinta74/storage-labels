import { AxiosInstance } from 'axios';

export type LocationEndpoints = ReturnType<typeof getLocationEndpoints>;

export const getLocationEndpoints = (client: AxiosInstance) => ({
    getLocaions: () =>
        client.get<StorageLocation[]>(`location`),

    getLocation: (locationId: number) =>
        client.get<StorageLocation>(`location/${locationId}`),

    createLocation: (location: LocationRequest) =>
        client.post<StorageLocation>(`location`, location),

    updateLocation: (locationId: number, location: Location) =>
        client.put<StorageLocation>(`location/${locationId}`, location),

    deleteLocation: (locationId: number) =>
        client.delete<never>(`location/${locationId}`),

    // User access management
    getLocationUsers: (locationId: number) =>
        client.get<UserLocationResponse[]>(`location/${locationId}/users`),

    addUserToLocation: (locationId: number, request: AddUserLocationRequest) =>
        client.post<UserLocationResponse>(`location/${locationId}/users`, request),

    updateUserLocationAccess: (locationId: number, userId: string, request: UpdateUserLocationRequest) =>
        client.put<UserLocationResponse>(`location/${locationId}/users/${userId}`, request),

    removeUserFromLocation: (locationId: number, userId: string) =>
        client.delete(`location/${locationId}/users/${userId}`),
});