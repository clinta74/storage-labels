import React, { useEffect, useState } from 'react';
import { CircularProgress, Box } from '@mui/material';
import { useApi } from '../../../api';
import { CONFIG } from '../../../config';

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
            setLoading(false);
            return;
        }

        let objectUrl: string;

        const fetchImage = async () => {
            try {
                setLoading(true);
                setError(false);
                
                // Get the access token
                const token = await Api.getAccessToken();
                
                // Construct full URL if src is relative
                const fullUrl = src.startsWith('http') 
                    ? src 
                    : `${CONFIG.API_URL}${src.startsWith('/') ? src : '/' + src}`;
                
                // Fetch the image with authentication
                const response = await fetch(fullUrl, {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                    },
                });

                if (!response.ok) {
                    throw new Error(`Failed to fetch image: ${response.status} ${response.statusText}`);
                }

                const blob = await response.blob();
                objectUrl = URL.createObjectURL(blob);
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
