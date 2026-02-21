import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';
import { ScrollReveal } from '../../components/animations/ScrollReveal';
import { SmartContent } from '../../components/ui/SmartContent';

const TimelineItem = ({ item, isLeft }) => {
    const [ref, inView] = useInView({ threshold: 0.3, triggerOnce: true });

    return (
        <div className={`flex items-center gap-8 ${isLeft ? 'md:flex-row-reverse' : ''}`}>
            {/* Content card */}
            <motion.div
                ref={ref}
                initial={{ opacity: 0, x: isLeft ? 50 : -50 }}
                animate={inView ? { opacity: 1, x: 0 } : {}}
                transition={{ duration: 0.6, delay: 0.2 }}
                className={`flex-1 ${isLeft ? 'md:text-right' : ''}`}
            >
                <motion.div
                    whileHover={{ scale: 1.02, y: -4 }}
                    transition={{ type: "spring", stiffness: 300 }}
                    className="relative p-6 md:p-8 rounded-2xl
                             bg-white dark:bg-slate-800/80
                             border border-slate-100 dark:border-slate-700/50
                             shadow-xl shadow-slate-200/50 dark:shadow-black/20
                             hover:shadow-2xl transition-shadow duration-500
                             backdrop-blur-sm"
                >
                    {/* Decorative gradient */}
                    <div className={`absolute top-0 ${isLeft ? 'right-0 rounded-tr-2xl' : 'left-0 rounded-tl-2xl'} 
                                   w-24 h-1 bg-gradient-to-r from-purple-500 to-blue-500`} />
                    
                    {/* Company badge */}
                    <div className={`flex items-center gap-3 mb-4 ${isLeft ? 'md:flex-row-reverse' : ''}`}>
                        <span className="flex items-center justify-center w-12 h-12 rounded-xl
                                       bg-gradient-to-br from-purple-500 to-blue-600
                                       text-white text-xl font-bold shadow-lg">
                            {item.Company?.charAt(0) || '?'}
                        </span>
                        <div>
                            <h3 className="text-xl font-bold text-slate-900 dark:text-white">
                                {item.Role}
                            </h3>
                            <p className="text-purple-600 dark:text-purple-400 font-semibold">
                                {item.Company}
                            </p>
                        </div>
                    </div>
                    
                    {/* Smart bullet points for experience achievements */}
                    <SmartContent 
                        content={item.Points || item.Description} 
                        variant="timeline" 
                    />
                </motion.div>
            </motion.div>

            {/* Timeline center */}
            <div className="relative hidden md:flex flex-col items-center">
                {/* Animated dot */}
                <motion.div
                    initial={{ scale: 0 }}
                    animate={inView ? { scale: 1 } : {}}
                    transition={{ type: "spring", delay: 0.3, stiffness: 300 }}
                    className="relative z-10"
                >
                    <div className="w-5 h-5 rounded-full bg-gradient-to-br from-purple-500 to-blue-600 
                                  shadow-lg shadow-purple-500/50" />
                    <motion.div
                        animate={{ scale: [1, 1.5, 1], opacity: [0.5, 0, 0.5] }}
                        transition={{ duration: 2, repeat: Infinity }}
                        className="absolute inset-0 rounded-full bg-purple-400"
                    />
                </motion.div>
            </div>

            {/* Empty space for the other side */}
            <div className="flex-1 hidden md:block" />
        </div>
    );
};

export const AnimatedTimelineSection = ({ content }) => {
    let items = [];
    try {
        const parsed = JSON.parse(content);
        items = parsed.items || [];
    } catch (e) {
        console.warn("Failed to parse timeline JSON", e);
        return null;
    }

    if (items.length === 0) return null;

    return (
        <section id="experience" className="py-24 relative overflow-hidden
                                          bg-gradient-to-b from-slate-50 to-white
                                          dark:from-slate-900 dark:to-slate-950">
            {/* Background decorations */}
            <div className="absolute top-1/4 -left-48 w-96 h-96 bg-purple-200/40 dark:bg-purple-500/10 rounded-full blur-3xl" />
            <div className="absolute bottom-1/4 -right-48 w-96 h-96 bg-blue-200/40 dark:bg-blue-500/10 rounded-full blur-3xl" />

            <div className="relative max-w-5xl mx-auto px-6">
                <ScrollReveal>
                    <div className="text-center mb-16">
                        <span className="inline-block px-4 py-2 rounded-full 
                                       bg-purple-100 dark:bg-purple-900/30 
                                       text-purple-600 dark:text-purple-400
                                       text-sm font-semibold mb-4">
                            Career Journey
                        </span>
                        <h2 className="text-4xl md:text-5xl font-black text-slate-900 dark:text-white mb-4">
                            Experience
                        </h2>
                        <p className="text-lg text-slate-500 dark:text-slate-400 max-w-2xl mx-auto">
                            Building impactful solutions at scale
                        </p>
                    </div>
                </ScrollReveal>

                {/* Timeline */}
                <div className="relative">
                    {/* Center line */}
                    <div className="absolute left-1/2 top-0 bottom-0 w-0.5 
                                  bg-gradient-to-b from-purple-500 via-blue-500 to-purple-500
                                  hidden md:block" />

                    <div className="space-y-12 md:space-y-24">
                        {items.map((item, index) => (
                            <TimelineItem
                                key={index}
                                item={item}
                                index={index}
                                isLeft={index % 2 === 0}
                            />
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
};
