import { AxiosInstance } from 'axios';

export interface SearchEndpoints {
    searchByQrCode: (code: string) => Promise<{data: SearchResultResponse}>;
    searchBoxesAndItems: (query: string, locationId?: string, boxId?: string) => Promise<{data: SearchResultsResponse}>;
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
        })
});
