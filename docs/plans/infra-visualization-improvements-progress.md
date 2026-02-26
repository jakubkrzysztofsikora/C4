# Progress: InfraVisualizationImprovements
Scope: FeatureSet
Created: 2026-02-26
Last Updated: 2026-02-26
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: Cross-Module Contract Fixes
- [ ] 1.1 – Move TelemetryUpdatedIntegrationEvent to Shared.Kernel.IntegrationEvents
- [ ] 1.2 – Move DriftDetectedIntegrationEvent to Shared.Kernel.IntegrationEvents
- [ ] 1.3 – Extract ServiceHealthStatus.FromScore domain helper in Telemetry.Domain
- [ ] 1.4 – Extract trafficColor/healthColor utility in frontend diagram feature
- [ ] 1.5 – Write tests for Epic 1 changes

### Epic 2: Telemetry → Visualization Real-Time Bridge
- [ ] 2.1 – Add NotifyHealthOverlayChangedAsync to IDiagramNotifier port
- [ ] 2.2 – Implement NotifyHealthOverlayChangedAsync in SignalRDiagramNotifier
- [ ] 2.3 – Create TelemetryUpdatedHandler in Visualization.Application
- [ ] 2.4 – Register TelemetryUpdatedHandler in Visualization DI
- [ ] 2.5 – Write tests for telemetry-to-visualization bridge

### Epic 3: Enrich Graph API with Health and Traffic Data
- [ ] 3.1 – Define ITelemetryQueryService port in Shared.Kernel for cross-module telemetry reads
- [ ] 3.2 – Implement TelemetryQueryService adapter in Telemetry.Infrastructure
- [ ] 3.3 – Add Health and Traffic fields to GraphNodeDto and GraphEdgeDto
- [ ] 3.4 – Enrich GetGraphHandler to merge telemetry data into graph response
- [ ] 3.5 – Update frontend useDiagram to map real health/traffic from API
- [ ] 3.6 – Write tests for enriched graph API and frontend mapping

### Epic 4: Frontend SignalR Integration
- [ ] 4.1 – Add @microsoft/signalr npm dependency
- [ ] 4.2 – Create SignalR hub connection manager replacing WebSocketManager
- [ ] 4.3 – Create useSignalR hook for diagram feature
- [ ] 4.4 – Integrate useSignalR into useDiagram for real-time health updates
- [ ] 4.5 – Remove dead WebSocket code and unused components
- [ ] 4.6 – Write tests for SignalR integration

### Epic 5: Azure Resource Type Classifier
- [ ] 5.1 – Create AzureResourceClassification value object in Discovery.Domain
- [ ] 5.2 – Create AzureResourceTypeCatalog with classification rules
- [ ] 5.3 – Enrich DiscoveredResource with classification and friendly name
- [ ] 5.4 – Enrich ResourcesDiscoveredIntegrationEvent with classification data
- [ ] 5.5 – Update ResourcesDiscoveredHandler in Graph to use classification for C4 level
- [ ] 5.6 – Filter excluded resources before publishing integration event
- [ ] 5.7 – Update FakeAzureResourceGraphClient with diverse resource types
- [ ] 5.8 – Write tests for resource classifier

### Epic 6: Resource Grouping and Parent-Child Hierarchy
- [ ] 6.1 – Propagate ParentResourceId through Discovery to integration event
- [ ] 6.2 – Update ArchitectureGraph.AddOrUpdateNode to accept parentId
- [ ] 6.3 – Update ResourcesDiscoveredHandler to set parent-child relationships
- [ ] 6.4 – Add ParentId to GraphNodeDto and update GetGraphHandler
- [ ] 6.5 – Update frontend types and useDiagram to handle hierarchical nodes
- [ ] 6.6 – Write tests for parent-child hierarchy

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-26 | Initial plan created | – | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-26 | Use Shared.Kernel contract interface (ITelemetryQueryService) for cross-module telemetry reads instead of HTTP | Avoids network overhead for in-process monolith; maintains module decoupling via abstraction |
| 2026-02-26 | Replace WebSocket with SignalR JS client rather than adding a compatibility layer | Backend already uses SignalR; raw WebSocket cannot negotiate SignalR protocol |
| 2026-02-26 | Resource classifier lives in Discovery.Domain as a static catalog, not configuration | ARM resource types are stable; domain logic is the right home. Can be promoted to config-driven later |
| 2026-02-26 | Edge traffic derived from average of source+target node health scores as MVP heuristic | True per-edge traffic requires Application Insights dependency telemetry ingestion, which is out of scope |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
