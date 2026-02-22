import { create } from 'zustand';
import { THEME_PRESETS } from '../config/themes';
import { CustomizationService } from '../services/customizationService';

/**
 * Zustand store for portfolio customization state.
 * 
 * Manages theme presets, colors, fonts, layout, and sections
 * with live preview support and dirty-state tracking.
 */
export const useCustomizationStore = create((set, get) => ({
    // --- State ---
    portfolio: null,         // full portfolio data from API
    themeName: 'default',
    primaryColor: '#3B82F6',
    secondaryColor: '#10B981',
    backgroundColor: '#FFFFFF',
    textColor: '#1F2937',
    fontHeading: 'Inter',
    fontBody: 'Inter',
    layout: 'single-column',
    sections: [],            // array of { id, sectionType, sortOrder, isVisible, variant, content }
    isDirty: false,
    isSaving: false,
    isLoading: false,

    // --- Load from API ---
    loadCustomization: async (portfolioId) => {
        set({ isLoading: true });
        try {
            const data = await CustomizationService.get(portfolioId);
            set({
                portfolio: data,
                themeName: data.theme?.name || 'default',
                primaryColor: data.theme?.primaryColor || '#3B82F6',
                secondaryColor: data.theme?.secondaryColor || '#10B981',
                backgroundColor: data.theme?.backgroundColor || '#FFFFFF',
                textColor: data.theme?.textColor || '#1F2937',
                fontHeading: data.theme?.fontHeading || 'Inter',
                fontBody: data.theme?.fontBody || 'Inter',
                layout: data.theme?.layout || 'single-column',
                sections: (data.sections || [])
                    .map(s => ({
                        id: s.id,
                        sectionType: s.sectionType,
                        sortOrder: s.sortOrder,
                        isVisible: s.isVisible ?? true,
                        variant: s.variant || 'default',
                        content: s.content,
                    }))
                    .sort((a, b) => a.sortOrder - b.sortOrder),
                isDirty: false,
            });
        } catch (err) {
            console.error('Failed to load customization:', err);
        } finally {
            set({ isLoading: false });
        }
    },

    // --- Save to API ---
    saveCustomization: async (portfolioId) => {
        const state = get();
        set({ isSaving: true });
        try {
            await CustomizationService.save(portfolioId, {
                themeName: state.themeName,
                primaryColor: state.primaryColor,
                secondaryColor: state.secondaryColor,
                backgroundColor: state.backgroundColor,
                textColor: state.textColor,
                fontHeading: state.fontHeading,
                fontBody: state.fontBody,
                layout: state.layout,
                sections: state.sections.map(s => ({
                    sectionId: s.id,
                    sortOrder: s.sortOrder,
                    isVisible: s.isVisible,
                    variant: s.variant,
                })),
            });
            set({ isDirty: false });
        } catch (err) {
            console.error('Failed to save customization:', err);
            throw err;
        } finally {
            set({ isSaving: false });
        }
    },

    // --- Apply a theme preset ---
    applyTheme: (themeId) => {
        const theme = THEME_PRESETS.find(t => t.id === themeId);
        if (!theme) return;
        set({
            themeName: theme.id,
            primaryColor: theme.colors.primary,
            secondaryColor: theme.colors.secondary,
            backgroundColor: theme.colors.background,
            textColor: theme.colors.text,
            fontHeading: theme.fonts.heading,
            fontBody: theme.fonts.body,
            layout: theme.layout,
            isDirty: true,
        });
    },

    // --- Individual setters ---
    setColor: (key, value) => set({ [key]: value, isDirty: true }),
    setFont: (key, value) => set({ [key]: value, isDirty: true }),
    setLayout: (layout) => set({ layout, isDirty: true }),

    // --- Section management ---
    toggleSection: (sectionId) => {
        set(state => ({
            sections: state.sections.map(s =>
                s.id === sectionId ? { ...s, isVisible: !s.isVisible } : s
            ),
            isDirty: true,
        }));
    },

    reorderSections: (newSections) => {
        set({
            sections: newSections.map((s, i) => ({ ...s, sortOrder: i })),
            isDirty: true,
        });
    },

    setSectionVariant: (sectionId, variant) => {
        set(state => ({
            sections: state.sections.map(s =>
                s.id === sectionId ? { ...s, variant } : s
            ),
            isDirty: true,
        }));
    },

    // --- Reset ---
    resetToDefaults: () => {
        const defaultTheme = THEME_PRESETS[0];
        set({
            themeName: defaultTheme.id,
            primaryColor: defaultTheme.colors.primary,
            secondaryColor: defaultTheme.colors.secondary,
            backgroundColor: defaultTheme.colors.background,
            textColor: defaultTheme.colors.text,
            fontHeading: defaultTheme.fonts.heading,
            fontBody: defaultTheme.fonts.body,
            layout: defaultTheme.layout,
            sections: get().sections.map(s => ({ ...s, isVisible: true, variant: 'default' })),
            isDirty: true,
        });
    },
}));
