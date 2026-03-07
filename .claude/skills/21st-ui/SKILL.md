---
name: 21st-ui
description: Browse, inspect, and install UI components from 21st.dev — the largest open registry of shadcn/ui-based React Tailwind components — and search brand logos via SVGL. Use this skill whenever the user asks for React UI components, shadcn components, Tailwind components, design-engineer-quality blocks, hero sections, pricing tables, cards, forms, navbars, or any UI element for a React/Next.js project. Also trigger when the user asks for brand logos, SVG icons, or company logos for their UI. Trigger for phrases like "find me a component", "add a button", "install shadcn", "21st.dev", "bento grid", "magic ui", "get me a logo for Stripe", "SVG logo", or any request involving browsing or installing community UI components. This skill does NOT require any API key — it uses the public 21st.dev registry and SVGL API directly.
---

# 21st.dev UI Components & SVGL Logos

This skill helps you browse, inspect, and install React UI components from [21st.dev](https://21st.dev) and search brand logos from [SVGL](https://svgl.app). No API keys required.

## When to use this skill

- User wants a React/Next.js UI component (button, card, dialog, table, hero, pricing, etc.)
- User asks to browse or search 21st.dev or shadcn community components
- User wants to install a component via the shadcn CLI
- User needs a brand/company SVG logo for their project
- User is building a shadcn/ui-based project and needs community components

## Core Concepts

### 21st.dev Registry

21st.dev is an open-source community registry — "npm for design engineers." It hosts shadcn/ui-compatible React components that install directly into projects via the shadcn CLI. Components come from multiple authors/registries.

**Registry endpoint** (public, no auth):
```
https://21st.dev/r/{author}/{component-slug}
```

Returns shadcn-compatible JSON with: component name, type, full source code, npm dependencies, registry dependencies, CSS variables, and Tailwind config extensions.

**Install command:**
```bash
npx shadcn@latest add "https://21st.dev/r/{author}/{component-slug}"
```

This automatically: creates component files, installs npm dependencies, extends Tailwind config, and sets up CSS variables.

### Known Authors/Registries on 21st.dev

These are the most popular component sources. Use `scripts/fetch_component.py` to inspect any of them:

| Author | Focus | Example components |
|--------|-------|--------------------|
| `shadcn` | Core primitives | accordion, alert, badge, button, calendar, card, checkbox, collapsible, combobox, command, dialog, dropdown-menu, form, hover-card, input, label, menubar, navigation-menu, popover, progress, radio-group, scroll-area, select, separator, sheet, skeleton, slider, switch, table, tabs, textarea, toast, toggle, tooltip |
| `magicui` | Animated components | animated-beam, bento-grid, blur-fade, border-beam, dock, globe, marquee, number-ticker, orbiting-circles, particles, pulsating-button, rainbow-button, retro-grid, ripple, shimmer-button, sparkles-text, typing-animation |
| `originui` | Extended shadcn variants | Various enhanced inputs, selects, and form controls |

### SVGL Logo Library

[SVGL](https://svgl.app) provides 500+ SVG brand logos with light/dark variants. The API is public and free.

**Base URL:** `https://api.svgl.app`

Key endpoints:
- Search: `https://api.svgl.app?search={query}`
- By category: `https://api.svgl.app/category/{category}`
- Get SVG code: `https://api.svgl.app/svg/{filename}`
- Categories list: `https://api.svgl.app/categories`

Categories include: AI, Browser, CMS, Community, Crypto, Database, Design, Devtool, Framework, Hosting, Language, Library, Payment, Social, Software, and many more.

## Workflow

### 1. Understand the request

Determine what the user needs:
- A specific component type (e.g., "a pricing table", "an accordion")
- A component from a specific author (e.g., "magicui bento grid")
- A logo/brand asset (e.g., "Stripe logo in SVG")
- General browsing ("show me what's available")

### 2. Fetch and inspect components

Use `scripts/fetch_component.py` to pull component metadata from the registry:

```bash
python3 /path/to/skill/scripts/fetch_component.py inspect shadcn button
python3 /path/to/skill/scripts/fetch_component.py inspect magicui bento-grid
```

This returns: component name, dependencies, registry dependencies, CSS vars, file paths, and a code preview.

To get the full source code:

```bash
python3 /path/to/skill/scripts/fetch_component.py code shadcn button
```

### 3. Search SVGL logos

```bash
python3 /path/to/skill/scripts/fetch_component.py logo react
python3 /path/to/skill/scripts/fetch_component.py logo stripe
python3 /path/to/skill/scripts/fetch_component.py logo-categories
```

### 4. Install components

For Claude Code (filesystem access to a real project):
```bash
cd /path/to/project
npx shadcn@latest add "https://21st.dev/r/{author}/{component}"
```

For Claude.ai (no project filesystem): provide the install command and show the component source code so the user can copy it or use it as a reference/artifact.

### 5. Integrate into the user's codebase

After installation, help the user:
- Import the component in their page/layout
- Configure any required props
- Add demo/example usage
- Install any missing shadcn primitives that the component depends on

## Namespaced Registry Setup (Optional)

If the user wants to set up 21st.dev as a named registry in their `components.json`:

```json
{
  "registries": {
    "@21st": "https://21st.dev/r/{name}",
    "@magicui": "https://21st.dev/r/magicui/{name}",
    "@shadcn": "https://21st.dev/r/shadcn/{name}"
  }
}
```

Then install with: `npx shadcn@latest add @magicui/bento-grid`

## Important Notes

- Components follow the shadcn/ui pattern: they are NOT npm packages — source code is copied into your project under `components/ui/`
- All components use Tailwind CSS and support `cn()` utility for class merging
- Many components depend on other shadcn primitives (listed in `registryDependencies`). The CLI resolves these automatically
- Components support both light and dark themes via CSS variables from shadcn's theme system
- The 21st.dev registry endpoint returns JSON — it's the same format used by `npx shadcn add`
- SVGL logos are MIT-licensed and free to use in any project

## Troubleshooting

**"Component not found" from registry:**
- Check the author and slug — they're case-sensitive and use kebab-case
- Browse https://21st.dev to find the exact slug
- Use `web_fetch` to check: `https://21st.dev/r/{author}/{slug}`

**shadcn CLI not working:**
- Ensure the project has been initialized with `npx shadcn@latest init`
- Check that `components.json` exists in the project root
- Verify the project uses Tailwind CSS v3+ or v4

**Missing registry dependencies:**
- Run the install command for each dependency listed in the component's `registryDependencies` array
- Or let the CLI handle it — it usually resolves deps automatically

## Reference

For the full list of known components and categories, read `references/component-catalog.md`.
