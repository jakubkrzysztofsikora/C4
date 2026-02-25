# Git Workflow

## Branch Naming

```
feature/<module>-<short-description>     # New feature
fix/<module>-<short-description>         # Bug fix
refactor/<scope>-<short-description>     # Refactoring
chore/<short-description>                # Build, tooling, config changes
docs/<short-description>                 # Documentation only
```

Examples:
```
feature/ordering-place-order
fix/catalog-product-price-rounding
refactor/shared-result-type
docs/agentic-patterns-guide
```

## Commit Messages

Follow **Conventional Commits** format:

```
<type>(<scope>): <short description>

[optional body – what and why, not how]

[optional footer – breaking changes, issue references]
```

Types: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`

Examples:
```
feat(ordering): add place order vertical slice
fix(catalog): prevent negative inventory after cancellation
refactor(shared): extract Result type to shared kernel
test(ordering): add acceptance tests for place order endpoint
docs(architecture): add agentic patterns guide
chore(deps): upgrade Semantic Kernel to 1.25.0
```

## Pull Request Rules

1. PRs must be small – one feature, one fix, or one refactoring per PR
2. All tests must pass before requesting review
3. No unresolved TODO comments without a linked issue number
4. Self-review before requesting human review (use `/review-pr` command)
5. Squash-merge to keep history clean

## Protected Branch Rules

- `main` – production-ready code; requires PR + passing CI + 1 review
- No direct pushes to `main`
- CI must pass before merge

## CI Checks (Required to Pass)

1. `dotnet build --no-incremental` – clean build
2. `dotnet test` – all tests pass
3. Architecture boundary tests pass
4. `npm run build` – frontend builds
5. `npm run test` – frontend tests pass
