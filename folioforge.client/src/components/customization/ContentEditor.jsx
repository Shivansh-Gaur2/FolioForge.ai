import { useState } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { ChevronDown, Edit3 } from 'lucide-react';
import { useCustomizationStore } from '../../stores/useCustomizationStore';
import { BadgeEditor } from './BadgeEditor';
import { AboutEditor } from './AboutEditor';
import { TimelineEditor } from './TimelineEditor';
import { ProjectsEditor } from './ProjectsEditor';
import { EducationEditor } from './EducationEditor';

// ─── section registry ────────────────────────────────────────────────────────

/**
 * Maps a section type (lower-cased) to its display config and editor component.
 * Extend this object to add support for new section types without touching the
 * ContentEditor logic.
 */
const SECTION_EDITOR_MAP = {
    skills:    { label: 'Skills & Badges',  icon: '⚡', Editor: BadgeEditor,    hint: 'Add or remove skill badges.' },
    about:     { label: 'Bio / About',      icon: '👤', Editor: AboutEditor,    hint: 'Edit your bio text.'          },
    timeline:  { label: 'Work Experience',  icon: '💼', Editor: TimelineEditor, hint: 'Add, edit, or remove jobs.'   },
    projects:  { label: 'Projects',         icon: '🚀', Editor: ProjectsEditor, hint: 'Add, edit, or remove projects.' },
    education: { label: 'Education',        icon: '🎓', Editor: EducationEditor,hint: 'Add, edit, or remove degrees.' },
};

// ─── single accordion item ───────────────────────────────────────────────────

const SectionAccordion = ({ section }) => {
    const [isOpen, setIsOpen] = useState(false);
    const type = section.sectionType?.toLowerCase();
    const config = SECTION_EDITOR_MAP[type];

    if (!config) return null;
    const { label, icon, hint, Editor } = config;

    return (
        <div className="border border-white/10 rounded-xl overflow-hidden bg-white/[0.03]">
            {/* ── Header ─────────────────────────────────────────── */}
            <button
                onClick={() => setIsOpen((prev) => !prev)}
                className="w-full flex items-center gap-3 px-3 py-3 text-left
                           hover:bg-white/5 transition-colors group"
            >
                <span className="text-base flex-shrink-0" aria-hidden="true">{icon}</span>

                <div className="flex-1 min-w-0">
                    <p className="text-sm font-semibold text-white">{label}</p>
                    <p className="text-xs text-slate-500 truncate">{hint}</p>
                </div>

                <Edit3
                    size={13}
                    className="text-slate-600 group-hover:text-slate-400 transition-colors flex-shrink-0"
                />
                <ChevronDown
                    size={14}
                    className={`text-slate-500 transition-transform duration-200 flex-shrink-0
                               ${isOpen ? 'rotate-180' : ''}`}
                />
            </button>

            {/* ── Content (animated) ─────────────────────────────── */}
            <AnimatePresence initial={false}>
                {isOpen && (
                    <motion.div
                        key="editor"
                        initial={{ height: 0, opacity: 0 }}
                        animate={{ height: 'auto', opacity: 1 }}
                        exit={{ height: 0, opacity: 0 }}
                        transition={{ duration: 0.22 }}
                        className="overflow-hidden"
                    >
                        <div className="px-3 pb-3 pt-2 border-t border-white/10">
                            <Editor sectionId={section.id} content={section.content} />
                        </div>
                    </motion.div>
                )}
            </AnimatePresence>
        </div>
    );
};

// ─── public component ────────────────────────────────────────────────────────

/**
 * ContentEditor
 *
 * The "Content" tab inside `CustomizationPanel`.
 * Renders one collapsible accordion per editable section type.
 * Unrecognised section types are silently skipped — they remain
 * manageable via the "Sections" tab.
 */
export const ContentEditor = () => {
    const { sections } = useCustomizationStore();

    const editableSections = sections
        .filter((s) => s.sectionType?.toLowerCase() in SECTION_EDITOR_MAP)
        .sort((a, b) => a.sortOrder - b.sortOrder);

    if (editableSections.length === 0) {
        return (
            <div className="text-center py-10">
                <p className="text-3xl mb-3">📄</p>
                <p className="text-sm font-medium text-slate-400">No editable sections</p>
                <p className="text-xs text-slate-600 mt-1 max-w-xs mx-auto">
                    Upload a resume to generate portfolio sections, then return here to fine-tune them.
                </p>
            </div>
        );
    }

    return (
        <div className="space-y-3">
            <div>
                <h3 className="text-xs font-semibold text-slate-400 uppercase tracking-wider">
                    Content Editor
                </h3>
                <p className="text-xs text-slate-500 mt-1">
                    Click a section below to edit its text and badges.
                    Changes are saved when you click <strong className="text-slate-400">Save Changes</strong>.
                </p>
            </div>

            <div className="space-y-2">
                {editableSections.map((section) => (
                    <SectionAccordion key={section.id} section={section} />
                ))}
            </div>
        </div>
    );
};
