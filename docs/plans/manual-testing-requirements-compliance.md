# Plan: Manual Testing – Requirements Compliance

Scope: Testing
Created: 2026-02-28
Status: Draft

## Overview

Comprehensive manual test plan to verify the C4 app deployed at `c4.jakub.team` against all three requirement documents: `docs/mvp-user-requirements.md` (10 user-facing requirement sections), `docs/high-lvl-technical-requirements.md` (front-end and back-end technical requirements), and `docs/ux-requirements.md` (UX and brand guidelines). Each test case maps to a specific requirement clause with explicit pass/fail criteria.

## Success Criteria

- [ ] All P0 (Critical) test cases pass
- [ ] All P1 (High) test cases pass or have documented workarounds
- [ ] P2 (Medium) test cases are documented with gaps identified
- [ ] Zero console errors across all pages
- [ ] All C4 levels render correctly (Context, Container, Component)
- [ ] Export functionality works (SVG, PNG)
- [ ] Authentication flows work end-to-end
- [ ] Real-time updates via SignalR are functional

---

## Epic 1: Authentication & Authorization

**Goal:** Verify all auth flows match Requirement §9 (FE Auth & Authorization) and MVP §3 (Ease of Setup)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 1.1 | Create new account with email/password | MVP §3, Tech FE §9 | P0 | ⬚ |
| 1.2 | Login with valid credentials | MVP §3, Tech FE §9 | P0 | ⬚ |
| 1.3 | Login with invalid credentials shows error | Tech FE §9 | P0 | ⬚ |
| 1.4 | Auth token persists across page reload | Tech FE §9 | P0 | ⬚ |
| 1.5 | Protected routes redirect to login when unauthenticated | Tech FE §9 | P0 | ⬚ |
| 1.6 | Sign out clears token and redirects to login | Tech FE §9 | P0 | ⬚ |
| 1.7 | Sign in → Sign out → Sign in round trip | Tech FE §9 | P1 | ⬚ |

#### 1.1 – Create new account
- **Steps:** Navigate to `/login` → Click "Create Account" → Fill email + password → Submit
- **Expected:** Account created, redirect to dashboard
- **Pass criteria:** HTTP 200 from API, dashboard loads with user context

#### 1.2 – Login with valid credentials
- **Steps:** Navigate to `/login` → Enter valid email/password → Submit
- **Expected:** Redirect to dashboard, JWT stored in localStorage/cookie
- **Pass criteria:** Dashboard renders, API calls include auth header

#### 1.3 – Login with invalid credentials
- **Steps:** Navigate to `/login` → Enter wrong password → Submit
- **Expected:** Error message displayed (e.g., "Invalid credentials")
- **Pass criteria:** No redirect, error visible, no console errors

#### 1.4 – Token persistence
- **Steps:** Login → Reload page (F5)
- **Expected:** User remains authenticated, dashboard loads
- **Pass criteria:** No redirect to login page

#### 1.5 – Protected route redirect
- **Steps:** Clear auth token → Navigate to `/organizations`
- **Expected:** Redirect to `/login`
- **Pass criteria:** Login page renders

#### 1.6 – Sign out
- **Steps:** Click sign out button
- **Expected:** Token cleared, redirect to login
- **Pass criteria:** localStorage/cookie cleared, login page renders

#### 1.7 – Full auth round trip
- **Steps:** Sign in → Navigate pages → Sign out → Sign in again
- **Expected:** All steps succeed, no stale state
- **Pass criteria:** Dashboard loads correctly after re-sign-in

---

## Epic 2: Organization & Project Management

**Goal:** Verify org/project CRUD matches Tech BE §6 (User and Organization Management)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 2.1 | View organizations page | Tech BE §6 | P0 | ⬚ |
| 2.2 | Create a new organization | Tech BE §6 | P0 | ⬚ |
| 2.3 | Create a new project under organization | Tech BE §6 | P0 | ⬚ |
| 2.4 | Navigate between org and project views | Tech BE §6 | P1 | ⬚ |
| 2.5 | Organization data persists after page reload | Tech BE §6 | P1 | ⬚ |

#### 2.1 – View organizations
- **Steps:** Navigate to `/organizations`
- **Expected:** List of organizations displayed (or empty state if none)
- **Pass criteria:** Page renders without errors, organization list visible

