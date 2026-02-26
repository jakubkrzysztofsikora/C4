## Plan: DiagramFeedbackLoop
Scope: FeatureSet
Created: 2026-02-26
Last Updated: 2026-02-26
Status: Draft

### Overview
Build a feedback loop and evaluation system that captures user corrections, ratings, and annotations on AI-generated diagrams, classifications, threat assessments, and architecture analyses. This structured feedback feeds into a learning pipeline that refines future AI outputs — improving node classification accuracy (via `ResourceClassifierPlugin`), diagram layout quality, edge relationship correctness, threat assessment relevance (via `ThreatDetectionPlugin`), and architecture analysis quality (via `ArchitectureAnalysisPlugin`) over time. The system introduces a new **Feedback** module, extends the existing **Graph** and **Discovery** modules' AI plugins, and adds frontend feedback and eval dashboard features to the React app.

### Success Criteria
- [ ] Users can rate AI outputs (diagrams, classifications, threat assessments, architecture analyses) on a 1–5 scale with optional structured corrections
- [ ] Users can submit node-level corrections (reclassify C4 level, rename, change service type, correct parent hierarchy, correct relationships)
- [ ] Users can annotate diagram elements with free-text feedback
- [ ] All feedback is persisted with full provenance (authenticated user via JWT, timestamp, target AI output, correction details)
- [ ] Feedback is aggregated into learning summaries per feedback category
- [ ] AI prompts in `ArchitectureAnalysisPlugin`, `ThreatDetectionPlugin`, and `ResourceClassifierPlugin` are augmented with relevant learnings from past feedback
- [ ] An eval dashboard shows feedback trends, AI accuracy improvement, and top correction patterns
- [ ] All modules have unit tests for handlers and domain logic, plus acceptance tests for endpoints
- [ ] Solution compiles and all existing 59+ tests continue to pass after every task

---

### Epic 1: FeedbackDomainModel
Goal: Establish the Feedback module with domain entities that capture structured user feedback on AI-generated outputs.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Create Feedback module project structure | Infrastructure | Feedback | M | – | ⬚ |
| 1.2 | Define FeedbackEntry aggregate and value objects | Feature | Feedback | M | 1.1 | ⬚ |
| 1.3 | Define FeedbackCategory and FeedbackTargetType enums | Feature | Feedback | S | 1.1 | ⬚ |
| 1.4 | Define NodeCorrection and EdgeCorrection value objects | Feature | Feedback | S | 1.2 | ⬚ |
| 1.5 | Write domain model unit tests | Test | Feedback | M | 1.2, 1.3, 1.4 | ⬚ |

#### 1.1 – Create Feedback Module Project Structure
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Api/Feedback.Api.csproj`
  - `src/Modules/Feedback/Feedback.Api/AssemblyReference.cs`
  - `src/Modules/Feedback/Feedback.Api/GlobalUsings.cs`
  - `src/Modules/Feedback/Feedback.Api/ServiceCollectionExtensions.cs`
  - `src/Modules/Feedback/Feedback.Api/FeedbackHealthEndpoint.cs`
  - `src/Modules/Feedback/Feedback.Application/Feedback.Application.csproj`
  - `src/Modules/Feedback/Feedback.Application/AssemblyReference.cs`
  - `src/Modules/Feedback/Feedback.Domain/Feedback.Domain.csproj`
  - `src/Modules/Feedback/Feedback.Domain/AssemblyReference.cs`
  - `src/Modules/Feedback/Feedback.Infrastructure/Feedback.Infrastructure.csproj`
  - `src/Modules/Feedback/Feedback.Infrastructure/AssemblyReference.cs`
  - `src/Modules/Feedback/Feedback.Tests/Feedback.Tests.csproj`
  - `src/Modules/Feedback/Feedback.Tests/GlobalUsings.cs`
  - `src/Modules/Feedback/Feedback.Tests/SmokeTests.cs`
- **Files to modify**:
  - `C4.sln` (add new projects)
  - `src/Host/Host.csproj` (add Feedback.Api reference)
  - `src/Host/Program.cs` (register `.AddFeedbackModule(builder.Configuration)` alongside existing `.AddIdentityModule()`, `.AddDiscoveryModule()`, etc.)
- **Implementation notes**:
  - Follow the same module registration pattern as `Discovery.Api.ServiceCollectionExtensions`: register MediatR, DbContext (InMemory for dev / PostgreSQL for prod), endpoint discovery via `services.AddEndpoints(AssemblyReference.Assembly)`
  - `Feedback.Infrastructure.csproj` must reference `Microsoft.SemanticKernel` for the LearningAggregatorPlugin (Epic 5)
  - `Feedback.Api.csproj` references `Feedback.Application`, `Feedback.Infrastructure`, `Shared.Infrastructure`
- **Test plan (TDD)**:
  - Unit tests: `SmokeTests` – `HealthCheck_ReturnsHealthy`
  - Fakes/Fixtures needed: none yet
- **Acceptance criteria**:
  - All 5 Feedback projects compile
  - Project references follow Domain ← Application ← Infrastructure/Api
  - Health endpoint responds on `/health` aggregate
  - Solution builds green with zero warnings

#### 1.2 – Define FeedbackEntry Aggregate and Value Objects
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackEntry.cs`
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackEntryId.cs` (StronglyTypedId)
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackRating.cs` (value object: 1–5 scale)
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackTarget.cs` (value object: target type + target ID)
  - `src/Modules/Feedback/Feedback.Domain/Events/FeedbackSubmittedEvent.cs` (IDomainEvent)
  - `src/Modules/Feedback/Feedback.Domain/Errors/FeedbackErrors.cs`
- **Implementation notes**:
  - `FeedbackEntryId` follows the `StronglyTypedId<T>` pattern used throughout (e.g., `GraphNodeId`, `DiscoveredResourceId`, `UserId`)
  - `FeedbackEntry.Submit()` factory takes `UserId` (Guid) from JWT `sub` claim — aligns with Identity module's `User.Id`
- **Acceptance criteria**:
  - `FeedbackEntry` is an `AggregateRoot<FeedbackEntryId>` with factory method `Submit()`
  - `FeedbackRating` enforces 1–5 range via `Result<T>`
  - `FeedbackTarget` captures what was evaluated (diagram, classification, threat assessment, architecture analysis, resource classification)
  - `FeedbackSubmittedEvent` domain event is raised on creation
  - Immutable after creation (feedback entries are append-only)

#### 1.3 – Define FeedbackCategory and FeedbackTargetType Enums
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackCategory.cs`
  - `src/Modules/Feedback/Feedback.Domain/FeedbackEntry/FeedbackTargetType.cs`
