## Plan: SecurityRemediation
Scope: Remediation
Created: 2026-03-01
Status: Draft

### Overview
Remediate 15 security vulnerabilities discovered during a penetration test, ranging from critical (JWT auth bypass, IDOR, privilege escalation) to medium (prompt injection, OAuth CSRF). The initiative prioritizes fixes by severity: IMMEDIATE for exploitable auth/authz bypasses, HIGH for injection and infrastructure hardening, MEDIUM for AI safety and token encryption.

### Success Criteria
- [ ] No hardcoded secrets in source code or checked-in configuration
- [ ] Every handler that accesses project-scoped data verifies user membership
- [ ] Role-based access control enforced via JWT claims and authorization policies
- [ ] SignalR hub requires authentication and validates project membership
- [ ] All user-controlled URLs validated against allowlist before server-side requests
- [ ] No command injection vectors via Process.Start
- [ ] Rate limiting applied to all public endpoints
- [ ] Azure tokens encrypted at rest in the database
- [ ] OAuth state parameter validated on code exchange
- [ ] LLM prompt inputs sanitized to prevent prompt injection
- [ ] All existing tests continue to pass after remediation
- [ ] New tests cover every security fix

### Epic 1: Authentication & Secrets Hardening
Goal: Eliminate all hardcoded secrets and add role claims to JWT tokens

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Remove hardcoded JWT signing key fallback and require configuration | Refactor | Identity, Host | S | – | ⬚ |
| 1.2 | Move DB credentials to environment variables | Infrastructure | Host | S | – | ⬚ |
| 1.3 | Add role claims to JWT token generation | Feature | Identity | M | – | ⬚ |
| 1.4 | Tests for JWT and secrets changes | Test | Identity | M | 1.1, 1.3 | ⬚ |

#### 1.1 – Remove Hardcoded JWT Signing Key
- **Files to modify**: `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs`, `src/Host/Program.cs`, `src/Host/appsettings.json`
- **Test plan (TDD)**:
  - Unit tests: `JwtTokenServiceTests` – `GenerateToken_MissingSigningKeyConfig_ThrowsInvalidOperationException`
  - Fakes/Fixtures needed: In-memory `IConfiguration` with missing key
- **Acceptance criteria**:
  - JwtTokenService throws at startup if `Jwt:SigningKey` is not configured
  - Program.cs JWT bearer config throws at startup if key is missing
  - appsettings.json contains only a placeholder comment referencing env vars
  - No hardcoded key fallback (`?? "..."`) in any file

#### 1.2 – Move DB Credentials to Environment Variables
- **Files to modify**: `src/Host/appsettings.json`, `docker-compose.yml` (if exists)
- **Test plan (TDD)**:
  - No unit tests needed – infrastructure configuration change
  - Manual verification: app starts with env vars, fails without them
- **Acceptance criteria**:
  - appsettings.json connection strings use placeholder format (`Host=postgres;...;Password=`) or are removed entirely
  - README or docker-compose references env vars for credentials

#### 1.3 – Add Role Claims to JWT Token Generation
- **Files to modify**: `src/Modules/Identity/Identity.Infrastructure/Security/JwtTokenService.cs`, `src/Modules/Identity/Identity.Application/Ports/ITokenService.cs`, `src/Modules/Identity/Identity.Application/LoginUser/LoginUserHandler.cs`
- **Files to create**: None (modify existing interface and implementation)
- **Test plan (TDD)**:
  - Unit tests: `JwtTokenServiceTests` – `GenerateToken_UserWithOwnerRole_IncludesRoleClaim`, `GenerateToken_UserWithMultipleProjectRoles_IncludesAllRoleClaims`
  - Fakes/Fixtures needed: In-memory `IConfiguration` with valid JWT config
- **Acceptance criteria**:
  - JWT tokens include project-role claims (e.g., `project_role:{projectId}:{role}`)
  - LoginUserHandler queries member roles and passes them to token generation
  - Existing login flow still works end-to-end

