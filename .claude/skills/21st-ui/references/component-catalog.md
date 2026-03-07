# 21st.dev Component Catalog & SVGL Reference

## 21st.dev Registry URL Pattern

All components are accessible at:
```
https://21st.dev/r/{author}/{component-slug}
```

Install with:
```bash
npx shadcn@latest add "https://21st.dev/r/{author}/{component-slug}"
```

## shadcn Core Components (`shadcn` author)

These are the foundational shadcn/ui primitives. Most community components depend on these.

### Layout & Structure
- `accordion` — Collapsible content sections
- `card` — Container with header, content, footer
- `collapsible` — Show/hide content toggle
- `separator` — Visual divider (horizontal/vertical)
- `scroll-area` — Custom scrollbar container
- `sheet` — Slide-out panel (side drawer)
- `tabs` — Tabbed content panels
- `resizable` — Resizable panel groups

### Forms & Inputs
- `button` — Primary interactive element with variants (default, destructive, outline, secondary, ghost, link)
- `checkbox` — Boolean input
- `input` — Text input field
- `label` — Form label
- `radio-group` — Single-select from options
- `select` — Dropdown select
- `slider` — Range input
- `switch` — Toggle boolean
- `textarea` — Multi-line text input
- `form` — Form wrapper with react-hook-form integration
- `input-otp` — One-time password input
- `toggle` — Pressable on/off button
- `toggle-group` — Group of toggles

### Overlays & Modals
- `alert-dialog` — Confirmation dialog
- `dialog` — Modal dialog
- `drawer` — Bottom sheet / drawer
- `popover` — Floating content panel
- `tooltip` — Hover info popup
- `hover-card` — Rich hover content
- `dropdown-menu` — Context/dropdown menu
- `context-menu` — Right-click menu
- `menubar` — Top menu bar

### Navigation
- `breadcrumb` — Breadcrumb trail
- `command` — Command palette (⌘K)
- `navigation-menu` — Top nav with dropdowns
- `pagination` — Page navigation

### Data Display
- `avatar` — User avatar
- `badge` — Status/label badge
- `calendar` — Date picker calendar
- `data-table` — Full data table with sorting/filtering
- `progress` — Progress bar
- `skeleton` — Loading placeholder
- `table` — HTML table wrapper

### Feedback
- `alert` — Inline alert message
- `sonner` — Toast notifications (via sonner)
- `toast` — Toast notifications

### Typography
- `aspect-ratio` — Aspect ratio container
- `carousel` — Content carousel

## Magic UI Components (`magicui` author)

Animated, visually impressive components for landing pages and marketing sites.

### Animation & Effects
- `animated-beam` — Animated connecting beams between elements
- `blur-fade` — Blur-to-focus fade-in animation
- `border-beam` — Animated gradient border
- `cool-mode` — Confetti/particle effects on click
- `dot-pattern` — Animated dot background pattern
- `magic-card` — Card with spotlight hover effect
- `meteors` — Falling meteor animation
- `particles` — Floating particle background
- `retro-grid` — Perspective grid background
- `ripple` — Ripple animation effect
- `sparkles-text` — Sparkling text animation

### Layout Components
- `bento-grid` — Bento-style grid layout
- `dock` — macOS-style dock
- `marquee` — Infinite scrolling marquee
- `orbiting-circles` — Orbiting icon circles

### Interactive
- `globe` — 3D interactive globe
- `number-ticker` — Animated number counter
- `pulsating-button` — Button with pulse animation
- `rainbow-button` — Rainbow gradient button
- `shimmer-button` — Shimmer effect button
- `typing-animation` — Typewriter text effect
- `word-rotate` — Rotating word animation

## Origin UI Components (`originui` author)

Extended variants and enhanced versions of shadcn primitives.

## Browsing 21st.dev

The website at https://21st.dev allows browsing by:
- **Homepage**: Featured/trending components
- **Author profiles**: e.g., https://21st.dev/shadcn, https://21st.dev/magicui
- **Community**: https://21st.dev/community/components

## SVGL Categories & Usage

### API Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET https://api.svgl.app` | All logos |
| `GET https://api.svgl.app?search=react` | Search by title |
| `GET https://api.svgl.app?limit=20` | Limit results |
| `GET https://api.svgl.app/category/framework` | Filter by category |
| `GET https://api.svgl.app/svg/react.svg` | Raw SVG code |
| `GET https://api.svgl.app/categories` | List all categories |

### Response Format

```json
{
  "id": 34,
  "title": "React",
  "category": "Library",
  "route": "https://svgl.app/library/react.svg",
  "url": "https://react.dev/",
  "wordmark": "https://svgl.app/library/react-wordmark.svg"
}
```

Some logos have light/dark variants:
```json
{
  "route": {
    "light": "https://svgl.app/library/logo-light.svg",
    "dark": "https://svgl.app/library/logo-dark.svg"
  }
}
```

### Major Categories

- **AI** (55+): OpenAI, Anthropic, Hugging Face, Mistral, Perplexity, Gemini, etc.
- **Framework** (49+): Next.js, Nuxt, SvelteKit, Astro, Remix, etc.
- **Library** (76+): React, Vue, Angular, Svelte, jQuery, Three.js, etc.
- **Software** (217+): VS Code, Figma, Slack, Discord, Docker, etc.
- **Database** (21+): PostgreSQL, MongoDB, Redis, Supabase, Firebase, etc.
- **Design** (29+): Figma, Sketch, Adobe XD, Framer, etc.
- **Language** (36+): TypeScript, Python, Rust, Go, etc.
- **Payment** (8+): Stripe, PayPal, etc.
- **Hosting** (12+): Vercel, Netlify, AWS, Azure, etc.
- **Social** (27+): Twitter/X, GitHub, LinkedIn, etc.

### Using Logos in React Components

Direct SVG URL in img tag:
```tsx
<img src="https://svgl.app/library/react.svg" alt="React" className="h-6 w-6" />
```

Inline SVG (fetch the code via API):
```tsx
// Use the raw SVG endpoint: https://api.svgl.app/svg/react.svg
// Then embed as dangerouslySetInnerHTML or use an SVG component
```

With next/image:
```tsx
import Image from "next/image"
<Image src="https://svgl.app/library/react.svg" alt="React" width={24} height={24} />
```

## Namespaced Registry Configuration

Add to your project's `components.json` for convenient CLI access:

```json
{
  "$schema": "https://ui.shadcn.com/schema.json",
  "registries": {
    "@21st": "https://21st.dev/r/{name}",
    "@magic": "https://21st.dev/r/magicui/{name}",
    "@origin": "https://21st.dev/r/originui/{name}"
  }
}
```

Then install with short names:
```bash
npx shadcn@latest add @magic/bento-grid
npx shadcn@latest add @21st/shadcn/button
```
