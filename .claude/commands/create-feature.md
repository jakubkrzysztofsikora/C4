Scaffold a complete vertical slice feature end-to-end, following all architecture conventions.

## Arguments
- `$ARGUMENTS` – feature description in the format: `<Module>: <FeatureName> [– <brief description>]`
  - Example: `Ordering: PlaceOrder – customer places an order from a cart`

## Instructions

1. Parse module name and feature name from `$ARGUMENTS`.
2. Read `docs/architecture/vertical-slice.md` and `docs/architecture/ports-and-adapters.md`.
3. Identify an existing slice in the same module to use as a canonical reference.
4. Generate all files for the new slice:

### Backend Slice Files
```
src/Modules/<Module>/<Module>.Application/<FeatureName>/
  <FeatureName>Command.cs          # or Query.cs for reads
  <FeatureName>Handler.cs          # IRequestHandler<,> implementation
  <FeatureName>Validator.cs        # FluentValidation
  <FeatureName>Response.cs         # Response record (if applicable)

src/Modules/<Module>/<Module>.Api/Endpoints/
  <FeatureName>Endpoint.cs         # Minimal API endpoint

src/Modules/<Module>/<Module>.Tests/<FeatureName>/
  <FeatureName>HandlerTests.cs     # Unit tests for handler
  <FeatureName>EndpointTests.cs    # Acceptance tests via WAF
```

### Frontend Slice Files (if feature has UI)
```
web/src/features/<featureName>/
  <FeatureName>Page.tsx            # Page component
  use<FeatureName>.ts              # Feature hook
  <FeatureName>.types.ts           # TypeScript types
  <FeatureName>.test.tsx           # Component tests
```

5. For each file:
   - Use `record` types for commands, queries, responses
   - Handler constructor-injects only ports (interfaces), never adapters
   - Endpoint maps HTTP → command/query → response; no business logic
   - Validator uses FluentValidation rule chains only
   - Tests follow Arrange/Act/Assert; one assertion group per test

6. Register the endpoint in the module's endpoint registration file.

7. After generation, run `dotnet build` to confirm compilation.

## Quality Checklist (verify before finishing)
- [ ] No cross-module application-layer dependencies
- [ ] No infrastructure types in command/handler/domain
- [ ] Handler tested in isolation (fakes, not mocks where possible)
- [ ] Acceptance test covers the happy path via HTTP
- [ ] No code comments (only self-documenting names)
- [ ] All new types have matching unit tests
