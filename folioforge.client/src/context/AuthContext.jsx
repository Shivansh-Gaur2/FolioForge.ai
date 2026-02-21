import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { AuthService } from '../services/authService';

/**
 * AuthContext
 *
 * Provides authentication state and actions to the entire app.
 * - Persists JWT + user info in localStorage
 * - Restores session on page reload
 * - Exposes login, register, logout functions
 */
const AuthContext = createContext(undefined);

export const AuthProvider = ({ children }) => {
    const [user, setUser] = useState(null);
    const [token, setToken] = useState(AuthService.getToken);
    const [loading, setLoading] = useState(true); // initial bootstrap

    /**
     * Bootstrap: if we have a stored token, validate it by calling /me.
     * If invalid (401), clear auth state.
     */
    useEffect(() => {
        const bootstrap = async () => {
            const storedToken = AuthService.getToken();
            if (!storedToken) {
                setLoading(false);
                return;
            }

            try {
                const profile = await AuthService.me();
                setUser(profile);
                setToken(storedToken);
                AuthService.setStoredUser(profile);
            } catch {
                // Token expired or invalid — clear everything
                AuthService.clearAuth();
                setUser(null);
                setToken(null);
            } finally {
                setLoading(false);
            }
        };

        bootstrap();
    }, []);

    /**
     * Persist auth response (from login or register).
     */
    const persistAuth = useCallback((authResponse) => {
        AuthService.setToken(authResponse.token);
        setToken(authResponse.token);

        const userInfo = {
            userId: authResponse.userId,
            email: authResponse.email,
            fullName: authResponse.fullName,
            tenantId: authResponse.tenantId,
            tenantIdentifier: authResponse.tenantIdentifier,
        };
        AuthService.setStoredUser(userInfo);
        setUser(userInfo);
    }, []);

    /**
     * Register a new user and auto-login.
     */
    const register = useCallback(async ({ email, fullName, password, tenantIdentifier }) => {
        const response = await AuthService.register({ email, fullName, password, tenantIdentifier });
        persistAuth(response);
        return response;
    }, [persistAuth]);

    /**
     * Login with email + password.
     */
    const login = useCallback(async ({ email, password }) => {
        const response = await AuthService.login({ email, password });
        persistAuth(response);
        return response;
    }, [persistAuth]);

    /**
     * Logout: clear all stored auth data and reset state.
     */
    const logout = useCallback(() => {
        AuthService.clearAuth();
        setUser(null);
        setToken(null);
    }, []);

    const value = {
        user,
        token,
        isAuthenticated: !!token && !!user,
        loading,
        login,
        register,
        logout,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
};

/**
 * Hook to consume auth context.
 * Must be used within <AuthProvider>.
 */
export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider');
    }
    return context;
};