#### 2.2 – Create organization
- **Steps:** Click "Create Organization" → Fill name → Submit
- **Expected:** New org appears in list
- **Pass criteria:** API returns 201, org visible in list

#### 2.3 – Create project
- **Steps:** Select org → Click "Create Project" → Fill name → Submit
- **Expected:** New project appears under org
- **Pass criteria:** API returns 201, project visible

#### 2.4 – Navigate org/project
- **Steps:** Click through org → project → back to org list
- **Expected:** Smooth navigation, correct data on each page
- **Pass criteria:** No 404s, data matches

#### 2.5 – Persistence check
- **Steps:** Create org → Reload page
- **Expected:** Org still present
- **Pass criteria:** Data persists across reload

---

## Epic 3: Subscription & Azure Integration

**Goal:** Verify Azure integration per MVP §1 (Agent-less Discovery), MVP §4 (Azure/IaC Integration), Tech BE §1 (Telemetry Ingestion)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 3.1 | View subscriptions page | MVP §4, Tech BE §1 | P0 | ⬚ |
| 3.2 | Connect Azure subscription wizard renders | MVP §4 | P0 | ⬚ |
| 3.3 | Subscription wizard fields are present (Tenant ID, Client ID, Secret, Subscription ID) | MVP §4 | P0 | ⬚ |
| 3.4 | Git IaC configuration fields visible | MVP §4 | P1 | ⬚ |
| 3.5 | MCP server configuration section visible | MVP §4 | P2 | ⬚ |
| 3.6 | Submit subscription connection | MVP §4 | P0 | ⬚ |

#### 3.1 – View subscriptions page
- **Steps:** Navigate to `/subscriptions`
- **Expected:** Page renders with current subscription state
- **Pass criteria:** No errors, subscription list or empty state visible

#### 3.2 – Subscription wizard
- **Steps:** Click "Connect Subscription" or wizard trigger
- **Expected:** Multi-step wizard renders
- **Pass criteria:** First step visible with form fields

#### 3.3 – Azure credential fields
- **Steps:** Open subscription wizard
- **Expected:** Fields for Tenant ID, Client ID, Client Secret, Subscription ID
- **Pass criteria:** All four fields present and editable

#### 3.4 – Git IaC fields
- **Steps:** Navigate to IaC configuration step in wizard
- **Expected:** Fields for Git Repo URL and PAT token
- **Pass criteria:** Fields present (may be optional)

#### 3.5 – MCP server config
- **Steps:** Look for MCP server section in wizard or settings
- **Expected:** Section for configuring MCP server endpoints
- **Pass criteria:** Section visible (P2 — may not be implemented yet)

#### 3.6 – Submit connection
- **Steps:** Fill all required fields → Submit
- **Expected:** Connection saved, subscription appears in list
- **Pass criteria:** API returns success, subscription listed

---

## Epic 4: Resource Discovery

**Goal:** Verify auto-discovery per MVP §1 (Agent-less Discovery), Tech BE §3 (Graph Modeling Engine)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 4.1 | Dashboard shows discovery trigger | MVP §1 | P0 | ⬚ |
| 4.2 | Trigger discovery and verify resource count | MVP §1, Tech BE §3 | P0 | ⬚ |
| 4.3 | Discovery finds resources across subscription | MVP §1 | P0 | ⬚ |
| 4.4 | Resources are classified into C4 levels | MVP §1, Tech BE §3 | P0 | ⬚ |
| 4.5 | Resource types are correctly identified | MVP §1 | P1 | ⬚ |
| 4.6 | Discovery status/progress indicator shown | MVP §3, UX §1.1 | P1 | ⬚ |
| 4.7 | Orphaned resources detected | MVP §1 | P2 | ⬚ |

#### 4.1 – Dashboard discovery trigger
- **Steps:** Navigate to dashboard
- **Expected:** "Discover" or "Sync" button visible for connected subscription
- **Pass criteria:** Button present and clickable

#### 4.2 – Trigger discovery
- **Steps:** Click discover → Wait for completion
- **Expected:** Resource count displayed (e.g., "1000 resources found")
- **Pass criteria:** Non-zero resource count, no errors

#### 4.3 – Cross-subscription resources
- **Steps:** After discovery, check resource list
- **Expected:** Resources from all connected subscriptions appear
- **Pass criteria:** Resource count matches expected scope

