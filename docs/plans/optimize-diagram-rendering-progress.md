# Progress: OptimizeDiagramRendering
Scope: Refactor
Created: 2026-04-01
Last Updated: 2026-04-01
Status: Not Started

## Current Focus
Planning complete – ready to start with Epic 0 (Agent Teams setup)

## Task Progress

### Epic 0: Infrastructure & Agent Teams Setup
- [ ] 0.1 – Enable CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS in settings.json
- [ ] 0.2 – Verify agent team orchestration with a dry-run test

### Epic 1: React Rendering Optimizations (Quick Wins) — Agent Team 1, Teammate A
- [ ] 1.1 – Memoize ServiceNode and GroupNode components with React.memo
- [ ] 1.2 – Memoize node/edge array transformations in DiagramCanvas with useMemo
- [ ] 1.3 – Enable onlyRenderVisibleElements for large graphs (500+ nodes)
- [ ] 1.4 – Add snapToGrid to reduce drag-induced re-renders
- [ ] 1.5 – Stabilize MiniMap nodeColor callback with useCallback
- [ ] 1.6 – Write rendering performance tests with large mock datasets (Team 1, Teammate C)

### Epic 2: Layout Computation Offloading — Agent Team 1, Teammate B
- [ ] 2.1 – Switch ELK import from bundled to web worker build
- [ ] 2.2 – Add layout loading indicator during computation
- [ ] 2.3 – Verify web worker layout with large graphs

### Epic 3: Level-of-Detail (LOD) Rendering — Agent Team 2, Teammate A
- [ ] 3.1 – Implement zoom-aware adaptive ServiceNode rendering
- [ ] 3.2 – Create compact and dot node CSS styles
- [ ] 3.3 – Write LOD rendering tests (Team 2, Teammate D)

### Epic 4: Backend Data Optimization — Agent Team 2, Teammates B + C
- [ ] 4.1 – Add server-side graph summary endpoint for initial overview (Teammate B)
- [ ] 4.2 – Implement response compression for graph payloads (Teammate C)
- [ ] 4.3 – Add node count metadata to graph response (Teammate B)
- [ ] 4.4 – Optimize GetGraphHandler projection to reduce allocations (Teammate B)
- [ ] 4.5 – Write backend performance tests for large graphs (Team 2, Teammate D)

### Epic 5: Frontend Data Layer Optimization — Agent Team 3, Teammate A
- [ ] 5.1 – Optimize useDiagram filtering pipeline with indexed lookups
- [ ] 5.2 – Implement progressive node rendering for large graphs
- [ ] 5.3 – Add graph size-aware rendering strategy selection (Team 3, Teammate B)
- [ ] 5.4 – Write data layer optimization tests (Team 3, Teammate C)

### Epic 6: Edge Rendering Optimization — Agent Team 3, Teammate B
- [ ] 6.1 – Simplify edge rendering at low zoom levels
- [ ] 6.2 – Reduce edge label rendering for dense graphs
- [ ] 6.3 – Write edge rendering tests (Team 3, Teammate C)

### Epic 7: Collapsible Node Groups (C4 Hierarchy) — Agent Team 4
- [ ] 7.1 – Add expand/collapse interaction to GroupNode (Teammate A)
- [ ] 7.2 – Track collapsed state in diagram state (Teammate A)
- [ ] 7.3 – Filter collapsed children from layout computation (Teammate B, depends on 7.2)
- [ ] 7.4 – Write collapsible group tests (Teammate C)

## Agent Teams Execution Order
1. Epic 0: Sequential setup (single agent)
2. **Team 1**: Epics 1 + 2 in parallel (3 teammates) → verification gate
3. **Team 2**: Epics 3 + 4 in parallel (4 teammates) → verification gate
4. **Team 3**: Epics 5 + 6 in parallel (3 teammates) → verification gate
5. **Team 4**: Epic 7 with dependencies (3 teammates) → final verification

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-04-01 | Initial plan created | – | – |
| 2026-04-01 | Added Epic 0 (Agent Teams setup) and Agent Teams execution strategy | Enable experimental Agent Teams feature for parallel cross-layer implementation | +2 tasks, ~1h; estimated ~2x wall-clock speedup via parallelization |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-04-01 | Stay with React Flow rather than migrating to WebGL renderer (Sigma.js/Reagraph) | React Flow's SVG-based rendering is sufficient for the 1000-2000 node target range when combined with viewport culling, LOD, and memoization. WebGL migration would be a larger effort with diminishing returns below 5000 nodes. |
| 2026-04-01 | Use ELK web worker build rather than custom Web Worker wrapper | ELK.js provides an official worker build with identical API, making this a single-line change with zero risk |
| 2026-04-01 | Implement LOD with three zoom tiers (dot/compact/full) rather than continuous scaling | Three discrete tiers are simpler to implement, test, and maintain; continuous scaling adds complexity without proportional benefit |
| 2026-04-01 | Defer Zustand migration to future work | Current useState/useMemo approach is adequate when properly memoized; Zustand adds a dependency for marginal gain at current scale |
| 2026-04-01 | Use Agent Teams for cross-layer parallelization, subagents for single-layer tasks | Agent Teams provide peer-to-peer messaging and shared task coordination that subagents lack — critical when frontend and backend teammates need to agree on API contracts (e.g., Team 2: LOD frontend needs to know summary endpoint shape from backend) |
| 2026-04-01 | Limit teams to 3-4 teammates each | Token cost scales linearly with team size; 3-4 provides parallelism without excessive cost |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
