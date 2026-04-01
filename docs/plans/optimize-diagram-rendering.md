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

**Team 1: Quick Wins Blitz** (Epics 1 + 2 — independent, no file conflicts)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 1.1, 1.2, 1.3, 1.4, 1.5 — DiagramCanvas.tsx + GroupNode.tsx memoization
├── Teammate B (react-writer): Tasks 2.1, 2.2 — useElkLayout.ts web worker switch + loading indicator
├── Teammate C (test-generator): Task 1.6 — Performance test suite (can start once A finishes 1.1)
└── Teammate D (visual-qa): Task 1.7 — Browser-based QA: measure DOM node counts, FPS during pan/zoom, verify no console errors after memoization changes
```
- **Why teams over subagents**: Teammates A and B work on different files simultaneously (DiagramCanvas vs useElkLayout), Teammate C writes tests informed by the changes both made via peer messaging, and Teammate D validates visual correctness and measures real rendering performance in the browser using Playwright.
- **File safety**: A owns DiagramCanvas.tsx + GroupNode.tsx, B owns useElkLayout.ts, C owns __tests__/, D is read-only (screenshots + metrics).
- **QA teammate role**: D launches the dev server, navigates to a diagram page, takes baseline screenshots, measures FPS and DOM node counts before/after the memoization changes, and reports any visual regressions or console errors.

**Team 2: Full-Stack LOD + Backend** (Epics 3 + 4 — frontend and backend in parallel)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 3.1, 3.2 — LOD rendering + CSS in DiagramCanvas.tsx
├── Teammate B (csharp-writer): Tasks 4.1, 4.3, 4.4 — Backend summary endpoint + handler optimization
├── Teammate C (csharp-writer): Task 4.2 — Response compression in Host
├── Teammate D (test-generator): Tasks 3.3, 4.5 — LOD tests + backend performance tests
└── Teammate E (visual-qa): Task 3.4 — Browser-based QA: verify LOD tiers render correctly at each zoom level, measure API response sizes with compression, validate network performance
```
- **Why teams over subagents**: Frontend LOD (Teammate A) and backend optimization (Teammates B/C) are fully independent layers. Teammate D writes tests for both layers via peer messaging, and Teammate E validates visually that LOD tiers switch correctly at zoom thresholds and that compressed API responses are smaller.
- **File safety**: A owns web/src/features/diagram/components/ + diagram.css, B owns Graph.Application/, C owns Host/, D owns test files, E is read-only.
- **QA teammate role**: E zooms to each LOD threshold (< 0.2, 0.2-0.5, > 0.5) and screenshots each tier, verifies the correct CSS classes are present (`.service-node-dot`, `.service-node-compact`, `.service-node`), and checks network requests for `Content-Encoding` headers and response sizes.

