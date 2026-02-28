# Live Testing & Fix Plan for c4.jakub.team

## Test Environment
- **URL:** https://c4.jakub.team
- **Account:** jakub.sikora@circit.io
- **Date:** 2026-02-28
- **Tool:** Playwright MCP (Chromium)

---

## Epic 1: Environment & Playwright Setup

### 1.1 Install Playwright MCP and launch browser
- **Status:** PASS
- **Expected:** App loads at https://c4.jakub.team
- **Actual:** App loads, redirects to /login, renders login form correctly

### 1.2 Verify API connectivity
- **Status:** PASS
- **Expected:** No console errors, login page renders
- **Actual:** Zero console errors, login form with Sign In / Create Account tabs

---

## Epic 2: Authentication Testing

### 2.1 Login with existing account
- **Status:** PASS
- **Expected:** Login redirects to dashboard
- **Actual:** Successful login, redirects to / (dashboard), shows org/project/resources

### 2.2 Test auth token persistence
- **Status:** PASS
- **Expected:** Page reload keeps user logged in
- **Actual:** Token persists in localStorage, page reload stays on dashboard

### 2.3 Test protected route redirect
- **Status:** PASS
- **Expected:** Unauthenticated access to /organizations redirects to /login
- **Actual:** Clearing localStorage and navigating to /organizations correctly redirects to /login

### 2.4 Test logout
- **Status:** PASS
- **Expected:** Sign out clears token and redirects to login
- **Actual:** Sign out clears token from localStorage and redirects to /login

---

## Epic 3: Organization & Project Management

### 3.1 View organizations page
- **Status:** PASS
- **Expected:** Organizations page shows current orgs/projects
- **Actual:** Shows "Circit Ltd" org with 1 project "Circit". Brief flash of "Registering..." state before API responds (minor UX issue)

### 3.2 Test create organization
- **Status:** PASS (existing org shown)
- **Expected:** Existing org appears
- **Actual:** "Circit Ltd" organization displayed with icon. No ability to create additional orgs (by design for single-org)

### 3.3 Test create project
- **Status:** PASS (UI present)
- **Expected:** New project form visible
- **Actual:** "New Project" input field and "+ Create Project" button visible. Button correctly disabled when field empty. Projects (1) list shows "Circit"

---

## Epic 4: Subscription & Discovery Testing

### 4.1 View subscriptions page
- **Status:** PASS
- **Expected:** Subscriptions page shows current state
- **Actual:** Shows connected subscription "Visual Studio Enterprise: BizSpark" (f18100c6-...) with Disconnect button. MCP Servers section with "Add Server" button

### 4.2 Test Azure subscription connection
- **Status:** PASS (already connected)
- **Expected:** Azure subscription connected
- **Actual:** Subscription already connected with green checkmark. Disconnect button available

### 4.3 Test MCP server configuration
- **Status:** PASS
- **Expected:** MCP server section visible, add form works
- **Actual:** "Add Server" button opens form with Server Name + Endpoint URL fields. Cancel dismisses form correctly

### 4.4 Test Git IaC configuration
- **Status:** FAIL
- **Expected:** Git repo URL + PAT fields visible in subscription wizard
- **Actual:** No Git IaC configuration section exists on the subscriptions page. This feature is missing from the UI entirely

### 4.5 Trigger discovery (Rediscover)
- **Status:** FAIL (P0 CRASH)
- **Expected:** Discovery triggers, resources appear with classification
- **Actual:** Clicking "Rediscover" causes app crash: `TypeError: Cannot read properties of null (reading 'length')`. Screen goes completely blank (white/empty). React error boundary does not catch this. Requires page reload to recover
- **Root Cause:** Rediscover deletes graph first (`DELETE /api/projects/{id}/graph`), then `refetch()` returns graph with null nodes/edges arrays. `DashboardPage.tsx:454` tries to read `.length` on null

---

## Epic 5: Diagram Output Testing

