import { useCallback, useMemo } from 'react';
import Particles from '@tsparticles/react';
import { loadSlim } from '@tsparticles/slim';
import { motion } from 'framer-motion';
import { useTheme } from '../../context/ThemeContext';

export const ParticleHero = ({ title, bio, onContactClick, onDownloadClick }) => {
    const { isDark } = useTheme();

    const particlesInit = useCallback(async (engine) => {
        await loadSlim(engine);
    }, []);

    const particleOptions = useMemo(() => ({
        fullScreen: false,
        background: { color: { value: 'transparent' } },
        fpsLimit: 60,
        particles: {
            color: { value: isDark ? '#6366f1' : '#3b82f6' },
            links: {
                color: isDark ? '#6366f1' : '#93c5fd',
                distance: 150,
                enable: true,
                opacity: 0.3,
                width: 1,
            },
            move: {
                enable: true,
                speed: 1,
                direction: 'none',
                random: true,
                straight: false,
                outModes: { default: 'bounce' },
            },
            number: {
                density: { enable: true, area: 800 },
                value: 60,
            },
            opacity: {
                value: { min: 0.3, max: 0.7 },
                animation: { enable: true, speed: 0.5, minimumValue: 0.1 },
            },
            shape: { type: 'circle' },
            size: {
                value: { min: 1, max: 3 },
            },
        },
        interactivity: {
            events: {
                onHover: { enable: true, mode: 'grab' },
                onClick: { enable: true, mode: 'push' },
            },
            modes: {
                grab: { distance: 140, links: { opacity: 0.5 } },
                push: { quantity: 4 },
            },
        },
        detectRetina: true,
    }), [isDark]);

    // Animated text variants
    const containerVariants = {
        hidden: { opacity: 0 },
        visible: {
            opacity: 1,
            transition: { staggerChildren: 0.1, delayChildren: 0.3 },
        },
    };

    const itemVariants = {
        hidden: { opacity: 0, y: 30 },
        visible: {
            opacity: 1,
            y: 0,
            transition: { duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] },
        },
    };

    return (
        <header className="relative min-h-screen flex items-center justify-center overflow-hidden
                          bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-100
                          dark:from-slate-950 dark:via-slate-900 dark:to-indigo-950">
            {/* Particle background */}
            <Particles
                id="hero-particles"
                init={particlesInit}
                options={particleOptions}
                className="absolute inset-0 z-0"
            />

            {/* Gradient orbs for visual interest */}
            <div className="absolute top-1/4 -left-20 w-72 h-72 bg-blue-400/30 dark:bg-blue-500/20 rounded-full blur-3xl animate-pulse" />
            <div className="absolute bottom-1/4 -right-20 w-96 h-96 bg-purple-400/20 dark:bg-purple-500/20 rounded-full blur-3xl animate-pulse" style={{ animationDelay: '1s' }} />
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[600px] h-[600px] bg-indigo-300/10 dark:bg-indigo-500/10 rounded-full blur-3xl" />

            {/* Content */}
            <motion.div
                className="relative z-10 text-center px-6 max-w-4xl mx-auto"
                variants={containerVariants}
                initial="hidden"
                animate="visible"
            >
                {/* Greeting badge */}
                <motion.div variants={itemVariants}>
                    <span className="inline-flex items-center gap-2 px-4 py-2 rounded-full 
                                   bg-white/70 dark:bg-white/10 backdrop-blur-sm
                                   border border-slate-200/50 dark:border-white/10
                                   text-sm font-medium text-slate-600 dark:text-slate-300
                                   shadow-lg shadow-slate-200/50 dark:shadow-black/20">
                        <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse" />
                        Available for opportunities
                    </span>
                </motion.div>

                {/* Main title */}
                <motion.h1
                    variants={itemVariants}
                    className="mt-8 text-5xl md:text-7xl font-black tracking-tight
                             bg-gradient-to-r from-slate-900 via-slate-700 to-slate-900
                             dark:from-white dark:via-slate-200 dark:to-white
                             bg-clip-text text-transparent"
                >
                    {title}
                </motion.h1>

                {/* Role/subtitle with typing effect simulation */}
                <motion.p
                    variants={itemVariants}
                    className="mt-6 text-xl md:text-2xl font-medium
                             text-slate-600 dark:text-slate-400"
                >
                    <span className="text-blue-600 dark:text-blue-400">{'<'}</span>
                    Software Development Engineer
                    <span className="text-blue-600 dark:text-blue-400">{' />'}</span>
                </motion.p>

                {/* Bio */}
                <motion.p
                    variants={itemVariants}
                    className="mt-6 text-lg text-slate-500 dark:text-slate-400 
                             leading-relaxed max-w-2xl mx-auto"
                >
                    {bio}
                </motion.p>

                {/* CTA Buttons */}
                <motion.div
                    variants={itemVariants}
                    className="mt-10 flex flex-wrap justify-center gap-4"
                >
                    <motion.button
                        onClick={onContactClick}
                        className="group relative px-8 py-4 rounded-xl font-semibold
                                 bg-gradient-to-r from-blue-600 to-indigo-600
                                 hover:from-blue-500 hover:to-indigo-500
                                 text-white shadow-xl shadow-blue-500/25
                                 transition-all duration-300"
                        whileHover={{ scale: 1.02, y: -2 }}
                        whileTap={{ scale: 0.98 }}
                    >
                        <span className="flex items-center gap-2">
                            Let's Connect
                            <svg className="w-5 h-5 group-hover:translate-x-1 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" />
                            </svg>
                        </span>
                    </motion.button>

                    <motion.button
                        onClick={onDownloadClick}
                        className="px-8 py-4 rounded-xl font-semibold
                                 bg-white/80 dark:bg-white/10 backdrop-blur-sm
                                 border border-slate-200 dark:border-white/20
                                 text-slate-700 dark:text-white
                                 hover:bg-white dark:hover:bg-white/20
                                 shadow-lg transition-all duration-300"
                        whileHover={{ scale: 1.02, y: -2 }}
                        whileTap={{ scale: 0.98 }}
                    >
                        <span className="flex items-center gap-2">
                            <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                            </svg>
                            Download CV
                        </span>
                    </motion.button>
                </motion.div>

                {/* Scroll indicator */}
                <motion.div
                    className="absolute bottom-10 left-1/2 -translate-x-1/2"
                    animate={{ y: [0, 10, 0] }}
                    transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                >
                    <a 
                        href="#skills" 
                        className="flex flex-col items-center text-slate-400 dark:text-slate-500
                                 hover:text-slate-600 dark:hover:text-slate-300 transition-colors"
                    >
                        <span className="text-xs font-medium mb-2">Scroll to explore</span>
                        <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                        </svg>
                    </a>
                </motion.div>
            </motion.div>
        </header>
    );
};
