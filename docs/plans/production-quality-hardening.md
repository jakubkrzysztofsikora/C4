## Plan: ProductionQualityHardening
Scope: FeatureSet
Created: 2026-03-06
Status: Draft

### Overview
Address all P0–P2 production gaps identified in the March 6 probe against project `42ef18bc-bb27-4189-a45f-e7423137c1ae`. The initiative fixes real telemetry ingestion (currently 0 metrics), eliminates synthetic/derived edge traffic, unblocks Code-level C4 population, upgrades overlay provenance from heuristic-only to source-aware, corrects misleading security color defaults, completes drift run metadata, evolves telemetry targets to typed v2, achieves export fidelity parity with canvas state, and improves data quality (reduce fallback classifications and unknown environments).

### Success Criteria
- [ ] SC-1: Telemetry sync on production project returns `metricsIngested > 0` when valid App Insights targets are configured
- [ ] SC-2: Edge telemetry source is `app-insights.dependencies` for edges with real dependency data; `service-health.derived` is only used as explicit fallback with UI indicator
- [ ] SC-3: Code-level C4 nodes appear in graph when repository source is configured and classifiable
- [ ] SC-4: Overlay responses include `dataProvenance` field that accurately reflects actual source (`ai`, `rule-based`, `heuristic`) — not hardcoded `heuristic`
- [ ] SC-5: Security overlay maps `none`/`unknown` severity to gray (neutral), not green
- [ ] SC-6: Drift overview API returns `lastRunAtUtc`, `status`, `error` metadata; UI displays run state
- [ ] SC-7: Telemetry targets API supports typed provider metadata (auth mode, provider type)
- [ ] SC-8: SVG/PDF export renders overlay colors, edge states, and legends matching canvas state
- [ ] SC-9: `fallbackClassificationCount` reduced by ≥30% from current 333; `unknownEnvironmentCount` reduced by ≥30% from current 394
- [ ] SC-10: `knownNodes > 0` and `knownEdges > 0` in graph quality metrics after telemetry sync
- [ ] SC-11: All existing tests pass; no regressions introduced
- [ ] SC-12: Build compiles with zero errors

### Epic 1: Real Telemetry Ingestion (P0)
Goal: Make telemetry sync actually ingest metrics from configured Application Insights targets

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Fix project-scoped credential resolution in ApplicationInsightsClient | Feature | Telemetry | M | – | ⬚ |
| 1.2 | Add telemetry ingestion diagnostic endpoint | Feature | Telemetry | S | 1.1 | ⬚ |
| 1.3 | Propagate ingestion status to graph quality metrics | Feature | Graph | M | 1.1 | ⬚ |
| 1.4 | Write telemetry ingestion tests | Test | Telemetry | M | 1.1 | ⬚ |

#### 1.1 – Fix project-scoped credential resolution
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Infrastructure/Adapters/ApplicationInsightsClient.cs`
- **Test plan (TDD)**:
  - Unit tests: `ApplicationInsightsClientTests` – `ExecuteKqlQueriesAsync_WithProjectScopedAppIds_UsesProjectCredentials`, `ExecuteKqlQueriesAsync_WithGlobalFallback_FallsBackToGlobalApiKey`, `ExecuteKqlQueriesAsync_WithInvalidCredentials_ReturnsEmptyNotException`
  - Fakes/Fixtures needed: `FakeHttpMessageHandler`, `FakeAppInsightsConfigStore`
- **Acceptance criteria**:
  - When project has App Insights config with valid AppIds, queries use project-scoped credentials first
  - Global API key used only when project-scoped config is absent
  - Failed auth logs structured warning with project ID and target AppId
  - Individual target failures don't prevent other targets from being queried

#### 1.2 – Add telemetry ingestion diagnostic endpoint
- **Files to create**: `src/Modules/Telemetry/Telemetry.Api/Endpoints/GetTelemetryDiagnostics/GetTelemetryDiagnosticsEndpoint.cs`, `src/Modules/Telemetry/Telemetry.Application/GetTelemetryDiagnostics/GetTelemetryDiagnosticsQuery.cs`, `src/Modules/Telemetry/Telemetry.Application/GetTelemetryDiagnostics/GetTelemetryDiagnosticsHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetTelemetryDiagnosticsHandlerTests` – `Handle_WithConfiguredTargets_ReturnsDiagnosticStatus`, `Handle_WithNoTargets_ReturnsEmptyDiagnostics`
- **Acceptance criteria**:
  - Returns per-target connectivity status (reachable, auth-failed, no-data, error)
  - Returns last successful sync timestamp per target
  - Returns total metrics ingested since last sync

#### 1.3 – Propagate ingestion status to graph quality metrics
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_WithTelemetryData_SetsKnownNodesAndEdgesCount`, `Handle_WithNoTelemetry_SetsKnownCountsToZero`
- **Acceptance criteria**:
  - `knownNodes` reflects count of nodes with real telemetry data (telemetryStatus == "known")
  - `knownEdges` reflects count of edges with `app-insights.dependencies` source
  - Quality metrics accurately reflect real vs derived data

