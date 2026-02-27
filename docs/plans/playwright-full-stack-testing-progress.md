# Progress: Playwright Full-Stack Testing via MCP
Scope: FeatureSet
Created: 2026-02-27
Last Updated: 2026-02-27
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: Environment Setup & Infrastructure
- [ ] 1.1 – Create .env and start Docker Compose (PostgreSQL + backend + frontend)
- [ ] 1.2 – Verify all Docker services are healthy
- [ ] 1.3 – Verify database migrations ran and seed data exists
- [ ] 1.4 – Install Playwright and browser dependencies
- [ ] 1.5 – Configure Playwright MCP server (.mcp.json)
- [ ] 1.6 – Verify full stack connectivity via browser

### Epic 2: Authentication Testing
- [ ] 2.1 – Test login with seeded demo credentials
- [ ] 2.2 – Test login with invalid credentials
- [ ] 2.3 – Test new user registration (persisted to PostgreSQL)
- [ ] 2.4 – Test logout and protected route redirect
- [ ] 2.5 – Test registration validation

### Epic 3: Organization & Project Management Testing
- [ ] 3.1 – Test navigate to Organizations page
- [ ] 3.2 – Test create new organization
- [ ] 3.3 – Test create project within organization
- [ ] 3.4 – Test organization page states

### Epic 4: Subscription Wizard Testing
- [ ] 4.1 – Test navigate to Subscriptions page
- [ ] 4.2 – Test connect Azure subscription
- [ ] 4.3 – Test subscription connected state

### Epic 5: Dashboard Testing
- [ ] 5.1 – Test dashboard page load and layout
- [ ] 5.2 – Test project graph loading by ID
- [ ] 5.3 – Test dashboard empty and error states

### Epic 6: Diagram Page Testing
- [ ] 6.1 – Test diagram page navigation and load
- [ ] 6.2 – Test diagram canvas rendering with XyFlow
- [ ] 6.3 – Test C4 level filter selector
- [ ] 6.4 – Test diagram search/filter functionality
- [ ] 6.5 – Test diagram zoom controls
- [ ] 6.6 – Test diagram export (SVG/PDF)
- [ ] 6.7 – Test diagram with seeded project ID via URL param

### Epic 7: Feedback System Testing
- [ ] 7.1 – Test feedback panel component
- [ ] 7.2 – Test star rating interaction
- [ ] 7.3 – Test eval dashboard page
- [ ] 7.4 – Test feedback summary and learnings display

### Epic 8: Cross-Cutting Concerns Testing
- [ ] 8.1 – Test dark/light mode toggle
- [ ] 8.2 – Test navigation between all pages
- [ ] 8.3 – Test toast notification display
- [ ] 8.4 – Test 401 auto-logout behavior
- [ ] 8.5 – Test responsive layout and header

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-27 | Initial plan created | – | – |
| 2026-02-27 | Changed from in-memory DBs to Docker Compose with real PostgreSQL | User requirement: full stack with real services, no mocks/stubs | All services run via Docker Compose; real PostgreSQL for all modules |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-27 | Run full stack via Docker Compose with real PostgreSQL | User explicitly required no in-memory DBs, no stubs — real full stack only |
| 2026-02-27 | Use Playwright MCP for browser automation | Direct requirement — all tests driven through real browser via Playwright MCP |
| 2026-02-27 | Test against seeded demo data from SeedDataService | SeedDataService creates demo user (demo@c4.local/Password123!), org, project, and view preset on startup |
| 2026-02-27 | Skip Ollama/AI-dependent features | Ollama LLM not available locally; test UI components but not AI-generated results |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
