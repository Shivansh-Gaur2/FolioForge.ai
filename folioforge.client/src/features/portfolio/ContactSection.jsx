import { useState } from 'react';
import { motion } from 'framer-motion';
import { ScrollReveal } from '../../components/animations/ScrollReveal';

const socialLinks = [
    {
        name: 'GitHub',
        icon: (
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                <path fillRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clipRule="evenodd" />
            </svg>
        ),
        url: '#',
        color: 'hover:bg-slate-800 hover:text-white',
    },
    {
        name: 'LinkedIn',
        icon: (
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                <path d="M20.447 20.452h-3.554v-5.569c0-1.328-.027-3.037-1.852-3.037-1.853 0-2.136 1.445-2.136 2.939v5.667H9.351V9h3.414v1.561h.046c.477-.9 1.637-1.85 3.37-1.85 3.601 0 4.267 2.37 4.267 5.455v6.286zM5.337 7.433c-1.144 0-2.063-.926-2.063-2.065 0-1.138.92-2.063 2.063-2.063 1.14 0 2.064.925 2.064 2.063 0 1.139-.925 2.065-2.064 2.065zm1.782 13.019H3.555V9h3.564v11.452zM22.225 0H1.771C.792 0 0 .774 0 1.729v20.542C0 23.227.792 24 1.771 24h20.451C23.2 24 24 23.227 24 22.271V1.729C24 .774 23.2 0 22.222 0h.003z"/>
            </svg>
        ),
        url: '#',
        color: 'hover:bg-blue-600 hover:text-white',
    },
    {
        name: 'Twitter',
        icon: (
            <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                <path d="M18.244 2.25h3.308l-7.227 8.26 8.502 11.24H16.17l-5.214-6.817L4.99 21.75H1.68l7.73-8.835L1.254 2.25H8.08l4.713 6.231zm-1.161 17.52h1.833L7.084 4.126H5.117z"/>
            </svg>
        ),
        url: '#',
        color: 'hover:bg-slate-900 dark:hover:bg-white dark:hover:text-slate-900 hover:text-white',
    },
    {
        name: 'Email',
        icon: (
            <svg className="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
        ),
        url: 'mailto:hello@example.com',
        color: 'hover:bg-red-500 hover:text-white',
    },
];

