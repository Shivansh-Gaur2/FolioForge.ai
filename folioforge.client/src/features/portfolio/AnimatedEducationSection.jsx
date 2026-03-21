import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';
import { ScrollReveal } from '../../components/animations/ScrollReveal';

/**
 * Parse education content from the AI-extracted JSON.
 * Expects: { items: [{ Institution, Degree, Year, GPA, Points/Description }] }
 * or a JSON array directly.
 */
const parseEducation = (content) => {
    try {
        const parsed = typeof content === 'string' ? JSON.parse(content) : content;
        const items = Array.isArray(parsed) ? parsed : parsed?.items || [];
        return items;
    } catch {
        return [];
    }
};

const EducationCard = ({ item, index }) => {
    const [ref, inView] = useInView({ threshold: 0.2, triggerOnce: true });
    const isLeft = index % 2 === 0;

    return (
        <div className={`flex items-start gap-8 ${isLeft ? '' : 'md:flex-row-reverse'}`}>
            {/* Card */}
            <motion.div
                ref={ref}
                initial={{ opacity: 0, x: isLeft ? -40 : 40 }}
                animate={inView ? { opacity: 1, x: 0 } : {}}
                transition={{ duration: 0.6, delay: 0.15 }}
                className={`flex-1 ${isLeft ? '' : 'md:text-right'}`}
            >
                <motion.div
                    whileHover={{ scale: 1.02, y: -4 }}
                    transition={{ type: 'spring', stiffness: 300 }}
                    className="relative p-6 md:p-8 rounded-2xl
                             bg-white dark:bg-slate-800/80
                             border border-slate-100 dark:border-slate-700/50
                             shadow-xl shadow-slate-200/50 dark:shadow-black/20
                             hover:shadow-2xl transition-shadow duration-500
                             backdrop-blur-sm"
                >
                    {/* Top accent */}
                    <div
                        className={`absolute top-0 ${isLeft ? 'left-0 rounded-tl-2xl' : 'right-0 rounded-tr-2xl'}
                                    w-24 h-1 bg-gradient-to-r from-emerald-500 to-teal-500`}
                    />

                    {/* Institution badge + info */}
                    <div className={`flex items-center gap-3 mb-4 ${isLeft ? '' : 'md:flex-row-reverse'}`}>
                        <span
                            className="flex items-center justify-center w-12 h-12 rounded-xl
                                       bg-gradient-to-br from-emerald-500 to-teal-600
                                       text-white text-xl shadow-lg"
                        >
                            🎓
                        </span>
                        <div>
                            <h3 className="text-lg font-bold text-slate-900 dark:text-white">
                                {item.Degree || item.degree || 'Degree'}
                            </h3>
                            <p className="text-emerald-600 dark:text-emerald-400 font-semibold text-sm">
                                {item.Institution || item.institution || item.School || item.school || ''}
                            </p>
                        </div>
                    </div>

                    {/* Year & GPA row */}
                    <div className={`flex items-center gap-3 text-sm text-slate-500 dark:text-slate-400 mb-3
                                    ${isLeft ? '' : 'md:justify-end'}`}>
                        {(item.Year || item.year || item.Duration || item.duration) && (
                            <span className="flex items-center gap-1">
                                📅 {item.Year || item.year || item.Duration || item.duration}
                            </span>
                        )}
                        {(item.GPA || item.gpa || item.Grade || item.grade) && (
                            <span className="px-2 py-0.5 rounded-full text-xs font-medium
                                           bg-emerald-100 dark:bg-emerald-900/40
                                           text-emerald-700 dark:text-emerald-300">
                                GPA: {item.GPA || item.gpa || item.Grade || item.grade}
                            </span>
                        )}
                    </div>

                    {/* Description / Points */}
                    {(item.Points || item.Description || item.description || item.points) && (
                        <p className="text-sm text-slate-600 dark:text-slate-300 leading-relaxed">
                            {item.Points || item.Description || item.description || item.points}
                        </p>
                    )}
                </motion.div>
            </motion.div>

            {/* Timeline center line + dot */}
            <div className="relative hidden md:flex flex-col items-center">
                <motion.div
                    initial={{ scale: 0 }}
                    animate={inView ? { scale: 1 } : {}}
                    transition={{ type: 'spring', delay: 0.3, stiffness: 300 }}
                    className="relative z-10"
                >
                    <div
                        className="w-5 h-5 rounded-full bg-gradient-to-br from-emerald-500 to-teal-600
                                    shadow-lg shadow-emerald-500/50"
                    />
                    <motion.div
                        animate={{ scale: [1, 1.5, 1], opacity: [0.5, 0, 0.5] }}
                        transition={{ duration: 2, repeat: Infinity }}
                        className="absolute inset-0 rounded-full bg-emerald-400"
                    />
                </motion.div>
            </div>

            {/* Empty space for the other side */}
            <div className="flex-1 hidden md:block" />
        </div>
    );
};

/**
 * AnimatedEducationSection
 *
 * Vertical timeline with graduation cap icons, alternating left/right cards.
 * Matches the visual style of AnimatedTimelineSection but with emerald/teal accents.
 */
export const AnimatedEducationSection = ({ content, variant = 'default' }) => {
    const items = parseEducation(content);

    if (items.length === 0) return null;

    return (
        <section
            id="education"
            className="py-24 relative overflow-hidden"
            style={{
                backgroundImage: 'linear-gradient(to bottom, color-mix(in srgb, var(--color-bg) 90%, var(--color-primary) 10%), var(--color-bg))',
            }}
        >
            {/* Background decoration */}
            <div className="absolute inset-0 overflow-hidden pointer-events-none">
                <div
                    className="absolute top-20 right-10 w-72 h-72 rounded-full blur-3xl"
                    style={{ backgroundColor: 'color-mix(in srgb, var(--color-primary) 20%, transparent)' }}
                />
                <div
                    className="absolute bottom-20 left-10 w-96 h-96 rounded-full blur-3xl"
                    style={{ backgroundColor: 'color-mix(in srgb, var(--color-secondary) 18%, transparent)' }}
                />
            </div>

            <div className="relative max-w-5xl mx-auto px-6">
                {/* Section header */}
                <ScrollReveal>
                    <div className="text-center mb-16">
                        <motion.span
                            initial={{ opacity: 0, y: 10 }}
                            whileInView={{ opacity: 1, y: 0 }}
                            className="section-badge inline-block px-4 py-1.5 rounded-full text-sm font-medium
                                       bg-emerald-100 dark:bg-emerald-900/40
                                       text-emerald-700 dark:text-emerald-300 mb-4"
                        >
                            Academic Background
                        </motion.span>
                        <h2 className="section-heading text-4xl md:text-5xl font-bold
                                       bg-gradient-to-r from-emerald-600 to-teal-600
                                       dark:from-emerald-400 dark:to-teal-400
                                       bg-clip-text text-transparent">
                            Education
                        </h2>
                    </div>
                </ScrollReveal>

                {/* Timeline */}
                <div className="relative">
                    {/* Vertical line */}
                    <div className="absolute left-1/2 top-0 bottom-0 w-0.5 -translate-x-1/2
                                   bg-gradient-to-b from-emerald-300 via-teal-300 to-transparent
                                   dark:from-emerald-700 dark:via-teal-700 hidden md:block" />

                    <div className="space-y-12">
                        {items.map((item, index) => (
                            <EducationCard key={index} item={item} index={index} />
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
};
