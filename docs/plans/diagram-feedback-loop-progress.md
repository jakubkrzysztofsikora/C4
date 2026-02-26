# Progress: DiagramFeedbackLoop
Scope: FeatureSet
Created: 2026-02-26
Last Updated: 2026-02-26
Status: Not Started

## Current Focus
Planning complete – ready to start

## Task Progress

### Epic 1: FeedbackDomainModel
- [ ] 1.1 – Create Feedback module project structure
- [ ] 1.2 – Define FeedbackEntry aggregate and value objects
- [ ] 1.3 – Define FeedbackCategory and FeedbackTargetType enums
- [ ] 1.4 – Define NodeCorrection, EdgeCorrection, and ClassificationCorrection value objects
- [ ] 1.5 – Write domain model unit tests

### Epic 2: FeedbackPersistence
- [ ] 2.1 – Define feedback repository port
- [ ] 2.2 – Implement FeedbackDbContext and entity configuration
- [ ] 2.3 – Implement FeedbackEntryRepository adapter
- [ ] 2.4 – Create InMemoryFeedbackEntryRepository for tests
- [ ] 2.5 – Create EF Core migration

### Epic 3: SubmitFeedbackSlice
- [ ] 3.1 – Create SubmitFeedback command, handler, and validator
- [ ] 3.2 – Create SubmitFeedback API endpoint with JWT auth
- [ ] 3.3 – Write SubmitFeedbackHandler unit tests
- [ ] 3.4 – Write SubmitFeedback acceptance tests

### Epic 4: QueryFeedbackSlices
- [ ] 4.1 – Create GetFeedbackByProject query and handler
- [ ] 4.2 – Create GetFeedbackSummary query and handler
- [ ] 4.3 – Create query endpoints
- [ ] 4.4 – Write query handler unit tests

### Epic 5: LearningAggregation
- [ ] 5.1 – Define LearningInsight aggregate
- [ ] 5.2 – Define learning aggregation port
- [ ] 5.3 – Implement SK-based LearningAggregatorPlugin
- [ ] 5.4 – Create AggregateLearnings command and handler
- [ ] 5.5 – Create GetLearnings query and handler
- [ ] 5.6 – Write learning aggregation tests

### Epic 6: AIPromptAugmentation
- [ ] 6.1 – Define ILearningProvider cross-module contract
- [ ] 6.2 – Implement LearningProvider adapter
- [ ] 6.3 – Augment ArchitectureAnalysisPlugin with learnings
- [ ] 6.4 – Augment ThreatDetectionPlugin with learnings
- [ ] 6.5 – Augment ResourceClassifierPlugin with learnings
- [ ] 6.6 – Create FeedbackSubmitted integration event
- [ ] 6.7 – Add SignalR notification for learning updates
- [ ] 6.8 – Write AI augmentation tests

### Epic 7: FeedbackFrontend
- [ ] 7.1 – Create feedback API layer and types
- [ ] 7.2 – Create FeedbackPanel component
- [ ] 7.3 – Create NodeFeedbackDialog component
- [ ] 7.4 – Integrate feedback into DiagramPage and DiagramCanvas
- [ ] 7.5 – Create EvalDashboard page
- [ ] 7.6 – Write frontend tests

### Epic 8: EvalMetricsAndObservability
- [ ] 8.1 – Create GetEvalMetrics query and handler
- [ ] 8.2 – Create eval metrics endpoint
- [ ] 8.3 – Add feedback telemetry logging
- [ ] 8.4 – Write eval metrics tests

## Scope Changes
| Date | Change | Reason | Impact |
|------|--------|--------|--------|
| 2026-02-26 | Initial plan created | – | – |
| 2026-02-26 | Added ClassificationCorrection value object (task 1.4) | ResourceClassifierPlugin now exists in Discovery module — feedback must capture corrections to AI resource classifications | +1 value object, minor effort increase |
| 2026-02-26 | Added task 6.5: Augment ResourceClassifierPlugin | New SK-powered ResourceClassifierPlugin merged from main needs learning augmentation alongside ArchitectureAnalysis and ThreatDetection plugins | +1 M task (~2.5h) |
| 2026-02-26 | Added task 6.7: SignalR notification for learning updates | SignalR infrastructure now exists (DiagramHub, useSignalR hook) — learnings should notify clients for diagram refresh | +1 S task (~30min) |
| 2026-02-26 | Updated auth approach from generic to JWT Bearer | Real Identity module with JWT auth now exists — feedback endpoints use .RequireAuthorization() and extract UserId from sub claim | No effort change, implementation clarity |
| 2026-02-26 | Updated frontend tasks to use existing shared components | useToast, AuthContext, API client with auth headers, Toast component, ProtectedRoute, CSS design tokens all now exist | Reduced effort for frontend tasks |
| 2026-02-26 | Moved FeedbackSubmittedIntegrationEvent to Shared.Kernel | All integration events now live in Shared.Kernel.IntegrationEvents — follow established pattern | Cleaner architecture alignment |

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-26 | New Feedback module rather than extending Visualization | Feedback is a cross-cutting concern spanning Graph, Visualization, Discovery, and AI analysis — a dedicated module avoids coupling |
| 2026-02-26 | ILearningProvider as shared kernel contract alongside ITelemetryQueryService | Allows Graph and Discovery modules to consume learnings without depending on Feedback module directly; follows the exact same pattern as ITelemetryQueryService |
| 2026-02-26 | SK-based learning aggregation rather than rules-based | AI can identify patterns across diverse feedback that static rules would miss; confidence scoring filters low-quality insights |
| 2026-02-26 | JSON columns for corrections rather than normalized tables | Corrections are write-once read-rarely; JSON keeps the schema simple while still being queryable via PostgreSQL jsonb operators |
| 2026-02-26 | All three SK plugins augmented (not just two) | ResourceClassifierPlugin is now a third AI plugin producing classifications that users may want to correct; consistent augmentation across all AI outputs |
| 2026-02-26 | UserId from JWT sub claim, not request body | Identity module stores userId as sub claim in JWT; extract at endpoint level following established auth pattern |
| 2026-02-26 | FeedbackSubmittedIntegrationEvent in Shared.Kernel.IntegrationEvents | Follow the established pattern where ResourcesDiscoveredIntegrationEvent, TelemetryUpdatedIntegrationEvent, and DriftDetectedIntegrationEvent all live in Shared.Kernel |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
