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
    (error) => {
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

        // Auto-clear auth on 401 (token expired / invalid)
        if (status === 401) {
            localStorage.removeItem('ff_token');
            localStorage.removeItem('ff_user');
        }

        return Promise.reject(createErrorFromResponse(status, data));
    }
);

export default apiClient;
