import { useState, useMemo, useCallback } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Plus, X, Tag } from 'lucide-react';
import { useCustomizationStore } from '../../stores/useCustomizationStore';

// ─── helpers ────────────────────────────────────────────────────────────────

const parseItems = (content) => {
    try {
        const parsed = JSON.parse(content);
        return Array.isArray(parsed) ? parsed : (parsed?.items ?? []);
    } catch {
        return [];
    }
};

const serializeItems = (items) => JSON.stringify({ items });

// ─── component ───────────────────────────────────────────────────────────────

/**
 * BadgeEditor
 *
 * Renders an interactive badge chip-set for any section whose content is
 * `{ items: string[] }` (Skills, Tags, etc.).
 *
 * - Type a skill and press Enter or click ➕ to add.
 * - Click ✕ on a chip to remove it.
 * - Deduplicates on add; sorts alphabetically for a stable display.
 */
export const BadgeEditor = ({ sectionId, content }) => {
    const { updateSectionContent } = useCustomizationStore();
    const [inputValue, setInputValue] = useState('');
    const [errorMsg, setErrorMsg] = useState('');

    const items = useMemo(() => parseItems(content), [content]);

    const commit = useCallback(
        (nextItems) => updateSectionContent(sectionId, serializeItems(nextItems)),
        [sectionId, updateSectionContent],
    );

    const addBadge = useCallback(() => {
        const trimmed = inputValue.trim();
        if (!trimmed) return;

        if (items.some((i) => i.toLowerCase() === trimmed.toLowerCase())) {
            setErrorMsg(`"${trimmed}" already exists.`);
            setTimeout(() => setErrorMsg(''), 2000);
            return;
        }

        commit([...items, trimmed]);
        setInputValue('');
        setErrorMsg('');
    }, [inputValue, items, commit]);

    const removeBadge = useCallback(
        (badge) => commit(items.filter((i) => i !== badge)),
        [items, commit],
    );

    const onKeyDown = (e) => {
        if (e.key === 'Enter') {
            e.preventDefault();
            addBadge();
        }
    };

    return (
        <div className="space-y-3">
            {/* ── Add input ── */}
            <div className="flex gap-2">
                <input
                    type="text"
                    value={inputValue}
                    onChange={(e) => {
                        setInputValue(e.target.value);
                        setErrorMsg('');
                    }}
                    onKeyDown={onKeyDown}
                    placeholder="Add a skill (e.g. React, Docker)…"
                    className="flex-1 bg-white/5 border border-white/10 rounded-lg px-3 py-2
                               text-sm text-white placeholder:text-slate-500
                               focus:outline-none focus:border-blue-500/60 transition-colors"
                />
                <button
                    onClick={addBadge}
                    disabled={!inputValue.trim()}
                    aria-label="Add skill"
                    className="p-2 rounded-lg bg-blue-500/20 hover:bg-blue-500/30 text-blue-400
                               border border-blue-500/30 disabled:opacity-40 disabled:cursor-not-allowed
                               transition-colors flex-shrink-0"
                >
                    <Plus size={16} />
                </button>
            </div>

            {/* ── Validation error ── */}
            {errorMsg && (
                <p className="text-xs text-rose-400">{errorMsg}</p>
            )}

            {/* ── Badge chips ── */}
            {items.length > 0 ? (
                <motion.div layout className="flex flex-wrap gap-2">
                    <AnimatePresence initial={false}>
                        {items.map((badge) => (
                            <motion.span
                                key={badge}
                                layout
                                initial={{ opacity: 0, scale: 0.8 }}
                                animate={{ opacity: 1, scale: 1 }}
                                exit={{ opacity: 0, scale: 0.7 }}
                                transition={{ duration: 0.15 }}
                                className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full
                                           bg-white/10 border border-white/15 text-xs font-medium text-slate-200
                                           hover:bg-white/15 transition-colors"
                            >
                                <Tag size={9} className="text-blue-400 flex-shrink-0" />
                                {badge}
                                <button
                                    onClick={() => removeBadge(badge)}
                                    aria-label={`Remove ${badge}`}
                                    className="ml-0.5 text-slate-500 hover:text-rose-400 transition-colors"
                                >
                                    <X size={11} />
                                </button>
                            </motion.span>
                        ))}
                    </AnimatePresence>
                </motion.div>
            ) : (
                <p className="text-xs text-slate-500 text-center py-3 border border-dashed border-white/10 rounded-lg">
                    No skills yet — type one above and press Enter.
                </p>
            )}

            <p className="text-xs text-slate-600">
                {items.length} skill{items.length !== 1 ? 's' : ''} · Press Enter or ➕ to add
            </p>
        </div>
    );
};
