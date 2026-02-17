import { motion } from 'framer-motion';
import { usePortfolio } from '../hooks/usePortfolio';
import { AsyncStateHandler } from '../components/AsyncStateHandler';
import { FloatingNav } from '../components/layout/FloatingNav';
import { ParticleHero } from '../features/portfolio/ParticleHero';
import { AnimatedSkillsSection } from '../features/portfolio/AnimatedSkillsSection';
import { AnimatedTimelineSection } from '../features/portfolio/AnimatedTimelineSection';
import { AnimatedProjectsSection } from '../features/portfolio/AnimatedProjectsSection';
import { ContactSection } from '../features/portfolio/ContactSection';

/**
 * PortfolioContent Component
 * Renders the stunning portfolio with full animations
 */
const PortfolioContent = ({ portfolio }) => {
    // Helper to find sections safely
    const getSection = (type) => 
        portfolio.sections?.find(s => s.sectionType?.toLowerCase() === type.toLowerCase());

    // Parse bio from About section
    let bio = "";
    try { 
        const aboutContent = getSection('About')?.content;
        if (aboutContent) bio = JSON.parse(aboutContent).content; 
    } catch {}

    const scrollToContact = () => {
        document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' });
    };

    return (
        <motion.div 
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="min-h-screen bg-white dark:bg-slate-950 
                      font-sans text-slate-900 dark:text-white
                      transition-colors duration-300"
        >
            {/* Floating Navigation */}
            <FloatingNav />

            {/* Hero Section with Particles */}
            <div id="hero">
                <ParticleHero 
                    title={portfolio.title}
                    bio={bio}
                    onContactClick={scrollToContact}
                    onDownloadClick={() => alert('CV download coming soon!')}
                />
            </div>
            
            {/* Skills Section */}
            {getSection('Skills') && (
                <AnimatedSkillsSection content={getSection('Skills').content} />
            )}

            {/* Experience Timeline */}
            {getSection('Timeline') && (
                <AnimatedTimelineSection content={getSection('Timeline').content} />
            )}

            {/* Projects Showcase */}
            {getSection('Projects') && (
                <AnimatedProjectsSection content={getSection('Projects').content} />
            )}

            {/* Contact Section */}
            <ContactSection />

            {/* Footer */}
            <footer className="relative py-12 text-center
                             bg-slate-900 dark:bg-black text-white">
                <div className="max-w-5xl mx-auto px-6">
                    <motion.div
                        initial={{ opacity: 0, y: 20 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        className="mb-6"
                    >
                        <h3 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                            {portfolio.title}
                        </h3>
                    </motion.div>
                    
                    <p className="text-slate-400 text-sm">
                        &copy; {new Date().getFullYear()} All rights reserved.
                    </p>
                    <p className="text-slate-500 text-xs mt-2">
                        Crafted with ❤️ using FolioForge AI
                    </p>
                </div>
            </footer>
        </motion.div>
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