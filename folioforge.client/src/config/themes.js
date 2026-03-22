/**
 * Theme presets and section configuration for portfolio customization.
 */

export const THEME_PRESETS = [
  {
    id: 'default',
    name: 'Classic',
    description: 'Clean and professional look',
    colors: { primary: '#3B82F6', secondary: '#10B981', background: '#FFFFFF', text: '#1F2937' },
    fonts: { heading: 'Inter', body: 'Inter' },
    layout: 'single-column',
  },
  {
    id: 'midnight',
    name: 'Midnight',
    description: 'Sleek dark theme with purple accents',
    colors: { primary: '#8B5CF6', secondary: '#EC4899', background: '#0F172A', text: '#E2E8F0' },
    fonts: { heading: 'Space Grotesk', body: 'Inter' },
    layout: 'single-column',
  },
  {
    id: 'forest',
    name: 'Forest',
    description: 'Natural earthy tones',
    colors: { primary: '#059669', secondary: '#D97706', background: '#FEFCE8', text: '#1C1917' },
    fonts: { heading: 'Playfair Display', body: 'Source Sans Pro' },
    layout: 'single-column',
  },
  {
    id: 'ocean',
    name: 'Ocean',
    description: 'Cool blue tones inspired by the sea',
    colors: { primary: '#0EA5E9', secondary: '#06B6D4', background: '#F0F9FF', text: '#0C4A6E' },
    fonts: { heading: 'Poppins', body: 'Nunito' },
    layout: 'single-column',
  },
  {
    id: 'minimal',
    name: 'Minimal',
    description: 'Ultra-clean black and white',
    colors: { primary: '#000000', secondary: '#6B7280', background: '#FFFFFF', text: '#111827' },
    fonts: { heading: 'DM Sans', body: 'DM Sans' },
    layout: 'sidebar',
  },
  {
    id: 'sunset',
    name: 'Sunset',
    description: 'Warm gradient-inspired palette',
    colors: { primary: '#F59E0B', secondary: '#EF4444', background: '#FFFBEB', text: '#451A03' },
    fonts: { heading: 'Montserrat', body: 'Lato' },
    layout: 'single-column',
  },
];

export const SECTION_VARIANTS = {
  Hero: [
    { id: 'default',    name: 'Centered',   description: 'Name and title centered' },
    { id: 'split',      name: 'Split',      description: 'Text left, photo right' },
    { id: 'fullscreen', name: 'Fullscreen', description: 'Full viewport hero image' },
  ],
  About: [
    { id: 'default',      name: 'Standard',    description: 'Text with optional photo' },
    { id: 'storytelling', name: 'Story',       description: 'Narrative format' },
  ],
  Timeline: [
    { id: 'default',  name: 'Timeline',    description: 'Vertical timeline' },
    { id: 'card',     name: 'Cards',       description: 'Card-based layout' },
    { id: 'minimal',  name: 'Minimal',     description: 'Simple list format' },
  ],
  Projects: [
    { id: 'default',  name: 'Grid Cards',  description: '3-column card grid' },
    { id: 'showcase', name: 'Showcase',    description: 'Large featured project view' },
    { id: 'minimal',  name: 'List',        description: 'Compact list style' },
  ],
  Skills: [
    { id: 'default', name: 'Badges',        description: 'Skill badges / tags' },
    { id: 'bars',    name: 'Progress Bars', description: 'Horizontal progress bars' },
    { id: 'grouped', name: 'Grouped',      description: 'Skills grouped by category' },
  ],
  Education: [
    { id: 'default',  name: 'Standard', description: 'Simple education list' },
    { id: 'timeline', name: 'Timeline', description: 'Timeline format' },
  ],
  Contact: [
    { id: 'default', name: 'Form',       description: 'Contact form' },
    { id: 'links',   name: 'Links Only', description: 'Social links and email' },
  ],
  Markdown: [
    { id: 'default', name: 'Standard', description: 'Simple markdown text' },
    { id: 'card',    name: 'Card',     description: 'Card with background' },
  ],
};

export const AVAILABLE_FONTS = [
  'Inter',
  'Poppins',
  'Montserrat',
  'Playfair Display',
  'Space Grotesk',
  'DM Sans',
  'Nunito',
  'Lato',
  'Source Sans Pro',
  'Roboto',
  'Open Sans',
  'Raleway',
  'Ubuntu',
  'Merriweather',
  'Fira Code',
];
