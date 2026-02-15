export const Card = ({ title, subtitle, children, footer }) => (
    <div className="bg-white p-6 rounded-xl border border-slate-200 shadow-sm hover:shadow-md transition-shadow duration-300">
        <div className="flex justify-between items-start mb-2">
            <h3 className="text-lg font-bold text-slate-900">{title}</h3>
            {subtitle && <span className="text-sm font-medium text-slate-500 bg-slate-100 px-2 py-1 rounded">{subtitle}</span>}
        </div>
        <div className="text-slate-600 leading-relaxed text-sm mb-4">
            {children}
        </div>
        {footer && <div className="mt-4 pt-4 border-t border-slate-100 text-xs text-slate-400">{footer}</div>}
    </div>
);