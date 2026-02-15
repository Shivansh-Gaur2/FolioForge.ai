/**
 * AsyncStateHandler Component
 * 
 * Pattern: Render props / children-as-function for async state management.
 * 
 * Why this pattern:
 * - Eliminates repetitive if/else chains in every page
 * - Consistent loading/error UX across the app
 * - Easy to customize via props
 * - Composable with other patterns
 * 
 * Trade-offs:
 * - Slightly more indirection than inline if/else
 * - Could use Suspense but it's still experimental for data fetching
 */
import { NetworkError, NotFoundError, ServerError } from '../api/errors';

const LoadingSpinner = ({ message = 'Loading...' }) => (
    <div className="flex flex-col h-64 items-center justify-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-slate-900 mb-4"></div>
        <p className="text-slate-500 text-sm">{message}</p>
    </div>
);

const ErrorDisplay = ({ error, onRetry }) => {
    // Determine error type for tailored messaging
    let title = 'Something went wrong';
    let message = 'An unexpected error occurred. Please try again.';
    let icon = '⚠️';
    let canRetry = true;

    if (error instanceof NetworkError) {
        title = 'Connection Error';
        message = error.message;
        icon = '🔌';
    } else if (error instanceof NotFoundError) {
        title = 'Not Found';
        message = 'The requested resource could not be found.';
        icon = '🔍';
        canRetry = false;
    } else if (error instanceof ServerError) {
        title = 'Server Error';
        message = 'Our servers are having trouble. Please try again later.';
        icon = '🔧';
    } else if (typeof error === 'string') {
        message = error;
    } else if (error?.message) {
        message = error.message;
    }

    return (
        <div className="flex flex-col items-center justify-center min-h-64 p-8 text-center">
            <span className="text-4xl mb-4">{icon}</span>
            <h2 className="text-xl font-bold text-slate-900 mb-2">{title}</h2>
            <p className="text-slate-600 mb-6 max-w-md">{message}</p>
            {canRetry && onRetry && (
                <button
                    onClick={onRetry}
                    className="px-6 py-2 bg-slate-900 text-white font-medium rounded-lg hover:bg-slate-800 transition"
                >
                    Try Again
                </button>
            )}
        </div>
    );
};

const EmptyState = ({ message = 'No data available', children }) => (
    <div className="flex flex-col items-center justify-center min-h-64 p-8 text-center">
        <span className="text-4xl mb-4">📭</span>
        <p className="text-slate-500">{message}</p>
        {children}
    </div>
);

/**
 * AsyncStateHandler - Handles loading, error, empty, and success states
 * 
 * @param {Object} props
 * @param {boolean} props.loading - Is data loading?
 * @param {Error|string|null} props.error - Error object or message
 * @param {any} props.data - The async data
 * @param {Function} props.onRetry - Retry callback for error state
 * @param {Function|ReactNode} props.children - Render prop receiving data
 * @param {string} props.loadingMessage - Custom loading message
 * @param {string} props.emptyMessage - Custom empty state message
 * @param {Function} props.isEmpty - Custom empty check function
 */
export const AsyncStateHandler = ({
    loading,
    error,
    data,
    onRetry,
    children,
    loadingMessage,
    emptyMessage,
    isEmpty = (d) => !d || (Array.isArray(d) && d.length === 0),
}) => {
    if (loading) {
        return <LoadingSpinner message={loadingMessage} />;
    }

    if (error) {
        return <ErrorDisplay error={error} onRetry={onRetry} />;
    }

    if (isEmpty(data)) {
        return <EmptyState message={emptyMessage} />;
    }

    // Support both render props and regular children
    if (typeof children === 'function') {
        return children(data);
    }

    return children;
};

export default AsyncStateHandler;
