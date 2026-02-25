## Plan: RemainingEpics
Scope: MVP
Created: 2026-02-25
Status: Draft

### Overview
Complete all remaining work from the Dynamic Architecture MVP plan. This covers Graph/Visualization persistence and real-time updates, Discovery persistence, AI integration via Semantic Kernel, database migrations, seed data, end-to-end acceptance tests, architecture boundary enforcement, and Docker smoke testing. The goal is to bring the MVP to production-ready state where all success criteria from the original plan are met.

### Success Criteria
- [ ] All modules persist data to PostgreSQL via EF Core (no in-memory-only repositories remain in production paths)
- [ ] Real-time diagram updates flow from backend to frontend via SignalR
- [ ] Semantic Kernel is configured with Ollama (local LLM) for chat completion and embeddings
- [ ] ArchitectureAnalysis and ThreatDetection SK plugins are functional with local models
- [ ] AnalyzeArchitecture and GetThreatAssessment slices are functional
- [ ] EF Core migrations exist for all modules and apply cleanly to a fresh database
- [ ] Seed data populates a demo environment for first-time users
- [ ] E2E acceptance tests verify key flows via WebApplicationFactory
- [ ] Architecture boundary tests enforce modular monolith rules via ArchUnitNET
- [ ] Docker Compose brings up backend, frontend, PostgreSQL, and Ollama with `docker compose up` and passes smoke tests
- [ ] Ollama container pulls required models (chat + embeddings) on first start
- [ ] All existing + new tests pass (`dotnet test`, `npm test`)
- [ ] Solution compiles with zero warnings

---

### Epic 1: Package and Infrastructure Prerequisites
Goal: Add all NuGet and npm packages required for remaining epics so that subsequent tasks can reference them immediately.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Add NuGet packages to Directory.Packages.props and module .csproj files | Infrastructure | All | S | – | ⬚ |
| 1.2 | Add SignalR client package to frontend | Infrastructure | Web | S | – | ⬚ |

#### 1.1 – Add NuGet Packages
- **Files to modify**:
  - `Directory.Packages.props` – add package versions for:
    - `Microsoft.SemanticKernel` (latest 1.x stable)
    - `Microsoft.SemanticKernel.Connectors.Ollama` (Ollama-native SK connector)
    - `OllamaSharp` (underlying Ollama client library used by SK connector)
    - `Microsoft.AspNetCore.SignalR.Client` (for integration tests)
    - `Testcontainers` and `Testcontainers.PostgreSql`
    - `ArchUnitNET` and `ArchUnitNET.xUnit`
    - `NSubstitute` (for mock-based tests where fakes are insufficient)
    - `Microsoft.AspNetCore.Mvc.Testing` (WebApplicationFactory)
  - `src/Modules/Graph/Graph.Infrastructure/Graph.Infrastructure.csproj` – add EF Core, Npgsql references
  - `src/Modules/Visualization/Visualization.Infrastructure/Visualization.Infrastructure.csproj` – add EF Core, Npgsql references
  - `src/Modules/Discovery/Discovery.Infrastructure/Discovery.Infrastructure.csproj` – add EF Core, Npgsql references
  - `src/Modules/Graph/Graph.Tests/Graph.Tests.csproj` – add Testcontainers, Microsoft.AspNetCore.Mvc.Testing
  - `src/Modules/Visualization/Visualization.Tests/Visualization.Tests.csproj` – add Testcontainers, Microsoft.AspNetCore.Mvc.Testing
  - `src/Modules/Discovery/Discovery.Tests/Discovery.Tests.csproj` – add Testcontainers
  - `src/Modules/Identity/Identity.Tests/Identity.Tests.csproj` – add Testcontainers, Microsoft.AspNetCore.Mvc.Testing
  - `src/Modules/Telemetry/Telemetry.Tests/Telemetry.Tests.csproj` – add Testcontainers, Microsoft.AspNetCore.Mvc.Testing
- **Acceptance criteria**:
  - `dotnet restore` succeeds with all new packages resolved
  - `dotnet build` succeeds with zero warnings
  - All existing 35 tests still pass

#### 1.2 – Add SignalR Client Package to Frontend
- **Files to modify**:
  - `web/package.json` – add `@microsoft/signalr` dependency
- **Acceptance criteria**:
  - `npm install` succeeds
  - `npm run build` produces production bundle without errors
  - Existing frontend test still passes

---

### Epic 2: Graph Module Completion
Goal: Add EF Core persistence to the Graph module, replacing in-memory stubs, and achieve comprehensive test coverage.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Create GraphDbContext with EF Core entity configurations | Infrastructure | Graph | M | 1.1 | ⬚ |
| 2.2 | Create Graph EF Core repositories | Infrastructure | Graph | M | 2.1 | ⬚ |
| 2.3 | Update Graph module registration for EF Core persistence | Infrastructure | Graph | S | 2.2 | ⬚ |
| 2.4 | Write Graph persistence integration tests | Test | Graph | M | 2.3 | ⬚ |
| 2.5 | Write Graph handler and acceptance tests | Test | Graph | M | 2.3 | ⬚ |

