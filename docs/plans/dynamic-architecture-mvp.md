## Plan: DynamicArchitectureMVP
Scope: MVP
Created: 2026-02-25
Status: Draft

### Overview
Build a complete dynamic architecture visualization SaaS from greenfield to first-client-ready, running in Docker Compose. The system automatically discovers Azure resources, builds a C4-model architecture graph, overlays real-time telemetry from Application Insights with red/green health indicators, and renders interactive diagrams in a React frontend. Scoped to Azure environments with Bicep/Terraform IaC integration.

### Success Criteria
- [ ] Solution compiles with zero warnings; all tests pass
- [ ] Docker Compose brings up backend, frontend, and PostgreSQL with a single `docker compose up`
- [ ] A user can authenticate via OAuth/OIDC, connect an Azure subscription, and see auto-discovered resources as a C4 context-level diagram within 5 minutes
- [ ] Diagrams update in real-time via WebSocket when resources change or telemetry arrives
- [ ] Traffic overlays color-code connections green/yellow/red based on Application Insights metrics
- [ ] Users can drill down from context → container → component levels
- [ ] IaC drift detection compares Bicep/Terraform desired state against live Azure state
- [ ] Export diagrams as SVG/PDF
- [ ] Multi-tenant: organizations, projects, role-based access control
- [ ] All modules have unit tests for handlers and domain logic, plus acceptance tests for endpoints

---

### Epic 1: ProjectScaffolding
Goal: Set up the .NET solution, project files, shared kernel, Docker Compose, and React frontend skeleton so all subsequent work has a compilable foundation.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Create .NET solution and module project structure | Infrastructure | All | M | – | ⬚ |
| 1.2 | Create shared kernel (Result, Entity, ValueObject, StronglyTypedId, DomainEvent) | Feature | Shared | M | 1.1 | ⬚ |
| 1.3 | Create shared infrastructure (IUnitOfWork, BaseDbContext, MediatR pipeline behaviors) | Feature | Shared | M | 1.2 | ⬚ |
| 1.4 | Create ASP.NET Core Host with module registration and endpoint discovery | Feature | Host | M | 1.3 | ⬚ |
| 1.5 | Create Docker Compose with PostgreSQL, backend, and frontend | Infrastructure | All | M | 1.4 | ⬚ |
| 1.6 | Create React + TypeScript frontend skeleton with Vite, routing, and design system foundation | Feature | Web | L | 1.1 | ⬚ |
| 1.7 | Write shared kernel unit tests | Test | Shared | S | 1.2 | ⬚ |

#### 1.1 – Create .NET Solution and Module Project Structure
- **Files to create**:
  - `C4.sln`
  - `src/Shared/Kernel/Kernel.csproj`
  - `src/Shared/Infrastructure/Infrastructure.csproj`
  - `src/Modules/Identity/Identity.Api/Identity.Api.csproj`
  - `src/Modules/Identity/Identity.Application/Identity.Application.csproj`
  - `src/Modules/Identity/Identity.Domain/Identity.Domain.csproj`
  - `src/Modules/Identity/Identity.Infrastructure/Identity.Infrastructure.csproj`
  - `src/Modules/Identity/Identity.Tests/Identity.Tests.csproj`
  - `src/Modules/Discovery/Discovery.Api/Discovery.Api.csproj`
  - `src/Modules/Discovery/Discovery.Application/Discovery.Application.csproj`
  - `src/Modules/Discovery/Discovery.Domain/Discovery.Domain.csproj`
  - `src/Modules/Discovery/Discovery.Infrastructure/Discovery.Infrastructure.csproj`
  - `src/Modules/Discovery/Discovery.Tests/Discovery.Tests.csproj`
  - `src/Modules/Graph/Graph.Api/Graph.Api.csproj`
  - `src/Modules/Graph/Graph.Application/Graph.Application.csproj`
  - `src/Modules/Graph/Graph.Domain/Graph.Domain.csproj`
  - `src/Modules/Graph/Graph.Infrastructure/Graph.Infrastructure.csproj`
  - `src/Modules/Graph/Graph.Tests/Graph.Tests.csproj`
  - `src/Modules/Telemetry/Telemetry.Api/Telemetry.Api.csproj`
  - `src/Modules/Telemetry/Telemetry.Application/Telemetry.Application.csproj`
  - `src/Modules/Telemetry/Telemetry.Domain/Telemetry.Domain.csproj`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Telemetry.Infrastructure.csproj`
  - `src/Modules/Telemetry/Telemetry.Tests/Telemetry.Tests.csproj`
  - `src/Modules/Visualization/Visualization.Api/Visualization.Api.csproj`
  - `src/Modules/Visualization/Visualization.Application/Visualization.Application.csproj`
  - `src/Modules/Visualization/Visualization.Domain/Visualization.Domain.csproj`
  - `src/Modules/Visualization/Visualization.Infrastructure/Visualization.Infrastructure.csproj`
  - `src/Modules/Visualization/Visualization.Tests/Visualization.Tests.csproj`
  - `src/Host/Host.csproj`
  - `Directory.Build.props` (shared build properties, nullable, implicit usings, .NET 9)
  - `Directory.Packages.props` (central package management)
  - `.editorconfig`
- **Acceptance criteria**:
  - `dotnet build` compiles the entire solution without errors or warnings
  - Project references follow the dependency rule: Domain ← Application ← Infrastructure/Api
  - Test projects reference only their own module

#### 1.2 – Create Shared Kernel
- **Files to create**:
  - `src/Shared/Kernel/Result.cs` (Result<T>, Error)
  - `src/Shared/Kernel/Entity.cs` (Entity<TId> base)
  - `src/Shared/Kernel/AggregateRoot.cs`
  - `src/Shared/Kernel/ValueObject.cs`
  - `src/Shared/Kernel/StronglyTypedId.cs`
  - `src/Shared/Kernel/IDomainEvent.cs`
  - `src/Shared/Kernel/IIntegrationEvent.cs`
  - `src/Shared/Kernel/IAuditableEntity.cs`
  - `src/Shared/Kernel/IUnitOfWork.cs`
  - `src/Shared/Kernel/AssemblyReference.cs`
- **Test plan (TDD)**:
  - Unit tests: `ResultTests` – `Success_WithValue_ReturnsSuccessResult`, `Failure_WithError_ReturnsFailureResult`, `Map_OnSuccess_TransformsValue`, `Map_OnFailure_PreservesError`
  - Unit tests: `StronglyTypedIdTests` – `New_GeneratesUniqueId`, `Equality_SameValue_ReturnsTrue`
  - Fakes/Fixtures needed: none (pure value types)
- **Acceptance criteria**:
  - Result<T> supports Success/Failure with error propagation
  - StronglyTypedId provides type-safe domain identifiers
  - All types are records or abstract classes; sealed where appropriate

#### 1.3 – Create Shared Infrastructure
- **Files to create**:
  - `src/Shared/Infrastructure/Persistence/BaseDbContext.cs`
  - `src/Shared/Infrastructure/Persistence/UnitOfWork.cs`
  - `src/Shared/Infrastructure/Behaviors/ValidationBehavior.cs`
  - `src/Shared/Infrastructure/Behaviors/LoggingBehavior.cs`
  - `src/Shared/Infrastructure/Endpoints/IEndpoint.cs`
  - `src/Shared/Infrastructure/Endpoints/EndpointExtensions.cs`
  - `src/Shared/Infrastructure/Clock/IClock.cs`
  - `src/Shared/Infrastructure/Clock/SystemClock.cs`
  - `src/Shared/Infrastructure/AssemblyReference.cs`
- **Acceptance criteria**:
  - ValidationBehavior intercepts MediatR pipeline and validates commands via FluentValidation
  - IEndpoint interface enables auto-discovery of minimal API endpoints
  - IClock abstraction supports deterministic time in tests

#### 1.4 – Create ASP.NET Core Host
- **Files to create**:
  - `src/Host/Program.cs`
  - `src/Host/appsettings.json`
  - `src/Host/appsettings.Development.json`
  - `src/Host/Dockerfile`
- **Acceptance criteria**:
  - Host registers all modules via AddXxxModule() extension methods
  - Auto-discovers and maps all IEndpoint implementations
  - Configures CORS for frontend, Swagger/OpenAPI, health checks
  - `dotnet run` starts the host and Swagger UI is accessible

#### 1.5 – Create Docker Compose
- **Files to create**:
  - `docker-compose.yml` (PostgreSQL, backend, frontend)
  - `docker-compose.override.yml` (development overrides with volumes, ports)
  - `.env.example` (environment variable template)
  - `web/Dockerfile`
- **Acceptance criteria**:
  - `docker compose up` starts all services
  - PostgreSQL is accessible on port 5432 with default dev credentials
  - Backend is accessible on port 5000 with Swagger
  - Frontend is accessible on port 3000 with Vite dev server
  - Health check endpoints respond with 200

#### 1.6 – Create React Frontend Skeleton
- **Files to create**:
  - `web/package.json`
  - `web/tsconfig.json`
  - `web/vite.config.ts`
  - `web/index.html`
  - `web/src/main.tsx`
  - `web/src/App.tsx`
  - `web/src/shared/api/client.ts` (typed API client base)
  - `web/src/shared/api/websocket.ts` (WebSocket connection manager)
  - `web/src/shared/hooks/useApi.ts`
  - `web/src/shared/hooks/useWebSocket.ts`
  - `web/src/shared/components/Layout.tsx`
  - `web/src/shared/components/CommandPalette.tsx`
  - `web/src/shared/theme/tokens.ts` (design tokens: colors, spacing, typography)
  - `web/src/shared/theme/ThemeProvider.tsx` (dark/light mode)
  - `web/src/features/dashboard/DashboardPage.tsx`
  - `web/vitest.config.ts`
  - `web/.eslintrc.cjs`
- **Acceptance criteria**:
  - `npm run dev` starts Vite dev server
  - `npm run build` produces production bundle without errors
  - Strict TypeScript (`"strict": true`), no `any`
  - Dark/light theme toggle works
  - Routing scaffold in place

#### 1.7 – Write Shared Kernel Unit Tests
- **Files to create**:
  - `src/Shared/Kernel.Tests/Kernel.Tests.csproj`
  - `src/Shared/Kernel.Tests/ResultTests.cs`
  - `src/Shared/Kernel.Tests/StronglyTypedIdTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `ResultTests` – `Success_WithValue_ReturnsSuccessResult`, `Failure_WithError_ReturnsFailureResult`, `Bind_OnSuccess_CallsNext`, `Bind_OnFailure_ShortCircuits`
  - Unit tests: `StronglyTypedIdTests` – `New_GeneratesUniqueId`, `Equality_SameValue_ReturnsTrue`, `Equality_DifferentValue_ReturnsFalse`
