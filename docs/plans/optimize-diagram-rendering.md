## Plan: OptimizeDiagramRendering
Scope: Refactor
Created: 2026-04-01
Status: Draft

### Overview
Optimize the C4 diagram rendering pipeline to maintain 60fps interactive performance with 1000+ resource nodes. The current implementation uses React Flow (SVG/DOM-based) with ELK.js layout on the main thread, unmemoized custom node components, and no viewport culling or level-of-detail rendering. This plan addresses bottlenecks across the full stack: backend data preparation, layout computation, React rendering, and interactive performance.

### Success Criteria
- [ ] Diagram with 1000 nodes renders initial layout in under 3 seconds
- [ ] Pan/zoom interactions maintain 60fps with 1000+ nodes
- [ ] ELK layout computation does not block the main thread
- [ ] Nodes outside the viewport are not rendered to the DOM
- [ ] Zoomed-out views (< 0.3 zoom) render simplified node representations
- [ ] Backend supports paginated/chunked graph responses for 2000+ node graphs
- [ ] No regressions in existing diagram functionality (filters, overlays, diff, export)
- [ ] All changes verified with performance profiling (Chrome DevTools / Lighthouse)
- [ ] Agent Teams experimental feature enabled and used for parallel cross-layer implementation

### Agent Teams Execution Strategy

This plan leverages the **Claude Code Agent Teams** experimental feature (`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1`) to parallelize cross-layer implementation work. Agent teams coordinate multiple independent Claude Code sessions working simultaneously, with shared task lists, peer-to-peer messaging, and file locking to prevent conflicts.

**Configuration**: Enabled via `env.CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS` in `.claude/settings.json`.

#### Team Compositions

**DiagramCanvas.tsx Ownership Rule**: This file is touched by tasks across Epics 1, 2, 3, 5, and 6. To prevent merge conflicts, **all DiagramCanvas.tsx edits are assigned to a single named teammate per team**, and teams are serialized (Team 1 finishes all DiagramCanvas changes before Team 2 starts its own).

**Team 1: Quick Wins Blitz** (Epics 1 + 2 — independent file owners)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 1.1, 1.2, 1.3, 1.4, 1.5, 2.2 — ALL DiagramCanvas.tsx + GroupNode.tsx + diagram.css changes
├── Teammate B (react-writer): Task 2.1 — useElkLayout.ts web worker switch + Vite config
├── Teammate C (test-generator): Task 1.6 — Performance test suite (can start once A finishes 1.2)
└── Teammate D (visual-qa): Task 1.7 — Browser-based QA: measure DOM node counts, FPS, MiniMap culling interaction
```
- **Why teams over subagents**: A and B work on different files simultaneously (DiagramCanvas vs useElkLayout), C writes tests informed by both via peer messaging, D validates in real browser.
- **File safety**: A owns DiagramCanvas.tsx + GroupNode.tsx + diagram.css, B owns useElkLayout.ts + vite.config.ts, C owns __tests__/, D is read-only.
- **Note**: Task 2.2 (loading indicator) moved to Teammate A since it modifies DiagramCanvas.tsx.

**Team 2: Full-Stack LOD + Backend** (Epics 3 + 4 — frontend and backend in parallel)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 3.1, 3.2, 4.7 — LOD rendering in DiagramCanvas.tsx + CSS + ELK routing switch
├── Teammate B (csharp-writer): Tasks 4.1, 4.2, 4.4, 4.5, 4.6 — EF Core projection + summary endpoint + ETag + node count + handler optimization
├── Teammate C (csharp-writer): Task 4.3 — Response compression in Host
├── Teammate D (test-generator): Tasks 3.3, 4.9 — LOD tests + backend performance tests
└── Teammate E (visual-qa): Task 3.4 — Browser-based QA: verify LOD tiers, API compression, network perf
```
- **Why teams over subagents**: Frontend LOD (A) and backend optimization (B/C) are fully independent layers. D writes tests for both via peer messaging, E validates visually.
- **File safety**: A owns DiagramCanvas.tsx + diagram.css + useElkLayout.ts (routing only), B owns Graph.Application/ + Graph.Infrastructure/ + Graph.Api/, C owns Host/, D owns test files, E is read-only.

**Team 3: Data Layer + Edge Optimization** (Epics 5 + 6)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 5.1, 5.2, 6.1, 6.2, 5.3 — ALL frontend changes: useDiagram.ts + DiagramCanvas.tsx edges + new hooks
├── Teammate B (csharp-writer): Task 4.8 — SignalR differential updates (backend + useDiagram.ts SignalR handler)
├── Teammate C (test-generator): Tasks 5.4, 6.3 — Data layer + edge rendering tests
└── Teammate D (visual-qa): Task 6.4 — Browser-based QA: edge simplification, progressive rendering, filter FPS
```
- **Why teams over subagents**: A consolidates ALL frontend file edits (resolving the DiagramCanvas.tsx ownership conflict between old Teams 3's A and B). B works independently on backend SignalR delta support.
- **File safety**: A owns all web/ files (useDiagram.ts, DiagramCanvas.tsx, new hooks), B owns Visualization.Api/Hubs/ + SignalRDiagramNotifier.cs + useSignalR.ts, C owns test files, D is read-only.

**Team 4: Collapsible Groups** (Epic 7 — simplified to 3 teammates since tasks are sequential)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 7.1, 7.2, 7.3 — All GroupNode + collapsed state + ELK filtering (sequential)
├── Teammate B (test-generator + change-verifier): Task 7.4 — Tests + full verification (starts after 7.2)
└── Teammate C (visual-qa): Task 7.5 — Browser-based QA: collapse/expand UX, node count reduction, edge rerouting
```
- **Why 3 instead of 4**: Tasks 7.1→7.2→7.3 are tightly sequential (each depends on the previous). A second react-writer waiting for 7.2 adds cost without speedup. Single-owner A does all three sequentially; B writes tests in parallel starting after 7.2; C validates in browser after 7.1.
- **File safety**: A owns GroupNode.tsx + useDiagram.ts (collapsed state) + useElkLayout.ts, B owns test files, C is read-only.