#### 2.1 – Create GraphDbContext with EF Core Entity Configurations
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/GraphDbContext.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Configurations/ArchitectureGraphConfiguration.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Configurations/GraphNodeConfiguration.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Configurations/GraphEdgeConfiguration.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Configurations/GraphSnapshotConfiguration.cs`
- **Files to modify**:
  - `src/Modules/Graph/Graph.Infrastructure/Graph.Infrastructure.csproj` – add project references to Graph.Domain, Graph.Application, Shared Infrastructure
- **Test plan (TDD)**:
  - Unit tests: `GraphDbContextTests` – `OnModelCreating_ConfiguresAllEntities`, `ArchitectureGraph_HasCorrectTableMapping`
  - Fakes/Fixtures needed: EF Core InMemory provider for basic config validation
- **Acceptance criteria**:
  - DbContext maps ArchitectureGraph, GraphNode, GraphEdge, GraphSnapshot entities
  - Strongly typed IDs are converted via value converters
  - Owned types and value objects configured correctly
  - `dotnet build` succeeds

#### 2.2 – Create Graph EF Core Repositories
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Repositories/ArchitectureGraphRepository.cs`
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/GraphUnitOfWork.cs`
- **Acceptance criteria**:
  - `ArchitectureGraphRepository` implements `IArchitectureGraphRepository` from Application layer
  - `GraphUnitOfWork` implements `IUnitOfWork` wrapping `GraphDbContext.SaveChangesAsync`
  - Repository methods use `Include` for eager loading of nodes, edges, snapshots
  - All methods propagate `CancellationToken`

#### 2.3 – Update Graph Module Registration for EF Core Persistence
- **Files to modify**:
  - `src/Modules/Graph/Graph.Api/ServiceCollectionExtensions.cs` – register GraphDbContext, repositories, unit of work
- **Files to delete** (or mark as test-only):
  - Move `src/Modules/Graph/Graph.Api/Persistence/InMemoryArchitectureGraphRepository.cs` to test project as fake
  - Move `src/Modules/Graph/Graph.Api/Persistence/NoOpGraphUnitOfWork.cs` to test project as fake
- **Acceptance criteria**:
  - `AddGraphModule(configuration)` registers `GraphDbContext` with PostgreSQL connection string
  - Falls back to InMemory provider when no connection string configured (for tests)
  - `dotnet build` succeeds; all existing tests still pass

#### 2.4 – Write Graph Persistence Integration Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/Infrastructure/ArchitectureGraphRepositoryTests.cs`
  - `src/Modules/Graph/Graph.Tests/Fixtures/GraphDatabaseFixture.cs`
  - `src/Modules/Graph/Graph.Tests/Builders/ArchitectureGraphBuilder.cs`
- **Test plan (TDD)**:
  - Integration tests: `ArchitectureGraphRepositoryTests` – `UpsertAsync_NewGraph_PersistsSuccessfully`, `GetByProjectIdAsync_ExistingGraph_ReturnsWithNodesAndEdges`, `UpsertAsync_UpdatedGraph_MergesChanges`, `GetByProjectIdAsync_NonExistentProject_ReturnsNull`
  - Fakes/Fixtures needed: `GraphDatabaseFixture` using EF Core InMemory or Testcontainers PostgreSQL
  - Builders needed: `ArchitectureGraphBuilder` with fluent API
- **Acceptance criteria**:
  - All repository methods tested against a real (or in-memory) database
  - Tests are tagged `[Trait("Category", "Integration")]`
  - Tests pass in isolation and as part of full suite

#### 2.5 – Write Graph Handler and Acceptance Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/GetGraph/GetGraphHandlerTests.cs` (expand existing)
  - `src/Modules/Graph/Graph.Tests/GetGraphDiff/GetGraphDiffHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeArchitectureGraphRepository.cs` (move from Api project)
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeGraphUnitOfWork.cs` (move from Api project)
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_ExistingProject_ReturnsGraph`, `Handle_NonExistentProject_ReturnsFailure`
  - Unit tests: `GetGraphDiffHandlerTests` – `Handle_TwoSnapshots_ReturnsDiff`, `Handle_NoSnapshots_ReturnsFailure`
  - Unit tests: `ResourcesDiscoveredHandlerTests` – (expand existing) `Handle_NewResources_CreatesGraphAndSnapshot`, `Handle_ExistingGraph_AddsNewSnapshot`
- **Acceptance criteria**:
  - Every handler has at least 2 meaningful test cases
  - All tests use fakes (not mocks) for repository dependencies
  - Tests follow Arrange/Act/Assert pattern with FluentAssertions

---

### Epic 3: Visualization Module Completion
Goal: Add EF Core persistence, SignalR real-time hub, and comprehensive test coverage to the Visualization module.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Create VisualizationDbContext with EF Core entity configurations | Infrastructure | Visualization | M | 1.1 | ⬚ |
| 3.2 | Create Visualization EF Core repositories | Infrastructure | Visualization | M | 3.1 | ⬚ |
| 3.3 | Create SignalR DiagramHub for real-time diagram updates | Feature | Visualization | M | 1.2 | ⬚ |
| 3.4 | Wire SignalR hub into Host and update frontend WebSocket hook | Feature | Visualization/Web | M | 3.3 | ⬚ |
| 3.5 | Update Visualization module registration | Infrastructure | Visualization | S | 3.2, 3.3 | ⬚ |
| 3.6 | Write Visualization integration and acceptance tests | Test | Visualization | M | 3.5 | ⬚ |

#### 3.1 – Create VisualizationDbContext with EF Core Entity Configurations
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/VisualizationDbContext.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/Configurations/ViewPresetConfiguration.cs`
- **Files to modify**:
  - `src/Modules/Visualization/Visualization.Infrastructure/Visualization.Infrastructure.csproj` – add project references
- **Acceptance criteria**:
  - DbContext maps ViewPreset entity with proper column types
  - Strongly typed IDs use value converters
  - `dotnet build` succeeds

#### 3.2 – Create Visualization EF Core Repositories
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/Repositories/ViewPresetRepository.cs`
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/VisualizationUnitOfWork.cs`
- **Acceptance criteria**:
  - `ViewPresetRepository` implements `IViewPresetRepository` from Application layer
  - All methods propagate `CancellationToken`
  - Repository uses async EF Core operations

#### 3.3 – Create SignalR DiagramHub for Real-Time Diagram Updates
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Api/Hubs/DiagramHub.cs`
  - `src/Modules/Visualization/Visualization.Api/Hubs/IDiagramClient.cs` (strongly typed hub interface)
  - `src/Modules/Visualization/Visualization.Application/Ports/IDiagramNotifier.cs` (port for pushing updates)
  - `src/Modules/Visualization/Visualization.Api/Adapters/SignalRDiagramNotifier.cs` (adapter wrapping IHubContext)