- **Acceptance criteria**:
  - All kernel types have at least 2 tests
  - `dotnet test` passes

---

### Epic 2: IdentityModule
Goal: Implement multi-tenant authentication, organization management, project scoping, and role-based access control.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Create Identity domain model (Organization, Project, Member, Role) | Feature | Identity | M | 1.2 | ⬚ |
| 2.2 | Create RegisterOrganization slice | Feature | Identity | M | 2.1 | ⬚ |
| 2.3 | Create CreateProject slice | Feature | Identity | M | 2.1 | ⬚ |
| 2.4 | Create InviteMember and ManageRoles slices | Feature | Identity | M | 2.1 | ⬚ |
| 2.5 | Configure OAuth/OIDC authentication in Host | Feature | Identity | M | 2.2 | ⬚ |
| 2.6 | Create Identity persistence (EF Core, migrations) | Infrastructure | Identity | M | 2.2 | ⬚ |
| 2.7 | Write Identity module tests | Test | Identity | L | 2.2, 2.3, 2.4, 2.5 | ⬚ |

#### 2.1 – Create Identity Domain Model
- **Files to create**:
  - `src/Modules/Identity/Identity.Domain/Organization/Organization.cs`
  - `src/Modules/Identity/Identity.Domain/Organization/OrganizationId.cs`
  - `src/Modules/Identity/Identity.Domain/Project/Project.cs`
  - `src/Modules/Identity/Identity.Domain/Project/ProjectId.cs`
  - `src/Modules/Identity/Identity.Domain/Member/Member.cs`
  - `src/Modules/Identity/Identity.Domain/Member/MemberId.cs`
  - `src/Modules/Identity/Identity.Domain/Member/Role.cs` (enum: Owner, Admin, Contributor, Viewer)
  - `src/Modules/Identity/Identity.Domain/Events/OrganizationCreatedEvent.cs`
  - `src/Modules/Identity/Identity.Domain/Events/ProjectCreatedEvent.cs`
  - `src/Modules/Identity/Identity.Domain/Events/MemberInvitedEvent.cs`
  - `src/Modules/Identity/Identity.Domain/Errors/IdentityErrors.cs`
- **Acceptance criteria**:
  - Organization is an aggregate root with factory methods
  - Project belongs to Organization; enforces unique name within org
  - Member tracks external identity provider ID and role within project
  - All IDs are strongly typed

#### 2.2 – Create RegisterOrganization Slice
- **Files to create**:
  - `src/Modules/Identity/Identity.Application/RegisterOrganization/RegisterOrganizationCommand.cs`
  - `src/Modules/Identity/Identity.Application/RegisterOrganization/RegisterOrganizationHandler.cs`
  - `src/Modules/Identity/Identity.Application/RegisterOrganization/RegisterOrganizationValidator.cs`
  - `src/Modules/Identity/Identity.Application/RegisterOrganization/RegisterOrganizationResponse.cs`
  - `src/Modules/Identity/Identity.Application/Ports/IOrganizationRepository.cs`
  - `src/Modules/Identity/Identity.Api/Endpoints/RegisterOrganizationEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `RegisterOrganizationHandlerTests` – `Handle_ValidCommand_CreatesOrganization`, `Handle_DuplicateName_ReturnsError`
  - Module tests: `RegisterOrganizationEndpointTests` – `Post_ValidRequest_Returns201`, `Post_InvalidRequest_Returns400`
  - Fakes/Fixtures needed: `FakeOrganizationRepository`, `FakeUnitOfWork`
- **Acceptance criteria**:
  - POST /api/organizations creates an organization and returns 201
  - Duplicate organization names return a domain error

#### 2.3 – Create CreateProject Slice
- **Files to create**:
  - `src/Modules/Identity/Identity.Application/CreateProject/CreateProjectCommand.cs`
  - `src/Modules/Identity/Identity.Application/CreateProject/CreateProjectHandler.cs`
  - `src/Modules/Identity/Identity.Application/CreateProject/CreateProjectValidator.cs`
  - `src/Modules/Identity/Identity.Application/CreateProject/CreateProjectResponse.cs`
  - `src/Modules/Identity/Identity.Application/Ports/IProjectRepository.cs`
  - `src/Modules/Identity/Identity.Api/Endpoints/CreateProjectEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `CreateProjectHandlerTests` – `Handle_ValidCommand_CreatesProject`, `Handle_OrganizationNotFound_ReturnsError`, `Handle_DuplicateProjectName_ReturnsError`
  - Module tests: `CreateProjectEndpointTests` – `Post_ValidRequest_Returns201`
  - Fakes/Fixtures needed: `FakeProjectRepository`
- **Acceptance criteria**:
  - POST /api/organizations/{orgId}/projects creates a project scoped to the organization
  - Validates organization exists

