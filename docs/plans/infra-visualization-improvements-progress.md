# Progress: InfraVisualizationImprovements
Scope: FeatureSet
Created: 2026-02-26
Last Updated: 2026-02-26
Status: Completed

## Current Focus
All epics completed

## Task Progress

### Epic 1: Cross-Module Contract Fixes
- [x] 1.1 – Move TelemetryUpdatedIntegrationEvent to Shared.Kernel.IntegrationEvents
- [x] 1.2 – Move DriftDetectedIntegrationEvent to Shared.Kernel.IntegrationEvents
- [x] 1.3 – Extract ServiceHealthStatus.FromScore domain helper in Telemetry.Domain
- [x] 1.4 – Extract trafficColor/healthColor utility in frontend diagram feature
- [x] 1.5 – Write tests for Epic 1 changes

### Epic 2: Telemetry → Visualization Real-Time Bridge
- [x] 2.1 – Add NotifyHealthOverlayChangedAsync to IDiagramNotifier port
- [x] 2.2 – Implement NotifyHealthOverlayChangedAsync in SignalRDiagramNotifier
- [x] 2.3 – Create TelemetryUpdatedHandler in Visualization.Application
- [x] 2.4 – Register TelemetryUpdatedHandler in Visualization DI
- [x] 2.5 – Write tests for telemetry-to-visualization bridge

### Epic 3: Enrich Graph API with Health and Traffic Data
- [x] 3.1 – Define ITelemetryQueryService port in Shared.Kernel for cross-module telemetry reads
- [x] 3.2 – Implement TelemetryQueryService adapter in Telemetry.Infrastructure
- [x] 3.3 – Add Health and Traffic fields to GraphNodeDto and GraphEdgeDto
- [x] 3.4 – Enrich GetGraphHandler to merge telemetry data into graph response
- [x] 3.5 – Update frontend useDiagram to map real health/traffic from API
- [x] 3.6 – Write tests for enriched graph API and frontend mapping

### Epic 4: Frontend SignalR Integration
- [x] 4.1 – Add @microsoft/signalr npm dependency
- [x] 4.2 – Create SignalR hub connection manager replacing WebSocketManager
- [x] 4.3 – Create useSignalR hook for diagram feature
- [x] 4.4 – Integrate useSignalR into useDiagram for real-time health updates
- [x] 4.5 – Remove dead WebSocket code and unused components
- [x] 4.6 – Write tests for SignalR integration

### Epic 5: Azure Resource Type Classifier with SK AI Agent
- [x] 5.1 – Create AzureResourceClassification value object in Discovery.Domain
- [x] 5.2 – Create AzureResourceTypeCatalog with classification rules (20+ ARM types)
- [x] 5.3 – Create IResourceClassifier port and ResourceClassifierPlugin SK adapter
- [x] 5.4 – Enrich DiscoveredResource with classification and friendly name
- [x] 5.5 – Enrich ResourcesDiscoveredIntegrationEvent with classification data
- [x] 5.6 – Update ResourcesDiscoveredHandler in Graph to use classification for C4 level
- [x] 5.7 – Filter excluded resources before publishing integration event
- [x] 5.8 – Update FakeAzureResourceGraphClient with 12 diverse resource types
- [x] 5.9 – Wire SK Kernel and ResourceClassifierPlugin in Discovery DI
- [x] 5.10 – Write tests for resource classifier and handler filtering

### Epic 6: Resource Grouping and Parent-Child Hierarchy
- [x] 6.1 – Add ParentResourceId to DiscoveredResourceEventItem
- [x] 6.2 – Propagate ParentResourceId in DiscoverResourcesHandler
- [x] 6.3 – Add SetParent to GraphNode and ResolveNodeParents to ArchitectureGraph
- [x] 6.4 – Update ResourcesDiscoveredHandler with two-pass parent resolution
- [x] 6.5 – Add ParentNodeId to GraphNodeDto and update GetGraphHandler
- [x] 6.6 – Update frontend types and useDiagram to handle hierarchical nodes
- [x] 6.7 – Write tests for parent-child hierarchy

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-26 | Initial plan created | – | – |
| 2026-02-26 | Epic 5 expanded: added SK AI agent for unknown resource classification | User requested leveraging SK agents for classification/grouping work | Added IResourceClassifier port, ResourceClassifierPlugin with LLM fallback |
| 2026-02-26 | Epic 5 tasks reorganized from 5.1-5.8 to 5.1-5.10 | SK integration added more tasks | +2 tasks for SK DI wiring and plugin implementation |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-26 | Use Shared.Kernel contract interface (ITelemetryQueryService) for cross-module telemetry reads instead of HTTP | Avoids network overhead for in-process monolith; maintains module decoupling via abstraction |
| 2026-02-26 | Replace WebSocket with SignalR JS client rather than adding a compatibility layer | Backend already uses SignalR; raw WebSocket cannot negotiate SignalR protocol |
| 2026-02-26 | Resource classifier uses static catalog + SK AI agent fallback for unknowns | Catalog covers 20+ known ARM types. SK LLM classifies unknown types via Ollama with structured prompt. Falls back to catalog default on LLM failure |
| 2026-02-26 | Edge traffic derived from average of source+target node health scores as MVP heuristic | True per-edge traffic requires Application Insights dependency telemetry ingestion, which is out of scope |
| 2026-02-26 | Two-pass parent resolution in ResourcesDiscoveredHandler | First pass adds all nodes, second pass resolves parents. Handles cases where parent appears after child in the batch |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
| Date | Epic | Tasks |
|------|------|-------|
| 2026-02-26 | Epic 1 | 1.1–1.5: Cross-module contract fixes |
| 2026-02-26 | Epic 2 | 2.1–2.5: Telemetry → Visualization bridge |
| 2026-02-26 | Epic 3 | 3.1–3.6: Graph API health/traffic enrichment |
| 2026-02-26 | Epic 4 | 4.1–4.6: Frontend SignalR integration |
| 2026-02-26 | Epic 5 | 5.1–5.10: SK-powered resource type classifier |
| 2026-02-26 | Epic 6 | 6.1–6.7: Parent-child resource hierarchy |
