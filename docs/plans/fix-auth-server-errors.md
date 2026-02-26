## Plan: Fix Auth Server Errors
Scope: Bugfix
Created: 2026-02-26
Status: Draft

### Overview
The `/api/auth/login` and `/api/auth/register` endpoints return HTTP 500 because the `users` table is missing from the EF Core migration. The `InitialCreate` migration was generated before the `User` entity was added to `IdentityDbContext`, so the `users` table was never created in PostgreSQL. Any query to `dbContext.Users` throws an unhandled Npgsql exception (`relation "users" does not exist`) caught by `GlobalExceptionHandler`. Additionally, there are no unit tests for the auth handlers, so this regression went undetected.

### Success Criteria
- [ ] `POST /api/auth/register` returns 201 with a valid JWT when given valid credentials
- [ ] `POST /api/auth/login` returns 200 with a valid JWT for existing users
- [ ] The `users` table is created in PostgreSQL when migrations are applied
- [ ] Unit tests exist for both `RegisterUserHandler` and `LoginUserHandler`
- [ ] All existing tests continue to pass
- [ ] Solution compiles without errors

### Epic 1: Database Migration Fix
Goal: Add the missing `users` table to the EF Core migration so auth endpoints can persist and query users.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | Add EF Core migration for `users` table | Infrastructure | Identity | M | – | ⬚ |

#### 1.1 – Add EF Core migration for `users` table
- **Files to create**: `src/Modules/Identity/Identity.Infrastructure/Persistence/Migrations/<timestamp>_AddUsersTable.cs`, `src/Modules/Identity/Identity.Infrastructure/Persistence/Migrations/<timestamp>_AddUsersTable.Designer.cs`
- **Files to modify**: `src/Modules/Identity/Identity.Infrastructure/Persistence/Migrations/IdentityDbContextModelSnapshot.cs`
- **Details**:
  - The migration `Up()` must create the `users` table with columns: `Id` (uuid PK), `Email` (varchar(256) NOT NULL, unique index), `PasswordHash` (text NOT NULL), `DisplayName` (varchar(150) NOT NULL)
  - The migration `Down()` must drop the `users` table
  - The `IdentityDbContextModelSnapshot` must be updated to include the `User` entity so future migrations are generated correctly
  - The `SeedDataService.SeedIdentityAsync` already seeds a demo user — it will work once the table exists
- **Test plan (TDD)**:
  - Verify migration file creates correct schema by inspecting the generated code
  - Integration validation: the `SeedDataService` seeds a user without exceptions after migration runs
- **Acceptance criteria**:
  - Migration file exists and creates `users` table with all columns matching `UserConfiguration`
  - `IdentityDbContextModelSnapshot.cs` includes the `User` entity
  - Migration `Down()` cleanly drops the `users` table

### Epic 2: Auth Handler Unit Tests
Goal: Add comprehensive unit tests for `RegisterUserHandler` and `LoginUserHandler` to prevent future regressions and validate handler behavior.

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 2.1 | Write unit tests for `RegisterUserHandler` | Test | Identity | M | – | ⬚ |
| 2.2 | Write unit tests for `LoginUserHandler` | Test | Identity | M | – | ⬚ |

#### 2.1 – Write unit tests for `RegisterUserHandler`
- **Files to create**: `src/Modules/Identity/Identity.Tests/RegisterUserHandlerTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `RegisterUserHandlerTests`
    - `Handle_ValidCommand_CreatesUserAndReturnsToken` — happy path: new user is created, token is returned
    - `Handle_DuplicateEmail_ReturnsError` — user with same email already exists, returns `identity.user.duplicate_email` error
    - `Handle_ValidCommand_HashesPassword` — verifies `IPasswordHasher.Hash()` is called
    - `Handle_ValidCommand_SavesChanges` — verifies `IUnitOfWork.SaveChangesAsync()` is called
  - Fakes/Fixtures needed: `FakeUserRepository`, `FakePasswordHasher`, `FakeTokenService`, `FakeUnitOfWork`
- **Acceptance criteria**:
  - All test cases pass
  - Tests use in-memory fakes (no database dependency)
  - Tests follow project conventions: `Method_Scenario_ExpectedOutcome`, Arrange/Act/Assert with blank-line separation

#### 2.2 – Write unit tests for `LoginUserHandler`
- **Files to create**: `src/Modules/Identity/Identity.Tests/LoginUserHandlerTests.cs`
- **Test plan (TDD)**:
  - Unit tests: `LoginUserHandlerTests`
    - `Handle_ValidCredentials_ReturnsToken` — happy path: existing user, correct password, returns token
    - `Handle_UnknownEmail_ReturnsInvalidCredentialsError` — email not found, returns `identity.user.invalid_credentials` error
    - `Handle_WrongPassword_ReturnsInvalidCredentialsError` — email exists but password hash doesn't match, returns `identity.user.invalid_credentials` error
  - Fakes/Fixtures needed: reuses `FakeUserRepository`, `FakePasswordHasher`, `FakeTokenService` from 2.1
- **Acceptance criteria**:
  - All test cases pass
  - Tests use in-memory fakes (no database dependency)
  - Tests follow project conventions

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | Hand-written migration diverges from what EF Core would generate | Medium | Medium | Match the exact patterns from the existing `InitialCreate` migration; include Designer and Snapshot updates |
| R2 | `dotnet ef` tools not available in this environment for auto-generation | High | Low | Write migration files manually following the established EF Core patterns in the codebase |
| R3 | InMemory provider masks the missing table in dev, so tests pass but PostgreSQL fails | Medium | High | Document that PostgreSQL integration tests should be run before deployment; InMemory tests verify handler logic, not schema |

### Critical Path
1.1 (migration is required for the endpoints to function in production)

### Estimated Total Effort
- S tasks: 0
- M tasks: 3 x ~2.5 h = ~7.5 h
- L tasks: 0
- XL tasks: 0
- **Total: ~7.5 hours**