- **Acceptance criteria**:
  - `FeedbackCategory`: `DiagramLayout`, `NodeClassification`, `EdgeRelationship`, `ThreatAssessment`, `ArchitectureAnalysis`, `ResourceClassification`, `General`
  - `FeedbackTargetType`: `Diagram`, `GraphNode`, `GraphEdge`, `AnalysisResult`, `ThreatResult`, `ClassificationResult`
- **Implementation notes**:
  - `ResourceClassification` category maps to feedback on `ResourceClassifierPlugin` outputs (C4 level, service type, friendly name, include/exclude decisions)
  - `ClassificationResult` target type maps to feedback on a specific `AzureResourceClassification` result

#### 1.4 – Define NodeCorrection and EdgeCorrection Value Objects
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Domain/Corrections/NodeCorrection.cs`
  - `src/Modules/Feedback/Feedback.Domain/Corrections/EdgeCorrection.cs`
  - `src/Modules/Feedback/Feedback.Domain/Corrections/ClassificationCorrection.cs`
  - `src/Modules/Feedback/Feedback.Domain/Corrections/CorrectionType.cs`
- **Acceptance criteria**:
  - `NodeCorrection` record: `OriginalName`, `CorrectedName`, `OriginalLevel` (C4Level string), `CorrectedLevel`, `OriginalServiceType` (app/api/database/queue/cache/external), `CorrectedServiceType`, `OriginalParentId` (Guid?), `CorrectedParentId` (Guid?)
  - `EdgeCorrection` record: `OriginalRelationship`, `CorrectedRelationship`, `ShouldExist` (bool for "this edge shouldn't be here")
  - `ClassificationCorrection` record: `ArmResourceType`, `OriginalFriendlyName`, `CorrectedFriendlyName`, `OriginalServiceType`, `CorrectedServiceType`, `OriginalC4Level`, `CorrectedC4Level`, `OriginalIncludeInDiagram`, `CorrectedIncludeInDiagram`
  - `CorrectionType` enum: `Reclassification`, `Rename`, `RelationshipChange`, `ParentChange`, `Addition`, `Removal`, `InclusionChange`
  - All are `sealed record` types with value equality
- **Implementation notes**:
  - `ClassificationCorrection` directly mirrors the fields of `AzureResourceClassification` (FriendlyName, ServiceType, C4Level, IncludeInDiagram) so feedback can precisely correct any field the `ResourceClassifierPlugin` classified
  - `NodeCorrection.ParentId` fields align with `GraphNode.ParentId` and `GraphNodeDto.ParentNodeId` introduced in the parent-child hierarchy feature

#### 1.5 – Write Domain Model Unit Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/Domain/FeedbackEntryTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/Domain/FeedbackRatingTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/Domain/NodeCorrectionTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/Domain/ClassificationCorrectionTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `FeedbackEntryTests` – `Submit_ValidInput_CreatesFeedbackEntry`, `Submit_InvalidRating_ReturnsFailure`, `Submit_RaisesFeedbackSubmittedEvent`
  - Unit tests: `FeedbackRatingTests` – `Create_ValidScore_ReturnsSuccess`, `Create_ZeroScore_ReturnsFailure`, `Create_ScoreAboveFive_ReturnsFailure`
  - Unit tests: `NodeCorrectionTests` – `Create_WithCorrectedLevel_CapturesChange`, `Create_WithCorrectedParent_CapturesChange`
  - Unit tests: `ClassificationCorrectionTests` – `Create_WithCorrectedServiceType_CapturesChange`, `Create_WithInclusionChange_CapturesChange`
  - Fakes/Fixtures needed: none (pure domain tests)
- **Acceptance criteria**:
  - All domain invariants are covered by tests
  - Tests follow `Arrange/Act/Assert` with blank-line separation

---

### Epic 2: FeedbackPersistence
Goal: Implement the data access layer for storing and querying feedback entries using EF Core and PostgreSQL.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Define feedback repository port | Feature | Feedback | S | 1.2 | ⬚ |
| 2.2 | Implement FeedbackDbContext and entity configuration | Feature | Feedback | M | 2.1 | ⬚ |
| 2.3 | Implement FeedbackEntryRepository adapter | Feature | Feedback | M | 2.2 | ⬚ |
| 2.4 | Create InMemoryFeedbackEntryRepository for tests | Test | Feedback | S | 2.1 | ⬚ |
| 2.5 | Create EF Core migration | Infrastructure | Feedback | S | 2.2 | ⬚ |

#### 2.1 – Define Feedback Repository Port
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/Ports/IFeedbackEntryRepository.cs`
- **Acceptance criteria**:
  - Interface defines: `AddAsync`, `FindByIdAsync`, `FindByTargetAsync(FeedbackTargetType, Guid)`, `GetByProjectAsync(Guid, int skip, int take)`, `CountByTargetAsync`
  - Lives in Application layer (port, not adapter)
  - Follows the same pattern as `ITelemetryRepository` and `IDiscoveredResourceRepository`

#### 2.2 – Implement FeedbackDbContext and Entity Configuration
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/Persistence/FeedbackDbContext.cs`
  - `src/Modules/Feedback/Feedback.Infrastructure/Persistence/FeedbackDbContextFactory.cs`
  - `src/Modules/Feedback/Feedback.Infrastructure/Persistence/Configurations/FeedbackEntryConfiguration.cs`
- **Implementation notes**:
  - Follow the same DbContext pattern as `GraphDbContext` and `TelemetryDbContext` (extend `BaseDbContext`, implement `IUnitOfWork`)
  - Use `HasConversion` for `FeedbackEntryId` → `Guid` and `FeedbackRating` → `int` following the `StronglyTypedId` pattern
  - Use `HasColumnType("jsonb")` for PostgreSQL JSON column storage of `NodeCorrection`, `EdgeCorrection`, and `ClassificationCorrection`
- **Acceptance criteria**:
  - `FeedbackDbContext` extends `BaseDbContext`
  - `FeedbackEntry` maps to `feedback.feedback_entries` table
  - Strongly typed IDs converted via value converters
  - `FeedbackRating` stored as integer column
  - Corrections stored as JSON columns
  - Index on `(TargetType, TargetId)` and `(ProjectId, CreatedAtUtc)`

#### 2.3 – Implement FeedbackEntryRepository Adapter
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/Persistence/Repositories/FeedbackEntryRepository.cs`
- **Implementation notes**:
  - Follow the pattern from `TelemetryRepository` and `ArchitectureGraphRepository`
- **Acceptance criteria**:
  - Implements `IFeedbackEntryRepository`
  - All methods are `async`/`await` with `CancellationToken` propagation
  - Class is `internal sealed` with primary constructor

