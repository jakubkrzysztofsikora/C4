## Plan: Apply UI/UX Requirements
Scope: FeatureSet
Created: 2026-03-07
Status: Draft

### Overview
Systematically audit and update the C4 frontend to comply with all UI/UX requirements documented in `docs/ux-requirements.md`, `docs/mvp-user-requirements.md`, and `docs/high-lvl-technical-requirements.md`. The work spans design tokens, global styles, component polish, accessibility, microinteractions, responsive design, and progressive disclosure patterns across all pages.

### Success Criteria
- [ ] 60-30-10 colour rule applied consistently (60% background, 30% panel/secondary, 10% accent)
- [ ] All interactive elements have visible focus states (WCAG 2.1)
- [ ] Skeleton loaders present on every page with async data
- [ ] Command palette upgraded to a true modal palette with keyboard shortcuts
- [ ] All pages fully responsive at 768px and 480px breakpoints
- [ ] Microinteractions (hover ripples, transitions, button feedback) on all interactive elements
- [ ] Sticky header with navigation always visible during scroll
- [ ] High-contrast mode support via CSS custom properties
- [ ] Design tokens expanded with full spacing, radius, shadow, and motion scales
- [ ] Node tooltip upgraded with rich metrics display (request rate, latency, errors)
- [ ] GraphNode component uses design system tokens instead of hardcoded colours
- [ ] Typography hierarchy uses clear size/weight scale per UX requirements
- [ ] Theme toggle uses icon (sun/moon) instead of text label
- [ ] All forms have proper validation feedback with accessible error messages
- [ ] Export and sharing controls are clearly discoverable
- [ ] Progressive disclosure: advanced diagram filters collapse by default
- [ ] Keyboard shortcuts (1-4 for C4 levels, R for reset) are documented in UI

### Epic 1: Design System Foundation
Goal: Expand design tokens and global CSS to establish the complete visual vocabulary required by UX guidelines.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Expand design tokens with spacing, radius, shadow, motion, and typography scales | Infrastructure | web/shared | M | – | ⬚ |
| 1.2 | Add high-contrast mode CSS custom properties | Infrastructure | web/shared | S | 1.1 | ⬚ |
| 1.3 | Apply 60-30-10 colour rule and refine palette for both themes | Infrastructure | web/shared | M | 1.1 | ⬚ |
| 1.4 | Add global microinteraction utilities (button press, ripple, hover lift) | Infrastructure | web/shared | S | 1.1 | ⬚ |
| 1.5 | Expand responsive breakpoints (add 480px mobile) and improve existing 768px styles | Infrastructure | web/shared | S | 1.1 | ⬚ |

#### 1.1 – Expand Design Tokens
- **Files to modify**: `web/src/shared/theme/tokens.ts`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Unit tests: `TokensTests` – verify all token values are defined and consistent
  - Manual verification: visual inspection of token application
- **Acceptance criteria**:
  - tokens.ts exports spacing (xs through 3xl), radius (sm, md, lg, xl, 2xl), shadow (sm, md, lg, xl), motion (fast, normal, slow), and typography (size scale from xs to 4xl with corresponding weights)
  - CSS variables map to all token values
  - Existing components continue to render correctly

#### 1.2 – High-Contrast Mode
- **Files to modify**: `web/src/styles.css`, `web/src/shared/theme/ThemeProvider.tsx`
- **Test plan (TDD)**:
  - Unit tests: `ThemeProviderTests` – verify high-contrast mode toggles correctly
- **Acceptance criteria**:
  - `[data-theme="dark-hc"]` and `[data-theme="light-hc"]` CSS variable sets with increased contrast ratios
  - All text meets WCAG AAA contrast (7:1) in high-contrast modes
  - ThemeProvider supports cycling through modes or a separate toggle

#### 1.3 – 60-30-10 Colour Rule
- **Files to modify**: `web/src/styles.css`, `web/src/features/diagram/diagram.css`, `web/src/features/feedback/feedback.css`
- **Test plan (TDD)**:
  - Manual verification: ensure backgrounds use 60% primary bg, 30% panel/secondary, 10% accent
- **Acceptance criteria**:
  - Background areas (body, main content) use `--bg` (60%)
  - Panels, cards, sidebars use `--panel`/`--panel-2` (30%)
  - CTAs, active states, highlights use `--accent` (10%)
  - No stray hardcoded colours