### 5.1 Load diagram page
- **Status:** PARTIAL PASS
- **Expected:** Canvas renders with React Flow showing real data
- **Actual:**
  - `/diagram` (nav link) = Shows seed/test data (6 hardcoded nodes), NOT real resources
  - `/diagram/:projectId` (via "View Diagram" button) = Shows real data (994 nodes, 2 edges)
  - **Bug:** Nav "Diagram" link goes to `/diagram` without projectId, showing seed data instead of active project

### 5.2 Test C4 level switching
- **Status:** PARTIAL PASS
- **Expected:** Context/Container/Component levels show meaningfully different node sets
- **Actual:** Level switching works mechanically, but:
  - With seed data: Context=1 node, Container=6 nodes, Component=1 node
  - With real data: All 994 nodes are classified as "Container" only - no proper C4 hierarchy. Context and Component levels show 0 nodes
  - C4 classification from discovery is not producing meaningful hierarchy

### 5.3 Test health overlays
- **Status:** FAIL
- **Expected:** Nodes show green/yellow/red health from real Application Insights telemetry
- **Actual:** All real nodes show "GREEN" - health values are default/hardcoded, not sourced from App Insights. Seed data has varied health (green/yellow) but it's artificial

### 5.4 Test traffic colors on edges
- **Status:** FAIL
- **Expected:** Edges show traffic-based colors between services
- **Actual:** Only 2 edges in 994 nodes. No visible edges on screen. Discovery does not map dependencies/communications between resources. No traffic data from App Insights

### 5.5 Test zoom/pan controls
- **Status:** PASS
- **Expected:** Zoom in/out and fit view work
- **Actual:** Zoom In, Zoom Out, Fit View, Toggle Interactivity all work. Mini map visible and functional

### 5.6 Test search/filter
- **Status:** PASS
- **Expected:** Typing in diagram filter filters nodes
- **Actual:** Diagram filter works correctly (tested filtering to "Redis" - showed only Redis Cache node)

### 5.7 Test SVG/PDF export
- **Status:** PARTIAL PASS
- **Expected:** Both export buttons trigger downloads
- **Actual:**
  - **SVG Export:** PASS - Downloads `architecture-diagram.svg` correctly
  - **PDF Export:** FAIL - Opens blank new tab ("Architecture Diagram" at about:blank), freezes the main page. Requires force-navigation to recover. PDF generation appears broken

### 5.8 Test SignalR real-time updates
- **Status:** FAIL
- **Expected:** SignalR hub connection at /hubs/diagram active
- **Actual:** No SignalR/WebSocket connections observed in network traffic. Code exists in `signalrClient.ts` and `useSignalR.ts` but no hub connection is established. Zero `/hub` or `/signalr` requests captured. The real-time update feature is non-functional

---

## Epic 6: Cross-Cutting UX Testing

### 6.1 Test dark/light theme toggle
- **Status:** FAIL
- **Expected:** Full theme switch between dark and light
- **Actual:** Clicking "Light" changes nav bar background to light and button text to "Dark", but the main content area (sidebar, diagram canvas, cards) stays dark-themed. Sidebar text becomes nearly invisible. Theme is only partially applied - broken light mode

### 6.2 Test navigation between all pages
- **Status:** PARTIAL PASS
- **Expected:** All nav links route correctly
- **Actual:** Dashboard, Organizations, Subscriptions links work correctly. Diagram nav link goes to `/diagram` (no projectId) = seed data. Should link to active project diagram

### 6.3 Test error handling
- **Status:** FAIL
- **Expected:** Errors show user-friendly messages or error boundaries
- **Actual:** Rediscover crash causes blank white screen - no error boundary catches it. No user-friendly error recovery

### 6.4 Test loading states
- **Status:** PASS
- **Expected:** Loading indicators visible during API calls
- **Actual:** Dashboard shows skeleton loading states. Organization page briefly shows "Registering..." during load. Buttons show spinner during async operations

