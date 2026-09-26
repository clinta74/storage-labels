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
    // The object URL is stored with the src it was fetched for, so loading is derived rather than set from the effect.
    // A null url means the fetch failed.
    const [loaded, setLoaded] = useState<{ src: string; url: string | null }>();

    // Default to true if undefined or null
    const showImages = user?.preferences?.showImages !== false;

    const loading = !!src && showImages && loaded?.src !== src;
    const imageUrl = loaded?.src === src ? loaded.url : null;
    const error = !imageUrl;

    useEffect(() => {
        if (!src || !showImages) {
            return;
        }

        let objectUrl: string | undefined;
        let cancelled = false;

        // Construct full URL if src is relative
        const fullUrl = src.startsWith('http')
            ? src
            : `${CONFIG.API_URL}${src.startsWith('/') ? src : '/' + src}`;

        Api.getAccessToken()
            // Fetch the image with authentication
            .then(token => fetch(fullUrl, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                },
            }))
            .then(response => {
                if (!response.ok) {
                    // Handle 404 silently as it just means the image doesn't exist on disk
                    if (response.status !== 404) {
                        console.error(`Failed to fetch image: ${response.status} ${response.statusText}`);
                    }
                    return null;
                }
                return response.blob();
            })
            .then(blob => {
                if (cancelled) return;
                if (blob) {
                    objectUrl = URL.createObjectURL(blob);
                }
                setLoaded({ src, url: objectUrl ?? null });
            })
            .catch(err => {
                if (cancelled) return;
                console.error('AuthenticatedImage: Error loading image:', err);
                setLoaded({ src, url: null });
            });

        // Cleanup: revoke object URL when component unmounts or src changes, and forget it so it's never rendered revoked
        return () => {
            cancelled = true;
            if (objectUrl) {
                URL.revokeObjectURL(objectUrl);
            }
            setLoaded(undefined);
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
