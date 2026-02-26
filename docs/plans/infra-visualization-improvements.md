## Plan: InfraVisualizationImprovements
Scope: FeatureSet
Created: 2026-02-26
Status: Draft

### Overview
Close the end-to-end gaps in the infrastructure visualization pipeline identified during validation. This covers six areas: (1) fixing cross-module integration event contracts, (2) wiring the Telemetry → Visualization real-time bridge, (3) enriching the Graph API with health/traffic data, (4) replacing the frontend's raw WebSocket with SignalR, (5) building an Azure resource type classifier to reduce noise, and (6) implementing resource grouping and parent-child hierarchy in the architecture graph. The goal is to make the "Google Maps"-like traffic overlay functional with real telemetry data and make Azure resource discovery produce clean, noise-free architecture diagrams.

### Success Criteria
- [ ] `TelemetryUpdatedIntegrationEvent` flows end-to-end from telemetry ingest to SignalR push to frontend diagram update
- [ ] Graph API returns health scores on nodes and traffic scores on edges from real telemetry data
- [ ] Frontend diagram renders real health/traffic colors from API data (not hardcoded green/1.0)
- [ ] Frontend connects to backend via SignalR and receives real-time `HealthOverlayChanged` and `DiagramUpdated` pushes
- [ ] Azure resources are classified by type — infrastructure noise (NICs, NSGs, Disks) is excluded from diagrams
- [ ] Resources are mapped to friendly names and correct C4 levels based on a type catalog
- [ ] `ParentResourceId` is used to create node hierarchies in the architecture graph
- [ ] Related Azure resources are grouped into logical composite nodes
- [ ] All integration events consumed cross-module live in `Shared.Kernel.IntegrationEvents`
- [ ] Health threshold logic (>=0.8 green, >=0.5 yellow, <0.5 red) is defined once per language layer
- [ ] All existing + new tests pass; solution compiles with zero warnings

---

### Epic 1: Cross-Module Contract Fixes
Goal: Fix integration event placement and extract duplicated domain logic so that subsequent epics can safely consume events across module boundaries.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Move TelemetryUpdatedIntegrationEvent to Shared.Kernel.IntegrationEvents | Refactor | Telemetry, Shared | S | – | ⬚ |
| 1.2 | Move DriftDetectedIntegrationEvent to Shared.Kernel.IntegrationEvents | Refactor | Discovery, Shared | S | – | ⬚ |
| 1.3 | Extract ServiceHealthStatus.FromScore domain helper in Telemetry.Domain | Refactor | Telemetry | S | – | ⬚ |
| 1.4 | Extract trafficColor/healthColor utility in frontend diagram feature | Refactor | Frontend | S | – | ⬚ |
| 1.5 | Write tests for Epic 1 changes | Test | Telemetry, Discovery, Frontend | S | 1.1–1.4 | ⬚ |

#### 1.1 – Move TelemetryUpdatedIntegrationEvent to Shared.Kernel
- **Files to modify**: `src/Shared/Kernel/IntegrationEvents/TelemetryUpdatedIntegrationEvent.cs` (create), `src/Modules/Telemetry/Telemetry.Application/IntegrationEvents/TelemetryUpdatedIntegrationEvent.cs` (delete)
- **Files to modify**: All files that `using C4.Modules.Telemetry.Application.IntegrationEvents` → change to `using C4.Shared.Kernel.IntegrationEvents`
- **Test plan (TDD)**:
  - Unit tests: Existing `IngestTelemetryHandlerTests` and `SyncApplicationInsightsTelemetryHandlerTests` must still pass with the moved type
- **Acceptance criteria**:
  - `TelemetryUpdatedIntegrationEvent` and `TelemetryUpdatedServiceItem` live in `C4.Shared.Kernel.IntegrationEvents`
  - No module references `C4.Modules.Telemetry.Application.IntegrationEvents` for this event
  - All existing tests pass

#### 1.2 – Move DriftDetectedIntegrationEvent to Shared.Kernel
- **Files to modify**: `src/Shared/Kernel/IntegrationEvents/DriftDetectedIntegrationEvent.cs` (create), `src/Modules/Discovery/Discovery.Application/IntegrationEvents/DriftDetectedIntegrationEvent.cs` (delete)
- **Files to modify**: All files that `using C4.Modules.Discovery.Application.IntegrationEvents` → change to `using C4.Shared.Kernel.IntegrationEvents`
- **Test plan (TDD)**:
  - Unit tests: Existing `DetectDriftHandlerTests` must still pass
