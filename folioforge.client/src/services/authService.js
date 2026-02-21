import apiClient from '../api/client';

/**
 * Auth Service
 * 
 * Handles authentication API calls: register, login, profile fetch.
 * JWT token management (storage, retrieval, removal) lives here
 * so that the rest of the app never touches localStorage directly.
 */

const TOKEN_KEY = 'ff_token';
const USER_KEY  = 'ff_user';

export const AuthService = {
    // ── Token management ─────────────────────────────────

    getToken: () => localStorage.getItem(TOKEN_KEY),

    setToken: (token) => localStorage.setItem(TOKEN_KEY, token),

    removeToken: () => localStorage.removeItem(TOKEN_KEY),

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
     * Fetch current user profile from the JWT.
     * Requires a valid Bearer token in the request.
     */
    me: async () => {
        return await apiClient.get('/auth/me');
    },
};
