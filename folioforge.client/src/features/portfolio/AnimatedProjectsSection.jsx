import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';
import { ScrollReveal } from '../../components/animations/ScrollReveal';
import { SmartContent } from '../../components/ui/SmartContent';

// Generate gradient based on project index
const gradients = [
    'from-blue-500 via-cyan-500 to-teal-500',
    'from-purple-500 via-pink-500 to-rose-500',
    'from-amber-500 via-orange-500 to-red-500',
    'from-emerald-500 via-green-500 to-lime-500',
    'from-indigo-500 via-blue-500 to-cyan-500',
    'from-rose-500 via-fuchsia-500 to-purple-500',
];

// Tech stack icons (simplified mapping)
const techIcons = {
    'react': '⚛️',
    'node': '🟢',
    'express': '🚂',
    'mongodb': '🍃',
    'redis': '🔴',
    'websocket': '🔌',
    'socket.io': '🔌',
    'jwt': '🔐',
    'docker': '🐳',
    'aws': '☁️',
    'typescript': '📘',
    'postgresql': '🐘',
};

const getTechIcon = (tech) => {
    const key = Object.keys(techIcons).find(k => tech.toLowerCase().includes(k));
    return key ? techIcons[key] : '🔧';
};

const ProjectCard = ({ project, index }) => {
    const [ref, inView] = useInView({ threshold: 0.2, triggerOnce: true });
    const gradient = gradients[index % gradients.length];
    const techStack = project.TechStack?.split(',').map(t => t.trim()) || [];

    return (
        <motion.div
            ref={ref}
            initial={{ opacity: 0, y: 50 }}
            animate={inView ? { opacity: 1, y: 0 } : {}}
            transition={{ duration: 0.5, delay: index * 0.1 }}
            className="group relative"
        >
            <motion.div
                whileHover={{ y: -8 }}
                transition={{ type: "spring", stiffness: 300 }}
                className="relative h-full rounded-3xl overflow-hidden
                         bg-white dark:bg-slate-800
                         border border-slate-100 dark:border-slate-700/50
                         shadow-xl shadow-slate-200/50 dark:shadow-black/30
                         hover:shadow-2xl transition-shadow duration-500"
            >
                {/* Project preview header */}
                <div className={`relative h-48 bg-gradient-to-br ${gradient} p-6 overflow-hidden`}>
                    {/* Animated background pattern */}
                    <div className="absolute inset-0 opacity-30">
                        <div className="absolute top-4 left-4 w-32 h-32 border border-white/30 rounded-2xl transform rotate-12" />
                        <div className="absolute bottom-4 right-4 w-24 h-24 border border-white/30 rounded-full" />
                        <div className="absolute top-1/2 left-1/2 w-40 h-40 border border-white/20 rounded-3xl transform -translate-x-1/2 -translate-y-1/2 rotate-45" />
                    </div>
                    
                    {/* Project number */}
                    <motion.span
                        initial={{ scale: 0.5, opacity: 0 }}
                        animate={inView ? { scale: 1, opacity: 1 } : {}}
                        transition={{ delay: 0.3 }}
                        className="absolute top-4 right-4 w-10 h-10 
                                 flex items-center justify-center
                                 bg-white/20 backdrop-blur-sm rounded-full
                                 text-white font-bold text-sm"
                    >
                        {String(index + 1).padStart(2, '0')}
                    </motion.span>

                    {/* Project icon */}
                    <div className="absolute bottom-4 left-6">
                        <motion.div
                            initial={{ y: 20, opacity: 0 }}
                            animate={inView ? { y: 0, opacity: 1 } : {}}
                            transition={{ delay: 0.4 }}
                            className="text-5xl"
                        >
                            {index % 2 === 0 ? '🚀' : '💡'}
                        </motion.div>
                    </div>

                    {/* Hover overlay */}
                    <motion.div
                        initial={{ opacity: 0 }}
                        whileHover={{ opacity: 1 }}
                        className="absolute inset-0 bg-black/30 backdrop-blur-sm
                                 flex items-center justify-center gap-4"
                    >
                        <motion.button
                            whileHover={{ scale: 1.1 }}
                            whileTap={{ scale: 0.9 }}
                            className="px-4 py-2 rounded-xl bg-white text-slate-900
                                     font-semibold text-sm shadow-lg"
                        >
                            View Details
                        </motion.button>
                    </motion.div>
                </div>

                {/* Content */}
                <div className="p-6">
                    <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-2
                                 group-hover:text-blue-600 dark:group-hover:text-blue-400
                                 transition-colors">
                        {project.Name}
                    </h3>
                    
                    {/* Smart bullet points for project achievements */}
                    <div className="mb-4">
                        <SmartContent 
                            content={project.Points || project.Description} 
                            variant="gradient" 
                        />
                    </div>

                    {/* Tech stack */}
                    <div className="flex flex-wrap gap-2">
                        {techStack.map((tech, idx) => (
                            <motion.span
                                key={tech}
                                initial={{ opacity: 0, scale: 0.8 }}
                                animate={inView ? { opacity: 1, scale: 1 } : {}}
                                transition={{ delay: 0.5 + idx * 0.05 }}
                                className="inline-flex items-center gap-1 px-3 py-1.5 
                                         rounded-lg text-xs font-medium
                                         bg-slate-100 dark:bg-slate-700/50
                                         text-slate-700 dark:text-slate-300"
                            >
                                <span>{getTechIcon(tech)}</span>
                                {tech}
                            </motion.span>
                        ))}
                    </div>
                </div>

                {/* Bottom action bar */}
                <div className="px-6 py-4 border-t border-slate-100 dark:border-slate-700/50
                              flex items-center justify-between">
                    <div className="flex items-center gap-4 text-slate-400 text-sm">
                        <span className="flex items-center gap-1">
                            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
                            </svg>
                            Code
                        </span>
                        <span className="flex items-center gap-1">
                            <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                            </svg>
                            Demo
                        </span>
                    </div>
                    <motion.div
                        whileHover={{ x: 4 }}
                        className="text-blue-600 dark:text-blue-400 cursor-pointer"
                    >
                        <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" />
                        </svg>
                    </motion.div>
                </div>
            </motion.div>
        </motion.div>
    );
};

