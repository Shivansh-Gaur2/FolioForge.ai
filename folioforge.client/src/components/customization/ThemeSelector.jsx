import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { THEME_PRESETS } from '../../config/themes';
import { Check } from 'lucide-react';

/**
 * ThemeSelector
 * 
 * Grid of theme preset cards. Clicking one applies the full theme
 * (colors, fonts, layout) instantly for live preview.
 */
export const ThemeSelector = () => {
    const { themeName, applyTheme } = useCustomizationStore();

    return (
        <div className="space-y-3">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Choose a Theme
            </h3>
            <div className="grid grid-cols-2 gap-3">
                {THEME_PRESETS.map(theme => {
                    const isActive = themeName === theme.id;
                    return (
                        <button
                            key={theme.id}
                            onClick={() => applyTheme(theme.id)}
                            className={`relative rounded-xl p-3 border-2 transition-all text-left ${
                                isActive
                                    ? 'border-blue-500 ring-2 ring-blue-500/30 bg-blue-500/10'
                                    : 'border-white/10 hover:border-white/20 bg-white/5'
                            }`}
                        >
                            {isActive && (
                                <div className="absolute top-2 right-2 bg-blue-500 rounded-full p-0.5">
                                    <Check size={10} className="text-white" />
                                </div>
                            )}

                            {/* Color swatches */}
                            <div className="flex gap-1 mb-2">
                                {Object.values(theme.colors).map((color, i) => (
                                    <div
                                        key={i}
                                        className="w-4 h-4 rounded-full border border-white/20"
                                        style={{ backgroundColor: color }}
                                    />
                                ))}
                            </div>

                            <p className="text-sm font-medium text-white">{theme.name}</p>
                            <p className="text-xs text-slate-500 mt-0.5">{theme.description}</p>
                        </button>
                    );
                })}
            </div>
        </div>
    );
};
