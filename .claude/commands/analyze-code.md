Perform a deep analysis of a file, class, module, or agent to assess quality, architecture compliance, and improvement opportunities.

## Arguments
- `$ARGUMENTS` – path or description of what to analyze (e.g., "src/Modules/Ordering/Ordering.Application/PlaceOrder", "the authentication module", "the SK planning agent")

## Instructions

1. Locate the target (file, directory, or logical unit) from `$ARGUMENTS`.
2. Read all relevant source files. If a module is specified, read the full module tree.
3. Analyze across these dimensions:

### Architecture Compliance
- Does code respect layer boundaries (no infra leaking into domain)?
- Are ports defined in domain/application and adapters in infrastructure?
- Is each vertical slice self-contained?
- Do modules communicate only through defined contracts?

### Code Quality
- Are names precise and intention-revealing?
- Are methods small and single-purpose?
- Is there any dead code, duplication, or unnecessary abstraction?
- Are there any comments that could be replaced by better naming?
- Is error handling explicit (Result type) vs implicit (exceptions)?

### Semantic Kernel Compliance (if AI code present)
- Are plugins properly registered via DI?
- Are prompt templates externalized and versioned?
- Are AI calls observable (filters, logging)?
- Is model configuration externalized?

### Test Coverage
- Does corresponding test coverage exist?
- Do tests cover happy path, edge cases, and failure scenarios?
- Are tests isolated (unit) vs integrated (with real infrastructure)?

### Improvement Opportunities
- List up to 5 concrete, actionable refactoring suggestions
- Order by impact (highest first)

4. Produce a structured report.

## Output Format

```
## Analysis: <target>

### Architecture Compliance: ✅ / ⚠️ / ❌
<findings>

### Code Quality: ✅ / ⚠️ / ❌
<findings>

### Semantic Kernel Compliance: ✅ / ⚠️ / ❌ / N/A
<findings>

### Test Coverage: ✅ / ⚠️ / ❌
<findings>

### Top Improvement Opportunities
1. <suggestion> – Impact: High/Medium/Low
2. ...
```
