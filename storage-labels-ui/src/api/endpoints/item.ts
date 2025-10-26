import { AxiosInstance } from 'axios';

export type ItemEndpoints = ReturnType<typeof getItemEndpoints>;

export const getItemEndpoints = (client: AxiosInstance) => ({
    createItem: (item: ItemRequest) =>
        client.post<ItemResponse>('item', item),

    getItemsByBoxId: (boxId: string) =>
        client.get<ItemResponse[]>(`item/box/${boxId}`),

    getItemById: (itemId: string) =>
        client.get<ItemResponse>(`item/${itemId}`),

    updateItem: (itemId: string, item: ItemRequest) =>
        client.put<ItemResponse>(`item/${itemId}`, item),

    deleteItem: (itemId: string) =>
        client.delete(`item/${itemId}`),
});
