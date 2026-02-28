# Progress: Fix Connector Unavailable Error
Scope: BugFix
Created: 2026-02-27
Last Updated: 2026-02-27
Status: In Progress

## Root Cause Analysis

The 400 error `{"errorCode":"discovery.connector.unavailable","errorMessage":"Connector 'azure-resource-graph' is unavailable."}` occurs because:

1. `InMemoryAzureTokenStore` stores Azure OAuth tokens in a `ConcurrentDictionary` (RAM only)
2. Server restart (deployment via GitHub push) wipes all in-memory tokens
3. `AzureResourceGraphClient.ResolveAccessTokenAsync()` calls `tokenStore.GetAsync()` → gets `null`
4. Throws `InvalidOperationException("No Azure credentials found. Please re-authenticate with Azure.")`
5. Exception propagates through `CompositeDiscoveryInputProvider` → `DiscoverResourcesHandler`
6. `DiscoveryEscalationMapper.MapExternalFailure` maps `InvalidOperationException` to default `_` case → `ConnectorUnavailable`

Error path: `DiscoverResourcesHandler` (line 46-51) → `CompositeDiscoveryInputProvider.GetResourcesAsync` → `AzureSubscriptionDiscoverySourceAdapter.GetResourcesAsync` → `AzureResourceGraphClient.GetResourcesAsync` → `ResolveAccessTokenAsync` → `tokenStore.GetAsync()` returns null → throws

## Current Focus
Task 1.1 – Create AzureTokenEntity and DatabaseAzureTokenStore

## Task Progress

### Epic 1: Token Persistence
- [x] 1.1 – Create AzureTokenEntity for EF Core model
- [x] 1.2 – Create DatabaseAzureTokenStore adapter (raw Npgsql)
- [x] 1.3 – Create EF Core migration for azure_tokens table
- [x] 1.4 – Update DI registration (conditional DB vs InMemory)

### Epic 2: Error Mapping Improvement
- [x] 2.1 – Map InvalidOperationException with auth messages to AuthPermissionFailure
- [x] 2.2 – Improve error message clarity for users

### Epic 3: Deploy and Verify
- [ ] 3.1 – Build and run all tests
- [ ] 3.2 – Commit and push to trigger deployment
- [ ] 3.3 – Verify fix end-to-end on c4.jakub.team with Playwright

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-27 | Initial plan created | – | – |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-27 | Use raw Npgsql for DatabaseAzureTokenStore instead of EF Core DbContext | Avoids singleton/scoped lifetime conflict since adapters are registered as singletons but DbContext is scoped |
| 2026-02-27 | Add AzureTokenEntity to DiscoveryDbContext for migration support | Table schema managed by EF migrations but read/write via raw Npgsql |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
