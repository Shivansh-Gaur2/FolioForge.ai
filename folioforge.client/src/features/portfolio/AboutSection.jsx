import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';

/**
 * AboutSection
 *
 * Standalone section that renders the "About" / bio text.
 * Supports 'default' and 'storytelling' variants.
 */
export const AboutSection = ({ content, variant = 'default' }) => {
    const [ref, inView] = useInView({ threshold: 0.05, triggerOnce: true });

    let bio = '';
    try {
        const parsed = typeof content === 'string' ? JSON.parse(content) : content;
        if (typeof parsed === 'string') {
            bio = parsed;
        } else if (parsed) {
            bio = parsed.content || parsed.bio || parsed.summary || parsed.text || '';
        }
    } catch {
        bio = typeof content === 'string' ? content : '';
    }

    if (variant === 'storytelling') {
        return (
            <section ref={ref} className="py-20 px-6 md:px-12 lg:px-24">
                <div className="max-w-3xl mx-auto">
                    <motion.span
                        initial={{ opacity: 0, y: 20 }}
                        animate={inView ? { opacity: 1, y: 0 } : {}}
                        transition={{ duration: 0.5 }}
                        className="section-badge inline-block px-4 py-1.5 rounded-full text-xs font-semibold tracking-wider uppercase mb-6"
                    >
                        About Me
                    </motion.span>
                    <motion.h2
                        initial={{ opacity: 0, y: 20 }}
                        animate={inView ? { opacity: 1, y: 0 } : {}}
                        transition={{ duration: 0.5, delay: 0.1 }}
                        className="section-heading text-3xl md:text-4xl font-bold mb-8"
                    >
                        My Story
                    </motion.h2>
                    <motion.div
                        initial={{ opacity: 0, y: 20 }}
                        animate={inView ? { opacity: 1, y: 0 } : {}}
                        transition={{ duration: 0.5, delay: 0.2 }}
                        className="relative pl-6 border-l-2"
                        style={{ borderColor: 'var(--color-primary)' }}
                    >
                        <p className="text-base md:text-lg leading-relaxed whitespace-pre-line"
                           style={{ color: 'var(--color-text)', opacity: 0.85 }}>
                            {bio || 'No bio content yet. Use the Content tab to add your bio.'}
                        </p>
                    </motion.div>
                </div>
            </section>
        );
    }

    // Default variant
    return (
        <section ref={ref} className="py-20 px-6 md:px-12 lg:px-24">
            <div className="max-w-4xl mx-auto text-center">
                <motion.span
                    initial={{ opacity: 0, y: 20 }}
                    animate={inView ? { opacity: 1, y: 0 } : {}}
                    transition={{ duration: 0.5 }}
                    className="section-badge inline-block px-4 py-1.5 rounded-full text-xs font-semibold tracking-wider uppercase mb-6"
                >
                    About Me
                </motion.span>
                <motion.h2
                    initial={{ opacity: 0, y: 20 }}
                    animate={inView ? { opacity: 1, y: 0 } : {}}
                    transition={{ duration: 0.5, delay: 0.1 }}
                    className="section-heading text-3xl md:text-4xl font-bold mb-8"
                >
                    Who I Am
                </motion.h2>
                <motion.p
                    initial={{ opacity: 0, y: 20 }}
                    animate={inView ? { opacity: 1, y: 0 } : {}}
                    transition={{ duration: 0.5, delay: 0.2 }}
                    className="text-base md:text-lg leading-relaxed max-w-2xl mx-auto whitespace-pre-line"
                    style={{ color: 'var(--color-text)', opacity: 0.85 }}
                >
                    {bio || 'No bio content yet. Use the Content tab to add your bio.'}
                </motion.p>
            </div>
        </section>
    );
};