export const AnimatedProjectsSection = ({ content }) => {
    let projects = [];
    try {
        const parsed = JSON.parse(content);
        projects = parsed.items || [];
    } catch (e) {
        console.warn("Failed to parse projects JSON", e);
        return null;
    }

    if (projects.length === 0) return null;

    return (
        <section id="projects" className="py-24 relative overflow-hidden">
            {/* Background */}
            <div className="absolute inset-0 bg-gradient-to-b from-white via-slate-50 to-white
                          dark:from-slate-950 dark:via-slate-900 dark:to-slate-950" />
            <div className="absolute top-1/3 left-0 w-full h-px bg-gradient-to-r from-transparent via-slate-200 dark:via-slate-700 to-transparent" />

            <div className="relative max-w-6xl mx-auto px-6">
                <ScrollReveal>
                    <div className="text-center mb-16">
                        <span className="inline-block px-4 py-2 rounded-full 
                                       bg-emerald-100 dark:bg-emerald-900/30 
                                       text-emerald-600 dark:text-emerald-400
                                       text-sm font-semibold mb-4">
                            Featured Work
                        </span>
                        <h2 className="text-4xl md:text-5xl font-black text-slate-900 dark:text-white mb-4">
                            Projects
                        </h2>
                        <p className="text-lg text-slate-500 dark:text-slate-400 max-w-2xl mx-auto">
                            Handcrafted solutions that push boundaries
                        </p>
                    </div>
                </ScrollReveal>

                {/* Projects grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                    {projects.map((project, index) => (
                        <ProjectCard key={project.Name} project={project} index={index} />
                    ))}
                </div>

                {/* View more hint */}
                <ScrollReveal delay={0.4}>
                    <div className="mt-12 text-center">
                        <motion.button
                            whileHover={{ scale: 1.05 }}
                            whileTap={{ scale: 0.95 }}
                            className="inline-flex items-center gap-2 px-6 py-3 
                                     rounded-xl border-2 border-dashed border-slate-300 dark:border-slate-600
                                     text-slate-500 dark:text-slate-400 font-medium
                                     hover:border-blue-500 hover:text-blue-600
                                     dark:hover:border-blue-400 dark:hover:text-blue-400
                                     transition-colors"
                        >
                            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                            </svg>
                            View All Projects on GitHub
                        </motion.button>
                    </div>
                </ScrollReveal>
            </div>
        </section>
    );
};
