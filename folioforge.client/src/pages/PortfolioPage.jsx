import { useMemo } from 'react';
import { motion } from 'framer-motion';
import { usePortfolio } from '../hooks/usePortfolio';
import { AsyncStateHandler } from '../components/AsyncStateHandler';
import { FloatingNav } from '../components/layout/FloatingNav';
import { ParticleHero } from '../features/portfolio/ParticleHero';
import { AnimatedSkillsSection } from '../features/portfolio/AnimatedSkillsSection';
import { AnimatedTimelineSection } from '../features/portfolio/AnimatedTimelineSection';
import { AnimatedProjectsSection } from '../features/portfolio/AnimatedProjectsSection';
import { AnimatedEducationSection } from '../features/portfolio/AnimatedEducationSection';
import { ContactSection } from '../features/portfolio/ContactSection';

/** Parse bio text from about section content JSON */
const parseBio = (content) => {
    try {
        const parsed = typeof content === 'string' ? JSON.parse(content) : content;
        if (typeof parsed === 'string') return parsed;
        return parsed?.content || parsed?.bio || parsed?.summary || '';
    } catch { return typeof content === 'string' ? content : ''; }
};

/**
 * Maps sectionType (case-insensitive) to the component that renders it.
 * About renders as the full-screen hero with particles + bio.
 */
const SECTION_RENDERERS = {
    about:     (section, portfolio) => (
        <ParticleHero
            key={section.id}
            title={portfolio?.title || 'Portfolio'}
            bio={parseBio(section.content)}
            onContactClick={() => document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' })}
            onDownloadClick={() => alert('CV download coming soon!')}
        />
    ),
    skills:    (section) => <AnimatedSkillsSection key={section.id} content={section.content} variant={section.variant} />,
    timeline:  (section) => <AnimatedTimelineSection key={section.id} content={section.content} variant={section.variant} />,
    projects:  (section) => <AnimatedProjectsSection key={section.id} content={section.content} variant={section.variant} />,
    contact:   (section) => <ContactSection key={section.id} variant={section.variant} />,
    education: (section) => <AnimatedEducationSection key={section.id} content={section.content} variant={section.variant} />,
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

    // All sections flow in sortOrder (about renders as hero, hero type is hidden)
    const bodySections = visibleSections.filter(
        s => s.sectionType?.toLowerCase() !== 'hero'
    );

    // Build dynamic nav items from visible sections for FloatingNav
    const hasContactSection = bodySections.some(
        s => s.sectionType?.toLowerCase() === 'contact'
    );

    const navItems = useMemo(() => {
        const items = [];
        bodySections.forEach(s => {
            const type = s.sectionType?.toLowerCase();
            items.push({
                id: type,
                label: type === 'about' ? 'Home' : s.sectionType,
                icon: type === 'about' ? '🏠' : (SECTION_ICONS[type] || '📄'),
            });
        });
        if (!items.some(i => i.id === 'contact')) {
            items.push({ id: 'contact', label: 'Contact', icon: SECTION_ICONS.contact });
        }
        return items;
    }, [bodySections]);

    const scrollToContact = () => {
        document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' });
    };

    // ── Render a single section by its type ────────────────────
    const renderSection = (section, portfolioData) => {
        const type = section.sectionType?.toLowerCase();
        const renderer = SECTION_RENDERERS[type];
        if (!renderer) return null;
        return (
            <div key={section.id} id={type}>
                {renderer(section, portfolioData)}
            </div>
        );
    };

    // ── Layout class for the body sections wrapper ─────────────
    const layoutClass =
        layout === 'sidebar' ? 'flex' : 'flex flex-col';

    return (
        <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            className="portfolio-root min-h-screen transition-colors duration-300"
            style={{
                backgroundColor,
                color: textColor,
                '--color-primary': primaryColor,
                '--color-secondary': secondaryColor,
                '--color-bg': backgroundColor,
                '--color-text': textColor,
                '--font-heading': fontHeading,
                '--font-body': fontBody,
                '--font-heading-family': `"${fontHeading}"`,
                '--font-body-family': `"${fontBody}"`,
            }}
        >
            {/* Inject themed utility classes that override Tailwind defaults */}
            <style>{`
                .portfolio-root {
                    font-family: var(--font-body-family), ui-sans-serif, system-ui, sans-serif;
                }
                
                .portfolio-root .section-heading {
                    font-family: var(--font-heading-family), ui-sans-serif, system-ui, sans-serif;
                    color: var(--color-primary) !important;
                }
                .portfolio-root .section-badge {
                    background: color-mix(in srgb, var(--color-primary) 15%, transparent) !important;
                    color: var(--color-primary) !important;
                }
                .portfolio-root .section-subtitle {
                    color: color-mix(in srgb, var(--color-text) 60%, transparent) !important;
                }
                .portfolio-root .accent-text {
                    color: var(--color-primary) !important;
                }
                .portfolio-root .accent-bg {
                    background-color: var(--color-primary) !important;
                }
            `}</style>
            {/* Floating Navigation – dynamic items */}
            <FloatingNav items={navItems} />

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
                    {bodySections.map(s => renderSection(s, portfolio))}
                    {!hasContactSection && (
                        <div id="contact">
                            <ContactSection variant="default" />
                        </div>
                    )}
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
                        <h3
                            className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r from-blue-400 to-purple-400"
                            style={{ fontFamily: `var(--font-heading), ui-sans-serif, system-ui, sans-serif`, backgroundImage: `linear-gradient(to right, ${primaryColor}, ${secondaryColor})` }}
                        >
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