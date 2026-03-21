import axios from 'axios';
import { config } from '../config/environment';
import { 
    NetworkError, 
    createErrorFromResponse 
} from './errors';

/**
 * Axios API Client
 * 
 * Pattern: Centralized HTTP client with interceptors.
 * Trade-offs:
 * - Axios adds ~13KB gzip, but provides robust features (interceptors, cancellation)
 * - Could use native fetch, but would need to reimplement retry/error handling
 * 
 * Features:
 * - Configurable base URL via environment
 * - Typed error handling
 * - Request/response logging in development
 * - Automatic retry for network errors (future enhancement)
 */
const apiClient = axios.create({
    baseURL: config.api.baseUrl,
    timeout: config.api.timeout,
    headers: {
        'Content-Type': 'application/json',
    },
});

// Request interceptor - logging and auth token injection
apiClient.interceptors.request.use(
    (requestConfig) => {
        if (config.features.debugLogging) {
            console.log(`[API] ${requestConfig.method?.toUpperCase()} ${requestConfig.url}`);
        }
        
        // Multi-Tenancy: Inject tenant identifier into every request
        if (config.tenant.identifier) {
            requestConfig.headers['X-Tenant-Id'] = config.tenant.identifier;
        }

        // Auth: Inject JWT Bearer token if present
        const token = localStorage.getItem('ff_token');
        if (token) {
            requestConfig.headers['Authorization'] = `Bearer ${token}`;
        }
        
        return requestConfig;
    },
    (error) => Promise.reject(error)
);

// Response interceptor - error normalization
apiClient.interceptors.response.use(
    (response) => {
        // Unwrap data for cleaner consumption
        return response.data;
    },
    async (error) => {
        // Network errors (no response from server)
        if (!error.response) {
            if (config.features.debugLogging) {
                console.error('[API] Network error:', error.message);
            }
            return Promise.reject(new NetworkError(
                error.code === 'ECONNABORTED' 
                    ? 'Request timed out. Please try again.'
                    : 'Unable to connect to the server. Please check if the backend is running.'
            ));
        }

        // HTTP error responses
        const { status, data } = error.response;
        
        if (config.features.debugLogging) {
            console.error(`[API] Error ${status}:`, data);
        }

        // Auto-refresh on 401 (access token expired)
        // Skip refresh attempts for auth endpoints to avoid infinite loops
        const originalRequest = error.config;
        if (status === 401 && !originalRequest._retry && !originalRequest.url?.includes('/auth/')) {
            originalRequest._retry = true;

            try {
                const refreshToken = localStorage.getItem('ff_refresh_token');
                const accessToken = localStorage.getItem('ff_token');
                if (refreshToken && accessToken) {
                    const refreshResponse = await apiClient.post('/auth/refresh', {
                        accessToken,
                        refreshToken,
                    });
                    // Store new tokens
                    localStorage.setItem('ff_token', refreshResponse.accessToken);
                    localStorage.setItem('ff_refresh_token', refreshResponse.refreshToken);
                    // Retry the original request with the new token
                    originalRequest.headers['Authorization'] = `Bearer ${refreshResponse.accessToken}`;
                    return apiClient(originalRequest);
                }
            } catch {
                // Refresh failed — clear auth and force re-login
            }

            localStorage.removeItem('ff_token');
            localStorage.removeItem('ff_refresh_token');
            localStorage.removeItem('ff_user');
        }

        return Promise.reject(createErrorFromResponse(status, data));
    }
);

export default apiClient;
