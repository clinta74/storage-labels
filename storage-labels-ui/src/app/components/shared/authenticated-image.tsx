import React, { useEffect, useState } from 'react';
import { CircularProgress, Box } from '@mui/material';
import { useApi } from '../../../api';

interface AuthenticatedImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
    src: string;
    alt: string;
}

export const AuthenticatedImage: React.FC<AuthenticatedImageProps> = ({ src, alt, style, ...props }) => {
    const { Api } = useApi();
    const [imageUrl, setImageUrl] = useState<string>('');
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(false);

    useEffect(() => {
        if (!src) {
            console.log('AuthenticatedImage: No src provided');
            setLoading(false);
            return;
        }

        let objectUrl: string;

        const fetchImage = async () => {
            try {
                setLoading(true);
                setError(false);
                
                console.log('AuthenticatedImage: Fetching image from:', src);
                
                // Get the access token
                const token = await Api.getAccessToken();
                console.log('AuthenticatedImage: Got access token');
                
                // Fetch the image with authentication
                const response = await fetch(src, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    },
                });

                console.log('AuthenticatedImage: Response status:', response.status);

                if (!response.ok) {
                    throw new Error(`Failed to fetch image: ${response.status} ${response.statusText}`);
                }

                const blob = await response.blob();
                console.log('AuthenticatedImage: Got blob, size:', blob.size, 'type:', blob.type);
                objectUrl = URL.createObjectURL(blob);
                console.log('AuthenticatedImage: Created object URL:', objectUrl);
                setImageUrl(objectUrl);
            } catch (err) {
                console.error('AuthenticatedImage: Error loading image:', err);
                setError(true);
            } finally {
                setLoading(false);
            }
        };

        fetchImage();

        // Cleanup: revoke object URL when component unmounts or src changes
        return () => {
            if (objectUrl) {
                console.log('AuthenticatedImage: Revoking object URL');
                URL.revokeObjectURL(objectUrl);
            }
        };
    }, [src, Api]);

    if (loading) {
        return (
            <Box
                sx={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    minHeight: 100,
                    ...style,
                }}
            >
                <CircularProgress />
            </Box>
        );
    }

    if (error || !imageUrl) {
        return (
            <Box
                sx={{
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: 'grey.200',
                    color: 'grey.500',
                    minHeight: 100,
                    ...style,
                }}
            >
                Failed to load image
            </Box>
        );
    }

    return <img src={imageUrl} alt={alt} style={style} {...props} />;
};
