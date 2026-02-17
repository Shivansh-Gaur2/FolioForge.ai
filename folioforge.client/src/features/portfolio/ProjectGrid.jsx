import { Card } from '../../components/ui/Card';
import { SmartContent } from '../../components/ui/SmartContent';

export const ProjectGrid = ({ content }) => {
    let projects = [];
    try {
        projects = JSON.parse(content).items || [];
    } catch (e) { return null; }

    return (
        <section className="mb-12">
            <h2 className="text-2xl font-bold text-slate-800 dark:text-slate-100 mb-6 flex items-center">
                <span className="w-2 h-8 bg-purple-600 rounded-full mr-3"></span>
                Featured Projects
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {projects.map((proj, idx) => (
                    <Card 
                        key={idx} 
                        title={proj.Name} 
                        subtitle={proj.TechStack}
                    >
                        {/* Handles both new Points array and legacy Description string */}
                        <SmartContent 
                            content={proj.Points || proj.Description} 
                            variant="diamond" 
                        />
                    </Card>
                ))}
            </div>
        </section>
    );
};