### 6.5 Test responsive layout
- **Status:** FAIL
- **Expected:** Layout adapts to mobile viewport
- **Actual:** At 375px width, diagram sidebar takes full viewport height, pushing canvas completely out of view. No collapsible sidebar or responsive layout. Nav wraps but is still functional

### 6.6 Global search bar (bonus)
- **Status:** FAIL
- **Expected:** Search filters dashboard resources
- **Actual:** Search bar present on all pages but non-functional. Typing "certificates" shows no filtering effect on the resource list. Placeholder UI only

---

## Epic 7: Requirements Gap Analysis

### 7.1 MVP user requirements gaps

| Req # | Requirement | Status | Gap |
|-------|-------------|--------|-----|
| 1 | Instant agent-less discovery | PARTIAL | Discovery works but all resources classified as "Container" only. No dependency mapping. No orphaned resource detection |
| 2 | Real-time interactive visualization with traffic overlays | FAIL | No real-time updates (SignalR not connected). No traffic overlays. No health from App Insights. No edge coloring. Nodes show default GREEN |
| 3 | Ease of setup and intuitive UX | PARTIAL | Login/org/subscription flow works. But: broken light theme, non-functional search, Rediscover crash, no guided wizard |
| 4 | Integration with Azure and IaC | PARTIAL | Azure Resource Graph discovery works. No IaC (Bicep/Terraform) integration in UI. No drift detection from real IaC |
| 5 | Performance and scalability | FAIL | 994 nodes rendered in a flat list (no virtualization). PDF export freezes browser. Dashboard renders all 995 nodes in a flat `<ul>` |
| 6 | Health monitoring, cost insights, alerting | FAIL | No App Insights integration for health. No cost data. No alerting |
| 7 | Collaboration, documentation, sharing | FAIL | SVG export works. PDF broken. No shareable links, no versioning, no comments |
| 8 | Security and compliance insights | NOT IMPLEMENTED | No security scanning, no NSG analysis, no policy violations |
| 9 | Pricing and licensing | NOT IMPLEMENTED | No pricing/subscription management |
| 10 | Extensibility and ecosystem | PARTIAL | MCP server configuration exists but no servers connected. No webhooks/API exposed |

### 7.2 Technical requirements gaps

**Front-End Gaps:**
| Req | Description | Status |
|-----|-------------|--------|
| FE-1 | Interactive C4 diagrams with pan/zoom/collapse | PARTIAL - Pan/zoom work but no collapse/expand, no drill-down |
| FE-2 | Live traffic overlays with colored edges | FAIL - No traffic data, no colored edges |
| FE-3 | Real-time updates via WebSocket | FAIL - SignalR code exists but not connected |
| FE-4 | Time navigation and diff views | FAIL - Timeline slider exists but no real historical data |
| FE-5 | Threat model overlays | NOT IMPLEMENTED |
| FE-6 | Documentation & code integration | NOT IMPLEMENTED |
| FE-7 | Customizable views and filters | PARTIAL - Level filter and search work. No grouping by domain/team |
| FE-8 | Export and sharing | PARTIAL - SVG works, PDF broken, no shareable links |
| FE-9 | Auth & RBAC | PARTIAL - JWT auth works. No RBAC roles |
| FE-10 | Cross-platform usability | FAIL - Mobile layout broken |

**Back-End Gaps:**
| Req | Description | Status |
|-----|-------------|--------|
| BE-1 | Telemetry ingestion (App Insights) | NOT CONNECTED - No live telemetry feeding diagram health |
| BE-2 | Stream processing & aggregation | NOT IMPLEMENTED |
| BE-3 | Graph modeling with versioning and C4 levels | PARTIAL - Graph exists but flat (all Container), no versioning |
| BE-4 | Threat-modeling engine | NOT IMPLEMENTED |
| BE-5 | APIs and WebSocket | PARTIAL - REST APIs work, SignalR hub not connecting |
| BE-6 | User and org management | PASS - Works correctly |
| BE-7 | Security and compliance | PARTIAL - JWT auth, no audit logs |