- **Test plan (TDD)**:
  - Unit tests: `SignalRDiagramNotifierTests` – `NotifyDiagramUpdated_SendsToGroup`, `NotifyHealthChanged_SendsToGroup`
  - Fakes/Fixtures needed: `FakeDiagramNotifier` for handler tests
- **Acceptance criteria**:
  - Hub at `/hubs/diagram` accepts connections
  - Clients join project-specific groups
  - Server can push `DiagramUpdated`, `HealthOverlayChanged`, `NodeAdded`, `NodeRemoved` events
  - IDiagramNotifier port keeps Application layer clean of SignalR dependency

#### 3.4 – Wire SignalR Hub into Host and Update Frontend
- **Files to modify**:
  - `src/Host/Program.cs` – add `builder.Services.AddSignalR()`, map hub endpoint
  - `web/src/shared/hooks/useWebSocket.ts` – replace raw WebSocket with `@microsoft/signalr` `HubConnectionBuilder`
  - `web/src/shared/api/websocket.ts` – update to use SignalR connection
- **Acceptance criteria**:
  - SignalR hub accessible at `/hubs/diagram`
  - Frontend connects via SignalR client with automatic reconnection
  - CORS policy allows SignalR from frontend origin
  - Existing diagram page receives push updates

#### 3.5 – Update Visualization Module Registration
- **Files to modify**:
  - `src/Modules/Visualization/Visualization.Api/ServiceCollectionExtensions.cs` – register DbContext, repositories, SignalR notifier
- **Files to move to tests**:
  - `src/Modules/Visualization/Visualization.Api/Persistence/InMemoryDiagramReadModel.cs` → test fake
  - `src/Modules/Visualization/Visualization.Api/Persistence/InMemoryViewPresetRepository.cs` → test fake
  - `src/Modules/Visualization/Visualization.Api/Persistence/NoOpVisualizationUnitOfWork.cs` → test fake
- **Acceptance criteria**:
  - Module registers all persistence and SignalR services
  - Falls back to InMemory when no connection string configured
  - `dotnet build` succeeds; all existing tests pass

#### 3.6 – Write Visualization Integration and Acceptance Tests
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Tests/Infrastructure/ViewPresetRepositoryTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/GetDiagram/GetDiagramHandlerTests.cs` (expand existing)
  - `src/Modules/Visualization/Visualization.Tests/ExportDiagram/ExportDiagramHandlerTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/SaveViewPreset/SaveViewPresetHandlerTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/Fakes/FakeViewPresetRepository.cs`
  - `src/Modules/Visualization/Visualization.Tests/Fakes/FakeDiagramNotifier.cs`
  - `src/Modules/Visualization/Visualization.Tests/Builders/ViewPresetBuilder.cs`
- **Test plan (TDD)**:
  - Integration tests: `ViewPresetRepositoryTests` – `SaveAsync_NewPreset_Persists`, `GetByProjectIdAsync_ReturnsAll`
  - Unit tests: `ExportDiagramHandlerTests` – `Handle_SvgFormat_ReturnsSvgBytes`, `Handle_PdfFormat_ReturnsPdfBytes`
  - Unit tests: `SaveViewPresetHandlerTests` – `Handle_ValidPreset_Saves`, `Handle_InvalidPreset_ReturnsError`
- **Acceptance criteria**:
  - Every handler and repository method has test coverage
  - Tests tagged with appropriate `[Trait("Category", "...")]`

---

### Epic 4: Discovery Module Persistence
Goal: Replace in-memory repositories in the Discovery module with EF Core persistence backed by PostgreSQL.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Create DiscoveryDbContext with EF Core entity configurations | Infrastructure | Discovery | M | 1.1 | ⬚ |
| 4.2 | Create Discovery EF Core repositories | Infrastructure | Discovery | M | 4.1 | ⬚ |
| 4.3 | Update Discovery module registration for EF Core persistence | Infrastructure | Discovery | S | 4.2 | ⬚ |
| 4.4 | Write Discovery persistence integration tests | Test | Discovery | M | 4.3 | ⬚ |

#### 4.1 – Create DiscoveryDbContext with EF Core Entity Configurations
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/DiscoveryDbContext.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Configurations/AzureSubscriptionConfiguration.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Configurations/DiscoveredResourceConfiguration.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Configurations/DriftResultConfiguration.cs`
- **Files to modify**:
  - `src/Modules/Discovery/Discovery.Infrastructure/Discovery.Infrastructure.csproj` – add project references to Discovery.Domain, Discovery.Application, Shared Infrastructure
- **Test plan (TDD)**:
  - Integration tests: covered in 4.4
- **Acceptance criteria**:
  - DbContext maps AzureSubscription, DiscoveredResource, ResourceRelationship, DriftResult entities
  - Strongly typed IDs converted via value converters
  - `dotnet build` succeeds

#### 4.2 – Create Discovery EF Core Repositories
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Repositories/AzureSubscriptionRepository.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Repositories/DiscoveredResourceRepository.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Repositories/DriftResultRepository.cs`
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/DiscoveryUnitOfWork.cs`
- **Acceptance criteria**:
  - All repositories implement ports from Discovery.Application
  - All methods use async EF Core operations with CancellationToken
  - Repositories follow established patterns from Identity and Telemetry modules

