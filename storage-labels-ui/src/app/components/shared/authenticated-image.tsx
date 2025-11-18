import React, { useEffect, useState } from 'react';
import { CircularProgress, Box, Typography } from '@mui/material';
import ImageIcon from '@mui/icons-material/Image';
import { useApi } from '../../../api';
import { CONFIG } from '../../../config';
import { useUser } from '../../providers/user-provider';

interface AuthenticatedImageProps extends React.ImgHTMLAttributes<HTMLImageElement> {
    src: string;
    alt: string;
}

export const AuthenticatedImage: React.FC<AuthenticatedImageProps> = ({ src, alt, style, ...props }) => {
    const { Api } = useApi();
    const { user } = useUser();
    const [imageUrl, setImageUrl] = useState<string>('');
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(false);

    // Default to true if undefined or null
    const showImages = user?.preferences?.showImages !== false;

    useEffect(() => {
        if (!src || !showImages) {
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
                    // Handle 404 silently as it just means the image doesn't exist on disk
                    if (response.status === 404) {
                        setError(true);
                        return;
                    }
                    console.error(`Failed to fetch image: ${response.status} ${response.statusText}`);
                    setError(true);
                    return;
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
    }, [src, Api, showImages]);

    // If images are disabled, show a placeholder
    if (!showImages) {
        return (
            <Box
                sx={{
                    display: 'flex',
                    flexDirection: 'column',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: 'action.hover',
                    color: 'text.secondary',
                    minHeight: 100,
                    ...style,
                }}
            >
                <ImageIcon sx={{ fontSize: 48, mb: 1, opacity: 0.5 }} />
                <Typography variant="caption">Images disabled</Typography>
            </Box>
        );
    }

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
