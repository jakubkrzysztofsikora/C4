export type FeedbackTargetType =
  | 'Diagram'
  | 'GraphNode'
  | 'GraphEdge'
  | 'AnalysisResult'
  | 'ThreatResult'
  | 'ClassificationResult';

export type FeedbackCategory =
  | 'DiagramLayout'
  | 'NodeClassification'
  | 'EdgeRelationship'
  | 'ThreatAssessment'
  | 'ArchitectureAnalysis'
  | 'ResourceClassification'
  | 'General';

export interface NodeCorrection {
  originalName?: string;
  correctedName?: string;
  originalLevel?: string;
  correctedLevel?: string;
  originalServiceType?: string;
  correctedServiceType?: string;
  originalParentId?: string;
  correctedParentId?: string;
}

export interface EdgeCorrection {
  originalRelationship?: string;
  correctedRelationship?: string;
  shouldExist: boolean;
}

export interface ClassificationCorrection {
  armResourceType: string;
  originalFriendlyName?: string;
  correctedFriendlyName?: string;
  originalServiceType?: string;
  correctedServiceType?: string;
  originalC4Level?: string;
  correctedC4Level?: string;
  originalIncludeInDiagram?: boolean;
  correctedIncludeInDiagram?: boolean;
}

export interface SubmitFeedbackRequest {
  targetType: FeedbackTargetType;
  targetId: string;
  category: FeedbackCategory;
  rating: number;
  comment?: string;
  nodeCorrection?: NodeCorrection;
  edgeCorrection?: EdgeCorrection;
  classificationCorrection?: ClassificationCorrection;
}

export interface SubmitFeedbackResponse {
  feedbackEntryId: string;
}

export interface FeedbackEntry {
  id: string;
  targetType: FeedbackTargetType;
  targetId: string;
  category: FeedbackCategory;
  rating: number;
  comment?: string;
  submittedAtUtc: string;
  userId: string;
}

export interface FeedbackByProjectResponse {
  entries: FeedbackEntry[];
  totalCount: number;
}

export interface CategoryBreakdownItem {
  category: string;
  count: number;
  averageRating: number;
}

export interface FeedbackSummary {
  totalCount: number;
  averageRating: number;
  categoryBreakdown: CategoryBreakdownItem[];
}

export interface LearningInsight {
  id: string;
  category: FeedbackCategory;
  insightType: string;
  description: string;
  confidence: number;
  feedbackCount: number;
  createdAtUtc: string;
}

export interface LearningsResponse {
  insights: LearningInsight[];
}

export interface AggregateLearningsResponse {
  insightsGenerated: number;
}
