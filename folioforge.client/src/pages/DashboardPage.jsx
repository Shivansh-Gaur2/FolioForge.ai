import { useState, useEffect, useCallback, useRef } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';
import { PortfolioService } from '../services/portfolioService';

/**
 * DashboardPage
 *
 * Protected landing page after login.
 * - Lists the user's portfolios
 * - Create a new portfolio (title + slug)
 * - Upload a resume PDF to an existing portfolio
 * - Navigate to view a portfolio
 */
export const DashboardPage = () => {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    // ── Portfolio list state ─────────────────────────────
    const [portfolios, setPortfolios] = useState([]);
    const [listLoading, setListLoading] = useState(true);
    const [listError, setListError] = useState(null);

    // ── Create form state ────────────────────────────────
    const [showCreate, setShowCreate] = useState(false);
    const [createForm, setCreateForm] = useState({ title: '', slug: '' });
    const [creating, setCreating] = useState(false);
    const [createError, setCreateError] = useState(null);

    // ── Upload state ─────────────────────────────────────
    const [uploadingId, setUploadingId] = useState(null);
    const [uploadStatus, setUploadStatus] = useState(null); // { type: 'success'|'error', message }

    // ── Delete state ─────────────────────────────────────
    const [deletingId, setDeletingId] = useState(null);

    // ── AI processing poll state ──────────────────────────
    const [processingId, setProcessingId] = useState(null);
    const pollingRef = useRef(null);


    // ── Fetch user's portfolios ──────────────────────────
    const fetchPortfolios = useCallback(async () => {
        setListLoading(true);
        setListError(null);
        try {
            const result = await PortfolioService.listMine();
            // Backend returns PagedResult { items, page, totalCount, ... }
            setPortfolios(result.items ?? result);
        } catch (err) {
            setListError(err.message || 'Failed to load portfolios');
        } finally {
            setListLoading(false);
        }
    }, []);

    const startPolling = useCallback((portfolioId, initialSectionCount) => {
        if(pollingRef.current) clearInterval(pollingRef.current);

        setProcessingId(portfolioId);
        setUploadStatus({
            type: 'processing', 
            message: 'Processing your resume may take some time...',
        });

        let attempts = 0;
        const MAX_ATTEMPTS = 20;

        pollingRef.current = setInterval(async () => {
            attempts++;

            if(attempts > MAX_ATTEMPTS){
                clearInterval(pollingRef.current);
                setProcessingId(null);
                setUploadStatus({
                    type: 'error',
                    message: 'Processing is taking longer than expected. Please refresh later.',
                });
                return;
            }

            try {
                const portfolio = await PortfolioService.getById(portfolioId);
                if((portfolio.sections?.length ?? 0) > initialSectionCount){
                    clearInterval(pollingRef.current);
                    setProcessingId(null);
                    await fetchPortfolios();
                    setUploadStatus({
                        type: 'success',
                        message: '✅ Portfolio updated successfully from your resume!',
                    });
                }
            }
            catch{

            }
        }, 3000);

    }, [fetchPortfolios]);

    useEffect(() => {
        fetchPortfolios();
    }, [fetchPortfolios]);
    
    useEffect(() => {
        return () => {
            if(pollingRef.current) clearInterval(pollingRef.current);
        }
    }, []);

    // ── Handlers ─────────────────────────────────────────
    const handleLogout = () => {
        logout();
        navigate('/login', { replace: true });
    };

    const handleCreate = async (e) => {
        e.preventDefault();
        setCreating(true);
        setCreateError(null);
        try {
            await PortfolioService.create({
                title: createForm.title.trim(),
                slug: createForm.slug.trim().toLowerCase(),
            });
            setCreateForm({ title: '', slug: '' });
            setShowCreate(false);
            await fetchPortfolios(); // refresh list
        } catch (err) {
            setCreateError(err.message || 'Failed to create portfolio');
        } finally {
            setCreating(false);
        }
    };

const handleUpload = async (portfolioId, file) => {
    setUploadingId(portfolioId);
    setUploadStatus(null);

    const currentPortfolio = portfolios.find(p => p.id === portfolioId);
    const initialSectionCount = currentPortfolio?.sections?.length ?? 0;

    try {
        await PortfolioService.uploadResume(portfolioId, file);
        startPolling(portfolioId, initialSectionCount);
    } catch (err) {
        setUploadStatus({
            type: 'error',
            message: err.message || 'Upload failed',
        });
    } finally {
        setUploadingId(null);
    }
};

    const handleDelete = async (portfolioId) => {
        if (!window.confirm('Are you sure you want to delete this portfolio? This cannot be undone.')) return;
        setDeletingId(portfolioId);
        try {
            await PortfolioService.delete(portfolioId);
            await fetchPortfolios();
        } catch (err) {
            setUploadStatus({
                type: 'error',
                message: err.message || 'Failed to delete portfolio',
            });
        } finally {
            setDeletingId(null);
        }
    };

    const autoSlug = (title) =>
        title
            .toLowerCase()
            .replace(/[^a-z0-9]+/g, '-')
            .replace(/^-|-$/g, '');

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
                    <div className="mb-10 flex items-end justify-between">
                        <div>
                            <h2 className="text-3xl font-bold">
                                Welcome back, {user?.fullName?.split(' ')[0] || 'User'}
                            </h2>
                            <p className="text-slate-400 mt-2">
                                Manage your portfolios from here.
                            </p>
                        </div>
                        <button
                            onClick={() => setShowCreate(true)}
                            className="px-5 py-2.5 rounded-lg text-sm font-semibold
                                       bg-gradient-to-r from-blue-500 to-purple-500
                                       hover:from-blue-600 hover:to-purple-600
                                       transition-all shadow-lg shadow-blue-500/25
                                       flex items-center gap-2"
                        >
                            <span className="text-lg">✨</span> New Portfolio
                        </button>
                    </div>

                    {/* Upload Status Banner */}
                    <AnimatePresence>
                        {uploadStatus && (
                            <motion.div
                                initial={{ opacity: 0, y: -10 }}
                                animate={{ opacity: 1, y: 0 }}
                                exit={{ opacity: 0, y: -10 }}
                                className={`mb-6 p-4 rounded-xl border flex items-center justify-between ${
                                    uploadStatus.type === 'success'
                                        ? 'bg-emerald-500/10 border-emerald-500/30 text-emerald-300'
                                        : uploadStatus.type === 'processing'
                                        ? 'bg-blue-500/10 border-blue-500/30 text-blue-300'
                                        : 'bg-red-500/10 border-red-500/30 text-red-300'
                                }`}
                            >
                                <span className="flex items-center gap-2">
                                    {uploadStatus.type === 'processing' && (
                                        <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                                        </svg>
                                    )}
                                    {uploadStatus.message}
                                </span>
                                {uploadStatus.type !== 'processing' && (
                                    <button
                                        onClick={() => {
                                            setUploadStatus(null);
                                            if (uploadStatus.type === 'success') fetchPortfolios();
                                        }}
                                        className="text-xs underline ml-4"
                                    >
                                        {uploadStatus.type === 'success' ? 'Refresh & Dismiss' : 'Dismiss'}
                                    </button>
                                )}
                            </motion.div>
                        )}
                    </AnimatePresence>

                    {/* Create Portfolio Modal */}
                    <AnimatePresence>
                        {showCreate && (
                            <motion.div
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                exit={{ opacity: 0 }}
                                className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm"
                                onClick={() => setShowCreate(false)}
                            >
                                <motion.div
                                    initial={{ scale: 0.9, opacity: 0 }}
                                    animate={{ scale: 1, opacity: 1 }}
                                    exit={{ scale: 0.9, opacity: 0 }}
                                    onClick={(e) => e.stopPropagation()}
                                    className="bg-slate-900 border border-white/10 rounded-2xl p-8 w-full max-w-md shadow-2xl"
                                >
                                    <h3 className="text-xl font-bold mb-6">Create Portfolio</h3>
                                    <form onSubmit={handleCreate} className="space-y-5">
                                        <div>
                                            <label className="block text-sm font-medium text-slate-300 mb-1.5">
                                                Portfolio Title
                                            </label>
                                            <input
                                                type="text"
                                                required
                                                placeholder="My Developer Portfolio"
                                                value={createForm.title}
                                                onChange={(e) => {
                                                    const title = e.target.value;
                                                    setCreateForm({
                                                        title,
                                                        slug: autoSlug(title),
                                                    });
                                                }}
                                                className="w-full px-4 py-2.5 rounded-lg bg-white/5
                                                           border border-white/10 focus:border-blue-500
                                                           text-white placeholder-slate-500
                                                           outline-none transition-colors"
                                            />
                                        </div>
                                        <div>
                                            <label className="block text-sm font-medium text-slate-300 mb-1.5">
                                                URL Slug
                                            </label>
                                            <input
                                                type="text"
                                                required
                                                placeholder="my-developer-portfolio"
                                                value={createForm.slug}
                                                onChange={(e) =>
                                                    setCreateForm((f) => ({
                                                        ...f,
                                                        slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''),
                                                    }))
                                                }
                                                className="w-full px-4 py-2.5 rounded-lg bg-white/5
                                                           border border-white/10 focus:border-blue-500
                                                           text-white placeholder-slate-500
                                                           outline-none transition-colors font-mono text-sm"
                                            />
                                            <p className="text-xs text-slate-500 mt-1">
                                                Letters, numbers, and dashes only
                                            </p>
                                        </div>
                                        {createError && (
                                            <p className="text-sm text-red-400">{createError}</p>
                                        )}
                                        <div className="flex gap-3 pt-2">
                                            <button
                                                type="button"
                                                onClick={() => {
                                                    setShowCreate(false);
                                                    setCreateError(null);
                                                }}
                                                className="flex-1 px-4 py-2.5 rounded-lg text-sm
                                                           bg-white/10 hover:bg-white/20
                                                           border border-white/10 transition-colors"
                                            >
                                                Cancel
                                            </button>
                                            <button
                                                type="submit"
                                                disabled={creating}
                                                className="flex-1 px-4 py-2.5 rounded-lg text-sm font-semibold
                                                           bg-gradient-to-r from-blue-500 to-purple-500
                                                           hover:from-blue-600 hover:to-purple-600
                                                           disabled:opacity-50 disabled:cursor-not-allowed
                                                           transition-all"
                                            >
                                                {creating ? 'Creating…' : 'Create'}
                                            </button>
                                        </div>
                                    </form>
                                </motion.div>
                            </motion.div>
                        )}
                    </AnimatePresence>

                    {/* Portfolio List */}
                    <div>
                        {listLoading ? (
                            <div className="flex justify-center py-20">
                                <div className="flex items-center gap-3 text-slate-400">
                                    <svg className="animate-spin h-5 w-5" viewBox="0 0 24 24">
                                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                                    </svg>
                                    Loading portfolios…
                                </div>
                            </div>
                        ) : listError ? (
                            <div className="text-center py-20">
                                <p className="text-red-400 mb-4">{listError}</p>
                                <button
                                    onClick={fetchPortfolios}
                                    className="px-4 py-2 rounded-lg bg-white/10 hover:bg-white/20
                                               text-sm transition-colors"
                                >
                                    Retry
                                </button>
                            </div>
                        ) : portfolios.length === 0 ? (
                            <EmptyState onCreateClick={() => setShowCreate(true)} />
                        ) : (
                            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                                {portfolios.map((p, i) => (
                                    <PortfolioCard
                                        key={p.id}
                                        portfolio={p}
                                        index={i}
                                        onView={() => navigate(`/portfolio/${p.id}`)}
                                        onCustomize={() => navigate(`/portfolio/${p.id}/edit`)}
                                        onUpload={handleUpload}
                                        isUploading={uploadingId === p.id}
                                        isProcessing={processingId === p.id}
                                        onDelete={handleDelete}
                                        isDeleting={deletingId === p.id}
                                    />
                                ))}
                            </div>
                        )}
                    </div>
                </motion.div>
            </main>
        </div>
    );
};

