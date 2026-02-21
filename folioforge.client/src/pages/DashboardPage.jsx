import { motion } from 'framer-motion';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

/**
 * DashboardPage
 * 
 * Protected landing page after login. Shows user info and quick actions.
 * In the future this will list all portfolios, allow creating new ones, etc.
 */
export const DashboardPage = () => {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    const handleLogout = () => {
        logout();
        navigate('/login', { replace: true });
    };

    return (
        <div className="min-h-screen bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950 text-white">
            {/* Header */}
            <header className="border-b border-white/10 bg-white/5 backdrop-blur-lg sticky top-0 z-50">
                <div className="max-w-6xl mx-auto px-6 py-4 flex items-center justify-between">
                    <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                        FolioForge
                    </h1>
                    <div className="flex items-center gap-4">
                        <span className="text-sm text-slate-400">
                            {user?.email}
                        </span>
                        <button
                            onClick={handleLogout}
                            className="px-4 py-1.5 rounded-lg text-sm font-medium
                                       bg-white/10 hover:bg-white/20
                                       border border-white/10
                                       transition-colors"
                        >
                            Sign Out
                        </button>
                    </div>
                </div>
            </header>

            {/* Main Content */}
            <main className="max-w-6xl mx-auto px-6 py-12">
                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.4 }}
                >
                    {/* Welcome */}
                    <div className="mb-10">
                        <h2 className="text-3xl font-bold">
                            Welcome back, {user?.fullName?.split(' ')[0] || 'User'}
                        </h2>
                        <p className="text-slate-400 mt-2">
                            Manage your portfolio from here.
                        </p>
                    </div>

                    {/* Stats / Info Cards */}
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
                        <InfoCard
                            label="Account"
                            value={user?.fullName}
                            sub={user?.email}
                        />
                        <InfoCard
                            label="Tenant"
                            value={user?.tenantIdentifier || '—'}
                            sub={`ID: ${user?.tenantId?.slice(0, 8) || '—'}…`}
                        />
                        <InfoCard
                            label="User ID"
                            value={user?.userId?.slice(0, 8) + '…'}
                            sub="Unique identifier"
                        />
                    </div>

                    {/* Quick Actions */}
                    <div className="bg-white/5 border border-white/10 rounded-2xl p-8">
                        <h3 className="text-lg font-semibold mb-4">Quick Actions</h3>
                        <div className="flex flex-wrap gap-4">
                            <ActionButton label="View Portfolio" emoji="📄" disabled />
                            <ActionButton label="Create Portfolio" emoji="✨" disabled />
                            <ActionButton label="Upload Resume" emoji="📤" disabled />
                        </div>
                        <p className="text-sm text-slate-500 mt-4">
                            These features will be connected in future updates.
                        </p>
                    </div>
                </motion.div>
            </main>
        </div>
    );
};

const InfoCard = ({ label, value, sub }) => (
    <div className="bg-white/5 border border-white/10 rounded-xl p-5">
        <p className="text-xs uppercase tracking-wider text-slate-500 mb-1">{label}</p>
        <p className="text-lg font-semibold truncate">{value}</p>
        {sub && <p className="text-sm text-slate-400 truncate mt-0.5">{sub}</p>}
    </div>
);

const ActionButton = ({ label, emoji, disabled }) => (
    <button
        disabled={disabled}
        className="px-5 py-2.5 rounded-lg text-sm font-medium
                   bg-white/10 hover:bg-white/20 border border-white/10
                   disabled:opacity-40 disabled:cursor-not-allowed
                   transition-colors flex items-center gap-2"
    >
        <span>{emoji}</span>
        {label}
    </button>
);
