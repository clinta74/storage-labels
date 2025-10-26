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

    deleteBox: (boxId: string) =>
        client.delete<never>(`box/${boxId}`),
});