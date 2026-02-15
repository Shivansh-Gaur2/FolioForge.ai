import { usePortfolio } from '../hooks/usePortfolio';
import { AsyncStateHandler } from '../components/AsyncStateHandler';
import { SkillsSection } from '../features/portfolio/SkillsSection';
import { TimelineSection } from '../features/portfolio/TimelineSection';
import { ProjectGrid } from '../features/portfolio/ProjectGrid';

/**
 * PortfolioHeader Component
 * Extracted for readability and potential reuse
 */
const PortfolioHeader = ({ title, aboutContent }) => {
    let bio = "";
    try { bio = JSON.parse(aboutContent).content; } catch {}

    return (
        <header className="text-center py-20 bg-gradient-to-b from-slate-50 to-white border-b border-slate-100">
            <div className="max-w-4xl mx-auto px-6">
                <h1 className="text-5xl font-extrabold text-slate-900 tracking-tight mb-6">{title}</h1>
                <p className="text-xl text-slate-600 leading-relaxed max-w-2xl mx-auto">
                    {bio}
                </p>
                <div className="mt-8 flex justify-center gap-4">
                    <button className="px-6 py-3 bg-slate-900 text-white font-medium rounded-lg hover:bg-slate-800 transition">Contact Me</button>
                    <button className="px-6 py-3 bg-white text-slate-900 border border-slate-300 font-medium rounded-lg hover:bg-slate-50 transition">Download CV</button>
                </div>
            </div>
        </header>
    );
};

/**
 * PortfolioContent Component
 * Renders the main portfolio content
 */
const PortfolioContent = ({ portfolio }) => {
    // Helper to find sections safely
    const getSection = (type) => 
        portfolio.sections?.find(s => s.sectionType?.toLowerCase() === type.toLowerCase());

    return (
        <div className="min-h-screen bg-white font-sans text-slate-900">
            <PortfolioHeader 
                title={portfolio.title} 
                aboutContent={getSection('About')?.content} 
            />
            
            <main className="max-w-5xl mx-auto px-6 py-16">
                {getSection('Skills') && <SkillsSection content={getSection('Skills').content} />}
                {getSection('Timeline') && <TimelineSection content={getSection('Timeline').content} />}
                {getSection('Projects') && <ProjectGrid content={getSection('Projects').content} />}
            </main>

            <footer className="text-center py-10 text-slate-400 text-sm border-t border-slate-100">
                &copy; {new Date().getFullYear()} {portfolio.title}. Powered by FolioForge AI.
            </footer>
        </div>
    );
};

/**
 * PortfolioPage - Main page component
 * 
 * Pattern: Container component that handles data fetching,
 * delegates rendering to presentational components.
 */
export const PortfolioPage = ({ id }) => {
    const { portfolio, loading, error, retry } = usePortfolio(id);

    return (
        <AsyncStateHandler
            loading={loading}
            error={error}
            data={portfolio}
            onRetry={retry}
            loadingMessage="Loading portfolio..."
            emptyMessage="Portfolio not found"
        >
            {(data) => <PortfolioContent portfolio={data} />}
        </AsyncStateHandler>
    );
};