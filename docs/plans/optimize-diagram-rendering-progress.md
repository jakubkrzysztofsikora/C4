# Progress: OptimizeDiagramRendering
Scope: Refactor
Created: 2026-04-01
Last Updated: 2026-04-01
Status: Not Started

## Current Focus
Planning complete (reviewed by React expert + web architect) – ready to start with Epic 0

## Task Progress

### Epic 0: Infrastructure & Agent Teams Setup
- [ ] 0.1 – Enable CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS in settings.json
- [ ] 0.2 – Create visual-qa agent definition with Playwright MCP tools
- [ ] 0.3 – Verify agent team orchestration with a dry-run test (incl. visual-qa teammate)

### Epic 1: React Rendering Optimizations — Team 1, Teammate A
- [ ] 1.1 – Memoize ServiceNode/GroupNode with React.memo + memoize title tooltip string (depends on 1.2)
- [ ] 1.2 – Memoize node/edge array transformations in DiagramCanvas with useMemo
- [ ] 1.3 – Enable onlyRenderVisibleElements for large graphs (500+ nodes)
- [ ] 1.4 – Add CSS containment (`contain: layout style paint`) to node classes
- [ ] 1.5 – Stabilize MiniMap nodeColor callback with useCallback
- [ ] 1.6 – Write performance tests — data transform timing only, not jsdom render (Teammate C)
- [ ] 1.7 – Visual QA: validate memoization, DOM counts, FPS, MiniMap culling (Teammate D)

### Epic 2: Layout Computation Offloading — Team 1, Teammate B
- [ ] 2.1 – Switch ELK to web worker build with concrete Vite setup + bundle size measurement
- [ ] 2.2 – Add layout loading indicator during computation (Teammate A, same as Epic 1)
- [ ] 2.3 – Verify web worker layout with large graphs (Teammate C)

### Epic 3: Level-of-Detail (LOD) Rendering — Team 2, Teammate A
- [ ] 3.1 – Implement zoom-aware LOD using `useStore` discretized selector (NOT useViewport!)
- [ ] 3.2 – Create compact and dot node CSS styles
- [ ] 3.3 – Write LOD rendering tests (Teammate D)
- [ ] 3.4 – Visual QA: verify LOD tiers at each zoom level, validate API compression (Teammate E)

### Epic 4: Backend Data Optimization — Team 2, Teammates B + C
- [ ] 4.1 – Add EF Core projection query to avoid loading full aggregate + all snapshots (Teammate B)
- [ ] 4.2 – Add server-side graph summary endpoint (Teammate B)
- [ ] 4.3 – Implement response compression for graph payloads (Teammate C)
- [ ] 4.4 – Add ETag/conditional-GET for graph endpoint (Teammate B)
- [ ] 4.5 – Add node count metadata to graph response (Teammate B)
- [ ] 4.6 – Optimize GetGraphHandler projection to reduce allocations (Teammate B)
- [ ] 4.7 – Switch ELK edge routing to POLYLINE for graphs over 500 nodes (Team 2, Teammate A)
- [ ] 4.8 – Add SignalR differential updates instead of full graph re-fetch (Team 3, Teammate B)
- [ ] 4.9 – Write backend performance tests for large graphs (Teammate D)

### Epic 5: Frontend Data Layer Optimization — Team 3, Teammate A
- [ ] 5.1 – Optimize useDiagram filtering pipeline + mapGraphDtoToDiagramData allocation storm
- [ ] 5.2 – Implement progressive node rendering using useDeferredValue (layout-first, then reveal)
- [ ] 5.3 – Add graph size-aware rendering strategy selection
- [ ] 5.4 – Write data layer optimization tests (Teammate C)

### Epic 6: Edge Rendering Optimization — Team 3, Teammate A
- [ ] 6.1 – Simplify edge rendering at low zoom levels
- [ ] 6.2 – Reduce edge label rendering for dense graphs
- [ ] 6.3 – Write edge rendering tests (Teammate C)
- [ ] 6.4 – Visual QA: validate edge simplification, progressive rendering, filter FPS (Teammate D)

### Epic 7: Collapsible Node Groups — Team 4 (simplified to 3 teammates)
- [ ] 7.1 – Add expand/collapse interaction to GroupNode (Teammate A)
- [ ] 7.2 – Track collapsed state in diagram state (Teammate A)
- [ ] 7.3 – Filter collapsed children from layout computation (Teammate A, sequential after 7.2)
- [ ] 7.4 – Write collapsible group tests (Teammate B: test-generator)
- [ ] 7.5 – Visual QA: validate collapse/expand UX, node count reduction, edge rerouting (Teammate C)

## Agent Teams Execution Order
1. Epic 0: Sequential setup (single agent)
2. **Team 1**: Epics 1 + 2 (4 teammates) — Teammate A owns ALL DiagramCanvas.tsx edits → verification gate
3. **Team 2**: Epics 3 + 4 (5 teammates) — frontend LOD ∥ backend optimization → verification gate
4. **Team 3**: Epics 5 + 6 + task 4.8 (4 teammates) — data layer + edges + SignalR delta → verification gate
5. **Team 4**: Epic 7 (3 teammates, simplified from 4) — sequential collapse feature → final verification

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-04-01 | Initial plan created | – | – |
| 2026-04-01 | Added Epic 0 and Agent Teams execution strategy | Enable experimental Agent Teams feature | +2 tasks, ~1h |
| 2026-04-01 | Added visual-qa agent and QA tasks to every team | Browser-based rendering validation | +5 QA tasks |
| 2026-04-01 | Incorporated React expert + web architect review findings | Expert review identified critical issues | +4 new tasks (4.1, 4.4, 4.7, 4.8), replaced task 1.4, expanded multiple task descriptions, fixed team compositions |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-04-01 | Stay with React Flow rather than migrating to WebGL | Sufficient for 1000-2000 node range with optimizations |
| 2026-04-01 | Use ELK web worker build with concrete Vite setup | Official worker build, but requires Vite-specific configuration |
| 2026-04-01 | LOD with three zoom tiers using `useStore` discretized selector | `useViewport()` would cause all nodes to re-render on every zoom frame — `useStore` with enum selector only re-renders on tier change |
| 2026-04-01 | Defer Zustand migration to future work | useState/useMemo adequate when properly memoized |
| 2026-04-01 | Use Agent Teams with single DiagramCanvas.tsx owner per team | Prevents merge conflicts on the most-edited file |
| 2026-04-01 | Simplify Team 4 to 3 teammates | Tasks 7.1→7.2→7.3 are sequential; second react-writer adds cost without speedup |
| 2026-04-01 | Remove snapToGrid (old 1.4), replace with CSS containment | Users don't drag ELK-positioned nodes; snapToGrid has zero impact |
| 2026-04-01 | Scope jsdom tests to data transform timing only | jsdom doesn't trigger real layout/paint; render perf baselines from Playwright |
| 2026-04-01 | Add EF Core projection + ETag as backend priorities | Full aggregate loading and redundant polling are top backend bottlenecks |
| 2026-04-01 | Use `useDeferredValue` for progressive render instead of manual RAF | React 19 scheduler integration, cleaner than manual requestAnimationFrame |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