#### 4.4 – C4 level classification
- **Steps:** After discovery, switch between Context/Container/Component in diagram
- **Expected:** Different node counts at each level
- **Pass criteria:** Context < Container < Component (proper hierarchy)

#### 4.5 – Resource type identification
- **Steps:** Inspect nodes in diagram
- **Expected:** Resource types shown (e.g., App Service, SQL Database, Virtual Network)
- **Pass criteria:** Types match Azure resource types

#### 4.6 – Discovery progress
- **Steps:** Trigger discovery, observe UI during process
- **Expected:** Loading indicator or progress message
- **Pass criteria:** Visual feedback during async operation

#### 4.7 – Orphaned resources
- **Steps:** Check if discovery flags resources without dependencies
- **Expected:** Orphaned/isolated resources identifiable
- **Pass criteria:** Some resources appear without connections (edge count = 0)

---

## Epic 5: Interactive C4 Diagrams

**Goal:** Verify diagram rendering per MVP §2 (Interactive Visualization), Tech FE §1 (High-fidelity Diagrams), Tech FE §7 (Customizable Views)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 5.1 | Diagram page loads with React Flow canvas | Tech FE §1 | P0 | ⬚ |
| 5.2 | C4 Context level renders correctly | Tech FE §1, MVP §2 | P0 | ⬚ |
| 5.3 | C4 Container level renders correctly | Tech FE §1, MVP §2 | P0 | ⬚ |
| 5.4 | C4 Component level renders correctly | Tech FE §1, MVP §2 | P0 | ⬚ |
| 5.5 | Switch between C4 levels | Tech FE §1 | P0 | ⬚ |
| 5.6 | Zoom in/out controls work | Tech FE §1, MVP §2 | P0 | ⬚ |
| 5.7 | Pan canvas by dragging | Tech FE §1 | P1 | ⬚ |
| 5.8 | Fit view button centers diagram | Tech FE §1 | P1 | ⬚ |
| 5.9 | Minimap visible and functional | Tech FE §1 | P1 | ⬚ |
| 5.10 | Node display names show instance + type format | Tech FE §1 | P1 | ⬚ |
| 5.11 | Nodes show correct count at each level | Tech BE §3 | P1 | ⬚ |
| 5.12 | Edges render between connected nodes | Tech BE §3 | P1 | ⬚ |
| 5.13 | Toggle interactivity control works | Tech FE §1 | P2 | ⬚ |

#### 5.1 – Diagram loads
- **Steps:** Navigate to diagram page
- **Expected:** React Flow canvas renders with nodes and edges
- **Pass criteria:** Canvas visible, nodes displayed, no JS errors

#### 5.2 – Context level
- **Steps:** Select "Context" from C4 level dropdown
- **Expected:** High-level system context nodes (few nodes: resource groups, vnets)
- **Pass criteria:** Small number of nodes (< 10 for typical subscription)

#### 5.3 – Container level
- **Steps:** Select "Container" from C4 level dropdown
- **Expected:** Application containers (hundreds of nodes)
- **Pass criteria:** Significantly more nodes than Context level

#### 5.4 – Component level
- **Steps:** Select "Component" from C4 level dropdown
- **Expected:** Individual components (e.g., deployment slots, databases)
- **Pass criteria:** Different set of nodes from Container

#### 5.5 – Level switching
- **Steps:** Toggle between Context → Container → Component → Context
- **Expected:** Nodes change at each level, no errors
- **Pass criteria:** Each level shows different node count

#### 5.6 – Zoom controls
- **Steps:** Click Zoom In, Zoom Out buttons
- **Expected:** Canvas zooms smoothly
- **Pass criteria:** Zoom level changes visually

#### 5.7 – Pan canvas
- **Steps:** Click and drag on empty canvas area
- **Expected:** Canvas pans in drag direction
- **Pass criteria:** Viewport moves

#### 5.8 – Fit view
- **Steps:** Zoom in/pan away → Click Fit View
- **Expected:** All nodes visible and centered
- **Pass criteria:** Entire diagram fits viewport

#### 5.9 – Minimap
- **Steps:** Look for minimap in corner of diagram
- **Expected:** Small overview map of full diagram
- **Pass criteria:** Minimap renders, shows node positions

#### 5.10 – Node display names
- **Steps:** Hover/inspect nodes in diagram
- **Expected:** Names in "instanceName (FriendlyName)" format
- **Pass criteria:** At least some nodes show dual-name format