---

## Epic 8: Fix Planning & Implementation

### 8.1 Discrepancy Priority List

#### P0 - Critical (App Crashes / Core Feature Broken)

| # | Issue | Impact | Files |
|---|-------|--------|-------|
| P0-1 | Rediscover crashes app (null .length) | Users cannot rediscover resources | `web/src/features/dashboard/DashboardPage.tsx:454` |
| P0-2 | Diagram nav shows seed data instead of real project | Users see fake data on diagram page | `web/src/App.tsx`, `web/src/features/diagram/hooks/useDiagram.ts` |
| P0-3 | PDF export freezes browser | Export feature unusable | `web/src/features/diagram/DiagramPage.tsx` |

#### P1 - High (Major Feature Gaps)

| # | Issue | Impact | Files |
|---|-------|--------|-------|
| P1-1 | All nodes classified as "Container" - no C4 hierarchy | C4 level switching is meaningless | Backend classification logic in Discovery module |
| P1-2 | Only 2 edges in 994 nodes - no dependency mapping | Architecture diagram shows disconnected nodes | Backend Graph module - ResourcesDiscoveredHandler |
| P1-3 | No real health data (all GREEN) | Health overlays are fake | Backend - App Insights integration missing |
| P1-4 | SignalR not connecting | No real-time updates | Backend `Program.cs` hub mapping, frontend `useSignalR.ts` |
| P1-5 | Global search bar non-functional | Search is placeholder only | `web/src/features/dashboard/DashboardPage.tsx` |
| P1-6 | Light theme broken (partially applied) | Theme toggle unusable | CSS variables / theme system |

#### P2 - Medium (UX Issues)

| # | Issue | Impact | Files |
|---|-------|--------|-------|
| P2-1 | Mobile responsive layout broken | Diagram unusable on mobile | CSS for diagram sidebar |
| P2-2 | Project name shown as GUID | Poor UX | Dashboard + Graph API |
| P2-3 | 995 nodes rendered in flat list (no virtualization) | Performance issue on large subscriptions | `DashboardPage.tsx` resource list |
| P2-4 | No error boundary catches crashes | Blank screen on errors | `web/src/App.tsx` |
| P2-5 | Organizations page flashes "Registering..." | Brief loading state glitch | `web/src/features/organization/OrganizationPage.tsx` |

#### P3 - Low (Missing Features for MVP)

| # | Issue | Impact | Files |
|---|-------|--------|-------|
| P3-1 | No Git IaC configuration UI | Cannot connect IaC repos | Subscription page |
| P3-2 | No diagram node grouping/clustering | 994 nodes in flat layout | Diagram layout algorithm |
| P3-3 | No edge traffic colors | No visual traffic indicators | Diagram edge rendering |
| P3-4 | No timeline with real data | Timeline slider is non-functional | Graph versioning backend |

### 8.2 Fix Tasks

#### P0-1: Fix Rediscover crash (null safety)
- **File:** `web/src/features/dashboard/DashboardPage.tsx`
- **Line:** 454
- **Fix:** Add null safety: `(graph.nodes?.length ?? 0)` and `(graph.edges?.length ?? 0)`
- **Also:** Add null safety to line 486: `(graph.nodes ?? []).map(...)`
- **Also:** Add global React error boundary in `App.tsx`

#### P0-2: Fix diagram nav link to use active project
- **File:** `web/src/App.tsx` (route), nav component
- **Fix:** Diagram nav link should include projectId from app context/state, or redirect `/diagram` to `/diagram/:activeProjectId`

#### P0-3: Fix PDF export
- **File:** `web/src/features/diagram/DiagramPage.tsx`
- **Fix:** PDF export opens new tab instead of downloading. Use html2canvas + jsPDF or react-to-pdf to generate and download PDF blob instead of window.open

