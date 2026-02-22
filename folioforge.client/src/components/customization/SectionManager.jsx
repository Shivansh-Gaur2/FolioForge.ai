import { useState } from 'react';
import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { SECTION_VARIANTS } from '../../config/themes';
import {
    GripVertical, Eye, EyeOff,
    ChevronDown, ChevronUp,
} from 'lucide-react';

/**
 * SectionManager
 * 
 * Reorder, show/hide, and choose display variants for each portfolio section.
 * Sections come from the actual portfolio data (populated by AI resume parsing).
 */
export const SectionManager = () => {
    const { sections, toggleSection, reorderSections, setSectionVariant } =
        useCustomizationStore();
    const [expandedId, setExpandedId] = useState(null);

    const sorted = [...sections].sort((a, b) => a.sortOrder - b.sortOrder);

    const move = (index, direction) => {
        const arr = [...sorted];
        const swapIdx = direction === 'up' ? index - 1 : index + 1;
        if (swapIdx < 0 || swapIdx >= arr.length) return;
        [arr[index], arr[swapIdx]] = [arr[swapIdx], arr[index]];
        reorderSections(arr);
    };

    if (sorted.length === 0) {
        return (
            <div className="text-center py-8">
                <p className="text-slate-500 text-sm">
                    No sections yet. Upload a resume to generate portfolio sections.
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-3">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Sections
            </h3>
            <p className="text-xs text-slate-500">
                Toggle visibility, reorder, and choose display styles.
            </p>

            <div className="space-y-2">
                {sorted.map((section, index) => {
                    const variants = SECTION_VARIANTS[section.sectionType] || [];
                    const isExpanded = expandedId === section.id;

                    return (
                        <div
                            key={section.id}
                            className={`border rounded-lg transition-all ${
                                section.isVisible
                                    ? 'border-white/10 bg-white/5'
                                    : 'border-white/5 bg-white/[0.02] opacity-50'
                            }`}
                        >
                            {/* Section header row */}
                            <div className="flex items-center gap-2 p-3">
                                <GripVertical size={14} className="text-slate-600 flex-shrink-0" />

                                {/* Reorder buttons */}
                                <div className="flex flex-col">
                                    <button
                                        onClick={() => move(index, 'up')}
                                        disabled={index === 0}
                                        className="text-slate-500 hover:text-white disabled:opacity-20 transition-colors"
                                    >
                                        <ChevronUp size={12} />
                                    </button>
                                    <button
                                        onClick={() => move(index, 'down')}
                                        disabled={index === sorted.length - 1}
                                        className="text-slate-500 hover:text-white disabled:opacity-20 transition-colors"
                                    >
                                        <ChevronDown size={12} />
                                    </button>
                                </div>

                                {/* Title & type */}
                                <div className="flex-1 min-w-0">
                                    <p className="text-sm font-medium text-white truncate">
                                        {section.sectionType}
                                    </p>
                                    <p className="text-xs text-slate-500">
                                        Variant: {section.variant}
                                    </p>
                                </div>

                                {/* Visibility toggle */}
                                <button
                                    onClick={() => toggleSection(section.id)}
                                    className="p-1.5 rounded hover:bg-white/10 transition-colors"
                                    title={section.isVisible ? 'Hide section' : 'Show section'}
                                >
                                    {section.isVisible ? (
                                        <Eye size={14} className="text-emerald-400" />
                                    ) : (
                                        <EyeOff size={14} className="text-slate-500" />
                                    )}
                                </button>

                                {/* Expand for variants */}
                                {variants.length > 0 && (
                                    <button
                                        onClick={() => setExpandedId(isExpanded ? null : section.id)}
                                        className="p-1.5 rounded hover:bg-white/10 transition-colors text-slate-500"
                                    >
                                        <ChevronDown
                                            size={14}
                                            className={`transition-transform ${isExpanded ? 'rotate-180' : ''}`}
                                        />
                                    </button>
                                )}
                            </div>

                            {/* Variant selector (expanded) */}
                            {isExpanded && variants.length > 0 && (
                                <div className="px-3 pb-3 border-t border-white/5 mt-1 pt-2">
                                    <p className="text-xs font-medium text-slate-400 mb-2">Display Style</p>
                                    <div className="space-y-1.5">
                                        {variants.map(variant => (
                                            <button
                                                key={variant.id}
                                                onClick={() => setSectionVariant(section.id, variant.id)}
                                                className={`w-full text-left px-3 py-2 rounded-lg border text-xs transition-all ${
                                                    section.variant === variant.id
                                                        ? 'border-blue-500 bg-blue-500/10 text-blue-300'
                                                        : 'border-white/10 hover:border-white/20 text-slate-400'
                                                }`}
                                            >
                                                <span className="font-medium text-white">{variant.name}</span>
                                                <span className="block text-slate-500 mt-0.5">{variant.description}</span>
                                            </button>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
};
