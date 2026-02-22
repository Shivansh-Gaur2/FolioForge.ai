import { useState } from 'react';
import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { ThemeSelector } from './ThemeSelector';
import { ColorPicker } from './ColorPicker';
import { FontSelector } from './FontSelector';
import { LayoutSelector } from './LayoutSelector';
import { SectionManager } from './SectionManager';
import { Palette, Type, Layout, Layers, Sparkles, Save, RotateCcw } from 'lucide-react';

const TABS = [
    { id: 'themes',   label: 'Themes',   Icon: Sparkles },
    { id: 'colors',   label: 'Colors',   Icon: Palette },
    { id: 'fonts',    label: 'Fonts',    Icon: Type },
    { id: 'layout',   label: 'Layout',   Icon: Layout },
    { id: 'sections', label: 'Sections', Icon: Layers },
];

/**
 * CustomizationPanel
 * 
 * Left sidebar with tabs for theme, colors, fonts, layout, and sections.
 * Includes save/reset actions in the footer.
 */
export const CustomizationPanel = ({ portfolioId }) => {
    const [activeTab, setActiveTab] = useState('themes');
    const { isDirty, isSaving, saveCustomization, resetToDefaults } = useCustomizationStore();

    const handleSave = async () => {
        try {
            await saveCustomization(portfolioId);
        } catch {
            // error logged in store
        }
    };

    return (
        <div className="w-80 bg-slate-900 border-r border-white/10 h-full flex flex-col flex-shrink-0">
            {/* Header */}
            <div className="p-4 border-b border-white/10">
                <h2 className="text-lg font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">
                    Customize Portfolio
                </h2>
                <p className="text-sm text-slate-500 mt-1">Make it uniquely yours</p>
            </div>

            {/* Tabs */}
            <div className="flex border-b border-white/10">
                {TABS.map(({ id, label, Icon }) => (
                    <button
                        key={id}
                        onClick={() => setActiveTab(id)}
                        className={`flex-1 flex flex-col items-center gap-1 py-3 px-1 text-xs font-medium transition-colors ${
                            activeTab === id
                                ? 'text-blue-400 border-b-2 border-blue-400'
                                : 'text-slate-500 hover:text-slate-300'
                        }`}
                    >
                        <Icon size={16} />
                        {label}
                    </button>
                ))}
            </div>

            {/* Content */}
            <div className="flex-1 overflow-y-auto p-4">
                {activeTab === 'themes' && <ThemeSelector />}
                {activeTab === 'colors' && <ColorPicker />}
                {activeTab === 'fonts' && <FontSelector />}
                {activeTab === 'layout' && <LayoutSelector />}
                {activeTab === 'sections' && <SectionManager />}
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-white/10 space-y-2">
                <button
                    onClick={handleSave}
                    disabled={!isDirty || isSaving}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2.5
                               bg-gradient-to-r from-blue-500 to-purple-500
                               hover:from-blue-600 hover:to-purple-600
                               text-white text-sm font-semibold rounded-lg
                               disabled:opacity-40 disabled:cursor-not-allowed
                               transition-all shadow-lg shadow-blue-500/20"
                >
                    <Save size={16} />
                    {isSaving ? 'Saving…' : isDirty ? 'Save Changes' : 'Saved'}
                </button>
                <button
                    onClick={resetToDefaults}
                    className="w-full flex items-center justify-center gap-2 px-4 py-2
                               text-sm text-slate-400 border border-white/10 rounded-lg
                               hover:bg-white/5 transition-colors"
                >
                    <RotateCcw size={14} />
                    Reset to Defaults
                </button>
            </div>
        </div>
    );
};