#### 2.4 – Create InMemoryFeedbackEntryRepository for Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/Fakes/InMemoryFeedbackEntryRepository.cs`
  - `src/Modules/Feedback/Feedback.Tests/Fakes/InMemoryFeedbackUnitOfWork.cs`
- **Implementation notes**:
  - Follow the pattern from `InMemoryTelemetryRepository` in `Telemetry.Api/Persistence/`
- **Acceptance criteria**:
  - Implements `IFeedbackEntryRepository` using in-memory dictionary
  - Exposes `All` property for test assertions

#### 2.5 – Create EF Core Migration
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/Persistence/Migrations/` (generated)
- **Files to modify**:
  - `src/Host/SeedDataService.cs` (add Feedback migration alongside existing Graph, Telemetry, Identity, Discovery migrations)
- **Acceptance criteria**:
  - Migration creates `feedback.feedback_entries` table
  - Schema matches entity configuration
  - `SeedDataService` includes feedback DB migration

---

### Epic 3: SubmitFeedbackSlice
Goal: Implement the vertical slice for users to submit feedback on AI-generated outputs, using JWT authentication to identify the user.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Create SubmitFeedback command, handler, and validator | Feature | Feedback | M | 1.2, 2.1 | ⬚ |
| 3.2 | Create SubmitFeedback API endpoint with JWT auth | Feature | Feedback | S | 3.1 | ⬚ |
| 3.3 | Write SubmitFeedbackHandler unit tests | Test | Feedback | M | 3.1, 2.4 | ⬚ |
| 3.4 | Write SubmitFeedback acceptance tests | Test | Feedback | M | 3.2 | ⬚ |

#### 3.1 – Create SubmitFeedback Command, Handler, and Validator
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackCommand.cs`
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackValidator.cs`
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackResponse.cs`
- **Implementation notes**:
  - `UserId` is extracted from JWT `sub` claim at the endpoint level (see 3.2) and passed into the command — not from the request body
  - Follow the handler pattern from `IngestTelemetryHandler` and `RegisterUserHandler`
- **Acceptance criteria**:
  - `SubmitFeedbackCommand` record: `ProjectId`, `TargetType`, `TargetId`, `Category`, `Rating` (1–5), `Comment` (optional), `NodeCorrection` (optional), `EdgeCorrection` (optional), `ClassificationCorrection` (optional), `UserId`
  - Handler creates `FeedbackEntry` via domain factory, persists via repository
  - Validator ensures: Rating 1–5, TargetType valid, ProjectId not empty
  - Returns `SubmitFeedbackResponse(FeedbackEntryId)`

#### 3.2 – Create SubmitFeedback API Endpoint with JWT Auth
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Api/Endpoints/SubmitFeedbackEndpoint.cs`
- **Implementation notes**:
  - Use `.RequireAuthorization()` on the endpoint (aligns with the JWT Bearer auth configured in `Host/Program.cs`)
  - Extract `UserId` from `ClaimTypes.NameIdentifier` (the `sub` claim mapped by `JwtTokenService`) via `HttpContext.User`
  - Follows the pattern established by `LoginUserEndpoint` and `RegisterUserEndpoint` in the Identity module
  - Frontend will send `Authorization: Bearer <token>` header via the `getAuthHeaders()` function in `shared/api/client.ts`
- **Acceptance criteria**:
  - `POST /api/projects/{projectId}/feedback`
  - Requires JWT authentication via `.RequireAuthorization()`
  - Extracts `UserId` from JWT claims
  - Returns `201 Created` with location header
  - Returns `400 Bad Request` for validation failures
  - Returns `401 Unauthorized` for missing/invalid token

#### 3.3 – Write SubmitFeedbackHandler Unit Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/SubmitFeedback/SubmitFeedbackHandlerTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/Builders/FeedbackCommandBuilder.cs`
- **Test plan (TDD)**:
  - Unit tests: `SubmitFeedbackHandlerTests` – `Handle_ValidFeedback_CreatesFeedbackEntry`, `Handle_ValidFeedback_PersistsFeedbackEntry`, `Handle_WithNodeCorrection_IncludesCorrection`, `Handle_WithEdgeCorrection_IncludesCorrection`, `Handle_WithClassificationCorrection_IncludesCorrection`, `Handle_InvalidRating_ReturnsFailure`
  - Fakes/Fixtures needed: `InMemoryFeedbackEntryRepository`, `InMemoryFeedbackUnitOfWork`, `FeedbackCommandBuilder`
- **Acceptance criteria**:
  - All handler behaviors tested in isolation
  - Builder pattern used for test data

#### 3.4 – Write SubmitFeedback Acceptance Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/SubmitFeedback/SubmitFeedbackEndpointTests.cs`
- **Implementation notes**:
  - Use `C4WebApplicationFactory` with `TestAuthHandler` for authenticated requests (follows pattern from existing acceptance tests)
- **Test plan (TDD)**:
  - Module tests: `SubmitFeedbackEndpointTests` – `POST_Feedback_ValidRequest_ReturnsCreated`, `POST_Feedback_InvalidRating_ReturnsBadRequest`, `POST_Feedback_Unauthenticated_ReturnsUnauthorized`
  - Fakes/Fixtures needed: `C4WebApplicationFactory`, `TestAuthHandler`
- **Acceptance criteria**:
  - Tests exercise the full vertical slice via HTTP
  - JWT authentication is enforced

---

### Epic 4: QueryFeedbackSlices
Goal: Implement vertical slices for querying feedback data — by project, by target, and aggregated summaries.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Create GetFeedbackByProject query and handler | Feature | Feedback | M | 2.1 | ⬚ |
| 4.2 | Create GetFeedbackSummary query and handler | Feature | Feedback | M | 2.1 | ⬚ |
| 4.3 | Create query endpoints | Feature | Feedback | S | 4.1, 4.2 | ⬚ |
| 4.4 | Write query handler unit tests | Test | Feedback | M | 4.1, 4.2 | ⬚ |

#### 4.1 – Create GetFeedbackByProject Query and Handler
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackByProject/GetFeedbackByProjectQuery.cs`
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackByProject/GetFeedbackByProjectHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackByProject/FeedbackEntryDto.cs`
- **Acceptance criteria**:
  - Query accepts `ProjectId`, `Skip`, `Take`, optional `Category` filter
  - Returns paginated list of `FeedbackEntryDto`
  - DTO includes: `Id`, `TargetType`, `TargetId`, `Category`, `Rating`, `Comment`, `CreatedAtUtc`, `UserId`

