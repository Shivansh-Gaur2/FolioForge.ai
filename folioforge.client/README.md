    │       ├── AnimatedEducationSection.jsx # Education timeline
# FolioForge Client 🎨

> **Frontend Application - React 19 + Vite 7 + Tailwind CSS**

Modern, animated single-page application for the FolioForge portfolio builder. Features JWT authentication, multi-tenant support, dark/light theming, and rich portfolio rendering with particle effects and scroll animations.

---

## 📋 Responsibilities

| Responsibility | Description |
|----------------|-------------|
| **Authentication** | JWT-based login/register with auto-token injection |
| **Multi-Tenancy** | X-Tenant-Id header injection via Axios interceptors |
| **Portfolio Rendering** | Animated sections (Hero, Skills, Timeline, Projects, Contact) |
| **Theme Management** | Dark/light mode with system preference detection |
| **Route Protection** | `ProtectedRoute` and `GuestRoute` wrappers |
| **Error Handling** | Global ErrorBoundary + typed API errors |
| **Portfolio Customization** | Live-preview editor with color picker, font selector, layout, section manager, theme presets |
| **State Management** | Zustand store for portfolio customization with dirty-state tracking |

---

## 📂 Project Structure

```
folioforge.client/
├── public/                        # Static assets
├── src/
│   ├── api/
│   │   ├── client.js              # Axios instance with JWT + tenant interceptors
│   │   └── errors.js              # Typed error classes (ApiError, NetworkError, etc.)
│   ├── components/
│   │   ├── animations/
│   │   │   └── ScrollReveal.jsx   # Intersection Observer scroll animations
│   │   ├── layout/
│   │   │   └── FloatingNav.jsx    # Floating navigation bar
│   │   ├── ui/
│   │   │   ├── Badge.jsx          # Tag/badge component
│   │   │   ├── Card.jsx           # Card container component
│   │   │   ├── SmartContent.jsx   # Markdown/JSON content renderer
│   │   │   └── ThemeToggle.jsx    # Dark/light mode toggle
    │   ├── customization/
    │   │   ├── ColorPicker.jsx    # Color input with hex preview
    │   │   ├── CustomizationPanel.jsx # Full editor sidebar
    │   │   ├── FontSelector.jsx   # Font family dropdown
    │   │   ├── LayoutSelector.jsx # Single-column vs sidebar layout
    │   │   ├── SectionManager.jsx # Drag-order, hide/show, variant per section
    │   │   └── ThemeSelector.jsx  # Theme preset swatches
│   │   ├── AsyncStateHandler.jsx  # Loading/error/empty state wrapper
│   │   ├── ErrorBoundary.jsx      # React error boundary
│   │   └── ProtectedRoute.jsx     # Auth-guarded route wrapper
│   ├── config/
│   │   └── environment.js         # Centralized env var config
│   ├── context/
│   │   ├── AuthContext.jsx        # JWT auth state (login, register, logout)
│   │   └── ThemeContext.jsx       # Dark/light theme state
│   ├── features/
│   │   └── portfolio/
│   │       ├── ParticleHero.jsx           # tsParticles hero with name/summary
│   │       ├── AnimatedSkillsSection.jsx  # Animated skill badges
│   │       ├── AnimatedTimelineSection.jsx # Timeline with scroll animations
│   │       ├── AnimatedProjectsSection.jsx # Project cards with hover effects
│   │       ├── ContactSection.jsx          # Contact information section
│   │       ├── SkillsSection.jsx          # Static skills display
│   │       ├── TimelineSection.jsx        # Static timeline display
│   │       └── ProjectGrid.jsx           # Static project grid
│   ├── hooks/
│   │   └── usePortfolio.js        # Portfolio data fetching hook
│   ├── pages/
│   │   ├── LoginPage.jsx          # Login form
│   │   ├── RegisterPage.jsx       # Registration form (with tenant)
│   │   ├── DashboardPage.jsx      # User dashboard (portfolio management)
│   │   └── PortfolioPage.jsx      # Portfolio viewer (public)
    │   │   ├── PortfolioEditorPage.jsx # Split-screen live-preview customization editor
    │   ├── services/
    │   │   ├── authService.js         # Auth API service layer
    │   │   ├── customizationService.js # Customization GET/PUT API service
    │   │   └── portfolioService.js    # Portfolio API service layer
    │   ├── stores/
    │   │   └── useCustomizationStore.js # Zustand store for editor state
│   ├── App.jsx                    # Root component with routing
│   ├── App.css                    # Global styles
│   ├── index.css                  # Tailwind CSS imports
│   └── main.jsx                   # React DOM entry point
├── eslint.config.js               # ESLint 9 flat config
├── tailwind.config.js             # Tailwind CSS configuration
├── postcss.config.js              # PostCSS with Tailwind + Autoprefixer
├── vite.config.js                 # Vite build configuration
├── index.html                     # HTML entry point
└── package.json
```