export const ContactSection = () => {
    const [formData, setFormData] = useState({ name: '', email: '', message: '' });
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitted, setSubmitted] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setIsSubmitting(true);
        // Simulate API call
        await new Promise(resolve => setTimeout(resolve, 1500));
        setIsSubmitting(false);
        setSubmitted(true);
        setFormData({ name: '', email: '', message: '' });
    };

    const handleChange = (e) => {
        setFormData(prev => ({ ...prev, [e.target.name]: e.target.value }));
    };

    return (
        <section id="contact" className="py-24 relative overflow-hidden
                                        bg-gradient-to-b from-slate-50 to-slate-100
                                        dark:from-slate-900 dark:to-slate-950">
            {/* Background decorations */}
            <div className="absolute top-0 left-1/4 w-96 h-96 bg-blue-200/30 dark:bg-blue-500/10 rounded-full blur-3xl" />
            <div className="absolute bottom-0 right-1/4 w-96 h-96 bg-purple-200/30 dark:bg-purple-500/10 rounded-full blur-3xl" />

            <div className="relative max-w-5xl mx-auto px-6">
                <ScrollReveal>
                    <div className="text-center mb-16">
                        <span className="inline-block px-4 py-2 rounded-full 
                                       bg-rose-100 dark:bg-rose-900/30 
                                       text-rose-600 dark:text-rose-400
                                       text-sm font-semibold mb-4">
                            Get In Touch
                        </span>
                        <h2 className="text-4xl md:text-5xl font-black text-slate-900 dark:text-white mb-4">
                            Let's Work Together
                        </h2>
                        <p className="text-lg text-slate-500 dark:text-slate-400 max-w-2xl mx-auto">
                            Have a project in mind? Let's discuss how we can bring your ideas to life.
                        </p>
                    </div>
                </ScrollReveal>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
                    {/* Contact form */}
                    <ScrollReveal direction="left">
                        <motion.div
                            className="bg-white dark:bg-slate-800/50 rounded-3xl p-8
                                     border border-slate-100 dark:border-slate-700/50
                                     shadow-xl shadow-slate-200/50 dark:shadow-black/30"
                        >
                            {submitted ? (
                                <motion.div
                                    initial={{ opacity: 0, scale: 0.9 }}
                                    animate={{ opacity: 1, scale: 1 }}
                                    className="text-center py-12"
                                >
                                    <div className="text-6xl mb-4">🎉</div>
                                    <h3 className="text-2xl font-bold text-slate-900 dark:text-white mb-2">
                                        Message Sent!
                                    </h3>
                                    <p className="text-slate-500 dark:text-slate-400">
                                        Thanks for reaching out. I'll get back to you soon!
                                    </p>
                                    <button
                                        onClick={() => setSubmitted(false)}
                                        className="mt-6 text-blue-600 dark:text-blue-400 font-medium"
                                    >
                                        Send another message
                                    </button>
                                </motion.div>
                            ) : (
                                <form onSubmit={handleSubmit} className="space-y-6">
                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                                            Your Name
                                        </label>
                                        <input
                                            type="text"
                                            name="name"
                                            value={formData.name}
                                            onChange={handleChange}
                                            required
                                            className="w-full px-4 py-3 rounded-xl
                                                     bg-slate-50 dark:bg-slate-900/50
                                                     border border-slate-200 dark:border-slate-700
                                                     text-slate-900 dark:text-white
                                                     placeholder-slate-400
                                                     focus:ring-2 focus:ring-blue-500 focus:border-transparent
                                                     transition-all"
                                            placeholder="John Doe"
                                        />
                                    </div>

                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                                            Email Address
                                        </label>
                                        <input
                                            type="email"
                                            name="email"
                                            value={formData.email}
                                            onChange={handleChange}
                                            required
                                            className="w-full px-4 py-3 rounded-xl
                                                     bg-slate-50 dark:bg-slate-900/50
                                                     border border-slate-200 dark:border-slate-700
                                                     text-slate-900 dark:text-white
                                                     placeholder-slate-400
                                                     focus:ring-2 focus:ring-blue-500 focus:border-transparent
                                                     transition-all"
                                            placeholder="john@example.com"
                                        />
                                    </div>

                                    <div>
                                        <label className="block text-sm font-medium text-slate-700 dark:text-slate-300 mb-2">
                                            Message
                                        </label>
                                        <textarea
                                            name="message"
                                            value={formData.message}
                                            onChange={handleChange}
                                            required
                                            rows={5}
                                            className="w-full px-4 py-3 rounded-xl resize-none
                                                     bg-slate-50 dark:bg-slate-900/50
                                                     border border-slate-200 dark:border-slate-700
                                                     text-slate-900 dark:text-white
                                                     placeholder-slate-400
                                                     focus:ring-2 focus:ring-blue-500 focus:border-transparent
                                                     transition-all"
                                            placeholder="Tell me about your project..."
                                        />
                                    </div>

                                    <motion.button
                                        type="submit"
                                        disabled={isSubmitting}
                                        whileHover={{ scale: 1.02 }}
                                        whileTap={{ scale: 0.98 }}
                                        className="w-full py-4 rounded-xl font-semibold
                                                 bg-gradient-to-r from-blue-600 to-indigo-600
                                                 hover:from-blue-500 hover:to-indigo-500
                                                 text-white shadow-lg shadow-blue-500/25
                                                 disabled:opacity-50 disabled:cursor-not-allowed
                                                 transition-all duration-300"
                                    >
                                        {isSubmitting ? (
                                            <span className="flex items-center justify-center gap-2">
                                                <svg className="animate-spin w-5 h-5" fill="none" viewBox="0 0 24 24">
                                                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                                                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
                                                </svg>
                                                Sending...
                                            </span>
                                        ) : (
                                            <span className="flex items-center justify-center gap-2">
                                                Send Message
                                                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                                                </svg>
                                            </span>
                                        )}
                                    </motion.button>
                                </form>
                            )}
                        </motion.div>
                    </ScrollReveal>

                    {/* Contact info */}
                    <ScrollReveal direction="right">
                        <div className="space-y-8">
                            <div className="bg-white dark:bg-slate-800/50 rounded-3xl p-8
                                          border border-slate-100 dark:border-slate-700/50
                                          shadow-xl shadow-slate-200/50 dark:shadow-black/30">
                                <h3 className="text-xl font-bold text-slate-900 dark:text-white mb-4">
                                    Quick Connect
                                </h3>
                                <p className="text-slate-600 dark:text-slate-400 mb-6">
                                    Prefer a quick chat? Reach out through any of these platforms.
                                </p>

                                <div className="grid grid-cols-2 gap-4">
                                    {socialLinks.map((link, idx) => (
                                        <motion.a
                                            key={link.name}
                                            href={link.url}
                                            initial={{ opacity: 0, y: 20 }}
                                            whileInView={{ opacity: 1, y: 0 }}
                                            transition={{ delay: idx * 0.1 }}
                                            whileHover={{ scale: 1.05, y: -2 }}
                                            className={`flex items-center gap-3 p-4 rounded-xl
                                                      bg-slate-50 dark:bg-slate-900/50
                                                      border border-slate-100 dark:border-slate-700
                                                      text-slate-600 dark:text-slate-400
                                                      transition-all duration-300 ${link.color}`}
                                        >
                                            {link.icon}
                                            <span className="font-medium">{link.name}</span>
                                        </motion.a>
                                    ))}
                                </div>
                            </div>

                            {/* Availability status */}
                            <motion.div
                                initial={{ opacity: 0, y: 20 }}
                                whileInView={{ opacity: 1, y: 0 }}
                                className="bg-gradient-to-br from-emerald-500 to-teal-600 
                                         rounded-3xl p-8 text-white"
                            >
                                <div className="flex items-center gap-3 mb-4">
                                    <span className="relative flex h-3 w-3">
                                        <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-white opacity-75"></span>
                                        <span className="relative inline-flex rounded-full h-3 w-3 bg-white"></span>
                                    </span>
                                    <span className="font-semibold">Currently Available</span>
                                </div>
                                <p className="text-emerald-100">
                                    I'm open to new opportunities and exciting projects. 
                                    Let's create something amazing together!
                                </p>
                            </motion.div>
                        </div>
                    </ScrollReveal>
                </div>
            </div>
        </section>
    );
};