#### 1.4 – Tests for JWT and Secrets Changes
- **Files to create**: `src/Modules/Identity/Identity.Tests/Security/JwtTokenServiceTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `JwtTokenServiceTests` – all test cases from 1.1 and 1.3 above
  - Module tests: Verify login endpoint returns token with role claims
- **Acceptance criteria**:
  - All new tests pass
  - Existing Identity tests still pass

---

### Epic 2: Authorization Infrastructure
Goal: Build a reusable project-level authorization service that all modules can use

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Create ICurrentUserService port and implementation | Feature | Shared, Host | S | – | ⬚ |
| 2.2 | Create IProjectAuthorizationService port | Feature | Shared | S | 2.1 | ⬚ |
| 2.3 | Implement ProjectAuthorizationService adapter | Feature | Identity | M | 2.2 | ⬚ |
| 2.4 | Add GetByProjectAndUserAsync to IMemberRepository | Feature | Identity | S | – | ⬚ |
| 2.5 | Tests for authorization infrastructure | Test | Identity | M | 2.3, 2.4 | ⬚ |

#### 2.1 – Create ICurrentUserService Port and Implementation
- **Files to create**: `src/Shared/Kernel/ICurrentUserService.cs`, `src/Shared/Infrastructure/Security/HttpContextCurrentUserService.cs`
- **Test plan (TDD)**:
  - Unit tests: `HttpContextCurrentUserServiceTests` – `GetUserId_ValidJwt_ReturnsUserId`, `GetUserId_NoAuth_ThrowsUnauthorized`
- **Acceptance criteria**:
  - ICurrentUserService exposes `UserId` (Guid) and `Email` (string) extracted from JWT claims
  - Registered as scoped service in DI
  - Works with the existing JWT sub claim format

#### 2.2 – Create IProjectAuthorizationService Port
- **Files to create**: `src/Shared/Kernel/IProjectAuthorizationService.cs`
- **Test plan (TDD)**:
  - No unit tests – interface definition only
- **Acceptance criteria**:
  - Interface defines `AuthorizeAsync(Guid projectId, CancellationToken ct)` returning `Result<Unit>`
  - Interface defines `AuthorizeOwnerAsync(Guid projectId, CancellationToken ct)` returning `Result<Unit>`
  - Lives in Shared.Kernel so all modules can reference it

#### 2.3 – Implement ProjectAuthorizationService Adapter
- **Files to create**: `src/Modules/Identity/Identity.Infrastructure/Security/ProjectAuthorizationService.cs`
- **Files to modify**: Identity module DI registration
- **Test plan (TDD)**:
  - Unit tests: `ProjectAuthorizationServiceTests` – `Authorize_UserIsMember_ReturnsSuccess`, `Authorize_UserIsNotMember_ReturnsFailure`, `AuthorizeOwner_UserIsOwner_ReturnsSuccess`, `AuthorizeOwner_UserIsContributor_ReturnsFailure`
  - Fakes/Fixtures needed: `InMemoryMemberRepository` extended with `GetByProjectAndUserAsync`
- **Acceptance criteria**:
  - Service queries member repository to verify user's membership in the project
  - Owner-level checks verify the member has Role.Owner
  - Returns typed error results, not exceptions

#### 2.4 – Add GetByProjectAndUserAsync to IMemberRepository
- **Files to modify**: `src/Modules/Identity/Identity.Application/Ports/IMemberRepository.cs`, `src/Modules/Identity/Identity.Infrastructure/Persistence/MemberRepository.cs`
- **Test plan (TDD)**:
  - Unit tests: `MemberRepositoryTests` (if integration tests exist) – `GetByProjectAndUser_Exists_ReturnsMember`, `GetByProjectAndUser_NotExists_ReturnsNull`
- **Acceptance criteria**:
  - New method: `Task<Member?> GetByProjectAndUserAsync(ProjectId, string externalUserId, CancellationToken)`
  - EF Core implementation queries by both project ID and external user ID

#### 2.5 – Tests for Authorization Infrastructure
- **Files to create**: `src/Modules/Identity/Identity.Tests/Security/ProjectAuthorizationServiceTests.cs`, `src/Modules/Identity/Identity.Tests/Security/HttpContextCurrentUserServiceTests.cs`
- **Test plan (TDD)**:
  - All test cases from 2.1 and 2.3
- **Acceptance criteria**:
  - Full coverage of auth service happy and error paths
  - All existing tests still pass

---

### Epic 3: Project-Level Authorization Enforcement
Goal: Add project membership verification to every handler that accesses project-scoped data

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 3.1 | Secure SignalR DiagramHub with auth and membership check | Feature | Visualization, Host | S | 2.3 | ⬚ |
| 3.2 | Fix InviteMemberHandler – require Owner role | Feature | Identity | S | 2.3 | ⬚ |
| 3.3 | Fix UpdateMemberRoleHandler – require Owner role | Feature | Identity | S | 2.3 | ⬚ |
| 3.4 | Fix GetOrganizationHandler – filter by user membership | Feature | Identity | S | 2.1 | ⬚ |
| 3.5 | Add authorization to Graph module handlers | Feature | Graph | M | 2.3 | ⬚ |
| 3.6 | Add authorization to Discovery module handlers | Feature | Discovery | M | 2.3 | ⬚ |
| 3.7 | Add authorization to Visualization module handlers | Feature | Visualization | S | 2.3 | ⬚ |
| 3.8 | Add authorization to Telemetry module handlers | Feature | Telemetry | S | 2.3 | ⬚ |
| 3.9 | Add authorization to Feedback module handlers | Feature | Feedback | S | 2.3 | ⬚ |
| 3.10 | Tests for all authorization enforcement | Test | All | L | 3.1–3.9 | ⬚ |

#### 3.1 – Secure SignalR DiagramHub
- **Files to modify**: `src/Host/Program.cs` (line 104), `src/Modules/Visualization/Visualization.Api/Hubs/DiagramHub.cs`
- **Test plan (TDD)**:
  - Unit tests: `DiagramHubTests` – `JoinProject_UnauthenticatedUser_ThrowsUnauthorized`, `JoinProject_NonMember_ThrowsUnauthorized`, `JoinProject_ValidMember_JoinsGroup`
- **Acceptance criteria**:
  - `MapHub<DiagramHub>` includes `.RequireAuthorization()`
  - `JoinProject` validates user is a member of the project before adding to group
  - Unauthenticated connections are rejected

#### 3.2 – Fix InviteMemberHandler
- **Files to modify**: `src/Modules/Identity/Identity.Application/InviteMember/InviteMemberHandler.cs`, `src/Modules/Identity/Identity.Application/InviteMember/InviteMemberCommand.cs`
- **Test plan (TDD)**:
  - Unit tests: `InviteMemberHandlerTests` – `Handle_NonOwnerInvites_ReturnsUnauthorized`, `Handle_OwnerInvites_Succeeds`, `Handle_SelfInviteAsOwner_ReturnsError`
  - Fakes/Fixtures needed: Updated `InMemoryMemberRepository`
- **Acceptance criteria**:
  - Only project Owners can invite new members
  - Users cannot invite themselves
  - Existing invite flow for Owners continues to work

#### 3.3 – Fix UpdateMemberRoleHandler
- **Files to modify**: `src/Modules/Identity/Identity.Application/UpdateMemberRole/UpdateMemberRoleHandler.cs`, `src/Modules/Identity/Identity.Application/UpdateMemberRole/UpdateMemberRoleCommand.cs`
- **Test plan (TDD)**:
  - Unit tests: `UpdateMemberRoleHandlerTests` – `Handle_NonOwnerUpdatesRole_ReturnsUnauthorized`, `Handle_OwnerUpdatesRole_Succeeds`
- **Acceptance criteria**:
  - Only project Owners can change member roles
  - Cannot demote last Owner (existing logic preserved)

#### 3.4 – Fix GetOrganizationHandler
- **Files to modify**: `src/Modules/Identity/Identity.Application/GetOrganization/GetOrganizationHandler.cs`, `src/Modules/Identity/Identity.Application/GetOrganization/GetOrganizationQuery.cs`, `src/Modules/Identity/Identity.Application/Ports/IOrganizationRepository.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetOrganizationHandlerTests` – `Handle_UserBelongsToOrg_ReturnsOrg`, `Handle_UserNotInAnyOrg_ReturnsNotFound`
- **Acceptance criteria**:
  - Handler uses current user's ID to find their organization
  - Only returns projects the user is a member of
  - Users cannot see other organizations' data

#### 3.5 – Add Authorization to Graph Module Handlers
- **Files to modify**: `src/Modules/Graph/Graph.Application/GetGraph/GetGraphHandler.cs`, `src/Modules/Graph/Graph.Application/ResetGraph/ResetGraphHandler.cs`, `src/Modules/Graph/Graph.Application/GetGraphDiff/GetGraphDiffHandler.cs`, `src/Modules/Graph/Graph.Application/GetThreatAssessment/GetThreatAssessmentHandler.cs`, `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `GetGraphHandlerTests` – `Handle_NonMember_ReturnsUnauthorized`; similar for each handler
  - Fakes/Fixtures needed: Fake `IProjectAuthorizationService`
