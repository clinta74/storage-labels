import { AxiosInstance } from 'axios';

export interface EncryptionKeyEndpoints {
    getEncryptionKeys: () => Promise<EncryptionKey[]>;
    getEncryptionKeyStats: (kid: number) => Promise<EncryptionKeyStats>;
    createEncryptionKey: (request: CreateEncryptionKeyRequest) => Promise<EncryptionKey>;
    activateEncryptionKey: (kid: number, autoRotate?: boolean) => Promise<{ rotationId?: string }>;
    retireEncryptionKey: (kid: number) => Promise<void>;
    startKeyRotation: (request: StartRotationRequest) => Promise<string>;
    getRotations: (status?: RotationStatus) => Promise<EncryptionKeyRotation[]>;
    getRotationProgress: (rotationId: string) => Promise<RotationProgress>;
    cancelRotation: (rotationId: string) => Promise<void>;
    streamRotationProgress: (rotationId: string, onProgress: (progress: RotationProgress) => void, getAccessToken: () => Promise<string>) => Promise<() => void>;
}

export const getEncryptionKeyEndpoints = (client: AxiosInstance): EncryptionKeyEndpoints => ({
    // Get all encryption keys
    getEncryptionKeys: () => 
        client.get('/admin/encryption-keys').then(res => res.data),

    // Get encryption key statistics
    getEncryptionKeyStats: (kid: number) => 
        client.get(`/admin/encryption-keys/${kid}/stats`).then(res => res.data),

    // Create a new encryption key
    createEncryptionKey: (request: CreateEncryptionKeyRequest) => 
        client.post('/admin/encryption-keys', request).then(res => res.data),

    // Activate an encryption key (with optional auto-rotation)
    activateEncryptionKey: (kid: number, autoRotate: boolean = true) => 
        client.put(`/admin/encryption-keys/${kid}/activate?autoRotate=${autoRotate}`, {}).then(res => res.data),

    // Retire an encryption key
    retireEncryptionKey: (kid: number) => 
        client.put(`/admin/encryption-keys/${kid}/retire`, {}).then(res => res.data),

    // Start manual key rotation
    startKeyRotation: (request: StartRotationRequest) => 
        client.post('/admin/encryption-keys/rotate', request).then(res => res.data),

    // Get all rotations (optionally filter by status)
    getRotations: (status?: RotationStatus) => 
        client.get('/admin/encryption-keys/rotations', { params: status ? { status } : {} }).then(res => res.data),

    // Get rotation progress
    getRotationProgress: (rotationId: string) => 
        client.get(`/admin/encryption-keys/rotations/${rotationId}`).then(res => res.data),

    // Cancel a rotation
    cancelRotation: (rotationId: string) => 
        client.delete(`/admin/encryption-keys/rotations/${rotationId}`).then(res => res.data),

    // Stream rotation progress via SSE
    streamRotationProgress: async (rotationId: string, onProgress: (progress: RotationProgress) => void, getAccessToken: () => Promise<string>) => {
        const baseURL = client.defaults.baseURL || '';
        const token = await getAccessToken();
        const url = `${baseURL}admin/encryption-keys/rotations/${rotationId}/stream`;
        
        const abortController = new AbortController();

        fetch(url, {
            headers: {
                'Authorization': `Bearer ${token}`,
            },
            signal: abortController.signal,
        })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                if (!response.body) {
                    throw new Error('No response body');
                }

                const reader = response.body.getReader();
                const decoder = new TextDecoder();

                const processStream = async () => {
                    let buffer = '';

                    while (true) {
                        const { done, value } = await reader.read();
                        
                        if (done) {
                            break;
                        }

                        buffer += decoder.decode(value, { stream: true });
                        const lines = buffer.split('\n');
                        buffer = lines.pop() || '';

                        for (const line of lines) {
                            if (line.startsWith('data: ')) {
                                try {
                                    const data = line.slice(6);
                                    const progress: RotationProgress = JSON.parse(data);
                                    onProgress(progress);

                                    // Stop reading when rotation is complete
                                    if (progress.status !== 'InProgress') {
                                        abortController.abort();
                                        return;
                                    }
                                } catch (error) {
                                    console.error('Failed to parse SSE message:', error);
                                }
                            }
                        }
                    }
                };

                processStream().catch(error => {
                    if (error.name !== 'AbortError') {
                        console.error('Stream processing error:', error);
                    }
                });
            })
            .catch(error => {
                if (error.name !== 'AbortError') {
                    console.error('SSE fetch error:', error);
                }
            });

        // Return cleanup function
        return () => abortController.abort();
    },
});

