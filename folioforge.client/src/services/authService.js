import apiClient from '../api/client';

/**
 * Auth Service
 * 
 * Handles authentication API calls: register, login, refresh, profile fetch.
 * JWT token management (storage, retrieval, removal) lives here
 * so that the rest of the app never touches localStorage directly.
 * 
 * Token strategy:
 * - accessToken:  short-lived JWT (15 min), stored in memory + localStorage
 * - refreshToken: long-lived opaque string (7 days), stored in localStorage
 */

const TOKEN_KEY         = 'ff_token';
const REFRESH_TOKEN_KEY = 'ff_refresh_token';
const USER_KEY          = 'ff_user';

export const AuthService = {
    // ── Token management ─────────────────────────────────

    getToken: () => localStorage.getItem(TOKEN_KEY),

    setToken: (token) => localStorage.setItem(TOKEN_KEY, token),

    removeToken: () => localStorage.removeItem(TOKEN_KEY),

    getRefreshToken: () => localStorage.getItem(REFRESH_TOKEN_KEY),

    setRefreshToken: (token) => localStorage.setItem(REFRESH_TOKEN_KEY, token),

    removeRefreshToken: () => localStorage.removeItem(REFRESH_TOKEN_KEY),

    getStoredUser: () => {
        try {
            const raw = localStorage.getItem(USER_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch {
            return null;
        }
    },

    setStoredUser: (user) => localStorage.setItem(USER_KEY, JSON.stringify(user)),

    removeStoredUser: () => localStorage.removeItem(USER_KEY),

    clearAuth: () => {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(REFRESH_TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
    },

    // ── API calls ────────────────────────────────────────

    /**
     * Register a new user.
     * @param {{ email: string, fullName: string, password: string, tenantIdentifier: string }} data
     */
    register: async (data) => {
        return await apiClient.post('/auth/register', data);
    },

    /**
     * Login with email & password.
     * @param {{ email: string, password: string }} data
     */
    login: async (data) => {
        return await apiClient.post('/auth/login', data);
    },

    /**
     * Refresh an expired access token using the stored refresh token.
     * Returns { accessToken, refreshToken, ... } on success.
     */
    refresh: async () => {
        const accessToken = AuthService.getToken();
        const refreshToken = AuthService.getRefreshToken();
        if (!accessToken || !refreshToken) throw new Error('No tokens to refresh');

        return await apiClient.post('/auth/refresh', { accessToken, refreshToken });
    },

    /**
     * Revoke the current refresh token (server-side logout).
     */
    revoke: async () => {
        const refreshToken = AuthService.getRefreshToken();
        if (!refreshToken) return;
        try {
            await apiClient.post('/auth/revoke', { refreshToken });
        } catch {
            // Best effort — clear locally even if server call fails
        }
    },

    /**
     * Fetch current user profile from the JWT.
     * Requires a valid Bearer token in the request.
     */
    me: async () => {
        return await apiClient.get('/auth/me');
    },
};