#### 1.4 – Write telemetry ingestion tests
- **Files to create**: `src/Modules/Telemetry/Telemetry.Tests/Adapters/ApplicationInsightsClientTests.cs`
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Tests/` (existing test infrastructure)
- **Test plan (TDD)**:
  - Unit tests: `ApplicationInsightsClientTests` – full coverage of credential resolution, multi-target query, error handling, metric aggregation
  - Module tests: `TelemetrySyncEndpointTests` – end-to-end sync with mocked App Insights API
- **Acceptance criteria**:
  - ≥80% line coverage on ApplicationInsightsClient
  - Edge cases covered: empty config, invalid credentials, partial target failures, timeout

### Epic 2: True Edge Telemetry (P0)
Goal: Eliminate synthetic edge traffic derived from node averages; clearly distinguish real vs derived edge data

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Replace averaged-node fallback with explicit derived marker | Feature | Graph | M | 1.1 | ⬚ |
| 2.2 | Add edge telemetry provenance to GraphEdgeDto | Feature | Graph | S | 2.1 | ⬚ |
| 2.3 | Display derived-edge indicator in diagram UI | Feature | Frontend | S | 2.2 | ⬚ |
| 2.4 | Write edge telemetry tests | Test | Graph | M | 2.1 | ⬚ |

#### 2.1 – Replace averaged-node fallback with explicit derived marker
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs` (lines 199–201, 709–721)
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `BuildEdgeDto_WithDependencyTelemetry_UsesDependencyData`, `BuildEdgeDto_WithoutDependencyTelemetry_MarksAsDerived`, `BuildEdgeDto_WithNoTelemetry_SetsUnknown`
- **Acceptance criteria**:
  - Edges with real App Insights dependency data use actual metrics (not averages)
  - Edges without dependency data but with node health: set `telemetrySource: "service-health.derived"` and `isDerived: true`
  - Edges with no telemetry at all: set `telemetrySource: null`, metrics null

#### 2.2 – Add edge telemetry provenance to GraphEdgeDto
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs` (GraphEdgeDto record)
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `GraphEdgeDto_IncludesIsDerivedField`
- **Acceptance criteria**:
  - `GraphEdgeDto` includes `IsDerived` boolean field
  - JSON serialization includes the field for frontend consumption

#### 2.3 – Display derived-edge indicator in diagram UI
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx`
- **Test plan (TDD)**:
  - Unit tests: Frontend component test for derived edge rendering (dashed line or label)
- **Acceptance criteria**:
  - Derived edges render with dashed stroke pattern instead of solid
  - Tooltip on derived edges says "Estimated from node health data"
  - Real edges render with solid stroke (current behavior)

#### 2.4 – Write edge telemetry tests
- **Files to create**: `src/Modules/Graph/Graph.Tests/GetGraph/EdgeTelemetryResolutionTests.cs`
- **Test plan (TDD)**:
  - Unit tests: Cover all 3 telemetry resolution paths (real, derived, unknown)
  - Module tests: End-to-end graph query with telemetry data verifies edge DTOs
- **Acceptance criteria**:
  - Tests cover exact match, substring match, no-match paths
  - Tests verify averaging logic only activates for derived path

