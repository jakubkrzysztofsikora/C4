# C4 – Claude Code Instructions

## Project Overview

C4 is a **modular monolith** built with **.NET 9** (backend) and **React + TypeScript** (frontend), using **Semantic Kernel** as the primary AI orchestration framework. The system is heavily oriented toward agentic AI workflows and follows **Vertical Slice Architecture**, **Ports and Adapters (Hexagonal Architecture)**, and **Domain-Driven Design** principles.

## Repository Structure

```
src/
  Modules/                         # Modular monolith modules
    <Module>/
      <Module>.Api/                # HTTP endpoints (vertical slices)
      <Module>.Application/        # Use cases, handlers, agent skills
      <Module>.Domain/             # Domain model, ports (interfaces)
      <Module>.Infrastructure/     # Adapters (implementations)
      <Module>.Tests/              # All tests for the module
  Shared/
    Kernel/                        # Shared kernel (value objects, base types)
    Infrastructure/                # Cross-cutting adapters (DB, messaging)
  Host/                            # ASP.NET Core host / composition root
web/                               # React + TypeScript frontend
  src/
    features/                      # Feature-based folder structure
    shared/                        # Shared UI components and hooks
docs/
  architecture/                    # Architecture decision records and guides
  standards/                       # Coding and testing standards
```

## Architecture Principles

### Vertical Slice Architecture
Every feature is a self-contained vertical slice through all layers. A slice owns its request, handler, response, validation, and tests. Slices do not share application-layer code with other slices. See `docs/architecture/vertical-slice.md`.

### Modular Monolith
Modules communicate through explicit contracts (interfaces or in-process messages). No direct cross-module type references in application or domain code. See `docs/architecture/modular-monolith.md`.

### Ports and Adapters
Domain and application layers define ports (interfaces). Infrastructure implements adapters. The direction of dependency always points inward. See `docs/architecture/ports-and-adapters.md`.

### Agentic Patterns with Semantic Kernel
AI capabilities are implemented as Semantic Kernel plugins, planners, and process steps. Agents are composable and observable. See `docs/architecture/agentic-patterns.md`.

## Technology Stack

| Layer | Technology |
|---|---|
| Backend runtime | .NET 9 / ASP.NET Core |
| AI orchestration | Semantic Kernel 1.x |
| Frontend | React 19 + TypeScript 5 |
| Database | PostgreSQL (EF Core) |
| Messaging | MediatR (in-process), optionally RabbitMQ |
| Testing | xUnit, FluentAssertions, Testcontainers, bUnit |
| Build | .NET SDK, Vite |

## Coding Standards

See `docs/standards/coding-standards.md` for full details. Key rules:

### No Comments Rule
**Do not write code comments.** Code must be self-documenting through:
- Precise, intention-revealing names
- Small, single-purpose methods and classes
- Expressive domain language (ubiquitous language)

The only exceptions are:
- Public API XML doc comments where tooling requires them (e.g., generated OpenAPI spec)
- Temporary `// TODO:` tags that are immediately tracked as issues

### C# Style
- Use `record` types for value objects, DTOs, and commands/queries
- Use `sealed` on leaf classes by default
- Prefer `Result<T>` / `OneOf` over exceptions for expected failure paths
- Use primary constructors where they reduce noise
- File-scoped namespaces always
- One type per file always
- `async`/`await` throughout; never `.Result` or `.Wait()`

### TypeScript Style
- Strict TypeScript (`"strict": true`)
- No `any` – use `unknown` and narrow
- Prefer functional patterns; avoid classes for state management
- Co-locate feature code (component, hook, types, tests in same folder)

## Testing Standards

See `docs/standards/testing-standards.md` for full details. Key rules:

- **Unit tests** cover domain logic and application handlers in isolation using fakes
- **Integration tests** use Testcontainers (real databases, real infra) for adapter verification
- **Acceptance tests** exercise vertical slices end-to-end via `WebApplicationFactory`
- Test class name: `<SystemUnderTest>Tests`
- Test method name: `<Method>_<Scenario>_<ExpectedOutcome>` (MethodName_GivenContext_ExpectedResult)
- No magic strings – use constants or builders
- Arrange/Act/Assert separated by blank lines; no comments labeling the sections
- One logical assertion per test (use `FluentAssertions` assertion groups)
- No test helpers that cross module boundaries

## Running the Project

```bash
# Backend
dotnet restore
dotnet build
dotnet test

# Frontend
cd web
npm install
npm run dev

# All tests
dotnet test --configuration Release
```

## Key Commands

Use these Claude Code slash commands for common tasks:

| Command | Purpose |
|---|---|
| `/find-pattern` | Search for an architectural or code pattern across the codebase |
| `/analyze-code` | Deep analysis of a file, class, or module |
| `/investigate-issue` | Root cause investigation for a bug or unexpected behavior |
| `/create-feature` | Scaffold a new vertical slice feature end-to-end |
| `/create-test` | Generate tests for a given unit, handler, or slice |
| `/refactor` | Guided refactoring with architecture compliance check |
| `/review-pr` | Structured pull request review |
| `/web-research` | Research a topic and synthesize findings into actionable guidance |

## Agent Behavior Guidelines

When acting as a coding agent in this repository:

1. **Always read the relevant architecture docs** in `docs/architecture/` before generating new modules or features
2. **Follow the existing slice structure** – copy the nearest slice as a template, do not invent new patterns
3. **Write the test first** when implementing a new handler or domain behavior
4. **Validate architectural boundaries** – never let infrastructure types leak into domain or application layers
5. **Use Semantic Kernel conventions** – plugins, filters, process steps; not custom AI abstractions
6. **Never leave broken builds** – every commit must compile and all tests must pass
7. **Prefer composition over inheritance** in both domain and SK plugin design

## Working with Semantic Kernel

- Register plugins via DI in the module's composition root, not inline in handlers
- Use `KernelFunction` attribute on methods; keep plugin classes focused on a single capability
- Prefer `ChatCompletionAgent` and `AgentGroupChat` for multi-agent scenarios
- Use `KernelProcessStep` for stateful, durable workflows
- Log all AI interactions via the SK `IPromptRenderFilter` and `IFunctionInvocationFilter` hooks
- Never hard-code model names – use configuration
