import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { motion } from 'framer-motion';
import { useCustomizationStore } from '../stores/useCustomizationStore';
import { CustomizationPanel } from '../components/customization/CustomizationPanel';
import { ArrowLeft } from 'lucide-react';
import { AnimatedSkillsSection } from '../features/portfolio/AnimatedSkillsSection';
import { AnimatedTimelineSection } from '../features/portfolio/AnimatedTimelineSection';
import { AnimatedProjectsSection } from '../features/portfolio/AnimatedProjectsSection';
import { ContactSection } from '../features/portfolio/ContactSection';
import { AnimatedEducationSection } from '../features/portfolio/AnimatedEducationSection';
import { ParticleHero } from '../features/portfolio/ParticleHero';

/** Parse bio text from about section content JSON */
const parseBio = (content) => {
    try {
        const parsed = typeof content === 'string' ? JSON.parse(content) : content;
        if (typeof parsed === 'string') return parsed;
        return parsed?.content || parsed?.bio || parsed?.summary || '';
    } catch { return typeof content === 'string' ? content : ''; }
};

/** Map section types to the real rendered components */
const SECTION_RENDERERS = {
    about:    (section, portfolio) => (
        <ParticleHero
            key={section.id}
            title={portfolio?.title || 'Portfolio'}
            bio={parseBio(section.content)}
            onContactClick={() => document.getElementById('contact')?.scrollIntoView({ behavior: 'smooth' })}
            onDownloadClick={() => {}}
        />
    ),
    skills:   (section) => <AnimatedSkillsSection key={section.id} content={section.content} variant={section.variant} />,
    timeline: (section) => <AnimatedTimelineSection key={section.id} content={section.content} variant={section.variant} />,
    projects: (section) => <AnimatedProjectsSection key={section.id} content={section.content} variant={section.variant} />,
    contact:  (section) => <ContactSection key={section.id} content={section.content} variant={section.variant} />,
    education: (section) => <AnimatedEducationSection key={section.id} content={section.content} variant={section.variant} />,
};

const SECTION_ICONS = {
    skills: '⚡', timeline: '💼', projects: '🚀', contact: '✉️',
    education: '🎓', about: '👤', hero: '🏠', markdown: '📝',
};

/**
 * PortfolioEditorPage
 * 
 * Split screen: left panel for customization controls, right side for live preview.
 * Loads the portfolio data and populates the customization store.
 */
