import { useState, useMemo, useCallback } from 'react';
import { Check, RefreshCw } from 'lucide-react';
import { useCustomizationStore } from '../../stores/useCustomizationStore';

// ─── helpers ────────────────────────────────────────────────────────────────

const parseAbout = (content) => {
    try {
        return JSON.parse(content) ?? {};
    } catch {
        return {};
    }
};

// ─── component ───────────────────────────────────────────────────────────────

/**
 * AboutEditor
 *
 * Editable textarea for the "About / Bio" section whose content shape is:
 * `{ content: string, ...otherFields }`.
 *
 * All other fields in the parsed JSON object are preserved on save so that
 * AI-generated metadata is never accidentally blown away.
 */
export const AboutEditor = ({ sectionId, content }) => {
    const { updateSectionContent } = useCustomizationStore();

    const parsed = useMemo(() => parseAbout(content), [content]);
    const [text, setText] = useState(parsed.content ?? '');
    const [applied, setApplied] = useState(false);

    const handleApply = useCallback(() => {
        updateSectionContent(sectionId, JSON.stringify({ ...parsed, content: text }));
        setApplied(true);
        setTimeout(() => setApplied(false), 1800);
    }, [sectionId, text, parsed, updateSectionContent]);

    const handleReset = useCallback(() => {
        setText(parsed.content ?? '');
        setApplied(false);
    }, [parsed]);

    const isDirty = text !== (parsed.content ?? '');

    return (
        <div className="space-y-3">
            <textarea
                value={text}
                onChange={(e) => setText(e.target.value)}
                rows={6}
                placeholder="Write your bio here — let visitors know who you are and what you do…"
                className="w-full bg-white/5 border border-white/10 rounded-lg px-3 py-2.5
                           text-sm text-white placeholder:text-slate-500
                           focus:outline-none focus:border-blue-500/60
                           resize-y min-h-[5rem] leading-relaxed transition-colors"
            />

            <div className="flex items-center justify-between">
                <span className="text-xs text-slate-600">
                    {text.length} character{text.length !== 1 ? 's' : ''}
                </span>

                <div className="flex items-center gap-2">
                    {isDirty && (
                        <button
                            onClick={handleReset}
                            aria-label="Discard changes"
                            className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg text-xs
                                       text-slate-400 hover:text-white border border-white/10
                                       hover:bg-white/5 transition-colors"
                        >
                            <RefreshCw size={11} />
                            Reset
                        </button>
                    )}

                    <button
                        onClick={handleApply}
                        disabled={!isDirty && !applied}
                        className={`flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium
                                   border transition-all
                                   ${applied
                                    ? 'bg-emerald-500/20 border-emerald-500/30 text-emerald-400'
                                    : isDirty
                                        ? 'bg-blue-500/20 border-blue-500/30 text-blue-400 hover:bg-blue-500/30'
                                        : 'opacity-40 cursor-not-allowed bg-white/5 border-white/10 text-slate-400'
                                   }`}
                    >
                        {applied ? <Check size={12} /> : null}
                        {applied ? 'Applied!' : 'Apply Changes'}
                    </button>
                </div>
            </div>

            {isDirty && !applied && (
                <p className="text-xs text-amber-500/70">
                    Unsaved — click Apply then Save to persist.
                </p>
            )}
        </div>
    );
};
