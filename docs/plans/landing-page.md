## Plan: C4 Landing Page

Scope: FeatureSet
Created: 2026-03-07
Status: Draft

### Overview

Create a high-converting landing page for C4 Dynamic Architecture — a SaaS that auto-discovers Azure resources and generates interactive C4-model diagrams with live telemetry. The page will be a standalone static site within the same repo, deployed via GitHub Pages, and designed to drive sign-ups and communicate value to platform engineers, architects, and DevOps teams.

### Success Criteria

- [ ] Landing page is live on GitHub Pages at the repo's GitHub Pages URL
- [ ] Page scores 90+ on Lighthouse (Performance, Accessibility, Best Practices, SEO)
- [ ] Page is fully responsive (mobile, tablet, desktop)
- [ ] Page includes all key sections: hero, features, how-it-works, pricing, CTA, footer
- [ ] Dark/light theme support aligned with existing C4 design tokens
- [ ] GitHub Actions workflow deploys landing page automatically on push to main
- [ ] Page loads in under 2 seconds on 3G connection
- [ ] WCAG 2.1 AA compliant

### Epic 1: Project Scaffolding & Infrastructure

Goal: Set up the landing page project structure, build pipeline, and GitHub Pages deployment.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Scaffold landing page project with Vite + React + TypeScript | Infrastructure | landing-page | M | – | ⬚ |
| 1.2 | Create GitHub Actions workflow for GitHub Pages deployment | Infrastructure | .github | S | 1.1 | ⬚ |
| 1.3 | Configure design tokens and base styles from existing C4 theme | Infrastructure | landing-page | S | 1.1 | ⬚ |

#### 1.1 – Scaffold Landing Page Project

- **Files to create**: `landing-page/package.json`, `landing-page/tsconfig.json`, `landing-page/vite.config.ts`, `landing-page/index.html`, `landing-page/src/main.tsx`, `landing-page/src/App.tsx`, `landing-page/src/styles/global.css`
- **Files to modify**: none
- **Acceptance criteria**:
  - Standalone Vite + React + TypeScript project in `landing-page/` directory
  - Builds to `landing-page/dist/` as static HTML/CSS/JS
  - Uses Inter font consistent with main app
  - Base path configured for GitHub Pages (repo subpath)
  - TypeScript strict mode enabled, no `any`

#### 1.2 – GitHub Actions Workflow for GitHub Pages

- **Files to create**: `.github/workflows/deploy-landing-page.yml`
- **Files to modify**: none
- **Acceptance criteria**:
  - Triggers on push to `main` when `landing-page/**` files change
  - Builds the landing page with `npm run build`
  - Deploys to GitHub Pages using `actions/deploy-pages`
  - Uses environment protection for `github-pages`

#### 1.3 – Design Tokens & Base Styles

