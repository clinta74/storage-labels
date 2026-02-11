import { AxiosInstance } from 'axios';

export interface SearchEndpoints {
    searchByQrCode: (code: string) => Promise<{data: SearchResultResponse}>;
    searchBoxesAndItems: (
        query: string, 
        locationId?: string, 
        boxId?: string, 
        pageNumber?: number, 
        pageSize?: number
    ) => Promise<{data: SearchResultResponse[]; totalCount: number; totalPages: number}>;
}

export const getSearchEndpoints = (client: AxiosInstance): SearchEndpoints => ({
    searchByQrCode: (code: string) =>
        client.get<SearchResultResponse>(`/search/qrcode/${encodeURIComponent(code)}`),

    searchBoxesAndItems: async (
        query: string, 
        locationId?: string, 
        boxId?: string, 
        pageNumber: number = 1, 
        pageSize: number = 10
    ) => {
        const response = await client.get<SearchResultResponse[]>('/search', {
            params: {
                query,
                locationId,
                boxId,
                pageNumber,
                pageSize
            }
        });
        
        // Extract total count from header and calculate total pages
        const totalCount = parseInt(response.headers['x-total-count'] || '0', 10);
        const totalPages = Math.ceil(totalCount / pageSize);
        
        return {
            data: response.data,
            totalCount,
            totalPages
        };
    }
});