#### 5.11 – Node counts per level
- **Steps:** Check node count in status bar at each C4 level
- **Expected:** Different counts: Context (2-5), Container (800+), Component (3-10)
- **Pass criteria:** Counts change per level

#### 5.12 – Edge rendering
- **Steps:** Check for edges (lines) between nodes
- **Expected:** Edges connect related nodes
- **Pass criteria:** Edge count > 0, edges visible

#### 5.13 – Toggle interactivity
- **Steps:** Click Toggle Interactivity button
- **Expected:** Nodes become locked/unlocked for dragging
- **Pass criteria:** Interaction behavior changes

---

## Epic 6: Health Monitoring & Traffic Overlays

**Goal:** Verify health visualization per MVP §2 (Traffic Overlays), MVP §6 (Health Monitoring), Tech FE §2 (Live Traffic Overlays)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 6.1 | Nodes show health badges (green/yellow/red) | MVP §2, MVP §6, Tech FE §2 | P0 | ⬚ |
| 6.2 | Legend shows health color meanings | MVP §2, Tech FE §2 | P1 | ⬚ |
| 6.3 | Legend shows drift indicator | MVP §4 | P1 | ⬚ |
| 6.4 | Health defaults to green when no App Insights configured | Tech FE §2, Tech BE §1 | P1 | ⬚ |
| 6.5 | Drift badges appear on nodes with IaC drift | MVP §4, Tech FE §5 | P2 | ⬚ |
| 6.6 | Edge colors reflect traffic health | MVP §2, Tech FE §2 | P2 | ⬚ |

#### 6.1 – Health badges
- **Steps:** View diagram with nodes
- **Expected:** Each node has a color-coded health indicator
- **Pass criteria:** Green, yellow, or red badges visible on nodes

#### 6.2 – Legend health colors
- **Steps:** Check legend/key on diagram page
- **Expected:** Legend explains green=healthy, yellow=warning, red=critical
- **Pass criteria:** All three colors documented in legend

#### 6.3 – Legend drift indicator
- **Steps:** Check legend
- **Expected:** Drift indicator explained in legend
- **Pass criteria:** Drift symbol/color present in legend

#### 6.4 – Default green health
- **Steps:** View nodes without App Insights API key configured
- **Expected:** All nodes show green (healthy by default)
- **Pass criteria:** No red/yellow unless App Insights data exists

#### 6.5 – Drift badges
- **Steps:** Check nodes that have IaC drift detected
- **Expected:** Drift icon/badge on drifted nodes
- **Pass criteria:** Badge visible (requires drift scan data)

#### 6.6 – Edge traffic colors
- **Steps:** Inspect edges between nodes
- **Expected:** Colors reflect traffic health/volume
- **Pass criteria:** Edges have color differentiation (may be MVP+1)

---

## Epic 7: Search, Filter & Customization

**Goal:** Verify filtering per MVP §2 (Drill-down and Filtering), Tech FE §7 (Customizable Views)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 7.1 | Search box visible on diagram page | MVP §2, Tech FE §7 | P0 | ⬚ |
| 7.2 | Search filters nodes by name | MVP §2, Tech FE §7 | P0 | ⬚ |
| 7.3 | Clear search restores all nodes | Tech FE §7 | P0 | ⬚ |
| 7.4 | Search updates node count display | Tech FE §7 | P1 | ⬚ |
| 7.5 | Filter by resource type | Tech FE §7 | P2 | ⬚ |
| 7.6 | Filter by environment/tag | Tech FE §7 | P2 | ⬚ |

#### 7.1 – Search box
- **Steps:** Navigate to diagram page, look for search input
- **Expected:** Search/filter input field visible
- **Pass criteria:** Input element present

#### 7.2 – Search filters
- **Steps:** Type a search term (e.g., "circit") → Observe diagram
- **Expected:** Only matching nodes remain visible
- **Pass criteria:** Node count decreases, filtered nodes relevant

#### 7.3 – Clear search
- **Steps:** Clear the search input
- **Expected:** All nodes restored
- **Pass criteria:** Node count returns to original

#### 7.4 – Count updates
- **Steps:** Search → Check node count in status bar
- **Expected:** Count reflects filtered nodes (e.g., "541 of 892")
- **Pass criteria:** Count changes with search

#### 7.5 – Resource type filter
- **Steps:** Look for resource type dropdown/filter
- **Expected:** Can filter by specific Azure resource types
- **Pass criteria:** Filter present and functional (P2 — may not exist yet)