#### 4.2 – Create GetFeedbackSummary Query and Handler
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackSummary/GetFeedbackSummaryQuery.cs`
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackSummary/GetFeedbackSummaryHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/GetFeedbackSummary/FeedbackSummaryDto.cs`
- **Files to modify**:
  - `src/Modules/Feedback/Feedback.Application/Ports/IFeedbackEntryRepository.cs` (add `GetSummaryByProjectAsync`)
- **Acceptance criteria**:
  - Returns aggregated summary per project: average rating by category, total count by category, most common corrections, trend over time (last 30 days)
  - `FeedbackSummaryDto`: `TotalCount`, `AverageRating`, `CategoryBreakdown[]` (category, count, avgRating), `TopCorrections[]` (description, frequency), `TrendPoints[]` (date, avgRating, count)

#### 4.3 – Create Query Endpoints
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Api/Endpoints/GetFeedbackByProjectEndpoint.cs`
  - `src/Modules/Feedback/Feedback.Api/Endpoints/GetFeedbackSummaryEndpoint.cs`
- **Implementation notes**:
  - Both endpoints use `.RequireAuthorization()` for JWT auth
- **Acceptance criteria**:
  - `GET /api/projects/{projectId}/feedback?skip=0&take=20&category=` – paginated feedback list
  - `GET /api/projects/{projectId}/feedback/summary` – aggregated summary
  - Both require JWT authentication

#### 4.4 – Write Query Handler Unit Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/GetFeedbackByProject/GetFeedbackByProjectHandlerTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/GetFeedbackSummary/GetFeedbackSummaryHandlerTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetFeedbackByProjectHandlerTests` – `Handle_HasFeedback_ReturnsPaginatedList`, `Handle_EmptyProject_ReturnsEmptyList`, `Handle_CategoryFilter_ReturnsFilteredResults`
  - Unit tests: `GetFeedbackSummaryHandlerTests` – `Handle_HasFeedback_ReturnsAggregatedSummary`, `Handle_NoFeedback_ReturnsZeroCounts`, `Handle_MultipleCategories_ReturnsBreakdown`
  - Fakes/Fixtures needed: `InMemoryFeedbackEntryRepository` (extended with summary support)
- **Acceptance criteria**:
  - Pagination is tested
  - Category filtering is tested
  - Summary aggregation logic is covered

---

### Epic 5: LearningAggregation
Goal: Build the learning pipeline that aggregates user feedback into structured insights that can be fed back into AI prompts.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Define LearningInsight aggregate | Feature | Feedback | M | 1.2 | ⬚ |
| 5.2 | Define learning aggregation port | Feature | Feedback | S | 5.1 | ⬚ |
| 5.3 | Implement SK-based LearningAggregatorPlugin | Feature | Feedback | L | 5.2 | ⬚ |
| 5.4 | Create AggregateLearnings command and handler | Feature | Feedback | M | 5.1, 5.2 | ⬚ |
| 5.5 | Create GetLearnings query and handler | Feature | Feedback | S | 5.1 | ⬚ |
| 5.6 | Write learning aggregation tests | Test | Feedback | M | 5.3, 5.4, 5.5 | ⬚ |

#### 5.1 – Define LearningInsight Aggregate
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Domain/Learning/LearningInsight.cs`
  - `src/Modules/Feedback/Feedback.Domain/Learning/LearningInsightId.cs`
  - `src/Modules/Feedback/Feedback.Domain/Learning/InsightType.cs`
  - `src/Modules/Feedback/Feedback.Domain/Events/LearningsAggregatedEvent.cs`
- **Acceptance criteria**:
  - `LearningInsight` is an `AggregateRoot<LearningInsightId>`
  - Properties: `ProjectId`, `Category` (FeedbackCategory), `InsightType` (enum: `ClassificationPattern`, `NamingConvention`, `RelationshipRule`, `LayoutPreference`, `ThreatPattern`, `ResourceTypeMapping`), `Description` (natural language insight), `Confidence` (0.0–1.0), `FeedbackCount` (how many feedback entries contributed), `CreatedAtUtc`, `ExpiresAtUtc`
  - Factory method `Aggregate()` creates from feedback data
  - Insights have a confidence score and expiration (stale learnings expire)
- **Implementation notes**:
  - `ResourceTypeMapping` insight type captures learnings about how specific ARM resource types should be classified, directly informing `ResourceClassifierPlugin`

#### 5.2 – Define Learning Aggregation Port
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/Ports/ILearningAggregator.cs`
  - `src/Modules/Feedback/Feedback.Application/Ports/ILearningInsightRepository.cs`
- **Acceptance criteria**:
  - `ILearningAggregator`: `AggregateAsync(IReadOnlyCollection<FeedbackEntry> entries, CancellationToken) → IReadOnlyCollection<LearningInsight>`
  - `ILearningInsightRepository`: `AddAsync`, `FindByProjectAndCategoryAsync`, `GetActiveByProjectAsync(Guid projectId)`, `UpdateAsync`
  - Ports live in Application layer

#### 5.3 – Implement SK-based LearningAggregatorPlugin
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/AI/LearningAggregatorPlugin.cs`
- **Implementation notes**:
  - Follow the SK plugin pattern from `ArchitectureAnalysisPlugin` and `ResourceClassifierPlugin`: primary constructor with `Kernel`, invoke prompt, parse structured response
  - Use the same Ollama/SK configuration pattern as `Graph.Api.ServiceCollectionExtensions` (configurable endpoint and model via `IConfiguration`)
  - Register the SK `Kernel` instance in `Feedback.Api.ServiceCollectionExtensions` using `Kernel.CreateBuilder().AddOllamaChatCompletion()`
- **Acceptance criteria**:
  - Uses Semantic Kernel to analyze batches of feedback entries
  - Identifies patterns: recurring corrections, common reclassifications, naming preference trends, resource type mapping corrections
  - Returns structured `LearningInsight` objects with confidence scores
  - Temperature = 0 for deterministic aggregation
  - Prompt includes feedback entries as structured input, asks for pattern extraction
  - Implements `ILearningAggregator` port

#### 5.4 – Create AggregateLearnings Command and Handler
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/AggregateLearnings/AggregateLearningsCommand.cs`
  - `src/Modules/Feedback/Feedback.Application/AggregateLearnings/AggregateLearningsHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/AggregateLearnings/AggregateLearningsResponse.cs`
  - `src/Modules/Feedback/Feedback.Api/Endpoints/AggregateLearningsEndpoint.cs`
- **Acceptance criteria**:
  - Command accepts `ProjectId` and optional `Category` filter
  - Handler fetches recent feedback (not yet aggregated), passes to `ILearningAggregator`
  - Persists resulting `LearningInsight` entities
  - Returns count of new insights generated
  - Endpoint: `POST /api/projects/{projectId}/feedback/aggregate` with `.RequireAuthorization()`

