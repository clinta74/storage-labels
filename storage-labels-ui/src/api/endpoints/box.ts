import { AxiosInstance } from 'axios';

export type BoxEndpoints = ReturnType<typeof getBoxEndpoints>;

export const getBoxEndpoints = (client: AxiosInstance) => ({
    getBoxes: (locationId: number) =>
        client.get<Box[]>(`box/location/${locationId}`),

    getBox: (boxId: string) =>
        client.get<Box>(`box/${boxId}`),

    createBox: (box: BoxRequest) =>
        client.post<Box>(`box`, box),

    updateBox: (boxId: string, box: Box) =>
        client.put<Box>(`box/${boxId}`, box),

    moveBox: (boxId: string, destinationLocationId: number) =>
        client.put<Box>(`box/${boxId}/move`, { destinationLocationId }),

    deleteBox: (boxId: string, force?: boolean) =>
        client.delete<never>(`box/${boxId}${force ? '?force=true' : ''}`),
});