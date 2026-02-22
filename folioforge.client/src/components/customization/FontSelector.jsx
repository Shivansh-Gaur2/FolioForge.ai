import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { AVAILABLE_FONTS } from '../../config/themes';

/**
 * FontSelector
 * 
 * Dropdowns for heading and body font with live preview text.
 */
export const FontSelector = () => {
    const { fontHeading, fontBody, setFont } = useCustomizationStore();

    return (
        <div className="space-y-5">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Typography
            </h3>

            {/* Heading Font */}
            <div>
                <label className="block text-sm font-medium text-slate-300 mb-1.5">
                    Heading Font
                </label>
                <select
                    value={fontHeading}
                    onChange={e => setFont('fontHeading', e.target.value)}
                    className="w-full rounded-lg px-3 py-2 text-sm
                               bg-white/5 border border-white/10 text-white
                               focus:border-blue-500 outline-none"
                >
                    {AVAILABLE_FONTS.map(font => (
                        <option key={font} value={font} style={{ fontFamily: font }}>
                            {font}
                        </option>
                    ))}
                </select>
                <p
                    className="mt-2 text-xl font-bold text-white"
                    style={{ fontFamily: fontHeading }}
                >
                    Heading Preview
                </p>
            </div>

            {/* Body Font */}
            <div>
                <label className="block text-sm font-medium text-slate-300 mb-1.5">
                    Body Font
                </label>
                <select
                    value={fontBody}
                    onChange={e => setFont('fontBody', e.target.value)}
                    className="w-full rounded-lg px-3 py-2 text-sm
                               bg-white/5 border border-white/10 text-white
                               focus:border-blue-500 outline-none"
                >
                    {AVAILABLE_FONTS.map(font => (
                        <option key={font} value={font} style={{ fontFamily: font }}>
                            {font}
                        </option>
                    ))}
                </select>
                <p
                    className="mt-2 text-sm text-slate-300"
                    style={{ fontFamily: fontBody }}
                >
                    This is how the body text will appear in your portfolio.
                    The quick brown fox jumps over the lazy dog.
                </p>
            </div>
        </div>
    );
};
