# Circit Production Graph Gap Analysis (2026-03-02)

## Baseline Request
- Endpoint: `GET /api/projects/42ef18bc-bb27-4189-a45f-e7423137c1ae/graph`
- Result: `1525` nodes, `1448` edges
- Current persisted distribution:
  - `Context=2`
  - `Container=1518`
  - `Component=5`
  - `serviceType external=1525`

## Catalog-Reclassified Effective Distribution
Using `AzureResourceTypeCatalog` against node ARM types parsed from `externalResourceId`:
- `Context=148`
- `Container=570`
- `Component=807`
- `level mismatches=948`
- `service mismatches=808`
- `any mismatch (level or service)=1392`
- Known-type current-vs-effective match: `12 / 1404` (`0.85%`)

## Top Mismatched ARM Types
1. `microsoft.network/networkinterfaces` (`191`) - expected `Component`
2. `microsoft.network/privateendpoints` (`189`) - expected `Component`
3. `microsoft.storage/storageaccounts` (`136`) - expected `serviceType=storage`
4. `microsoft.network/privatednszones` (`133`) - expected `Component`
5. `Microsoft.Resources/subscriptions/resourceGroups` (`110`) - expected `Context + boundary`
6. `microsoft.web/sites` (`58`) - expected `serviceType=app`
7. `microsoft.alertsmanagement/smartdetectoralertrules` (`56`) - expected `Component + monitoring`
8. `microsoft.web/serverfarms` (`55`) - expected `Component + app`

## Why UI Looks Empty on Context/Component
- Production API currently returns only `2` Context and `5` Component nodes.
- Production frontend applies client-side filters (`environment=production`, hide-unconnected behavior), which can drop these low-cardinality levels to zero visible nodes.
- In Playwright checks:
  - `Container + production`: rendered
  - `Context + production`: empty
  - `Component + production`: empty
  - Switching to `all` environments revealed the sparse non-container nodes.

## Subscription Inventory Cross-Check
Azure subscription `f18100c6-5af0-49d6-9295-33614054d3df`:
- Resources: `1622`
- Resource groups: `110`
- Dominant types include networking and private endpoint infrastructure (`networkInterfaces`, `privateEndpoints`, `privateDnsZones*`, `storageAccounts`), confirming that infra-heavy footprint must be managed by level/scope/infra policies.

