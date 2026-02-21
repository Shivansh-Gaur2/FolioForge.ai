import { BrowserRouter, Routes, Route, Navigate, useParams } from 'react-router-dom';
import { ErrorBoundary } from './components/ErrorBoundary';
import { ThemeProvider } from './context/ThemeContext';
import { AuthProvider, useAuth } from './context/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { DashboardPage } from './pages/DashboardPage';
import { PortfolioPage } from './pages/PortfolioPage';

/**
 * App Component
 * 
 * Root component wrapping the application with:
 * - BrowserRouter for client-side routing
 * - Auth Provider for login state
 * - Theme Provider for dark/light mode
 * - Error Boundary for catching render errors
 */

/**
 * RedirectIfAuthenticated: sends logged-in users away from login/register
 */
const GuestRoute = ({ children }) => {
    const { isAuthenticated, loading } = useAuth();

    if (loading) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-slate-950">
                <div className="animate-pulse text-slate-400">Loading…</div>
            </div>
        );
    }

    if (isAuthenticated) {
        return <Navigate to="/dashboard" replace />;
    }

    return children;
};

function App() {
    return (
        <BrowserRouter>
            <ThemeProvider>
                <AuthProvider>
                    <ErrorBoundary>
                        <Routes>
                            {/* Public: auth pages */}
                            <Route path="/login" element={<GuestRoute><LoginPage /></GuestRoute>} />
                            <Route path="/register" element={<GuestRoute><RegisterPage /></GuestRoute>} />

                            {/* Protected: dashboard */}
                            <Route path="/dashboard" element={
                                <ProtectedRoute><DashboardPage /></ProtectedRoute>
                            } />

                            {/* Public: view a portfolio by id */}
                            <Route path="/portfolio/:id" element={<PortfolioPageWrapper />} />

                            {/* Default redirect */}
                            <Route path="*" element={<DefaultRedirect />} />
                        </Routes>
                    </ErrorBoundary>
                </AuthProvider>
            </ThemeProvider>
        </BrowserRouter>
    );
}

/**
 * Wrapper to pass URL param to PortfolioPage.
 */
function PortfolioPageWrapper() {
    const { id } = useParams();
    return <PortfolioPage id={id} />;
}

/**
 * Default redirect: if logged in → dashboard, otherwise → login.
 */
function DefaultRedirect() {
    const { isAuthenticated, loading } = useAuth();
    if (loading) return null;
    return <Navigate to={isAuthenticated ? '/dashboard' : '/login'} replace />;
}

export default App;