### Epic 3: Code-Level C4 Population (P0)
Goal: Allow Code-level C4 nodes to be classified and appear in the graph

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Add "Code" to valid C4 levels in ResourceClassifierPlugin | Feature | Discovery | S | – | ⬚ |
| 3.2 | Add Code-level rendering support in graph handler | Feature | Graph | S | 3.1 | ⬚ |
| 3.3 | Write Code-level classification tests | Test | Discovery | S | 3.1 | ⬚ |

#### 3.1 – Add "Code" to valid C4 levels in ResourceClassifierPlugin
- **Files to modify**: `src/Modules/Discovery/Discovery.Infrastructure/AI/ResourceClassifierPlugin.cs` (line 154)
- **Test plan (TDD)**:
  - Unit tests: `ResourceClassifierPluginTests` – `ParseAiResponse_WithCodeLevel_ReturnsValidClassification`, `ParseAiResponse_WithInvalidLevel_ReturnsNull`
- **Acceptance criteria**:
  - `validC4Levels` includes `"Code"` alongside Context, Container, Component
  - AI-generated Code-level classifications are accepted and returned

#### 3.2 – Add Code-level rendering support in graph handler
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_WithCodeLevelNodes_IncludesInResponse`
- **Acceptance criteria**:
  - Code-level nodes included in graph response when present
  - Code-level nodes appear in correct parent hierarchy

#### 3.3 – Write Code-level classification tests
- **Files to create**: `src/Modules/Discovery/Discovery.Tests/AI/ResourceClassifierPluginCodeLevelTests.cs`
- **Test plan (TDD)**:
  - Unit tests: Classification with Code level accepted, rejected for invalid levels
- **Acceptance criteria**:
  - Test confirms Code is now accepted
  - Test confirms levels outside {Context, Container, Component, Code} are still rejected

### Epic 4: Overlay Provenance Accuracy (P1)
Goal: Overlay responses reflect actual data source, not hardcoded "heuristic"

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Fix threat assessment provenance tracking | Feature | Graph | M | – | ⬚ |
| 4.2 | Fix security findings provenance tracking | Feature | Graph | S | – | ⬚ |
| 4.3 | Fix cost insights provenance tracking | Feature | Graph | S | – | ⬚ |
| 4.4 | Add provenance metadata to overlay responses | Feature | Graph | S | 4.1, 4.2, 4.3 | ⬚ |
| 4.5 | Write overlay provenance tests | Test | Graph | M | 4.4 | ⬚ |

#### 4.1 – Fix threat assessment provenance tracking
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentHandler.cs` (line 71)
- **Test plan (TDD)**:
  - Unit tests: `GetThreatAssessmentHandlerTests` – `Handle_WhenAiSucceeds_SetsProvenanceToAi`, `Handle_WhenAiTimesOut_SetsProvenanceToRuleBased`, `Handle_WhenAiFails_SetsProvenanceToRuleBased`
- **Acceptance criteria**:
  - When AI `DetectThreatsAsync` completes within timeout: `dataProvenance: "ai"`
  - When AI times out or throws: `dataProvenance: "rule-based"`
  - Response includes `generatedAtUtc` timestamp

#### 4.2 – Fix security findings provenance tracking
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetSecurityFindings/GetSecurityFindingsHandler.cs` (line 82)
- **Test plan (TDD)**:
  - Unit tests: `GetSecurityFindingsHandlerTests` – `Handle_Always_SetsProvenanceToRuleBased`
- **Acceptance criteria**:
  - `dataProvenance: "rule-based"` (since findings are deterministic rules, not heuristic)
  - Response includes `generatedAtUtc` timestamp

#### 4.3 – Fix cost insights provenance tracking
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetCostInsights/GetCostInsightsHandler.cs` (line 59)
- **Test plan (TDD)**:
  - Unit tests: `GetCostInsightsHandlerTests` – `Handle_Always_SetsProvenanceToHeuristic`
- **Acceptance criteria**:
  - `dataProvenance: "heuristic"` (cost estimates are genuinely heuristic)
  - Response includes `generatedAtUtc` and `isHeuristic: true` flag

#### 4.4 – Add provenance metadata to overlay responses
- **Files to modify**: All three overlay response records
- **Test plan (TDD)**:
  - Unit tests: Verify all response DTOs include provenance fields
