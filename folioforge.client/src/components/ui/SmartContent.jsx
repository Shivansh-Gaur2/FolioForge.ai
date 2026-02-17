import React from 'react';
import { motion } from 'framer-motion';

/**
 * SmartContent - A visually stunning component for rendering structured content
 * Handles both arrays (bullet points) and strings (paragraphs) gracefully
 */
export const SmartContent = ({ content, variant = 'timeline' }) => {
    // Scenario 1: It's an array - render as animated bullet points
    if (Array.isArray(content) && content.length > 0) {
        return <BulletList items={content} variant={variant} />;
    }

    // Scenario 2: It's a string - render as styled paragraph
    if (typeof content === 'string' && content.trim()) {
        return (
            <p className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed mt-3">
                {content}
            </p>
        );
    }

    return null;
};

/**
 * BulletList - Animated list with multiple visual variants
 */
const BulletList = ({ items, variant }) => {
    const containerVariants = {
        hidden: { opacity: 0 },
        visible: {
            opacity: 1,
            transition: {
                staggerChildren: 0.08,
                delayChildren: 0.1,
            },
        },
    };

    const itemVariants = {
        hidden: { opacity: 0, x: -20 },
        visible: {
            opacity: 1,
            x: 0,
            transition: {
                duration: 0.4,
                ease: [0.25, 0.46, 0.45, 0.94],
            },
        },
    };

    return (
        <motion.ul
            className="space-y-3 mt-4"
            initial="hidden"
            animate="visible"
            variants={containerVariants}
        >
            {items.map((item, idx) => (
                <motion.li
                    key={idx}
                    variants={itemVariants}
                    className="group"
                >
                    <BulletItem content={item} variant={variant} index={idx} />
                </motion.li>
            ))}
        </motion.ul>
    );
};

/**
 * BulletItem - Individual bullet point with unique styling variants
 */
const BulletItem = ({ content, variant, index }) => {
    const variants = {
        // Glowing orb with gradient line connector
        timeline: (
            <div className="relative pl-6 flex items-start">
                {/* Animated orb */}
                <span className="absolute left-0 top-1.5 flex items-center justify-center">
                    <span className="absolute w-3 h-3 bg-blue-500/20 dark:bg-blue-400/20 rounded-full animate-ping" />
                    <span className="relative w-2 h-2 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full shadow-sm shadow-blue-500/50" />
                </span>
                {/* Content */}
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Diamond bullet with hover glow
        diamond: (
            <div className="relative pl-6 flex items-start">
                <span className="absolute left-0 top-1.5 w-2 h-2 bg-gradient-to-br from-purple-500 to-pink-500 rotate-45 rounded-sm transform group-hover:scale-125 group-hover:shadow-lg group-hover:shadow-purple-500/50 transition-all duration-300" />
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Chevron with animated gradient
        chevron: (
            <div className="relative pl-6 flex items-start">
                <span className="absolute left-0 top-0.5 text-transparent bg-clip-text bg-gradient-to-r from-cyan-500 to-blue-500 font-bold text-sm group-hover:from-cyan-400 group-hover:to-blue-400 transition-all duration-300">
                    ›
                </span>
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Numbered badge style
        numbered: (
            <div className="relative pl-8 flex items-start">
                <span className="absolute left-0 top-0 w-5 h-5 flex items-center justify-center rounded-full bg-gradient-to-br from-indigo-500 to-purple-600 text-white text-xs font-bold shadow-md group-hover:shadow-indigo-500/40 group-hover:scale-110 transition-all duration-300">
                    {index + 1}
                </span>
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Minimalist dash with accent border
        minimal: (
            <div className="relative pl-4 border-l-2 border-slate-200 dark:border-slate-700 group-hover:border-blue-500 dark:group-hover:border-blue-400 transition-colors duration-300">
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Arrow with gradient background on hover
        arrow: (
            <div className="relative pl-6 py-1 rounded-md group-hover:bg-gradient-to-r group-hover:from-blue-50/80 group-hover:to-transparent dark:group-hover:from-blue-900/20 transition-all duration-300">
                <span className="absolute left-1 top-1/2 -translate-y-1/2 w-2 h-2 border-r-2 border-b-2 border-blue-500 dark:border-blue-400 rotate-[-45deg] group-hover:translate-x-0.5 transition-transform duration-300" />
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Gradient pill with animated hover effect - perfect for project cards
        gradient: (
            <div className="relative pl-5 py-0.5 flex items-start">
                {/* Animated gradient bar */}
                <span className="absolute left-0 top-1 bottom-1 w-1 rounded-full bg-gradient-to-b from-emerald-400 via-cyan-500 to-blue-500 group-hover:from-pink-500 group-hover:via-purple-500 group-hover:to-indigo-500 transition-all duration-500 group-hover:shadow-lg group-hover:shadow-purple-500/30" />
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Rocket style - fun and energetic for achievements
        rocket: (
            <div className="relative pl-7 flex items-start">
                <span className="absolute left-0 top-0.5 text-base transform group-hover:translate-x-0.5 group-hover:-translate-y-0.5 transition-transform duration-300">
                    🚀
                </span>
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),

        // Checkmark success style
        check: (
            <div className="relative pl-6 flex items-start">
                <span className="absolute left-0 top-0.5 w-4 h-4 flex items-center justify-center rounded-full bg-gradient-to-br from-green-400 to-emerald-600 shadow-sm group-hover:shadow-green-500/40 group-hover:scale-110 transition-all duration-300">
                    <svg className="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={3}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                    </svg>
                </span>
                <span className="text-slate-600 dark:text-slate-300 text-sm leading-relaxed group-hover:text-slate-800 dark:group-hover:text-slate-100 transition-colors duration-200">
                    {content}
                </span>
            </div>
        ),
    };

    return variants[variant] || variants.timeline;
};

/**
 * SmartContentSection - Full section wrapper with title and SmartContent
 * Use this for complete sections with headers
 */
export const SmartContentSection = ({ 
    title, 
    content, 
    variant = 'timeline',
    className = '' 
}) => {
    return (
        <div className={`${className}`}>
            {title && (
                <h4 className="text-sm font-semibold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-2">
                    {title}
                </h4>
            )}
            <SmartContent content={content} variant={variant} />
        </div>
    );
};

export default SmartContent;
