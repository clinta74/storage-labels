import { AxiosInstance } from 'axios';

export type ImageEndpoints = ReturnType<typeof getImageEndpoints>;

export const getImageEndpoints = (client: AxiosInstance) => ({
    uploadImage: (file: File) => {
        const formData = new FormData();
        formData.append('file', file);
        return client.post<string>('images', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
    },

    getUserImages: () =>
        client.get<ImageMetadataResponse[]>('images'),

    deleteImage: (imageId: string, force?: boolean) =>
        client.delete(force ? `images/${imageId}/force` : `images/${imageId}`),

    getImageUrl: (hashedUserId: string, imageId: string) =>
        `${client.defaults.baseURL}images/${hashedUserId}/${imageId}`,
});