#### 5.5 – Create GetLearnings Query and Handler
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/GetLearnings/GetLearningsQuery.cs`
  - `src/Modules/Feedback/Feedback.Application/GetLearnings/GetLearningsHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/GetLearnings/LearningInsightDto.cs`
  - `src/Modules/Feedback/Feedback.Api/Endpoints/GetLearningsEndpoint.cs`
- **Acceptance criteria**:
  - Query by `ProjectId`, optional `Category` filter
  - Returns only active (non-expired) insights
  - DTO: `Id`, `Category`, `InsightType`, `Description`, `Confidence`, `FeedbackCount`, `CreatedAtUtc`
  - Endpoint: `GET /api/projects/{projectId}/feedback/learnings` with `.RequireAuthorization()`

#### 5.6 – Write Learning Aggregation Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/AggregateLearnings/AggregateLearningsHandlerTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/GetLearnings/GetLearningsHandlerTests.cs`
  - `src/Modules/Feedback/Feedback.Tests/Fakes/InMemoryLearningInsightRepository.cs`
  - `src/Modules/Feedback/Feedback.Tests/Fakes/FakeLearningAggregator.cs`
  - `src/Modules/Feedback/Feedback.Tests/Domain/LearningInsightTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `AggregateLearningsHandlerTests` – `Handle_WithFeedback_CreatesInsights`, `Handle_NoFeedback_ReturnsZeroInsights`, `Handle_CategoryFilter_AggregatesOnlyMatchingFeedback`
  - Unit tests: `GetLearningsHandlerTests` – `Handle_HasActiveInsights_ReturnsInsights`, `Handle_ExpiredInsights_ExcludesFromResults`
  - Unit tests: `LearningInsightTests` – `Aggregate_ValidInput_CreatesInsight`, `IsExpired_PastExpiry_ReturnsTrue`
  - Fakes: `FakeLearningAggregator` returns predetermined insights, `InMemoryLearningInsightRepository`
- **Acceptance criteria**:
  - Handler logic tested with fake aggregator
  - Expiration logic covered
  - Domain invariants tested

---

### Epic 6: AIPromptAugmentation
Goal: Integrate learnings from the feedback loop into all three existing AI plugins so future outputs improve based on user corrections.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Define ILearningProvider cross-module contract | Feature | Shared | S | 5.1 | ⬚ |
| 6.2 | Implement LearningProvider adapter | Feature | Feedback | S | 6.1 | ⬚ |
| 6.3 | Augment ArchitectureAnalysisPlugin with learnings | Feature | Graph | M | 6.2 | ⬚ |
| 6.4 | Augment ThreatDetectionPlugin with learnings | Feature | Graph | M | 6.2 | ⬚ |
| 6.5 | Augment ResourceClassifierPlugin with learnings | Feature | Discovery | M | 6.2 | ⬚ |
| 6.6 | Create FeedbackSubmitted integration event for cross-module notification | Feature | Shared | S | 3.1 | ⬚ |
| 6.7 | Add SignalR notification for learning updates | Feature | Feedback | S | 5.4 | ⬚ |
| 6.8 | Write AI augmentation tests | Test | Graph, Discovery | M | 6.3, 6.4, 6.5 | ⬚ |

#### 6.1 – Define ILearningProvider Cross-Module Contract
- **Files to create**:
  - `src/Shared/Kernel/Contracts/ILearningProvider.cs`
- **Implementation notes**:
  - Follow the same pattern as `ITelemetryQueryService` in `Shared/Kernel/Contracts/` — a simple interface with DTO record
- **Acceptance criteria**:
  - Interface: `GetActiveLearningsAsync(Guid projectId, string category, CancellationToken) → IReadOnlyCollection<LearningDto>`
  - `LearningDto` record: `Description`, `Confidence`, `InsightType`
  - Lives in shared kernel contracts (same location as `ITelemetryQueryService`)
  - No Feedback module types leak into shared kernel beyond DTOs

#### 6.2 – Implement LearningProvider Adapter
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/LearningProvider.cs`
- **Files to modify**:
  - `src/Modules/Feedback/Feedback.Api/ServiceCollectionExtensions.cs` (register `ILearningProvider`)
- **Acceptance criteria**:
  - Implements `ILearningProvider` using `ILearningInsightRepository`
  - Maps domain `LearningInsight` to shared `LearningDto`
  - Registered as scoped service in DI

#### 6.3 – Augment ArchitectureAnalysisPlugin with Learnings
- **Files to modify**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ArchitectureAnalysisPlugin.cs` (add `ILearningProvider` to primary constructor)
  - `src/Modules/Graph/Graph.Api/ServiceCollectionExtensions.cs` (inject `ILearningProvider`)
- **Implementation notes**:
  - Current plugin signature: `ArchitectureAnalysisPlugin(Kernel kernel) : IArchitectureAnalyzer`
  - Updated: `ArchitectureAnalysisPlugin(Kernel kernel, ILearningProvider? learningProvider = null) : IArchitectureAnalyzer`
  - The `AnalyzeAsync` method currently takes `nodesDescription` and `edgesDescription` — add optional `Guid? projectId` parameter to `IArchitectureAnalyzer` for learning lookup (or pass projectId through the handler)
- **Acceptance criteria**:
  - Plugin constructor accepts `ILearningProvider` (optional — gracefully degrades if no learnings)
  - Before analysis, fetches active learnings for the project with category `ArchitectureAnalysis`
  - Learnings are injected into the prompt as "Previous user corrections and preferences" section
  - High-confidence learnings (>0.8) are prefixed with "IMPORTANT:"
  - Prompt structure: system context → learnings → current analysis request
  - If no learnings exist, prompt is unchanged (backward compatible)

#### 6.4 – Augment ThreatDetectionPlugin with Learnings
- **Files to modify**:
  - `src/Modules/Graph/Graph.Infrastructure/AI/ThreatDetectionPlugin.cs` (add `ILearningProvider` to primary constructor)
  - `src/Modules/Graph/Graph.Api/ServiceCollectionExtensions.cs`
- **Implementation notes**:
  - Current plugin signature: `ThreatDetectionPlugin(Kernel kernel) : IThreatDetector`
  - Same augmentation pattern as 6.3
- **Acceptance criteria**:
  - Same pattern as 6.3 but for threat detection
  - Fetches learnings with category `ThreatAssessment`
  - Learnings inform threat prioritization and false-positive reduction

#### 6.5 – Augment ResourceClassifierPlugin with Learnings
- **Files to modify**:
  - `src/Modules/Discovery/Discovery.Infrastructure/AI/ResourceClassifierPlugin.cs` (add `ILearningProvider` to primary constructor)
  - `src/Modules/Discovery/Discovery.Api/ServiceCollectionExtensions.cs` (inject `ILearningProvider`)
- **Implementation notes**:
  - Current plugin signature: `ResourceClassifierPlugin(Kernel kernel) : IResourceClassifier`
  - The plugin currently has a two-tier strategy: known types via `AzureResourceTypeCatalog.Classify()` → fallback to SK prompt. Learnings should augment the SK prompt fallback path to inform classification of unknown resource types
  - Classification corrections from feedback (task 1.4 `ClassificationCorrection`) map directly to this plugin's output format: `FriendlyName`, `ServiceType`, `C4Level`, `IncludeInDiagram`
  - The `ClassifyAsync` method needs project context — currently takes `armResourceType` and `resourceName` only. Either add `Guid? projectId` parameter or pass a pre-fetched learnings collection
- **Acceptance criteria**:
  - Plugin accepts `ILearningProvider` (optional)
  - For unknown resource types (those not in `AzureResourceTypeCatalog`), learnings with category `ResourceClassification` are injected into the SK prompt
  - Learnings can override the default fallback classification for resource types users have previously corrected
  - If no learnings, behavior is unchanged (backward compatible)
  - High-confidence learnings about specific ARM resource types are applied as direct overrides before SK invocation

#### 6.6 – Create FeedbackSubmitted Integration Event
- **Files to create**:
  - `src/Shared/Kernel/IntegrationEvents/FeedbackSubmittedIntegrationEvent.cs`
- **Files to modify**:
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackHandler.cs` (publish integration event via MediatR)
- **Implementation notes**:
  - Follow the exact pattern of `ResourcesDiscoveredIntegrationEvent` and `TelemetryUpdatedIntegrationEvent` — place in `Shared/Kernel/IntegrationEvents/`, implement `INotification`