#### Sequential Quality Gates Between Teams

After each team completes, run a **verification pipeline** before starting the next team. The visual-qa teammate runs as part of the team (not the gate), providing real-time browser-based validation during implementation:

```
Team 1 completes (visual-qa teammate already validated during implementation)
  → build-runner: "Build solution and run all tests"
  → change-verifier: "Verify Epic 1 + 2 changes"
Team 2 completes (visual-qa teammate already validated LOD + compression)
  → build-runner: "Build solution and run all tests"
  → arch-validator: "Validate Graph module boundaries after 4.1/4.4 changes"
Team 3 completes (visual-qa teammate already validated edges + progressive render)
  → build-runner: "Build solution and run all tests"
  → code-quality-reviewer: "Review data layer changes for standards compliance"
Team 4 completes (visual-qa teammate already validated collapse/expand UX)
  → build-runner: "Build solution and run all tests"
  → pr-reviewer: "Full PR review of all optimization changes"
```

#### When NOT to Use Agent Teams

- Tasks within the same file (e.g., multiple edits to DiagramCanvas.tsx) — use a single teammate
- Simple one-line changes (e.g., 2.1 ELK import swap) — subagent or direct edit is faster
- Sequential verification tasks (build-runner, change-verifier) — these are single-agent operations

#### Estimated Speedup

| Approach | Estimated Wall Clock |
|----------|---------------------|
| Sequential (no teams) | ~38 hours |
| With Agent Teams (4 teams) | ~18 hours (~2x speedup) |

The speedup comes from parallelizing frontend/backend work (Team 2), parallelizing data/rendering layers (Team 3), and running tests alongside implementation (all teams).

### Epic 0: Infrastructure & Agent Teams Setup
Goal: Enable the Agent Teams experimental feature and verify the development environment is ready

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 0.1 | Enable CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS in settings.json | Infrastructure | .claude | S | – | ⬚ |
| 0.2 | Create visual-qa agent definition with Playwright MCP tools | Infrastructure | .claude | S | – | ⬚ |
| 0.3 | Verify agent team orchestration with a dry-run test | Spike | .claude | S | 0.1, 0.2 | ⬚ |

#### 0.1 – Enable Agent Teams Feature Flag
- **Files to modify**: `.claude/settings.json`
- **Test plan (TDD)**:
  - Verify the `env` block contains `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS: "1"`
  - Verify existing permissions are not affected
- **Acceptance criteria**:
  - `env.CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS` set to `"1"` in `.claude/settings.json`
  - Existing `permissions.allow` and `permissions.deny` arrays unchanged
  - Claude Code recognizes the feature flag on next session start

#### 0.2 – Create Visual QA Agent Definition
- **Files to create**: `.claude/agents/visual-qa.md`
- **Test plan (TDD)**:
  - Verify agent definition has correct YAML frontmatter (name, description, tools, model, color)
  - Verify all Playwright MCP tools are listed in the tools field
  - Verify agent instructions include QA protocol for performance measurement
- **Acceptance criteria**:
  - `visual-qa` agent defined with Playwright MCP tools (browser_navigate, browser_snapshot, browser_take_screenshot, browser_evaluate, browser_console_messages, browser_network_requests, browser_click, browser_hover, browser_drag, browser_run_code, etc.)
  - Agent instructions include protocols for: DOM node counting, FPS measurement, LOD verification, viewport culling validation, console error checking, network performance analysis
  - Agent can be invoked as a teammate in Agent Teams

#### 0.3 – Verify Agent Teams Dry-Run
- **Files to modify**: none (verification only)
- **Test plan (TDD)**:
  - Spawn a 3-teammate team with a trivial task:
    - Teammate A (react-writer): read DiagramCanvas.tsx and report line count
    - Teammate B (visual-qa): launch browser, navigate to localhost, take screenshot
  - Verify teammates can communicate via peer messaging
  - Verify file locking prevents simultaneous edits to the same file
- **Acceptance criteria**:
  - Agent team spawns successfully with both implementation and QA teammates
  - visual-qa teammate can access Playwright MCP tools
  - Teammates report back with results
  - No errors in team coordination

### Epic 1: React Rendering Optimizations (Quick Wins)
Goal: Eliminate unnecessary re-renders and DOM operations in the existing React Flow setup

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Memoize ServiceNode/GroupNode with React.memo + memoize title tooltip string | Refactor | web | S | 1.2 | ⬚ |
| 1.2 | Memoize node/edge array transformations in DiagramCanvas with useMemo | Refactor | web | S | – | ⬚ |
| 1.3 | Enable onlyRenderVisibleElements for large graphs (500+ nodes) | Feature | web | S | – | ⬚ |
| 1.4 | Add CSS containment (`contain: layout style paint`) to node classes | Refactor | web | S | – | ⬚ |
| 1.5 | Stabilize MiniMap nodeColor callback with useCallback | Refactor | web | S | – | ⬚ |
| 1.6 | Write rendering performance tests (data transform timing only, not jsdom render) | Test | web | M | 1.1 | ⬚ |
| 1.7 | Visual QA: validate memoization, measure DOM counts and FPS via Playwright | Test | web | M | 1.1, 1.2, 1.3 | ⬚ |

#### 1.1 – Memoize ServiceNode and GroupNode
- **Depends on**: 1.2 (React.memo is inert without useMemo stabilizing data object references)
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`, `web/src/features/diagram/components/GroupNode.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `ServiceNode_SameProps_DoesNotRerender`, `GroupNode_SameProps_DoesNotRerender`
  - Verify via React DevTools profiler that node re-renders drop to zero during pan/zoom
