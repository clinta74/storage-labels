import React from 'react';
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
    Alert,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import { useAuth } from '../../auth/auth-provider';
import { useUserPermission } from '../providers/user-permission-provider';

export const NavigationBar: React.FC = () => {
    const { logout, isAuthenticated, authMode } = useAuth();
    const { hasPermission } = useUserPermission();
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
        logout();
        navigate('/login');
        handleMenuClose();
    };

    const handleLogin = () => {
        navigate('/login');
    };

    const handleRegister = () => {
        navigate('/register');
    };

    if (!isAuthenticated && authMode !== 'None') {
        return (
            <AppBar position="static" sx={{ mb: 3 }}>
                <Toolbar>
                    <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                        Storage Labels
                    </Typography>
                    <Box sx={{ display: 'flex', gap: 2 }}>
                        <Button color="inherit" onClick={handleLogin}>
                            Login
                        </Button>
                        <Button color="inherit" onClick={handleRegister}>
                            Register
                        </Button>
                    </Box>
                </Toolbar>
            </AppBar>
        );
    }

    const menuItems = [
        { label: 'Locations', path: '/locations' },
        { label: 'Images', path: '/images' },
        { label: 'Common Locations', path: '/common-locations' },
        { label: 'Preferences', path: '/preferences' },
    ];

    // Add Encryption Keys menu item if user has permission
    if (hasPermission('read:encryption-keys')) {
        menuItems.splice(3, 0, { label: 'Encryption Keys', path: '/encryption-keys' });
    }

    // Add User Management menu item if user has write:user permission
    if (hasPermission('write:user')) {
        menuItems.push({ label: 'Users', path: '/users' });
    }

    return (
        <React.Fragment>
            {authMode === 'None' && (
                <Alert severity="warning" sx={{ mb: 2 }}>
                    Running in No Authentication mode - all users have full access
                </Alert>
            )}
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
                            {authMode === 'Local' && (
                                <MenuItem onClick={handleLogout}>Logout</MenuItem>
                            )}
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
                    {authMode === 'Local' && (
                        <Box sx={{ display: { xs: 'none', md: 'block' } }}>
                            <Button color="inherit" onClick={handleLogout}>
                                Logout
                            </Button>
                        </Box>
                    )}
                </Toolbar>
            </AppBar>
        </React.Fragment>
    );
};
