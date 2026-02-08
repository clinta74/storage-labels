import { AxiosInstance } from 'axios';

export interface SearchEndpoints {
    searchByQrCode: (code: string) => Promise<{data: SearchResultResponse}>;
    searchBoxesAndItems: (query: string, locationId?: string, boxId?: string) => Promise<{data: SearchResultsResponse}>;
    searchBoxesAndItemsV2: (
        query: string, 
        locationId?: string, 
        boxId?: string, 
        pageNumber?: number, 
        pageSize?: number
    ) => Promise<{data: SearchResultsResponseV2}>;
}

export const getSearchEndpoints = (client: AxiosInstance): SearchEndpoints => ({
    searchByQrCode: (code: string) =>
        client.get<SearchResultResponse>(`/search/qrcode/${encodeURIComponent(code)}`),

    searchBoxesAndItems: (query: string, locationId?: string, boxId?: string) =>
        client.get<SearchResultsResponse>('/search', {
            params: {
                query,
                locationId,
                boxId
            }
        }),

    searchBoxesAndItemsV2: (
        query: string, 
        locationId?: string, 
        boxId?: string, 
        pageNumber: number = 1, 
        pageSize: number = 20
    ) =>
        client.get<SearchResultsResponseV2>('/v2/search', {
            params: {
                query,
                locationId,
                boxId,
                pageNumber,
                pageSize
            }
        })
});