- **Acceptance criteria**:
  - `FeedbackSubmittedIntegrationEvent` record: `ProjectId`, `FeedbackEntryId` (Guid), `TargetType`, `Category`, `Rating`
  - Published after successful feedback submission via `IMediator.Publish()`
  - Consumers can trigger learning re-aggregation or cache invalidation

#### 6.7 – Add SignalR Notification for Learning Updates
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/IntegrationEventHandlers/LearningsAggregatedHandler.cs`
- **Files to modify**:
  - `src/Modules/Visualization/Visualization.Application/Ports/IDiagramNotifier.cs` (add `NotifyLearningsUpdatedAsync` method)
  - `src/Modules/Visualization/Visualization.Infrastructure/Adapters/SignalRDiagramNotifier.cs` (implement new method)
- **Implementation notes**:
  - Follow the pattern of `TelemetryUpdatedHandler` — listens to `LearningsAggregatedEvent` (or a new `LearningsAggregatedIntegrationEvent`) and notifies connected clients via SignalR
  - Frontend `useSignalR` hook (in `web/src/features/diagram/hooks/useSignalR.ts`) can be extended with an `onLearningsUpdated` callback
- **Acceptance criteria**:
  - When learnings are aggregated, connected clients are notified via SignalR `DiagramHub`
  - Clients can refresh their diagram to benefit from updated AI classifications

#### 6.8 – Write AI Augmentation Tests
- **Files to create**:
  - `src/Modules/Graph/Graph.Tests/Application/ArchitectureAnalysisWithLearningsTests.cs`
  - `src/Modules/Graph/Graph.Tests/Application/ThreatDetectionWithLearningsTests.cs`
  - `src/Modules/Discovery/Discovery.Tests/Application/ResourceClassifierWithLearningsTests.cs`
  - `src/Modules/Graph/Graph.Tests/Fakes/FakeLearningProvider.cs`
  - `src/Modules/Discovery/Discovery.Tests/Fakes/FakeLearningProvider.cs`
- **Test plan (TDD)**:
  - Unit tests: `ArchitectureAnalysisWithLearningsTests` – `AnalyzeAsync_WithLearnings_IncludesLearningsInPrompt`, `AnalyzeAsync_NoLearnings_UsesDefaultPrompt`, `AnalyzeAsync_HighConfidenceLearning_MarkedAsImportant`
  - Unit tests: `ThreatDetectionWithLearningsTests` – `DetectThreatsAsync_WithLearnings_IncludesLearningsInPrompt`, `DetectThreatsAsync_NoLearnings_UsesDefaultPrompt`
  - Unit tests: `ResourceClassifierWithLearningsTests` – `ClassifyAsync_UnknownType_WithLearnings_IncludesLearningsInPrompt`, `ClassifyAsync_KnownType_SkipsLearnings`, `ClassifyAsync_HighConfidenceMapping_OverridesDefault`, `ClassifyAsync_NoLearnings_UsesDefaultBehavior`
  - Fakes: `FakeLearningProvider` returns configurable learning DTOs (one per test project)
- **Acceptance criteria**:
  - Prompt augmentation is verified without calling real LLM
  - Backward compatibility (no learnings = unchanged behavior) is proven
  - High-confidence learning priority is tested
  - ResourceClassifier known-type bypass is preserved (learnings only affect unknown types)

---

### Epic 7: FeedbackFrontend
Goal: Build the React UI for submitting feedback, viewing feedback history, and displaying the eval dashboard, reusing existing shared components and patterns.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 7.1 | Create feedback API layer and types | Feature | Web | S | 3.2, 4.3 | ⬚ |
| 7.2 | Create FeedbackPanel component (inline rating + corrections) | Feature | Web | L | 7.1 | ⬚ |
| 7.3 | Create NodeFeedbackDialog component | Feature | Web | M | 7.1 | ⬚ |
| 7.4 | Integrate feedback into DiagramPage and DiagramCanvas | Feature | Web | M | 7.2, 7.3 | ⬚ |
| 7.5 | Create EvalDashboard page | Feature | Web | L | 7.1 | ⬚ |
| 7.6 | Write frontend tests | Test | Web | M | 7.2, 7.3, 7.4, 7.5 | ⬚ |

#### 7.1 – Create Feedback API Layer and Types
- **Files to create**:
  - `web/src/features/feedback/feedback.types.ts`
  - `web/src/features/feedback/feedback.api.ts`
- **Implementation notes**:
  - Use the existing `getJson`, `postJson` functions from `shared/api/client.ts` — these already handle JWT auth headers via `getAuthHeaders()` and 401 redirects via `handleUnauthorized()`
  - Types should align with backend DTOs and the existing frontend `DiagramNode` type (which has `serviceType`, `level`, `health`, `parentId`)
- **Acceptance criteria**:
  - Types: `FeedbackEntry`, `FeedbackSummary`, `CategoryBreakdown`, `LearningInsight`, `SubmitFeedbackRequest`, `NodeCorrection`, `EdgeCorrection`, `ClassificationCorrection`
  - API functions: `submitFeedback()`, `getFeedbackByProject()`, `getFeedbackSummary()`, `getLearnings()`, `aggregateLearnings()`
  - All use typed API client from `shared/api/client.ts` (`getJson<T>`, `postJson<TReq, TRes>`)
  - No `any` types — all use `unknown` and narrow where needed

#### 7.2 – Create FeedbackPanel Component
- **Files to create**:
  - `web/src/features/feedback/components/FeedbackPanel.tsx`
  - `web/src/features/feedback/components/StarRating.tsx`
  - `web/src/features/feedback/hooks/useSubmitFeedback.ts`
  - `web/src/features/feedback/feedback.css`
- **Implementation notes**:
  - Use `useToast()` hook from `shared/hooks/useToast.ts` and `ToastContainer` from `shared/components/Toast.tsx` for success/error notifications (already used in DashboardPage)
  - Use `useAuth()` hook from `shared/auth/AuthContext.tsx` to get `user.userId` for display purposes (actual auth is handled by API client headers)
  - Style using CSS variables and design system tokens from `styles.css` (e.g., `var(--primary)`, `var(--error)`, `.btn`, `.card`, `.badge`)
- **Acceptance criteria**:
  - Sliding panel that appears when user clicks "Rate this output"
  - 1–5 star rating with visual feedback
  - Category selector (pre-filled based on context)
  - Optional free-text comment field
  - Submit button with loading state (`.btn` class)
  - Success/error toast via `useToast()`
  - Panel can be triggered from diagram canvas, analysis results, or threat assessment views

#### 7.3 – Create NodeFeedbackDialog Component
- **Files to create**:
  - `web/src/features/feedback/components/NodeFeedbackDialog.tsx`
  - `web/src/features/feedback/components/CorrectionForm.tsx`
  - `web/src/features/feedback/hooks/useNodeCorrection.ts`
- **Implementation notes**:
  - Dialog receives a `DiagramNode` (from `features/diagram/types.ts`) which has `id`, `label`, `level`, `serviceType`, `parentId`
  - Correction form should use the `ServiceType` union type (`'app' | 'api' | 'database' | 'queue' | 'cache' | 'external'`) from `features/diagram/types.ts`
  - C4 level options match the existing select in DiagramPage: `'Context' | 'Container' | 'Component'`
- **Acceptance criteria**:
  - Modal dialog triggered from right-click context menu on a diagram node
  - Shows current classification (C4 level, service type, name) from the `DiagramNode` data
  - Allows user to propose corrections: reclassify level, rename, change service type, change parent
  - Submits a `NodeCorrection` alongside the feedback entry
  - Pending corrections shown with visual indicator (`.badge` style) on the node

#### 7.4 – Integrate Feedback into DiagramPage and DiagramCanvas
- **Files to modify**:
  - `web/src/features/diagram/DiagramPage.tsx` (add "Rate Diagram" button in sidebar toolbox, mount FeedbackPanel)
  - `web/src/features/diagram/components/DiagramCanvas.tsx` (add right-click context menu to `ServiceNode`, mount NodeFeedbackDialog)
  - `web/src/features/diagram/hooks/useSignalR.ts` (add `onLearningsUpdated` callback to re-fetch diagram when learnings change)
- **Implementation notes**:
  - `DiagramCanvas.tsx` currently renders `ServiceNode` as a custom ReactFlow node type — add `onContextMenu` handler to `ServiceNode` that opens `NodeFeedbackDialog` with the node's data
  - "Rate Diagram" button goes in the `.toolbox` div in `DiagramPage.tsx` alongside Export buttons
  - Use `useToast()` for feedback confirmation (already available in the component tree)
  - When `onLearningsUpdated` fires via SignalR, trigger a diagram data re-fetch
- **Acceptance criteria**:
  - "Rate Diagram" button in sidebar triggers `FeedbackPanel` for diagram-level feedback
  - Right-click on `ServiceNode` opens `NodeFeedbackDialog`
  - Nodes with pending corrections show a feedback badge (`.badge` class)
  - After AI analysis/threat assessment, a feedback prompt appears
  - Context menu on edges allows relationship correction feedback

#### 7.5 – Create EvalDashboard Page
- **Files to create**:
  - `web/src/features/feedback/EvalDashboardPage.tsx`
  - `web/src/features/feedback/components/FeedbackTrendChart.tsx`
  - `web/src/features/feedback/components/CategoryBreakdownCard.tsx`
  - `web/src/features/feedback/components/TopCorrectionsTable.tsx`
  - `web/src/features/feedback/components/LearningInsightsList.tsx`
  - `web/src/features/feedback/hooks/useFeedbackSummary.ts`
  - `web/src/features/feedback/hooks/useLearnings.ts`
- **Files to modify**:
  - `web/src/App.tsx` (add `<Route path="eval/:projectId" element={<ProtectedRoute><EvalDashboardPage /></ProtectedRoute>} />` inside the Layout route)
  - `web/src/shared/components/Layout.tsx` (add "Eval" nav link alongside existing Dashboard, Diagram, etc.)
- **Implementation notes**:
  - Follow the routing pattern in `App.tsx` which uses `ProtectedRoute` wrapper and layout nesting
  - Use `.card`, `.stat-card`, `.stat-number`, `.stat-label` classes from `styles.css` for dashboard cards (same pattern as `DashboardPage`)
  - "Aggregate Learnings" button follows the `.btn` pattern
  - Use `useToast()` for aggregate learnings success/error feedback
- **Acceptance criteria**:
  - Route: `/eval/:projectId` (follows existing `/diagram/:projectId` pattern)
  - Displays: total feedback count, average rating per category, rating trend chart (last 30 days)
  - Top corrections table: most frequent corrections with frequency and category
  - Active learnings list: AI-generated insights with confidence scores
  - "Aggregate Learnings" button to trigger learning pipeline
  - Responsive layout using existing CSS grid patterns
  - Nav link in sidebar

#### 7.6 – Write Frontend Tests
- **Files to create**:
  - `web/src/features/feedback/components/FeedbackPanel.test.tsx`
  - `web/src/features/feedback/components/StarRating.test.tsx`
  - `web/src/features/feedback/components/NodeFeedbackDialog.test.tsx`
  - `web/src/features/feedback/hooks/useSubmitFeedback.test.tsx`
  - `web/src/features/feedback/hooks/useFeedbackSummary.test.tsx`
- **Implementation notes**:
  - Follow the testing patterns from `useDiagram.test.tsx` and `useSignalR.test.tsx` — mock fetch/API calls, test hook state transitions
- **Test plan (TDD)**:
  - `FeedbackPanel.test.tsx` – renders rating stars, submits feedback on click, shows success toast
  - `StarRating.test.tsx` – renders correct number of stars, highlights on hover, calls onChange
  - `NodeFeedbackDialog.test.tsx` – shows current node values, submits correction, validates required fields
  - `useSubmitFeedback.test.tsx` – handles loading state, handles API error, clears form on success
  - `useFeedbackSummary.test.tsx` – fetches and returns summary data, handles empty state
- **Acceptance criteria**:
  - All components have at least one render test
  - Hooks have API interaction tests
  - No `any` types in test code

---

### Epic 8: EvalMetricsAndObservability
Goal: Add observability to the feedback loop — track AI accuracy improvements, feedback volume, and learning effectiveness.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 8.1 | Create GetEvalMetrics query and handler | Feature | Feedback | M | 2.1, 5.1 | ⬚ |
| 8.2 | Create eval metrics endpoint | Feature | Feedback | S | 8.1 | ⬚ |
| 8.3 | Add feedback telemetry logging | Feature | Feedback | S | 3.1, 5.4 | ⬚ |
| 8.4 | Write eval metrics tests | Test | Feedback | M | 8.1 | ⬚ |

#### 8.1 – Create GetEvalMetrics Query and Handler
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Application/GetEvalMetrics/GetEvalMetricsQuery.cs`
  - `src/Modules/Feedback/Feedback.Application/GetEvalMetrics/GetEvalMetricsHandler.cs`
  - `src/Modules/Feedback/Feedback.Application/GetEvalMetrics/EvalMetricsDto.cs`
