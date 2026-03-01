# Progress: SecurityRemediation
Scope: Remediation
Created: 2026-03-01
Last Updated: 2026-03-01
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: Authentication & Secrets Hardening
- [ ] 1.1 – Remove hardcoded JWT signing key fallback and require configuration
- [ ] 1.2 – Move DB credentials to environment variables
- [ ] 1.3 – Add role claims to JWT token generation
- [ ] 1.4 – Tests for JWT and secrets changes

### Epic 2: Authorization Infrastructure
- [ ] 2.1 – Create ICurrentUserService port and implementation
- [ ] 2.2 – Create IProjectAuthorizationService port
- [ ] 2.3 – Implement ProjectAuthorizationService adapter
- [ ] 2.4 – Add GetByProjectAndUserAsync to IMemberRepository
- [ ] 2.5 – Tests for authorization infrastructure

### Epic 3: Project-Level Authorization Enforcement
- [ ] 3.1 – Secure SignalR DiagramHub with auth and membership check
- [ ] 3.2 – Fix InviteMemberHandler – require Owner role
- [ ] 3.3 – Fix UpdateMemberRoleHandler – require Owner role
- [ ] 3.4 – Fix GetOrganizationHandler – filter by user membership
- [ ] 3.5 – Add authorization to Graph module handlers
- [ ] 3.6 – Add authorization to Discovery module handlers
- [ ] 3.7 – Add authorization to Visualization module handlers
- [ ] 3.8 – Add authorization to Telemetry module handlers
- [ ] 3.9 – Add authorization to Feedback module handlers
- [ ] 3.10 – Tests for all authorization enforcement

### Epic 4: Input Validation & Injection Prevention
- [ ] 4.1 – Add URL validation for MCP server endpoints (SSRF prevention)
- [ ] 4.2 – Sanitize git repo URLs to prevent command injection
- [ ] 4.3 – Tests for input validation

### Epic 5: Infrastructure Security Hardening
- [ ] 5.1 – Add rate limiting middleware
- [ ] 5.2 – Encrypt Azure tokens at rest
- [ ] 5.3 – Tests for infrastructure security

### Epic 6: OAuth & AI Security
- [ ] 6.1 – Validate OAuth state parameter in Azure code exchange
- [ ] 6.2 – Sanitize LLM prompt inputs in LearningAggregatorPlugin
- [ ] 6.3 – Sanitize LLM prompt inputs in AnalyzeArchitectureHandler
- [ ] 6.4 – Tests for OAuth and AI security

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-03-01 | Initial plan created | – | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-03-01 | Place IProjectAuthorizationService in Shared.Kernel | Allows all modules to reference without cross-module dependency |
| 2026-03-01 | Use ASP.NET Core Data Protection for token encryption | Built-in key management, supports key rotation, no external dependency |
| 2026-03-01 | JSON-encode user inputs before LLM prompt embedding | Simpler than content filtering, preserves data fidelity |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
