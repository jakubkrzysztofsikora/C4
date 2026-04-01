# Progress: OptimizeDiagramRendering
Scope: Refactor
Created: 2026-04-01
Last Updated: 2026-04-01
Status: Not Started

## Current Focus
Planning complete – ready to start with Epic 0 (Agent Teams + Visual QA setup)

## Task Progress

### Epic 0: Infrastructure & Agent Teams Setup
- [ ] 0.1 – Enable CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS in settings.json
- [ ] 0.2 – Create visual-qa agent definition with Playwright MCP tools
- [ ] 0.3 – Verify agent team orchestration with a dry-run test (incl. visual-qa teammate)

### Epic 1: React Rendering Optimizations (Quick Wins) — Agent Team 1
- [ ] 1.1 – Memoize ServiceNode and GroupNode components with React.memo (Teammate A)
- [ ] 1.2 – Memoize node/edge array transformations in DiagramCanvas with useMemo (Teammate A)
- [ ] 1.3 – Enable onlyRenderVisibleElements for large graphs (Teammate A)
- [ ] 1.4 – Add snapToGrid to reduce drag-induced re-renders (Teammate A)
- [ ] 1.5 – Stabilize MiniMap nodeColor callback with useCallback (Teammate A)
- [ ] 1.6 – Write rendering performance tests with large mock datasets (Teammate C: test-generator)
- [ ] 1.7 – Visual QA: validate memoization, measure DOM counts and FPS via Playwright (Teammate D: visual-qa)

### Epic 2: Layout Computation Offloading — Agent Team 1
- [ ] 2.1 – Switch ELK import from bundled to web worker build (Teammate B)
- [ ] 2.2 – Add layout loading indicator during computation (Teammate B)
- [ ] 2.3 – Verify web worker layout with large graphs (Teammate C: test-generator)

### Epic 3: Level-of-Detail (LOD) Rendering — Agent Team 2
- [ ] 3.1 – Implement zoom-aware adaptive ServiceNode rendering (Teammate A: react-writer)
- [ ] 3.2 – Create compact and dot node CSS styles (Teammate A: react-writer)
- [ ] 3.3 – Write LOD rendering tests (Teammate D: test-generator)
- [ ] 3.4 – Visual QA: verify LOD tiers at each zoom level, validate API compression (Teammate E: visual-qa)

### Epic 4: Backend Data Optimization — Agent Team 2
- [ ] 4.1 – Add server-side graph summary endpoint (Teammate B: csharp-writer)
- [ ] 4.2 – Implement response compression for graph payloads (Teammate C: csharp-writer)
- [ ] 4.3 – Add node count metadata to graph response (Teammate B: csharp-writer)
- [ ] 4.4 – Optimize GetGraphHandler projection to reduce allocations (Teammate B: csharp-writer)
- [ ] 4.5 – Write backend performance tests for large graphs (Teammate D: test-generator)

### Epic 5: Frontend Data Layer Optimization — Agent Team 3
- [ ] 5.1 – Optimize useDiagram filtering pipeline with indexed lookups (Teammate A: react-writer)
- [ ] 5.2 – Implement progressive node rendering for large graphs (Teammate A: react-writer)
- [ ] 5.3 – Add graph size-aware rendering strategy selection (Teammate B: react-writer)
- [ ] 5.4 – Write data layer optimization tests (Teammate C: test-generator)

### Epic 6: Edge Rendering Optimization — Agent Team 3
- [ ] 6.1 – Simplify edge rendering at low zoom levels (Teammate B: react-writer)
- [ ] 6.2 – Reduce edge label rendering for dense graphs (Teammate B: react-writer)
- [ ] 6.3 – Write edge rendering tests (Teammate C: test-generator)
- [ ] 6.4 – Visual QA: validate edge simplification, progressive rendering, filter FPS (Teammate D: visual-qa)

### Epic 7: Collapsible Node Groups (C4 Hierarchy) — Agent Team 4
- [ ] 7.1 – Add expand/collapse interaction to GroupNode (Teammate A: react-writer)
- [ ] 7.2 – Track collapsed state in diagram state (Teammate A: react-writer)
- [ ] 7.3 – Filter collapsed children from layout computation (Teammate B: react-writer)
- [ ] 7.4 – Write collapsible group tests (Teammate C: test-generator)
- [ ] 7.5 – Visual QA: validate collapse/expand UX, node count reduction, edge rerouting (Teammate D: visual-qa)

## Agent Teams Execution Order
1. Epic 0: Sequential setup (single agent)
2. **Team 1**: Epics 1 + 2 (4 teammates: react-writer x2 + test-generator + visual-qa) → verification gate
3. **Team 2**: Epics 3 + 4 (5 teammates: react-writer + csharp-writer x2 + test-generator + visual-qa) → verification gate
4. **Team 3**: Epics 5 + 6 (4 teammates: react-writer x2 + test-generator + visual-qa) → verification gate
5. **Team 4**: Epic 7 (4 teammates: react-writer x2 + test-generator + visual-qa) → final verification

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-04-01 | Initial plan created | – | – |
| 2026-04-01 | Added Epic 0 and Agent Teams execution strategy | Enable experimental Agent Teams feature for parallel implementation | +2 tasks, ~1h; ~2x wall-clock speedup |
| 2026-04-01 | Added visual-qa agent and QA tasks to every team | Each team needs a QA teammate using Playwright MCP for browser-based visual validation and performance measurement | +5 QA tasks (1.7, 3.4, 6.4, 7.5, 0.2), +1 agent definition; visual QA runs in parallel with implementation |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-04-01 | Stay with React Flow rather than migrating to WebGL renderer | Sufficient for 1000-2000 node range with optimizations |
| 2026-04-01 | Use ELK web worker build rather than custom wrapper | Official worker build with identical API, zero-risk change |
| 2026-04-01 | Implement LOD with three zoom tiers (dot/compact/full) | Simpler to implement, test, and maintain than continuous scaling |
| 2026-04-01 | Defer Zustand migration to future work | useState/useMemo adequate when properly memoized |
| 2026-04-01 | Use Agent Teams for cross-layer parallelization | Peer messaging enables frontend/backend coordination without blocking |
| 2026-04-01 | Limit teams to 3-5 teammates each | Token cost scales linearly; 3-5 provides parallelism without excess |
| 2026-04-01 | Add visual-qa agent using Playwright MCP to every team | Browser-based QA catches rendering issues that unit tests miss (DOM counts, FPS, visual regressions, console errors); runs in parallel with implementation as a teammate |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
