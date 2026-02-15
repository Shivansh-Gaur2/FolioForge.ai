/**
 * TimelineSection Component
 * Renders work experience/education as a visual timeline
 */
export const TimelineSection = ({ content }) => {
    let items = [];
    try {
        const parsed = JSON.parse(content);
        items = parsed.items || [];
    } catch (e) {
        console.warn("Failed to parse timeline JSON", e);
        return null;
    }

    if (items.length === 0) return null;

    return (
        <section className="mb-12 animate-fade-in-up">
            <h2 className="text-2xl font-bold text-slate-800 mb-6 flex items-center">
                <span className="w-2 h-8 bg-purple-600 rounded-full mr-3"></span>
                Experience
            </h2>
            <div className="relative">
                {/* Timeline line */}
                <div className="absolute left-4 top-0 bottom-0 w-0.5 bg-slate-200"></div>
                
                <div className="space-y-8">
                    {items.map((item, idx) => (
                        <div key={idx} className="relative pl-12">
                            {/* Timeline dot */}
                            <div className="absolute left-2.5 top-1.5 w-3 h-3 bg-purple-600 rounded-full border-2 border-white shadow"></div>
                            
                            <div className="bg-slate-50 rounded-lg p-5 border border-slate-100 hover:shadow-md transition-shadow">
                                <h3 className="text-lg font-semibold text-slate-900">{item.Role}</h3>
                                <p className="text-purple-600 font-medium text-sm mb-2">{item.Company}</p>
                                <p className="text-slate-600 text-sm leading-relaxed">{item.Description}</p>
                            </div>
                        </div>
                    ))}
                </div>
            </div>
        </section>
    );
};