---

## 🔐 Authentication Flow

### AuthContext

Provides authentication state across the app:

```jsx
const { user, isAuthenticated, loading, login, register, logout } = useAuth();
```

| Method | Description |
|--------|-------------|
| `login(email, password)` | POST `/api/auth/login` → stores `accessToken` + `refreshToken` + user in localStorage |
| `register(email, fullName, password, tenantIdentifier)` | POST `/api/auth/register` → stores tokens + user |
| `logout()` | Clears localStorage and resets state |
| `refreshToken()` | POST `/api/auth/refresh` → rotates token pair silently on 401 |

- `ff_token` — Short-lived access token (JWT, 15 min)
- `ff_refresh_token` — Long-lived refresh token (opaque, 7 days)
- `ff_user` — Serialized user object (userId, email, fullName, tenantId)

### Route Protection

```jsx
<Route path="/dashboard" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
<Route path="/login" element={<GuestRoute><LoginPage /></GuestRoute>} />
```

| Wrapper | Behavior |
|---------|----------|
| `ProtectedRoute` | Redirects to `/login` if not authenticated |
| `GuestRoute` | Redirects to `/dashboard` if already authenticated |

---

## 🌐 Routing

| Path | Component | Access | Description |
|------|-----------|--------|-------------|
| `/login` | `LoginPage` | Guest only | Email/password login |
| `/register` | `RegisterPage` | Guest only | User registration with tenant |
| `/dashboard` | `DashboardPage` | Protected | Portfolio management |
| `/portfolio/:id` | `PortfolioPage` | Public | View portfolio by ID |
| `/portfolio/:id/edit` | `PortfolioEditorPage` | Protected | Live-preview customization editor |
| `*` | `DefaultRedirect` | — | Redirects to `/dashboard` or `/login` |

---

## 🔌 API Client

Centralized Axios instance with automatic interceptors:

```javascript
const apiClient = axios.create({
    baseURL: config.api.baseUrl,
    timeout: config.api.timeout,
});
```

### Request Interceptors

| Interceptor | Header | Source |
|-------------|--------|--------|
| JWT Token | `Authorization: Bearer {token}` | `localStorage.getItem('ff_token')` |
| Tenant ID | `X-Tenant-Id: {identifier}` | `config.tenant.identifier` (env var) |

### Response Interceptors

| Behavior | Description |
|----------|-------------|
| Data unwrap | Returns `response.data` directly |
| 401 auto-clear | Removes `ff_token` and `ff_user` from localStorage |
| Error typing | Converts HTTP errors to typed `ApiError` instances |

---

## 🎨 Portfolio Sections

The portfolio page renders AI-generated sections with animations:

| Component | Section Type | Features |
|-----------|-------------|----------|
| `ParticleHero` | Hero/About | tsParticles background, animated text reveal |
| `AnimatedSkillsSection` | Skills | Staggered badge animations, category grouping |
| `AnimatedTimelineSection` | Timeline/Experience | Scroll-triggered timeline with motion |
| `AnimatedProjectsSection` | Projects | Hover-to-expand cards, tech stack badges |
| `ContactSection` | Contact | Contact information display |
| `AnimatedEducationSection` | Education | Education timeline with institution/degree display |

