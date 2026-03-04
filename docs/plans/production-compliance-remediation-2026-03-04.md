# Production Compliance Remediation Plan (March 4, 2026)

## Summary
Primary goals:
- Restore reliable discovery and prevent destructive rediscover behavior.
- Make telemetry overlays real and trustworthy (not permanently unknown).
- Improve C4 correctness and diagram data quality.
- Complete missing UX workflows (drift, no-graph behavior, security overlay semantics).
- Ensure export APIs match selected diagram state for API consumers.

## Baseline
Validated against production `https://c4.jakub.team` on March 4, 2026:
- Discovery from Dashboard/Diagram failed with 400 due enum request contract mismatch.
- Rediscover could clear graph before discovery success.
- Discovery backend could fail with connector/unavailable + DB save error path.
- Telemetry targets existed but sync ingested 0 and graph remained mostly unknown telemetry.
- C4 Code level existed in UI but lacked meaningful data.
- Fallback classification and unknown environment counts remained high.
- Threat/Security/Cost overlays were mostly heuristic.
- Security overlay visual style used drift state rather than security severity.
- Drift run loop was not user-complete in UI.
- GET export did not honor selected diagram state for API-only consumers.
- No-graph path still produced avoidable 404 noise.

## Scope
In scope:
- Functional behavior, feature completeness, and UX behavior from:
  - `docs/mvp-user-requirements.md`
  - `docs/high-lvl-technical-requirements.md`
  - Functional UX parts of `docs/ux-requirements.md`

Out of scope:
- GTM/commercial/marketplace operations.

## Workstreams

### Workstream 1 (P0): Discovery Contract + Safe Rediscover
1. Accept discovery `sources` as enum strings and numbers.
2. Return explicit validation payload for invalid `sources`.
3. Remove destructive pre-clear graph behavior from rediscover flow.
4. Ensure failed rediscover preserves current graph.

### Workstream 2 (P0): Discovery Persistence Reliability
1. Add post-normalization dedupe before persistence.
2. Add repository-level defensive dedupe by stable resource identity.
3. Log duplicate elimination counts.

### Workstream 3 (P1): Real Telemetry Ingestion and Mapping
1. Prefer project-scoped telemetry credentials over global-only API key.
2. Keep multi-target App Insights query behavior.
3. Preserve explicit unknown telemetry state when data is missing.

### Workstream 4 (P1): C4 Correctness + Data Quality
1. Improve Code-level population path (when repository source configured).
2. Reduce fallback classifications and unknown environments.
3. Keep raw declaration labels out of runtime map.

### Workstream 5 (P1/P2): Overlay Truthfulness and Security Semantics
1. Include provenance metadata in threat/cost/security responses.
2. Drive security overlay styling from security severity (not drift).
3. Keep heuristic fallback explicitly labeled.

### Workstream 6 (P2): Drift Workflow Completion
1. Add explicit run-drift action and run status in UI.
2. Surface drift run metadata and details in diagram flow.

### Workstream 7 (P2): Export API State Parity
1. Extend GET export to honor diagram query/filter state.
2. Keep POST export as client-geometry source of truth.
3. Add parity tests between GET and POST semantics.

### Workstream 8 (P2): No-Graph UX Noise Reduction
1. Treat known 404 empty-state conditions as expected in client.
2. Suppress repetitive error noise and avoid repeated fetch loops.
3. Keep guided empty-state CTA behavior.

## API / Type Changes
1. Discovery: `sources` accepts string and numeric enums with explicit invalid-value payloads.
2. Telemetry: prefer project-scoped credentials and include ingestion status fields.
3. Overlays: include `dataProvenance`, `generatedAtUtc`, and `isHeuristic`.
4. Export GET: support filter/state query parameters.
5. Drift status: include `lastRunAtUtc`, `lastRunStatus`, `lastRunError` where applicable.

## Test Plan
### API
1. Discovery accepts string/numeric enum values.
2. Invalid discovery sources return actionable 400 payload.
3. Discovery dedupe prevents duplicate-save failures.
4. Failed rediscover preserves existing graph.
5. Telemetry sync reports known values when config is valid.
6. GET export returns filtered scope.

### Frontend
1. Dashboard/Diagram discovery sends valid contract.
2. Rediscover no longer clears graph first.
3. No-graph state avoids repeated 404 noise.
4. Security overlay styling follows finding severity.
5. Export flows produce expected file behavior.

### E2E
1. Login -> discover succeeds from dashboard and diagram empty-state.
2. Failed discovery does not wipe existing graph.
3. Security overlay colors align with findings severity.
4. GET and POST export match selected filtered view scope.

## Rollout
1. Feature flags:
   - `discovery_contract_compat`
   - `safe_rediscover_atomic`
   - `discovery_dedupe_guard`
   - `telemetry_targets_v2`
   - `sourcebacked_overlays`
   - `security_overlay_severity`
   - `export_get_stateful`
   - `nograph_404_quiet_mode`
2. Canary on project `42ef18bc-bb27-4189-a45f-e7423137c1ae` for 48h.
3. Rollback gates:
   - Discovery failure rate >2% over 30min.
   - Graph-not-found after rediscover >0.
   - Telemetry known coverage remains 0 after sync attempts.
   - Export failure rate >1%.

## Assumptions
1. Compliance target is functional/API/UX behavior from the three requirement docs.
2. Unknown telemetry remains explicit unknown and never represented as healthy/zero.
3. Endpoints remain backward compatible where feasible.
4. Code-level nodes expected only when repo source is configured and parsable.
5. If source-backed providers are unavailable, heuristic fallback is explicitly labeled.
