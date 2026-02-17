import { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { ThemeToggle } from '../ui/ThemeToggle';

const navItems = [
    { id: 'hero', label: 'Home', icon: '🏠' },
    { id: 'skills', label: 'Skills', icon: '⚡' },
    { id: 'experience', label: 'Experience', icon: '💼' },
    { id: 'projects', label: 'Projects', icon: '🚀' },
    { id: 'contact', label: 'Contact', icon: '✉️' },
];

export const FloatingNav = () => {
    const [activeSection, setActiveSection] = useState('hero');
    const [isScrolled, setIsScrolled] = useState(false);
    const [isVisible, setIsVisible] = useState(true);
    const [lastScrollY, setLastScrollY] = useState(0);

    useEffect(() => {
        const handleScroll = () => {
            const currentScrollY = window.scrollY;
            
            // Show/hide based on scroll direction
            setIsVisible(currentScrollY < lastScrollY || currentScrollY < 100);
            setLastScrollY(currentScrollY);
            
            // Add background when scrolled
            setIsScrolled(currentScrollY > 50);

            // Update active section
            const sections = navItems.map(item => document.getElementById(item.id));
            const scrollPosition = currentScrollY + window.innerHeight / 3;

            for (let i = sections.length - 1; i >= 0; i--) {
                const section = sections[i];
                if (section && section.offsetTop <= scrollPosition) {
                    setActiveSection(navItems[i].id);
                    break;
                }
            }
        };

        window.addEventListener('scroll', handleScroll, { passive: true });
        return () => window.removeEventListener('scroll', handleScroll);
    }, [lastScrollY]);

    const scrollToSection = (id) => {
        const element = document.getElementById(id);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    };

    return (
        <AnimatePresence>
            {isVisible && (
                <motion.nav
                    initial={{ y: -100, opacity: 0 }}
                    animate={{ y: 0, opacity: 1 }}
                    exit={{ y: -100, opacity: 0 }}
                    transition={{ duration: 0.3 }}
                    className={`fixed top-4 left-1/2 -translate-x-1/2 z-50
                              px-2 py-2 rounded-2xl
                              transition-all duration-500
                              ${isScrolled 
                                  ? 'bg-white/80 dark:bg-slate-900/80 backdrop-blur-xl shadow-xl shadow-slate-200/50 dark:shadow-black/30 border border-slate-200/50 dark:border-white/10' 
                                  : 'bg-transparent'}`}
                >
                    <ul className="flex items-center gap-1">
                        {navItems.map((item) => (
                            <li key={item.id}>
                                <motion.button
                                    onClick={() => scrollToSection(item.id)}
                                    className={`relative px-4 py-2 rounded-xl text-sm font-medium
                                              transition-colors duration-300
                                              ${activeSection === item.id
                                                  ? 'text-blue-600 dark:text-blue-400'
                                                  : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white'
                                              }`}
                                    whileHover={{ scale: 1.05 }}
                                    whileTap={{ scale: 0.95 }}
                                >
                                    {activeSection === item.id && (
                                        <motion.div
                                            layoutId="activeNav"
                                            className="absolute inset-0 bg-blue-100 dark:bg-blue-900/30 rounded-xl"
                                            transition={{ type: "spring", stiffness: 400, damping: 30 }}
                                        />
                                    )}
                                    <span className="relative flex items-center gap-1.5">
                                        <span className="hidden sm:inline">{item.icon}</span>
                                        {item.label}
                                    </span>
                                </motion.button>
                            </li>
                        ))}
                        
                        {/* Divider */}
                        <li className="w-px h-6 bg-slate-200 dark:bg-slate-700 mx-2" />
                        
                        {/* Theme toggle */}
                        <li>
                            <ThemeToggle />
                        </li>
                    </ul>
                </motion.nav>
            )}
        </AnimatePresence>
    );
};