export const PortfolioEditorPage = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const { isLoading, loadCustomization, portfolio, sections,
            primaryColor, secondaryColor, backgroundColor, textColor,
            fontHeading, fontBody, layout } = useCustomizationStore();

    useEffect(() => {
        if (id) loadCustomization(id);
    }, [id, loadCustomization]);

    if (isLoading) {
        return (
            <div className="h-screen flex items-center justify-center bg-slate-950">
                <div className="flex items-center gap-3 text-slate-400">
                    <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10"
                                stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor"
                              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    Loading portfolio…
                </div>
            </div>
        );
    }

    const visibleSections = sections
        .filter(s => s.isVisible)
        .sort((a, b) => a.sortOrder - b.sortOrder);

    // All sections flow in sortOrder (about renders as hero, hero type is hidden)
    const bodySections = visibleSections.filter(
        s => s.sectionType?.toLowerCase() !== 'hero'
    );

    const layoutClass =
        layout === 'sidebar' ? 'flex' : 'flex flex-col';

    return (
        <div className="h-screen flex bg-slate-950 text-white">
            {/* Left: Customization Panel */}
            <CustomizationPanel portfolioId={id} />

            {/* Right: Live Preview */}
            <div className="flex-1 flex flex-col overflow-hidden">
                {/* Preview toolbar */}
                <div className="h-12 border-b border-white/10 bg-slate-900/80 backdrop-blur
                                flex items-center justify-between px-4 flex-shrink-0">
                    <button
                        onClick={() => navigate('/dashboard')}
                        className="flex items-center gap-1.5 text-sm text-slate-400 hover:text-white transition-colors"
                    >
                        <ArrowLeft size={16} />
                        Back to Dashboard
                    </button>
                    <div className="flex items-center gap-3">
                        <span className="text-xs text-slate-500">Live Preview</span>
                        <button
                            onClick={() => navigate(`/portfolio/${id}`)}
                            className="px-3 py-1 text-xs font-medium rounded-md
                                       bg-white/10 hover:bg-white/20 border border-white/10
                                       transition-colors"
                        >
                            View Full Page →
                        </button>
                    </div>
                </div>

                {/* Preview area */}
                <div
                    className="flex-1 overflow-auto portfolio-root"
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
                    {/* Font styles applied via CSS custom properties */}
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
                    {/* Body sections in chosen layout */}
                    <motion.div
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        className={`min-h-full ${layoutClass}`}
                    >
                        {/* Sidebar layout nav */}
                        {layout === 'sidebar' && (
                            <aside
                                className="w-56 min-h-full p-6 flex-shrink-0 sticky top-0 self-start"
                                style={{ backgroundColor: primaryColor, height: '100vh' }}
                            >
                                <h3
                                    className="text-lg font-bold text-white"
                                    style={{ fontFamily: fontHeading }}
                                >
                                    {portfolio?.title || 'Portfolio'}
                                </h3>
                                <nav className="mt-6 space-y-2">
                                    {bodySections.map(s => {
                                        const type = s.sectionType?.toLowerCase();
                                        return (
                                            <a
                                                key={s.id}
                                                href={`#section-${s.id}`}
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
                            {bodySections.length === 0 ? (
                                <div className="flex items-center justify-center h-64 text-center p-12">
                                    <div>
                                        <p className="text-4xl mb-4">📄</p>
                                        <p className="text-lg font-medium" style={{ color: textColor }}>
                                            No sections to show
                                        </p>
                                        <p className="text-sm mt-2" style={{ color: textColor, opacity: 0.5 }}>
                                            Upload a resume to populate your portfolio, then customize it here.
                                        </p>
                                    </div>
                                </div>
                            ) : (
                                bodySections.map(section => {
                                    const type = section.sectionType?.toLowerCase();
                                    const renderer = SECTION_RENDERERS[type];
                                    if (renderer) {
                                        return (
                                            <div key={section.id} id={`section-${section.id}`}>
                                                {renderer(section, portfolio)}
                                            </div>
                                        );
                                    }
                                    return (
                                        <SectionPreviewFallback
                                            key={section.id}
                                            section={section}
                                            colors={{ primaryColor, secondaryColor, textColor }}
                                            fonts={{ fontHeading, fontBody }}
                                        />
                                    );
                                })
                            )}
                        </main>
                    </motion.div>
                </div>
            </div>
        </div>
    );
};

/**
 * SectionPreviewFallback
 * 
 * Renders a preview card for section types that don't have
 * a dedicated component (e.g. Education, Markdown).
 */
const SectionPreviewFallback = ({ section, colors, fonts }) => {
    let contentSummary = '';
    try {
        const parsed = JSON.parse(section.content);
        if (typeof parsed === 'string') {
            contentSummary = parsed;
        } else if (parsed.content) {
            contentSummary = parsed.content;
        } else if (Array.isArray(parsed)) {
            contentSummary = `${parsed.length} items`;
        } else {
            contentSummary = Object.keys(parsed).join(', ');
        }
    } catch {
        contentSummary = section.content?.substring(0, 100) || '';
    }

    return (
        <motion.section
            id={`section-${section.id}`}
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="py-12 px-8 border-b"
            style={{ borderColor: `${colors.textColor}15` }}
        >
            <div className="max-w-3xl mx-auto">
                <div className="flex items-center gap-3 mb-4">
                    <h2
                        className="text-2xl font-bold"
                        style={{ color: colors.primaryColor, fontFamily: fonts.fontHeading }}
                    >
                        {section.sectionType}
                    </h2>
                    <span
                        className="text-xs px-2 py-0.5 rounded-full"
                        style={{
                            backgroundColor: `${colors.secondaryColor}20`,
                            color: colors.secondaryColor,
                        }}
                    >
                        {section.variant}
                    </span>
                </div>
                {contentSummary && (
                    <p
                        className="text-sm leading-relaxed"
                        style={{ color: colors.textColor, opacity: 0.7, fontFamily: fonts.fontBody }}
                    >
                        {contentSummary.length > 200
                            ? contentSummary.substring(0, 200) + '…'
                            : contentSummary}
                    </p>
                )}
                <div
                    className="mt-4 h-24 rounded-lg border-2 border-dashed flex items-center justify-center text-sm"
                    style={{
                        borderColor: `${colors.textColor}20`,
                        color: `${colors.textColor}40`,
                    }}
                >
                    Full section renders on the portfolio view page
                </div>
            </div>
        </motion.section>
    );
};