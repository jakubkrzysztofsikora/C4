Perform a structured architectural refactoring of a target file, class, or module.

## Arguments
- `$ARGUMENTS` – what to refactor and why (e.g., "OrderService has too many responsibilities", "InfraType leaking into PlaceOrderHandler", "remove magic strings in OrderModule")

## Instructions

1. Parse the target and goal from `$ARGUMENTS`.
2. Read the relevant source files and their tests.
3. Run existing tests to establish a baseline: `dotnet test --filter <relevant filter>`
4. Assess the current state against:
   - Coding standards (`docs/standards/coding-standards.md`)
   - Architecture principles (`docs/architecture/`)
   - Single Responsibility Principle
   - No-comments rule
   - Test coverage

5. Plan the refactoring:
   - State what is wrong (one sentence each)
   - State the target state
   - List the transformation steps in safe, small increments
   - Identify which tests to update or add

6. Execute each transformation step:
   - Make the change
   - Run affected tests after each step
   - Do not proceed if tests break (fix first)

7. After all steps, run the full module test suite.

8. Verify:
   - [ ] No architectural boundary violations introduced
   - [ ] No infrastructure types in domain/application
   - [ ] All existing tests still pass
   - [ ] New tests added for any new behavior
   - [ ] No code comments introduced
   - [ ] Code compiles cleanly (`dotnet build`)

## Output Format

```
## Refactoring: <target>

### Problem Statement
<what is wrong and why>

### Target State
<what it should look like>

### Steps Executed
1. <step> – ✅ Done | ❌ Blocked: <reason>
2. ...

### Test Results
Before: N passing, M failing
After:  N passing, 0 failing

### Remaining Work
<anything not completed and why>
```