- **Acceptance criteria**:
  - Every Graph handler calls `IProjectAuthorizationService.AuthorizeAsync` before accessing data
  - Non-members receive a clear authorization error

#### 3.6 – Add Authorization to Discovery Module Handlers
- **Files to modify**: All Discovery handlers that accept a project ID (DiscoverResources, GetDiscoveryStatus, GetSubscription, ConnectAzureSubscription, DisconnectSubscription, DetectDrift, AddMcpServer, DeleteMcpServer, ListMcpServers)
- **Test plan (TDD)**:
  - Unit tests: One representative test per handler – `Handle_NonMember_ReturnsUnauthorized`
  - Fakes/Fixtures needed: Fake `IProjectAuthorizationService`
- **Acceptance criteria**:
  - Every project-scoped Discovery handler checks membership
  - Non-members cannot trigger discovery or manage subscriptions

#### 3.7 – Add Authorization to Visualization Module Handlers
- **Files to modify**: All Visualization handlers (GetDiagram, SaveViewPreset, ExportDiagram, GetViewPresets)
- **Test plan (TDD)**:
  - Unit tests: `GetDiagramHandlerTests` – `Handle_NonMember_ReturnsUnauthorized`
- **Acceptance criteria**:
  - Visualization data only accessible to project members

#### 3.8 – Add Authorization to Telemetry Module Handlers
- **Files to modify**: All Telemetry handlers (SyncApplicationInsightsTelemetry, GetServiceHealth, IngestTelemetry)
- **Test plan (TDD)**:
  - Unit tests: `GetServiceHealthHandlerTests` – `Handle_NonMember_ReturnsUnauthorized`
