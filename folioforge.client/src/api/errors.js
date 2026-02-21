/**
 * API Error Classes
 * 
 * Pattern: Typed error hierarchy for precise error handling.
 * Benefits:
 * - Instanceof checks for error-specific handling
 * - Consistent error structure across the app
 * - Easy to extend for new error types
 */

/**
 * Base API Error - all API errors extend this
 */
export class ApiError extends Error {
    constructor(message, statusCode = null, details = null) {
        super(message);
        this.name = 'ApiError';
        this.statusCode = statusCode;
        this.details = details;
        this.timestamp = new Date().toISOString();
    }

    toJSON() {
        return {
            name: this.name,
            message: this.message,
            statusCode: this.statusCode,
            details: this.details,
            timestamp: this.timestamp,
        };
    }
}

/**
 * Network Error - connection failed, timeout, etc.
 */
export class NetworkError extends ApiError {
    constructor(message = 'Unable to connect to the server. Please check your connection.') {
        super(message, null);
        this.name = 'NetworkError';
        this.isRetryable = true;
    }
}

/**
 * Validation Error - 400 Bad Request with field-level errors
 */
export class ValidationError extends ApiError {
    constructor(message, fieldErrors = {}) {
        super(message, 400);
        this.name = 'ValidationError';
        this.fieldErrors = fieldErrors;
        this.isRetryable = false;
    }
}

/**
 * Not Found Error - 404
 */
export class NotFoundError extends ApiError {
    constructor(resource = 'Resource') {
        super(`${resource} not found`, 404);
        this.name = 'NotFoundError';
        this.isRetryable = false;
    }
}

/**
 * Unauthorized Error - 401
 */
export class UnauthorizedError extends ApiError {
    constructor(message = 'Authentication required') {
        super(message, 401);
        this.name = 'UnauthorizedError';
        this.isRetryable = false;
    }
}

/**
 * Server Error - 500+
 */
export class ServerError extends ApiError {
    constructor(message = 'An unexpected server error occurred') {
        super(message, 500);
        this.name = 'ServerError';
        this.isRetryable = true;
    }
}

/**
 * Factory function to create appropriate error from HTTP response
 */
export const createErrorFromResponse = (status, data) => {
    const message = data?.error || data?.message || data?.title || 'An unexpected error occurred';
    
    switch (status) {
        case 400:
            return new ValidationError(message, data?.errors);
        case 401:
            return new UnauthorizedError(message);
        case 404:
            return new NotFoundError();
        case 409:
            return new ApiError(message, 409, data);
        case 500:
        case 502:
        case 503:
        case 504:
            return new ServerError(message);
        default:
            return new ApiError(message, status, data);
    }
};
