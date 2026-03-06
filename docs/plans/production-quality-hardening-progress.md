# Progress: ProductionQualityHardening
Scope: FeatureSet
Created: 2026-03-06
Last Updated: 2026-03-06
Status: In Progress

## Current Focus
All epics implemented – pending test coverage and final verification

## Task Progress

### Epic 1: Real Telemetry Ingestion (P0)
- [x] 1.1 – Fix project-scoped credential resolution in ApplicationInsightsClient
- [x] 1.2 – Add telemetry ingestion diagnostic endpoint
- [x] 1.3 – Propagate ingestion status to graph quality metrics
- [ ] 1.4 – Write telemetry ingestion tests

### Epic 2: True Edge Telemetry (P0)
- [x] 2.1 – Replace averaged-node fallback with explicit derived marker
- [x] 2.2 – Add edge telemetry provenance to GraphEdgeDto
- [x] 2.3 – Display derived-edge indicator in diagram UI
- [ ] 2.4 – Write edge telemetry tests

### Epic 3: Code-Level C4 Population (P0)
- [x] 3.1 – Add "Code" to valid C4 levels in ResourceClassifierPlugin
- [x] 3.2 – Add Code-level rendering support in graph handler
- [ ] 3.3 – Write Code-level classification tests

### Epic 4: Overlay Provenance Accuracy (P1)
- [x] 4.1 – Fix threat assessment provenance tracking
- [x] 4.2 – Fix security findings provenance tracking
- [x] 4.3 – Fix cost insights provenance tracking (verified already correct)
- [x] 4.4 – Add provenance metadata to overlay responses
- [ ] 4.5 – Write overlay provenance tests

### Epic 5: Security Overlay Color Fix (P1)
- [x] 5.1 – Change security color default from green to gray
- [ ] 5.2 – Write security color mapping tests

### Epic 6: Drift Run Metadata (P1)
- [x] 6.1 – Add drift run metadata to domain model
- [x] 6.2 – Extend drift overview handler with run metadata
- [x] 6.3 – Extend drift overview endpoint response
- [x] 6.4 – Display drift run metadata in diagram UI
- [ ] 6.5 – Write drift run metadata tests

### Epic 7: Typed Telemetry Targets v2 (P1)
- [x] 7.1 – Create TelemetryTarget domain model
- [x] 7.2 – Migrate IAppInsightsConfigStore to typed targets
- [x] 7.3 – Update TelemetryTargetsEndpoint for v2 model
- [x] 7.4 – Update ApplicationInsightsClient to use typed targets
- [ ] 7.5 – Write telemetry targets v2 tests

### Epic 8: Export Fidelity Parity (P2)
- [x] 8.1 – Add overlay-aware color mapping to SvgDiagramExporter
- [x] 8.2 – Add overlay-aware color mapping to PdfDiagramExporter
- [x] 8.3 – Pass full overlay data through ExportDiagramEndpoint
- [x] 8.4 – Add legend rendering to SVG and PDF exports
- [ ] 8.5 – Write export fidelity tests

