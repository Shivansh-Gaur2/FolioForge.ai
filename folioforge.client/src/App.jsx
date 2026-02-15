import { ErrorBoundary } from './components/ErrorBoundary';
import { PortfolioPage } from './pages/PortfolioPage';

/**
 * App Component
 * 
 * Root component wrapping the application with:
 * - Error Boundary for catching render errors
 * - Future: Router, Auth Provider, Theme Provider, etc.
 */
function App() {
  // TODO: Get this from URL params via React Router
  const myPortfolioId = "6E075C99-C11A-43E4-8B6F-351E2D342D9D"; 

  return (
    <ErrorBoundary>
      <PortfolioPage id={myPortfolioId} />
    </ErrorBoundary>
  );
}

export default App;