- **Acceptance criteria**:
  - All overlay responses include: `dataProvenance` (string), `generatedAtUtc` (DateTimeOffset), `isHeuristic` (bool)
  - Frontend can distinguish real from heuristic data

#### 4.5 – Write overlay provenance tests
- **Files to create**: `src/Modules/Graph/Graph.Tests/Overlays/OverlayProvenanceTests.cs`
- **Test plan (TDD)**:
  - Unit tests: All three handlers return correct provenance under various conditions
- **Acceptance criteria**:
  - Each handler tested for provenance accuracy
  - Response serialization includes all provenance fields

### Epic 5: Security Overlay Color Fix (P1)
Goal: Unknown/no-data security state shows neutral gray, not misleading green

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Change security color default from green to gray | Feature | Frontend | S | – | ⬚ |
| 5.2 | Write security color mapping tests | Test | Frontend | S | 5.1 | ⬚ |

#### 5.1 – Change security color default from green to gray
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx` (lines 36–42)
- **Test plan (TDD)**:
  - Unit tests: `securityColor_NoneOrUnknown_ReturnsGray`, `securityColor_Low_ReturnsGreen`, `securityColor_High_ReturnsRed`
- **Acceptance criteria**:
  - `none` and `unknown` severity map to `#6b7280` (gray-500) not `#2e8f5e` (green)
  - `low` keeps green
  - `medium` keeps orange
  - `high` and `critical` keep red shades

#### 5.2 – Write security color mapping tests
- **Files to create**: `web/src/features/diagram/components/__tests__/DiagramCanvas.test.tsx` (or extend existing)
- **Test plan (TDD)**:
  - Unit tests: All severity levels map to correct colors
- **Acceptance criteria**:
  - Test covers all 6 severity levels (critical, high, medium, low, none, unknown)
  - Test covers default/fallback case

### Epic 6: Drift Run Metadata (P1)
Goal: Drift API returns run metadata; UI displays run state

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Add drift run metadata to domain model | Feature | Graph | M | – | ⬚ |
| 6.2 | Extend drift overview handler with run metadata | Feature | Graph | M | 6.1 | ⬚ |
| 6.3 | Extend drift overview endpoint response | Feature | Graph | S | 6.2 | ⬚ |
| 6.4 | Display drift run metadata in diagram UI | Feature | Frontend | S | 6.3 | ⬚ |
| 6.5 | Write drift run metadata tests | Test | Graph | M | 6.3 | ⬚ |

#### 6.1 – Add drift run metadata to domain model
- **Files to create**: `src/Modules/Graph/Graph.Domain/DriftRun/DriftRunRecord.cs`
- **Files to modify**: `src/Modules/Graph/Graph.Application/Ports/IDriftQueryService.cs`
- **Test plan (TDD)**:
  - Unit tests: `DriftRunRecordTests` – constructor validation, value equality
- **Acceptance criteria**:
  - `DriftRunRecord` record with: `LastRunAtUtc`, `Status` (enum: NotRun, Running, Completed, Failed), `Error` (nullable string), `DriftedCount` (int)
  - `IDriftQueryService` extended with `GetLatestRunAsync` returning `DriftRunRecord?`

#### 6.2 – Extend drift overview handler with run metadata
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetDriftOverview/GetDriftOverviewHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetDriftOverviewHandlerTests` – `Handle_WithCompletedRun_IncludesRunMetadata`, `Handle_WithNoRun_ReturnsNullMetadata`
- **Acceptance criteria**:
  - Response includes `lastRunAtUtc`, `status`, `error` from drift service
  - Null-safe when no drift run has occurred yet

#### 6.3 – Extend drift overview endpoint response
- **Files to modify**: `src/Modules/Graph/Graph.Api/Endpoints/GetDriftOverviewEndpoint.cs`
- **Test plan (TDD)**:
  - Module tests: `GetDriftOverviewEndpointTests` – response shape includes run metadata
- **Acceptance criteria**:
  - `/api/projects/{projectId}/drift/runs/latest` returns distinct response with run metadata
  - `/api/projects/{projectId}/drift` returns overview + latest run metadata embedded

#### 6.4 – Display drift run metadata in diagram UI
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx` (or drift panel component)
- **Test plan (TDD)**:
  - Unit tests: Component renders run timestamp, status badge, error message when present