#### 1.4 – Microinteraction Utilities
- **Files to modify**: `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: buttons show press scale, cards show hover lift
- **Acceptance criteria**:
  - `.btn` gets `active:scale(0.97)` transform
  - `.card` clickable variants get subtle `hover:translateY(-1px)` lift
  - Smooth transitions using `--motion-fast` timing
  - Focus-visible ring updated to use design token

#### 1.5 – Responsive Breakpoints
- **Files to modify**: `web/src/styles.css`, `web/src/features/diagram/diagram.css`, `web/src/features/feedback/feedback.css`
- **Test plan (TDD)**:
  - Manual verification: test at 480px, 768px, and 1320px viewports
- **Acceptance criteria**:
  - At 480px: single-column layouts, compact padding, full-width buttons
  - Auth card stacks vertically with reduced padding
  - Diagram sidebar becomes collapsible drawer on mobile
  - Feedback eval grid collapses to single column

### Epic 2: Layout and Navigation Polish
Goal: Make the header sticky, upgrade the command palette, improve nav discoverability and add keyboard shortcut hints.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Make app header sticky with scroll shadow | Feature | web/shared | S | 1.1 | ⬚ |
| 2.2 | Upgrade CommandPalette to modal with keyboard shortcut (Cmd+K) | Feature | web/shared | M | 1.1 | ⬚ |
| 2.3 | Replace theme toggle text button with sun/moon icon toggle | Feature | web/shared | S | 1.1 | ⬚ |
| 2.4 | Add keyboard shortcut hints overlay (? key) | Feature | web/shared | M | 2.2 | ⬚ |

#### 2.1 – Sticky Header
- **Files to modify**: `web/src/styles.css`, `web/src/shared/components/Layout.tsx`
- **Test plan (TDD)**:
  - Manual verification: header stays fixed at top during scroll, shadow appears on scroll
- **Acceptance criteria**:
  - Header is `position: sticky; top: 0; z-index: 100`
  - A subtle bottom shadow appears when user has scrolled (`backdrop-filter` or JS scroll detection)
  - Content does not jump when header becomes sticky

#### 2.2 – Command Palette Modal
- **Files to modify**: `web/src/shared/components/CommandPalette.tsx`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Unit tests: `CommandPaletteTests` – opens on Cmd+K, closes on Escape, filters items
- **Acceptance criteria**:
  - Cmd+K (or Ctrl+K) opens a centered modal overlay with search input
  - Typing filters available pages/actions (Dashboard, Diagram, Organizations, Subscriptions)
  - Arrow keys navigate results, Enter activates
  - Escape closes
  - A "⌘K" hint badge is visible in the header search area

#### 2.3 – Icon Theme Toggle
- **Files to modify**: `web/src/shared/components/Layout.tsx`
- **Test plan (TDD)**:
  - Manual verification: sun icon in dark mode, moon icon in light mode
- **Acceptance criteria**:
  - Uses `MdLightMode`/`MdDarkMode` icons from react-icons
  - Smooth rotate/fade transition between icons
  - Accessible aria-label ("Switch to light mode" / "Switch to dark mode")

#### 2.4 – Keyboard Shortcut Hints
- **Files to create**: `web/src/shared/components/KeyboardShortcutsDialog.tsx`
- **Files to modify**: `web/src/shared/components/Layout.tsx`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Unit tests: `KeyboardShortcutsDialogTests` – opens on ? key press, lists all shortcuts
- **Acceptance criteria**:
  - Pressing `?` opens a modal listing all keyboard shortcuts
  - Shortcuts include: 1-4 (C4 levels), R (reset), Cmd+K (command palette), ? (this dialog)
  - Modal is accessible with focus trap

### Epic 3: Component Visual Compliance
Goal: Polish all page components to match UX requirements for cards, buttons, forms, empty states, and loading patterns.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Refine card and button styles with depth, hover lift, and gradient refinements | Feature | web/shared | S | 1.4 | ⬚ |
| 3.2 | Improve form inputs with animated focus labels and validation states | Feature | web/shared | M | 1.1 | ⬚ |
| 3.3 | Enhance empty states with larger illustrations and CTAs | Feature | web/shared | S | 1.1 | ⬚ |
| 3.4 | Improve toast notifications with icons and progress bar auto-dismiss | Feature | web/shared | S | 1.1 | ⬚ |
| 3.5 | Add subtle fade-in animations to page transitions | Feature | web/shared | S | 1.4 | ⬚ |

#### 3.1 – Card and Button Refinements
- **Files to modify**: `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: cards have subtle depth, buttons have clear hover/active states
- **Acceptance criteria**:
  - Cards use `box-shadow` with `--shadow-md` for depth
  - Buttons have hover brightness shift plus the active scale from 1.4
  - btn-primary uses a more vibrant gradient with subtle glow on hover
  - All interactive cards get cursor pointer and hover lift

#### 3.2 – Form Input Enhancements
- **Files to modify**: `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: focus state shows animated border, error inputs show red border with icon
- **Acceptance criteria**:
  - Input focus transition is smooth with `--motion-normal` duration
  - Error state: red border + red shadow + error icon
  - Success state: green border for validated fields
  - Disabled state has reduced opacity and cursor not-allowed (already exists, verify)

#### 3.3 – Enhanced Empty States
- **Files to modify**: `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: empty states are visually prominent with clear CTA
- **Acceptance criteria**:
  - Larger icon size (64px instead of 48px)
  - Gradient or subtle background pattern behind the empty state
  - Primary CTA button below description text
  - Animate in with fade-in-up