- **Acceptance criteria**:
  - `ServiceNode` wrapped in `React.memo` with stable reference
  - `GroupNode` wrapped in `React.memo`
  - `title` tooltip string (lines 56-74) memoized with `useMemo` inside ServiceNode to avoid 500+ string allocations per render
  - `nodeTypes` object remains defined at module level (already correct)
  - No visual or behavioral changes to existing rendering

#### 1.2 – Memoize Node/Edge Array Transformations
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `NodeArray_SameData_ReturnsSameReference`, `EdgeArray_SameData_ReturnsSameReference`
  - Verify React Flow does not re-initialize nodes/edges when data hasn't changed
- **Acceptance criteria**:
  - `groups`, `serviceNodes`, `nodes` array wrapped in `useMemo` with deps `[groupNodes, data.nodes, overlayMode]`
  - `edges` array wrapped in `useMemo` with deps `[data.edges]`
  - Referential equality maintained when inputs unchanged

#### 1.3 – Enable Conditional Viewport Culling
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `LargeGraph_EnablesViewportCulling`, `SmallGraph_DisablesViewportCulling`
  - Manual verification: with 1000 nodes, only ~50-100 DOM nodes exist at typical zoom
- **Acceptance criteria**:
  - `onlyRenderVisibleElements` prop set to `true` when `nodes.length > 500`
  - Performance improvement measurable via DOM node count reduction
  - No visual artifacts when panning across large diagrams

