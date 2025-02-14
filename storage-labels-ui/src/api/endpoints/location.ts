import { AxiosInstance } from 'axios';

export type LocationEndpoints = ReturnType<typeof getLocationEndpoints>;

export const getLocationEndpoints = (client: AxiosInstance) => ({
    getLocaions: () =>
        client.get<Location[]>(`location`),

    getLocation: (locationId: number) =>
        client.get<Location>(`location/${locationId}`),

    createLocation: (location: LocationRequest) =>
        client.post<Location>(`location`, location),

    updateLocation: (locationId: number, location: Location) =>
        client.put<Location>(`location/${locationId}`, location),

    deleteLocation: (locationId: number) =>
        client.delete<never>(`location/${locationId}`),

});