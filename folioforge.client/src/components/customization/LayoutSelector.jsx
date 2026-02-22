import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { Check } from 'lucide-react';

const LAYOUTS = [
    {
        id: 'single-column',
        name: 'Single Column',
        description: 'Content flows in one column, great for storytelling',
    },
    {
        id: 'two-column',
        name: 'Two Column',
        description: 'Smart grid — compact sections pair side-by-side, wide ones span full',
    },
    {
        id: 'sidebar',
        name: 'Sidebar',
        description: 'Fixed sidebar with scrolling main content',
    },
];

/**
 * LayoutSelector
 * 
 * Three layout options with mini visual previews.
 */
export const LayoutSelector = () => {
    const { layout, setLayout } = useCustomizationStore();

    return (
        <div className="space-y-3">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Page Layout
            </h3>

            <div className="space-y-3">
                {LAYOUTS.map(l => {
                    const isActive = layout === l.id;
                    return (
                        <button
                            key={l.id}
                            onClick={() => setLayout(l.id)}
                            className={`w-full rounded-xl p-4 border-2 text-left transition-all flex items-start gap-3 ${
                                isActive
                                    ? 'border-blue-500 bg-blue-500/10'
                                    : 'border-white/10 hover:border-white/20 bg-white/5'
                            }`}
                        >
                            {/* Mini layout preview */}
                            <div className="w-12 h-16 bg-slate-800 rounded border border-white/20 flex-shrink-0 overflow-hidden">
                                {l.id === 'single-column' && (
                                    <div className="flex flex-col gap-1 p-1.5">
                                        <div className="h-2 bg-blue-400 rounded" />
                                        <div className="h-2 bg-slate-600 rounded" />
                                        <div className="h-2 bg-slate-600 rounded" />
                                        <div className="h-2 bg-slate-600 rounded" />
                                    </div>
                                )}
                                {l.id === 'two-column' && (
                                    <div className="flex flex-col gap-1 p-1.5 h-full">
                                        <div className="h-2 bg-blue-400 rounded" />
                                        <div className="flex gap-1">
                                            <div className="flex-1 h-3 bg-slate-600 rounded" />
                                            <div className="flex-1 h-3 bg-slate-600 rounded" />
                                        </div>
                                        <div className="h-2 bg-slate-600 rounded" />
                                    </div>
                                )}
                                {l.id === 'sidebar' && (
                                    <div className="flex h-full">
                                        <div className="w-4 bg-blue-400" />
                                        <div className="flex-1 flex flex-col gap-1 p-1">
                                            <div className="h-2 bg-slate-600 rounded" />
                                            <div className="h-2 bg-slate-600 rounded" />
                                            <div className="h-2 bg-slate-600 rounded" />
                                        </div>
                                    </div>
                                )}
                            </div>

                            <div className="flex-1">
                                <div className="flex items-center justify-between">
                                    <p className="text-sm font-medium text-white">{l.name}</p>
                                    {isActive && <Check size={16} className="text-blue-400" />}
                                </div>
                                <p className="text-xs text-slate-500 mt-0.5">{l.description}</p>
                            </div>
                        </button>
                    );
                })}
            </div>
        </div>
    );
};