- **Acceptance criteria**:
  - `DriftDetectedIntegrationEvent` and `DriftDetectedEventItem` live in `C4.Shared.Kernel.IntegrationEvents`
  - All existing tests pass

#### 1.3 – Extract ServiceHealthStatus.FromScore Domain Helper
- **Files to create**: `src/Modules/Telemetry/Telemetry.Domain/Metrics/ServiceHealthStatusExtensions.cs`
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Application/IngestTelemetry/IngestTelemetryHandler.cs` (remove private `ToStatus`), `src/Modules/Telemetry/Telemetry.Application/SyncApplicationInsightsTelemetry/SyncApplicationInsightsTelemetryHandler.cs` (remove private `ToStatus`), `src/Modules/Telemetry/Telemetry.Infrastructure/Repositories/TelemetryRepository.cs` (use shared method)
- **Test plan (TDD)**:
  - Unit tests: `ServiceHealthStatusExtensionsTests` – `FromScore_ScoreAbove08_ReturnsGreen`, `FromScore_ScoreExactly08_ReturnsGreen`, `FromScore_ScoreAbove05_ReturnsYellow`, `FromScore_ScoreExactly05_ReturnsYellow`, `FromScore_ScoreBelow05_ReturnsRed`, `FromScore_Zero_ReturnsRed`
- **Acceptance criteria**:
  - Single `FromScore(double)` method in Telemetry.Domain used by all handlers
  - No duplicate threshold logic in C# code
  - All existing tests pass

#### 1.4 – Extract Frontend Traffic/Health Color Utility
- **Files to create**: `web/src/features/diagram/utils.ts`
- **Files to modify**: `web/src/features/diagram/components/DiagramCanvas.tsx` (import from utils), `web/src/features/diagram/hooks/useDiagramExport.ts` (import from utils), `web/src/features/diagram/components/GraphEdge.tsx` (import from utils)
- **Test plan (TDD)**:
  - Unit tests: `utils.test.ts` – `trafficColor_highTraffic_returnsGreen`, `trafficColor_mediumTraffic_returnsYellow`, `trafficColor_lowTraffic_returnsRed`, `trafficColor_boundaryAt08_returnsGreen`, `trafficColor_boundaryAt05_returnsYellow`
- **Acceptance criteria**:
  - Single `trafficColor(traffic: number): string` function used by all frontend files
  - No duplicate threshold logic in TypeScript code
  - All frontend tests pass

#### 1.5 – Write Tests for Epic 1 Changes
- **Files to create**: `src/Modules/Telemetry/Telemetry.Tests/Domain/ServiceHealthStatusExtensionsTests.cs`, `web/src/features/diagram/utils.test.ts`
- **Test plan (TDD)**:
  - Unit tests: As defined in 1.3 and 1.4
  - Regression: Run all existing test suites to confirm no breakage from moves
- **Acceptance criteria**:
  - All new tests green
  - All existing tests green
  - Zero compilation warnings

---

### Epic 2: Telemetry → Visualization Real-Time Bridge
Goal: Create the missing backend handler that subscribes to `TelemetryUpdatedIntegrationEvent` and pushes health data to the frontend via SignalR.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Add NotifyHealthOverlayChangedAsync to IDiagramNotifier port | Feature | Visualization | S | 1.1 | ⬚ |
| 2.2 | Implement NotifyHealthOverlayChangedAsync in SignalRDiagramNotifier | Feature | Visualization | S | 2.1 | ⬚ |
| 2.3 | Create TelemetryUpdatedHandler in Visualization.Application | Feature | Visualization | M | 2.2 | ⬚ |
| 2.4 | Register TelemetryUpdatedHandler in Visualization DI | Infrastructure | Visualization | S | 2.3 | ⬚ |
| 2.5 | Write tests for telemetry-to-visualization bridge | Test | Visualization | M | 2.3 | ⬚ |

#### 2.1 – Add NotifyHealthOverlayChangedAsync to IDiagramNotifier
- **Files to modify**: `src/Modules/Visualization/Visualization.Application/Ports/IDiagramNotifier.cs`
- **Test plan (TDD)**:
  - Covered by 2.5
- **Acceptance criteria**:
  - `IDiagramNotifier` has `Task NotifyHealthOverlayChangedAsync(Guid projectId, string healthJson, CancellationToken cancellationToken)`
  - Compiles cleanly

#### 2.2 – Implement NotifyHealthOverlayChangedAsync in SignalRDiagramNotifier
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/Adapters/SignalRDiagramNotifier.cs`
- **Test plan (TDD)**:
  - Covered by 2.5
