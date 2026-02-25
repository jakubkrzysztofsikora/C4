Find all usages of a specific pattern, convention, or architectural construct across the codebase and report what you find.

## Arguments
- `$ARGUMENTS` – describe the pattern to search for (e.g., "vertical slice handler pattern", "IRepository port definition", "KernelFunction plugin registration")

## Instructions

1. Parse `$ARGUMENTS` to extract the pattern name and optional scope (module name, file type, layer).
2. Use `grep`, `rg`, and file system exploration to locate all relevant occurrences.
3. Group findings by:
   - **Module / feature** they belong to
   - **Layer** (Domain, Application, Infrastructure, Api, Tests)
4. For each group, show:
   - File path
   - Relevant code snippet (5–15 lines)
   - Assessment: does this follow the established pattern correctly?
5. Identify any **deviations or inconsistencies** from the canonical pattern.
6. Summarize:
   - How many instances exist
   - Which modules use it
   - Any pattern violations that should be addressed
   - Recommended canonical example to use as a reference

## Output Format

```
## Pattern: <name>

### Occurrences (<count> total)

#### <Module> – <Layer>
File: <path>
```csharp / typescript
<snippet>
```
Assessment: ✅ Correct | ⚠️ Deviation: <description>

---

### Summary
- Total: N
- Violations: N
- Canonical reference: <file path>
```
