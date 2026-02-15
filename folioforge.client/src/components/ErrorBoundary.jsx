import { Component } from 'react';
import { config } from '../config/environment';

/**
 * Error Boundary Component
 * 
 * Pattern: React Error Boundary for catching render errors.
 * Why class component: Error boundaries MUST be class components (React limitation).
 * 
 * Trade-offs:
 * - Could use react-error-boundary library, but keeping deps minimal
 * - Manual implementation gives us full control over error reporting
 * 
 * Features:
 * - Catches JavaScript errors in child component tree
 * - Logs errors for debugging
 * - Provides user-friendly fallback UI
 * - Reset capability to retry rendering
 */
export class ErrorBoundary extends Component {
    constructor(props) {
        super(props);
        this.state = { 
            hasError: false, 
            error: null,
            errorInfo: null,
        };
    }

    static getDerivedStateFromError(error) {
        // Update state to trigger fallback UI
        return { hasError: true, error };
    }

    componentDidCatch(error, errorInfo) {
        // Log error details
        this.setState({ errorInfo });
        
        if (config.features.debugLogging) {
            console.error('[ErrorBoundary] Caught error:', error);
            console.error('[ErrorBoundary] Component stack:', errorInfo.componentStack);
        }

        // Future: Send to error tracking service (Sentry, DataDog, etc.)
        // errorTrackingService.captureException(error, { extra: errorInfo });
    }

    handleReset = () => {
        this.setState({ 
            hasError: false, 
            error: null, 
            errorInfo: null 
        });
        
        // Optional: Notify parent of reset
        this.props.onReset?.();
    };

    render() {
        if (this.state.hasError) {
            // Allow custom fallback UI via props
            if (this.props.fallback) {
                return this.props.fallback({
                    error: this.state.error,
                    resetError: this.handleReset,
                });
            }

            // Default fallback UI
            return (
                <div className="min-h-screen flex items-center justify-center bg-slate-50 p-6">
                    <div className="max-w-md w-full bg-white rounded-xl shadow-lg p-8 text-center">
                        <div className="w-16 h-16 mx-auto mb-4 bg-red-100 rounded-full flex items-center justify-center">
                            <svg 
                                className="w-8 h-8 text-red-600" 
                                fill="none" 
                                viewBox="0 0 24 24" 
                                stroke="currentColor"
                            >
                                <path 
                                    strokeLinecap="round" 
                                    strokeLinejoin="round" 
                                    strokeWidth={2} 
                                    d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" 
                                />
                            </svg>
                        </div>
                        
                        <h1 className="text-xl font-bold text-slate-900 mb-2">
                            Something went wrong
                        </h1>
                        
                        <p className="text-slate-600 mb-6">
                            We encountered an unexpected error. Please try again or contact support if the issue persists.
                        </p>

                        {config.env.isDevelopment && this.state.error && (
                            <details className="mb-6 text-left bg-slate-100 rounded-lg p-4">
                                <summary className="cursor-pointer text-sm font-medium text-slate-700">
                                    Error Details (Dev Only)
                                </summary>
                                <pre className="mt-2 text-xs text-red-600 overflow-auto max-h-40">
                                    {this.state.error.toString()}
                                    {this.state.errorInfo?.componentStack}
                                </pre>
                            </details>
                        )}

                        <div className="flex gap-3 justify-center">
                            <button
                                onClick={this.handleReset}
                                className="px-6 py-2 bg-slate-900 text-white font-medium rounded-lg hover:bg-slate-800 transition"
                            >
                                Try Again
                            </button>
                            <button
                                onClick={() => window.location.reload()}
                                className="px-6 py-2 bg-white text-slate-700 border border-slate-300 font-medium rounded-lg hover:bg-slate-50 transition"
                            >
                                Reload Page
                            </button>
                        </div>
                    </div>
                </div>
            );
        }

        return this.props.children;
    }
}

export default ErrorBoundary;