- **Acceptance criteria**:
  - Calls `hubContext.Clients.Group(projectId).HealthOverlayChanged(projectId, healthJson)`
  - Compiles cleanly

#### 2.3 – Create TelemetryUpdatedHandler in Visualization.Application
- **Files to create**: `src/Modules/Visualization/Visualization.Application/IntegrationEventHandlers/TelemetryUpdatedHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `TelemetryUpdatedHandlerTests` – `Handle_ValidEvent_CallsNotifier`, `Handle_MultipleServices_SerializesAll`, `Handle_EmptyServices_CallsNotifierWithEmptyJson`
  - Fakes needed: `FakeDiagramNotifier` (in-memory, records calls)
- **Acceptance criteria**:
  - Subscribes to `TelemetryUpdatedIntegrationEvent` via `INotificationHandler<>`
  - Serializes service health data to JSON
  - Calls `IDiagramNotifier.NotifyHealthOverlayChangedAsync` with the project ID and health JSON
  - Does not reference any infrastructure types

#### 2.4 – Register TelemetryUpdatedHandler in Visualization DI
- **Files to modify**: `src/Modules/Visualization/Visualization.Api/ServiceCollectionExtensions.cs`
- **Test plan (TDD)**:
  - Smoke test: Verify handler is resolvable from DI container
- **Acceptance criteria**:
  - MediatR assembly scanning picks up the Visualization.Application assembly (already registered — verify)
  - No additional registration needed if assembly scanning is correct

#### 2.5 – Write Tests for Telemetry-to-Visualization Bridge
- **Files to create**: `src/Modules/Visualization/Visualization.Tests/Application/TelemetryUpdatedHandlerTests.cs`, `src/Modules/Visualization/Visualization.Tests/Fakes/FakeDiagramNotifier.cs`
- **Test plan (TDD)**:
  - Unit tests: As defined in 2.3
  - Integration test: `TelemetryToVisualizationBridgeTests` – `IngestTelemetry_PublishesTelemetryUpdated_NotifiesDiagramHub` (end-to-end via MediatR in a test host)
- **Acceptance criteria**:
  - All new tests green
  - Handler correctly bridges the event

---

### Epic 3: Enrich Graph API with Health and Traffic Data
Goal: Add health scores to nodes and traffic scores to edges in the Graph API response so the frontend can render real telemetry-driven colors.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Define ITelemetryQueryService port in Shared.Kernel for cross-module telemetry reads | Feature | Shared | S | 1.1 | ⬚ |
| 3.2 | Implement TelemetryQueryService adapter in Telemetry.Infrastructure | Feature | Telemetry | M | 3.1 | ⬚ |
| 3.3 | Add Health and Traffic fields to GraphNodeDto and GraphEdgeDto | Feature | Graph | S | 3.1 | ⬚ |
| 3.4 | Enrich GetGraphHandler to merge telemetry data into graph response | Feature | Graph | M | 3.2, 3.3 | ⬚ |
| 3.5 | Update frontend useDiagram to map real health/traffic from API | Feature | Frontend | M | 3.3 | ⬚ |
| 3.6 | Write tests for enriched graph API and frontend mapping | Test | Graph, Telemetry, Frontend | M | 3.4, 3.5 | ⬚ |

#### 3.1 – Define ITelemetryQueryService in Shared.Kernel
- **Files to create**: `src/Shared/Kernel/Contracts/ITelemetryQueryService.cs`
- **Test plan (TDD)**:
  - Covered by 3.6
- **Acceptance criteria**:
  - Interface with `Task<IReadOnlyCollection<ServiceHealthSummary>> GetServiceHealthSummariesAsync(Guid projectId, CancellationToken ct)`
  - `ServiceHealthSummary` record: `(string Service, double Score, string Status)`
  - Lives in Shared.Kernel so Graph module can depend on it without referencing Telemetry

#### 3.2 – Implement TelemetryQueryService Adapter
- **Files to create**: `src/Modules/Telemetry/Telemetry.Infrastructure/Services/TelemetryQueryService.cs`
- **Files to modify**: `src/Modules/Telemetry/Telemetry.Api/ServiceCollectionExtensions.cs` (register adapter)
- **Test plan (TDD)**:
  - Integration test: `TelemetryQueryServiceTests` – `GetServiceHealthSummaries_WithMetrics_ReturnsSummaries`, `GetServiceHealthSummaries_NoMetrics_ReturnsEmpty`
- **Acceptance criteria**:
  - Queries `ITelemetryRepository` for all service health records for the project
  - Maps to `ServiceHealthSummary` DTOs
  - Registered as scoped in Telemetry DI

#### 3.3 – Add Health/Traffic Fields to GraphDto
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GraphDto.cs`
- **Test plan (TDD)**:
  - Covered by 3.6
