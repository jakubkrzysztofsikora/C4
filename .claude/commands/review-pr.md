Conduct a thorough pull request review focused on architecture, code quality, and standards compliance.

## Arguments
- `$ARGUMENTS` – PR number or branch name (e.g., "PR #42", "feature/ordering-place-order")

## Instructions

1. Get the diff: `git diff main...<branch>` or inspect the provided PR.
2. Read all changed files.
3. Review across these dimensions:

### Architecture Review
- Do changes respect module boundaries? (No cross-module application dependencies)
- Do changes respect layer boundaries? (No infra in domain/application)
- Are new ports defined in the correct layer?
- Are vertical slices self-contained?

### Code Quality Review
- Are names precise and intention-revealing?
- Are there any code comments that should be removed or replaced with better naming?
- Are methods small and focused (single responsibility)?
- Is error handling explicit via Result types?
- Is there unnecessary abstraction or premature generalization?
- Is there code duplication that should be shared (within the same slice)?

### Semantic Kernel Review (if AI code changed)
- Are plugins registered via DI?
- Are prompts externalized?
- Are AI calls observable?
- Is model configuration in config, not hardcoded?

### Test Review
- Do new behaviors have corresponding tests?
- Do tests follow naming convention: `<Method>_<Scenario>_<ExpectedOutcome>`?
- Are tests isolated appropriately (unit vs integration vs acceptance)?
- Are acceptance tests covering the happy path?
- Is there test coverage for error/failure paths?

### Standards Compliance
- No `async void`
- No `.Result` / `.Wait()` on Tasks
- `sealed` on leaf classes
- File-scoped namespaces
- One type per file
- No magic strings (use constants)
- TypeScript: no `any`, strict mode

4. Categorize findings:
   - **Blocker** – must fix before merge (architectural violation, broken test, security issue)
   - **Required** – should fix (standards violation, missing test coverage)
   - **Suggestion** – optional improvement (style, naming preference)

5. Produce structured review output.

## Output Format

```
## PR Review: <PR identifier>

### Summary
Changed files: N | Additions: N | Deletions: N

### Architecture: ✅ / ⚠️ / ❌
<findings>

### Code Quality: ✅ / ⚠️ / ❌
<findings>

### Semantic Kernel: ✅ / ⚠️ / ❌ / N/A
<findings>

### Tests: ✅ / ⚠️ / ❌
<findings>

### Findings

#### 🔴 Blockers
- [File:Line] <description>

#### 🟡 Required
- [File:Line] <description>

#### 🟢 Suggestions
- [File:Line] <description>

### Verdict: ✅ Approve | 🔄 Request Changes | ❌ Reject
```
