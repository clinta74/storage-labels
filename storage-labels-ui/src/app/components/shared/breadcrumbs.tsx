import React from 'react';
import { Link as RouterLink } from 'react-router';
import { Breadcrumbs as MuiBreadcrumbs, Link, Typography } from '@mui/material';
import NavigateNextIcon from '@mui/icons-material/NavigateNext';
import HomeIcon from '@mui/icons-material/Home';

interface BreadcrumbItem {
    label: string;
    path?: string;
}

interface BreadcrumbsProps {
    items: BreadcrumbItem[];
}

export const Breadcrumbs: React.FC<BreadcrumbsProps> = ({ items }) => {
    return (
        <MuiBreadcrumbs 
            separator={<NavigateNextIcon fontSize="small" />}
            aria-label="breadcrumb"
            sx={{ mb: 2 }}
        >
            <Link
                component={RouterLink}
                to="/locations"
                underline="hover"
                color="inherit"
                sx={{ 
                    display: 'flex', 
                    alignItems: 'center',
                    '&:hover': {
                        color: 'primary.main',
                    }
                }}
            >
                <HomeIcon sx={{ mr: 0.5 }} fontSize="small" />
                Locations
            </Link>
            {items.map((item, index) => {
                const isLast = index === items.length - 1;
                
                if (isLast || !item.path) {
                    return (
                        <Typography 
                            key={index} 
                            color="text.primary"
                            sx={{ fontWeight: 500 }}
                        >
                            {item.label}
                        </Typography>
                    );
                }
                
                return (
                    <Link
                        key={index}
                        component={RouterLink}
                        to={item.path}
                        underline="hover"
                        color="inherit"
                        sx={{
                            '&:hover': {
                                color: 'primary.main',
                            }
                        }}
                    >
                        {item.label}
                    </Link>
                );
            })}
        </MuiBreadcrumbs>
    );
};