**Team 3: Data Layer + Edge Optimization** (Epics 5 + 6 — frontend data and rendering)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 5.1, 5.2 — useDiagram optimization + progressive rendering hook
├── Teammate B (react-writer): Tasks 6.1, 6.2, 5.3 — Edge optimization + rendering strategy hook
├── Teammate C (test-generator): Tasks 5.4, 6.3 — Data layer + edge rendering tests
└── Teammate D (visual-qa): Task 6.4 — Browser-based QA: verify edge labels hidden at density threshold, progressive rendering shows nodes appearing in batches, measure frame rate during filter changes
```
- **Why teams over subagents**: A works on hooks (useDiagram.ts, useProgressiveRender.ts) while B works on DiagramCanvas edge rendering + useRenderingStrategy.ts. Teammate D validates that edge simplification and progressive rendering work visually in a real browser.
- **File safety**: A owns useDiagram.ts + new hooks, B owns edge sections of DiagramCanvas.tsx + useRenderingStrategy.ts, C owns test files, D is read-only.
- **QA teammate role**: D verifies edge labels disappear when edge count > 500, watches progressive rendering batches appear visually, and measures FPS during rapid filter changes using Playwright performance APIs.

**Team 4: Collapsible Groups** (Epic 7 — tightly coupled, sequential with verification)
```
Lead: Orchestrator
├── Teammate A (react-writer): Tasks 7.1, 7.2 — GroupNode interaction + collapsed state management
├── Teammate B (react-writer): Task 7.3 — ELK layout filtering for collapsed groups (depends on 7.2)
├── Teammate C (test-generator + change-verifier): Task 7.4 — Collapsible group tests + full verification
└── Teammate D (visual-qa): Task 7.5 — Browser-based QA: verify collapse/expand interaction, measure node count reduction, validate edge rerouting visually, screenshot collapsed vs expanded states
```
- **Why teams over subagents**: Task 7.3 depends on the collapsed state interface from 7.2. With peer messaging, B can ask A for the exact `Set<string>` interface shape as soon as A finishes 7.2. Teammate D validates the full collapse/expand UX in a real browser.
- **Task dependency**: B waits for A to finish 7.2 before starting 7.3. D starts after A completes 7.1.
- **QA teammate role**: D clicks group headers to toggle collapse/expand, screenshots both states, verifies DOM node count drops when groups are collapsed, and checks that edges reroute correctly to group nodes without visual artifacts.

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
| 1.1 | Memoize ServiceNode and GroupNode components with React.memo | Refactor | web | S | – | ⬚ |
| 1.2 | Memoize node/edge array transformations in DiagramCanvas with useMemo | Refactor | web | S | – | ⬚ |
| 1.3 | Enable onlyRenderVisibleElements for large graphs (500+ nodes) | Feature | web | S | – | ⬚ |
| 1.4 | Add snapToGrid to reduce drag-induced re-renders | Feature | web | S | – | ⬚ |
| 1.5 | Stabilize MiniMap nodeColor callback with useCallback | Refactor | web | S | – | ⬚ |
| 1.6 | Write rendering performance tests with large mock datasets | Test | web | M | 1.1 | ⬚ |
| 1.7 | Visual QA: validate memoization, measure DOM counts and FPS via Playwright | Test | web | M | 1.1, 1.2, 1.3 | ⬚ |

#### 1.1 – Memoize ServiceNode and GroupNode
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`, `web/src/features/diagram/components/GroupNode.tsx`
- **Test plan (TDD)**:
  - Unit tests: `DiagramCanvasTests` – `ServiceNode_SameProps_DoesNotRerender`, `GroupNode_SameProps_DoesNotRerender`
  - Verify via React DevTools profiler that node re-renders drop to zero during pan/zoom
- **Acceptance criteria**:
  - `ServiceNode` wrapped in `React.memo` with stable reference
  - `GroupNode` wrapped in `React.memo`
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

#### 1.4 – Add Snap-to-Grid
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Manual verification: node drag updates less frequently
- **Acceptance criteria**:
  - `snapToGrid={true}` and `snapGrid={[25, 25]}` added to ReactFlow
  - Reduced state update frequency during node dragging

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
  - Verify MiniMap renders correctly after callback stabilization
- **Acceptance criteria**:
  - No visual regressions compared to pre-optimization screenshots
  - DOM node count reduced when `onlyRenderVisibleElements` is active (task 1.3)
  - FPS >= 30 during pan/zoom at default zoom (0.3)
  - Zero console errors

#### 1.6 – Performance Test Suite
- **Files to create**: `web/src/features/diagram/__tests__/DiagramPerformance.test.tsx`
- **Test plan (TDD)**:
  - Create mock datasets: 100, 500, 1000, 2000 nodes with proportional edges
  - Measure: initial render time, re-render time on filter change, DOM node count
  - Assert: render time < 3s for 1000 nodes, DOM nodes < 200 when zoomed out
  - Fakes/Fixtures: `createMockDiagramData(nodeCount: number)` builder
- **Acceptance criteria**:
  - Performance baselines established and documented
  - Tests run in CI and fail on significant regressions