#### 3.4 – Toast Notification Improvements
- **Files to modify**: `web/src/shared/components/Toast.tsx`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: toasts show icons, auto-dismiss progress bar animates
- **Acceptance criteria**:
  - Success toast shows checkmark icon, error shows X icon, info shows info icon
  - Auto-dismiss progress bar at bottom that counts down visually
  - Slide-out animation on dismiss

#### 3.5 – Page Transition Animations
- **Files to modify**: `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: sections fade in smoothly when route changes
- **Acceptance criteria**:
  - `.fade-in` animation triggers on route mount
  - Animation is subtle (200-300ms) and non-distracting
  - Stagger children elements where multiple cards appear (e.g., dashboard setup steps)

### Epic 4: Diagram Page UX Compliance
Goal: Upgrade the diagram page to meet all interactive diagram requirements including progressive disclosure, rich tooltips, and improved filter UX.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Implement progressive disclosure for diagram sidebar filters | Feature | web/features/diagram | M | 1.1 | ⬚ |
| 4.2 | Upgrade GraphNode to use design system tokens and proper styling | Feature | web/features/diagram | M | 1.1 | ⬚ |
| 4.3 | Upgrade NodeTooltip with rich metrics display | Feature | web/features/diagram | M | 4.2 | ⬚ |
| 4.4 | Improve diagram sidebar scrolling and section organization | Feature | web/features/diagram | S | 4.1 | ⬚ |
| 4.5 | Add visual keyboard shortcut badges in diagram controls | Feature | web/features/diagram | S | 2.4 | ⬚ |

#### 4.1 – Progressive Disclosure for Filters
- **Files to modify**: `web/src/features/diagram/DiagramPage.tsx`, `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Unit tests: collapsible sections expand/collapse, state persists
- **Acceptance criteria**:
  - Core filters (C4 Level, Environment, Search) always visible
  - Advanced filters (Resource Type, Technology, Domain, Risk, Tag, Drift, Infrastructure) in a collapsible "Advanced Filters" section
  - Overlay, Diff, and Timeline controls in separate collapsible sections
  - Export controls in a collapsible section
  - Collapsible sections animate open/close smoothly
  - Active filter count shown as badge on collapsed section headers

#### 4.2 – GraphNode Design System Compliance
- **Files to modify**: `web/src/features/diagram/components/GraphNode.tsx`
- **Test plan (TDD)**:
  - Unit tests: `GraphNodeTests` – renders correct CSS class based on health status
- **Acceptance criteria**:
  - Uses CSS classes from diagram.css instead of inline styles
  - Health colours use CSS variables (green/yellow/red/unknown badges)
  - Drift state uses `.badge.drift` styling
  - No hardcoded color strings
  - Accessible focus state on interactive elements

#### 4.3 – Rich NodeTooltip
- **Files to modify**: `web/src/features/diagram/components/NodeTooltip.tsx`, `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Unit tests: `NodeTooltipTests` – renders metrics when provided, fallback for no data
- **Acceptance criteria**:
  - Tooltip displays: name, type, health badge, request rate, error rate, p95 latency
  - Styled as a card with shadow and border-radius
  - Shows "No telemetry" message when metrics unavailable
  - Positioned to avoid viewport overflow

#### 4.4 – Sidebar Organization
- **Files to modify**: `web/src/features/diagram/DiagramPage.tsx`, `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Manual verification: sidebar scrolls independently, sections are logically grouped
- **Acceptance criteria**:
  - Sidebar has `overflow-y: auto` with custom scrollbar styling
  - Sections: Overview metrics → Core filters → Advanced filters → Timeline/Diff → Overlays → Export → Drift → Legend
  - Section headers have consistent styling
  - Active section indicators