#### 7.6 – Environment filter
- **Steps:** Look for environment or tag filter
- **Expected:** Can filter by environment labels or tags
- **Pass criteria:** Filter present (P2 — may not exist yet)

---

## Epic 8: Export & Sharing

**Goal:** Verify export per MVP §7 (Export and Embed Diagrams), Tech FE §8 (Export and Sharing)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 8.1 | SVG export button present | MVP §7, Tech FE §8 | P0 | ⬚ |
| 8.2 | SVG export triggers download | MVP §7, Tech FE §8 | P0 | ⬚ |
| 8.3 | PNG export button present | MVP §7, Tech FE §8 | P1 | ⬚ |
| 8.4 | PNG export triggers download | MVP §7, Tech FE §8 | P1 | ⬚ |
| 8.5 | PDF export available | MVP §7, Tech FE §8 | P2 | ⬚ |
| 8.6 | Exported diagram is readable | MVP §7 | P1 | ⬚ |

#### 8.1 – SVG export button
- **Steps:** Look for "Export SVG" button on diagram page
- **Expected:** Button visible and clickable
- **Pass criteria:** Button present

#### 8.2 – SVG download
- **Steps:** Click "Export SVG"
- **Expected:** SVG file downloads
- **Pass criteria:** File download triggered, valid SVG content

#### 8.3 – PNG export button
- **Steps:** Look for "Export PNG" button
- **Expected:** Button visible
- **Pass criteria:** Button present

#### 8.4 – PNG download
- **Steps:** Click "Export PNG"
- **Expected:** PNG file downloads
- **Pass criteria:** File download triggered, valid PNG

#### 8.5 – PDF export
- **Steps:** Look for "Export PDF" button
- **Expected:** PDF export available
- **Pass criteria:** Button present (P2 — may not be implemented)

#### 8.6 – Export quality
- **Steps:** Open exported SVG/PNG in viewer
- **Expected:** Diagram readable, nodes/edges visible, labels legible
- **Pass criteria:** Visual quality acceptable for documentation

---

## Epic 9: Real-time Updates

**Goal:** Verify real-time per MVP §2 (Live Diagrams), Tech FE §3 (Real-time Updates)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 9.1 | SignalR connection established | Tech FE §3 | P0 | ⬚ |
| 9.2 | SignalR hub messages received | Tech FE §3 | P1 | ⬚ |
| 9.3 | Diagram updates on new discovery | Tech FE §3, MVP §2 | P1 | ⬚ |
| 9.4 | Stale data indicator when disconnected | Tech FE §3 | P2 | ⬚ |

#### 9.1 – SignalR connection
- **Steps:** Open diagram page → Check network tab for WebSocket
- **Expected:** SignalR WebSocket connection established
- **Pass criteria:** WebSocket connection open in network tab

#### 9.2 – Hub messages
- **Steps:** Monitor SignalR messages in network tab
- **Expected:** Heartbeat or data messages flowing
- **Pass criteria:** Messages visible in WebSocket frames

#### 9.3 – Live update on discovery
- **Steps:** Trigger discovery while diagram is open
- **Expected:** Diagram updates with new resources without manual refresh
- **Pass criteria:** New nodes appear automatically

#### 9.4 – Stale indicator
- **Steps:** Disconnect network briefly → Observe UI
- **Expected:** Indicator shows data may be stale
- **Pass criteria:** Warning or indicator visible (P2)

---

## Epic 10: Time Navigation

**Goal:** Verify timeline per Tech FE §4 (Time Navigation and Diff Views)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 10.1 | Timeline slider visible on diagram | Tech FE §4 | P1 | ⬚ |
| 10.2 | Timeline slider interactive | Tech FE §4 | P1 | ⬚ |
| 10.3 | Diagram changes when timeline moves | Tech FE §4 | P2 | ⬚ |
| 10.4 | Diff view between two timestamps | Tech FE §4 | P2 | ⬚ |

#### 10.1 – Timeline visible
- **Steps:** Look for timeline slider on diagram page
- **Expected:** Horizontal slider with time labels
- **Pass criteria:** Slider element present

#### 10.2 – Timeline interaction
- **Steps:** Drag timeline slider
- **Expected:** Slider moves, value changes
- **Pass criteria:** Slider is draggable/clickable

