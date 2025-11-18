import React, { createContext, PropsWithChildren, useContext, useEffect, useState } from 'react';
import axios from 'axios';
import { CONFIG } from '../config';

interface AuthConfig {
    mode: 'Local' | 'None';
}

interface User {
    username: string;
    email: string;
    firstName: string;
    lastName: string;
}

interface AuthContextType {
    isAuthenticated: boolean;
    isLoading: boolean;
    user: User | null;
    authMode: 'Local' | 'None' | null;
    login: (usernameOrEmail: string, password: string) => Promise<void>;
    register: (email: string, username: string, password: string, firstName: string, lastName: string) => Promise<void>;
    logout: () => void;
    getAccessToken: () => Promise<string>;
}

const AuthContext = createContext<AuthContextType | null>(null);

const TOKEN_KEY = 'auth_token';

export const AuthProvider: React.FC<PropsWithChildren> = ({ children }) => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [user, setUser] = useState<User | null>(null);
    const [authMode, setAuthMode] = useState<'Local' | 'None' | null>(null);
    const [token, setToken] = useState<string | null>(null);

    // Load auth config and check for existing token
    useEffect(() => {
        const initAuth = async () => {
            try {
                // Fetch auth config
                const { data } = await axios.get<AuthConfig>(`${CONFIG.API_URL}/api/auth/config`);
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
                        setUser(userResponse.data);
                        setIsAuthenticated(true);
                    } catch (error) {
                        // Token invalid, clear it
                        localStorage.removeItem(TOKEN_KEY);
                        setToken(null);
                    }
                }
            } catch (error) {
                console.error('Failed to initialize auth:', error);
            } finally {
                setIsLoading(false);
            }
        };

        initAuth();
    }, []);

    const login = async (usernameOrEmail: string, password: string) => {
        try {
            const { data } = await axios.post(`${CONFIG.API_URL}/api/auth/login`, {
                usernameOrEmail,
                password
            });

            const newToken = data.token;
            setToken(newToken);
            localStorage.setItem(TOKEN_KEY, newToken);

            // Fetch user info
            const userResponse = await axios.get(`${CONFIG.API_URL}/api/auth/me`, {
                headers: { Authorization: `Bearer ${newToken}` }
            });
            setUser(userResponse.data);
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

    const logout = () => {
        setToken(null);
        setUser(null);
        setIsAuthenticated(false);
        localStorage.removeItem(TOKEN_KEY);
    };

    const getAccessToken = async (): Promise<string> => {
        if (authMode === 'None') {
            return ''; // No token needed in NoAuth mode
        }
        if (!token) {
            throw new Error('Not authenticated');
        }
        return token;
    };

    return (
        <AuthContext.Provider
            value={{
                isAuthenticated,
                isLoading,
                user,
                authMode,
                login,
                register,
                logout,
                getAccessToken
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
