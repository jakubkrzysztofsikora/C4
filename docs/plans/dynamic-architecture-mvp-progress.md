# Progress: DynamicArchitectureMVP
Scope: MVP
Created: 2026-02-25
Last Updated: 2026-02-25
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: ProjectScaffolding
- [ ] 1.1 – Create .NET solution and module project structure
- [ ] 1.2 – Create shared kernel (Result, Entity, ValueObject, StronglyTypedId, DomainEvent)
- [ ] 1.3 – Create shared infrastructure (IUnitOfWork, BaseDbContext, MediatR pipeline behaviors)
- [ ] 1.4 – Create ASP.NET Core Host with module registration and endpoint discovery
- [ ] 1.5 – Create Docker Compose with PostgreSQL, backend, and frontend
- [ ] 1.6 – Create React + TypeScript frontend skeleton with Vite, routing, and design system foundation
- [ ] 1.7 – Write shared kernel unit tests

### Epic 2: IdentityModule
- [ ] 2.1 – Create Identity domain model (Organization, Project, Member, Role)
- [ ] 2.2 – Create RegisterOrganization slice
- [ ] 2.3 – Create CreateProject slice
- [ ] 2.4 – Create InviteMember and ManageRoles slices
- [ ] 2.5 – Configure OAuth/OIDC authentication in Host
- [ ] 2.6 – Create Identity persistence (EF Core, migrations)
- [ ] 2.7 – Write Identity module tests

### Epic 3: DiscoveryModule
- [ ] 3.1 – Create Discovery domain model (AzureSubscription, DiscoveredResource, ResourceRelationship)
- [ ] 3.2 – Create ConnectAzureSubscription slice
- [ ] 3.3 – Create Azure Resource Graph adapter for resource discovery
- [ ] 3.4 – Create DiscoverResources slice
- [ ] 3.5 – Create IaC parser adapter (Bicep/Terraform)
- [ ] 3.6 – Create DetectDrift slice
- [ ] 3.7 – Create GetDiscoveryStatus query slice
- [ ] 3.8 – Create Discovery persistence and integration events
- [ ] 3.9 – Write Discovery module tests

### Epic 4: GraphModule
- [ ] 4.1 – Create Graph domain model (ArchitectureGraph, GraphNode, GraphEdge, C4Level)
- [ ] 4.2 – Create BuildGraphFromDiscovery integration event handler
- [ ] 4.3 – Create GetGraph query slice
- [ ] 4.4 – Create GetGraphDiff query slice
- [ ] 4.5 – Create Graph versioning and snapshot persistence
- [ ] 4.6 – Create Graph persistence and module registration
- [ ] 4.7 – Write Graph module tests

### Epic 5: TelemetryModule
- [ ] 5.1 – Create Telemetry domain model (MetricDataPoint, ServiceHealth, HealthScore)
- [ ] 5.2 – Create Application Insights adapter
- [ ] 5.3 – Create IngestTelemetry slice
- [ ] 5.4 – Create GetServiceHealth query slice
- [ ] 5.5 – Create TelemetryUpdated integration event for Graph overlay
- [ ] 5.6 – Create Telemetry persistence and module registration
- [ ] 5.7 – Write Telemetry module tests

### Epic 6: VisualizationModule
- [ ] 6.1 – Create Visualization domain model (DiagramView, ViewPreset, ExportFormat)
- [ ] 6.2 – Create GetDiagram query slice
- [ ] 6.3 – Create WebSocket hub for real-time diagram updates
- [ ] 6.4 – Create ExportDiagram slice (SVG/PDF)
- [ ] 6.5 – Create SaveViewPreset and GetViewPresets slices
- [ ] 6.6 – Create Visualization persistence and module registration
- [ ] 6.7 – Write Visualization module tests

### Epic 7: FrontendApplication
- [ ] 7.1 – Create authentication flow (login, token management, protected routes)
- [ ] 7.2 – Create organization and project management pages
- [ ] 7.3 – Create Azure subscription connection wizard
- [ ] 7.4 – Create C4 diagram renderer (canvas-based, interactive)
- [ ] 7.5 – Create real-time update integration (WebSocket → diagram)
- [ ] 7.6 – Create traffic overlay and health indicators
- [ ] 7.7 – Create drill-down navigation (context → container → component)
- [ ] 7.8 – Create filtering, grouping, and search controls
- [ ] 7.9 – Create IaC drift visualization overlay
- [ ] 7.10 – Create diagram export feature (SVG/PDF download)
- [ ] 7.11 – Create timeline/time navigation slider
- [ ] 7.12 – Write frontend tests

### Epic 8: AIIntegration
- [ ] 8.1 – Configure Semantic Kernel in Host (Azure OpenAI, filters, logging)
- [ ] 8.2 – Create ArchitectureAnalysis SK plugin
- [ ] 8.3 – Create BasicThreatDetection SK plugin (STRIDE-based risk scoring)
- [ ] 8.4 – Create AnalyzeArchitecture slice
- [ ] 8.5 – Create GetThreatAssessment slice
- [ ] 8.6 – Write AI integration tests

### Epic 9: EndToEndIntegration
- [ ] 9.1 – Create database migrations for all modules
- [ ] 9.2 – Create seed data and demo mode
- [ ] 9.3 – End-to-end acceptance tests
- [ ] 9.4 – Docker Compose smoke test and health verification
- [ ] 9.5 – API documentation (OpenAPI/Swagger polish)
- [ ] 9.6 – Architecture boundary tests (ArchUnitNET)

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-25 | Initial plan created | – | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-25 | Scope MVP to Azure-only with Bicep/Terraform IaC | User requirements specify Azure as first target; multi-cloud deferred to post-MVP |
| 2026-02-25 | Docker Compose only, no cloud deployment | First client integration runs locally; deployment logic deferred |
| 2026-02-25 | 5 backend modules: Identity, Discovery, Graph, Telemetry, Visualization | Aligned with bounded contexts from requirements; AI integrated into Graph module |
| 2026-02-25 | SignalR for real-time updates | Built into ASP.NET Core; supports WebSocket transport with automatic fallback |
| 2026-02-25 | Canvas/WebGL for diagram rendering | Required for performance at 500+ nodes; standard HTML/SVG won't scale |
| 2026-02-25 | Threat modeling in Graph module rather than separate module | Basic STRIDE-based analysis is tightly coupled to graph; separate module warranted post-MVP when complexity grows |
| 2026-02-25 | Demo mode with seed data | Allows first-time users and demos without Azure subscription; critical for sales |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
