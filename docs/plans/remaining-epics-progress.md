# Progress: RemainingEpics
Scope: MVP
Created: 2026-02-25
Last Updated: 2026-02-25
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: Package and Infrastructure Prerequisites
- [ ] 1.1 – Add NuGet packages to Directory.Packages.props and module .csproj files
- [ ] 1.2 – Add SignalR client package to frontend

### Epic 2: Graph Module Completion
- [ ] 2.1 – Create GraphDbContext with EF Core entity configurations
- [ ] 2.2 – Create Graph EF Core repositories
- [ ] 2.3 – Update Graph module registration for EF Core persistence
- [ ] 2.4 – Write Graph persistence integration tests
- [ ] 2.5 – Write Graph handler and acceptance tests

### Epic 3: Visualization Module Completion
- [ ] 3.1 – Create VisualizationDbContext with EF Core entity configurations
- [ ] 3.2 – Create Visualization EF Core repositories
- [ ] 3.3 – Create SignalR DiagramHub for real-time diagram updates
- [ ] 3.4 – Wire SignalR hub into Host and update frontend WebSocket hook
- [ ] 3.5 – Update Visualization module registration
- [ ] 3.6 – Write Visualization integration and acceptance tests

### Epic 4: Discovery Module Persistence
- [ ] 4.1 – Create DiscoveryDbContext with EF Core entity configurations
- [ ] 4.2 – Create Discovery EF Core repositories
- [ ] 4.3 – Update Discovery module registration for EF Core persistence
- [ ] 4.4 – Write Discovery persistence integration tests

### Epic 5: AI Integration with Semantic Kernel + Ollama
- [ ] 5.1 – Configure Semantic Kernel in Host with Ollama (local LLM)
- [ ] 5.2 – Create SK logging filters (IPromptRenderFilter, IFunctionInvocationFilter)
- [ ] 5.3 – Create ArchitectureAnalysis SK plugin
- [ ] 5.4 – Create BasicThreatDetection SK plugin (STRIDE-based risk scoring)
- [ ] 5.5 – Create AnalyzeArchitecture vertical slice
- [ ] 5.6 – Create GetThreatAssessment vertical slice
- [ ] 5.7 – Write AI integration tests

### Epic 6: Database Migrations and Seed Data
- [ ] 6.1 – Create Identity module EF Core migrations
- [ ] 6.2 – Create Telemetry module EF Core migrations
- [ ] 6.3 – Create Graph module EF Core migrations
- [ ] 6.4 – Create Discovery module EF Core migrations
- [ ] 6.5 – Create Visualization module EF Core migrations
- [ ] 6.6 – Create seed data service and demo mode
- [ ] 6.7 – Wire auto-migration and seeding into Host startup

### Epic 7: End-to-End Testing and Quality Assurance
- [ ] 7.1 – Create shared test infrastructure (WebApplicationFactory + Testcontainers)
- [ ] 7.2 – Write Identity module acceptance tests
- [ ] 7.3 – Write Discovery module acceptance tests
- [ ] 7.4 – Write Graph module acceptance tests
- [ ] 7.5 – Write Telemetry module acceptance tests
- [ ] 7.6 – Write Visualization module acceptance tests
- [ ] 7.7 – Write cross-module E2E scenario tests
- [ ] 7.8 – Create architecture boundary tests (ArchUnitNET)
- [ ] 7.9 – Docker Compose smoke test and health verification
- [ ] 7.10 – OpenAPI/Swagger polish
- [ ] 7.11 – Expand frontend test coverage

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-25 | Initial plan created | – | – |
| 2026-02-25 | Added Epic 4 (Discovery Persistence) as new gap | Discovery module only had in-memory repositories; needs EF Core like Identity and Telemetry | +4 tasks, ~7h |
| 2026-02-25 | Added task 7.11 (Frontend Tests) | Only 1 frontend test exists; need broader coverage | +1 task, ~2.5h |
| 2026-02-25 | Risk R8 added for .NET 9 SDK availability | .NET 9 SDK not in apt repos; required manual install via dotnet-install.sh | Already mitigated |
| 2026-02-25 | Switched AI backend from Azure OpenAI to Ollama | User requested local models; no API key dependency; added Ollama Docker service | Adds R9-R11 risks, adds ollama service to docker-compose |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-25 | Keep in-memory repository implementations as test fakes rather than deleting | Existing in-memory repos in Api projects are well-written and useful for unit testing; move to test projects instead of reimplementing |
| 2026-02-25 | Use EF Core InMemory provider as fallback for integration tests when Docker unavailable | Testcontainers requires Docker daemon; not all environments have it |
| 2026-02-25 | Create a shared Testing project for WebApplicationFactory and Testcontainers fixtures | Avoids duplicating test infrastructure across 5+ test projects |
| 2026-02-25 | Separate migration history tables per module | Multiple DbContexts sharing one history table causes conflicts; use `__EFMigrationsHistory_<Module>` convention |
| 2026-02-25 | AI plugins use FakeChatCompletionService in tests | Real LLM calls are non-deterministic, slow, and require API keys; all AI tests use canned responses |
| 2026-02-25 | Use Ollama with local models instead of Azure OpenAI | No external API keys needed; models run locally; Docker Compose manages Ollama lifecycle; llama3.1 for chat, nomic-embed-text for embeddings |
| 2026-02-25 | Use Microsoft.SemanticKernel.Connectors.Ollama (pre-release) | Native SK connector; uses OllamaSharp under the hood; wraps behind ports so connector can be swapped later if needed |
| 2026-02-25 | Docker volume for Ollama model persistence | Models are large (4-8 GB); volume prevents re-downloading on container restart; init container handles pull |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