// ── Sub-components ───────────────────────────────────────

const EmptyState = ({ onCreateClick }) => (
    <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        className="text-center py-20"
    >
        <div className="text-6xl mb-4">📁</div>
        <h3 className="text-xl font-semibold mb-2">No portfolios yet</h3>
        <p className="text-slate-400 mb-6">
            Create your first portfolio and upload a resume to generate it with AI.
        </p>
        <button
            onClick={onCreateClick}
            className="px-6 py-3 rounded-lg text-sm font-semibold
                       bg-gradient-to-r from-blue-500 to-purple-500
                       hover:from-blue-600 hover:to-purple-600
                       transition-all shadow-lg shadow-blue-500/25"
        >
            ✨ Create Your First Portfolio
        </button>
    </motion.div>
);

const PortfolioCard = ({ portfolio, index, onView, onCustomize, onUpload, isUploading, isProcessing, onDelete, isDeleting }) => {
    const [dragOver, setDragOver] = useState(false);
    const sectionCount = portfolio.sections?.length || 0;
    const hasContent = sectionCount > 1; // more than the default Markdown section

    const handleFileSelect = (e) => {
        const file = e.target.files?.[0];
        if (file) onUpload(portfolio.id, file);
    };

    const handleDrop = (e) => {
        e.preventDefault();
        setDragOver(false);
        const file = e.dataTransfer.files?.[0];
        if (file && file.type === 'application/pdf') {
            onUpload(portfolio.id, file);
        }
    };

    return (
        <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: index * 0.08 }}
            onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
            onDragLeave={() => setDragOver(false)}
            onDrop={handleDrop}
            className={`group bg-white/5 border rounded-2xl p-6
                        hover:bg-white/[0.08] transition-all duration-300
                        ${dragOver
                            ? 'border-blue-400 shadow-lg shadow-blue-500/20'
                            : 'border-white/10'}`}
        >
            {/* Title & Slug */}
            <div className="mb-4">
                <h3 className="text-lg font-bold truncate">{portfolio.title}</h3>
                <p className="text-sm text-slate-500 font-mono truncate">/{portfolio.slug}</p>
            </div>

            {/* Status badges */}
            <div className="flex items-center gap-2 mb-5 text-xs">
                {isProcessing && (
                    <span className="px-2 py-0.5 rounded-full bg-blue-500/20 text-blue-300 flex items-center gap-1">
                        <svg className="animate-spin h-3 w-3" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                        </svg>
                        AI processing…
                    </span>
                )}
                <span className={`px-2 py-0.5 rounded-full ${
                    hasContent
                        ? 'bg-emerald-500/20 text-emerald-300'
                        : 'bg-amber-500/20 text-amber-300'
                }`}>
                    {hasContent ? `${sectionCount} sections` : 'Needs resume'}
                </span>
            </div>

            {/* Actions */}
            <div className="flex gap-2">
                <button
                    onClick={onView}
                    className="flex-1 px-3 py-2 rounded-lg text-sm font-medium
                               bg-white/10 hover:bg-white/20
                               border border-white/10
                               transition-colors flex items-center justify-center gap-1.5"
                >
                    👁️ View
                </button>
                <button
                    onClick={onCustomize}
                    className="flex-1 px-3 py-2 rounded-lg text-sm font-medium
                               bg-purple-500/10 hover:bg-purple-500/20 text-purple-300
                               border border-purple-500/20
                               transition-colors flex items-center justify-center gap-1.5"
                >
                    🎨 Customize
                </button>
                <button
                    onClick={() => onDelete(portfolio.id)}
                    disabled={isDeleting}
                    className="px-3 py-2 rounded-lg text-sm font-medium
                               bg-red-500/10 hover:bg-red-500/20 text-red-400
                               border border-red-500/20
                               disabled:opacity-50 disabled:cursor-not-allowed
                               transition-colors flex items-center justify-center gap-1.5"
                >
                    {isDeleting ? (
                        <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                        </svg>
                    ) : '🗑️'}
                </button>
                <label
                    className={`flex-1 px-3 py-2 rounded-lg text-sm font-medium
                               border border-white/10 transition-colors
                               flex items-center justify-center gap-1.5 cursor-pointer
                               ${isUploading
                                   ? 'bg-blue-500/20 text-blue-300 cursor-wait'
                                   : 'bg-white/10 hover:bg-white/20'}`}
                >
                    {isUploading ? (
                        <>
                            <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                            </svg>
                            Uploading…
                        </>
                    ) : (
                        <>📤 Upload</>
                    )}
                    <input
                        type="file"
                        accept=".pdf"
                        className="hidden"
                        disabled={isUploading}
                        onChange={handleFileSelect}
                    />
                </label>
            </div>

            {/* Drop hint */}
            {dragOver && (
                <p className="text-xs text-blue-400 text-center mt-3 animate-pulse">
                    Drop PDF here to upload
                </p>
            )}
        </motion.div>
    );
};
