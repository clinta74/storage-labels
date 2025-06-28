import { AxiosInstance } from 'axios';

export type BoxEndpoints = ReturnType<typeof getBoxEndpoints>;

export const getBoxEndpoints = (client: AxiosInstance) => ({
    getBoxes: (locationId: number) =>
        client.get<Box[]>(`box/location/${locationId}`),

    getBox: (boxId: number) =>
        client.get<Box>(`box/${boxId}`),

    createLocatio: (box: BoxRequest) =>
        client.post<Box>(`box`, box),

    updateLocation: (boxId: number, box: Box) =>
        client.put<Box>(`box/${boxId}`, box),

    deleteLocation: (boxId: number) =>
        client.delete<never>(`box/${boxId}`),
});