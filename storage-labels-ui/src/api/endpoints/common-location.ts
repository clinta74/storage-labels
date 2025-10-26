import { AxiosInstance } from 'axios';

export type CommonLocationEndpoints = ReturnType<typeof getCommonLocationEndpoints>;

export const getCommonLocationEndpoints = (client: AxiosInstance) => ({
    createCommonLocation: (request: CommonLocationRequest) =>
        client.post<CommonLocation>('common-location', request),

    getCommonLocations: () =>
        client.get<CommonLocation[]>('common-location'),

    deleteCommonLocation: (commonLocationId: number) =>
        client.delete(`common-location/${commonLocationId}`),
});
