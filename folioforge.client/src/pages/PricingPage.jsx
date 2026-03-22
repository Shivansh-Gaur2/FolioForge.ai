import { useState, useEffect } from 'react';
import { motion } from 'framer-motion';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { BillingService } from '../services/billingService';

export const PricingPage = () => {
    const { isAuthenticated, user } = useAuth();
    const navigate = useNavigate();

    const [plans, setPlans] = useState([]);
    const [loading, setLoading] = useState(true);
    const [billingInterval, setBillingInterval] = useState('monthly');
    const [checkoutLoading, setCheckoutLoading] = useState(null);
    const [checkoutError, setCheckoutError] = useState(null);

    useEffect(() => {
        const fetchPlans = async () => {
            try {
                const data = await BillingService.getPlans();
                setPlans(data.plans ?? []);
            } catch {
                // Plans unavailable
            } finally {
                setLoading(false);
            }
        };
        fetchPlans();
    }, []);

    const handleUpgrade = async (plan) => {
        if (!isAuthenticated) {
            navigate('/register');
            return;
        }

        setCheckoutLoading(plan.id);
        setCheckoutError(null);
        try {
            // 1. Create subscription on backend
            const data = await BillingService.createSubscription({
                planId: plan.id,
                billingInterval,
            });

            // 2. Open Razorpay checkout modal
            BillingService.openCheckout({
                subscriptionId: data.subscriptionId,
                razorpayKeyId: data.razorpayKeyId,
                userEmail: data.userEmail,
                userName: data.userName,
                planId: plan.id,
                onSuccess: () => {
                    // Redirect to dashboard on successful payment
                    navigate('/dashboard?billing=success');
                },
                onError: (err) => {
                    setCheckoutLoading(null);
                    if (err?.message !== 'Payment cancelled') {
                        setCheckoutError('Payment verification failed. Please try again.');
                    }
                },
            });
        } catch (err) {
            setCheckoutError(err?.message || 'Failed to start checkout. Please try again.');
        } finally {
            setCheckoutLoading(null);
        }
    };

    const freePlan = plans.find(p => p.slug === 'free');
    const proPlan = plans.find(p => p.slug === 'pro');

    const formatPrice = (cents) => {
        if (cents === 0) return '$0';
        return `$${(cents / 100).toFixed(cents % 100 === 0 ? 0 : 2)}`;
    };

    const isCurrentPlan = (slug) => user?.planSlug === slug;

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 text-white">
            {/* Header */}
            <header className="border-b border-white/10 bg-white/5 backdrop-blur-lg">
                <div className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
                    <button
                        onClick={() => navigate(isAuthenticated ? '/dashboard' : '/login')}
                        className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent"
                    >
                        FolioForge
                    </button>
                    {!isAuthenticated && (
                        <button
                            onClick={() => navigate('/login')}
                            className="px-4 py-1.5 rounded-lg text-sm font-medium
                                       bg-white/10 hover:bg-white/20
                                       border border-white/10 transition-colors"
                        >
                            Sign In
                        </button>
                    )}
                </div>
            </header>

            <main className="max-w-5xl mx-auto px-6 py-16">
                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4 }}
                >
                    {/* Title */}
                    <div className="text-center mb-12">
                        <h1 className="text-4xl font-bold mb-4">
                            Simple, transparent pricing
                        </h1>
                        <p className="text-lg text-slate-400 max-w-xl mx-auto">
                            Start free and upgrade when you need more power.
                        </p>
                    </div>

                    {/* Billing Toggle */}
                    <div className="flex items-center justify-center gap-4 mb-12">
                        <span className={`text-sm ${billingInterval === 'monthly' ? 'text-white' : 'text-slate-500'}`}>
                            Monthly
                        </span>
                        <button
                            onClick={() => setBillingInterval(b => b === 'monthly' ? 'yearly' : 'monthly')}
                            className="relative w-14 h-7 rounded-full bg-white/10 border border-white/20 transition-colors"
                        >
                            <div className={`absolute top-0.5 w-6 h-6 rounded-full bg-gradient-to-r from-blue-500 to-purple-500 transition-transform
                                ${billingInterval === 'yearly' ? 'translate-x-7' : 'translate-x-0.5'}`} />
                        </button>
                        <span className={`text-sm ${billingInterval === 'yearly' ? 'text-white' : 'text-slate-500'}`}>
                            Yearly
                            <span className="ml-1 px-1.5 py-0.5 rounded-full text-xs bg-emerald-500/20 text-emerald-300">
                                Save 17%
                            </span>
                        </span>
                    </div>

                    {loading ? (
                        <div className="flex justify-center py-20">
                            <div className="animate-pulse text-slate-400">Loading plans...</div>
                        </div>
                    ) : (
                        <>
                            {checkoutError && (
                                <div className="mb-8 max-w-3xl mx-auto p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-300 text-sm text-center">
                                    {checkoutError}
                                </div>
                            )}
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 max-w-3xl mx-auto">
                            {/* Free Plan */}
                            <PlanCard
                                name="Free"
                                price={formatPrice(0)}
                                period=""
                                description="Perfect for getting started"
                                features={[
                                    { text: '1 portfolio', included: true },
                                    { text: '1 AI resume parse / month', included: true },
                                    { text: 'Basic themes', included: true },
                                    { text: 'FolioForge watermark', included: true },
                                    { text: 'Custom domain', included: false },
                                    { text: 'Analytics', included: false },
                                    { text: 'Password protection', included: false },
                                ]}
                                isCurrent={isCurrentPlan('free')}
                                ctaText={isCurrentPlan('free') ? 'Current Plan' : 'Get Started'}
                                onCta={() => !isCurrentPlan('free') && navigate('/register')}
                                disabled={isCurrentPlan('free')}
                            />

                            {/* Pro Plan */}
                            <PlanCard
                                name="Pro"
                                price={billingInterval === 'yearly'
                                    ? formatPrice(proPlan?.priceYearlyInCents ? Math.round(proPlan.priceYearlyInCents / 12) : 832)
                                    : formatPrice(proPlan?.priceMonthlyInCents ?? 999)}
                                period="/ month"
                                description="Everything you need to stand out"
                                highlighted
                                features={[
                                    { text: 'Unlimited portfolios', included: true },
                                    { text: '100 AI parses / month', included: true },
                                    { text: 'All premium themes', included: true },
                                    { text: 'No watermark', included: true },
                                    { text: 'Custom domain', included: true },
                                    { text: 'Analytics dashboard', included: true },
                                    { text: 'Password protection', included: true },
                                ]}
                                isCurrent={isCurrentPlan('pro')}
                                ctaText={isCurrentPlan('pro') ? 'Current Plan' : 'Upgrade to Pro'}
                                onCta={() => !isCurrentPlan('pro') && proPlan && handleUpgrade(proPlan)}
                                disabled={isCurrentPlan('pro')}
                                loading={checkoutLoading === proPlan?.id}
                            />
                        </div>
                        </>
                    )}
                </motion.div>
            </main>
        </div>
    );
};

