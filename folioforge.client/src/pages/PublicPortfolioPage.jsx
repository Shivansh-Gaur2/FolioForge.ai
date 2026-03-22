import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { PortfolioService } from '../services/portfolioService';
import { AsyncStateHandler } from '../components/AsyncStateHandler';

// Re-use the same PortfolioContent component (it's the same visual output)
// We lazy-import the PortfolioPage module to access PortfolioContent indirectly.
// Since PortfolioPage exports a named component that wraps PortfolioContent,
// we replicate the same data-fetching pattern here with the public API.

// Import all the section renderers and components used by PortfolioPage
import { motion } from 'framer-motion';
import { useMemo } from 'react';
import { ParticleHero } from '../features/portfolio/ParticleHero';
import { AnimatedSkillsSection } from '../features/portfolio/AnimatedSkillsSection';
import { AnimatedTimelineSection } from '../features/portfolio/AnimatedTimelineSection';
import { AnimatedProjectsSection } from '../features/portfolio/AnimatedProjectsSection';
import { ContactSection } from '../features/portfolio/ContactSection';
import { AnimatedEducationSection } from '../features/portfolio/AnimatedEducationSection';
import { FloatingNav } from '../components/layout/FloatingNav';

/** Parse bio text from section content */
const parseBio = (content) => {
    if (!content) return '';
    try {
        const parsed = typeof content === 'string' ? JSON.parse(content) : content;
        return parsed.content || parsed.bio || parsed.summary || parsed.text || '';
    } catch {
        return typeof content === 'string' ? content : '';
    }
};

const SECTION_RENDERERS = {
    about: (section, portfolio) => (
        <ParticleHero
            key={section.id}
            title={portfolio?.title || 'Portfolio'}
            bio={parseBio(section.content)}
            onContactClick={() => document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' })}
            onDownloadClick={() => {}}
        />
    ),
    skills: (section) => <AnimatedSkillsSection key={section.id} content={section.content} variant={section.variant} />,
    timeline: (section) => <AnimatedTimelineSection key={section.id} content={section.content} variant={section.variant} />,
    projects: (section) => <AnimatedProjectsSection key={section.id} content={section.content} variant={section.variant} />,
    contact: (section) => <ContactSection key={section.id} content={section.content} variant={section.variant} />,
    education: (section) => <AnimatedEducationSection key={section.id} content={section.content} variant={section.variant} />,
};

const SECTION_ICONS = {
    skills: '⚡', timeline: '💼', projects: '🚀', contact: '✉️',
    education: '🎓', about: '👤', hero: '🏠',
};

/**
 * PublicPortfolioContent — renders a public portfolio with full styling.
 * Identical to PortfolioContent in PortfolioPage.jsx but used for the public route.
 */
const PublicPortfolioContent = ({ portfolio }) => {
    const theme = portfolio.theme || {};
    const primaryColor = theme.primaryColor || '#3B82F6';
    const secondaryColor = theme.secondaryColor || '#10B981';
    const backgroundColor = theme.backgroundColor || '#FFFFFF';
    const textColor = theme.textColor || '#1F2937';
    const fontHeading = theme.fontHeading || 'Inter';
    const fontBody = theme.fontBody || 'Inter';
    const layout = theme.layout || 'single-column';

    const visibleSections = useMemo(() =>
        (portfolio.sections || [])
            .filter(s => s.isVisible !== false)
            .sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0)),
        [portfolio.sections]
    );

    const bodySections = visibleSections.filter(
        s => s.sectionType?.toLowerCase() !== 'hero'
    );

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

    const layoutClass = layout === 'sidebar' ? 'flex' : 'flex flex-col';

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

            <FloatingNav items={navItems} />

            <div className={layoutClass}>
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

                <main className="flex-1 min-w-0">
                    {bodySections.map(s => renderSection(s, portfolio))}
                    {!hasContactSection && (
                        <div id="contact">
                            <ContactSection variant="default" />
                        </div>
                    )}
                </main>
            </div>

            {/* Footer with FolioForge watermark */}
            <footer className="relative py-12 text-center bg-slate-900 dark:bg-black text-white">
                <div className="max-w-5xl mx-auto px-6">
                    <motion.div
                        initial={{ opacity: 0, y: 20 }}
                        whileInView={{ opacity: 1, y: 0 }}
                        className="mb-6"
                    >
                        <h3
                            className="text-2xl font-bold bg-clip-text text-transparent bg-gradient-to-r"
                            style={{
                                fontFamily: `var(--font-heading), ui-sans-serif, system-ui, sans-serif`,
                                backgroundImage: `linear-gradient(to right, ${primaryColor}, ${secondaryColor})`,
                            }}
                        >
                            {portfolio.title}
                        </h3>
                    </motion.div>
                    <p className="text-slate-400 text-sm">
                        &copy; {new Date().getFullYear()} All rights reserved.
                    </p>
                    {/* FolioForge watermark — hidden for Pro users */}
                    {portfolio.showWatermark !== false && (
                        <a
                            href="https://folioforge.ai"
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex items-center gap-1.5 mt-4 px-3 py-1.5 rounded-full bg-white/5 border border-white/10 text-slate-400 hover:text-white hover:border-white/20 transition-all text-xs"
                        >
                            <span>⚡</span>
                            <span>Built with <strong className="text-white">FolioForge</strong></span>
                        </a>
                    )}
                </div>
            </footer>
        </motion.div>
    );
};

/**
 * PublicPortfolioPage — fetches and renders a portfolio by slug (no auth).
 * Route: /p/:slug
 */
export const PublicPortfolioPage = () => {
    const { slug } = useParams();
    const [portfolio, setPortfolio] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchPortfolio = useCallback(async () => {
        if (!slug) return;
        setLoading(true);
        setError(null);
        try {
            const data = await PortfolioService.getPublicBySlug(slug);
            setPortfolio(data);
        } catch (err) {
            setError(err);
        } finally {
            setLoading(false);
        }
    }, [slug]);

    useEffect(() => {
        fetchPortfolio();
    }, [fetchPortfolio]);

    return (
        <AsyncStateHandler
            loading={loading}
            error={error}
            data={portfolio}
            onRetry={fetchPortfolio}
            loadingMessage="Loading portfolio..."
            emptyMessage="Portfolio not found or is not published."
        >
            {(data) => <PublicPortfolioContent portfolio={data} />}
        </AsyncStateHandler>
    );
};