- **Acceptance criteria**:
  - Telemetry data only accessible to project members

#### 3.9 – Add Authorization to Feedback Module Handlers
- **Files to modify**: All Feedback handlers (SubmitFeedback, GetFeedbackSummary, GetLearnings, GetFeedbackByProject, GetEvalMetrics, AggregateLearnings)
- **Test plan (TDD)**:
  - Unit tests: `SubmitFeedbackHandlerTests` – `Handle_NonMember_ReturnsUnauthorized`
- **Acceptance criteria**:
  - Feedback data only accessible to project members

#### 3.10 – Tests for All Authorization Enforcement
- **Files to create/modify**: Test files in each module's test project
- **Test plan (TDD)**:
  - All test cases from 3.1–3.9 above consolidated
  - Integration test: Verify end-to-end that an authenticated user without project membership gets 403
- **Acceptance criteria**:
  - Every handler has at least one authorization test
  - All existing tests updated with proper authorization context

---

### Epic 4: Input Validation & Injection Prevention
Goal: Eliminate SSRF and command injection attack vectors

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 4.1 | Add URL validation for MCP server endpoints (SSRF prevention) | Feature | Discovery | M | – | ⬚ |
| 4.2 | Sanitize git repo URLs to prevent command injection | Feature | Discovery | M | – | ⬚ |
| 4.3 | Tests for input validation | Test | Discovery | M | 4.1, 4.2 | ⬚ |

#### 4.1 – Add URL Validation for MCP Server Endpoints
- **Files to modify**: `src/Modules/Discovery/Discovery.Api/Adapters/RemoteMcpDiscoverySourceAdapter.cs`
- **Files to create**: `src/Shared/Kernel/UrlValidator.cs` (or in Discovery application layer)
- **Test plan (TDD)**:
  - Unit tests: `UrlValidatorTests` – `Validate_PrivateIp_ReturnsFalse`, `Validate_MetadataEndpoint_ReturnsFalse`, `Validate_ValidHttpsUrl_ReturnsTrue`, `Validate_HttpUrl_ReturnsFalse`, `Validate_LocalhostUrl_ReturnsFalse`
- **Acceptance criteria**:
  - Only HTTPS URLs accepted for MCP endpoints (except in Development environment)
  - Private IP ranges blocked (127.0.0.0/8, 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 169.254.0.0/16)
  - Cloud metadata endpoints blocked (169.254.169.254)
  - URL resolved to IP and validated before making request

#### 4.2 – Sanitize Git Repo URLs
- **Files to modify**: `src/Modules/Discovery/Discovery.Api/Adapters/RepositoryIacDiscoverySourceAdapter.cs`
- **Test plan (TDD)**:
  - Unit tests: `GitUrlSanitizerTests` – `Sanitize_ValidHttpsGitUrl_ReturnsUrl`, `Sanitize_UrlWithShellChars_ThrowsValidation`, `Sanitize_UrlWithArgInjection_ThrowsValidation`, `Sanitize_SshUrl_ThrowsValidation`