#### 4.3 – Update Discovery Module Registration for EF Core Persistence
- **Files to modify**:
  - `src/Modules/Discovery/Discovery.Api/ServiceCollectionExtensions.cs` – register DiscoveryDbContext and EF Core repositories
- **Files to move to tests**:
  - `src/Modules/Discovery/Discovery.Api/Persistence/InMemoryAzureSubscriptionRepository.cs` → test fake
  - `src/Modules/Discovery/Discovery.Api/Persistence/InMemoryDiscoveredResourceRepository.cs` → test fake
  - `src/Modules/Discovery/Discovery.Api/Persistence/InMemoryDriftResultRepository.cs` → test fake
- **Acceptance criteria**:
  - `AddDiscoveryModule(configuration)` registers `DiscoveryDbContext` with PostgreSQL
  - Falls back to InMemory when no connection string
  - All 8 existing Discovery tests still pass

#### 4.4 – Write Discovery Persistence Integration Tests
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Tests/Infrastructure/AzureSubscriptionRepositoryTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Infrastructure/DiscoveredResourceRepositoryTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Fixtures/DiscoveryDatabaseFixture.cs`
- **Test plan (TDD)**:
  - Integration tests: `AzureSubscriptionRepositoryTests` – `AddAsync_NewSubscription_Persists`, `GetByProjectIdAsync_ReturnsSubscriptions`
  - Integration tests: `DiscoveredResourceRepositoryTests` – `SaveAllAsync_NewResources_PersistsAll`, `GetBySubscriptionIdAsync_ReturnsResources`
- **Acceptance criteria**:
  - All repository operations verified against database
  - Tests tagged `[Trait("Category", "Integration")]`

---

### Epic 5: AI Integration with Semantic Kernel + Ollama
Goal: Configure Semantic Kernel with Ollama (local LLM) for chat completion and embeddings, create ArchitectureAnalysis and ThreatDetection plugins, and expose them as vertical slices. Uses locally-pulled models — no external API keys required.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Configure Semantic Kernel in Host with Ollama | Infrastructure | Host | M | 1.1 | ⬚ |
| 5.2 | Create SK logging filters (IPromptRenderFilter, IFunctionInvocationFilter) | Feature | Shared | S | 5.1 | ⬚ |
| 5.3 | Create ArchitectureAnalysis SK plugin | Feature | Graph | L | 5.1 | ⬚ |
| 5.4 | Create BasicThreatDetection SK plugin (STRIDE-based risk scoring) | Feature | Graph | L | 5.1 | ⬚ |
| 5.5 | Create AnalyzeArchitecture vertical slice | Feature | Graph | M | 5.3 | ⬚ |
| 5.6 | Create GetThreatAssessment vertical slice | Feature | Graph | M | 5.4 | ⬚ |
| 5.7 | Write AI integration tests | Test | Graph | M | 5.5, 5.6 | ⬚ |

#### 5.1 – Configure Semantic Kernel in Host with Ollama
- **Files to modify**:
  - `src/Host/Program.cs` – register Kernel with Ollama chat completion and embedding services
  - `src/Host/appsettings.json` – add Ollama configuration section:
    ```json
    "Ollama": {
      "Endpoint": "http://ollama:11434",
      "ChatModel": "llama3.1",
      "EmbeddingModel": "nomic-embed-text"
    }
    ```
  - `src/Host/appsettings.Development.json` – override with localhost endpoint for local dev:
    ```json
    "Ollama": {
      "Endpoint": "http://localhost:11434"
    }
    ```
- **Files to create**:
  - `src/Shared/Infrastructure/AI/SemanticKernelExtensions.cs` – extension method `AddSemanticKernel(configuration)` that:
    - Registers chat completion via `services.AddOllamaChatCompletion(chatModel, endpoint)` (requires `#pragma warning disable SKEXP0070` — all Ollama connector APIs are experimental/alpha)
    - Registers embeddings via `services.AddOllamaEmbeddingGenerator(embeddingModel, endpoint)` (uses new `IEmbeddingGenerator<string, Embedding<float>>` from `Microsoft.Extensions.AI`)
    - Registers `Kernel` as transient from the service provider
    - Registers all SK plugins from modules
  - `src/Shared/Infrastructure/AI/OllamaHealthCheck.cs` – health check that verifies Ollama is reachable and models are available
- **Docker Compose changes** (task 5.1 includes updating docker-compose.yml):
  - Add `ollama` service using `ollama/ollama` image
  - Add `ollama-pull` init service that pulls required models on first start:
    ```yaml
    ollama:
      image: ollama/ollama:latest
      ports:
        - "11434:11434"
      volumes:
        - ollama_data:/root/.ollama
      environment:
        - OLLAMA_NUM_PARALLEL=1
      healthcheck:
        test: ["CMD", "ollama", "list"]
        interval: 10s
        timeout: 5s
        retries: 5
      restart: unless-stopped

    ollama-init:
      image: ollama/ollama:latest
      depends_on:
        ollama:
          condition: service_healthy
      restart: "no"
      entrypoint: ["/bin/sh", "-c"]
      command:
        - |
          ollama pull qwen2.5-coder:7b --host http://ollama:11434
          ollama pull nomic-embed-text --host http://ollama:11434

    volumes:
      ollama_data:
    ```
  - Backend `depends_on` updated to include `ollama` healthy
- **Acceptance criteria**:
  - Kernel registered in DI with `IChatCompletionService` backed by Ollama
  - `ITextEmbeddingGenerationService` registered for embedding operations
  - Model names and endpoint come from configuration (never hard-coded)
  - Kernel is injectable into handlers and plugins
  - `docker compose up` starts Ollama and pulls models automatically
  - Health check endpoint reports Ollama status
  - `dotnet build` succeeds

