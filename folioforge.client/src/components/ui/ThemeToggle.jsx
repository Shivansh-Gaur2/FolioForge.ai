import { motion } from 'framer-motion';
import { useTheme } from '../../context/ThemeContext';

export const ThemeToggle = () => {
    const { isDark, toggle } = useTheme();

    return (
        <motion.button
            onClick={toggle}
            className="relative w-14 h-7 rounded-full p-1 transition-colors duration-300
                       bg-gradient-to-r from-amber-200 to-orange-300 
                       dark:from-indigo-600 dark:to-purple-700
                       shadow-inner"
            whileHover={{ scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
        >
            <motion.div
                className="w-5 h-5 rounded-full bg-white shadow-md flex items-center justify-center"
                animate={{ x: isDark ? 26 : 0 }}
                transition={{ type: "spring", stiffness: 500, damping: 30 }}
            >
                {/* Sun icon */}
                <motion.svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    className="w-3 h-3 text-amber-500"
                    initial={false}
                    animate={{ 
                        scale: isDark ? 0 : 1,
                        rotate: isDark ? -90 : 0,
                    }}
                >
                    <path d="M12 2.25a.75.75 0 01.75.75v2.25a.75.75 0 01-1.5 0V3a.75.75 0 01.75-.75zM7.5 12a4.5 4.5 0 119 0 4.5 4.5 0 01-9 0zM18.894 6.166a.75.75 0 00-1.06-1.06l-1.591 1.59a.75.75 0 101.06 1.061l1.591-1.59zM21.75 12a.75.75 0 01-.75.75h-2.25a.75.75 0 010-1.5H21a.75.75 0 01.75.75zM17.834 18.894a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 10-1.061 1.06l1.59 1.591zM12 18a.75.75 0 01.75.75V21a.75.75 0 01-1.5 0v-2.25A.75.75 0 0112 18zM7.758 17.303a.75.75 0 00-1.061-1.06l-1.591 1.59a.75.75 0 001.06 1.061l1.591-1.59zM6 12a.75.75 0 01-.75.75H3a.75.75 0 010-1.5h2.25A.75.75 0 016 12zM6.697 7.757a.75.75 0 001.06-1.06l-1.59-1.591a.75.75 0 00-1.061 1.06l1.59 1.591z" />
                </motion.svg>
                
                {/* Moon icon */}
                <motion.svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    fill="currentColor"
                    className="w-3 h-3 text-indigo-600 absolute"
                    initial={false}
                    animate={{ 
                        scale: isDark ? 1 : 0,
                        rotate: isDark ? 0 : 90,
                    }}
                >
                    <path fillRule="evenodd" d="M9.528 1.718a.75.75 0 01.162.819A8.97 8.97 0 009 6a9 9 0 009 9 8.97 8.97 0 003.463-.69.75.75 0 01.981.98 10.503 10.503 0 01-9.694 6.46c-5.799 0-10.5-4.701-10.5-10.5 0-4.368 2.667-8.112 6.46-9.694a.75.75 0 01.818.162z" clipRule="evenodd" />
                </motion.svg>
            </motion.div>
        </motion.button>
    );
};