#### 2.4 – Create InviteMember and ManageRoles Slices
- **Files to create**:
  - `src/Modules/Identity/Identity.Application/InviteMember/InviteMemberCommand.cs`
  - `src/Modules/Identity/Identity.Application/InviteMember/InviteMemberHandler.cs`
  - `src/Modules/Identity/Identity.Application/InviteMember/InviteMemberValidator.cs`
  - `src/Modules/Identity/Identity.Application/UpdateMemberRole/UpdateMemberRoleCommand.cs`
  - `src/Modules/Identity/Identity.Application/UpdateMemberRole/UpdateMemberRoleHandler.cs`
  - `src/Modules/Identity/Identity.Application/Ports/IMemberRepository.cs`
  - `src/Modules/Identity/Identity.Api/Endpoints/InviteMemberEndpoint.cs`
  - `src/Modules/Identity/Identity.Api/Endpoints/UpdateMemberRoleEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `InviteMemberHandlerTests` – `Handle_ValidCommand_AddsMember`, `Handle_AlreadyMember_ReturnsError`
  - Unit tests: `UpdateMemberRoleHandlerTests` – `Handle_ValidCommand_UpdatesRole`, `Handle_CannotDemoteLastOwner_ReturnsError`
  - Fakes/Fixtures needed: `FakeMemberRepository`
- **Acceptance criteria**:
  - POST /api/projects/{projectId}/members invites a member
  - PUT /api/projects/{projectId}/members/{memberId}/role updates role
  - Cannot demote the last owner of a project

#### 2.5 – Configure OAuth/OIDC Authentication
- **Files to modify**: `src/Host/Program.cs`
- **Files to create**:
  - `src/Modules/Identity/Identity.Infrastructure/Auth/CurrentUserAccessor.cs`
  - `src/Modules/Identity/Identity.Application/Ports/ICurrentUser.cs`
- **Acceptance criteria**:
  - Host configures JWT Bearer authentication
  - ICurrentUser provides tenant context (organization, project, role)
  - Unauthorized requests return 401; forbidden requests return 403

#### 2.6 – Create Identity Persistence
- **Files to create**:
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/IdentityDbContext.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Configurations/OrganizationConfiguration.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Configurations/ProjectConfiguration.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Configurations/MemberConfiguration.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Repositories/OrganizationRepository.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Repositories/ProjectRepository.cs`
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Repositories/MemberRepository.cs`
  - `src/Modules/Identity/Identity.Infrastructure/IdentityInfrastructureModule.cs`
- **Acceptance criteria**:
  - EF Core configurations map all domain entities correctly
  - Repositories implement ports from Application layer
  - Module registration wires up DbContext and all repositories

#### 2.7 – Write Identity Module Tests
- **Files to create**:
  - `src/Modules/Identity/Identity.Tests/RegisterOrganization/RegisterOrganizationHandlerTests.cs`
  - `src/Modules/Identity/Identity.Tests/RegisterOrganization/RegisterOrganizationEndpointTests.cs`
  - `src/Modules/Identity/Identity.Tests/CreateProject/CreateProjectHandlerTests.cs`
  - `src/Modules/Identity/Identity.Tests/InviteMember/InviteMemberHandlerTests.cs`
  - `src/Modules/Identity/Identity.Tests/UpdateMemberRole/UpdateMemberRoleHandlerTests.cs`
  - `src/Modules/Identity/Identity.Tests/Domain/OrganizationTests.cs`
  - `src/Modules/Identity/Identity.Tests/Domain/ProjectTests.cs`
  - `src/Modules/Identity/Identity.Tests/Fakes/FakeOrganizationRepository.cs`
  - `src/Modules/Identity/Identity.Tests/Fakes/FakeProjectRepository.cs`
  - `src/Modules/Identity/Identity.Tests/Fakes/FakeMemberRepository.cs`
  - `src/Modules/Identity/Identity.Tests/Fakes/FakeUnitOfWork.cs`
- **Test plan (TDD)**:
  - Domain tests: `OrganizationTests` – `Create_ValidInput_ReturnsOrganization`, `AddProject_DuplicateName_ReturnsError`
  - Domain tests: `ProjectTests` – `AddMember_ValidInput_ReturnsMember`, `RemoveLastOwner_ReturnsError`
  - Handler unit tests: All handlers tested with fakes
  - Acceptance tests: Endpoint tests via WebApplicationFactory
- **Acceptance criteria**:
  - 100% handler and domain logic coverage with unit tests
  - Acceptance tests verify HTTP contract (status codes, response shapes)

---

### Epic 3: DiscoveryModule
Goal: Automatically discover Azure resources, parse IaC (Bicep/Terraform), detect drift, and publish discovered topology to the Graph module.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Create Discovery domain model (AzureSubscription, DiscoveredResource, ResourceRelationship) | Feature | Discovery | M | 1.2 | ⬚ |
| 3.2 | Create ConnectAzureSubscription slice | Feature | Discovery | M | 3.1, 2.3 | ⬚ |
| 3.3 | Create Azure Resource Graph adapter for resource discovery | Feature | Discovery | L | 3.2 | ⬚ |
| 3.4 | Create DiscoverResources slice (triggers discovery and publishes results) | Feature | Discovery | L | 3.3 | ⬚ |
| 3.5 | Create IaC parser adapter (Bicep/Terraform) | Feature | Discovery | L | 3.1 | ⬚ |
| 3.6 | Create DetectDrift slice (compare IaC desired state vs live) | Feature | Discovery | M | 3.4, 3.5 | ⬚ |
| 3.7 | Create GetDiscoveryStatus query slice | Feature | Discovery | S | 3.4 | ⬚ |
| 3.8 | Create Discovery persistence and integration events | Infrastructure | Discovery | M | 3.4 | ⬚ |
| 3.9 | Write Discovery module tests | Test | Discovery | L | 3.2, 3.4, 3.5, 3.6 | ⬚ |

#### 3.1 – Create Discovery Domain Model
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Domain/Subscription/AzureSubscription.cs`
  - `src/Modules/Discovery/Discovery.Domain/Subscription/AzureSubscriptionId.cs`
  - `src/Modules/Discovery/Discovery.Domain/Resource/DiscoveredResource.cs`
  - `src/Modules/Discovery/Discovery.Domain/Resource/DiscoveredResourceId.cs`
  - `src/Modules/Discovery/Discovery.Domain/Resource/ResourceType.cs` (value object)
  - `src/Modules/Discovery/Discovery.Domain/Resource/ResourceRelationship.cs`
  - `src/Modules/Discovery/Discovery.Domain/Resource/DriftStatus.cs` (enum: InSync, Drifted, OrphanLive, OrphanIaC)
  - `src/Modules/Discovery/Discovery.Domain/Events/ResourcesDiscoveredEvent.cs`
  - `src/Modules/Discovery/Discovery.Domain/Events/DriftDetectedEvent.cs`
  - `src/Modules/Discovery/Discovery.Domain/Errors/DiscoveryErrors.cs`
- **Acceptance criteria**:
  - AzureSubscription stores connection info (subscription ID, tenant ID, credential reference)
  - DiscoveredResource captures Azure resource ID, type, name, tags, region, properties
  - ResourceRelationship models edges (e.g., App Service → SQL Database)
  - DriftStatus tracks IaC sync state per resource

#### 3.2 – Create ConnectAzureSubscription Slice
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/ConnectSubscription/ConnectSubscriptionCommand.cs`
  - `src/Modules/Discovery/Discovery.Application/ConnectSubscription/ConnectSubscriptionHandler.cs`
  - `src/Modules/Discovery/Discovery.Application/ConnectSubscription/ConnectSubscriptionValidator.cs`
  - `src/Modules/Discovery/Discovery.Application/ConnectSubscription/ConnectSubscriptionResponse.cs`
  - `src/Modules/Discovery/Discovery.Application/Ports/IAzureSubscriptionRepository.cs`
  - `src/Modules/Discovery/Discovery.Application/Ports/IAzureCredentialStore.cs`
  - `src/Modules/Discovery/Discovery.Api/Endpoints/ConnectSubscriptionEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `ConnectSubscriptionHandlerTests` – `Handle_ValidSubscription_Connects`, `Handle_InvalidCredentials_ReturnsError`
  - Module tests: `ConnectSubscriptionEndpointTests` – `Post_ValidRequest_Returns201`
  - Fakes/Fixtures needed: `FakeAzureSubscriptionRepository`, `FakeAzureCredentialStore`
- **Acceptance criteria**:
  - POST /api/projects/{projectId}/subscriptions connects an Azure subscription
  - Validates credential access by testing connectivity
  - Stores credential reference securely (not plaintext)