- **Acceptance criteria**:
  - Drift panel shows "Last run: <relative time>" with status badge
  - Failed runs show error message
  - "Never run" state handled gracefully

#### 6.5 – Write drift run metadata tests
- **Files to create**: `src/Modules/Graph/Graph.Tests/GetDriftOverview/GetDriftOverviewHandlerTests.cs`
- **Test plan (TDD)**:
  - Unit tests: Handler with various run states
  - Module tests: Endpoint returns correct HTTP response with metadata
- **Acceptance criteria**:
  - All run states tested: NotRun, Running, Completed, Failed
  - Null metadata handled

### Epic 7: Typed Telemetry Targets v2 (P1)
Goal: Telemetry targets API supports typed provider metadata (auth mode, provider type)

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Create TelemetryTarget domain model | Feature | Telemetry | M | – | ⬚ |
| 7.2 | Migrate IAppInsightsConfigStore to typed targets | Refactor | Telemetry | L | 7.1 | ⬚ |
| 7.3 | Update TelemetryTargetsEndpoint for v2 model | Feature | Telemetry | M | 7.2 | ⬚ |
| 7.4 | Update ApplicationInsightsClient to use typed targets | Refactor | Telemetry | M | 7.2 | ⬚ |
| 7.5 | Write telemetry targets v2 tests | Test | Telemetry | M | 7.3, 7.4 | ⬚ |

#### 7.1 – Create TelemetryTarget domain model
- **Files to create**: `src/Modules/Telemetry/Telemetry.Domain/TelemetryTarget.cs`, `src/Modules/Telemetry/Telemetry.Domain/TelemetryProvider.cs`, `src/Modules/Telemetry/Telemetry.Domain/AuthMode.cs`
- **Test plan (TDD)**:
  - Unit tests: `TelemetryTargetTests` – construction, validation, equality
- **Acceptance criteria**:
  - `TelemetryTarget` record with: `Id`, `Provider` (enum: ApplicationInsights, OpenTelemetry), `AuthMode` (enum: ApiKey, ClientCredentials, Delegated), `ConnectionMetadata` (dictionary)
  - Backward-compatible with existing AppId/InstrumentationKey data

#### 7.2 – Migrate IAppInsightsConfigStore to typed targets
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Application/Ports/IAppInsightsConfigStore.cs`, `src/Modules/Telemetry/Telemetry.Infrastructure/Repositories/AppInsightsConfigStore.cs`
- **Files to create**: `src/Modules/Telemetry/Telemetry.Application/Ports/ITelemetryTargetStore.cs`
- **Test plan (TDD)**:
  - Unit tests: `TelemetryTargetStoreTests` – CRUD operations with typed targets
  - Integration tests: persistence round-trip with EF Core
- **Acceptance criteria**:
  - New `ITelemetryTargetStore` port with typed target CRUD
  - Existing `IAppInsightsConfigStore` retained for backward compatibility during migration
  - Data migration path from flat AppId strings to typed targets

#### 7.3 – Update TelemetryTargetsEndpoint for v2 model
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Api/Endpoints/TelemetryTargetsEndpoint.cs`
- **Test plan (TDD)**:
  - Module tests: `TelemetryTargetsEndpointTests` – POST/GET/DELETE with typed targets
- **Acceptance criteria**:
  - POST accepts typed target with provider, auth mode, metadata
  - GET returns typed targets with full metadata
  - DELETE by target ID
  - Backward-compatible: old-format requests still work

