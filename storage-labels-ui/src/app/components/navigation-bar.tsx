import React from 'react';
import { useNavigate, Link } from 'react-router';
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
    Divider,
} from '@mui/material';
import MenuIcon from '@mui/icons-material/Menu';
import AccountCircle from '@mui/icons-material/AccountCircle';
import { useAuth } from '../../auth/auth-provider';
import { useUserPermission } from '../providers/user-permission-provider';
import { useUser } from '../providers/user-provider';
import { Permissions } from '../constants/permissions';

export const NavigationBar: React.FC = () => {
    const { logout, isAuthenticated, authMode } = useAuth();
    const { hasPermission } = useUserPermission();
    const { user } = useUser();
    const navigate = useNavigate();
    const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
    const [userMenuAnchorEl, setUserMenuAnchorEl] = React.useState<null | HTMLElement>(null);

    const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setAnchorEl(event.currentTarget);
    };

    const handleMenuClose = () => {
        setAnchorEl(null);
    };

    const handleUserMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
        setUserMenuAnchorEl(event.currentTarget);
    };

    const handleUserMenuClose = () => {
        setUserMenuAnchorEl(null);
    };

    const handleNavigate = (path: string) => {
        navigate(path);
        handleMenuClose();
        handleUserMenuClose();
    };

    const handleLogout = async () => {
        await logout();
        navigate('/login');
        handleMenuClose();
        handleUserMenuClose();
    };

    if ((!isAuthenticated && authMode !== 'None') || !user) {
        return (
            <AppBar position="static" sx={{ mb: 3 }}>
                <Toolbar>
                    <Typography variant="h6" component="div" sx={{ flexGrow: 1 }}>
                        Storage Labels
                    </Typography>
                </Toolbar>
            </AppBar>
        );
    }

    const menuItems = [
        { label: 'Locations', path: '/locations' },
        { label: 'Images', path: '/images' },
    ];

    // Add Common Locations menu item if user has permission
    if (hasPermission(Permissions.Read_CommonLocations)) {
        menuItems.push({ label: 'Common Locations', path: '/common-locations' });
    }

    // Add Encryption Keys menu item if user has permission
    if (hasPermission(Permissions.Read_EncryptionKeys)) {
        menuItems.push({ label: 'Encryption Keys', path: '/encryption-keys' });
    }

    // Add User Management menu item if user has write:user permission
    if (hasPermission(Permissions.Write_User)) {
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
                            <Divider />
                            <MenuItem onClick={() => handleNavigate('/preferences')}>
                                Preferences
                            </MenuItem>
                            <MenuItem onClick={() => handleNavigate('/change-password')}>
                                Change Password
                            </MenuItem>
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
                                component={Link}
                                to={item.path}
                            >
                                {item.label}
                            </Button>
                        ))}
                    </Box>

                    {/* Desktop user menu */}
                    {user && (
                        <Box sx={{ display: { xs: 'none', md: 'flex' } }}>
                            <Button
                                color="inherit"
                                onClick={handleUserMenuOpen}
                                startIcon={<AccountCircle />}
                            >
                                {user.firstName?.trim() || user.emailAddress || 'User'}
                            </Button>
                            <Menu
                                anchorEl={userMenuAnchorEl}
                                open={Boolean(userMenuAnchorEl)}
                                onClose={handleUserMenuClose}
                            >
                                <MenuItem onClick={() => handleNavigate('/preferences')}>
                                    Preferences
                                </MenuItem>
                                <MenuItem onClick={() => handleNavigate('/change-password')}>
                                    Change Password
                                </MenuItem>
                                {authMode === 'Local' && (
                                    <>
                                        <Divider />
                                        <MenuItem onClick={handleLogout}>Logout</MenuItem>
                                    </>
                                )}
                            </Menu>
                        </Box>
                    )}
                </Toolbar>
            </AppBar>
        </React.Fragment>
    );
};