- **Acceptance criteria**:
  - Git URLs validated against strict HTTPS pattern before use
  - Shell metacharacters rejected (`;`, `|`, `&`, `` ` ``, `$()`, etc.)
  - Git argument injection prevented (URLs starting with `-` rejected)
  - Process.Start arguments properly escaped or split

#### 4.3 – Tests for Input Validation
- **Files to create**: `src/Modules/Discovery/Discovery.Tests/Adapters/UrlValidatorTests.cs`, `src/Modules/Discovery/Discovery.Tests/Adapters/GitUrlSanitizerTests.cs`
- **Test plan (TDD)**:
  - All test cases from 4.1 and 4.2
- **Acceptance criteria**:
  - Full coverage of validation edge cases
  - Both valid and malicious inputs tested

---

### Epic 5: Infrastructure Security Hardening
Goal: Add rate limiting and encrypt sensitive data at rest

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 5.1 | Add rate limiting middleware | Infrastructure | Host | M | – | ⬚ |
| 5.2 | Encrypt Azure tokens at rest | Feature | Discovery | M | – | ⬚ |
| 5.3 | Tests for infrastructure security | Test | Discovery, Host | M | 5.1, 5.2 | ⬚ |

#### 5.1 – Add Rate Limiting Middleware
- **Files to modify**: `src/Host/Program.cs`
- **Test plan (TDD)**:
  - Module tests: `RateLimitingTests` – `Endpoint_ExceedsRateLimit_Returns429`, `Endpoint_WithinRateLimit_Returns200`
- **Acceptance criteria**:
  - `AddRateLimiter` / `UseRateLimiter` configured in Program.cs
  - Fixed window rate limiter: 100 requests per minute per IP for authenticated endpoints
  - Stricter limit for auth endpoints: 10 requests per minute per IP (login, register)
  - 429 Too Many Requests response with Retry-After header

#### 5.2 – Encrypt Azure Tokens at Rest
- **Files to modify**: `src/Modules/Discovery/Discovery.Api/Adapters/DatabaseAzureTokenStore.cs`
- **Files to create**: `src/Shared/Infrastructure/Security/IDataProtectionService.cs`, `src/Shared/Infrastructure/Security/AesDataProtectionService.cs`
- **Test plan (TDD)**:
  - Unit tests: `AesDataProtectionServiceTests` – `Protect_PlainText_ReturnsEncrypted`, `Unprotect_EncryptedText_ReturnsOriginal`, `Unprotect_TamperedText_ThrowsException`
  - Unit tests: `DatabaseAzureTokenStoreTests` – `SaveToken_EncryptsBeforeStorage`, `GetToken_DecryptsAfterRetrieval`
- **Acceptance criteria**:
  - Access tokens and refresh tokens encrypted using ASP.NET Core Data Protection or AES-256
  - Encryption key sourced from configuration (env var / secrets manager)
  - Existing tokens migration plan documented (can be lazy-migrated on read)

#### 5.3 – Tests for Infrastructure Security
- **Files to create**: `src/Modules/Discovery/Discovery.Tests/Adapters/DatabaseAzureTokenStoreTests.cs`
- **Test plan (TDD)**:
  - All test cases from 5.1 and 5.2
- **Acceptance criteria**:
  - Rate limiting behavior verified
  - Token encryption round-trip verified

---

### Epic 6: OAuth & AI Security
Goal: Fix OAuth CSRF and mitigate LLM prompt injection risks

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 6.1 | Validate OAuth state parameter in Azure code exchange | Feature | Discovery | M | – | ⬚ |
| 6.2 | Sanitize LLM prompt inputs in LearningAggregatorPlugin | Feature | Feedback | S | – | ⬚ |
| 6.3 | Sanitize LLM prompt inputs in AnalyzeArchitectureHandler | Feature | Graph | S | – | ⬚ |
| 6.4 | Tests for OAuth and AI security | Test | Discovery, Feedback, Graph | M | 6.1, 6.2, 6.3 | ⬚ |

#### 6.1 – Validate OAuth State Parameter
- **Files to modify**: `src/Modules/Discovery/Discovery.Api/Endpoints/ExchangeAzureCodeEndpoint.cs`, `src/Modules/Discovery/Discovery.Api/Endpoints/GetAzureAuthUrlEndpoint.cs` (to generate state)
- **Files to create**: `src/Modules/Discovery/Discovery.Application/Ports/IOAuthStateStore.cs`, `src/Modules/Discovery/Discovery.Infrastructure/Security/OAuthStateStore.cs`
- **Test plan (TDD)**:
  - Unit tests: `ExchangeAzureCodeHandlerTests` – `Handle_MissingState_ReturnsError`, `Handle_InvalidState_ReturnsError`, `Handle_ValidState_ExchangesCode`
- **Acceptance criteria**:
  - Auth URL endpoint generates cryptographic random state and stores it (cache or DB with TTL)
  - Exchange endpoint validates state parameter matches stored value
  - State is single-use (deleted after validation)
  - Stale states expire after 10 minutes

#### 6.2 – Sanitize LLM Prompt Inputs in LearningAggregatorPlugin
- **Files to modify**: `src/Modules/Feedback/Feedback.Infrastructure/AI/LearningAggregatorPlugin.cs`
- **Test plan (TDD)**:
  - Unit tests: `LearningAggregatorPluginTests` – `BuildFeedbackDescription_CommentWithPromptInjection_SanitizedOutput`, `BuildFeedbackDescription_NormalComment_PassesThrough`
- **Acceptance criteria**:
  - User-supplied feedback comments JSON-encoded before embedding in prompts
  - Maximum length enforced on individual feedback entries (e.g., 500 chars)
  - Prompt structure uses clear delimiters between system instructions and user data

#### 6.3 – Sanitize LLM Prompt Inputs in AnalyzeArchitectureHandler
- **Files to modify**: `src/Modules/Graph/Graph.Application/AnalyzeArchitecture/AnalyzeArchitectureHandler.cs`
- **Test plan (TDD)**:
  - Unit tests: `AnalyzeArchitectureHandlerTests` – `Handle_NodeNameWithPromptInjection_SanitizedBeforeAnalysis`
- **Acceptance criteria**:
  - Node names and edge descriptions JSON-encoded before embedding in prompts
  - Structured data (JSON arrays) passed to analyzer instead of concatenated strings
  - Maximum length enforced on individual node names

#### 6.4 – Tests for OAuth and AI Security
- **Files to create/modify**: Test files in Discovery, Feedback, and Graph test projects
- **Test plan (TDD)**:
  - All test cases from 6.1, 6.2, 6.3
- **Acceptance criteria**:
  - OAuth state validation fully tested
  - Prompt injection sanitization edge cases covered

---

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Authorization service creates cross-module dependency | Medium | High | Place port (IProjectAuthorizationService) in Shared.Kernel; implementation in Identity module; wire via DI |
| R2 | Adding auth checks to all handlers breaks existing tests | High | Medium | Update each module's test fakes to include authorized user context; batch update test setup |
| R3 | Token encryption migration breaks existing stored tokens | Medium | High | Implement lazy migration: detect unencrypted tokens on read, encrypt and save back |
| R4 | Rate limiting blocks legitimate automated workflows | Low | Medium | Configure generous limits; add API key bypass for known internal services |
| R5 | JWT role claims bloat token size for users with many project roles | Low | Low | Include only active project roles or use reference tokens for users with 10+ projects |
| R6 | OAuth state store adds infrastructure dependency (cache/DB) | Low | Low | Use ASP.NET Core distributed cache with in-memory fallback for development |

### Critical Path
1.1 → 1.3 → 2.1 → 2.2 → 2.3 → 2.4 → 2.5 → 3.5 → 3.10

The authorization infrastructure (Epic 2) is the bottleneck – all per-module authorization tasks (Epic 3) depend on it. Epics 4, 5, and 6 are independent and can proceed in parallel with Epic 3.

### Parallel Execution Opportunities
- **Epic 1** (secrets) and **Epic 4** (input validation) can start immediately in parallel
- **Epic 5** (rate limiting, encryption) can start immediately in parallel
- **Epic 6** (OAuth, AI safety) can start immediately in parallel
- **Epic 3** tasks 3.1–3.9 can all proceed in parallel once Epic 2 is complete

### Estimated Total Effort
- S tasks: 10 × ~30 min = ~5 h
- M tasks: 12 × ~2.5 h = ~30 h
- L tasks: 1 × ~6 h = ~6 h
- XL tasks: 0
- **Total: ~41 hours**