- **Acceptance criteria**:
  - `GraphNodeDto` gains: `string Health` (green/yellow/red), `double HealthScore`
  - `GraphEdgeDto` gains: `double Traffic`
  - Defaults: Health = "green", HealthScore = 1.0, Traffic = 1.0 (backward compatible)

#### 3.4 – Enrich GetGraphHandler with Telemetry Data
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Files to modify**: `src/Modules/Graph/Graph.Api/ServiceCollectionExtensions.cs` (ensure ITelemetryQueryService is resolvable)
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_WithTelemetry_ReturnsEnrichedNodes`, `Handle_NoTelemetry_ReturnsDefaultHealth`, `Handle_PartialTelemetry_EnrichesMatchingNodesOnly`
  - Fakes needed: `FakeTelemetryQueryService`
- **Acceptance criteria**:
  - Handler injects `ITelemetryQueryService`
  - Matches service health summaries to graph nodes by name
  - Populates Health and HealthScore on matched nodes
  - Unmatched nodes get default green/1.0
  - Edge traffic derived from average of source+target node health scores

#### 3.5 – Update Frontend useDiagram to Use Real API Data
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.ts` (remove hardcoded `health: 'green'` and `traffic: 1`), `web/src/features/diagram/types.ts` (if needed)
- **Test plan (TDD)**:
  - Unit tests: `useDiagram.test.tsx` – `useDiagram_apiDataWithHealth_mapsHealthCorrectly`, `useDiagram_apiDataWithTraffic_mapsTrafficCorrectly`, `useDiagram_apiDataMissingHealth_defaultsToGreen`
- **Acceptance criteria**:
  - `mapGraphDtoToDiagramData` reads health and traffic from API response
  - Falls back to green/1.0 only if API fields are missing (backward compatible)
  - Diagram renders real colors from backend telemetry

#### 3.6 – Write Tests for Enriched Graph API
- **Files to create**: `src/Modules/Graph/Graph.Tests/Application/GetGraphHandlerEnrichmentTests.cs`, `src/Modules/Graph/Graph.Tests/Fakes/FakeTelemetryQueryService.cs`
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.test.tsx`
- **Test plan (TDD)**:
  - As defined in 3.2, 3.4, 3.5
- **Acceptance criteria**:
  - All new tests green
  - All existing tests green

---

### Epic 4: Frontend SignalR Integration
Goal: Replace the raw WebSocket client with the SignalR JavaScript client so the frontend can receive real-time diagram and health updates from the backend hub.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Add @microsoft/signalr npm dependency | Infrastructure | Frontend | S | – | ⬚ |
| 4.2 | Create SignalR hub connection manager replacing WebSocketManager | Feature | Frontend | M | 4.1 | ⬚ |
| 4.3 | Create useSignalR hook for diagram feature | Feature | Frontend | M | 4.2 | ⬚ |
| 4.4 | Integrate useSignalR into useDiagram for real-time health updates | Feature | Frontend | M | 4.3, 3.5 | ⬚ |
| 4.5 | Remove dead WebSocket code and unused components | Refactor | Frontend | S | 4.4 | ⬚ |
| 4.6 | Write tests for SignalR integration | Test | Frontend | M | 4.4 | ⬚ |

#### 4.1 – Add @microsoft/signalr Dependency
- **Files to modify**: `web/package.json`
- **Test plan (TDD)**:
  - Smoke: `npm install` succeeds, `npm run build` succeeds
- **Acceptance criteria**:
  - `@microsoft/signalr` is in dependencies (not devDependencies)
  - No version conflicts

#### 4.2 – Create SignalR Hub Connection Manager
- **Files to create**: `web/src/shared/api/signalrClient.ts`
- **Test plan (TDD)**:
  - Covered by 4.6
- **Acceptance criteria**:
  - Exports `createDiagramHubConnection(baseUrl: string): HubConnection`
  - Uses `HubConnectionBuilder` with automatic reconnect
  - Provides `joinProject(projectId)` and `leaveProject(projectId)` helper functions
  - Typed event handlers for `DiagramUpdated`, `HealthOverlayChanged`, `NodeAdded`, `NodeRemoved`

#### 4.3 – Create useSignalR Hook
- **Files to create**: `web/src/features/diagram/hooks/useSignalR.ts`
- **Test plan (TDD)**:
  - Unit tests: `useSignalR.test.tsx` – `useSignalR_connects_joinsProject`, `useSignalR_unmount_leavesProject`, `useSignalR_healthUpdate_callsCallback`, `useSignalR_diagramUpdate_callsCallback`
- **Acceptance criteria**:
  - Hook manages HubConnection lifecycle (connect on mount, disconnect on unmount)
  - Calls `JoinProject(projectId)` on connection
  - Accepts callbacks for `onHealthOverlayChanged` and `onDiagramUpdated`
  - Handles reconnection gracefully

#### 4.4 – Integrate useSignalR into useDiagram
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.ts`
- **Test plan (TDD)**:
  - Unit tests: `useDiagram.test.tsx` – `useDiagram_healthOverlayChanged_updatesNodeHealth`, `useDiagram_diagramUpdated_refreshesData`
