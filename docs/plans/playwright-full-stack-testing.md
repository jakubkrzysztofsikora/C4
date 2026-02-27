## Plan: Playwright Full-Stack Testing via MCP
Scope: FeatureSet
Created: 2026-02-27
Status: Draft

### Overview
Set up and execute comprehensive full-stack browser-based testing of the entire C4 application using Playwright MCP. The entire stack runs via Docker Compose — PostgreSQL 17 (real database), .NET 9 backend (with EF Core migrations and seed data), and React/Vite frontend. Playwright MCP drives a real Chromium browser to test every user-facing feature end-to-end — authentication, organizations, subscriptions, diagrams, feedback, theme toggling, and navigation. No mocks, no stubs, no in-memory fakes.

### Success Criteria
- [ ] Docker Compose starts all 3 services (PostgreSQL, backend, frontend) successfully
- [ ] PostgreSQL is healthy and all EF Core migrations run
- [ ] Backend API on port 5000 with real database and seeded demo data
- [ ] Frontend dev server on port 3000 connecting to the real backend
- [ ] Playwright MCP is configured and can launch a browser session
- [ ] All authentication flows tested (register, login, logout, protected routes)
- [ ] Organization and project management flows tested
- [ ] Subscription wizard flow tested
- [ ] Dashboard page tested (load, display, empty state)
- [ ] Diagram page tested (load, canvas interaction, filtering, export)
- [ ] Feedback flows tested (submit feedback, view summary, eval dashboard)
- [ ] Cross-cutting concerns tested (theme toggle, navigation, toast notifications, error handling)
- [ ] All tests pass against the live running full-stack application

### Epic 1: Environment Setup & Infrastructure
Goal: Get the full stack running via Docker Compose and configure Playwright MCP for browser automation

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Create .env file and start Docker Compose (PostgreSQL + backend + frontend) | Infrastructure | All | M | – | ⬚ |
| 1.2 | Verify all Docker services are healthy (PostgreSQL, backend, frontend) | Infrastructure | All | S | 1.1 | ⬚ |
| 1.3 | Verify database migrations ran and seed data exists | Infrastructure | Host | S | 1.2 | ⬚ |
| 1.4 | Install Playwright and browser dependencies | Infrastructure | Root | S | – | ⬚ |
| 1.5 | Configure Playwright MCP server (.mcp.json) | Infrastructure | Root | S | 1.4 | ⬚ |
| 1.6 | Verify full stack connectivity via browser (health check, CORS, frontend loads) | Infrastructure | All | S | 1.2, 1.5 | ⬚ |

#### 1.1 – Create .env and Start Docker Compose
- **Actions**: Copy `.env.example` to `.env`, configure PostgreSQL credentials, update `appsettings.json` connection strings to point to the Compose PostgreSQL service, run `docker compose up -d`
- **Acceptance criteria**:
  - `.env` file exists with `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
  - `docker compose up -d` starts all 3 services without errors
  - All containers are running (`docker compose ps` shows healthy/running)

#### 1.2 – Verify All Docker Services Are Healthy
- **Actions**: Check `docker compose ps` for all services, verify PostgreSQL health check passes, verify backend responds on port 5000, verify frontend serves on port 3000
- **Acceptance criteria**:
  - PostgreSQL container is healthy (pg_isready succeeds)
  - Backend container is running and `/health` returns 200
  - All module health endpoints return OK (`/api/identity/health`, `/api/discovery/health`, etc.)
  - Frontend container is running and serves HTML on port 3000

#### 1.3 – Verify Database Migrations and Seed Data
- **Actions**: Check backend logs for migration completion, verify seed data via API calls (login with demo user, check for seeded org/project)
- **Acceptance criteria**:
  - EF Core migrations applied for all 5 DbContexts (Identity, Discovery, Graph, Telemetry, Visualization)
  - Demo user `demo@c4.local` can authenticate
  - "C4 Demo Organization" and "Sample Cloud Project" exist
  - Default view preset exists in Visualization DB

#### 1.4 – Install Playwright and Browser Dependencies
- **Actions**: Install `@playwright/mcp` package globally or locally, ensure Chromium browser binaries are available
- **Acceptance criteria**:
  - Playwright is available via npx
  - Chromium browser binary is installed and functional

#### 1.5 – Configure Playwright MCP Server
- **Actions**: Create `.mcp.json` at project root with Playwright MCP server configuration
- **Acceptance criteria**:
  - `.mcp.json` exists with correct Playwright MCP server entry
  - MCP server can be started and connects to a browser

#### 1.6 – Verify Full Stack Connectivity via Browser
- **Actions**: Use Playwright MCP to navigate to `http://localhost:3000`, verify the app loads, verify API calls from the browser to backend succeed (no CORS errors)
- **Acceptance criteria**:
  - Browser navigates to localhost:3000
  - Login page renders correctly
  - No console errors related to CORS or network
  - API calls from frontend to backend succeed through Docker networking

