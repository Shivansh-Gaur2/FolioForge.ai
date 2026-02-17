import { ErrorBoundary } from './components/ErrorBoundary';
import { ThemeProvider } from './context/ThemeContext';
import { PortfolioPage } from './pages/PortfolioPage';

/**
 * App Component
 * 
 * Root component wrapping the application with:
 * - Theme Provider for dark/light mode
 * - Error Boundary for catching render errors
 * - Future: Router, Auth Provider, etc.
 */
function App() {
  // TODO: Get this from URL params via React Router
  const myPortfolioId = "BD55DEFB-4071-4181-92CE-820CA02CC1E4"; 

  return (
    <ThemeProvider>
      <ErrorBoundary>
        <PortfolioPage id={myPortfolioId} />
      </ErrorBoundary>
    </ThemeProvider>
  );
}

export default App;