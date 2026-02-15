import { Badge } from '../../components/ui/Badge';

export const SkillsSection = ({ content }) => {
    let skills = [];
    try {
        const parsed = JSON.parse(content);
        skills = parsed.items || [];
    } catch (e) {
        console.warn("Failed to parse skills JSON", e);
        return null;
    }

    if (skills.length === 0) return null;

    return (
        <section className="mb-12 animate-fade-in-up">
            <h2 className="text-2xl font-bold text-slate-800 mb-6 flex items-center">
                <span className="w-2 h-8 bg-blue-600 rounded-full mr-3"></span>
                Core Competencies
            </h2>
            <div className="flex flex-wrap gap-2">
                {skills.map((skill, idx) => (
                    <Badge key={idx} color={idx % 2 === 0 ? "blue" : "green"}>{skill}</Badge>
                ))}
            </div>
        </section>
    );
};