Generate a comprehensive implementation plan for a large-scope initiative (MVP, POC, feature set, refactor, migration) and create a persistent progress-tracking file.

## Arguments
- `$ARGUMENTS` – initiative description in the format: `<Scope>: <InitiativeName> – <brief description>`
  - Scope: one of `MVP`, `POC`, `FeatureSet`, `Refactor`, `Migration`, or a custom label
  - Example: `MVP: OrderingSystem – end-to-end order placement with payment and notifications`
  - Example: `Refactor: AuthModule – migrate from cookie auth to JWT with refresh tokens`
  - Example: `FeatureSet: Reporting – add dashboard, export, and scheduled reports`

## Instructions

### Phase 1 – Discovery

1. Parse the scope, initiative name, and description from `$ARGUMENTS`.
2. Read relevant architecture docs:
   - `docs/architecture/vertical-slice.md`
   - `docs/architecture/ports-and-adapters.md`
   - `docs/architecture/modular-monolith.md`
   - `docs/architecture/agentic-patterns.md` (if AI features are involved)
   - `docs/standards/coding-standards.md`
   - `docs/standards/testing-standards.md`
3. Scan the existing codebase to identify:
   - Modules and slices that already exist and will be affected
   - Patterns and conventions currently in use (use the nearest existing slice as a canonical reference)
   - Infrastructure and shared kernel types available for reuse
   - Existing test infrastructure and fixtures

### Phase 2 – Decomposition

4. Break the initiative into **epics** (logical groupings of related work).
5. Break each epic into **tasks** (individual vertical slices or atomic units of work).
6. For each task, identify:
   - Type: `Feature`, `Refactor`, `Test`, `Infrastructure`, `Documentation`, `Spike`
   - Module(s) affected
   - Files to create, modify, or delete
   - Dependencies on other tasks (explicit ordering where needed)
   - Estimated complexity: `S` (< 1 hour), `M` (1–4 hours), `L` (4–8 hours), `XL` (> 8 hours)
7. Order tasks to maximize safe incremental delivery:
   - Domain model and ports first
   - Application handlers second
   - Infrastructure adapters third
   - API endpoints fourth
   - Frontend features fifth
   - Each task must leave the build green and all existing tests passing

### Phase 3 – Risk Assessment

8. Identify risks and unknowns:
   - Technical risks (new technology, complex integrations, performance concerns)
   - Architectural risks (boundary violations, coupling, breaking changes)
   - Dependency risks (external services, libraries, cross-module contracts)
9. For each risk, define a mitigation strategy or spike task.

### Phase 4 – Plan Generation

10. Produce the plan in the **deterministic output format** below.
11. Create the progress-tracking file at `docs/plans/<initiative-name-kebab-case>.md` using the **progress file format** below.
12. The progress file must be committed to the repository so that it is version-controlled and available across sessions.

### Phase 5 – Validation

13. Verify the plan:
    - [ ] Every task produces a green build when completed in isolation
    - [ ] No circular dependencies between tasks
    - [ ] Every feature task includes corresponding test tasks
    - [ ] No task introduces cross-module application-layer dependencies
    - [ ] No task introduces infrastructure types into domain or application layers
    - [ ] All tasks follow the vertical slice pattern
    - [ ] Critical path is identified and optimized
14. Run `dotnet build` to confirm the current state compiles before starting.

## Output Format

The plan MUST follow this exact structure:

```
## Plan: <InitiativeName>
Scope: <Scope>
Created: <YYYY-MM-DD>
Status: Draft

### Overview
<2–3 sentence description of the initiative and its goals>

### Success Criteria
- [ ] <measurable criterion 1>
- [ ] <measurable criterion 2>
- [ ] ...

### Epic 1: <EpicName>
Goal: <one-sentence goal>

| # | Task | Type | Module | Complexity | Depends On | Status |
|---|------|------|--------|------------|------------|--------|
| 1.1 | <task description> | Feature | <Module> | S/M/L/XL | – | ⬚ |
| 1.2 | <task description> | Test | <Module> | S/M/L/XL | 1.1 | ⬚ |
| ... | ... | ... | ... | ... | ... | ⬚ |

#### 1.1 – <Task Name>
- **Files to create**: `<path1>`, `<path2>`
- **Files to modify**: `<path3>`
- **Acceptance criteria**:
  - <criterion 1>
  - <criterion 2>

#### 1.2 – <Task Name>
...

### Epic 2: <EpicName>
...

### Risks
| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|------------|
| R1 | <description> | High/Medium/Low | High/Medium/Low | <strategy or spike task ref> |
| ... | ... | ... | ... | ... |

### Critical Path
<ordered list of task IDs that form the longest dependency chain>
1.1 → 1.2 → 2.1 → 2.3 → 3.1

### Estimated Total Effort
- S tasks: N × ~30 min = ~N h
- M tasks: N × ~2.5 h = ~N h
- L tasks: N × ~6 h = ~N h
- XL tasks: N × ~10 h = ~N h
- **Total: ~N hours**
```

## Progress File Format

Create `docs/plans/<initiative-name-kebab-case>.md` with this exact structure:

```markdown
# Progress: <InitiativeName>
Scope: <Scope>
Created: <YYYY-MM-DD>
Last Updated: <YYYY-MM-DD>
Status: Not Started

## Current Focus
<task ID and name currently being worked on, or "Planning complete – ready to start">

## Task Progress

### Epic 1: <EpicName>
- [ ] 1.1 – <Task Name>
- [ ] 1.2 – <Task Name>

### Epic 2: <EpicName>
- [ ] 2.1 – <Task Name>
- [ ] 2.2 – <Task Name>

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| <YYYY-MM-DD> | Initial plan created | – | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| <YYYY-MM-DD> | <decision made> | <why this decision was made> |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
<empty initially – move completed tasks here with completion date>
```

## Agent Execution Rules

When implementing tasks from a plan created by this command:

1. **Always update the progress file** after completing each task – mark the checkbox `[x]`, move it to Completed Work with the date, and update Current Focus.
2. **Update the Last Updated date** on every progress file modification.
3. **Update Status** as work progresses: `Not Started` → `In Progress` → `Blocked` → `Completed`.
4. **Log scope changes** whenever a task is added, removed, or modified after the initial plan.
5. **Log decisions** that deviate from the original plan with context for future reference.
6. **Never skip tests** – every feature task must have corresponding test coverage before marking complete.
7. **Build after every task** – run `dotnet build` (and `npm run build` for frontend tasks) to confirm green build.
8. **Run affected tests after every task** – confirm no regressions.
9. **Commit after every task** – each completed task should be a separate, atomic commit.
10. **Prefer small, reversible changes** – if a task is XL, consider breaking it down further before starting.