#### 10.3 – Timeline affects diagram
- **Steps:** Move timeline to different position
- **Expected:** Diagram state changes to reflect that point in time
- **Pass criteria:** Node/edge changes visible (requires history data)

#### 10.4 – Diff view
- **Steps:** Look for "Compare" or diff feature
- **Expected:** Can compare two timestamps
- **Pass criteria:** Diff view available (P2 — likely future)

---

## Epic 11: Navigation & Page Routing

**Goal:** Verify navigation per MVP §3 (Intuitive UX), Tech FE §10 (Cross-platform), UX §1.1 (Consistency)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 11.1 | Dashboard page accessible | MVP §3 | P0 | ⬚ |
| 11.2 | Organizations page accessible | MVP §3, Tech BE §6 | P0 | ⬚ |
| 11.3 | Subscriptions page accessible | MVP §4 | P0 | ⬚ |
| 11.4 | Diagram page accessible | MVP §2 | P0 | ⬚ |
| 11.5 | Navigation sidebar/header present | UX §1.1 | P0 | ⬚ |
| 11.6 | Active page highlighted in nav | UX §1.1 | P1 | ⬚ |
| 11.7 | Back/forward browser navigation works | Tech FE §10 | P1 | ⬚ |
| 11.8 | Deep linking to specific pages works | Tech FE §10 | P1 | ⬚ |

#### 11.1–11.4 – Page accessibility
- **Steps:** Click each nav link (Dashboard, Organizations, Subscriptions, Diagram)
- **Expected:** Each page renders correctly without errors
- **Pass criteria:** Page loads, content visible, no 404s

#### 11.5 – Navigation presence
- **Steps:** Observe sidebar or header navigation
- **Expected:** All main sections listed and clickable
- **Pass criteria:** Nav element present with all links

#### 11.6 – Active page indicator
- **Steps:** Navigate to each page, check nav highlighting
- **Expected:** Current page highlighted/active in nav
- **Pass criteria:** Visual distinction on active nav item

#### 11.7 – Browser navigation
- **Steps:** Navigate pages → Press browser Back → Forward
- **Expected:** Correct pages restore
- **Pass criteria:** History navigation works

#### 11.8 – Deep linking
- **Steps:** Copy URL of diagram page → Open in new tab
- **Expected:** Correct page loads directly
- **Pass criteria:** Same page renders (after auth)

---

## Epic 12: Theme & Visual Design

**Goal:** Verify theming per UX §3.1 (Dark Mode), UX §1.2 (Accessibility), UX §5 (Theming)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 12.1 | Dark theme renders correctly | UX §3.1, UX §5 | P0 | ⬚ |
| 12.2 | Light theme renders correctly | UX §3.1, UX §5 | P0 | ⬚ |
| 12.3 | Theme toggle works | UX §5 | P0 | ⬚ |
| 12.4 | Theme persists across page reload | UX §5 | P1 | ⬚ |
| 12.5 | Text is readable in both themes | UX §3.1, UX §1.2 | P1 | ⬚ |
| 12.6 | Diagram canvas readable in both themes | UX §3.1 | P1 | ⬚ |

#### 12.1 – Dark theme
- **Steps:** Switch to dark theme
- **Expected:** All UI elements have dark backgrounds, light text
- **Pass criteria:** No white-on-white or invisible elements

#### 12.2 – Light theme
- **Steps:** Switch to light theme
- **Expected:** Light backgrounds, dark text
- **Pass criteria:** No dark-on-dark or invisible elements

#### 12.3 – Theme toggle
- **Steps:** Click theme toggle button
- **Expected:** Theme switches immediately
- **Pass criteria:** Visual change within 100ms

#### 12.4 – Theme persistence
- **Steps:** Set theme → Reload page
- **Expected:** Same theme applied after reload
- **Pass criteria:** Theme preference saved (localStorage)

#### 12.5 – Text readability
- **Steps:** Check all text in both themes
- **Expected:** Sufficient contrast for readability
- **Pass criteria:** No unreadable text

#### 12.6 – Diagram readability
- **Steps:** View diagram in both themes
- **Expected:** Nodes, edges, labels all visible
- **Pass criteria:** Diagram elements have adequate contrast

---

## Epic 13: UX Patterns & Frontend Quality