### Epic 2: Authentication Testing
Goal: Test all authentication flows through the browser against real backend + PostgreSQL

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Test login with seeded demo credentials | Test | Identity | S | 1.6 | ⬚ |
| 2.2 | Test login with invalid credentials (error handling) | Test | Identity | S | 2.1 | ⬚ |
| 2.3 | Test new user registration (persisted to PostgreSQL) | Test | Identity | S | 2.1 | ⬚ |
| 2.4 | Test logout and protected route redirect | Test | Identity | S | 2.1 | ⬚ |
| 2.5 | Test registration validation (short password, empty fields) | Test | Identity | S | 2.3 | ⬚ |

#### 2.1 – Test Login with Demo Credentials
- **Actions**: Navigate to `/login`, enter `demo@c4.local` / `Password123!`, submit, verify redirect to dashboard
- **Acceptance criteria**:
  - Login form is visible with email/password fields
  - After submission, user is redirected to `/` (dashboard)
  - User email is displayed in the header/nav
  - JWT token is stored in localStorage
  - Token was generated by the real backend with real JWT signing key

#### 2.2 – Test Login with Invalid Credentials
- **Actions**: Attempt login with wrong password, verify error message appears
- **Acceptance criteria**:
  - Error message is displayed on the login form
  - User remains on the login page
  - No redirect occurs
  - Backend returns 400 (real password hash check against PostgreSQL)

#### 2.3 – Test New User Registration
- **Actions**: Switch to "Create Account" tab, fill in display name / email / password (8+ chars), submit, verify success
- **Acceptance criteria**:
  - Registration tab is selectable
  - Form accepts display name, email, password
  - After submission, user is redirected to dashboard
  - New user is persisted in PostgreSQL (can log in again after logout)

#### 2.4 – Test Logout and Protected Route Redirect
- **Actions**: While logged in, click logout, verify redirect to login page. Then try navigating directly to `/organizations`, verify redirect to `/login`
- **Acceptance criteria**:
  - Sign out button is visible and clickable
  - After logout, user is on `/login`
  - Direct navigation to protected routes redirects to `/login`
  - localStorage token is cleared

#### 2.5 – Test Registration Validation
- **Actions**: Attempt registration with password < 8 chars, attempt with empty email
- **Acceptance criteria**:
  - Validation error messages appear
  - Form does not submit with invalid data

### Epic 3: Organization & Project Management Testing
Goal: Test organization creation and project management workflows against real database

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Test navigate to Organizations page | Test | Identity | S | 2.1 | ⬚ |
| 3.2 | Test create new organization | Test | Identity | S | 3.1 | ⬚ |
| 3.3 | Test create project within organization | Test | Identity | M | 3.2 | ⬚ |
| 3.4 | Test organization page empty/populated states | Test | Identity | S | 3.1 | ⬚ |

#### 3.1 – Test Navigate to Organizations Page
- **Actions**: Log in, click "Organizations" in nav, verify page loads
- **Acceptance criteria**:
  - Organizations page is accessible from navigation
  - Page renders with organization form or existing orgs (seeded "C4 Demo Organization" may appear)

