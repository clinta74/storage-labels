import React, { createContext, PropsWithChildren, useContext, useEffect, useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router';
import { CONFIG } from '../config';

interface AuthConfig {
    mode: 'Local' | 'None';
}

interface AuthenticatedUser {
    userId: string;
    username: string;
    email: string;
    fullName?: string | null;
    profilePictureUrl?: string | null;
    roles: string[];
    permissions: string[];
    isActive: boolean;
}

interface AuthenticationResultResponse {
    token: string;
    expiresAt: string;
    user: AuthenticatedUser;
}

interface AuthContextType {
    isAuthenticated: boolean;
    isLoading: boolean;
    user: AuthenticatedUser | null;
    authMode: 'Local' | 'None' | null;
    login: (usernameOrEmail: string, password: string) => Promise<void>;
    register: (email: string, username: string, password: string, firstName: string, lastName: string) => Promise<void>;
    logout: () => Promise<void>;
    getAccessToken: () => Promise<string>;
    refreshAccessToken: () => Promise<string>;
    handleSessionExpired: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

const TOKEN_KEY = 'auth_token';

export const AuthProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [authMode, setAuthMode] = useState<'Local' | 'None' | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [currentUser, setCurrentUser] = useState<AuthenticatedUser | null>(null);
    const navigate = useNavigate();

    const persistToken = (value: string | null) => {
        setToken(value);
        if (value) {
            localStorage.setItem(TOKEN_KEY, value);
        } else {
            localStorage.removeItem(TOKEN_KEY);
        }
    };

    // Load auth config and check for existing token
    useEffect(() => {
        const initAuth = async () => {
            try {
                // Fetch auth config
                const { data } = await axios.get<AuthConfig>(`${CONFIG.API_URL}/api/auth/config`, { withCredentials: true });
                setAuthMode(data.mode);

                if (data.mode === 'None') {
                    // No auth mode - automatically authenticated
                    setIsAuthenticated(true);
                    setIsLoading(false);
                    return;
                }

                // Check for stored token
                const storedToken = localStorage.getItem(TOKEN_KEY);
                if (storedToken) {
                    setToken(storedToken);
                    // Verify token by fetching user info
                    try {
                        const userResponse = await axios.get(`${CONFIG.API_URL}/api/auth/me`, {
                            headers: { Authorization: `Bearer ${storedToken}` }
                        });
                        setCurrentUser(userResponse.data);
                        setIsAuthenticated(true);
                    } catch {
                        // Token invalid, clear it
                        localStorage.removeItem(TOKEN_KEY);
                        setToken(null);
                    }
                }
            } catch (error) {
                console.log('Failed to initialize auth:', error);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    const login = async (usernameOrEmail: string, password: string) => {
        try {
            const { data } = await axios.post<AuthenticationResultResponse>(
                `${CONFIG.API_URL}/api/auth/login`,
                {
                    usernameOrEmail,
                    password
                },
                {
                    withCredentials: true
                }
            );

            persistToken(data.token);
            setCurrentUser(data.user);
            setIsAuthenticated(true);
        } catch (error: any) {
            const errorMessage = error.response?.data?.message || error.response?.statusText || error.message || 'Login failed';
            console.warn('Login failed:', errorMessage);
            // Re-throw with message property for UI handling
            error.message = errorMessage;
            throw error;
        }
    };

    const register = async (
        email: string,
        username: string,
        password: string,
        firstName: string,
        lastName: string
    ) => {
        try {
            await axios.post(`${CONFIG.API_URL}/api/auth/register`, {
                email,
                username,
                password,
                firstName,
                lastName
            }, {
                withCredentials: true
            });

            // Auto-login after registration
            await login(username, password);
        } catch (error: any) {
            const errorMessage = error.response?.data?.message || error.response?.data?.title || error.response?.statusText || error.message || 'Registration failed';
            console.warn('Registration failed:', errorMessage, error.response?.data);
            // Re-throw with message property for UI handling
            error.message = errorMessage;
            throw error;
        }
    };

    const logout = async () => {
        if (authMode === 'Local' && token) {
            try {
                await axios.post(
                    `${CONFIG.API_URL}/api/auth/logout`,
                    {},
                    {
                        headers: { Authorization: `Bearer ${token}` },
                        withCredentials: true
                    }
                );
            } catch (error) {
                console.warn('Logout request failed:', error);
            }
        }

        persistToken(null);
        setCurrentUser(null);
        setIsAuthenticated(false);
    };

    const getAccessToken = async (): Promise<string> => {
        if (authMode === 'None') {
            return ''; // No token needed in NoAuth mode
        }
        const currentToken = token ?? localStorage.getItem(TOKEN_KEY);
        if (!currentToken) {
            throw new Error('Not authenticated');
        }
        return currentToken;
    };

    const refreshAccessToken = async (): Promise<string> => {
        if (authMode === 'None') {
            throw new Error('Refresh not supported in NoAuth mode');
        }

        const { data } = await axios.post<AuthenticationResultResponse>(
            `${CONFIG.API_URL}/api/auth/refresh`,
            {},
            { withCredentials: true }
        );

        persistToken(data.token);
        setCurrentUser(data.user);
        setIsAuthenticated(true);
        return data.token;
    };

    const handleSessionExpired = async () => {
        await logout();
        navigate('/login?notice=session-expired', { replace: true });
    };

    return (
        <AuthContext.Provider
            value={{
                isAuthenticated,
                isLoading,
                user: currentUser,
                authMode,
                login,
                register,
                logout,
                getAccessToken,
                refreshAccessToken,
                handleSessionExpired
            }}
        >
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (context === null) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