**Goal:** Verify UX patterns per UX §2 (Front-end Patterns), UX §1.1 (Speed/Feedback)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 13.1 | Loading states shown during API calls | UX §2.3 (Skeleton Loading) | P1 | ⬚ |
| 13.2 | Toast notifications for actions | UX §2.7 (Toast) | P1 | ⬚ |
| 13.3 | Error boundary catches runtime errors | UX §1.1 (Resilience) | P1 | ⬚ |
| 13.4 | Zero console errors on all pages | UX §1.1 | P0 | ⬚ |
| 13.5 | Zero console warnings on all pages | UX §1.1 | P1 | ⬚ |
| 13.6 | Responsive layout on tablet viewport | UX §1.2, Tech FE §10 | P2 | ⬚ |
| 13.7 | Sticky navigation/header | UX §2.5 (Sticky Headers) | P2 | ⬚ |
| 13.8 | Debounced search input | UX §2.1 (Debouncing) | P2 | ⬚ |

#### 13.1 – Loading states
- **Steps:** Trigger data-loading actions (navigate, discover)
- **Expected:** Skeleton loaders or spinners shown during load
- **Pass criteria:** Visual loading indicator present

#### 13.2 – Toast notifications
- **Steps:** Perform actions (create org, export diagram)
- **Expected:** Toast/snackbar feedback for success/error
- **Pass criteria:** Notification appears and auto-dismisses

#### 13.3 – Error boundary
- **Steps:** Navigate to a page with potential errors
- **Expected:** Errors caught by boundary, fallback UI shown instead of crash
- **Pass criteria:** No white screen of death

#### 13.4 – Zero console errors
- **Steps:** Navigate through all pages, check console
- **Expected:** No error-level console messages
- **Pass criteria:** Console error count = 0

#### 13.5 – Zero console warnings
- **Steps:** Check console for warnings across all pages
- **Expected:** No warning-level console messages
- **Pass criteria:** Console warning count = 0

#### 13.6 – Responsive layout
- **Steps:** Resize viewport to 768px width
- **Expected:** Layout adapts, no horizontal scroll, nav collapses
- **Pass criteria:** Content readable and functional

#### 13.7 – Sticky navigation
- **Steps:** Scroll down on a long page
- **Expected:** Navigation stays visible at top
- **Pass criteria:** Nav header remains fixed

#### 13.8 – Debounced search
- **Steps:** Type quickly in search → Check network tab
- **Expected:** API calls debounced (not one per keystroke)
- **Pass criteria:** Max 1 API call per 300ms of typing

---

## Epic 14: IaC Drift Detection

**Goal:** Verify drift per MVP §4 (IaC Synchronisation), Tech FE §5 (Threat Model Overlays — drift as risk)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 14.1 | Drift query service wired in backend | MVP §4 | P1 | ⬚ |
| 14.2 | Graph nodes enriched with drift status | MVP §4 | P1 | ⬚ |
| 14.3 | Frontend maps drift field to node badge | MVP §4, Tech FE §5 | P1 | ⬚ |
| 14.4 | Drift legend item present | MVP §4 | P1 | ⬚ |

#### 14.1 – Backend drift wiring
- **Steps:** Verify `IDriftQueryService` is registered in DI and `GetGraphHandler` injects it
- **Expected:** Drift data flows from Discovery module to Graph handler
- **Pass criteria:** Code review confirms wiring (already implemented)

#### 14.2 – Graph node drift
- **Steps:** Check GraphDto response includes `drift` field
- **Expected:** `GraphNodeDto` has `bool Drift` property
- **Pass criteria:** API response includes drift field per node

#### 14.3 – Frontend drift mapping
- **Steps:** Check diagram nodes for drift visual indicator
- **Expected:** Nodes with drift=true show drift badge
- **Pass criteria:** Badge visible (requires drift data from scan)

#### 14.4 – Drift legend
- **Steps:** Check legend on diagram page
- **Expected:** Drift indicator explained in legend
- **Pass criteria:** Drift entry present in legend

---

## Epic 15: App Insights Telemetry

**Goal:** Verify telemetry per MVP §6 (Health Monitoring), Tech BE §1 (Telemetry Ingestion)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 15.1 | Per-project App Insights config supported | Tech BE §1 | P1 | ⬚ |
| 15.2 | Auto-discovery stores App Insights config | Tech BE §1 | P1 | ⬚ |
| 15.3 | Auto-sync triggers after discovery | Tech BE §1 | P1 | ⬚ |
| 15.4 | Health scores feed node badges when configured | MVP §6, Tech FE §2 | P1 | ⬚ |
| 15.5 | Graceful fallback when no API key | MVP §6, Tech BE §8 | P1 | ⬚ |