#### 3.2 – Test Create New Organization
- **Actions**: Fill in organization name, submit, verify it appears in the list
- **Acceptance criteria**:
  - Organization name input is visible
  - After creation, the new organization is displayed
  - Organization is persisted in PostgreSQL
  - Success feedback is provided (toast or inline)

#### 3.3 – Test Create Project Within Organization
- **Actions**: After creating an org, fill in project name, submit, verify it appears
- **Acceptance criteria**:
  - Project creation form appears after org exists
  - New project is listed under the organization
  - Project is persisted in PostgreSQL with valid ID

#### 3.4 – Test Organization Page States
- **Actions**: Verify empty state messaging when no orgs exist, and populated state after creating one
- **Acceptance criteria**:
  - Empty state shows helpful guidance
  - Populated state shows the organization details

### Epic 4: Subscription Wizard Testing
Goal: Test the Azure subscription connection workflow against real backend

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Test navigate to Subscriptions page | Test | Discovery | S | 2.1 | ⬚ |
| 4.2 | Test connect Azure subscription | Test | Discovery | S | 4.1 | ⬚ |
| 4.3 | Test subscription connected state display | Test | Discovery | S | 4.2 | ⬚ |

#### 4.1 – Test Navigate to Subscriptions Page
- **Actions**: Log in, click "Subscriptions" in nav, verify the wizard page loads
- **Acceptance criteria**:
  - Subscription wizard page is accessible
  - Form for subscription ID and display name is visible

#### 4.2 – Test Connect Azure Subscription
- **Actions**: Enter an external subscription ID and display name, submit
- **Acceptance criteria**:
  - Form accepts both fields
  - Submission triggers API call to `/api/discovery/subscriptions`
  - Subscription is persisted in PostgreSQL (Discovery DB)
  - Success state is shown

#### 4.3 – Test Subscription Connected State
- **Actions**: After connecting, verify the page shows the connected subscription info
- **Acceptance criteria**:
  - Connected subscription details are displayed (ID, name, Azure icon)
  - State persists after page interaction

### Epic 5: Dashboard Testing
Goal: Test the main dashboard page with real graph data from PostgreSQL

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Test dashboard page load and layout | Test | Graph | S | 2.1 | ⬚ |
| 5.2 | Test project graph loading by ID | Test | Graph | M | 5.1 | ⬚ |
| 5.3 | Test dashboard empty and error states | Test | Graph | S | 5.1 | ⬚ |

#### 5.1 – Test Dashboard Page Load
- **Actions**: Log in, verify automatic redirect to dashboard, verify page layout
- **Acceptance criteria**:
  - Dashboard is the default authenticated route
  - Page renders with expected layout (header, content area)
  - No console errors

#### 5.2 – Test Project Graph Loading
- **Actions**: Enter the seeded project ID into the input form, submit, observe graph loading
- **Acceptance criteria**:
  - Project ID input field is visible and functional
  - Loading state is shown (skeleton/spinner)
  - API call hits real Graph module backed by PostgreSQL
  - Graph data displays or shows empty state if no graph nodes exist yet

#### 5.3 – Test Dashboard Empty/Error States
- **Actions**: Load with no project, load with invalid project ID
- **Acceptance criteria**:
  - Empty state message is shown when no data
  - Error state is handled gracefully (404 from real backend)

### Epic 6: Diagram Page Testing
Goal: Test the interactive architecture diagram viewer with real backend services

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Test diagram page navigation and load | Test | Visualization | S | 2.1 | ⬚ |
| 6.2 | Test diagram canvas rendering with XyFlow | Test | Visualization | M | 6.1 | ⬚ |
| 6.3 | Test C4 level filter selector | Test | Visualization | S | 6.1 | ⬚ |
| 6.4 | Test diagram search/filter functionality | Test | Visualization | S | 6.1 | ⬚ |
| 6.5 | Test diagram zoom controls | Test | Visualization | S | 6.2 | ⬚ |
| 6.6 | Test diagram export (SVG/PDF) | Test | Visualization | S | 6.2 | ⬚ |
| 6.7 | Test diagram with seeded project ID via URL param | Test | Visualization | S | 6.1 | ⬚ |