#### 3.3 – Create Azure Resource Graph Adapter
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/Ports/IAzureResourceDiscoverer.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Azure/AzureResourceGraphDiscoverer.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Azure/AzureResourceMapper.cs` (maps Azure SDK types to domain)
- **Acceptance criteria**:
  - Uses Azure Resource Graph SDK to query all resources in a subscription
  - Maps Azure resource types to C4-compatible categories (compute, storage, network, etc.)
  - Discovers relationships (VNET peering, App Service → SQL, etc.) via dependency analysis
  - Handles API rate limits with retry/backoff
  - Port interface enables testing with fakes

#### 3.4 – Create DiscoverResources Slice
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/DiscoverResources/DiscoverResourcesCommand.cs`
  - `src/Modules/Discovery/Discovery.Application/DiscoverResources/DiscoverResourcesHandler.cs`
  - `src/Modules/Discovery/Discovery.Application/DiscoverResources/DiscoverResourcesResponse.cs`
  - `src/Modules/Discovery/Discovery.Application/Ports/IDiscoveredResourceRepository.cs`
  - `src/Modules/Discovery/Discovery.Api/Endpoints/DiscoverResourcesEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `DiscoverResourcesHandlerTests` – `Handle_ValidSubscription_DiscoversAndPersists`, `Handle_SubscriptionNotFound_ReturnsError`, `Handle_EmptyResources_ReturnsEmptyResult`
  - Fakes/Fixtures needed: `FakeAzureResourceDiscoverer`, `FakeDiscoveredResourceRepository`
- **Acceptance criteria**:
  - POST /api/subscriptions/{subId}/discover triggers full discovery
  - Stores discovered resources and relationships
  - Publishes ResourcesDiscoveredEvent as integration event for Graph module

#### 3.5 – Create IaC Parser Adapter
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/Ports/IIaCParser.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/IaC/BicepParser.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/IaC/TerraformParser.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/IaC/IaCResourceMapper.cs`
- **Acceptance criteria**:
  - Parses Bicep .bicep files to extract desired resource definitions
  - Parses Terraform .tf files (HCL) to extract desired resource definitions
  - Maps IaC resources to the same domain model as live-discovered resources
  - Returns structured list of desired resources with properties

#### 3.6 – Create DetectDrift Slice
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/DetectDrift/DetectDriftCommand.cs`
  - `src/Modules/Discovery/Discovery.Application/DetectDrift/DetectDriftHandler.cs`
  - `src/Modules/Discovery/Discovery.Application/DetectDrift/DetectDriftResponse.cs`
  - `src/Modules/Discovery/Discovery.Api/Endpoints/DetectDriftEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `DetectDriftHandlerTests` – `Handle_AllInSync_ReturnsNoChanges`, `Handle_ResourceDrifted_ReturnsDrift`, `Handle_OrphanLive_FlagsOrphan`, `Handle_OrphanIaC_FlagsMissing`
  - Fakes/Fixtures needed: `FakeIaCParser` with sample templates
- **Acceptance criteria**:
  - POST /api/subscriptions/{subId}/drift compares live vs IaC
  - Returns list of drifted, orphaned, and new resources
  - Publishes DriftDetectedEvent

#### 3.7 – Create GetDiscoveryStatus Query
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Application/GetDiscoveryStatus/GetDiscoveryStatusQuery.cs`
  - `src/Modules/Discovery/Discovery.Application/GetDiscoveryStatus/GetDiscoveryStatusHandler.cs`
  - `src/Modules/Discovery/Discovery.Application/GetDiscoveryStatus/DiscoveryStatusResponse.cs`
  - `src/Modules/Discovery/Discovery.Api/Endpoints/GetDiscoveryStatusEndpoint.cs`
- **Acceptance criteria**:
  - GET /api/subscriptions/{subId}/discovery-status returns latest discovery run info
  - Includes count of resources, relationships, last run time, drift summary

#### 3.8 – Create Discovery Persistence and Integration Events
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/DiscoveryDbContext.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Configurations/*.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Repositories/*.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/DiscoveryInfrastructureModule.cs`
  - `src/Modules/Discovery/Discovery.Application/IntegrationEvents/ResourcesDiscoveredIntegrationEvent.cs`
  - `src/Modules/Discovery/Discovery.Application/IntegrationEvents/DriftDetectedIntegrationEvent.cs`
- **Acceptance criteria**:
  - All repositories persist to PostgreSQL via EF Core
  - Integration events are published via MediatR for in-process consumption
  - Module registration wires everything up

