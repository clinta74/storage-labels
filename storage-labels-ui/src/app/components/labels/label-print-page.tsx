import React from 'react';
import { Box, Button, Typography } from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PrintIcon from '@mui/icons-material/Print';
import { useLocation, useNavigate, useParams } from 'react-router';
import { LabelItem } from './label-item';

export const LabelPrintPage: React.FC = () => {
    const { jobId } = useParams<{ jobId: string }>();
    const location = useLocation();
    const navigate = useNavigate();
    const page: LabelPageResponse | undefined = location.state?.page;

    if (!page) {
        return (
            <Box sx={{ margin: 2 }}>
                <Typography>No label page data. Go back and click &quot;Print Next Page&quot;.</Typography>
                <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/labels/${jobId}`)}>Back</Button>
            </Box>
        );
    }

    return (
        <Box>
            <style>{`
                @page {
                    margin: 0 !important;
                    size: letter portrait;
                }
                @media print {
                    html, body {
                        margin: 0 !important;
                        padding: 0 !important;
                        height: 100%;
                    }
                }
                .no-print {
                    print-color-adjust: exact;
                    -webkit-print-color-adjust: exact;
                }
            `}</style>
            {/* Screen controls — hidden when printing */}
            <Box
                className="no-print"
                sx={{
                    display: 'flex',
                    gap: 2,
                    p: 2,
                    alignItems: 'center',
                    '@media print': { display: 'none' },
                }}
            >
                <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(`/labels/${jobId}`)}>
                    Back
                </Button>
                <Button variant="contained" startIcon={<PrintIcon />} onClick={() => window.print()}>
                    Print
                </Button>
                <Typography variant="body2" color="text.secondary">
                    Avery 94107 — align sheet per label guidelines before printing.
                </Typography>
            </Box>

            {/*
              * Print layout for Avery 94107 (2" × 2", 12 per page, 3 cols × 4 rows)
              * Sheet: 8.5" × 11" letter
              * Top margin: 0.5"  Left margin: 0.875"
              * Col gap: 0.375"   Row gap: 0.5"
              */}
            <Box
                sx={{
                    '@media print': {
                        margin: '0.5in 0 0 0.875in',
                    },
                    '@media screen': {
                        margin: 2,
                    },
                }}
            >
                <Box
                    sx={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(3, 2in)',
                        gridTemplateRows: 'repeat(4, 2in)',
                        columnGap: '0.375in',
                        rowGap: '0.5in',
                        width: 'fit-content',
                    }}
                >
                    {page.labels.map(item => (
                        <LabelItem
                            key={item.labelNumber}
                            code={item.code}
                            codeColorPattern={page.codeColorPattern}
                        />
                    ))}
                </Box>
            </Box>
        </Box>
    );
};
