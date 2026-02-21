/**
 * Centralized Environment Configuration
 * 
 * Pattern: Single source of truth for all environment variables.
 * Benefits:
 * - Type safety through explicit accessors
 * - Validation at startup (fail-fast)
 * - Easy mocking in tests
 * - No scattered import.meta.env calls throughout codebase
 */

const getEnvVar = (key, fallback = undefined) => {
    const value = import.meta.env[key];
    if (value === undefined && fallback === undefined) {
        console.warn(`[Config] Missing required env var: ${key}`);
    }
    return value ?? fallback;
};

const parseBoolean = (value, fallback = false) => {
    if (value === undefined) return fallback;
    return value === 'true' || value === '1';
};

const parseNumber = (value, fallback) => {
    if (value === undefined) return fallback;
    const parsed = Number(value);
    return isNaN(parsed) ? fallback : parsed;
};

/**
 * Application configuration object.
 * All environment variables should be accessed through this object.
 */
export const config = Object.freeze({
    // API Settings
    api: {
        baseUrl: getEnvVar('VITE_API_BASE_URL', 'https://localhost:7245/api'),
        timeout: parseNumber(getEnvVar('VITE_API_TIMEOUT'), 15000),
    },

    // Multi-Tenancy
    tenant: {
        // The tenant identifier sent with every API request via X-Tenant-Id header.
        // Override with VITE_TENANT_ID env var. Falls back to 'default' for development.
        identifier: getEnvVar('VITE_TENANT_ID', 'default'),
    },

    // Feature Flags
    features: {
        analytics: parseBoolean(getEnvVar('VITE_ENABLE_ANALYTICS'), false),
        debugLogging: parseBoolean(getEnvVar('VITE_ENABLE_DEBUG_LOGGING'), import.meta.env.DEV),
    },

    // Runtime info
    env: {
        isDevelopment: import.meta.env.DEV,
        isProduction: import.meta.env.PROD,
        mode: import.meta.env.MODE,
    },
});

// Validate critical config at startup
if (config.env.isProduction && !config.api.baseUrl.startsWith('https://')) {
    console.error('[Config] Production API URL must use HTTPS');
}

export default config;