- **Acceptance criteria**:
  - `useDiagram` subscribes to SignalR health updates
  - When `HealthOverlayChanged` fires, node health badges and edge traffic colors update in real-time
  - When `DiagramUpdated` fires, diagram data is refreshed

#### 4.5 – Remove Dead WebSocket Code and Unused Components
- **Files to delete**: `web/src/shared/api/websocket.ts`, `web/src/shared/hooks/useWebSocket.ts`
- **Files to delete**: `web/src/features/diagram/components/GraphEdge.tsx` (dead), `web/src/features/diagram/components/GraphNode.tsx` (dead), `web/src/features/diagram/components/MiniMap.tsx` (dead — React Flow's MiniMap is used instead)
- **Files to modify**: `web/src/features/diagram/hooks/usePanZoom.ts` (evaluate if dead — zoom is handled by React Flow internally)
- **Test plan (TDD)**:
  - Regression: All existing tests must still pass after removal
- **Acceptance criteria**:
  - No dead code remains
  - No import references to deleted files
  - All tests pass

#### 4.6 – Write Tests for SignalR Integration
- **Files to create**: `web/src/features/diagram/hooks/useSignalR.test.tsx`
- **Files to modify**: `web/src/features/diagram/hooks/useDiagram.test.tsx`
- **Test plan (TDD)**:
  - As defined in 4.3 and 4.4
  - Mock `@microsoft/signalr` HubConnection for unit testing
- **Acceptance criteria**:
  - All new tests green
  - All existing tests green

---

### Epic 5: Azure Resource Type Classifier
Goal: Build a resource type catalog in the Discovery domain that classifies Azure ARM resource types into include/exclude, C4 level, friendly name, and service type — eliminating noise from infrastructure helper resources.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Create AzureResourceClassification value object in Discovery.Domain | Feature | Discovery | S | – | ⬚ |
| 5.2 | Create AzureResourceTypeCatalog with classification rules | Feature | Discovery | M | 5.1 | ⬚ |
| 5.3 | Enrich DiscoveredResource with classification and friendly name | Feature | Discovery | S | 5.2 | ⬚ |
| 5.4 | Enrich ResourcesDiscoveredIntegrationEvent with classification data | Feature | Discovery, Shared | S | 5.3 | ⬚ |
| 5.5 | Update ResourcesDiscoveredHandler in Graph to use classification for C4 level | Feature | Graph | M | 5.4 | ⬚ |
| 5.6 | Filter excluded resources before publishing integration event | Feature | Discovery | S | 5.2 | ⬚ |
| 5.7 | Update FakeAzureResourceGraphClient with diverse resource types | Feature | Discovery | S | 5.2 | ⬚ |
| 5.8 | Write tests for resource classifier | Test | Discovery, Graph | M | 5.5, 5.6 | ⬚ |

#### 5.1 – Create AzureResourceClassification Value Object
- **Files to create**: `src/Modules/Discovery/Discovery.Domain/Resources/AzureResourceClassification.cs`
- **Test plan (TDD)**:
  - Covered by 5.8
- **Acceptance criteria**:
  - Record: `(string FriendlyName, string ServiceType, string C4Level, bool IncludeInDiagram)`
  - ServiceType aligns with frontend `ServiceType`: `app`, `api`, `database`, `queue`, `cache`, `external`
  - C4Level: `Context`, `Container`, `Component`

#### 5.2 – Create AzureResourceTypeCatalog
- **Files to create**: `src/Modules/Discovery/Discovery.Domain/Resources/AzureResourceTypeCatalog.cs`
- **Test plan (TDD)**:
  - Unit tests: `AzureResourceTypeCatalogTests` – `Classify_WebSites_ReturnsAppContainer`, `Classify_PostgreSQL_ReturnsDatabaseContainer`, `Classify_FunctionApp_ReturnsAppComponent`, `Classify_NetworkInterface_ExcludedFromDiagram`, `Classify_NSG_ExcludedFromDiagram`, `Classify_ManagedDisk_ExcludedFromDiagram`, `Classify_UnknownType_ReturnsDefaultContainer`, `Classify_KeyVault_ReturnsExternalContainer`, `Classify_ServiceBus_ReturnsQueueContainer`, `Classify_RedisCache_ReturnsCacheContainer`
- **Acceptance criteria**:
  - Static method: `AzureResourceClassification Classify(string armResourceType)`
  - Covers at minimum 20 common Azure resource types
  - Infrastructure noise types (NICs, NSGs, Disks, Public IPs, Managed Identities, Diagnostic Settings) are excluded
  - Workload types (App Services, Functions, SQL, PostgreSQL, Cosmos DB, Redis, Service Bus, Key Vault, AKS, Container Apps, API Management) are classified with correct C4 level, friendly name, and service type
  - Unknown types default to Container with `IncludeInDiagram = true` and generic friendly name

#### 5.3 – Enrich DiscoveredResource with Classification
- **Files to modify**: `src/Modules/Discovery/Discovery.Domain/Resources/DiscoveredResource.cs`
- **Test plan (TDD)**:
  - Unit tests: `DiscoveredResourceTests` – `Create_WithClassification_StoresClassification`
- **Acceptance criteria**:
  - `DiscoveredResource` gains `AzureResourceClassification Classification` property
  - `Create` factory method classifies the resource using `AzureResourceTypeCatalog`

#### 5.4 – Enrich ResourcesDiscoveredIntegrationEvent with Classification
- **Files to modify**: `src/Shared/Kernel/IntegrationEvents/ResourcesDiscoveredIntegrationEvent.cs`
- **Test plan (TDD)**:
  - Covered by 5.8
- **Acceptance criteria**:
  - `DiscoveredResourceEventItem` gains: `string FriendlyName`, `string ServiceType`, `string C4Level`, `bool IncludeInDiagram`
  - Backward compatible — existing consumers receive the new fields

#### 5.5 – Update ResourcesDiscoveredHandler to Use Classification
- **Files to modify**: `src/Modules/Graph/Graph.Application/IntegrationEventHandlers/ResourcesDiscoveredHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `ResourcesDiscoveredHandlerTests` – `Handle_ExcludedResource_NotAddedToGraph`, `Handle_FunctionApp_MappedToComponent`, `Handle_AppService_MappedToContainer`, `Handle_Database_MappedToContainer`
- **Acceptance criteria**:
  - Filters out resources where `IncludeInDiagram == false`
  - Uses `C4Level` from the event instead of the primitive `type.Contains("function")` heuristic
  - Uses `FriendlyName` instead of raw Azure resource name
  - Removes the old `MapLevel` private method

#### 5.6 – Filter Excluded Resources Before Publishing Event
- **Files to modify**: `src/Modules/Discovery/Discovery.Application/DiscoverResources/DiscoverResourcesHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `DiscoverResourcesHandlerTests` – `Handle_MixedResources_OnlyIncludedInEvent`, `Handle_AllExcluded_PublishesEmptyEvent`
- **Acceptance criteria**:
  - Only resources with `IncludeInDiagram == true` are included in `ResourcesDiscoveredIntegrationEvent`
  - All resources (including excluded) are still persisted in `DiscoveredResourceRepository` for audit/completeness
  - Resource count in response reflects total discovered (not just included)

#### 5.7 – Update FakeAzureResourceGraphClient with Diverse Types
- **Files to modify**: `src/Modules/Discovery/Discovery.Api/Adapters/FakeAzureResourceGraphClient.cs`
- **Test plan (TDD)**:
  - Smoke: Existing discovery flow still works with enriched fake data
- **Acceptance criteria**:
  - Returns at least 10 resources spanning: App Service, Function App, PostgreSQL, Redis, Service Bus, Key Vault, NSG, NIC, Managed Disk, Public IP
  - Includes parent-child relationships (NIC → VM, App Service Plan → App Service)

#### 5.8 – Write Tests for Resource Classifier
- **Files to create**: `src/Modules/Discovery/Discovery.Tests/Domain/AzureResourceTypeCatalogTests.cs`, `src/Modules/Discovery/Discovery.Tests/DiscoverResources/DiscoverResourcesFilteringTests.cs`
- **Files to modify**: `src/Modules/Graph/Graph.Tests/Application/ResourcesDiscoveredHandlerTests.cs`
- **Test plan (TDD)**:
  - As defined in 5.2, 5.5, 5.6
- **Acceptance criteria**:
  - All new tests green
  - All existing tests updated and green

---

### Epic 6: Resource Grouping and Parent-Child Hierarchy
Goal: Use `ParentResourceId` from Azure Resource Graph to create node hierarchies in the architecture graph and group related resources into logical composite nodes.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Propagate ParentResourceId through Discovery to integration event | Feature | Discovery, Shared | S | 5.4 | ⬚ |
| 6.2 | Update ArchitectureGraph.AddOrUpdateNode to accept parentId | Feature | Graph | M | 6.1 | ⬚ |
| 6.3 | Update ResourcesDiscoveredHandler to set parent-child relationships | Feature | Graph | M | 6.2 | ⬚ |
| 6.4 | Add ParentId to GraphNodeDto and update GetGraphHandler | Feature | Graph | S | 6.2 | ⬚ |
| 6.5 | Update frontend types and useDiagram to handle hierarchical nodes | Feature | Frontend | M | 6.4 | ⬚ |
| 6.6 | Write tests for parent-child hierarchy | Test | Discovery, Graph, Frontend | M | 6.3, 6.5 | ⬚ |

#### 6.1 – Propagate ParentResourceId to Integration Event
- **Files to modify**: `src/Shared/Kernel/IntegrationEvents/ResourcesDiscoveredIntegrationEvent.cs` (add `ParentResourceId` to `DiscoveredResourceEventItem`)
- **Files to modify**: `src/Modules/Discovery/Discovery.Application/DiscoverResources/DiscoverResourcesHandler.cs` (map `ParentResourceId` from `AzureResourceRecord` to event item)
- **Test plan (TDD)**:
  - Unit tests: `DiscoverResourcesHandlerTests` – `Handle_ResourceWithParent_ParentIdInEvent`
- **Acceptance criteria**:
  - `DiscoveredResourceEventItem` gains `string? ParentResourceId`
  - `DiscoverResourcesHandler` maps `AzureResourceRecord.ParentResourceId` to the event item
  - Backward compatible — null for resources without parents

#### 6.2 – Update ArchitectureGraph.AddOrUpdateNode to Accept ParentId
- **Files to modify**: `src/Modules/Graph/Graph.Domain/ArchitectureGraph/ArchitectureGraph.cs`
- **Test plan (TDD)**:
  - Unit tests: `ArchitectureGraphTests` – `AddOrUpdateNode_WithParentExternalId_SetsParentId`, `AddOrUpdateNode_ParentNotYetAdded_ParentIdNull`, `AddOrUpdateNode_ParentAddedLater_ParentIdResolvedOnUpdate`
- **Acceptance criteria**:
  - `AddOrUpdateNode` gains optional `string? parentExternalResourceId` parameter
  - Resolves parent by matching `ExternalResourceId` in existing nodes
  - Sets `ParentId` on the child node

#### 6.3 – Update ResourcesDiscoveredHandler to Set Parents
- **Files to modify**: `src/Modules/Graph/Graph.Application/IntegrationEventHandlers/ResourcesDiscoveredHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `ResourcesDiscoveredHandlerTests` – `Handle_ResourceWithParent_NodeHasParentId`, `Handle_ResourceWithoutParent_NodeHasNoParent`, `Handle_ParentNotInBatch_NodeHasNoParent`
- **Acceptance criteria**:
  - Handler passes `ParentResourceId` to `AddOrUpdateNode`
  - Parent-child relationships are established during graph construction
  - Two-pass approach: first add all nodes, then resolve parents

#### 6.4 – Add ParentId to GraphNodeDto
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GraphDto.cs`, `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_NodeWithParent_DtoIncludesParentId`
- **Acceptance criteria**:
  - `GraphNodeDto` gains `Guid? ParentNodeId`
  - `GetGraphHandler` maps `ParentId` to DTO

#### 6.5 – Update Frontend for Hierarchical Nodes
- **Files to modify**: `web/src/features/diagram/types.ts` (add `parentId?` to `DiagramNode`), `web/src/features/diagram/hooks/useDiagram.ts` (map `parentNodeId` from API)
- **Test plan (TDD)**:
  - Unit tests: `useDiagram.test.tsx` – `useDiagram_apiDataWithParent_mapsParentId`
- **Acceptance criteria**:
  - `DiagramNode` type gains `parentId?: string`
  - `mapGraphDtoToDiagramData` maps `ParentNodeId` from API
  - Visual grouping of child nodes near parents in the layout (via DAGre parent setting or proximity)

#### 6.6 – Write Tests for Parent-Child Hierarchy
- **Files to create**: `src/Modules/Graph/Graph.Tests/Domain/ArchitectureGraphParentChildTests.cs`
- **Files to modify**: `src/Modules/Graph/Graph.Tests/Application/ResourcesDiscoveredHandlerTests.cs`, `web/src/features/diagram/hooks/useDiagram.test.tsx`
- **Test plan (TDD)**:
  - As defined in 6.1–6.5
- **Acceptance criteria**:
  - All new tests green
  - All existing tests green

---

### Risks

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | ITelemetryQueryService creates tight coupling between Graph and Telemetry modules | Medium | Medium | Use Shared.Kernel contract interface; Telemetry owns implementation. Graph depends only on abstraction. If coupling proves problematic, switch to event-driven projection. |
| R2 | SignalR connection instability in frontend causes stale health data | Medium | Medium | Implement automatic reconnect with exponential backoff in useSignalR hook; show "connection lost" indicator in UI. |
| R3 | AzureResourceTypeCatalog becomes outdated as Azure adds new resource types | High | Low | Default unknown types to Container/included. Catalog is a domain value — easy to extend. Consider loading from configuration in future. |
| R4 | Parent-child resolution fails for resources discovered across separate batches | Medium | Medium | Two-pass approach in ResourcesDiscoveredHandler: add all nodes first, then resolve parents. For cross-batch parents, update on subsequent discovery runs. |
| R5 | GraphEdgeDto traffic calculation (average of source+target health) is a naive heuristic | Low | Low | Acceptable for MVP. Future: derive edge traffic from Application Insights dependency telemetry (actual call counts/latency between services). |
| R6 | Frontend bundle size increase from @microsoft/signalr | Low | Low | Library is ~50KB gzipped. Acceptable. Can lazy-load if needed. |
| R7 | Moving integration events to Shared.Kernel may break existing serialization in persisted outbox | Medium | High | Verify no outbox/queue persistence exists for integration events. Currently all events are in-process MediatR notifications — no serialization risk. If outbox is added later, use fully-qualified type names. |

### Critical Path
1.1 → 2.1 → 2.2 → 2.3 → 2.5 → 3.1 → 3.3 → 3.4 → 3.5 → 3.6 → 4.1 → 4.2 → 4.3 → 4.4 → 4.6 → 5.1 → 5.2 → 5.4 → 5.5 → 5.8 → 6.1 → 6.2 → 6.3 → 6.6

### Parallelization Opportunities
- **Epic 1 tasks 1.1–1.4** can all run in parallel (independent refactors)
- **Epic 4 (tasks 4.1–4.3)** can run in parallel with **Epic 5 (tasks 5.1–5.2)** since they affect different layers (frontend vs. Discovery domain)
- **Epic 3 task 3.5** (frontend mapping) can start as soon as 3.3 (DTO changes) is merged, without waiting for 3.4 (backend enrichment) — use mock data initially
- **Task 5.7** (fake client) can run in parallel with 5.3–5.6

### Estimated Total Effort
- S tasks: 13 x ~30 min = ~6.5 h
- M tasks: 14 x ~2.5 h = ~35 h
- L tasks: 0
- XL tasks: 0
- **Total: ~41.5 hours**