#### 3.9 – Write Discovery Module Tests
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Tests/ConnectSubscription/ConnectSubscriptionHandlerTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/DiscoverResources/DiscoverResourcesHandlerTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/DetectDrift/DetectDriftHandlerTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Domain/AzureSubscriptionTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Domain/DiscoveredResourceTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Fakes/*.cs`
  - `src/Modules/Discovery/Discovery.Tests/Infrastructure/BicepParserTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Infrastructure/TerraformParserTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/TestData/sample.bicep`
  - `src/Modules/Discovery/Discovery.Tests/TestData/sample.tf`
- **Test plan (TDD)**:
  - Domain tests: `AzureSubscriptionTests` – `Create_ValidInput_ReturnsSubscription`
  - Domain tests: `DiscoveredResourceTests` – `AddRelationship_Valid_AddsEdge`, `SetDriftStatus_UpdatesStatus`
  - Handler unit tests: All handlers tested with fakes
  - Integration tests: BicepParser and TerraformParser tested with sample IaC files
- **Acceptance criteria**:
  - All handlers tested with fakes
  - IaC parsers tested with real sample files
  - Domain logic fully covered

---

### Epic 4: GraphModule
Goal: Build the architecture graph engine that stores C4-model nodes/edges, supports versioning, hierarchy traversal, and incremental updates.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Create Graph domain model (ArchitectureGraph, GraphNode, GraphEdge, C4Level) | Feature | Graph | L | 1.2 | ⬚ |
| 4.2 | Create BuildGraphFromDiscovery integration event handler | Feature | Graph | M | 4.1, 3.8 | ⬚ |
| 4.3 | Create GetGraph query slice (with C4 level filtering) | Feature | Graph | M | 4.1 | ⬚ |
| 4.4 | Create GetGraphDiff query slice (compare two snapshots) | Feature | Graph | M | 4.1 | ⬚ |
| 4.5 | Create Graph versioning and snapshot persistence | Feature | Graph | L | 4.1 | ⬚ |
| 4.6 | Create Graph persistence and module registration | Infrastructure | Graph | M | 4.5 | ⬚ |
| 4.7 | Write Graph module tests | Test | Graph | L | 4.2, 4.3, 4.4, 4.5 | ⬚ |

#### 4.1 – Create Graph Domain Model
- **Files to create**:
  - `src/Modules/Graph/Graph.Domain/ArchitectureGraph/ArchitectureGraph.cs`
  - `src/Modules/Graph/Graph.Domain/ArchitectureGraph/ArchitectureGraphId.cs`
  - `src/Modules/Graph/Graph.Domain/GraphNode/GraphNode.cs`
  - `src/Modules/Graph/Graph.Domain/GraphNode/GraphNodeId.cs`
  - `src/Modules/Graph/Graph.Domain/GraphNode/NodeProperties.cs` (value object: technology, owner, tags, cost)
  - `src/Modules/Graph/Graph.Domain/GraphEdge/GraphEdge.cs`
  - `src/Modules/Graph/Graph.Domain/GraphEdge/GraphEdgeId.cs`
  - `src/Modules/Graph/Graph.Domain/GraphEdge/EdgeProperties.cs` (value object: protocol, port, direction)
  - `src/Modules/Graph/Graph.Domain/C4Level.cs` (enum: Context, Container, Component, Code)
  - `src/Modules/Graph/Graph.Domain/GraphSnapshot/GraphSnapshot.cs`
  - `src/Modules/Graph/Graph.Domain/GraphSnapshot/GraphSnapshotId.cs`
  - `src/Modules/Graph/Graph.Domain/Events/GraphUpdatedEvent.cs`
  - `src/Modules/Graph/Graph.Domain/Errors/GraphErrors.cs`
- **Acceptance criteria**:
  - ArchitectureGraph is the aggregate root containing nodes and edges
  - Nodes have a C4Level and can reference a parent node (hierarchy)
  - Edges connect two nodes with directional metadata
  - GraphSnapshot captures a point-in-time copy for diffing
  - Supports incremental updates (add/remove/update nodes and edges)

#### 4.2 – Create BuildGraphFromDiscovery Integration Event Handler
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/IntegrationEventHandlers/ResourcesDiscoveredHandler.cs`
  - `src/Modules/Graph/Graph.Application/Ports/IArchitectureGraphRepository.cs`
- **Test plan (TDD)**:
  - Unit tests: `ResourcesDiscoveredHandlerTests` – `Handle_NewResources_CreatesNodes`, `Handle_NewRelationships_CreatesEdges`, `Handle_RemovedResources_RemovesNodes`, `Handle_UpdatedResources_UpdatesProperties`
  - Fakes/Fixtures needed: `FakeArchitectureGraphRepository`
- **Acceptance criteria**:
  - Consumes ResourcesDiscoveredIntegrationEvent from Discovery module
  - Creates/updates/removes graph nodes and edges incrementally
  - Maps Azure resource types to appropriate C4 levels
  - Creates a new snapshot after each update

#### 4.3 – Create GetGraph Query Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/GetGraph/GetGraphQuery.cs`
  - `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`
  - `src/Modules/Graph/Graph.Application/GetGraph/GraphDto.cs` (includes nodes, edges, metadata)
  - `src/Modules/Graph/Graph.Api/Endpoints/GetGraphEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_ExistingGraph_ReturnsNodes`, `Handle_WithLevelFilter_ReturnsFilteredNodes`, `Handle_GraphNotFound_ReturnsError`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/graph returns the architecture graph
  - Supports query params: `level` (context/container/component), `parentNodeId` (drill-down)
  - Returns JSON with nodes, edges, and metadata

#### 4.4 – Create GetGraphDiff Query Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/GetGraphDiff/GetGraphDiffQuery.cs`
  - `src/Modules/Graph/Graph.Application/GetGraphDiff/GetGraphDiffHandler.cs`
  - `src/Modules/Graph/Graph.Application/GetGraphDiff/GraphDiffDto.cs`
  - `src/Modules/Graph/Graph.Api/Endpoints/GetGraphDiffEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphDiffHandlerTests` – `Handle_TwoSnapshots_ReturnsAddedRemovedModified`, `Handle_SnapshotNotFound_ReturnsError`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/graph/diff?from={snapshotId}&to={snapshotId} compares two graph snapshots
  - Returns added, removed, and modified nodes/edges

#### 4.5 – Create Graph Versioning and Snapshot Persistence
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/Ports/IGraphSnapshotRepository.cs`
  - `src/Modules/Graph/Graph.Application/CreateSnapshot/CreateSnapshotCommand.cs`
  - `src/Modules/Graph/Graph.Application/CreateSnapshot/CreateSnapshotHandler.cs`
- **Acceptance criteria**:
  - Snapshots are created after each graph update
  - Snapshots store full node/edge state at a point in time
  - Supports listing snapshots by project with timestamps

#### 4.6 – Create Graph Persistence and Module Registration
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/GraphDbContext.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Configurations/*.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Repositories/*.cs`
  - `src/Modules/Graph/Graph.Infrastructure/GraphInfrastructureModule.cs`
- **Acceptance criteria**:
  - PostgreSQL stores graph nodes, edges, and snapshots efficiently
  - Uses JSONB columns for flexible node/edge properties
  - Module registration wires up all services

#### 4.7 – Write Graph Module Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/Domain/ArchitectureGraphTests.cs`
  - `src/Modules/Graph/Graph.Tests/BuildGraphFromDiscovery/ResourcesDiscoveredHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/GetGraph/GetGraphHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/GetGraphDiff/GetGraphDiffHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/*.cs`
- **Test plan (TDD)**:
  - Domain tests: `ArchitectureGraphTests` – `AddNode_Valid_AddsToGraph`, `AddEdge_BothNodesExist_AddsEdge`, `AddEdge_NodeMissing_ReturnsError`, `RemoveNode_CascadesEdges`, `CreateSnapshot_CapturesFullState`
  - All handler tests with fakes
- **Acceptance criteria**:
  - Full domain logic coverage
  - All handlers tested in isolation
  - Graph operations (add, remove, diff) verified

---

### Epic 5: TelemetryModule
Goal: Ingest metrics from Azure Application Insights, aggregate health scores, and publish telemetry overlays for the Graph module.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Create Telemetry domain model (MetricDataPoint, ServiceHealth, HealthScore) | Feature | Telemetry | M | 1.2 | ⬚ |
| 5.2 | Create Application Insights adapter | Feature | Telemetry | L | 5.1 | ⬚ |
| 5.3 | Create IngestTelemetry slice (polls and aggregates metrics) | Feature | Telemetry | L | 5.2 | ⬚ |
| 5.4 | Create GetServiceHealth query slice | Feature | Telemetry | M | 5.3 | ⬚ |
| 5.5 | Create TelemetryUpdated integration event for Graph overlay | Feature | Telemetry | M | 5.3, 4.1 | ⬚ |
| 5.6 | Create Telemetry persistence and module registration | Infrastructure | Telemetry | M | 5.3 | ⬚ |
| 5.7 | Write Telemetry module tests | Test | Telemetry | L | 5.2, 5.3, 5.4, 5.5 | ⬚ |

#### 5.1 – Create Telemetry Domain Model
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Domain/Metric/MetricDataPoint.cs`
  - `src/Modules/Telemetry/Telemetry.Domain/Metric/MetricDataPointId.cs`
  - `src/Modules/Telemetry/Telemetry.Domain/Metric/MetricType.cs` (enum: RequestRate, ErrorRate, Latency, Availability)
  - `src/Modules/Telemetry/Telemetry.Domain/Health/ServiceHealth.cs`
  - `src/Modules/Telemetry/Telemetry.Domain/Health/ServiceHealthId.cs`
  - `src/Modules/Telemetry/Telemetry.Domain/Health/HealthScore.cs` (value object: Green/Yellow/Red with numeric score)
  - `src/Modules/Telemetry/Telemetry.Domain/Health/ConnectionHealth.cs` (health per edge)
  - `src/Modules/Telemetry/Telemetry.Domain/Events/TelemetryUpdatedEvent.cs`
  - `src/Modules/Telemetry/Telemetry.Domain/Errors/TelemetryErrors.cs`
- **Acceptance criteria**:
  - MetricDataPoint stores time-series metrics per resource
  - ServiceHealth aggregates metrics into a HealthScore (green/yellow/red)
  - ConnectionHealth tracks health per service-to-service edge
  - Scoring logic: green (error rate < 1%, latency p95 < 500ms), yellow (< 5%, < 2s), red (above)

#### 5.2 – Create Application Insights Adapter
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Application/Ports/ITelemetrySource.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Azure/ApplicationInsightsAdapter.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Azure/AppInsightsMetricMapper.cs`
- **Acceptance criteria**:
  - Queries Application Insights API for requests, dependencies, exceptions
  - Maps App Insights data to domain MetricDataPoint records
  - Supports configurable time windows and polling intervals
  - Handles API rate limits

#### 5.3 – Create IngestTelemetry Slice
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Application/IngestTelemetry/IngestTelemetryCommand.cs`
  - `src/Modules/Telemetry/Telemetry.Application/IngestTelemetry/IngestTelemetryHandler.cs`
  - `src/Modules/Telemetry/Telemetry.Application/Ports/IMetricRepository.cs`
  - `src/Modules/Telemetry/Telemetry.Application/Ports/IServiceHealthRepository.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/BackgroundJobs/TelemetryPollingJob.cs` (hosted service)
  - `src/Modules/Telemetry/Telemetry.Api/Endpoints/TriggerIngestionEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `IngestTelemetryHandlerTests` – `Handle_NewMetrics_PersistsAndComputesHealth`, `Handle_NoMetrics_ReturnsEmpty`, `Handle_DegradedService_SetsYellowHealth`
  - Fakes/Fixtures needed: `FakeTelemetrySource`, `FakeMetricRepository`, `FakeServiceHealthRepository`
- **Acceptance criteria**:
  - Background job polls App Insights at configurable intervals
  - Manual trigger via POST /api/subscriptions/{subId}/telemetry/ingest
  - Computes HealthScore per service and per connection
  - Publishes TelemetryUpdatedEvent

#### 5.4 – Create GetServiceHealth Query Slice
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Application/GetServiceHealth/GetServiceHealthQuery.cs`
  - `src/Modules/Telemetry/Telemetry.Application/GetServiceHealth/GetServiceHealthHandler.cs`
  - `src/Modules/Telemetry/Telemetry.Application/GetServiceHealth/ServiceHealthDto.cs`
  - `src/Modules/Telemetry/Telemetry.Api/Endpoints/GetServiceHealthEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetServiceHealthHandlerTests` – `Handle_ExistingService_ReturnsHealth`, `Handle_ServiceNotFound_ReturnsError`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/health returns all service health scores
  - Supports filtering by service name or health status
  - Returns request rate, error rate, p95 latency, and health color per service

#### 5.5 – Create TelemetryUpdated Integration Event
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Application/IntegrationEvents/TelemetryUpdatedIntegrationEvent.cs`
  - `src/Modules/Graph/Graph.Application/IntegrationEventHandlers/TelemetryUpdatedHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `TelemetryUpdatedHandlerTests` – `Handle_ValidTelemetry_UpdatesGraphNodeHealth`, `Handle_ConnectionMetrics_UpdatesEdgeHealth`
- **Acceptance criteria**:
  - Graph module consumes telemetry updates and enriches node/edge properties with health scores
  - Graph update triggers GraphUpdatedEvent for real-time push to frontend

#### 5.6 – Create Telemetry Persistence and Module Registration
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Persistence/TelemetryDbContext.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Persistence/Configurations/*.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Persistence/Repositories/*.cs`
  - `src/Modules/Telemetry/Telemetry.Infrastructure/TelemetryInfrastructureModule.cs`
- **Acceptance criteria**:
  - Time-series metrics stored efficiently (consider partitioning by time)
  - Health scores stored per service per snapshot
  - Module registration wires up background job and all services

#### 5.7 – Write Telemetry Module Tests
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Tests/Domain/HealthScoreTests.cs`
  - `src/Modules/Telemetry/Telemetry.Tests/IngestTelemetry/IngestTelemetryHandlerTests.cs`
  - `src/Modules/Telemetry/Telemetry.Tests/GetServiceHealth/GetServiceHealthHandlerTests.cs`
  - `src/Modules/Telemetry/Telemetry.Tests/Fakes/*.cs`
- **Test plan (TDD)**:
  - Domain tests: `HealthScoreTests` – `Compute_LowErrors_ReturnsGreen`, `Compute_HighErrors_ReturnsRed`, `Compute_MediumLatency_ReturnsYellow`
  - All handler tests with fakes
- **Acceptance criteria**:
  - Health scoring logic fully tested with boundary cases
  - All handlers tested in isolation

---

### Epic 6: VisualizationModule
Goal: Serve diagram data to the frontend via REST and WebSocket, support real-time updates, and handle export.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Create Visualization domain model (DiagramView, ViewPreset, ExportFormat) | Feature | Visualization | S | 1.2 | ⬚ |
| 6.2 | Create GetDiagram query slice (combines graph + health into renderable diagram) | Feature | Visualization | M | 6.1, 4.3, 5.4 | ⬚ |
| 6.3 | Create WebSocket hub for real-time diagram updates | Feature | Visualization | L | 6.2 | ⬚ |
| 6.4 | Create ExportDiagram slice (SVG/PDF) | Feature | Visualization | M | 6.2 | ⬚ |
| 6.5 | Create SaveViewPreset and GetViewPresets slices | Feature | Visualization | S | 6.1 | ⬚ |
| 6.6 | Create Visualization persistence and module registration | Infrastructure | Visualization | S | 6.2 | ⬚ |
| 6.7 | Write Visualization module tests | Test | Visualization | M | 6.2, 6.3, 6.4 | ⬚ |

#### 6.1 – Create Visualization Domain Model
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Domain/DiagramView/DiagramView.cs`
  - `src/Modules/Visualization/Visualization.Domain/DiagramView/DiagramViewId.cs`
  - `src/Modules/Visualization/Visualization.Domain/ViewPreset/ViewPreset.cs`
  - `src/Modules/Visualization/Visualization.Domain/ViewPreset/ViewPresetId.cs`
  - `src/Modules/Visualization/Visualization.Domain/ExportFormat.cs` (enum: Svg, Pdf, Png)
  - `src/Modules/Visualization/Visualization.Domain/Errors/VisualizationErrors.cs`
- **Acceptance criteria**:
  - DiagramView represents a renderable view combining graph + health data
  - ViewPreset stores saved filter/zoom/grouping configurations per user

#### 6.2 – Create GetDiagram Query Slice
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Application/GetDiagram/GetDiagramQuery.cs`
  - `src/Modules/Visualization/Visualization.Application/GetDiagram/GetDiagramHandler.cs`
  - `src/Modules/Visualization/Visualization.Application/GetDiagram/DiagramDto.cs` (nodes with positions, edges with health colors, metadata)
  - `src/Modules/Visualization/Visualization.Application/Ports/IGraphModuleApi.cs` (cross-module contract)
  - `src/Modules/Visualization/Visualization.Application/Ports/ITelemetryModuleApi.cs` (cross-module contract)
  - `src/Modules/Visualization/Visualization.Api/Endpoints/GetDiagramEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetDiagramHandlerTests` – `Handle_ValidProject_ReturnsDiagramWithHealth`, `Handle_FilterByLevel_ReturnsFilteredNodes`, `Handle_NoGraph_ReturnsEmpty`
  - Fakes/Fixtures needed: `FakeGraphModuleApi`, `FakeTelemetryModuleApi`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/diagram returns renderable diagram data
  - Combines graph topology with health overlays
  - Supports query params: level, parentNodeId, groupBy, filter

#### 6.3 – Create WebSocket Hub for Real-Time Updates
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Api/Hubs/DiagramHub.cs` (SignalR hub)
  - `src/Modules/Visualization/Visualization.Application/IntegrationEventHandlers/GraphUpdatedNotifier.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/RealTime/DiagramUpdateBroadcaster.cs`
- **Acceptance criteria**:
  - SignalR hub at /hubs/diagram allows frontend to subscribe to project updates
  - When GraphUpdatedEvent fires, broadcasts incremental diagram updates to connected clients
  - Supports connection groups per project (only sends updates to relevant clients)
  - Handles reconnection gracefully

#### 6.4 – Create ExportDiagram Slice
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Application/ExportDiagram/ExportDiagramCommand.cs`
  - `src/Modules/Visualization/Visualization.Application/ExportDiagram/ExportDiagramHandler.cs`
  - `src/Modules/Visualization/Visualization.Application/Ports/IDiagramExporter.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Export/SvgDiagramExporter.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Export/PdfDiagramExporter.cs`
  - `src/Modules/Visualization/Visualization.Api/Endpoints/ExportDiagramEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `ExportDiagramHandlerTests` – `Handle_SvgFormat_ReturnsSvgBytes`, `Handle_PdfFormat_ReturnsPdfBytes`, `Handle_InvalidFormat_ReturnsError`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/diagram/export?format=svg returns SVG content
  - GET /api/projects/{projectId}/diagram/export?format=pdf returns PDF content
  - Exports include current health overlays

#### 6.5 – Create SaveViewPreset and GetViewPresets Slices
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Application/SaveViewPreset/SaveViewPresetCommand.cs`
  - `src/Modules/Visualization/Visualization.Application/SaveViewPreset/SaveViewPresetHandler.cs`
  - `src/Modules/Visualization/Visualization.Application/GetViewPresets/GetViewPresetsQuery.cs`
  - `src/Modules/Visualization/Visualization.Application/GetViewPresets/GetViewPresetsHandler.cs`
  - `src/Modules/Visualization/Visualization.Application/Ports/IViewPresetRepository.cs`
  - `src/Modules/Visualization/Visualization.Api/Endpoints/SaveViewPresetEndpoint.cs`
  - `src/Modules/Visualization/Visualization.Api/Endpoints/GetViewPresetsEndpoint.cs`
- **Acceptance criteria**:
  - POST /api/projects/{projectId}/view-presets saves a named preset (filters, zoom, grouping)
  - GET /api/projects/{projectId}/view-presets returns saved presets for the project

#### 6.6 – Create Visualization Persistence and Module Registration
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/VisualizationDbContext.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/Configurations/*.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/Repositories/*.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/VisualizationInfrastructureModule.cs`
- **Acceptance criteria**:
  - ViewPresets persisted to PostgreSQL
  - SignalR configured for WebSocket transport
  - Module registration wires up hub, exporters, and all services

#### 6.7 – Write Visualization Module Tests
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Tests/GetDiagram/GetDiagramHandlerTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/ExportDiagram/ExportDiagramHandlerTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/SaveViewPreset/SaveViewPresetHandlerTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/Fakes/*.cs`
- **Acceptance criteria**:
  - All handlers tested with fakes
  - Diagram assembly logic (graph + health → renderable) fully tested

---

### Epic 7: FrontendApplication
Goal: Build the React frontend with interactive C4 diagram visualization, real-time updates, traffic overlays, drill-down navigation, and export.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Create authentication flow (login, token management, protected routes) | Feature | Web | M | 2.5 | ⬚ |
| 7.2 | Create organization and project management pages | Feature | Web | M | 7.1, 2.2, 2.3 | ⬚ |
| 7.3 | Create Azure subscription connection wizard | Feature | Web | M | 7.2, 3.2 | ⬚ |
| 7.4 | Create C4 diagram renderer (canvas-based, interactive) | Feature | Web | XL | 7.2, 6.2 | ⬚ |
| 7.5 | Create real-time update integration (WebSocket → diagram) | Feature | Web | L | 7.4, 6.3 | ⬚ |
| 7.6 | Create traffic overlay and health indicators | Feature | Web | L | 7.4, 5.4 | ⬚ |
| 7.7 | Create drill-down navigation (context → container → component) | Feature | Web | M | 7.4 | ⬚ |
| 7.8 | Create filtering, grouping, and search controls | Feature | Web | M | 7.4 | ⬚ |
| 7.9 | Create IaC drift visualization overlay | Feature | Web | M | 7.4, 3.6 | ⬚ |
| 7.10 | Create diagram export feature (SVG/PDF download) | Feature | Web | S | 7.4, 6.4 | ⬚ |
| 7.11 | Create timeline/time navigation slider | Feature | Web | M | 7.4, 4.4 | ⬚ |
| 7.12 | Write frontend tests | Test | Web | L | 7.1–7.11 | ⬚ |

#### 7.4 – Create C4 Diagram Renderer
- **Files to create**:
  - `web/src/features/diagram/DiagramPage.tsx`
  - `web/src/features/diagram/components/DiagramCanvas.tsx` (WebGL/Canvas renderer)
  - `web/src/features/diagram/components/GraphNode.tsx`
  - `web/src/features/diagram/components/GraphEdge.tsx`
  - `web/src/features/diagram/components/NodeTooltip.tsx`
  - `web/src/features/diagram/components/MiniMap.tsx`
  - `web/src/features/diagram/hooks/useDiagram.ts`
  - `web/src/features/diagram/hooks/useGraphLayout.ts` (auto-layout algorithm)
  - `web/src/features/diagram/hooks/usePanZoom.ts`
  - `web/src/features/diagram/types.ts`
  - `web/src/features/diagram/api.ts`
- **Acceptance criteria**:
  - Renders C4 diagram with nodes and edges on a canvas
  - Supports pan, zoom, and node selection
  - Auto-layout positions nodes in a readable arrangement
  - Minimap shows full graph with viewport indicator
  - Tooltip on hover shows node metadata (type, owner, health)
  - Performance: smooth rendering with 500+ nodes

#### 7.5 – Create Real-Time Update Integration
- **Files to create**:
  - `web/src/features/diagram/hooks/useDiagramUpdates.ts`
  - `web/src/features/diagram/hooks/useSignalR.ts`
- **Acceptance criteria**:
  - Connects to SignalR hub on diagram page mount
  - Receives incremental updates and applies them to the diagram state
  - Shows connection status indicator (connected/reconnecting/disconnected)
  - Gracefully handles reconnection with state reconciliation

#### 7.6 – Create Traffic Overlay and Health Indicators
- **Files to create**:
  - `web/src/features/diagram/components/HealthOverlay.tsx`
  - `web/src/features/diagram/components/TrafficAnimation.tsx`
  - `web/src/features/diagram/hooks/useHealthData.ts`
- **Acceptance criteria**:
  - Nodes display colored borders (green/yellow/red) based on health score
  - Edges are colored by connection health
  - Animated traffic flow dots move along edges proportional to request rate
  - Tooltip on edge shows request rate, error rate, p95 latency
  - Toggle to show/hide traffic overlay

#### 7.7 – Create Drill-Down Navigation
- **Files to create**:
  - `web/src/features/diagram/components/Breadcrumb.tsx`
  - `web/src/features/diagram/hooks/useDrillDown.ts`
- **Acceptance criteria**:
  - Double-click on a node drills down to its children (e.g., context → containers)
  - Breadcrumb shows current navigation path
  - Back button navigates up one level
  - Smooth zoom animation during drill-down transition

#### 7.11 – Create Timeline/Time Navigation Slider
- **Files to create**:
  - `web/src/features/diagram/components/TimelineSlider.tsx`
  - `web/src/features/diagram/hooks/useTimeline.ts`
  - `web/src/features/diagram/api/snapshots.ts`
- **Acceptance criteria**:
  - Slider control allows selecting a point in time
  - Diagram updates to show the graph state at the selected snapshot
  - Diff mode highlights added/removed/modified nodes when comparing two snapshots
  - Play button animates through snapshots

#### 7.12 – Write Frontend Tests
- **Files to create**:
  - `web/src/features/auth/__tests__/AuthProvider.test.tsx`
  - `web/src/features/diagram/__tests__/DiagramCanvas.test.tsx`
  - `web/src/features/diagram/__tests__/useDiagram.test.ts`
  - `web/src/features/diagram/__tests__/HealthOverlay.test.tsx`
  - `web/src/shared/hooks/__tests__/useApi.test.ts`
- **Acceptance criteria**:
  - Component tests for key UI components using vitest + testing-library
  - Hook tests for data fetching and WebSocket integration
  - No `any` types in test code

---

### Epic 8: AIIntegration
Goal: Integrate Semantic Kernel for AI-driven architecture analysis and threat detection (basic MVP capabilities).

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 8.1 | Configure Semantic Kernel in Host (Azure OpenAI, filters, logging) | Infrastructure | Host | M | 1.4 | ⬚ |
| 8.2 | Create ArchitectureAnalysis SK plugin (classifies resources, suggests groupings) | Feature | Graph | M | 8.1, 4.1 | ⬚ |
| 8.3 | Create BasicThreatDetection SK plugin (STRIDE-based risk scoring) | Feature | Graph | L | 8.1, 4.1 | ⬚ |
| 8.4 | Create AnalyzeArchitecture slice (AI-powered analysis) | Feature | Graph | M | 8.2 | ⬚ |
| 8.5 | Create GetThreatAssessment slice | Feature | Graph | M | 8.3 | ⬚ |
| 8.6 | Write AI integration tests | Test | Graph | M | 8.2, 8.3, 8.4, 8.5 | ⬚ |

#### 8.1 – Configure Semantic Kernel in Host
- **Files to create**:
  - `src/Host/AI/SemanticKernelSetup.cs`
  - `src/Host/AI/AiPromptLoggingFilter.cs`
  - `src/Host/AI/AiFunctionLoggingFilter.cs`
- **Files to modify**: `src/Host/Program.cs`, `src/Host/appsettings.json`
- **Acceptance criteria**:
  - SK Kernel registered in DI with Azure OpenAI chat completion
  - Prompt and function invocation filters registered for observability
  - Model name, endpoint, API key externalized to configuration
  - Temperature = 0 by default

#### 8.2 – Create ArchitectureAnalysis SK Plugin
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ArchitectureAnalysisPlugin.cs`
- **Acceptance criteria**:
  - KernelFunction that analyzes a graph and classifies resources into C4 levels
  - KernelFunction that suggests logical groupings (by domain, team, technology)
  - Plugin registered via DI in GraphInfrastructureModule

#### 8.3 – Create BasicThreatDetection SK Plugin
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ThreatDetectionPlugin.cs`
  - `src/Modules/Graph/Graph.Domain/Threat/ThreatAssessment.cs`
  - `src/Modules/Graph/Graph.Domain/Threat/ThreatCategory.cs` (STRIDE categories)
  - `src/Modules/Graph/Graph.Domain/Threat/RiskLevel.cs`
- **Acceptance criteria**:
  - KernelFunction that analyzes graph nodes/edges for STRIDE threats
  - Returns risk scores per node and per edge
  - Identifies common patterns: public endpoints without auth, unencrypted connections, overly permissive network rules

#### 8.4 – Create AnalyzeArchitecture Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureCommand.cs`
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureHandler.cs`
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/ArchitectureAnalysisDto.cs`
  - `src/Modules/Graph/Graph.Api/Endpoints/AnalyzeArchitectureEndpoint.cs`
- **Acceptance criteria**:
  - POST /api/projects/{projectId}/analyze triggers AI analysis
  - Returns C4 level suggestions and grouping recommendations
  - Results are advisory; user can accept or dismiss

#### 8.5 – Create GetThreatAssessment Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentQuery.cs`
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentHandler.cs`
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/ThreatAssessmentDto.cs`
  - `src/Modules/Graph/Graph.Api/Endpoints/GetThreatAssessmentEndpoint.cs`
- **Acceptance criteria**:
  - GET /api/projects/{projectId}/threats returns threat assessment
  - Includes risk scores per node and per edge, STRIDE categories, mitigations
  - Supports filtering by risk level and category

#### 8.6 – Write AI Integration Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/AI/ArchitectureAnalysisPluginTests.cs`
  - `src/Modules/Graph/Graph.Tests/AI/ThreatDetectionPluginTests.cs`
  - `src/Modules/Graph/Graph.Tests/AnalyzeArchitecture/AnalyzeArchitectureHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/GetThreatAssessment/GetThreatAssessmentHandlerTests.cs`
- **Test plan (TDD)**:
  - Plugin tests with mocked Kernel/IChatCompletionService
  - Handler tests with faked plugins
- **Acceptance criteria**:
  - Plugins testable with mocked SK services
  - Handlers testable with faked plugin responses

---

### Epic 9: EndToEndIntegration
Goal: Wire everything together, validate the full flow from Azure connection to rendered diagram, and ensure Docker Compose startup works reliably.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 9.1 | Create database migrations for all modules | Infrastructure | All | M | 2.6, 3.8, 4.6, 5.6, 6.6 | ⬚ |
| 9.2 | Create seed data and demo mode (sample Azure architecture) | Feature | All | M | 9.1 | ⬚ |
| 9.3 | End-to-end acceptance tests (full flow via WebApplicationFactory) | Test | Host | L | All epics | ⬚ |
| 9.4 | Docker Compose smoke test and health verification | Test | All | M | 9.1, 1.5 | ⬚ |
| 9.5 | API documentation (OpenAPI/Swagger polish) | Documentation | Host | S | All epics | ⬚ |
| 9.6 | Architecture boundary tests (ArchUnitNET) | Test | All | M | All epics | ⬚ |

#### 9.1 – Create Database Migrations for All Modules
- **Acceptance criteria**:
  - EF Core migrations for Identity, Discovery, Graph, Telemetry, Visualization modules
  - Migrations run automatically on startup in development
  - Each module has its own schema/namespace to avoid table collisions

#### 9.2 – Create Seed Data and Demo Mode
- **Files to create**:
  - `src/Host/Seeding/DemoDataSeeder.cs`
  - `src/Host/Seeding/SampleAzureArchitecture.cs`
- **Acceptance criteria**:
  - When DEMO_MODE=true, seeds a realistic Azure architecture (10-15 services with relationships)
  - Seeds telemetry data with mixed health (some green, some yellow, some red)
  - Allows first-time users to see the product working without connecting Azure

#### 9.3 – End-to-End Acceptance Tests
- **Files to create**:
  - `src/Host.Tests/Host.Tests.csproj`
  - `src/Host.Tests/FullFlowTests.cs`
  - `src/Host.Tests/CustomWebApplicationFactory.cs`
- **Test plan (TDD)**:
  - `FullFlowTests` – `RegisterOrg_CreateProject_ConnectSubscription_Discover_GetDiagram_ReturnsHealth`
  - `FullFlowTests` – `Discovery_Triggers_GraphUpdate_AndWebSocketNotification`
- **Acceptance criteria**:
  - Tests exercise the complete flow from org creation to diagram retrieval
  - Use Testcontainers for PostgreSQL
  - Verify cross-module integration events fire correctly

#### 9.4 – Docker Compose Smoke Test
- **Acceptance criteria**:
  - `docker compose up --build` starts all services within 60 seconds
  - Health check endpoints respond with 200
  - Frontend loads and can reach backend API
  - PostgreSQL is accessible and migrations have run

#### 9.5 – API Documentation
- **Acceptance criteria**:
  - All endpoints have OpenAPI metadata (tags, descriptions, response types)
  - Swagger UI accessible at /swagger
  - API responses use consistent envelope format

#### 9.6 – Architecture Boundary Tests
- **Files to create**:
  - `src/Host.Tests/Architecture/ModuleBoundaryTests.cs`
  - `src/Host.Tests/Architecture/DependencyRuleTests.cs`
- **Acceptance criteria**:
  - ArchUnitNET tests verify no cross-module Application/Domain references
  - Tests verify Domain never references Infrastructure
  - Tests verify all handlers are internal and sealed
  - Tests verify all IDs are strongly typed

---

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Azure Resource Graph API complexity and rate limits | Medium | High | Abstract behind IAzureResourceDiscoverer port; use caching and incremental discovery; comprehensive error handling with retry/backoff |
| R2 | IaC parsing complexity (Bicep/Terraform HCL) | Medium | Medium | Start with subset of resource types; use existing parser libraries (Bicep CLI, HCL2 parser); fuzz test with real-world templates |
| R3 | Canvas-based diagram rendering performance at scale (500+ nodes) | Medium | High | Use WebGL (e.g., react-force-graph, Pixi.js); implement viewport culling, level-of-detail; lazy-load deep hierarchy levels |
| R4 | Real-time WebSocket reliability under load | Low | Medium | SignalR handles reconnection; implement server-side backpressure; batch small updates; use message deduplication |
| R5 | Semantic Kernel version stability (1.x evolving) | Low | Medium | Pin SK version; wrap SK types behind ports; comprehensive unit tests for plugins |
| R6 | Multi-tenant data isolation | Medium | High | Per-module schemas with tenant ID filtering; EF Core global query filters; acceptance tests verify isolation |
| R7 | Application Insights API latency and data staleness | Medium | Medium | Configurable polling interval; show data freshness indicator; graceful degradation when telemetry unavailable |
| R8 | Frontend state management complexity with real-time updates | Medium | Medium | Use React Query for server state; dedicated WebSocket state store; optimistic updates with reconciliation |

### Critical Path
1.1 → 1.2 → 1.3 → 1.4 → 2.1 → 2.2 → 2.6 → 3.1 → 3.2 → 3.3 → 3.4 → 3.8 → 4.1 → 4.2 → 4.6 → 5.1 → 5.2 → 5.3 → 5.5 → 6.2 → 6.3 → 7.4 → 7.5 → 7.6 → 9.1 → 9.3

### Estimated Total Effort
- S tasks: 5 × ~30 min = ~2.5 h
- M tasks: 33 × ~2.5 h = ~82.5 h
- L tasks: 13 × ~6 h = ~78 h
- XL tasks: 1 × ~12 h = ~12 h
- **Total: ~175 hours**
