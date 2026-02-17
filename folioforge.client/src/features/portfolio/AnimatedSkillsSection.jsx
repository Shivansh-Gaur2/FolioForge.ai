import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';
import { ScrollReveal, StaggerContainer, StaggerItem } from '../../components/animations/ScrollReveal';

// Categorize skills for better visual organization
const categorizeSkills = (skills) => {
    const categories = {
        'Languages': ['C++', 'JavaScript', 'TypeScript', 'C#', 'SQL', 'Python', 'Java', 'Go'],
        'Frontend': ['React.js', 'Redux', 'Tailwind CSS', 'HTML5/CSS3', 'Bootstrap', 'Next.js', 'Vue.js'],
        'Backend': ['Node.js', 'Express.js', '.NET', 'ASP.NET', 'Django', 'Flask'],
        'Database': ['MongoDB', 'PostgreSQL', 'MySQL', 'Redis', 'SQL Server'],
        'DevOps & Cloud': ['AWS', 'Docker', 'Git/GitHub', 'CI/CD', 'Kubernetes', 'Azure'],
        'Tools & Other': ['Postman', 'WebSockets', 'Socket.io', 'REST API', 'GraphQL'],
    };

    const categorized = {};
    const uncategorized = [];

    skills.forEach(skill => {
        let found = false;
        for (const [category, keywords] of Object.entries(categories)) {
            if (keywords.some(kw => skill.toLowerCase().includes(kw.toLowerCase()) || kw.toLowerCase().includes(skill.toLowerCase()))) {
                if (!categorized[category]) categorized[category] = [];
                categorized[category].push(skill);
                found = true;
                break;
            }
        }
        if (!found) uncategorized.push(skill);
    });

    if (uncategorized.length > 0) {
        categorized['Other'] = uncategorized;
    }

    return categorized;
};

// Icon mapping for categories
const categoryIcons = {
    'Languages': '💻',
    'Frontend': '🎨',
    'Backend': '⚙️',
    'Database': '🗄️',
    'DevOps & Cloud': '☁️',
    'Tools & Other': '🔧',
    'Other': '✨',
};

// Color mapping for skill badges
const categoryColors = {
    'Languages': 'from-violet-500 to-purple-600',
    'Frontend': 'from-cyan-500 to-blue-600',
    'Backend': 'from-emerald-500 to-green-600',
    'Database': 'from-amber-500 to-orange-600',
    'DevOps & Cloud': 'from-rose-500 to-pink-600',
    'Tools & Other': 'from-slate-500 to-slate-700',
    'Other': 'from-indigo-500 to-blue-600',
};

const SkillBadge = ({ skill, color, index }) => (
    <motion.span
        initial={{ opacity: 0, scale: 0.8 }}
        animate={{ opacity: 1, scale: 1 }}
        transition={{ delay: index * 0.05 }}
        whileHover={{ scale: 1.1, y: -2 }}
        className={`inline-flex items-center px-4 py-2 rounded-xl text-sm font-medium
                   bg-gradient-to-r ${color} text-white
                   shadow-lg hover:shadow-xl transition-shadow cursor-default`}
    >
        {skill}
    </motion.span>
);

const SkillCategory = ({ category, skills, index }) => {
    const [ref, inView] = useInView({ threshold: 0.2, triggerOnce: true });
    const color = categoryColors[category] || categoryColors['Other'];
    const icon = categoryIcons[category] || '✨';

    return (
        <motion.div
            ref={ref}
            initial={{ opacity: 0, y: 30 }}
            animate={inView ? { opacity: 1, y: 0 } : {}}
            transition={{ duration: 0.5, delay: index * 0.1 }}
            className="bg-white dark:bg-slate-800/50 rounded-2xl p-6 
                      border border-slate-100 dark:border-slate-700/50
                      shadow-xl shadow-slate-200/50 dark:shadow-black/20
                      hover:shadow-2xl transition-shadow duration-500"
        >
            <div className="flex items-center gap-3 mb-4">
                <span className="text-2xl">{icon}</span>
                <h3 className="text-lg font-bold text-slate-800 dark:text-white">
                    {category}
                </h3>
                <span className="ml-auto px-2 py-1 text-xs font-medium rounded-full
                               bg-slate-100 dark:bg-slate-700 text-slate-500 dark:text-slate-400">
                    {skills.length}
                </span>
            </div>
            <div className="flex flex-wrap gap-2">
                {skills.map((skill, idx) => (
                    <SkillBadge key={skill} skill={skill} color={color} index={idx} />
                ))}
            </div>
        </motion.div>
    );
};

export const AnimatedSkillsSection = ({ content }) => {
    let skills = [];
    try {
        const parsed = JSON.parse(content);
        skills = parsed.items || [];
    } catch (e) {
        console.warn("Failed to parse skills JSON", e);
        return null;
    }

    if (skills.length === 0) return null;

    const categorizedSkills = categorizeSkills(skills);
    const categories = Object.entries(categorizedSkills);

    return (
        <section id="skills" className="py-24 relative overflow-hidden">
            {/* Background decoration */}
            <div className="absolute top-0 left-1/4 w-96 h-96 bg-blue-200/30 dark:bg-blue-500/10 rounded-full blur-3xl" />
            <div className="absolute bottom-0 right-1/4 w-96 h-96 bg-purple-200/30 dark:bg-purple-500/10 rounded-full blur-3xl" />

            <div className="relative max-w-6xl mx-auto px-6">
                <ScrollReveal>
                    <div className="text-center mb-16">
                        <span className="inline-block px-4 py-2 rounded-full 
                                       bg-blue-100 dark:bg-blue-900/30 
                                       text-blue-600 dark:text-blue-400
                                       text-sm font-semibold mb-4">
                            Technical Arsenal
                        </span>
                        <h2 className="text-4xl md:text-5xl font-black text-slate-900 dark:text-white mb-4">
                            Skills & Expertise
                        </h2>
                        <p className="text-lg text-slate-500 dark:text-slate-400 max-w-2xl mx-auto">
                            A comprehensive toolkit built over years of crafting scalable solutions
                        </p>
                    </div>
                </ScrollReveal>

                {/* Skills grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {categories.map(([category, categorySkills], index) => (
                        <SkillCategory
                            key={category}
                            category={category}
                            skills={categorySkills}
                            index={index}
                        />
                    ))}
                </div>

                {/* Stats bar */}
                <ScrollReveal delay={0.3}>
                    <div className="mt-16 grid grid-cols-2 md:grid-cols-4 gap-6">
                        {[
                            { value: skills.length + '+', label: 'Technologies' },
                            { value: categories.length, label: 'Categories' },
                            { value: '3+', label: 'Years Experience' },
                            { value: '∞', label: 'Learning Appetite' },
                        ].map((stat, idx) => (
                            <motion.div
                                key={stat.label}
                                initial={{ opacity: 0, y: 20 }}
                                whileInView={{ opacity: 1, y: 0 }}
                                transition={{ delay: idx * 0.1 }}
                                className="text-center p-6 rounded-2xl
                                         bg-gradient-to-br from-slate-50 to-slate-100
                                         dark:from-slate-800 dark:to-slate-900
                                         border border-slate-200/50 dark:border-slate-700/50"
                            >
                                <div className="text-3xl font-black text-blue-600 dark:text-blue-400">
                                    {stat.value}
                                </div>
                                <div className="text-sm text-slate-500 dark:text-slate-400 mt-1">
                                    {stat.label}
                                </div>
                            </motion.div>
                        ))}
                    </div>
                </ScrollReveal>
            </div>
        </section>
    );
};
