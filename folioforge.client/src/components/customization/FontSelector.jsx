import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { AVAILABLE_FONTS } from '../../config/themes';

/**
 * FontSelector
 *
 * Dropdown-free visual font picker with live preview.
 * Loads Google Fonts dynamically to ensure real-time preview.
 * Clean dark-theme design with clear selection feedback.
 */
export const FontSelector = () => {
    const { fontHeading, fontBody, setFont } = useCustomizationStore();

    return (
        <div className="space-y-6">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Typography
            </h3>

            {/* Heading Font */}
            <div>
                <label className="block text-sm font-medium text-slate-300 mb-3">
                    Heading Font
                </label>
                <div className="grid grid-cols-2 gap-2 mb-3">
                    {AVAILABLE_FONTS.map(font => (
                        <button
                            key={`heading-${font}`}
                            onClick={() => setFont('fontHeading', font)}
                            className={`px-3 py-2.5 rounded-lg text-xs font-medium transition-all
                                       border-2 text-center truncate
                                       ${
                                       fontHeading === font
                                           ? 'border-blue-500 bg-blue-500/15 text-blue-300 shadow-lg shadow-blue-500/20'
                                           : 'border-white/10 bg-white/5 text-slate-400 hover:border-white/20 hover:bg-white/10'
                                       }`}
                            style={{ fontFamily: font }}
                            title={font}
                        >
                            {font}
                        </button>
                    ))}
                </div>
                <div
                    className="p-4 rounded-lg bg-white/[0.03] border border-white/10 text-white"
                    style={{ fontFamily: fontHeading }}
                >
                    <p className="text-lg font-bold">Heading Preview</p>
                    <p className="text-sm text-slate-400 mt-1">Bold navigation & section titles</p>
                </div>
            </div>

            {/* Body Font */}
            <div>
                <label className="block text-sm font-medium text-slate-300 mb-3">
                    Body Font
                </label>
                <div className="grid grid-cols-2 gap-2 mb-3">
                    {AVAILABLE_FONTS.map(font => (
                        <button
                            key={`body-${font}`}
                            onClick={() => setFont('fontBody', font)}
                            className={`px-3 py-2.5 rounded-lg text-xs font-medium transition-all
                                       border-2 text-center truncate
                                       ${
                                       fontBody === font
                                           ? 'border-emerald-500 bg-emerald-500/15 text-emerald-300 shadow-lg shadow-emerald-500/20'
                                           : 'border-white/10 bg-white/5 text-slate-400 hover:border-white/20 hover:bg-white/10'
                                       }`}
                            style={{ fontFamily: font }}
                            title={font}
                        >
                            {font}
                        </button>
                    ))}
                </div>
                <div
                    className="p-4 rounded-lg bg-white/[0.03] border border-white/10 text-slate-300 leading-relaxed"
                    style={{ fontFamily: fontBody }}
                >
                    <p>
                        This is how body text will appear in your portfolio.
                        The quick brown fox jumps over the lazy dog.
                    </p>
                    <p className="mt-2 text-xs text-slate-500">
                        Used for paragraphs, descriptions, and general content.
                    </p>
                </div>
            </div>
        </div>
    );
};