### Epic 2: Layout Computation Offloading
Goal: Move ELK layout computation off the main thread to prevent UI freezes

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Switch ELK import from bundled to web worker build | Refactor | web | S | – | ⬚ |
| 2.2 | Add layout loading indicator during computation | Feature | web | S | 2.1 | ⬚ |
| 2.3 | Verify web worker layout with large graphs | Test | web | M | 2.1 | ⬚ |

#### 2.1 – Switch ELK to Web Worker Build
- **Files to modify**: `web/src/features/diagram/hooks/useElkLayout.ts`
- **Test plan (TDD)**:
  - Unit tests: `UseElkLayoutTests` – `Layout_LargeGraph_DoesNotBlockMainThread`, `Layout_ProducesIdenticalResults`
  - Verify main thread stays responsive during 1000-node layout
- **Acceptance criteria**:
  - Import changed from `elkjs/lib/elk.bundled.js` to `elkjs/lib/elk-worker.js`
  - Layout results identical to bundled version
  - Main thread frame budget maintained during layout (no jank)
  - Vite config updated if needed for web worker bundling

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
  - Fakes/Fixtures: mock `useViewport` hook
- **Acceptance criteria**:
  - zoom < 0.2: render colored dot (12x12px circle) with health color + Handles only
  - zoom 0.2-0.5: render compact node (icon + label + health badge) without metrics/tags
  - zoom > 0.5: render full detail node (current implementation)
  - Smooth visual transitions between LOD levels
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
| 4.1 | Add server-side graph summary endpoint for initial overview | Feature | Graph.Api | M | – | ⬚ |
| 4.2 | Implement response compression for graph payloads | Feature | Host | S | – | ⬚ |
| 4.3 | Add node count metadata to graph response | Feature | Graph.Application | S | – | ⬚ |
| 4.4 | Optimize GetGraphHandler projection to reduce allocations | Refactor | Graph.Application | M | – | ⬚ |
| 4.5 | Write backend performance tests for large graphs | Test | Graph.Tests | M | 4.1, 4.4 | ⬚ |

#### 4.1 – Graph Summary Endpoint
- **Files to create**: `src/Modules/Graph/Graph.Api/Endpoints/GetGraphSummaryEndpoint.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GetGraphSummaryQuery.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GetGraphSummaryHandler.cs`, `src/Modules/Graph/Graph.Application/GetGraphSummary/GraphSummaryDto.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphSummaryHandlerTests` – `Handle_ValidProject_ReturnsSummary`, `Handle_LargeGraph_ReturnsQuickly`
  - Module tests: `GetGraphSummaryEndpointTests` – `Get_ValidProject_Returns200WithSummary`
  - Fakes/Fixtures: `FakeArchitectureGraphRepository`
- **Acceptance criteria**:
  - `GET /api/projects/{id}/graph/summary` returns: totalNodes, totalEdges, nodesByLevel, nodesByServiceType, nodesByDomain
  - Response time < 100ms for 5000-node graphs
  - Frontend can use this to decide rendering strategy before fetching full graph

#### 4.2 – Response Compression
- **Files to modify**: `src/Host/Program.cs` (or startup configuration)
- **Test plan (TDD)**:
  - Integration tests: verify `Content-Encoding: gzip` or `br` on graph API responses
- **Acceptance criteria**:
  - Gzip/Brotli compression enabled for JSON responses
  - Graph response for 1000 nodes reduced by ~70-80%
  - No impact on non-JSON endpoints

#### 4.3 – Node Count Metadata in Graph Response
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GraphDto.cs`, `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_LargeGraph_IncludesNodeCount`
- **Acceptance criteria**:
  - `GraphDto` includes `totalNodeCount` (pre-filter) and `filteredNodeCount` (post-filter)
  - Frontend uses this to enable/disable performance optimizations dynamically

#### 4.4 – Optimize GetGraphHandler Projections
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_5000Nodes_CompletesUnder500ms`
  - Fakes/Fixtures: `ArchitectureGraphBuilder` with configurable node count