#### 6.1 – Test Diagram Page Navigation and Load
- **Actions**: Click "Diagram" in nav, verify page loads with canvas area
- **Acceptance criteria**:
  - Diagram page is accessible
  - Canvas container renders
  - Controls panel is visible (level selector, search, zoom)

#### 6.2 – Test Diagram Canvas Rendering
- **Actions**: Navigate to diagram with a project ID, observe canvas behavior
- **Acceptance criteria**:
  - XyFlow canvas initializes
  - MiniMap renders
  - Controls render (zoom in/out, fit view)
  - API call to real Visualization module + PostgreSQL

#### 6.3 – Test C4 Level Filter
- **Actions**: Click through C4 level options (Context, Container, Component)
- **Acceptance criteria**:
  - Level selector is visible and interactive
  - Changing level triggers an API call with the level query parameter to real backend
  - Canvas updates (or shows appropriate state)

#### 6.4 – Test Diagram Search/Filter
- **Actions**: Type into the search input, verify filtering behavior
- **Acceptance criteria**:
  - Search input is visible and functional
  - Typing triggers filtering of diagram nodes

#### 6.5 – Test Diagram Zoom Controls
- **Actions**: Click zoom in/out buttons, verify canvas zoom changes
- **Acceptance criteria**:
  - Zoom controls respond to clicks
  - Canvas zoom level changes visually

#### 6.6 – Test Diagram Export
- **Actions**: Trigger diagram export for SVG and/or PDF
- **Acceptance criteria**:
  - Export button/control is accessible
  - Export triggers download or blob fetch from real Visualization backend
  - No errors occur during export

#### 6.7 – Test Diagram with URL Parameter
- **Actions**: Navigate directly to `/diagram/{projectId}` with the seeded project ID
- **Acceptance criteria**:
  - Diagram page loads with the project context
  - Graph data is fetched from real Graph module backed by PostgreSQL

### Epic 7: Feedback System Testing
Goal: Test the feedback submission and analytics UI with real persistence

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Test feedback panel component | Test | Feedback | M | 2.1 | ⬚ |
| 7.2 | Test star rating interaction | Test | Feedback | S | 7.1 | ⬚ |
| 7.3 | Test eval dashboard page | Test | Feedback | M | 7.1 | ⬚ |
| 7.4 | Test feedback summary and learnings display | Test | Feedback | S | 7.1 | ⬚ |

#### 7.1 – Test Feedback Panel Component
- **Actions**: Navigate to a context where the feedback panel is accessible, open it, verify form elements
- **Acceptance criteria**:
  - Feedback panel/dialog opens
  - Contains star rating, comment field, category display
  - Can be submitted (persisted to PostgreSQL) and closed

#### 7.2 – Test Star Rating Interaction
- **Actions**: Click on stars 1-5, verify visual state changes
- **Acceptance criteria**:
  - Stars are interactive and highlight on hover/click
  - Selected rating value updates correctly

#### 7.3 – Test Eval Dashboard
- **Actions**: Navigate to or open the eval dashboard, verify it displays feedback metrics from real data
- **Acceptance criteria**:
  - Dashboard shows total feedback count, average rating
  - Category breakdown is visible
  - Learning insights section renders
  - Data comes from real Feedback module backed by PostgreSQL

#### 7.4 – Test Feedback Summary Display
- **Actions**: View feedback summary for a project
- **Acceptance criteria**:
  - Summary statistics are displayed from real data
  - Empty state is handled when no feedback exists

