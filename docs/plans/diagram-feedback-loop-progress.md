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
- [ ] 1.4 – Define NodeCorrection and EdgeCorrection value objects
- [ ] 1.5 – Write domain model unit tests

### Epic 2: FeedbackPersistence
- [ ] 2.1 – Define feedback repository port
- [ ] 2.2 – Implement FeedbackDbContext and entity configuration
- [ ] 2.3 – Implement FeedbackEntryRepository adapter
- [ ] 2.4 – Create InMemoryFeedbackEntryRepository for tests
- [ ] 2.5 – Create EF Core migration

### Epic 3: SubmitFeedbackSlice
- [ ] 3.1 – Create SubmitFeedback command, handler, and validator
- [ ] 3.2 – Create SubmitFeedback API endpoint
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
- [ ] 6.5 – Create FeedbackSubmitted integration event
- [ ] 6.6 – Write AI augmentation tests

### Epic 7: FeedbackFrontend
- [ ] 7.1 – Create feedback API layer and types
- [ ] 7.2 – Create FeedbackPanel component
- [ ] 7.3 – Create NodeFeedbackDialog component
- [ ] 7.4 – Integrate feedback into DiagramPage
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

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-26 | New Feedback module rather than extending Visualization | Feedback is a cross-cutting concern spanning Graph, Visualization, and AI analysis — a dedicated module avoids coupling |
| 2026-02-26 | ILearningProvider as shared kernel contract | Allows Graph module to consume learnings without depending on Feedback module directly |
| 2026-02-26 | SK-based learning aggregation rather than rules-based | AI can identify patterns across diverse feedback that static rules would miss; confidence scoring filters low-quality insights |
| 2026-02-26 | JSON columns for corrections rather than normalized tables | Corrections are write-once read-rarely; JSON keeps the schema simple while still being queryable via PostgreSQL JSON operators |

## Blocked Items
| Task | Blocker | Since | Resolution |
|------|---------|-------|------------|

## Completed Work
