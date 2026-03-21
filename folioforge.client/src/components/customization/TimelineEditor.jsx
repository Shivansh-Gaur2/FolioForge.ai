import { useState, useMemo, useCallback } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { Plus, Trash2, ChevronDown, GripVertical } from 'lucide-react';
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

const EMPTY_ITEM = () => ({ Role: '', Company: '', Duration: '', Description: '' });

const FIELDS = [
    { key: 'Role',        label: 'Job Title / Role',  type: 'text',     placeholder: 'e.g. Senior Software Engineer' },
    { key: 'Company',     label: 'Company',           type: 'text',     placeholder: 'e.g. Acme Corp' },
    { key: 'Duration',    label: 'Duration',          type: 'text',     placeholder: 'e.g. Jan 2022 – Present' },
    { key: 'Description', label: 'Description / Highlights', type: 'textarea', placeholder: 'Key responsibilities and achievements…' },
];

// ─── sub-components ──────────────────────────────────────────────────────────

const ItemEditor = ({ item, index, isExpanded, onToggle, onUpdate, onRemove }) => (
    <div className="border border-white/10 rounded-xl overflow-hidden bg-white/[0.02]">
        {/* Header row */}
        <div className="flex items-center gap-2 px-3 py-2.5">
            <GripVertical size={13} className="text-slate-600 flex-shrink-0" />

            <button onClick={onToggle} className="flex-1 min-w-0 text-left">
                <p className="text-xs font-semibold text-white truncate">
                    {item.Role || '(Untitled Role)'}
                </p>
                <p className="text-xs text-slate-500 truncate">
                    {[item.Company, item.Duration].filter(Boolean).join(' · ') || 'Click to edit'}
                </p>
            </button>

            <button
                onClick={onToggle}
                aria-label={isExpanded ? 'Collapse' : 'Expand'}
                className="p-1 text-slate-500 hover:text-white transition-colors"
            >
                <ChevronDown
                    size={13}
                    className={`transition-transform duration-200 ${isExpanded ? 'rotate-180' : ''}`}
                />
            </button>

            <button
                onClick={() => onRemove(index)}
                aria-label="Remove experience"
                className="p-1 text-slate-600 hover:text-rose-400 transition-colors"
            >
                <Trash2 size={13} />
            </button>
        </div>

        {/* Expandable fields */}
        <AnimatePresence initial={false}>
            {isExpanded && (
                <motion.div
                    key="fields"
                    initial={{ height: 0, opacity: 0 }}
                    animate={{ height: 'auto', opacity: 1 }}
                    exit={{ height: 0, opacity: 0 }}
                    transition={{ duration: 0.2 }}
                    className="overflow-hidden"
                >
                    <div className="px-3 pb-3 pt-2 border-t border-white/10 space-y-2.5">
                        {FIELDS.map(({ key, label, type, placeholder }) => (
                            <div key={key}>
                                <label className="block text-xs font-medium text-slate-400 mb-1">
                                    {label}
                                </label>
                                {type === 'textarea' ? (
                                    <textarea
                                        value={item[key] || ''}
                                        onChange={(e) => onUpdate(index, { ...item, [key]: e.target.value })}
                                        rows={3}
                                        placeholder={placeholder}
                                        className="w-full bg-white/5 border border-white/10 rounded-lg
                                                   px-2.5 py-2 text-xs text-white placeholder:text-slate-600
                                                   focus:outline-none focus:border-blue-500/60
                                                   resize-y transition-colors"
                                    />
                                ) : (
                                    <input
                                        type="text"
                                        value={item[key] || ''}
                                        onChange={(e) => onUpdate(index, { ...item, [key]: e.target.value })}
                                        placeholder={placeholder}
                                        className="w-full bg-white/5 border border-white/10 rounded-lg
                                                   px-2.5 py-2 text-xs text-white placeholder:text-slate-600
                                                   focus:outline-none focus:border-blue-500/60 transition-colors"
                                    />
                                )}
                            </div>
                        ))}
                    </div>
                </motion.div>
            )}
        </AnimatePresence>
    </div>
);

// ─── public component ─────────────────────────────────────────────────────────

/**
 * TimelineEditor
 *
 * Manages an ordered list of work-experience items.
 * Content shape: `{ items: [{ Role, Company, Duration, Description }] }`.
 */
export const TimelineEditor = ({ sectionId, content }) => {
    const { updateSectionContent } = useCustomizationStore();
    const [expandedIndex, setExpandedIndex] = useState(null);

    const items = useMemo(() => parseItems(content), [content]);

    const commit = useCallback(
        (nextItems) =>
            updateSectionContent(sectionId, JSON.stringify({ items: nextItems })),
        [sectionId, updateSectionContent],
    );

    const onUpdate = useCallback(
        (index, updated) => {
            const next = [...items];
            next[index] = updated;
            commit(next);
        },
        [items, commit],
    );

    const onRemove = useCallback(
        (index) => {
            commit(items.filter((_, i) => i !== index));
            setExpandedIndex(null);
        },
        [items, commit],
    );

    const onAdd = useCallback(() => {
        const next = [...items, EMPTY_ITEM()];
        commit(next);
        setExpandedIndex(next.length - 1);
    }, [items, commit]);

    const toggleExpand = (index) =>
        setExpandedIndex((prev) => (prev === index ? null : index));

    return (
        <div className="space-y-2">
            {items.map((item, i) => (
                <ItemEditor
                    key={i}
                    item={item}
                    index={i}
                    isExpanded={expandedIndex === i}
                    onToggle={() => toggleExpand(i)}
                    onUpdate={onUpdate}
                    onRemove={onRemove}
                />
            ))}

            <button
                onClick={onAdd}
                className="w-full flex items-center justify-center gap-2 py-2.5 rounded-xl
                           border border-dashed border-white/20 text-xs text-slate-400
                           hover:border-blue-500/50 hover:text-blue-400 transition-colors"
            >
                <Plus size={13} />
                Add Experience
            </button>
        </div>
    );
};