**Recommended Ollama Models:**
| Purpose | Model | Size | Notes |
|---------|-------|------|-------|
| Chat / Architecture Analysis | `qwen2.5-coder:7b` | ~4.7 GB | Best code model at 7B class; 128K context; supports function calling |
| Chat / Alternative | `llama3.1:8b` | ~4.7 GB | Strong general + code; good function calling support |
| Chat / Lightweight (CI) | `phi3:mini` (3.8B) | ~2.3 GB | Fast iteration; good structured output |
| Embeddings | `nomic-embed-text` | ~0.5 GB | 768 dimensions, 8K context; best speed/quality tradeoff |
| Embeddings / Alternative | `mxbai-embed-large` | ~1.2 GB | 1024 dimensions; higher retrieval quality |

**Key Ollama + SK Gotchas (from research):**
- All `AddOllama*` methods require `#pragma warning disable SKEXP0070` — the connector is alpha/experimental
- SK Ollama connector does NOT expose `ResponseFormat` for structured JSON output (GitHub issue #11452) — use prompt engineering + manual JSON parsing instead
- Function calling is model-dependent — only models tagged with `tools` capability on ollama.com support it (llama3.1, qwen2.5-coder, phi4, mistral)
- Function calling has broken across SK version upgrades — pin both SK and OllamaSharp versions together
- Running chat + embedding models simultaneously may cause model offloading if VRAM is constrained — set `OLLAMA_NUM_PARALLEL=1` if needed

#### 5.2 – Create SK Logging Filters
- **Files to create**:
  - `src/Shared/Infrastructure/AI/PromptLoggingFilter.cs` – implements `IPromptRenderFilter`
  - `src/Shared/Infrastructure/AI/FunctionInvocationLoggingFilter.cs` – implements `IFunctionInvocationFilter`
- **Acceptance criteria**:
  - All prompt renders logged with template name, rendered text length, timestamp
  - All function invocations logged with function name, duration, success/failure
  - Uses `ILogger<T>` for structured logging
  - Filters registered in kernel pipeline

#### 5.3 – Create ArchitectureAnalysis SK Plugin
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ArchitectureAnalysisPlugin.cs`
  - `src/Modules/Graph/Graph.Infrastructure/AI/Prompts/AnalyzeArchitecturePrompt.cs` (prompt constants)
- **Test plan (TDD)**:
  - Unit tests: `ArchitectureAnalysisPluginTests` – `AnalyzeGraph_WithNodes_ReturnsInsights`, `SuggestImprovements_WithGraph_ReturnsSuggestions`
  - Fakes/Fixtures needed: `FakeChatCompletionService` returning canned AI responses
- **Acceptance criteria**:
  - Plugin class with `[KernelFunction]` and `[Description]` on methods
  - `AnalyzeArchitectureAsync` accepts graph data, returns structured analysis (component coupling, dependency patterns, single points of failure)
  - `SuggestImprovementsAsync` returns actionable architecture recommendations
  - Plugin registered in Graph module DI
  - Uses `Temperature = 0` for deterministic output
  - Prompts designed for local model capabilities (clear instructions, structured output format, avoid relying on multi-step reasoning chains that exceed local model context windows)

#### 5.4 – Create BasicThreatDetection SK Plugin
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ThreatDetectionPlugin.cs`
  - `src/Modules/Graph/Graph.Infrastructure/AI/Prompts/ThreatDetectionPrompt.cs` (prompt constants)
  - `src/Modules/Graph/Graph.Domain/ThreatAssessment/ThreatAssessment.cs` (value object: risk level, category, description)
  - `src/Modules/Graph/Graph.Domain/ThreatAssessment/ThreatCategory.cs` (enum: Spoofing, Tampering, Repudiation, InformationDisclosure, DenialOfService, ElevationOfPrivilege)
  - `src/Modules/Graph/Graph.Domain/ThreatAssessment/RiskLevel.cs` (enum: Critical, High, Medium, Low, Informational)
- **Test plan (TDD)**:
  - Unit tests: `ThreatDetectionPluginTests` – `AssessThreats_WithGraph_ReturnsSTRIDEThreats`, `ScoreRisk_WithThreats_ReturnsAggregateScore`
  - Fakes/Fixtures needed: `FakeChatCompletionService`
- **Acceptance criteria**:
  - Plugin performs STRIDE-based threat analysis on architecture graph
  - Returns list of ThreatAssessment value objects with category, risk level, affected components, description, and mitigation suggestions
  - Calculates aggregate risk score for the overall architecture
  - Uses prompt-engineered JSON output with manual parsing (SK Ollama connector does not support ResponseFormat; request JSON in prompt, parse with `System.Text.Json`)

#### 5.5 – Create AnalyzeArchitecture Vertical Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureCommand.cs`
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureHandler.cs`
  - `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureResponse.cs`
  - `src/Modules/Graph/Graph.Application/Ports/IArchitectureAnalyzer.cs` (port for the SK plugin)
  - `src/Modules/Graph/Graph.Api/Endpoints/AnalyzeArchitectureEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `AnalyzeArchitectureHandlerTests` – `Handle_ValidProject_ReturnsAnalysis`, `Handle_NoGraph_ReturnsFailure`
  - Fakes/Fixtures needed: `FakeArchitectureAnalyzer`
- **Acceptance criteria**:
  - POST `/api/projects/{projectId}/analyze` triggers AI analysis
  - Handler loads graph, passes to analyzer port, returns structured results
  - Response includes component insights, dependency analysis, improvement suggestions

#### 5.6 – Create GetThreatAssessment Vertical Slice
- **Files to create**:
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentQuery.cs`
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentHandler.cs`
  - `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentResponse.cs`
  - `src/Modules/Graph/Graph.Application/Ports/IThreatDetector.cs` (port for the SK plugin)
  - `src/Modules/Graph/Graph.Api/Endpoints/GetThreatAssessmentEndpoint.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetThreatAssessmentHandlerTests` – `Handle_ValidProject_ReturnsThreatAssessment`, `Handle_NoGraph_ReturnsFailure`
  - Fakes/Fixtures needed: `FakeThreatDetector`
- **Acceptance criteria**:
  - GET `/api/projects/{projectId}/threats` returns STRIDE threat assessment
  - Response includes list of threats with category, risk level, affected nodes, mitigations
  - Response includes aggregate risk score

#### 5.7 – Write AI Integration Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/AnalyzeArchitecture/AnalyzeArchitectureHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/GetThreatAssessment/GetThreatAssessmentHandlerTests.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeArchitectureAnalyzer.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeThreatDetector.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeChatCompletionService.cs`
- **Test plan (TDD)**:
  - Unit tests: Handler tests with fake ports (no real Ollama calls)
  - Unit tests: Plugin tests with `FakeChatCompletionService` returning deterministic JSON responses matching the structured output format expected from local models
  - Optional: Integration tests tagged `[Trait("Category", "OllamaIntegration")]` that call a real local Ollama instance (skipped in CI if Ollama unavailable)
- **Acceptance criteria**:
  - All AI handlers tested without requiring running Ollama instance
  - Plugin tests verify prompt construction and response parsing (especially JSON structured output parsing since local models may produce less reliable structured output)
  - Tests are fast and deterministic
  - FakeChatCompletionService simulates Ollama-style responses (no function calling, text-based structured output)

---

### Epic 6: Database Migrations and Seed Data
Goal: Generate EF Core migrations for all modules and create a seed data service for demo environments.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Create Identity module EF Core migrations | Infrastructure | Identity | S | – | ⬚ |
| 6.2 | Create Telemetry module EF Core migrations | Infrastructure | Telemetry | S | – | ⬚ |
| 6.3 | Create Graph module EF Core migrations | Infrastructure | Graph | S | 2.1 | ⬚ |
| 6.4 | Create Discovery module EF Core migrations | Infrastructure | Discovery | S | 4.1 | ⬚ |
| 6.5 | Create Visualization module EF Core migrations | Infrastructure | Visualization | S | 3.1 | ⬚ |
| 6.6 | Create seed data service and demo mode | Feature | Host | M | 6.1, 6.2, 6.3, 6.4, 6.5 | ⬚ |
| 6.7 | Wire auto-migration and seeding into Host startup | Infrastructure | Host | S | 6.6 | ⬚ |

#### 6.1 – Create Identity Module EF Core Migrations
- **Files to create**:
  - `src/Modules/Identity/Identity.Infrastructure/Persistence/Migrations/` – initial migration files
- **Acceptance criteria**:
  - Migration creates Organizations, Projects, Members tables with correct column types
  - `dotnet ef migrations` output shows clean initial migration
  - Migration is idempotent (can run on fresh and existing databases)

#### 6.2 – Create Telemetry Module EF Core Migrations
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Infrastructure/Persistence/Migrations/` – initial migration files
- **Acceptance criteria**:
  - Migration creates MetricDataPoints, ServiceHealthRecords tables
  - Clean migration with proper indexes

#### 6.3 – Create Graph Module EF Core Migrations
- **Files to create**:
  - `src/Modules/Graph/Graph.Infrastructure/Persistence/Migrations/` – initial migration files
- **Acceptance criteria**:
  - Migration creates ArchitectureGraphs, GraphNodes, GraphEdges, GraphSnapshots tables
  - Foreign keys and cascading deletes configured correctly

#### 6.4 – Create Discovery Module EF Core Migrations
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Infrastructure/Persistence/Migrations/` – initial migration files
- **Acceptance criteria**:
  - Migration creates AzureSubscriptions, DiscoveredResources, DriftResults tables
  - Indexes on subscription ID and resource type for query performance

#### 6.5 – Create Visualization Module EF Core Migrations
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Infrastructure/Persistence/Migrations/` – initial migration files
- **Acceptance criteria**:
  - Migration creates ViewPresets table
  - Proper indexes on project ID

#### 6.6 – Create Seed Data Service and Demo Mode
- **Files to create**:
  - `src/Host/Seeding/SeedDataService.cs` – orchestrates seeding across all modules
  - `src/Host/Seeding/DemoData.cs` – constants for demo organization, project, subscription, resources, graph
- **Acceptance criteria**:
  - Seeds a demo organization with a demo project
  - Seeds a fake Azure subscription with discovered resources (App Service, SQL Database, Storage Account, Virtual Network)
  - Seeds an architecture graph with nodes and edges matching discovered resources
  - Seeds telemetry data with health metrics
  - Seeds view presets (default, security-focused, performance-focused)
  - Idempotent: does not duplicate data on repeated runs

#### 6.7 – Wire Auto-Migration and Seeding into Host Startup
- **Files to modify**:
  - `src/Host/Program.cs` – add migration and seeding on startup in Development environment
- **Acceptance criteria**:
  - In Development: auto-apply migrations and seed demo data on startup
  - In Production: migrations only via explicit command, no auto-seeding
  - Application starts successfully against fresh PostgreSQL

---

### Epic 7: End-to-End Testing and Quality Assurance
Goal: Establish E2E testing infrastructure, write acceptance tests for all modules, create architecture boundary tests, and validate Docker Compose deployment.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Create shared test infrastructure (WebApplicationFactory + Testcontainers) | Infrastructure | Tests | M | 1.1, 6.7 | ⬚ |
| 7.2 | Write Identity module acceptance tests | Test | Identity | M | 7.1 | ⬚ |
| 7.3 | Write Discovery module acceptance tests | Test | Discovery | M | 7.1 | ⬚ |
| 7.4 | Write Graph module acceptance tests | Test | Graph | M | 7.1 | ⬚ |
| 7.5 | Write Telemetry module acceptance tests | Test | Telemetry | M | 7.1 | ⬚ |
| 7.6 | Write Visualization module acceptance tests | Test | Visualization | M | 7.1 | ⬚ |
| 7.7 | Write cross-module E2E scenario tests | Test | Host | L | 7.2, 7.3, 7.4, 7.5, 7.6 | ⬚ |
| 7.8 | Create architecture boundary tests (ArchUnitNET) | Test | Shared | M | 1.1 | ⬚ |
| 7.9 | Docker Compose smoke test and health verification | Test | Infrastructure | M | 6.7 | ⬚ |
| 7.10 | OpenAPI/Swagger polish | Documentation | Host | S | – | ⬚ |
| 7.11 | Expand frontend test coverage | Test | Web | M | 1.2 | ⬚ |

#### 7.1 – Create Shared Test Infrastructure
- **Files to create**:
  - `src/Shared/Testing/Testing.csproj` – shared test utilities project
  - `src/Shared/Testing/C4WebApplicationFactory.cs` – custom `WebApplicationFactory<Program>` with Testcontainers PostgreSQL
  - `src/Shared/Testing/PostgresFixture.cs` – reusable Testcontainers PostgreSQL fixture
  - `src/Shared/Testing/HttpClientExtensions.cs` – helper methods for authenticated requests
  - `src/Shared/Testing/TestAuthHandler.cs` – fake JWT auth handler for tests
- **Acceptance criteria**:
  - `C4WebApplicationFactory` starts the full application with real database (Testcontainers)
  - Authentication is bypassed with configurable test claims
  - Each test class gets an isolated database
  - Fixture disposal cleans up containers

#### 7.2 – Write Identity Module Acceptance Tests
- **Files to create**:
  - `src/Modules/Identity/Identity.Tests/Acceptance/RegisterOrganizationEndpointTests.cs`
  - `src/Modules/Identity/Identity.Tests/Acceptance/CreateProjectEndpointTests.cs`
  - `src/Modules/Identity/Identity.Tests/Acceptance/InviteMemberEndpointTests.cs`
- **Test plan (TDD)**:
  - Acceptance tests: `RegisterOrganizationEndpointTests` – `Post_ValidRequest_Returns201WithOrganization`, `Post_DuplicateName_Returns409`, `Post_InvalidRequest_Returns400`
  - Acceptance tests: `CreateProjectEndpointTests` – `Post_ValidRequest_Returns201`, `Post_NonExistentOrg_Returns404`
- **Acceptance criteria**:
  - Tests exercise full HTTP request/response cycle
  - Tests verify status codes, response bodies, headers
  - Tagged `[Trait("Category", "Acceptance")]`

#### 7.3 – Write Discovery Module Acceptance Tests
- **Files to create**:
  - `src/Modules/Discovery/Discovery.Tests/Acceptance/ConnectSubscriptionEndpointTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Acceptance/DiscoverResourcesEndpointTests.cs`
- **Test plan (TDD)**:
  - Acceptance tests: Full HTTP cycle for connect, discover, detect drift, get status flows
- **Acceptance criteria**:
  - Tests verify integration events are published on discovery

#### 7.4 – Write Graph Module Acceptance Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/Acceptance/GetGraphEndpointTests.cs`
  - `src/Modules/Graph/Graph.Tests/Acceptance/AnalyzeArchitectureEndpointTests.cs`
- **Test plan (TDD)**:
  - Acceptance tests: `GetGraphEndpointTests` – `Get_ExistingProject_Returns200WithGraph`, `Get_NonExistentProject_Returns404`
- **Acceptance criteria**:
  - Full slice tested from HTTP through handler to database and back

#### 7.5 – Write Telemetry Module Acceptance Tests
- **Files to create**:
  - `src/Modules/Telemetry/Telemetry.Tests/Acceptance/IngestTelemetryEndpointTests.cs`
  - `src/Modules/Telemetry/Telemetry.Tests/Acceptance/GetServiceHealthEndpointTests.cs`
- **Acceptance criteria**:
  - Tests verify telemetry ingestion persists and health queries return correct data

#### 7.6 – Write Visualization Module Acceptance Tests
- **Files to create**:
  - `src/Modules/Visualization/Visualization.Tests/Acceptance/GetDiagramEndpointTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/Acceptance/ExportDiagramEndpointTests.cs`
  - `src/Modules/Visualization/Visualization.Tests/Acceptance/ViewPresetEndpointTests.cs`
- **Acceptance criteria**:
  - Tests verify export produces valid SVG/PDF bytes
  - Tests verify view presets round-trip correctly

#### 7.7 – Write Cross-Module E2E Scenario Tests
- **Files to create**:
  - `src/Host/Host.Tests/Host.Tests.csproj`
  - `src/Host/Host.Tests/Scenarios/FullDiscoveryFlowTests.cs` – register org → create project → connect subscription → discover resources → verify graph → check diagram
  - `src/Host/Host.Tests/Scenarios/TelemetryOverlayFlowTests.cs` – ingest telemetry → verify health → check diagram overlay
- **Test plan (TDD)**:
  - E2E test: `FullDiscoveryFlowTests` – `FullDiscoveryFlow_RegisterToVisualization_Succeeds`
  - E2E test: `TelemetryOverlayFlowTests` – `TelemetryIngestion_UpdatesHealthOverlay_Succeeds`
- **Acceptance criteria**:
  - Tests exercise the complete user journey across module boundaries
  - Tests verify integration events propagate correctly
  - Tagged `[Trait("Category", "E2E")]`

#### 7.8 – Create Architecture Boundary Tests (ArchUnitNET)
- **Files to create**:
  - `src/Shared/Kernel.Tests/Architecture/ModuleBoundaryTests.cs`
  - `src/Shared/Kernel.Tests/Architecture/DependencyRuleTests.cs`
  - `src/Shared/Kernel.Tests/Architecture/NamingConventionTests.cs`
- **Files to modify**:
  - `src/Shared/Kernel.Tests/Kernel.Tests.csproj` – add ArchUnitNET package references, add project references to all modules
- **Test plan (TDD)**:
  - `ModuleBoundaryTests` – `ApplicationLayer_DoesNotReference_OtherModulesApplication`, `DomainLayer_DoesNotReference_Infrastructure`
  - `DependencyRuleTests` – `InfrastructureTypes_DoNotAppearIn_DomainOrApplication`, `Adapters_AreInternal`
  - `NamingConventionTests` – `Handlers_AreSuffixedWithHandler`, `Commands_AreSuffixedWithCommand`, `Queries_AreSuffixedWithQuery`
- **Acceptance criteria**:
  - Tests enforce dependency direction: Domain ← Application ← Infrastructure
  - Tests verify no cross-module application references
  - Tests verify infrastructure types never leak into domain/application
  - Tests verify naming conventions

#### 7.9 – Docker Compose Smoke Test
- **Files to create**:
  - `tests/smoke/docker-smoke-test.sh` – script that brings up Docker Compose, waits for health, runs basic API calls (including AI analysis endpoint), tears down
- **Files to modify**:
  - `docker-compose.yml` – ensure health check endpoints are configured for all services including Ollama
  - `src/Host/Dockerfile` – verify multi-stage build works with all new packages
  - `web/Dockerfile` – verify production build works
- **Acceptance criteria**:
  - `docker compose up -d` starts all services (postgres, ollama, backend, frontend)
  - Ollama model pull completes successfully (ollama-pull init container exits 0)
  - Health endpoints return 200 within 120 seconds (longer timeout for model pull on first run)
  - Basic API calls (register org, create project) succeed
  - AI analysis endpoint responds (verifies Ollama connectivity)
  - `docker compose down` cleans up

#### 7.10 – OpenAPI/Swagger Polish
- **Files to modify**:
  - `src/Host/Program.cs` – add OpenAPI metadata (title, version, description, contact)
  - All endpoint files – add `.WithTags()`, `.WithName()`, `.Produces<T>()`, `.ProducesValidationProblem()` metadata
- **Acceptance criteria**:
  - Swagger UI shows grouped endpoints by module
  - Each endpoint has descriptive name and tag
  - Response types documented for 200, 400, 401, 404, 500

#### 7.11 – Expand Frontend Test Coverage
- **Files to create**:
  - `web/src/features/auth/AuthPage.test.tsx`
  - `web/src/features/diagram/hooks/useDiagram.test.ts`
  - `web/src/features/diagram/components/DiagramCanvas.test.tsx`
  - `web/src/shared/hooks/useApi.test.ts`
  - `web/src/shared/hooks/useWebSocket.test.ts`
- **Test plan (TDD)**:
  - Component tests: AuthPage renders login button, handles OAuth redirect
  - Hook tests: useDiagram returns nodes/edges, handles loading/error states
  - Hook tests: useApi handles fetch, loading, error states
  - Component tests: DiagramCanvas renders React Flow with nodes
- **Acceptance criteria**:
  - At least 10 frontend tests covering key components and hooks
  - `npm test` passes
  - Tests use vitest + jsdom

---

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Semantic Kernel Ollama connector is pre-release and may have breaking changes | Medium | High | Pin to specific pre-release version; wrap SK calls behind ports so connector can be swapped |
| R2 | Testcontainers requires Docker daemon running in CI/test environment | Medium | Medium | Fall back to EF Core InMemory provider when Docker unavailable; tag integration tests for conditional skip |
| R3 | SignalR client/server version mismatch between .NET and @microsoft/signalr npm | Low | Medium | Pin matching versions; verify in 3.4 integration |
| R4 | Multiple EF Core DbContexts may cause migration naming collisions | Low | Low | Use separate migration history tables per module (`__EFMigrationsHistory_<Module>`) |
| R5 | AI integration tests depend on deterministic LLM responses | Medium | Medium | Use `FakeChatCompletionService` returning canned responses; never call real Ollama in automated tests |
| R6 | Docker Compose build may fail with new .NET 9 SDK requirements | Low | Medium | Update Dockerfile to use correct .NET 9 SDK/runtime images; test in 7.9 |
| R7 | ArchUnitNET may require assembly scanning that conflicts with test isolation | Low | Low | Run boundary tests in dedicated test class with explicit assembly loading |
| R8 | .NET 9 SDK not available in package managers (only .NET 8 and 10) | High | High | Install via dotnet-install.sh script; document in README; already mitigated |
| R9 | Ollama model pull is slow on first start (~5 GB for llama3.1) | Medium | Low | Use Docker volume for model persistence; pull only once; CI can use phi3 (2.3 GB) for speed |
| R10 | Local model quality insufficient for architecture analysis | Medium | Medium | Design prompts with explicit structured output format (JSON); use few-shot examples; fall back to simpler heuristic analysis if LLM output is unreliable |
| R11 | Ollama GPU requirements may exceed available hardware | Medium | Medium | All recommended models run on CPU (slower but functional); GPU optional for performance; document minimum RAM: 8 GB for llama3.1, 4 GB for phi3 |

### Critical Path
1.1 → 2.1 → 2.2 → 2.3 → 6.3 → 6.7 → 7.1 → 7.4 → 7.7

### Estimated Total Effort
- S tasks: 8 × ~30 min = ~4 h
- M tasks: 24 × ~2.5 h = ~60 h
- L tasks: 3 × ~6 h = ~18 h
- XL tasks: 0
- **Total: ~82 hours**