- **Files to create**: `landing-page/src/styles/tokens.css`, `landing-page/src/styles/reset.css`
- **Files to modify**: `landing-page/src/styles/global.css`
- **Acceptance criteria**:
  - CSS custom properties for all colors, spacing, typography from existing C4 tokens
  - Dark theme as default with light theme support via `data-theme` attribute
  - Color palette: backgrounds (#0b1020, #111a2e), accent blue (#6ea8fe), teal (#7ce0c3), text (#e8edf7)
  - Smooth theme transitions (0.2s ease)
  - Inter font loaded with proper fallbacks

### Epic 2: Core Landing Page Sections

Goal: Build all visual sections of the landing page with compelling copy and responsive layout.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Build Hero section with headline, subheadline, CTA, and visual | Feature | landing-page | M | 1.3 | ⬚ |
| 2.2 | Build Features section with feature cards grid | Feature | landing-page | M | 1.3 | ⬚ |
| 2.3 | Build How It Works section with step-by-step flow | Feature | landing-page | S | 1.3 | ⬚ |
| 2.4 | Build Pricing section with tier cards | Feature | landing-page | M | 1.3 | ⬚ |
| 2.5 | Build Navigation bar and Footer | Feature | landing-page | S | 1.3 | ⬚ |
| 2.6 | Build Social Proof / Trust section | Feature | landing-page | S | 1.3 | ⬚ |

#### 2.1 – Hero Section

- **Files to create**: `landing-page/src/sections/Hero.tsx`, `landing-page/src/sections/Hero.module.css`
- **Acceptance criteria**:
  - Bold headline: value proposition about live architecture visualization
  - Subheadline: 1-2 sentences expanding the value prop for Azure teams
  - Primary CTA button ("Get Started" / "Start Free Trial") + secondary CTA ("See Demo")
  - Hero visual: animated/illustrated architecture diagram or gradient mesh background
  - Responsive: stacked layout on mobile, side-by-side on desktop
  - Gradient background with radial accents matching C4 theme

#### 2.2 – Features Section

- **Files to create**: `landing-page/src/sections/Features.tsx`, `landing-page/src/sections/Features.module.css`
- **Acceptance criteria**:
  - 6 feature cards in a responsive grid (3×2 desktop, 2×3 tablet, 1×6 mobile)
  - Features: Auto-Discovery, C4 Diagrams, Live Telemetry, IaC Drift Detection, AI Analysis (STRIDE), Export (SVG/PDF)
  - Each card: icon, title, 1-2 sentence description
  - Subtle hover effects (border glow or lift)
  - Section heading with supporting text

#### 2.3 – How It Works Section

- **Files to create**: `landing-page/src/sections/HowItWorks.tsx`, `landing-page/src/sections/HowItWorks.module.css`
- **Acceptance criteria**:
  - 3-step flow: Connect → Discover → Visualize
  - Each step: number/icon, title, description
  - Visual connector between steps (line or arrow)
  - Clean, scannable layout
  - Emphasizes "minutes, not days" setup speed

#### 2.4 – Pricing Section

- **Files to create**: `landing-page/src/sections/Pricing.tsx`, `landing-page/src/sections/Pricing.module.css`
- **Acceptance criteria**:
  - 3 tiers: Free/Starter, Professional, Enterprise
  - Each card: tier name, price, feature list, CTA button
  - Highlighted/recommended tier (Professional) with accent border
  - Feature comparison with checkmarks
  - "Contact Sales" for Enterprise tier
  - Responsive: horizontal on desktop, stacked on mobile

#### 2.5 – Navigation Bar & Footer

- **Files to create**: `landing-page/src/components/Navbar.tsx`, `landing-page/src/components/Navbar.module.css`, `landing-page/src/components/Footer.tsx`, `landing-page/src/components/Footer.module.css`
- **Acceptance criteria**:
  - Sticky navbar with logo/wordmark, nav links (Features, How It Works, Pricing), CTA button
  - Mobile hamburger menu with smooth expand/collapse
  - Theme toggle (dark/light)
  - Footer: logo, nav links, social links, copyright, legal links
  - Smooth scroll to sections on nav click

#### 2.6 – Social Proof / Trust Section

- **Files to create**: `landing-page/src/sections/Trust.tsx`, `landing-page/src/sections/Trust.module.css`
- **Acceptance criteria**:
  - Section for "Trusted by" or "Built for" with target audience badges
  - Key stats (e.g., "Auto-discover 1000+ resources", "Setup in 5 minutes")
  - Integration badges: Azure, Terraform, Bicep, Application Insights
  - Clean, minimal layout with trust-building visual cues

### Epic 3: Interactivity & Polish

Goal: Add animations, theme switching, scroll effects, and final polish for a premium feel.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Add scroll animations and intersection observer effects | Feature | landing-page | M | 2.1–2.6 | ⬚ |
| 3.2 | Implement dark/light theme toggle | Feature | landing-page | S | 1.3, 2.5 | ⬚ |
| 3.3 | Add SEO meta tags, Open Graph, and favicon | Feature | landing-page | S | 1.1 | ⬚ |
| 3.4 | Accessibility audit and fixes | Feature | landing-page | M | 2.1–2.6 | ⬚ |
| 3.5 | Performance optimization (lazy loading, font optimization) | Feature | landing-page | S | 3.1 | ⬚ |

#### 3.1 – Scroll Animations

- **Files to create**: `landing-page/src/hooks/useIntersectionObserver.ts`
- **Files to modify**: All section components
- **Acceptance criteria**:
  - Fade-in-up animations as sections enter viewport
  - Staggered card animations in Features and Pricing
  - Smooth, performant CSS animations (no JS animation libraries)
  - Respects `prefers-reduced-motion` media query
  - No layout shift during animations

#### 3.2 – Theme Toggle

- **Files to create**: `landing-page/src/hooks/useTheme.ts`, `landing-page/src/components/ThemeToggle.tsx`
- **Acceptance criteria**:
  - Toggle button in navbar (sun/moon icon)
  - Persists preference in localStorage
  - Defaults to dark theme
  - System preference detection via `prefers-color-scheme`
  - Smooth transition between themes

#### 3.3 – SEO & Meta Tags

- **Files to modify**: `landing-page/index.html`
- **Files to create**: `landing-page/public/favicon.svg`
- **Acceptance criteria**:
  - Title: "C4 Dynamic Architecture — Live Azure Visualization"
  - Meta description optimized for search
  - Open Graph tags for social sharing (title, description, image)
  - Twitter card meta tags
  - Canonical URL
  - SVG favicon matching C4 branding

#### 3.4 – Accessibility Audit

- **Files to modify**: All components as needed
- **Acceptance criteria**:
  - All interactive elements have focus indicators
  - Color contrast ratios meet WCAG AA (4.5:1 for text, 3:1 for large text)
  - All images/icons have alt text or aria-labels
  - Keyboard navigation works for all interactions
  - Skip-to-content link
  - Semantic HTML throughout (nav, main, section, footer)
  - ARIA landmarks properly defined

#### 3.5 – Performance Optimization

- **Files to modify**: `landing-page/vite.config.ts`, various components
- **Acceptance criteria**:
  - Font display: swap for Inter
  - Preload critical assets
  - Images lazy-loaded below the fold
  - CSS minified and tree-shaken
  - Bundle size under 200KB gzipped
  - Lighthouse performance score 90+

### Risks

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | GitHub Pages base path causes broken asset URLs | Medium | High | Configure Vite `base` option correctly for repo subpath; test with `vite preview` |
| R2 | Landing page styling conflicts with main app if served from same domain | Low | Medium | Fully isolated project in `landing-page/` directory with scoped CSS modules |
| R3 | GitHub Pages deployment conflicts with existing deploy workflow | Low | Medium | Use separate workflow file triggered only on `landing-page/**` changes |
| R4 | No actual product screenshots/visuals available yet | High | Medium | Use abstract geometric/gradient visuals and architectural diagram illustrations; replace with real screenshots when available |
| R5 | Pricing tiers not finalized by business | Medium | Low | Use placeholder pricing with clear structure; easy to update copy later |

### Critical Path

1.1 → 1.3 → 2.1 → 2.2 → 2.5 → 3.1 → 3.4 → 3.5 → 1.2

### Estimated Total Effort

- S tasks: 6 × ~30 min = ~3 h
- M tasks: 6 × ~2.5 h = ~15 h
- L tasks: 0
- XL tasks: 0
- **Total: ~18 hours**