#### 7.4 – Update ApplicationInsightsClient to use typed targets
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Infrastructure/Adapters/ApplicationInsightsClient.cs`
- **Test plan (TDD)**:
  - Unit tests: Client resolves auth mode from typed target, uses correct auth flow per mode
- **Acceptance criteria**:
  - Client reads auth mode from typed target
  - ApiKey mode uses existing API key flow
  - ClientCredentials mode uses existing token provider
  - Delegated mode uses existing delegated token flow

#### 7.5 – Write telemetry targets v2 tests
- **Files to create**: `src/Modules/Telemetry/Telemetry.Tests/TelemetryTargets/TelemetryTargetStoreTests.cs`, `src/Modules/Telemetry/Telemetry.Tests/TelemetryTargets/TelemetryTargetsEndpointV2Tests.cs`
- **Test plan (TDD)**:
  - Unit tests: Store CRUD, backward compatibility
  - Module tests: Endpoint accepts both v1 and v2 formats
- **Acceptance criteria**:
  - Full test coverage for typed target lifecycle
  - Backward compatibility tests pass

### Epic 8: Export Fidelity Parity (P2)
Goal: SVG/PDF export renders overlay colors and edge states matching the canvas

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 8.1 | Add overlay-aware color mapping to SvgDiagramExporter | Feature | Visualization | M | – | ⬚ |
| 8.2 | Add overlay-aware color mapping to PdfDiagramExporter | Feature | Visualization | M | 8.1 | ⬚ |
| 8.3 | Pass full overlay data through ExportDiagramEndpoint | Feature | Visualization | M | 8.1 | ⬚ |
| 8.4 | Add legend rendering to SVG and PDF exports | Feature | Visualization | M | 8.1, 8.2 | ⬚ |
| 8.5 | Write export fidelity tests | Test | Visualization | M | 8.3, 8.4 | ⬚ |

#### 8.1 – Add overlay-aware color mapping to SvgDiagramExporter
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Persistence/SvgDiagramExporter.cs`
- **Test plan (TDD)**:
  - Unit tests: `SvgDiagramExporterTests` – `Export_WithThreatOverlay_UsesRiskColors`, `Export_WithSecurityOverlay_UsesSeverityColors`, `Export_WithNoOverlay_UsesDefaultColors`
- **Acceptance criteria**:
  - Node fill/stroke colors reflect active overlay (threat/cost/security)
  - Edge stroke color reflects traffic state (green/yellow/red/gray)
  - Derived edges render with dashed stroke-dasharray

#### 8.2 – Add overlay-aware color mapping to PdfDiagramExporter
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Persistence/PdfDiagramExporter.cs`
- **Test plan (TDD)**:
  - Unit tests: `PdfDiagramExporterTests` – `Export_WithOverlay_UsesCorrectColors`
- **Acceptance criteria**:
  - Same color mapping logic as SVG (shared utility)
  - PDF node/edge rendering respects overlay state

#### 8.3 – Pass full overlay data through ExportDiagramEndpoint
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Endpoints/ExportDiagramEndpoint.cs` (line 96+)
- **Test plan (TDD)**:
  - Module tests: `ExportDiagramEndpointTests` – `Export_WithOverlayMode_IncludesNodeOverlayData`
- **Acceptance criteria**:
  - Export payload includes per-node overlay data (risk level, cost, security severity)
  - Export payload includes per-edge telemetry state and derived flag
  - Overlay mode parameter drives which overlay data is included