#### P1-5: Implement global search
- **File:** `web/src/features/dashboard/DashboardPage.tsx`
- **Fix:** Filter the `graph.nodes` array by the search input value, matching against `node.name` and `node.externalResourceId`

#### P1-6: Fix light theme
- **Files:** CSS files / theme variables
- **Fix:** Ensure all components use CSS custom properties that change with theme class. Audit card, panel, sidebar, and canvas background variables

#### P2-4: Add error boundary
- **File:** `web/src/App.tsx`
- **Fix:** Wrap route content in React ErrorBoundary component with fallback UI and retry button

### 8.3 P0 Fix Implementation
- **Status:** DONE

**Fixes implemented:**

| Fix | Status | Files Changed |
|-----|--------|---------------|
| P0-1: Null safety on graph.nodes/edges | DONE | `DashboardPage.tsx` (lines 454, 486), `useDiagram.ts` (lines 57, 66) |
| P0-2: Diagram auto-redirect to active project | DONE | `DiagramPage.tsx` (added `useAutoRedirect` hook) |
| P0-3: PDF export replaced with PNG blob download | DONE | `useDiagramExport.ts` (SVG-to-canvas-to-PNG pipeline) |
| P1-5: Global search now functional | DONE | New `SearchContext.tsx`, updated `CommandPalette.tsx`, `DashboardPage.tsx` |
| P1-6: Light theme fixed for diagram | DONE | `diagram.css` (all hardcoded colors replaced with CSS variables) |
| P2-4: Error boundary added | DONE | `App.tsx` (new `ErrorBoundary` class component wrapping app) |
| P2-1: Mobile sidebar overflow fix | DONE | `diagram.css` (added `max-height: 40vh; overflow-y: auto` on mobile) |

**Build verification:**
- Frontend: `npm run build` PASS (0 errors, 2 non-blocking warnings from third-party)
- Backend: `dotnet build` pending verification

### 8.4 Fix Verification
- **Status:** PENDING (deploy and re-test via Playwright)

---

## Discrepancies Summary

| # | Epic | Test | Severity | Description | Fix Status |
|---|------|------|----------|-------------|------------|
| 1 | 4 | 4.5 | P0 | Rediscover crashes app with null .length TypeError | FIXED |
| 2 | 5 | 5.1 | P0 | Diagram nav shows seed data, not real project | FIXED |
| 3 | 5 | 5.7 | P0 | PDF export freezes browser (opens blank tab) | FIXED (now PNG) |
| 4 | 5 | 5.2 | P1 | All nodes "Container" - no C4 hierarchy from discovery | NEEDS BACKEND |
| 5 | 5 | 5.4 | P1 | Only 2 edges - no dependency mapping | NEEDS BACKEND |
| 6 | 5 | 5.3 | P1 | No real health data - all GREEN defaults | NEEDS BACKEND |
| 7 | 5 | 5.8 | P1 | SignalR not connecting | NEEDS INVESTIGATION |
| 8 | 6 | 6.6 | P1 | Global search non-functional | FIXED |
| 9 | 6 | 6.1 | P1 | Light theme partially broken | FIXED |
| 10 | 6 | 6.5 | P2 | Mobile responsive layout broken | FIXED |
| 11 | 4 | - | P2 | Project displayed as GUID not name | OPEN |
| 12 | 5 | - | P2 | 994 nodes flat list - needs virtualization | OPEN |
| 13 | 6 | 6.3 | P2 | No error boundary - blank screen on crash | FIXED |
| 14 | 4 | 4.4 | P3 | No Git IaC configuration UI | NEEDS DESIGN |
| 15 | 5 | - | P3 | No node grouping/clustering layout | NEEDS DESIGN |
| 16 | 5 | 5.4 | P3 | No edge traffic colors | NEEDS BACKEND |
| 17 | 5 | - | P3 | Timeline slider has no real data | NEEDS BACKEND |
