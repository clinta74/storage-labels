import React from 'react';
import { useAuth0 } from '@auth0/auth0-react';
import { useNavigate } from 'react-router';
import {
    AppBar,
    Toolbar,
    Typography,
    Button,
    Box,
    IconButton,
    Menu,
    MenuItem,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import { useUserPermission } from '../providers/user-permission-provider';

export const NavigationBar: React.FC = () => {
    const { logout, isAuthenticated } = useAuth0();
    const { } = useUserPermission();
    const navigate = useNavigate();
    const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    const handleNavigate = (path: string) => {
        navigate(path);
        handleMenuClose();
    };

    const handleLogout = () => {
        logout({ returnTo: window.location.origin });
        handleMenuClose();
    };

    if (!isAuthenticated) {
        return null;
    }

    const menuItems = [
        { label: 'Locations', path: '/locations' },
        { label: 'Images', path: '/images' },
        { label: 'Common Locations', path: '/common-locations' },
        { label: 'Preferences', path: '/preferences' },
    ];

    return (
        <AppBar position="static" sx={{ mb: 3 }}>
            <Toolbar>
                <Typography variant="h6" component="div" sx={{ flexGrow: 0, mr: 4 }}>
                    Storage Labels
                </Typography>

                {/* Mobile menu */}
                <Box sx={{ flexGrow: 1, display: { xs: 'flex', md: 'none' }, justifyContent: 'flex-end' }}>
                    <IconButton
                        size="large"
                        edge="end"
                        color="inherit"
                        aria-label="menu"
                        onClick={handleMenuOpen}
                    >
                        <MenuIcon />
                    </IconButton>
                    <Menu
                        anchorEl={anchorEl}
                        open={Boolean(anchorEl)}
                        onClose={handleMenuClose}
                    >
                        {menuItems.map((item) => (
                            <MenuItem key={item.path} onClick={() => handleNavigate(item.path)}>
                                {item.label}
                            </MenuItem>
                        ))}
                        <MenuItem onClick={handleLogout}>Logout</MenuItem>
                    </Menu>
                </Box>

                {/* Desktop menu */}
                <Box sx={{ flexGrow: 1, display: { xs: 'none', md: 'flex' }, gap: 2 }}>
                    {menuItems.map((item) => (
                        <Button
                            key={item.path}
                            color="inherit"
                            onClick={() => handleNavigate(item.path)}
                        >
                            {item.label}
                        </Button>
                    ))}
                </Box>
                <Box sx={{ display: { xs: 'none', md: 'block' } }}>
                    <Button color="inherit" onClick={handleLogout}>
                        Logout
                    </Button>
                </Box>
            </Toolbar>
        </AppBar>
    );
};