Section visibility, sort order, and variant are controlled per-user via the **Portfolio Editor** (`PortfolioEditorPage`). The `SectionManager` panel in the editor lets users reorder, show/hide, and pick a display variant for each section.

### Animation Stack

- **Framer Motion** — Page transitions, element animations, layout animations
- **tsParticles** — Interactive particle backgrounds in hero section
- **ScrollReveal** — Intersection Observer-based reveal on scroll
- **React Intersection Observer** — Viewport detection for lazy animations

---

## 🎨 Portfolio Customization Editor

The `PortfolioEditorPage` is a split-screen view: left panel for controls, right side for live preview. Backed by `useCustomizationStore` (Zustand).

```jsx
const { primaryColor, fontBody, layout, sections,
        isDirty, isSaving, saveCustomization } = useCustomizationStore();
```

| Panel Component | Purpose |
|-----------------|---------|
| `ThemeSelector` | Switch between preset themes (default, ocean, forest, …) |
| `ColorPicker` | Primary, secondary, background, text color inputs with hex preview |
| `FontSelector` | Heading and body font family dropdowns |
| `LayoutSelector` | Single-column vs sidebar layout toggle |
| `SectionManager` | Per-section: sort order, visible/hidden, display variant |

Changes update local store state instantly (live preview). `Save` calls `PUT /api/portfolios/{id}/customization`. `isDirty` tracks unsaved changes.

---

## 🌗 Theme System

`ThemeContext` provides dark/light mode with system preference detection:

```jsx
const { theme, toggleTheme } = useTheme();
```

- Persists preference in localStorage
- Detects system `prefers-color-scheme` on first visit
- Applies `dark` class to `<html>` for Tailwind `dark:` variants
- Toggle via `ThemeToggle` component

---

## ⚙️ Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_BASE_URL` | `https://localhost:7245/api` | Backend API URL |
| `VITE_API_TIMEOUT` | `15000` | Request timeout (ms) |
| `VITE_TENANT_ID` | `default` | Tenant identifier for X-Tenant-Id header |
| `VITE_ENABLE_ANALYTICS` | `false` | Enable analytics |
| `VITE_ENABLE_DEBUG_LOGGING` | `true` (dev) | API request/response logging |

### .env.development

```env
VITE_API_BASE_URL=http://localhost:5090/api
VITE_TENANT_ID=test-tenant
VITE_ENABLE_DEBUG_LOGGING=true
```

---

## 🧪 Development

```bash
# Navigate to client
cd folioforge.client

# Install dependencies
npm install

# Start dev server
npm run dev

# Lint
npm run lint

# Build for production
npm run build
```

**Dev Server:** http://localhost:5173

---

## 📦 Tech Stack

| Package | Version | Purpose |
|---------|---------|---------|
| `react` | 19.2.0 | UI framework |
| `react-dom` | 19.2.0 | DOM rendering |
| `react-router-dom` | 7.13.0 | Client-side routing |
| `axios` | 1.13.5 | HTTP client with interceptors |
| `framer-motion` | 12.34.0 | Animations and transitions |
| `@tsparticles/react` | 3.0.0 | Particle background effects |
| `@tsparticles/slim` | 3.9.1 | Lightweight particle engine |
| `react-intersection-observer` | 10.0.2 | Viewport detection |
| `tailwindcss` | 3.4.17 | Utility-first CSS framework |
| `vite` | 7.3.1 | Build tool with HMR |
| `zustand` | 5.0.11 | Portfolio editor state management |
| `lucide-react` | 0.575.0 | Icon system |

### Dev Dependencies

| Package | Purpose |
|---------|---------|
| `eslint` 9.39.1 | JavaScript linting |
| `eslint-plugin-react` | React-specific lint rules (JSX uses vars) |
| `eslint-plugin-react-hooks` | Hooks rules of hooks |
| `eslint-plugin-react-refresh` | Fast Refresh compatibility |
| `autoprefixer` | CSS vendor prefixing |
| `postcss` | CSS processing pipeline |

---

## 🔗 Related Documentation

- [FolioForge.Api README](../backend/src/FolioForge.Api/README.md) — Backend API
- [Root README](../README.md) — Project overview
