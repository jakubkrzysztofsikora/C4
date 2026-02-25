Investigate a bug, unexpected behavior, or failing test through systematic root cause analysis.

## Arguments
- `$ARGUMENTS` – description of the issue, error message, failing test name, or GitHub issue number

## Instructions

1. Parse `$ARGUMENTS` to understand the symptom.
2. If a test name is given, run it: `dotnet test --filter "FullyQualifiedName~<TestName>"` and capture output.
3. Follow this investigation protocol:

### Step 1 – Reproduce
- Identify the exact failing condition (test, endpoint, agent invocation)
- Confirm the symptom is reproducible

### Step 2 – Narrow the Blast Radius
- Identify which module, layer, and component is involved
- Trace the request/command/event path from entry point to failure point

### Step 3 – Hypothesize
- List 3–5 plausible root causes ordered by likelihood
- For each hypothesis, state what evidence would confirm or refute it

### Step 4 – Verify
- For each hypothesis (highest likelihood first):
  - Read the relevant code
  - Check test coverage
  - Run targeted tests or grep for evidence
  - Mark hypothesis as confirmed, refuted, or unresolved

### Step 5 – Root Cause Statement
- State the confirmed root cause in one sentence
- Identify contributing factors (missing validation, wrong abstraction, missing test)

### Step 6 – Fix Plan
- Describe the minimal code change needed
- List which files change
- List which tests to add or update
- Confirm the fix does not break architectural boundaries

4. Implement the fix if confidence is high and scope is narrow. Otherwise, present the fix plan for human review.

## Output Format

```
## Investigation: <issue description>

### Reproduction: ✅ Confirmed | ❌ Cannot reproduce
<evidence>

### Affected Path
Entry point → ... → <failure point>

### Hypotheses
1. <hypothesis> – Likelihood: High/Medium/Low
   Evidence: <what we checked>
   Status: ✅ Confirmed | ❌ Refuted | ❓ Unresolved

### Root Cause
<one sentence>

### Fix Plan
Files: <list>
Tests: <list>
Risks: <list>
```