### Epic 8: Cross-Cutting Concerns Testing
Goal: Test theme, navigation, error handling, and UI polish

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 8.1 | Test dark/light mode toggle | Test | Shared | S | 2.1 | ⬚ |
| 8.2 | Test navigation between all pages | Test | Shared | S | 2.1 | ⬚ |
| 8.3 | Test toast notification display | Test | Shared | S | 3.2 | ⬚ |
| 8.4 | Test 401 auto-logout behavior | Test | Shared | S | 2.1 | ⬚ |
| 8.5 | Test responsive layout and header | Test | Shared | S | 2.1 | ⬚ |

#### 8.1 – Test Dark/Light Mode Toggle
- **Actions**: Click the theme toggle button, verify visual change
- **Acceptance criteria**:
  - Toggle button is visible in header
  - Clicking toggles between dark and light modes
  - `data-theme` attribute changes on document element
  - Theme persists in localStorage

#### 8.2 – Test Navigation Between All Pages
- **Actions**: Click through all nav links: Dashboard, Organizations, Subscriptions, Diagram
- **Acceptance criteria**:
  - All nav links are clickable and route correctly
  - Active page is visually indicated
  - No broken routes or 404s

#### 8.3 – Test Toast Notifications
- **Actions**: Trigger an action that shows a toast (e.g., create org), verify toast appears and auto-dismisses
- **Acceptance criteria**:
  - Toast appears on success/error actions
  - Toast auto-dismisses after ~4 seconds
  - Toast can be manually closed

#### 8.4 – Test 401 Auto-Logout Behavior
- **Actions**: Clear the auth token from localStorage while on a protected page, trigger an API call, verify redirect to login
- **Acceptance criteria**:
  - Expired/invalid token triggers redirect to `/login`
  - User session is cleared
  - Real backend returns 401 which frontend handles

#### 8.5 – Test Responsive Layout and Header
- **Actions**: Verify header shows user email, nav links, sign out button
- **Acceptance criteria**:
  - User email displayed in header
  - All navigation links present
  - Command palette area renders
  - Layout is properly structured

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Docker daemon not running | Medium | High | Start Docker daemon; it's required — no fallback to in-memory DBs |
| R2 | Docker Compose build fails (missing .NET SDK in image, npm issues) | Medium | High | Use pre-built images if available; fix Dockerfile issues; check build logs |
| R3 | PostgreSQL container fails health check | Low | High | Check logs, verify .env credentials match docker-compose.yml |
| R4 | Backend fails to connect to PostgreSQL | Medium | High | Ensure connection strings in appsettings.json use `Host=postgres` (Docker network name), verify credentials |
| R5 | Playwright Chromium missing system dependencies | Medium | High | Install via `npx playwright install --with-deps chromium` |
| R6 | CORS issues between frontend and backend | Low | Medium | Backend already configured for `http://localhost:3000` origin |
| R7 | Seeded data not sufficient for all test scenarios | Medium | Low | Tests will create additional data (register new users, create orgs/projects) as part of the flow |
| R8 | Ollama/AI features unavailable locally | High | Low | Skip AI-dependent features (aggregate learnings, architecture analysis); test UI components without AI backend |
| R9 | Port conflicts (3000, 5000, 5432 already in use) | Low | Medium | Kill existing processes or adjust Docker port mappings |
| R10 | EF Core migrations fail on real PostgreSQL | Low | High | Check migration files, verify schema compatibility, inspect backend container logs |

### Critical Path
1.1 → 1.2 → 1.3 → 1.6 → 2.1 → 3.1 → 3.2 → 3.3 → 5.2 → 6.1 → 6.2 → 7.1 → 8.2

### Estimated Total Effort
- S tasks: 23 × ~30 min = ~11.5 h
- M tasks: 6 × ~2.5 h = ~15 h
- L tasks: 0
- XL tasks: 0
- **Total: ~26.5 hours**

**Note**: With Playwright MCP automation driving the browser, actual execution time per test is 1-3 minutes. The estimates above reflect setup + authoring + verification. The full test suite execution should complete in under 1 hour once Docker Compose is running.
