import { useMemo } from 'react';
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
 * Maps sectionType (case-insensitive) to the component that renders it.
 * Hero/About are handled separately outside this map.
 */
const SECTION_RENDERERS = {
    skills:    (section) => <AnimatedSkillsSection key={section.id} content={section.content} />,
    timeline:  (section) => <AnimatedTimelineSection key={section.id} content={section.content} />,
    projects:  (section) => <AnimatedProjectsSection key={section.id} content={section.content} />,
    contact:   (section) => <ContactSection key={section.id} />,
};

/** Section-type icons used for sidebar navigation */
const SECTION_ICONS = {
    skills: '⚡', timeline: '💼', projects: '🚀', contact: '✉️',
    education: '🎓', about: '👤', hero: '🏠', markdown: '📝',
};

/**
 * PortfolioContent Component
 * Renders the portfolio with full animations,
 * respecting theme customization (layout, colours, fonts)
 * and section ordering / visibility from the API.
 */
const PortfolioContent = ({ portfolio }) => {
    // Theme customization
    const theme = portfolio.theme || {};
    const primaryColor   = theme.primaryColor   || '#3B82F6';
    const secondaryColor = theme.secondaryColor || '#10B981';
    const backgroundColor = theme.backgroundColor || '#FFFFFF';
    const textColor      = theme.textColor      || '#1F2937';
    const fontHeading    = theme.fontHeading     || 'Inter';
    const fontBody       = theme.fontBody        || 'Inter';
    const layout         = theme.layout          || 'single-column';

    // ── Resolve ordered, visible sections ──────────────────────
    const visibleSections = useMemo(() =>
        (portfolio.sections || [])
            .filter(s => s.isVisible !== false)
            .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
        [portfolio.sections]
    );

    // About section is used for the hero bio; hero rendering is handled via ParticleHero
    const aboutSection = visibleSections.find(s => s.sectionType?.toLowerCase() === 'about');

    // Parse bio from About section
    let bio = '';
    try {
        const aboutContent = aboutSection?.content;
        if (aboutContent) bio = JSON.parse(aboutContent).content;
    } catch { /* bio parse failed, use default */ }

    // Sections that are NOT hero/about — these follow the chosen layout
    const bodySections = visibleSections.filter(
        s => !['hero', 'about'].includes(s.sectionType?.toLowerCase())
    );

    // Build dynamic nav items from visible sections for FloatingNav
    const navItems = useMemo(() => {
        const items = [{ id: 'hero', label: 'Home', icon: '🏠' }];
        bodySections.forEach(s => {
            const type = s.sectionType?.toLowerCase();
            items.push({
                id: type,
                label: s.sectionType,
                icon: SECTION_ICONS[type] || '📄',
            });
        });
        return items;
    }, [bodySections]);

    const scrollToContact = () => {
        document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' });
    };

    // ── Render a single section by its type ────────────────────
    const renderSection = (section) => {
        const type = section.sectionType?.toLowerCase();
        const renderer = SECTION_RENDERERS[type];
        if (!renderer) return null;
        return (
            <div key={section.id} id={type}>
                {renderer(section)}
            </div>
        );
    };

    // ── Layout class for the body sections wrapper ─────────────
    const layoutClass =
        layout === 'two-column' ? 'grid grid-cols-1 md:grid-cols-2 gap-0' :
        layout === 'sidebar'    ? 'flex' :
                                  'flex flex-col';

    return (
        <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="min-h-screen transition-colors duration-300"
            style={{
                backgroundColor,
                color: textColor,
                fontFamily: fontBody,
                '--color-primary': primaryColor,
                '--color-secondary': secondaryColor,
                '--color-bg': backgroundColor,
                '--color-text': textColor,
                '--font-heading': fontHeading,
                '--font-body': fontBody,
            }}
        >
            {/* Floating Navigation – dynamic items */}
            <FloatingNav items={navItems} />

            {/* Hero Section with Particles – always full-width at top */}
            <div id="hero">
                <ParticleHero
                    title={portfolio.title}
                    bio={bio}
                    onContactClick={scrollToContact}
                    onDownloadClick={() => alert('CV download coming soon!')}
                />
            </div>

            {/* Body sections – rendered in sortOrder, respecting chosen layout */}
            <div className={layoutClass}>
                {/* Sidebar layout: persistent side-nav */}
                {layout === 'sidebar' && (
                    <aside
                        className="hidden md:flex w-56 min-h-full flex-col flex-shrink-0 sticky top-0 self-start p-6"
                        style={{ backgroundColor: primaryColor, height: '100vh' }}
                    >
                        <h3
                            className="text-lg font-bold text-white truncate"
                            style={{ fontFamily: fontHeading }}
                        >
                            {portfolio.title}
                        </h3>
                        <nav className="mt-8 space-y-3 flex-1">
                            {bodySections.map(s => {
                                const type = s.sectionType?.toLowerCase();
                                return (
                                    <a
                                        key={s.id}
                                        href={`#${type}`}
                                        className="flex items-center gap-2 text-sm text-white/70 hover:text-white transition-colors"
                                    >
                                        <span>{SECTION_ICONS[type] || '📄'}</span>
                                        {s.sectionType}
                                    </a>
                                );
                            })}
                        </nav>
                    </aside>
                )}

                {/* Main content area */}
                <main className="flex-1 min-w-0">
                    {bodySections.map(renderSection)}
                </main>
            </div>

            {/* Footer */}
            <footer className="relative py-12 text-center bg-slate-900 dark:bg-black text-white">
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