#### 1.4 – CSS Containment on Node Classes
- **Files to modify**: `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Visual QA verification that nodes render correctly with containment
  - Measure paint time reduction via Playwright performance API
- **Acceptance criteria**:
  - `.service-node`, `.service-node-compact`, `.group-node` classes include `contain: layout style paint`
  - Isolates paint/layout boundaries, reducing browser reflow cost during pan at 1000+ nodes
  - No visual regressions (verify `box-shadow` and `backdrop-filter` still render correctly)
  - Audit and remove any heavy CSS effects (`box-shadow`, `filter`) from node classes that trigger layer promotion

#### 1.5 – Stabilize MiniMap Callback
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `MiniMapNodeColor_SameOverlay_ReturnsSameCallback`
- **Acceptance criteria**:
  - `nodeColor` callback wrapped in `useCallback` with deps `[overlayMode]`
  - MiniMap does not re-render on unrelated state changes

#### 1.7 – Visual QA: Memoization & Rendering Baseline
- **Agent**: `visual-qa` (Playwright MCP)
- **Test plan**:
  - Launch dev server, navigate to diagram page with largest available project
  - Take baseline screenshots at zoom levels 0.1, 0.3, 0.5, 1.0
  - Measure DOM node count (`.react-flow__node` elements) before and after memoization
  - Measure FPS during 3-second pan interaction via `requestAnimationFrame` timing
  - Check browser console for React warnings, errors, or performance deprecations
  - Verify MiniMap renders correctly after callback stabilization — specifically check for blank areas when `onlyRenderVisibleElements` is active (known interaction issue)
  - Verify MiniMap shows all nodes colored correctly even when viewport culling hides off-screen nodes
- **Acceptance criteria**:
  - No visual regressions compared to pre-optimization screenshots
  - DOM node count reduced when `onlyRenderVisibleElements` is active (task 1.3)
  - MiniMap shows full graph overview without blank areas despite culling
  - FPS >= 30 during pan/zoom at default zoom (0.3)
  - Zero console errors

#### 1.6 – Performance Test Suite (Data Transform Timing)
- **Files to create**: `web/src/features/diagram/__tests__/DiagramPerformance.test.tsx`
- **Test plan (TDD)**:
  - Create mock datasets: 100, 500, 1000, 2000 nodes with proportional edges
  - Measure: data transformation time (mapGraphDtoToDiagramData), filter pipeline time, node/edge array creation time
  - Assert: data transform < 100ms for 1000 nodes, filter pipeline < 50ms for 1000 nodes
  - **Note**: Do NOT assert render timing in jsdom — jsdom does not trigger real browser layout/paint. Real render performance baselines come from visual-qa task 1.7 via Playwright.
  - Fakes/Fixtures: `createMockDiagramData(nodeCount: number)` builder
- **Acceptance criteria**:
  - Data transform and filter baselines established and documented
  - Tests scoped to JavaScript execution time, not browser rendering
  - Tests run in CI and fail on significant regressions
  - Bundle size delta measured and documented after ELK worker switch (task 2.1)

### Epic 2: Layout Computation Offloading
Goal: Move ELK layout computation off the main thread to prevent UI freezes

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Switch ELK import from bundled to web worker build | Refactor | web | S | – | ⬚ |
| 2.2 | Add layout loading indicator during computation | Feature | web | S | 2.1 | ⬚ |
| 2.3 | Verify web worker layout with large graphs | Test | web | M | 2.1 | ⬚ |

#### 2.1 – Switch ELK to Web Worker Build
- **Files to modify**: `web/src/features/diagram/hooks/useElkLayout.ts`, `web/vite.config.ts` (if needed)
- **Test plan (TDD)**:
  - Unit tests: `UseElkLayoutTests` – `Layout_LargeGraph_DoesNotBlockMainThread`, `Layout_ProducesIdenticalResults`
  - Verify main thread stays responsive during 1000-node layout
  - Verify worker teardown: no memory leaks on component unmount, no state updates after unmount
  - Measure bundle size delta before/after worker switch
- **Vite worker setup** (concrete steps, not "if needed"):
  - `elkjs/lib/elk-worker.js` requires a `workerUrl` pointing to a bundled worker script
  - In Vite, use `?worker` import syntax with a wrapper or configure `vite.config.ts` `optimizeDeps` to handle the worker bundle
  - Test both `npm run dev` (Vite dev server) and `npm run build` (production build)
  - If the official worker build fails with Vite, fall back to a custom `new Worker()` wrapper around `elk.bundled.js`
- **Acceptance criteria**:
  - Import changed from `elkjs/lib/elk.bundled.js` to `elkjs/lib/elk-worker.js` (or custom worker wrapper)
  - Layout results identical to bundled version
  - Main thread frame budget maintained during layout (no jank)
  - Both Vite dev and prod builds work correctly
  - Bundle size delta documented

#### 2.2 – Layout Loading Indicator
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `IsLayouting_ShowsLoadingIndicator`
- **Acceptance criteria**:
  - Shows a subtle loading overlay/spinner when `isLayouting` is true
  - Indicator disappears when layout completes
  - Does not interfere with existing ReactFlow rendering

#### 2.3 – Web Worker Layout Verification
- **Files to create**: `web/src/features/diagram/__tests__/useElkLayout.test.ts`
- **Test plan (TDD)**:
  - Test with graphs: 10, 100, 500, 1000, 2000 nodes
  - Verify: correct positions, group boundaries, edge routing
  - Verify: version tracking works (stale results ignored)
  - Fakes/Fixtures: `createMockElkInput(nodeCount)` builder
- **Acceptance criteria**:
  - All layout results correct across all sizes
  - Stale layout results properly discarded

### Epic 3: Level-of-Detail (LOD) Rendering
Goal: Render simplified node representations at low zoom levels to reduce DOM complexity and paint cost

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Implement zoom-aware adaptive ServiceNode rendering | Feature | web | M | 1.1 | ⬚ |
| 3.2 | Create compact and dot node CSS styles | Feature | web | S | 3.1 | ⬚ |
| 3.3 | Write LOD rendering tests | Test | web | S | 3.1 | ⬚ |
| 3.4 | Visual QA: verify LOD tiers at each zoom level, validate API compression | Test | web | M | 3.1, 3.2, 4.2 | ⬚ |

#### 3.1 – Adaptive ServiceNode with Zoom-Based LOD
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `ServiceNodeTests` – `ZoomBelow02_RendersDot`, `ZoomBetween02And05_RendersCompact`, `ZoomAbove05_RendersFull`
  - Fakes/Fixtures: mock `useStore` selector
- **CRITICAL IMPLEMENTATION NOTE**: Do NOT use `useViewport()` — it returns raw zoom float and triggers re-renders on every frame during pan/zoom, causing all 1000 nodes to re-render 60 times per second. Instead use `useStore` with a discretized LOD tier selector that only triggers re-renders when the tier actually changes:
  ```typescript
  type LodTier = 'dot' | 'compact' | 'full';
  const lodTier = useStore((s) => {
    const z = s.transform[2];
    if (z < 0.2) return 'dot';
    if (z < 0.5) return 'compact';
    return 'full';
  });
  ```
- **Acceptance criteria**:
  - LOD uses `useStore` selector returning `'dot' | 'compact' | 'full'` enum, NOT `useViewport()`
  - Re-renders only fire when the LOD tier changes (3 possible transitions), not on every zoom delta
  - zoom < 0.2: render colored dot (12x12px circle) with health color + Handles only
  - zoom 0.2-0.5: render compact node (icon + label + health badge) without metrics/tags
  - zoom > 0.5: render full detail node (current implementation)
  - Handles always present at all LOD levels for edge connections

#### 3.2 – LOD CSS Styles
- **Files to modify**: `web/src/features/diagram/diagram.css`
- **Test plan (TDD)**:
  - Visual verification at each zoom level
- **Acceptance criteria**:
  - `.service-node-dot` class: 12x12px circle, border-radius 50%, health-colored background
  - `.service-node-compact` class: reduced padding, single-line layout, no metrics section
  - CSS transitions for smooth LOD changes

#### 3.4 – Visual QA: LOD Tiers & API Compression
- **Agent**: `visual-qa` (Playwright MCP)
- **Test plan**:
  - Navigate to diagram page, zoom to < 0.2 and screenshot — verify `.service-node-dot` elements present
  - Zoom to 0.2-0.5 and screenshot — verify `.service-node-compact` elements present
  - Zoom to > 0.5 and screenshot — verify full `.service-node` elements present
  - Count DOM nodes at each zoom level to confirm LOD reduces element complexity
  - Check network requests to `/api/*/graph` for `Content-Encoding: gzip` or `br` headers
  - Compare response sizes before/after compression (if baseline available)
  - Verify no CSS transition glitches when crossing zoom thresholds
- **Acceptance criteria**:
  - Each LOD tier renders the correct CSS class at its zoom range
  - DOM node count at zoom < 0.2 is significantly lower than at zoom > 0.5
  - API responses include compression headers
  - No visual flickering or layout jumps during zoom transitions

#### 3.3 – LOD Tests
- **Files to create**: `web/src/features/diagram/__tests__/ServiceNodeLOD.test.tsx`
- **Test plan (TDD)**:
  - Test each zoom threshold renders the correct LOD level
  - Test edge Handles present at all zoom levels
  - Test health color correctly applied at all levels
- **Acceptance criteria**:
  - All three LOD levels covered
  - No missing Handles at any zoom level

### Epic 4: Backend Data Optimization
Goal: Reduce payload size and processing time for large graph responses

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Add EF Core projection query to avoid loading full aggregate + all snapshots | Refactor | Graph.Infrastructure | M | – | ⬚ |
| 4.2 | Add server-side graph summary endpoint for initial overview | Feature | Graph.Api | M | – | ⬚ |
| 4.3 | Implement response compression for graph payloads | Feature | Host | S | – | ⬚ |
| 4.4 | Add ETag/conditional-GET for graph endpoint to eliminate redundant polling | Feature | Graph.Api | M | – | ⬚ |
| 4.5 | Add node count metadata to graph response | Feature | Graph.Application | S | – | ⬚ |
| 4.6 | Optimize GetGraphHandler projection to reduce allocations | Refactor | Graph.Application | M | 4.1 | ⬚ |
| 4.7 | Switch ELK edge routing to POLYLINE for graphs over 500 nodes | Refactor | web | S | – | ⬚ |
| 4.8 | Add SignalR differential updates instead of full graph re-fetch | Feature | Graph, Visualization | L | – | ⬚ |
| 4.9 | Write backend performance tests for large graphs | Test | Graph.Tests | M | 4.1, 4.6 | ⬚ |

#### 4.1 – EF Core Projection Query
- **Files to modify**: `src/Modules/Graph/Graph.Infrastructure/Persistence/Repositories/ArchitectureGraphRepository.cs`, `src/Modules/Graph/Graph.Application/Ports/IArchitectureGraphRepository.cs`
- **Test plan (TDD)**:
  - Unit tests: `ArchitectureGraphRepositoryTests` – `GetByProjectId_ProjectsOnlyRequiredColumns`, `GetByProjectId_ExcludesUnrequestedSnapshots`
  - Fakes/Fixtures: In-memory EF Core DbContext
- **Acceptance criteria**:
  - New projection method `GetGraphForQueryAsync(projectId, snapshotId?)` that selects only required columns
  - When `snapshotId` is specified, loads only that single snapshot instead of all snapshots
  - When no `snapshotId`, does not load snapshots at all (they are only needed for snapshot queries)
  - Eliminates eager loading of full navigation properties via `.Include(g => g.Snapshots)` when not needed
  - Measured query time reduction documented for 5000-node graph with 20 snapshots

#### 4.2 – Graph Summary Endpoint
- **Files to create**: `src/Modules/Graph/Graph.Api/Endpoints/GetGraphSummaryEndpoint.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GetGraphSummaryQuery.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GetGraphSummaryHandler.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GraphSummaryDto.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphSummaryHandlerTests` – `Handle_ValidProject_ReturnsSummary`, `Handle_LargeGraph_ReturnsQuickly`
  - Module tests: `GetGraphSummaryEndpointTests` – `Get_ValidProject_Returns200WithSummary`
  - Fakes/Fixtures: `FakeArchitectureGraphRepository`
- **Acceptance criteria**:
  - `GET /api/projects/{id}/graph/summary` returns: totalNodes, totalEdges, nodesByLevel, nodesByServiceType, nodesByDomain
  - Response time < 100ms for 5000-node graphs
  - Frontend can use this to decide rendering strategy before fetching full graph

#### 4.3 – Response Compression
- **Files to modify**: `src/Host/Program.cs` (or startup configuration)
- **Test plan (TDD)**:
  - Integration tests: verify `Content-Encoding: gzip` or `br` on graph API responses
- **Acceptance criteria**:
  - Gzip/Brotli compression enabled for JSON responses
  - Graph response for 1000 nodes reduced by ~70-80%
  - No impact on non-JSON endpoints

#### 4.4 – ETag/Conditional-GET for Graph Endpoint
- **Files to modify**: `src/Modules/Graph/Graph.Api/Endpoints/GetGraphEndpoint.cs`, `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphEndpointTests` – `Get_UnchangedGraph_Returns304`, `Get_ChangedGraph_Returns200WithNewETag`
- **Acceptance criteria**:
  - Graph response includes `ETag` header based on hash of graph version/timestamp
  - Clients sending `If-None-Match` receive `304 Not Modified` when graph unchanged
  - The 60-second polling loop in `useDiagram.ts` sends `If-None-Match` header
  - Eliminates unnecessary deserialization and transfer for unchanged graphs
  - Measured: 90%+ of polling requests return 304 during idle periods

#### 4.5 – Node Count Metadata in Graph Response
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GraphDto.cs`, `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_LargeGraph_IncludesNodeCount`
- **Acceptance criteria**:
  - `GraphDto` includes `totalNodeCount` (pre-filter) and `filteredNodeCount` (post-filter)
  - Frontend uses this to enable/disable performance optimizations dynamically

#### 4.6 – Optimize GetGraphHandler Projections
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_5000Nodes_CompletesUnder500ms`
  - Fakes/Fixtures: `ArchitectureGraphBuilder` with configurable node count
- **Acceptance criteria**:
  - Replace `.ToArray()` intermediate allocations with `Span<T>` or pooled arrays where applicable
  - Use `Dictionary` instead of repeated `.FirstOrDefault()` lookups for node/edge matching
  - Telemetry index built once and reused across all node projections
  - No behavioral changes to response content

#### 4.7 – Switch ELK Edge Routing to POLYLINE for Large Graphs
- **Files to modify**: `web/src/features/diagram/hooks/useElkLayout.ts`
- **Test plan (TDD)**:
  - Unit tests: `UseElkLayoutTests` – `LargeGraph_UsesPolylineRouting`, `SmallGraph_UsesOrthogonalRouting`
- **Acceptance criteria**:
  - When node count > 500, ELK_OPTIONS uses `elk.edgeRouting: 'POLYLINE'` instead of `'ORTHOGONAL'`
  - ORTHOGONAL routing has O(n^2) edge routing complexity; POLYLINE is significantly faster
  - Layout time for 1000 nodes reduced by 30-50% with POLYLINE
  - Visual quality of edges is acceptable at the scale where POLYLINE activates

#### 4.8 – SignalR Differential Updates
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Hubs/DiagramHub.cs`, `src/Modules/Visualization/Visualization.Api/Adapters/SignalRDiagramNotifier.cs`, `web/src/features/diagram/hooks/useSignalR.ts`, `web/src/features/diagram/hooks/useDiagram.ts`
- **Test plan (TDD)**:
  - Unit tests: `SignalRDiagramNotifierTests` – `Notify_SendsDeltaNotFullGraph`, `UseDiagramTests` – `OnDiagramUpdated_PatchesInsteadOfRefetching`
- **Acceptance criteria**:
  - SignalR `DiagramUpdated` message carries changed node IDs and their new state (delta), not just a trigger
  - Client applies targeted patch to existing graph state instead of calling `fetchGraph(force=true)` which replaces `apiData` entirely
  - Eliminates: full `mapGraphDtoToDiagramData` allocation storm + new ELK layout computation on every live update
  - For topology changes (node added/removed), fall back to full refetch
  - For property changes (health, cost, telemetry), apply in-place patch

#### 4.9 – Backend Performance Tests
- **Files to create**: `src/Modules/Graph/Graph.Tests/Performance/GetGraphPerformanceTests.cs`
- **Test plan (TDD)**:
  - Create graphs with 100, 500, 1000, 5000 nodes
  - Assert handler completion time < 500ms for 1000 nodes
  - Assert handler completion time < 2s for 5000 nodes
  - Fakes/Fixtures: `LargeGraphBuilder`, `FakeTelemetryQueryService`
- **Acceptance criteria**:
  - Performance baselines documented
  - Tests run in CI

### Epic 5: Frontend Data Layer Optimization
Goal: Optimize data flow from API to rendering to reduce processing overhead

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Optimize useDiagram filtering pipeline with indexed lookups | Refactor | web | M | – | ⬚ |
| 5.2 | Implement progressive node rendering for large graphs | Feature | web | M | 1.3 | ⬚ |
| 5.3 | Add graph size-aware rendering strategy selection | Feature | web | M | 4.3, 1.3, 3.1 | ⬚ |
| 5.4 | Write data layer optimization tests | Test | web | M | 5.1, 5.2 | ⬚ |

#### 5.1 – Optimize Filtering Pipeline and DTO Mapping
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.ts`
- **Test plan (TDD)**:
  - Unit tests: `UseDiagramTests` – `Filter_1000Nodes_CompletesUnder50ms`, `Filter_SameInput_ReturnsSameReference`, `MapGraphDto_1000Nodes_CompletesUnder100ms`
  - Fakes/Fixtures: `createMockDiagramData(nodeCount)`, `createMockGraphDto(nodeCount)`
- **Acceptance criteria**:
  - Build `Set` / `Map` indexes for filter lookups instead of repeated `.filter()` / `.find()` chains
  - Memoize intermediate filter results to avoid recomputation
  - **Optimize `mapGraphDtoToDiagramData`** (lines ~170-215): replace spread-heavy mapping loop with direct object construction — the current pattern uses `...(condition ? { key: val } : {})` on every node/edge, producing thousands of short-lived objects that dominate GC pauses at 2000 nodes
  - Filtering 1000 nodes completes in < 50ms
  - DTO mapping for 1000 nodes completes in < 100ms

#### 5.2 – Progressive Node Rendering
- **Files to create**: `web/src/features/diagram/hooks/useProgressiveRender.ts`
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `UseProgressiveRenderTests` – `LargeDataset_RendersInBatches`, `SmallDataset_RendersImmediately`, `DataChange_ResetsProgress`
- **CRITICAL DESIGN NOTE**: ELK layout MUST run on the complete node set BEFORE progressive rendering begins. The progressive render hook only controls the React Flow `nodes` array slice size — it reveals already-positioned nodes in batches, it does NOT feed partial data to ELK. Add a guard in `useElkLayout` to ignore data changes from progressive reveal (only re-layout when the full dataset reference changes).
- **Implementation**: Prefer React 19's `useDeferredValue` over manual `requestAnimationFrame` loop — it integrates with the React scheduler and avoids manual cleanup:
  ```typescript
  const deferredNodes = useDeferredValue(allPositionedNodes);
  const visibleNodes = nodes.length > 500 ? deferredNodes.slice(0, visibleCount) : allPositionedNodes;
  ```
- **Acceptance criteria**:
  - For graphs > 500 nodes, nodes revealed progressively using `useDeferredValue` + batch slicing
  - For graphs <= 500 nodes, render immediately (no batching overhead)
  - ELK layout runs once on full dataset; progressive render only slices the positioned array
  - No re-triggering of ELK layout during progressive reveal
  - Visual progress: nodes appear progressively without layout jumping

#### 5.3 – Size-Aware Rendering Strategy
- **Files to create**: `web/src/features/diagram/hooks/useRenderingStrategy.ts`
- **Files to modify**: `web/src/features/diagram/DiagramPage.tsx`
- **Test plan (TDD)**:
  - Unit tests: `UseRenderingStrategyTests` – `Under500_UsesDefaultStrategy`, `Over500_UsesOptimizedStrategy`, `Over2000_UsesAggressiveStrategy`
- **Acceptance criteria**:
  - Strategy based on `totalNodeCount` from backend:
    - < 500 nodes: default (all features, full detail)
    - 500-2000 nodes: optimized (viewport culling, LOD, snap-to-grid)
    - > 2000 nodes: aggressive (progressive render, simplified edges, LOD always active)
  - Strategy communicated via context/prop to DiagramCanvas

#### 5.4 – Data Layer Tests
- **Files to create**: `web/src/features/diagram/__tests__/useDiagram.test.ts`, `web/src/features/diagram/__tests__/useProgressiveRender.test.ts`
- **Test plan (TDD)**:
  - Filtering correctness at all graph sizes
  - Progressive render batching behavior
  - Rendering strategy selection thresholds
  - Fakes/Fixtures: `createMockDiagramData`, `createMockGraphResponse`
- **Acceptance criteria**:
  - All optimization behaviors verified at boundary conditions
  - No regressions in filter logic

### Epic 6: Edge Rendering Optimization
Goal: Optimize edge rendering for large graphs where edges outnumber nodes

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Simplify edge rendering at low zoom levels | Feature | web | S | 3.1 | ⬚ |
| 6.2 | Reduce edge label rendering for dense graphs | Feature | web | S | – | ⬚ |
| 6.3 | Write edge rendering tests | Test | web | S | 6.1, 6.2 | ⬚ |
| 6.4 | Visual QA: validate edge simplification, progressive rendering, filter FPS | Test | web | M | 6.1, 5.2 | ⬚ |

#### 6.1 – Simplified Edge Rendering at Low Zoom
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `EdgeRenderingTests` – `ZoomBelow03_SimplifiedEdgeStyle`, `ZoomAbove03_FullEdgeStyle`
- **Acceptance criteria**:
  - zoom < 0.3: edges render as thin lines without labels, markers, or dash patterns
  - zoom >= 0.3: edges render with full styling (current behavior)
  - Reduced DOM complexity at low zoom

#### 6.2 – Reduce Edge Labels for Dense Graphs
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `EdgeRenderingTests` – `DenseGraph_HidesEdgeLabels`, `SparseGraph_ShowsEdgeLabels`
- **Acceptance criteria**:
  - When edge count > 500, edge labels hidden by default
  - Labels shown on hover or when edge selected
  - Reduces text rendering overhead significantly

#### 6.4 – Visual QA: Edge Simplification & Progressive Rendering
- **Agent**: `visual-qa` (Playwright MCP)
- **Test plan**:
  - Navigate to a diagram with 500+ edges, zoom to < 0.3 — verify edge labels are hidden
  - Zoom to >= 0.3 — verify edge labels reappear
  - Hover over an edge at low zoom — verify label appears on hover (if implemented)
  - Load a 1000+ node diagram — observe progressive rendering (nodes appearing in batches)
  - Measure FPS during rapid filter toggling (e.g., toggle infrastructure, change C4 level)
  - Check console for React batching warnings or performance issues
- **Acceptance criteria**:
  - Edge labels hidden at zoom < 0.3 for dense graphs
  - Progressive rendering visually smooth (no layout jumping)
  - FPS >= 30 during filter changes
  - No console errors during rapid interactions

#### 6.3 – Edge Rendering Tests
- **Files to create**: `web/src/features/diagram/__tests__/EdgeRendering.test.tsx`
- **Test plan (TDD)**:
  - Verify edge simplification at each zoom threshold
  - Verify label visibility based on edge density
- **Acceptance criteria**:
  - All edge rendering behaviors covered

### Epic 7: Collapsible Node Groups (C4 Hierarchy)
Goal: Leverage the C4 model hierarchy to collapse/expand groups, reducing visible node count

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Add expand/collapse interaction to GroupNode | Feature | web | L | 1.1 | ⬚ |
| 7.2 | Track collapsed state in diagram state | Feature | web | M | 7.1 | ⬚ |
| 7.3 | Filter collapsed children from layout computation | Feature | web | M | 7.2 | ⬚ |
| 7.4 | Write collapsible group tests | Test | web | M | 7.1, 7.2, 7.3 | ⬚ |
| 7.5 | Visual QA: validate collapse/expand UX, node count reduction, edge rerouting | Test | web | M | 7.1, 7.3 | ⬚ |

#### 7.1 – Collapsible GroupNode Component
- **Files to modify**: `web/src/features/diagram/components/GroupNode.tsx`
- **Test plan (TDD)**:
  - Unit tests: `GroupNodeTests` – `Click_TogglesCollapsedState`, `Collapsed_ShowsSummaryOnly`, `Expanded_ShowsChildren`
- **Acceptance criteria**:
  - GroupNode header is clickable to toggle collapsed/expanded
  - Collapsed state shows: group name, node count, aggregate health indicator
  - Expanded state shows children (current behavior)
  - Visual indicator (chevron/arrow) shows collapse state

#### 7.2 – Collapsed State Management
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.ts` or new hook
- **Test plan (TDD)**:
  - Unit tests: `CollapsedStateTests` – `Toggle_AddsToCollapsedSet`, `Toggle_RemovesFromCollapsedSet`, `CollapseAll_CollapsesAllGroups`
- **Acceptance criteria**:
  - `Set<string>` of collapsed group IDs managed in state
  - Collapse/expand all controls available in sidebar
  - Collapsed state persisted to URL params

#### 7.3 – Layout Filtering for Collapsed Groups
- **Files to modify**: `web/src/features/diagram/hooks/useElkLayout.ts`
- **Test plan (TDD)**:
  - Unit tests: `ElkLayoutTests` – `CollapsedGroup_ExcludesChildren`, `CollapsedGroup_RendersAsSingleNode`
- **Acceptance criteria**:
  - Collapsed groups represented as single nodes in ELK graph
  - Edges to/from collapsed children redirected to group node
  - Layout recomputed on collapse/expand
  - Significant node count reduction for large grouped diagrams

#### 7.4 – Collapsible Group Tests
- **Files to create**: `web/src/features/diagram/__tests__/CollapsibleGroups.test.tsx`
- **Test plan (TDD)**:
  - End-to-end flow: render -> collapse group -> verify node count reduced -> expand -> verify restored
  - Edge redirection on collapse
- **Acceptance criteria**:
  - Full collapse/expand lifecycle tested
  - Edge routing correctness verified

#### 7.5 – Visual QA: Collapse/Expand UX
- **Agent**: `visual-qa` (Playwright MCP)
- **Test plan**:
  - Navigate to diagram with multiple resource groups
  - Screenshot expanded state, note DOM node count
  - Click group header to collapse — screenshot, verify node count drops
  - Click again to expand — screenshot, verify node count restores
  - Verify edges reroute to group node when collapsed (no dangling edges)
  - Test "Collapse All" / "Expand All" controls if available in sidebar
  - Measure layout recomputation time during collapse/expand
  - Check for visual artifacts: overlapping nodes, misaligned edges, flickering
- **Acceptance criteria**:
  - Collapse reduces DOM node count proportionally to collapsed group size
  - Edges connect to group boundary when children are collapsed
  - No visual artifacts during collapse/expand transitions
  - Layout recomputation completes in < 1s for 1000-node graphs

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | ELK web worker build requires Vite-specific configuration (`?worker` import or `optimizeDeps`) | High | Medium | Concrete Vite setup steps in task 2.1; spike first; fallback to custom `new Worker()` wrapper around bundled build |
| R2 | `onlyRenderVisibleElements` may cause MiniMap blank areas for off-screen nodes | Medium | Medium | Explicit MiniMap test case in task 1.7; test at various pan speeds |
| R3 | **CRITICAL**: `useViewport()` in LOD causes all nodes to re-render on every zoom frame | High | High | MANDATORY: use `useStore` with discretized LOD tier selector returning enum, not raw zoom float |
| R4 | Progressive rendering re-triggers ELK layout when `nodes` array changes | High | High | Guard in `useElkLayout`: only re-layout when full dataset reference changes, not progressive slice |
| R5 | React.memo on ServiceNode inert without useMemo on data objects | Medium | High | Task 1.1 explicitly depends on 1.2; both must be implemented together |
| R6 | Backend response compression may break existing clients/SignalR | Low | Medium | Test all API consumers; SignalR uses its own transport negotiation |
| R7 | Collapsible groups may break edge routing for edges crossing group boundaries | Medium | High | Spike task: prototype with 5-group test case before committing full Epic 7 scope |
| R8 | Agent Teams feature is experimental with known limitations around session resumption | Medium | Medium | Documented fallback execution order without teams; fall back to sequential subagents |
| R9 | DiagramCanvas.tsx edited across multiple teams — merge conflict risk | High | High | Single named owner per team; all DiagramCanvas edits serialized across teams |
| R10 | Agent Teams token cost scales linearly with team size | Low | Medium | Team 4 reduced to 3 teammates; use subagents for trivial tasks |
| R11 | `mapGraphDtoToDiagramData` allocation storm (spread-heavy mapping) causes GC pauses at 2000 nodes | Medium | Medium | Task 5.1 explicitly targets this function; replace spreads with direct construction |
| R12 | SignalR `onDiagramUpdated` triggers full graph re-fetch + re-layout on every live update | Medium | High | Task 4.8 adds differential SignalR updates with targeted patching |
| R13 | Performance tests in jsdom give false confidence (no real layout/paint) | Medium | Medium | Unit tests scoped to data transform timing only; real render baselines from Playwright visual-qa |

### Fallback Execution Order (Without Agent Teams)
If the experimental Agent Teams feature fails (R8), execute all tasks sequentially:
1. Epic 0 (setup) → Epic 1 (memoization) → Epic 2 (ELK worker) → Epic 3 (LOD) → Epic 4 (backend) → Epic 5 (data layer) → Epic 6 (edges) → Epic 7 (groups)
2. Use standard subagents (react-writer, csharp-writer, test-generator) for each task
3. Run visual-qa as a separate subagent after each epic completes

### Critical Path
0.1 → 0.3 → Team 1 (1.2→1.1+1.3+1.4+1.5+2.2 ∥ 2.1 ∥ 1.6 ∥ 1.7) → verification → Team 2 (3.1+3.2+4.7 ∥ 4.1→4.6+4.2+4.4+4.5 ∥ 4.3 ∥ 3.3+4.9 ∥ 3.4) → verification → Team 3 (5.1+5.2+6.1+6.2+5.3 ∥ 4.8 ∥ 5.4+6.3 ∥ 6.4) → verification → Team 4 (7.1→7.2→7.3 ∥ 7.4 ∥ 7.5) → final verification

### Estimated Total Effort
- S tasks: 12 × ~30 min = ~6 h
- M tasks: 17 × ~2.5 h = ~42.5 h
- L tasks: 2 × ~6 h = ~12 h
- XL tasks: 0
- **Total sequential: ~60.5 hours**
- **Total with Agent Teams (~2x parallelization): ~30 hours wall clock**
- Note: Visual QA tasks run in parallel with implementation (same team), adding minimal wall-clock overhead

### Expert Review Findings Incorporated
This plan was reviewed by two specialized agents and updated to address their findings:

**React Performance Expert** identified:
- CRITICAL: `useViewport()` in LOD causes all nodes to re-render on every zoom frame → fixed with `useStore` discretized selector (task 3.1)
- Task 1.1 (React.memo) is inert without 1.2 (useMemo) → dependency made explicit
- MiniMap + `onlyRenderVisibleElements` interaction → test case added to 1.7
- CSS `contain` property missing → task 1.4 replaced snapToGrid (zero-impact for ELK-managed layouts)
- `useDeferredValue` preferred over manual RAF for progressive render → task 5.2 updated
- Vite worker setup for ELK understated → task 2.1 expanded with concrete steps

**Web Application Architect** identified:
- `mapGraphDtoToDiagramData` allocation storm (spread-heavy mapping) → added to task 5.1
- SignalR full graph re-fetch on every update → new task 4.8
- ELK `ORTHOGONAL` O(n^2) routing → new task 4.7 with POLYLINE fallback
- EF Core loading full aggregate with all snapshots → new task 4.1
- No ETag/conditional-GET for 60s polling → new task 4.4
- DiagramCanvas.tsx ownership conflict across teams → resolved with single-owner-per-team rule
- Team 4 over-designed for sequential work → reduced to 3 teammates
- jsdom performance tests give false confidence → task 1.6 scoped to data transforms only
- snapToGrid (old 1.4) has zero impact since users don't drag ELK-positioned nodes → removed
