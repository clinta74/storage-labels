import React, { useState } from 'react';
import { Box, Container, IconButton, Link, Typography, Collapse } from '@mui/material';
import ExpandLessIcon from '@mui/icons-material/ExpandLess';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';

export const Footer: React.FC = () => {
    const currentYear = new Date().getFullYear();
    const [isOpen, setIsOpen] = useState(false);

    const handleToggle = () => {
        console.log('Footer toggle clicked, current state:', isOpen);
        setIsOpen(!isOpen);
        console.log('Setting state to:', !isOpen);
    };

    console.log('Footer render, isOpen:', isOpen);

    return (
        <Box
            sx={{
                position: 'relative',
                mt: 'auto',
            }}
        >
            {/* Toggle button - only visible on short viewports */}
            <Box
                sx={{
                    display: 'flex',
                    justifyContent: 'center',
                    position: 'absolute',
                    top: -20,
                    left: '50%',
                    transform: 'translateX(-50%)',
                    zIndex: 1300,
                    '@media (min-height: 700px)': {
                        display: 'none',
                    },
                }}
            >
                <IconButton
                    onClick={handleToggle}
                    size="small"
                    sx={{
                        width: 60,
                        height: 20,
                        borderRadius: '8px 8px 0 0',
                        padding: 0,
                        minWidth: 'auto',
                        backgroundColor: (theme) =>
                            theme.palette.mode === 'light'
                                ? theme.palette.grey[200]
                                : theme.palette.grey[800],
                        '&:hover': {
                            backgroundColor: (theme) =>
                                theme.palette.mode === 'light'
                                    ? theme.palette.grey[300]
                                    : theme.palette.grey[700],
                        },
                        boxShadow: 1,
                        zIndex: 1300,
                    }}
                    aria-label={isOpen ? 'Hide footer' : 'Show footer'}
                >
                    {isOpen ? (
                        <ExpandMoreIcon sx={{ fontSize: 18 }} />
                    ) : (
                        <ExpandLessIcon sx={{ fontSize: 18 }} />
                    )}
                </IconButton>
            </Box>

            {/* Footer content - collapsible on short viewports */}
            <Box
                sx={{
                    display: 'block',
                    '@media (min-height: 700px)': {
                        display: 'none',
                    },
                }}
            >
                <Collapse in={isOpen} timeout="auto">
                    <Box
                        component="footer"
                        sx={{
                            py: 0.75,
                            px: 2,
                            backgroundColor: (theme) =>
                                theme.palette.mode === 'light'
                                    ? theme.palette.grey[200]
                                    : theme.palette.grey[800],
                        }}
                    >
                        <Container maxWidth="lg">
                            <Box
                                sx={{
                                    display: 'flex',
                                    justifyContent: 'space-between',
                                    alignItems: 'center',
                                    flexWrap: 'wrap',
                                    gap: 1,
                                }}
                            >
                                <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.75rem' }}>
                                    © {currentYear} Storage Labels. All rights reserved.
                                </Typography>
                                <Box sx={{ display: 'flex', gap: 2 }}>
                                    <Link
                                        href="/legal/privacy"
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        underline="hover"
                                        color="text.secondary"
                                        sx={{ fontSize: '0.75rem' }}
                                    >
                                        Privacy Policy
                                    </Link>
                                    <Link
                                        href="/legal/terms"
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        underline="hover"
                                        color="text.secondary"
                                        sx={{ fontSize: '0.75rem' }}
                                    >
                                        Terms of Service
                                    </Link>
                                </Box>
                            </Box>
                        </Container>
                    </Box>
                </Collapse>
            </Box>

            {/* Always visible footer on tall viewports */}
            <Box
                component="footer"
                sx={{
                    display: 'none',
                    '@media (min-height: 700px)': {
                        display: 'block',
                    },
                    py: 1,
                    px: 2,
                    backgroundColor: (theme) =>
                        theme.palette.mode === 'light'
                            ? theme.palette.grey[200]
                            : theme.palette.grey[800],
                }}
            >
                <Container maxWidth="lg">
                    <Box
                        sx={{
                            display: 'flex',
                            justifyContent: 'space-between',
                            alignItems: 'center',
                            flexWrap: 'wrap',
                            gap: 2,
                        }}
                    >
                        <Typography variant="body2" color="text.secondary" sx={{ fontSize: '0.875rem' }}>
                            © {currentYear} Storage Labels. All rights reserved.
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 3 }}>
                            <Link
                                href="/legal/privacy"
                                target="_blank"
                                rel="noopener noreferrer"
                                underline="hover"
                                color="text.secondary"
                                sx={{ fontSize: '0.875rem' }}
                            >
                                Privacy Policy
                            </Link>
                            <Link
                                href="/legal/terms"
                                target="_blank"
                                rel="noopener noreferrer"
                                underline="hover"
                                color="text.secondary"
                                sx={{ fontSize: '0.875rem' }}
                            >
                                Terms of Service
                            </Link>
                        </Box>
                    </Box>
                </Container>
            </Box>
        </Box>
    );
};