#### 4.5 – Keyboard Shortcut Badges
- **Files to modify**: `web/src/features/diagram/DiagramPage.tsx`, `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Manual verification: badges visible next to relevant controls
- **Acceptance criteria**:
  - C4 Level selector shows 1/2/3/4 key badges
  - Reset button shows "R" badge
  - Badges styled as subtle `kbd` elements
  - Responsive: hidden on mobile

### Epic 5: Auth and Onboarding Page Polish
Goal: Bring the auth page and dashboard onboarding wizard to UX compliance with clear visual hierarchy and engaging design.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Polish auth page with enhanced logo, gradient background, and form transitions | Feature | web/features/auth | S | 1.1 | ⬚ |
| 5.2 | Enhance dashboard setup wizard with progress indicator and step transitions | Feature | web/features/dashboard | M | 1.1 | ⬚ |
| 5.3 | Add skeleton loaders where missing (subscription page, organization page) | Feature | web/features | S | 1.1 | ⬚ |

#### 5.1 – Auth Page Polish
- **Files to modify**: `web/src/features/auth/AuthPage.tsx`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: auth page is visually polished with smooth tab transitions
- **Acceptance criteria**:
  - Logo mark has subtle glow/shadow effect
  - Tab switching animates content (fade/slide)
  - Password strength indicator on register tab
  - Input focus animations match design token timing
  - Light mode background gradient refined

#### 5.2 – Dashboard Setup Wizard Enhancement
- **Files to modify**: `web/src/features/dashboard/DashboardPage.tsx`, `web/src/styles.css`
- **Test plan (TDD)**:
  - Manual verification: progress bar shows completion, steps animate in sequence
- **Acceptance criteria**:
  - Progress bar at top showing 0/3, 1/3, 2/3, 3/3 completion
  - Completed steps have checkmark animation
  - Step transitions use staggered fade-in
  - Overall card has a more prominent visual treatment

#### 5.3 – Missing Skeleton Loaders
- **Files to modify**: `web/src/features/subscription/SubscriptionWizardPage.tsx`, `web/src/features/organization/OrganizationProjectsPage.tsx`
- **Test plan (TDD)**:
  - Manual verification: loading states show content-shaped skeletons instead of spinners
- **Acceptance criteria**:
  - Subscription page loading shows card-shaped skeleton with field placeholders
  - Organization page loading shows organization card skeleton + project list skeleton
  - Skeletons match the shape of the final rendered content

### Epic 6: Accessibility Compliance
Goal: Ensure all interactive elements, forms, and dynamic content meet WCAG 2.1 AA requirements.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Audit and fix focus management across all forms and modals | Feature | web | M | 2.2 | ⬚ |
| 6.2 | Add aria-labels, roles, and live regions to dynamic content | Feature | web | M | 1.1 | ⬚ |
| 6.3 | Ensure colour contrast ratios meet AA standards across all themes | Feature | web | S | 1.2 | ⬚ |

#### 6.1 – Focus Management
- **Files to modify**: `web/src/shared/components/CommandPalette.tsx`, `web/src/features/feedback/components/FeedbackPanel.tsx`, `web/src/features/feedback/components/NodeFeedbackDialog.tsx`
- **Test plan (TDD)**:
  - Manual verification: focus trapped in modals, returns to trigger on close
- **Acceptance criteria**:
  - All modal dialogs trap focus
  - Focus returns to triggering element on modal close
  - Tab order is logical on all pages
  - Skip-to-content link at top of page

#### 6.2 – ARIA and Live Regions
- **Files to modify**: multiple component files across features
- **Test plan (TDD)**:
  - Manual verification: screen reader announces dynamic content changes
- **Acceptance criteria**:
  - Discovery progress announcements via `aria-live="polite"`
  - Toast notifications have `role="alert"` (already exists, verify all paths)
  - Form errors linked via `aria-describedby`
  - Diagram filter changes announced
  - Loading states have `aria-busy="true"`

#### 6.3 – Colour Contrast Audit
- **Files to modify**: `web/src/styles.css`, `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Automated check: all text/background combinations meet 4.5:1 ratio (AA)
- **Acceptance criteria**:
  - `--muted` text colour adjusted if contrast insufficient against `--bg`
  - Badge text colours verified against badge backgrounds
  - Light mode contrast verified for all secondary text
  - Form placeholder text meets 3:1 minimum

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Large CSS changes cause visual regressions on pages not actively being worked on | Medium | Medium | Test all pages visually after each epic; use browser dev tools responsive mode |
| R2 | New animations cause janky performance on lower-end devices | Low | Medium | Use CSS transforms/opacity only (GPU-accelerated); test with Chrome performance profiler |
| R3 | Command palette modal conflicts with existing keyboard shortcuts in diagram | Medium | Low | Ensure Cmd+K is only captured when not inside input elements; diagram shortcuts already check for this |
| R4 | High-contrast mode increases maintenance burden | Low | Low | Use CSS custom property overrides only; no separate component code |
| R5 | GraphNode/NodeTooltip changes break React Flow integration | Medium | High | Test with existing diagram data after changes; ensure prop interfaces remain stable |

### Critical Path
1.1 → 1.3 → 1.4 → 3.1 → 4.1 → 4.2 → 4.3 → 6.1 → 6.3

### Estimated Total Effort
- S tasks: 10 × ~30 min = ~5 h
- M tasks: 9 × ~2.5 h = ~22.5 h
- L tasks: 0 × ~6 h = ~0 h
- XL tasks: 0 × ~12 h = ~0 h
- **Total: ~27.5 hours**