- **Files to modify**:
  - `src/Modules/Feedback/Feedback.Application/Ports/IFeedbackEntryRepository.cs` (add metrics query methods)
- **Acceptance criteria**:
  - `EvalMetricsDto`: `OverallAccuracyTrend` (list of weekly avg ratings), `CorrectionRate` (% of feedback with corrections), `LearningEffectiveness` (avg rating before vs after learning applied), `TopImprovementAreas`, `TopDeclineAreas`, `PerPluginBreakdown` (metrics split by ArchitectureAnalysis, ThreatAssessment, ResourceClassification)
  - Compares ratings before and after learnings were generated to measure improvement
  - Returns data suitable for charting

#### 8.2 – Create Eval Metrics Endpoint
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Api/Endpoints/GetEvalMetricsEndpoint.cs`
- **Acceptance criteria**:
  - `GET /api/projects/{projectId}/feedback/eval-metrics`
  - Requires JWT authentication
  - Returns `EvalMetricsDto`

#### 8.3 – Add Feedback Telemetry Logging
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Infrastructure/Telemetry/FeedbackTelemetryLogger.cs`
- **Files to modify**:
  - `src/Modules/Feedback/Feedback.Application/SubmitFeedback/SubmitFeedbackHandler.cs` (add `ILogger<T>`)
  - `src/Modules/Feedback/Feedback.Application/AggregateLearnings/AggregateLearningsHandler.cs` (add `ILogger<T>`)
