import { useCallback, useMemo, useState, useEffect } from 'react';
import Particles from '@tsparticles/react';
import { loadSlim } from '@tsparticles/slim';
import { motion } from 'framer-motion';
import { useTheme } from '../../context/ThemeContext';

export const ParticleHero = ({ title, bio, onContactClick, onDownloadClick }) => {
    const { isDark } = useTheme();
    const [mousePosition, setMousePosition] = useState({ x: 0, y: 0 });

    const particlesInit = useCallback(async (engine) => {
        await loadSlim(engine);
    }, []);

    // Track mouse for parallax orbs
    useEffect(() => {
        const handleMouseMove = (e) => {
            setMousePosition({
                x: (e.clientX / window.innerWidth - 0.5) * 2,
                y: (e.clientY / window.innerHeight - 0.5) * 2,
            });
        };
        window.addEventListener('mousemove', handleMouseMove);
        return () => window.removeEventListener('mousemove', handleMouseMove);
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
                opacity: 0.2,
                width: 1,
            },
            move: {
                enable: true,
                speed: 0.8,
                direction: 'none',
                random: true,
                straight: false,
                outModes: { default: 'bounce' },
            },
            number: {
                density: { enable: true, area: 800 },
                value: 50,
            },
            opacity: {
                value: { min: 0.2, max: 0.5 },
                animation: { enable: true, speed: 0.5, minimumValue: 0.1 },
            },
            shape: { type: 'circle' },
            size: { value: { min: 1, max: 3 } },
        },
        interactivity: {
            events: {
                onHover: { enable: true, mode: 'grab' },
                onClick: { enable: true, mode: 'push' },
            },
            modes: {
                grab: { distance: 140, links: { opacity: 0.5 } },
                push: { quantity: 3 },
            },
        },
        detectRetina: true,
    }), [isDark]);

    const containerVariants = {
        hidden: { opacity: 0 },
        visible: {
            opacity: 1,
            transition: { staggerChildren: 0.15, delayChildren: 0.2 },
        },
    };

    const itemVariants = {
        hidden: { opacity: 0, y: 40, filter: 'blur(10px)' },
        visible: {
            opacity: 1,
            y: 0,
            filter: 'blur(0px)',
            transition: { duration: 0.8, ease: [0.25, 0.46, 0.45, 0.94] },
        },
    };

    // Split name for letter-by-letter animation
    const nameLetters = (title || '').split('');

    return (
        <header
            id="hero"
            className="relative min-h-screen flex flex-col items-center justify-center overflow-hidden"
            style={{
                backgroundImage: 'linear-gradient(to bottom, color-mix(in srgb, var(--color-bg) 85%, var(--color-primary) 15%), color-mix(in srgb, var(--color-bg) 82%, var(--color-secondary) 18%), var(--color-bg))',
            }}
        >
            {/* Particle background */}
            <Particles
                id="hero-particles"
                init={particlesInit}
                options={particleOptions}
                className="absolute inset-0 z-0"
            />

            {/* Animated gradient orbs that follow the mouse */}
            <div
                className="absolute w-[500px] h-[500px] rounded-full blur-[120px] opacity-35"
                style={{
                    top: `calc(20% + ${mousePosition.y * 20}px)`,
                    left: `calc(15% + ${mousePosition.x * 20}px)`,
                    background: 'radial-gradient(circle at center, color-mix(in srgb, var(--color-primary) 45%, transparent) 0%, transparent 70%)',
                    transition: 'top 0.6s ease-out, left 0.6s ease-out',
                }}
            />
            <div
                className="absolute w-[400px] h-[400px] rounded-full blur-[120px] opacity-30"
                style={{
                    bottom: `calc(15% + ${mousePosition.y * -15}px)`,
                    right: `calc(10% + ${mousePosition.x * -15}px)`,
                    background: 'radial-gradient(circle at center, color-mix(in srgb, var(--color-secondary) 38%, transparent) 0%, transparent 70%)',
                    transition: 'bottom 0.8s ease-out, right 0.8s ease-out',
                }}
            />
            <div
                className="absolute w-[300px] h-[300px] rounded-full blur-[100px] opacity-25"
                style={{
                    top: `calc(50% + ${mousePosition.y * -10}px)`,
                    right: `calc(30% + ${mousePosition.x * 10}px)`,
                    background: 'radial-gradient(circle at center, color-mix(in srgb, var(--color-primary) 28%, var(--color-secondary) 22%) 0%, transparent 70%)',
                    transition: 'top 1s ease-out, right 1s ease-out',
                }}
            />

            {/* Subtle grid pattern overlay */}
            <div className="absolute inset-0 z-[1] opacity-[0.03] dark:opacity-[0.04]"
                 style={{
                     backgroundImage: `linear-gradient(rgba(99,102,241,0.3) 1px, transparent 1px),
                                       linear-gradient(90deg, rgba(99,102,241,0.3) 1px, transparent 1px)`,
                     backgroundSize: '60px 60px',
                 }}
            />

            {/* Radial vignette */}
            <div className="absolute inset-0 z-[1]"
                 style={{
                     background: 'radial-gradient(ellipse at center, transparent 0%, transparent 50%, rgba(0,0,0,0.15) 100%)',
                 }}
            />

            {/* Content */}
            <motion.div
                className="relative z-10 text-center px-6 max-w-4xl mx-auto"
                variants={containerVariants}
                initial="hidden"
                animate="visible"
            >
                {/* Status badge */}
                <motion.div variants={itemVariants}>
                    <span className="inline-flex items-center gap-2.5 px-5 py-2.5 rounded-full 
                                   bg-white/70 dark:bg-white/[0.07] backdrop-blur-md
                                   border border-slate-200/60 dark:border-white/[0.08]
                                   text-sm font-medium text-slate-600 dark:text-slate-300
                                   shadow-xl shadow-slate-200/30 dark:shadow-black/30">
                        <span className="relative flex h-2.5 w-2.5">
                            <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-green-400 opacity-75" />
                            <span className="relative inline-flex rounded-full h-2.5 w-2.5 bg-green-500" />
                        </span>
                        Available for opportunities
                    </span>
                </motion.div>

                {/* Main title with letter animation */}
                <motion.h1
                    variants={itemVariants}
                    className="mt-10 text-6xl md:text-8xl lg:text-9xl font-black tracking-tighter leading-none"
                >
                    {nameLetters.map((letter, i) => (
                        <motion.span
                            key={i}
                            className="inline-block bg-gradient-to-b from-slate-800 via-slate-600 to-slate-800
                                     dark:from-white dark:via-slate-100 dark:to-slate-400
                                     bg-clip-text text-transparent"
                            initial={{ opacity: 0, y: 60, rotateX: -90 }}
                            animate={{ opacity: 1, y: 0, rotateX: 0 }}
                            transition={{
                                duration: 0.6,
                                delay: 0.5 + i * 0.05,
                                ease: [0.25, 0.46, 0.45, 0.94],
                            }}
                            whileHover={{ 
                                scale: 1.1,
                                color: isDark ? '#818cf8' : '#4f46e5',
                                transition: { duration: 0.2 }, 
                            }}
                            style={{
                                cursor: 'default',
                                backgroundImage: 'linear-gradient(to bottom, var(--color-primary), color-mix(in srgb, var(--color-primary) 70%, var(--color-text) 30%), var(--color-secondary))',
                            }}
                        >
                            {letter === ' ' ? '\u00A0' : letter}
                        </motion.span>
                    ))}
                </motion.h1>

                {/* Role with code-style brackets */}
                <motion.div
                    variants={itemVariants}
                    className="mt-6 flex items-center justify-center gap-3"
                >
                    <motion.div
                        className="h-px flex-1 max-w-[80px] bg-gradient-to-r from-transparent to-blue-500/50"
                        initial={{ scaleX: 0 }}
                        animate={{ scaleX: 1 }}
                        transition={{ delay: 1.2, duration: 0.8 }}
                    />
                    <p className="text-lg md:text-xl font-mono font-medium tracking-wide
                               text-slate-500 dark:text-slate-400">
                        <span style={{ color: 'var(--color-primary)' }}>{'<'}</span>
                        <span className="text-slate-700 dark:text-slate-300">Software Development Engineer</span>
                        <span style={{ color: 'var(--color-primary)' }}>{' />'}</span>
                    </p>
                    <motion.div
                        className="h-px flex-1 max-w-[80px] bg-gradient-to-l from-transparent to-purple-500/50"
                        initial={{ scaleX: 0 }}
                        animate={{ scaleX: 1 }}
                        transition={{ delay: 1.2, duration: 0.8 }}
                    />
                </motion.div>

                {/* Bio */}
                {bio && (
                <motion.p
                    variants={itemVariants}
                    className="mt-8 text-base md:text-lg text-slate-500 dark:text-slate-400 
                             leading-relaxed max-w-xl mx-auto"
                >
                    {bio}
                </motion.p>
                )}

                {/* CTA Buttons */}
                <motion.div
                    variants={itemVariants}
                    className="mt-12 flex flex-wrap justify-center gap-5"
                >
                    <motion.button
                        onClick={onContactClick}
                        className="group relative px-8 py-4 rounded-2xl font-semibold text-white
                                 overflow-hidden transition-all duration-300"
                        whileHover={{ scale: 1.03, y: -3 }}
                        whileTap={{ scale: 0.97 }}
                    >
                        {/* Animated gradient background */}
                        <div className="absolute inset-0 bg-gradient-to-r from-blue-600 via-indigo-600 to-purple-600
                                       bg-[length:200%_100%] animate-[shimmer_3s_linear_infinite]" />
                        {/* Glow effect */}
                        <div className="absolute inset-0 rounded-2xl opacity-0 group-hover:opacity-100 transition-opacity duration-500
                                       shadow-[0_0_40px_rgba(99,102,241,0.5)]" />
                        <span className="relative flex items-center gap-2">
                            Let's Connect
                            <svg className="w-5 h-5 group-hover:translate-x-1.5 transition-transform duration-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17 8l4 4m0 0l-4 4m4-4H3" />
                            </svg>
                        </span>
                    </motion.button>

                    <motion.button
                        onClick={onDownloadClick}
                        className="group relative px-8 py-4 rounded-2xl font-semibold
                                 bg-white/80 dark:bg-white/[0.07] backdrop-blur-md
                                 border border-slate-200/80 dark:border-white/[0.12]
                                 text-slate-700 dark:text-white
                                 hover:bg-white/90 dark:hover:bg-white/[0.12]
                                 hover:border-blue-300 dark:hover:border-indigo-500/40
                                 shadow-lg shadow-slate-200/30 dark:shadow-black/20
                                 transition-all duration-300"
                        whileHover={{ scale: 1.03, y: -3 }}
                        whileTap={{ scale: 0.97 }}
                    >
                        <span className="flex items-center gap-2">
                            <svg className="w-5 h-5 group-hover:translate-y-0.5 transition-transform duration-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
                            </svg>
                            Download CV
                        </span>
                    </motion.button>
                </motion.div>
            </motion.div>

            {/* Scroll indicator — centered at the very bottom of the header */}
            <motion.a
                href="#skills"
                className="absolute bottom-8 left-0 right-0 z-10
                           flex flex-col items-center gap-3 group cursor-pointer"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                transition={{ delay: 2, duration: 1 }}
            >
                <span className="text-[10px] font-semibold tracking-[0.25em] uppercase
                               text-slate-400/70 dark:text-slate-500/70
                               group-hover:text-blue-500 dark:group-hover:text-indigo-400
                               transition-colors duration-300">
                    Scroll
                </span>
                <motion.div
                    className="w-[1.5px] h-8 bg-gradient-to-b from-slate-400/60 to-transparent
                               dark:from-slate-500/60 dark:to-transparent
                               group-hover:from-blue-500 dark:group-hover:from-indigo-400
                               transition-colors duration-300 origin-top"
                    animate={{ scaleY: [0, 1, 0] }}
                    transition={{ duration: 2, repeat: Infinity, ease: "easeInOut" }}
                />
            </motion.a>
        </header>
    );
};
