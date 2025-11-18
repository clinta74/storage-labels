import React from 'react';
import { Box, Container, Link, Typography } from '@mui/material';

export const Footer: React.FC = () => {
    const currentYear = new Date().getFullYear();

    return (
        <Box
            component="footer"
            sx={{
                py: { xs: .75, sm: 1 },
                px: 2,
                mt: 'auto',
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
                        gap: { xs: 1, sm: 2 },
                    }}
                >
                    <Typography variant="body2" color="text.secondary" sx={{ fontSize: { xs: '0.75rem', sm: '0.875rem' } }}>
                        © {currentYear} Storage Labels. All rights reserved.
                    </Typography>
                    <Box sx={{ display: 'flex', gap: { xs: 2, sm: 3 } }}>
                        <Link
                            href="/legal/privacy"
                            target="_blank"
                            rel="noopener noreferrer"
                            underline="hover"
                            color="text.secondary"
                            sx={{ fontSize: { xs: '0.75rem', sm: '0.875rem' } }}
                        >
                            Privacy Policy
                        </Link>
                        <Link
                            href="/legal/terms"
                            target="_blank"
                            rel="noopener noreferrer"
                            underline="hover"
                            color="text.secondary"
                            sx={{ fontSize: { xs: '0.75rem', sm: '0.875rem' } }}
                        >
                            Terms of Service
                        </Link>
                    </Box>
                </Box>
            </Container>
        </Box>
    );
};
