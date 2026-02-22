import { useCustomizationStore } from '../../stores/useCustomizationStore';

const COLOR_FIELDS = [
    { key: 'primaryColor',    label: 'Primary',    desc: 'Buttons, links, accents' },
    { key: 'secondaryColor',  label: 'Secondary',  desc: 'Supporting elements' },
    { key: 'backgroundColor', label: 'Background', desc: 'Page background' },
    { key: 'textColor',       label: 'Text',       desc: 'Body text color' },
];

/**
 * ColorPicker
 * 
 * Four color inputs with hex text fields and a live preview swatch.
 */
export const ColorPicker = () => {
    const store = useCustomizationStore();

    return (
        <div className="space-y-4">
            <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                Color Scheme
            </h3>

            {COLOR_FIELDS.map(({ key, label, desc }) => (
                <div key={key} className="flex items-center justify-between">
                    <div>
                        <p className="text-sm font-medium text-white">{label}</p>
                        <p className="text-xs text-slate-500">{desc}</p>
                    </div>
                    <div className="flex items-center gap-2">
                        <input
                            type="color"
                            value={store[key]}
                            onChange={e => store.setColor(key, e.target.value)}
                            className="w-8 h-8 rounded-lg border border-white/20 cursor-pointer bg-transparent"
                        />
                        <input
                            type="text"
                            value={store[key]}
                            onChange={e => store.setColor(key, e.target.value)}
                            className="w-20 text-xs px-2 py-1 rounded-md font-mono
                                       bg-white/5 border border-white/10 text-white
                                       focus:border-blue-500 outline-none"
                        />
                    </div>
                </div>
            ))}

            {/* Live preview */}
            <div className="mt-4 rounded-xl overflow-hidden border border-white/10">
                <div className="p-4" style={{ backgroundColor: store.backgroundColor }}>
                    <h4
                        className="text-lg font-bold"
                        style={{ color: store.primaryColor, fontFamily: store.fontHeading }}
                    >
                        Preview Heading
                    </h4>
                    <p
                        className="text-sm mt-1"
                        style={{ color: store.textColor, fontFamily: store.fontBody }}
                    >
                        This is how body text looks with the selected colors.
                    </p>
                    <div className="flex gap-2 mt-3">
                        <button
                            className="px-3 py-1 rounded-md text-white text-xs"
                            style={{ backgroundColor: store.primaryColor }}
                        >
                            Primary
                        </button>
                        <button
                            className="px-3 py-1 rounded-md text-white text-xs"
                            style={{ backgroundColor: store.secondaryColor }}
                        >
                            Secondary
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
};
