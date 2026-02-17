import { motion } from 'framer-motion';
import { useInView } from 'react-intersection-observer';

/**
 * Scroll-triggered animation wrapper
 * Reveals children with stunning animations when they enter viewport
 */
export const ScrollReveal = ({ 
    children, 
    direction = 'up', 
    delay = 0,
    duration = 0.6,
    className = '',
    once = true,
}) => {
    const [ref, inView] = useInView({
        threshold: 0.1,
        triggerOnce: once,
    });

    const directions = {
        up: { y: 60, x: 0 },
        down: { y: -60, x: 0 },
        left: { x: 60, y: 0 },
        right: { x: -60, y: 0 },
        none: { x: 0, y: 0 },
    };

    const initial = {
        opacity: 0,
        ...directions[direction],
    };

    return (
        <motion.div
            ref={ref}
            initial={initial}
            animate={inView ? { opacity: 1, x: 0, y: 0 } : initial}
            transition={{
                duration,
                delay,
                ease: [0.25, 0.46, 0.45, 0.94], // easeOutQuad
            }}
            className={className}
        >
            {children}
        </motion.div>
    );
};

/**
 * Staggered children animation - each child animates in sequence
 */
export const StaggerContainer = ({ 
    children, 
    staggerDelay = 0.1,
    className = '',
}) => {
    const [ref, inView] = useInView({
        threshold: 0.1,
        triggerOnce: true,
    });

    return (
        <motion.div
            ref={ref}
            initial="hidden"
            animate={inView ? "visible" : "hidden"}
            variants={{
                hidden: {},
                visible: {
                    transition: {
                        staggerChildren: staggerDelay,
                    },
                },
            }}
            className={className}
        >
            {children}
        </motion.div>
    );
};

/**
 * Individual stagger item - use inside StaggerContainer
 */
export const StaggerItem = ({ children, className = '' }) => (
    <motion.div
        variants={{
            hidden: { opacity: 0, y: 30 },
            visible: { 
                opacity: 1, 
                y: 0,
                transition: {
                    duration: 0.5,
                    ease: [0.25, 0.46, 0.45, 0.94],
                },
            },
        }}
        className={className}
    >
        {children}
    </motion.div>
);

/**
 * Floating animation - subtle up/down movement
 */
export const FloatingElement = ({ children, className = '', amplitude = 10 }) => (
    <motion.div
        animate={{ 
            y: [-amplitude, amplitude, -amplitude],
        }}
        transition={{
            duration: 4,
            ease: "easeInOut",
            repeat: Infinity,
        }}
        className={className}
    >
        {children}
    </motion.div>
);

/**
 * Scale on hover effect
 */
export const ScaleOnHover = ({ children, scale = 1.05, className = '' }) => (
    <motion.div
        whileHover={{ scale }}
        whileTap={{ scale: 0.98 }}
        transition={{ type: "spring", stiffness: 400, damping: 17 }}
        className={className}
    >
        {children}
    </motion.div>
);

/**
 * Page transition wrapper
 */
export const PageTransition = ({ children }) => (
    <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        exit={{ opacity: 0 }}
        transition={{ duration: 0.3 }}
    >
        {children}
    </motion.div>
);