const PlanCard = ({ name, price, period, description, features, highlighted, isCurrent, ctaText, onCta, disabled, loading }) => (
    <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ delay: highlighted ? 0.1 : 0 }}
        className={`relative rounded-2xl p-8 border ${
            highlighted
                ? 'bg-gradient-to-b from-blue-500/10 to-purple-500/10 border-blue-500/30 shadow-xl shadow-blue-500/10'
                : 'bg-white/5 border-white/10'
        }`}
    >
        {highlighted && (
            <div className="absolute -top-3 left-1/2 -translate-x-1/2">
                <span className="px-3 py-1 rounded-full text-xs font-semibold
                               bg-gradient-to-r from-blue-500 to-purple-500 text-white">
                    Most Popular
                </span>
            </div>
        )}

        <h3 className="text-xl font-bold mb-1">{name}</h3>
        <p className="text-sm text-slate-400 mb-6">{description}</p>

        <div className="mb-8">
            <span className="text-4xl font-bold">{price}</span>
            {period && <span className="text-slate-400 text-sm ml-1">{period}</span>}
        </div>

        <ul className="space-y-3 mb-8">
            {features.map((f, i) => (
                <li key={i} className="flex items-center gap-2 text-sm">
                    {f.included ? (
                        <svg className="w-4 h-4 text-emerald-400 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
                        </svg>
                    ) : (
                        <svg className="w-4 h-4 text-slate-600 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    )}
                    <span className={f.included ? 'text-slate-200' : 'text-slate-500'}>
                        {f.text}
                    </span>
                </li>
            ))}
        </ul>

        <button
            onClick={onCta}
            disabled={disabled || loading}
            className={`w-full py-3 rounded-lg text-sm font-semibold transition-all
                ${highlighted && !disabled
                    ? 'bg-gradient-to-r from-blue-500 to-purple-500 hover:from-blue-600 hover:to-purple-600 shadow-lg shadow-blue-500/25'
                    : disabled
                    ? 'bg-white/5 text-slate-500 cursor-not-allowed border border-white/10'
                    : 'bg-white/10 hover:bg-white/20 border border-white/10'
                }`}
        >
            {loading ? (
                <span className="flex items-center justify-center gap-2">
                    <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    Preparing checkout...
                </span>
            ) : ctaText}
        </button>

        {isCurrent && (
            <p className="text-xs text-emerald-400 text-center mt-3">
                You're on this plan
            </p>
        )}
    </motion.div>
);