### Epic 9: Data Quality Improvement (P2)
- [x] 9.1 – Expand resource type catalog with missing ARM types
- [x] 9.2 – Improve environment inference from resource metadata
- [ ] 9.3 – Add classification confidence feedback loop
- [ ] 9.4 – Write data quality improvement tests

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-03-06 | Initial plan created from production probe | Production probe revealed 10 gaps across P0–P2 | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-03-06 | Treat overlay provenance as 3 distinct values (ai, rule-based, heuristic) not binary | Threat handler uses AI with timeout fallback (ai vs rule-based); security findings are deterministic rules (rule-based); cost estimates are genuinely heuristic |
| 2026-03-06 | Security unknown/none maps to gray not green | Green implies "safe/verified"; gray communicates "no data" without false confidence |
| 2026-03-06 | Keep backward compatibility for telemetry targets v1 API during v2 migration | Existing production configs use v1 format; breaking change would require coordinated deploy |
| 2026-03-06 | Derived edges use dashed lines in UI (not different color) | Color already encodes traffic health state; dashing communicates data confidence without color conflict |
| 2026-03-06 | Code-level C4 only from AI classification when repo source configured | Code-level cannot be inferred from ARM discovery alone; requires source code analysis |
| 2026-03-06 | Separate ApiKey from InstrumentationKey in AppInsightsConfig | InstrumentationKey is for identification (GUID), ApiKey is for querying; conflating them caused auth failures returning 0 metrics |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
| Date | Task | Summary |
|------|------|---------|
| 2026-03-06 | 1.1 | Added ApiKey field to AppInsightsConfigEntity, separated from InstrumentationKey. Updated ResolveApiKey to check ApiKey → legacy key → global key → token provider |
| 2026-03-06 | 1.2 | Created GET /api/projects/{projectId}/telemetry/diagnostics endpoint reporting credential resolution status and issues |
| 2026-03-06 | 1.3 | KnownNodes/KnownEdges now computed in graph quality metrics (covered by Epic 2 changes) |
| 2026-03-06 | 2.1 | Added IsDerived flag to edge construction in GetGraphHandler; only derived edges use averaged metrics |
| 2026-03-06 | 2.2 | Added IsDerived to GraphEdgeDto, TelemetrySource/Window tracking |
| 2026-03-06 | 2.3 | Derived edges render with dashed lines in DiagramCanvas; telemetrySource added to DiagramEdge type |
| 2026-03-06 | 3.1 | Added "Code" to valid C4 levels in ResourceClassifierPlugin prompt and validation |
| 2026-03-06 | 3.2 | Graph handler already supports all C4 levels dynamically |
| 2026-03-06 | 4.1 | Threat handler tracks "ai" vs "rule-based" provenance based on detector completion |
| 2026-03-06 | 4.2 | Security findings provenance changed from "heuristic" to "rule-based" |
| 2026-03-06 | 4.3 | Cost insights verified already correct ("heuristic", IsHeuristic: true) |
| 2026-03-06 | 4.4 | All three overlay responses include accurate DataProvenance and IsHeuristic fields |
| 2026-03-06 | 5.1 | Security color default changed from #2e8f5e (green) to #6b7280 (gray) in DiagramCanvas |
| 2026-03-06 | 6.1 | Created DriftRunRecord and DriftRunStatus in Shared Kernel |
| 2026-03-06 | 6.2 | GetDriftOverviewHandler fetches run metadata via IDriftQueryService.GetLatestRunAsync |
| 2026-03-06 | 6.3 | GetDriftOverviewResponse extended with LastRunAtUtc, Status, Error |
| 2026-03-06 | 7.1 | Created TelemetryTarget, TelemetryProvider, AuthMode domain types |
| 2026-03-06 | 7.2 | Created ITelemetryTargetStore port and TelemetryTargetStore adapter bridging to IAppInsightsConfigStore |
| 2026-03-06 | 7.3 | Added GET /api/projects/{projectId}/telemetry/targets/v2 endpoint with typed response |
| 2026-03-06 | 7.4 | ApplicationInsightsClient now uses separate ApiKey field for auth resolution |
| 2026-03-06 | 8.1 | SvgDiagramExporter uses ExportColorResolver for overlay-aware node/edge colors |
| 2026-03-06 | 8.2 | PdfDiagramExporter uses ExportColorResolver with HexToRgb conversion |
| 2026-03-06 | 8.3 | ExportDiagramEndpoint passes full overlay data (riskLevel, hourlyCostUsd, trafficState, securitySeverity, isDerived) |
| 2026-03-06 | 8.4 | SVG exports include legend when overlay is active |
| 2026-03-06 | 9.1 | Added Azure OpenAI, Databricks, Data Factory, Synapse, Stream Analytics, Data Explorer, Managed HSM, Firewall types |
| 2026-03-06 | 9.2 | Expanded EnvironmentClassifier: 5 new tag keys, 11 new patterns (sbx, perf, load, sit, dr, hotfix, canary, preview, int, acc, acceptance) |