#### 8.4 – Add legend rendering to SVG and PDF exports
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Persistence/SvgDiagramExporter.cs`, `src/Modules/Visualization/Visualization.Api/Persistence/PdfDiagramExporter.cs`
- **Test plan (TDD)**:
  - Unit tests: `SvgDiagramExporterTests` – `Export_WithOverlay_IncludesLegend`
- **Acceptance criteria**:
  - SVG/PDF includes color legend in bottom-right corner
  - Legend labels match active overlay (risk levels / cost tiers / severity levels)
  - No legend when overlay is 'none'

#### 8.5 – Write export fidelity tests
- **Files to create**: `src/Modules/Visualization/Visualization.Tests/Export/ExportFidelityTests.cs`
- **Test plan (TDD)**:
  - Unit tests: SVG/PDF exporters produce correct colors per overlay
  - Module tests: Export endpoint passes overlay data through to exporter
- **Acceptance criteria**:
  - All overlay modes tested for both SVG and PDF
  - Derived edge rendering tested
  - Legend inclusion tested

### Epic 9: Data Quality Improvement (P2)
Goal: Reduce fallback classifications and unknown environments by ≥30%

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 9.1 | Expand resource type catalog with missing ARM types | Feature | Discovery | M | – | ⬚ |
| 9.2 | Improve environment inference from resource metadata | Feature | Discovery | M | – | ⬚ |
| 9.3 | Add classification confidence feedback loop | Feature | Discovery | L | 9.1 | ⬚ |
| 9.4 | Write data quality improvement tests | Test | Discovery | M | 9.1, 9.2 | ⬚ |

#### 9.1 – Expand resource type catalog with missing ARM types
- **Files to modify**: Resource type catalog/mapping in Discovery module
- **Test plan (TDD)**:
  - Unit tests: `ResourceTypeCatalogTests` – `Classify_CommonArmTypes_ReturnsKnownClassification` for top 50 ARM types
- **Acceptance criteria**:
  - Catalog covers top 50 ARM resource types by production frequency
  - Each mapping includes: friendly name, service type, C4 level, infrastructure flag
  - Catalog-hit rate targets ≥70% (up from current ~43%)

#### 9.2 – Improve environment inference from resource metadata
- **Files to modify**: Environment inference logic in Discovery module
- **Test plan (TDD)**:
  - Unit tests: `EnvironmentInferenceTests` – `Infer_FromResourceGroupName_ReturnsEnvironment`, `Infer_FromTags_ReturnsEnvironment`, `Infer_FromSubscriptionName_ReturnsEnvironment`
- **Acceptance criteria**:
  - Inference checks: resource tags (environment/env), resource group name patterns, subscription name patterns
  - Known environments: production, staging, development, test, shared
  - Unknown rate targets ≤276 (30% reduction from 394)

#### 9.3 – Add classification confidence feedback loop
- **Files to modify**: `src/Modules/Discovery/Discovery.Infrastructure/AI/ResourceClassifierPlugin.cs`
- **Files to create**: Classification feedback storage port and adapter
- **Test plan (TDD)**:
  - Unit tests: `ClassificationFeedbackTests` – feedback stored and applied on next classification
- **Acceptance criteria**:
  - When user corrects a classification, correction is stored
  - Next discovery run for same ARM type uses correction over AI/catalog
  - Corrections are project-scoped

#### 9.4 – Write data quality improvement tests
- **Files to create**: `src/Modules/Discovery/Discovery.Tests/DataQuality/DataQualityTests.cs`
- **Test plan (TDD)**:
  - Unit tests: Catalog coverage tests, environment inference tests
- **Acceptance criteria**:
  - Tests verify catalog covers expected ARM types
  - Tests verify environment inference from various metadata sources

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | App Insights API auth changes break telemetry pipeline | Medium | High | Task 1.4 covers auth edge cases; maintain existing fallback chains |
| R2 | Typed telemetry targets migration breaks existing configs | Medium | High | Task 7.2 keeps backward compatibility; migration tested with production data shapes |
| R3 | Export overlay rendering significantly increases file size | Low | Medium | Use shared color utility; test with 500-node graph; add size warning if >10MB |
| R4 | Code-level C4 nodes create overwhelming graph density | Medium | Medium | Default to collapsed Code level; only expand on drill-down |
| R5 | AI classifier prompt changes alter existing classification quality | Medium | High | Task 3.3 includes regression tests against known ARM type classifications |
| R6 | Drift run metadata requires new persistence schema | Low | Low | Use JSON column or existing table extension; no new migration needed if using existing drift table |
| R7 | Frontend color changes cause accessibility regression | Low | Medium | Task 5.2 includes WCAG contrast ratio verification |

### Critical Path
1.1 → 1.3 → 2.1 → 2.2 → 2.3 (telemetry → edges → UI)
3.1 → 3.2 (Code-level classification → rendering)
4.1 → 4.4 → 4.5 (overlay provenance)
6.1 → 6.2 → 6.3 → 6.4 (drift metadata)
7.1 → 7.2 → 7.3 → 7.4 (typed targets)
8.1 → 8.3 → 8.4 (export fidelity)

**Longest chain**: 1.1 → 1.3 → 2.1 → 2.2 → 2.3 (5 tasks)

### Parallelization Opportunities
- Epics 1+3+4+5+6 can start in parallel (no cross-dependencies until 2.1 depends on 1.1)
- Epic 8 and 9 can run fully in parallel with all other epics
- Within Epic 4, tasks 4.1/4.2/4.3 can run in parallel

### Estimated Total Effort
- S tasks: 10 × ~30 min = ~5 h
- M tasks: 20 × ~2.5 h = ~50 h
- L tasks: 2 × ~6 h = ~12 h
- XL tasks: 0
- **Total: ~67 hours**