- **Acceptance criteria**:
  - Logs structured events: `FeedbackSubmitted`, `LearningsAggregated`, `LearningApplied`
  - Each event includes: `ProjectId`, `Category`, `Rating`, `HasCorrection`, timestamp
  - Uses `ILogger<T>` structured logging (no custom telemetry infrastructure)

#### 8.4 – Write Eval Metrics Tests
- **Files to create**:
  - `src/Modules/Feedback/Feedback.Tests/GetEvalMetrics/GetEvalMetricsHandlerTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetEvalMetricsHandlerTests` – `Handle_WithFeedbackData_ReturnsMetrics`, `Handle_NoData_ReturnsEmptyMetrics`, `Handle_WithLearnings_CalculatesEffectiveness`, `Handle_PerPluginBreakdown_SplitsByCategory`
  - Fakes: reuse `InMemoryFeedbackEntryRepository`, `InMemoryLearningInsightRepository`
- **Acceptance criteria**:
  - Accuracy trend calculation is correct
  - Correction rate is calculated correctly
  - Learning effectiveness comparison is tested
  - Per-plugin breakdown is tested

---

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | LLM-based learning aggregation produces low-quality or hallucinated insights | Medium | High | Use Temperature=0, validate insight structure, require minimum feedback count (≥5) before aggregation, add confidence thresholds |
| R2 | Feedback volume is too low to generate meaningful learnings | Medium | Medium | Implement minimum threshold check before aggregation; provide UI nudges to encourage feedback; seed with example feedback |
| R3 | Stale learnings degrade AI quality over time | Low | Medium | Implement expiration on `LearningInsight` (configurable TTL); re-aggregate periodically; allow manual invalidation |
| R4 | Cross-module contract `ILearningProvider` creates tight coupling | Low | Medium | Keep contract minimal (DTO-only); use optional injection pattern (null provider = no learnings); contract lives in shared kernel alongside `ITelemetryQueryService` |
| R5 | Prompt size exceeds LLM context window when many learnings accumulate | Low | High | Limit to top 10 learnings by confidence score; summarize learnings into a compact format; monitor prompt token count |
| R6 | JSON column storage for corrections causes query performance issues at scale | Low | Medium | Add materialized views or denormalized summary tables if query patterns warrant it; monitor query performance |
| R7 | Frontend feedback UI creates friction that slows core diagram workflow | Medium | Medium | Make feedback optional and non-intrusive; use progressive disclosure (simple rating first, details on expand); test UX with users |
| R8 | ResourceClassifierPlugin augmentation requires adding projectId to `IResourceClassifier.ClassifyAsync` — breaking change | Medium | Medium | Use method overload with optional projectId or pass pre-fetched learnings collection; keep original method signature working |
| R9 | Three separate SK Kernel instances (Graph, Discovery, Feedback modules) increase resource usage | Low | Low | Consider sharing a single Kernel registration via shared infrastructure in a future refactor; acceptable for MVP |

### Critical Path
1.1 → 1.2 → 2.1 → 2.2 → 3.1 → 3.2 → 5.1 → 5.2 → 5.3 → 5.4 → 6.1 → 6.2 → 6.5 → 7.1 → 7.4

### Estimated Total Effort
- S tasks: 10 × ~30 min = ~5 h
- M tasks: 18 × ~2.5 h = ~45 h
- L tasks: 3 × ~6 h = ~18 h
- XL tasks: 0
- **Total: ~68 hours**