#### 15.1 – Per-project config
- **Steps:** Verify `IAppInsightsConfigStore` stores per-project AppId
- **Expected:** Config stored and retrievable per project
- **Pass criteria:** Code review confirms (already implemented)

#### 15.2 – Auto-discovery config
- **Steps:** Run discovery → Check if App Insights resource detected and config stored
- **Expected:** `ConfigureAppInsightsOnDiscoveryHandler` fires
- **Pass criteria:** Config persisted after discovery

#### 15.3 – Auto-sync
- **Steps:** After discovery, verify telemetry sync is triggered
- **Expected:** `SyncApplicationInsightsTelemetryCommand` sent
- **Pass criteria:** Telemetry data available post-discovery

#### 15.4 – Health scores on nodes
- **Steps:** Configure valid App Insights API key → View diagram
- **Expected:** Nodes show yellow/red where health < 1.0
- **Pass criteria:** Non-green health badges visible (requires API key)

#### 15.5 – Graceful fallback
- **Steps:** View diagram without App Insights API key
- **Expected:** All nodes default to green, no errors
- **Pass criteria:** No crashes, warning logged server-side

---

## Epic 16: Performance & Scalability

**Goal:** Verify performance per MVP §5 (Handle Large Environments), Tech BE §8 (Scalability), UX §1.1 (Speed)

| # | Test Case | Requirement Source | Priority | Status |
|---|-----------|-------------------|----------|--------|
| 16.1 | Diagram renders 800+ nodes without crash | MVP §5, Tech BE §8 | P0 | ⬚ |
| 16.2 | Page load time < 5 seconds | UX §1.1, MVP §5 | P1 | ⬚ |
| 16.3 | C4 level switching < 2 seconds | MVP §5 | P1 | ⬚ |
| 16.4 | Search response < 1 second | UX §1.1 | P1 | ⬚ |
| 16.5 | No browser memory leaks on long session | Tech BE §8 | P2 | ⬚ |
| 16.6 | Zoom/pan smooth at 60fps | UX §1.1 | P2 | ⬚ |

#### 16.1 – Large diagram render
- **Steps:** Load diagram with 800+ nodes (Container level)
- **Expected:** All nodes render, canvas interactive
- **Pass criteria:** No crash, no frozen UI, nodes visible

#### 16.2 – Page load time
- **Steps:** Measure time from navigation to content visible
- **Expected:** < 5 seconds for diagram page
- **Pass criteria:** Performance acceptable

#### 16.3 – Level switch speed
- **Steps:** Time the switch between C4 levels
- **Expected:** < 2 seconds for re-render
- **Pass criteria:** Smooth transition

#### 16.4 – Search speed
- **Steps:** Type search term, measure filter time
- **Expected:** Results appear within 1 second
- **Pass criteria:** Near-instant filtering

#### 16.5 – Memory stability
- **Steps:** Navigate pages for 10+ minutes, monitor memory
- **Expected:** No significant memory growth
- **Pass criteria:** Memory stable within reasonable bounds

#### 16.6 – Smooth interactions
- **Steps:** Zoom and pan rapidly
- **Expected:** No visible jank or frame drops
- **Pass criteria:** Smooth visual experience

---

## Risks

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | App Insights tests require valid API key | High | Medium | Test graceful fallback; document as known limitation |
| R2 | Drift detection tests require IaC scan data | High | Medium | Verify code wiring; test with seed data if available |
| R3 | Some P2 features may not be implemented yet | High | Low | Document as gaps, not failures |
| R4 | Login credentials may change between tests | Medium | Low | Create fresh test account each session |
| R5 | CI/CD deployment lag may show old version | Low | High | Verify commit hash matches deployment |

## Critical Path

1.1 → 1.2 → 4.1 → 4.2 → 5.1 → 5.5 → 6.1 → 7.2 → 8.2 → 9.1 → 13.4

## Estimated Total Effort

- P0 tests: 20 cases × ~5 min = ~100 min
- P1 tests: 30 cases × ~5 min = ~150 min
- P2 tests: 16 cases × ~5 min = ~80 min
- **Total: ~330 minutes (~5.5 hours)**