- **Acceptance criteria**:
  - Replace `.ToArray()` intermediate allocations with `Span<T>` or pooled arrays where applicable
  - Use `Dictionary` instead of repeated `.FirstOrDefault()` lookups for node/edge matching
  - Telemetry index built once and reused across all node projections
  - No behavioral changes to response content

#### 4.5 – Backend Performance Tests
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

#### 5.1 – Optimize Filtering Pipeline
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.ts`
- **Test plan (TDD)**:
  - Unit tests: `UseDiagramTests` – `Filter_1000Nodes_CompletesUnder50ms`, `Filter_SameInput_ReturnsSameReference`
  - Fakes/Fixtures: `createMockDiagramData(nodeCount)`
- **Acceptance criteria**:
  - Build `Set` / `Map` indexes for filter lookups instead of repeated `.filter()` / `.find()` chains
  - Memoize intermediate filter results to avoid recomputation
  - Filtering 1000 nodes completes in < 50ms

#### 5.2 – Progressive Node Rendering
- **Files to create**: `web/src/features/diagram/hooks/useProgressiveRender.ts`
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: `UseProgressiveRenderTests` – `LargeDataset_RendersInBatches`, `SmallDataset_RendersImmediately`, `DataChange_ResetsProgress`
- **Acceptance criteria**:
  - For graphs > 500 nodes, render in batches of 100 using `requestAnimationFrame` + `startTransition`
  - For graphs <= 500 nodes, render immediately (no batching overhead)
  - Visual progress: nodes appear progressively without layout jumping
  - User can interact with already-rendered nodes while more load

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
| R1 | ELK web worker build may have Vite bundling issues | Medium | Medium | Test with Vite dev and prod builds; fallback to bundled + manual Web Worker wrapper |
| R2 | onlyRenderVisibleElements may cause visual flickering during fast panning | Medium | Low | Test at various pan speeds; add transition buffer zone around viewport |
| R3 | LOD zoom threshold transitions may cause jarring visual jumps | Medium | Medium | Add CSS transitions; test threshold values with real users; make thresholds configurable |
| R4 | Progressive rendering may interfere with ELK layout (partial node set) | Medium | High | Run layout on full dataset first, then progressively reveal already-positioned nodes |
| R5 | React.memo on ServiceNode may break if data object identity changes unexpectedly | Low | High | Ensure useMemo on node data objects; add referential equality tests |
| R6 | Backend response compression may break existing clients/SignalR | Low | Medium | Test all API consumers; SignalR uses its own transport negotiation |
| R7 | Collapsible groups may break edge routing for edges crossing group boundaries | Medium | High | Spike task: prototype with 5-group test case before full implementation |
| R8 | Agent Teams feature is experimental with known limitations around session resumption | Medium | Medium | Run verification gate after each team; fall back to sequential subagents if team coordination fails |
| R9 | Agent Teams teammates may attempt same-file edits despite file safety plan | Low | High | Assign explicit file ownership per teammate; use verification pipeline after each team completes |
| R10 | Agent Teams token cost scales linearly with team size | Low | Medium | Limit teams to 3-4 teammates; use subagents for trivial tasks (one-line changes) |

### Critical Path
0.1 → 0.2 → Team 1 (1.1-1.5 ∥ 2.1) → verification → Team 2 (3.1-3.2 ∥ 4.1-4.4) → verification → Team 3 (5.1-5.2 ∥ 6.1-6.2 ∥ 5.3) → verification → Team 4 (7.1-7.2 → 7.3) → final verification

### Estimated Total Effort
- S tasks: 12 × ~30 min = ~6 h
- M tasks: 15 × ~2.5 h = ~37.5 h
- L tasks: 1 × ~6 h = ~6 h
- XL tasks: 0
- **Total sequential: ~49.5 hours**
- **Total with Agent Teams (~2x parallelization): ~25 hours wall clock**
- Note: Visual QA tasks run in parallel with implementation (same team), adding minimal wall-clock overhead
