import { getJson, postJson } from '../../shared/api/client';
import type {
  AggregateLearningsResponse,
  FeedbackByProjectResponse,
  FeedbackSummary,
  LearningsResponse,
  SubmitFeedbackRequest,
  SubmitFeedbackResponse,
} from './feedback.types';

export function submitFeedback(
  projectId: string,
  request: SubmitFeedbackRequest,
): Promise<SubmitFeedbackResponse> {
  return postJson<SubmitFeedbackRequest, SubmitFeedbackResponse>(
    `/api/projects/${projectId}/feedback`,
    request,
  );
}

export function getFeedbackByProject(
  projectId: string,
  skip = 0,
  take = 20,
  category?: string,
): Promise<FeedbackByProjectResponse> {
  const params = new URLSearchParams({ skip: String(skip), take: String(take) });
  if (category !== undefined) params.set('category', category);
  return getJson<FeedbackByProjectResponse>(`/api/projects/${projectId}/feedback?${params}`);
}

export function getFeedbackSummary(projectId: string): Promise<FeedbackSummary> {
  return getJson<FeedbackSummary>(`/api/projects/${projectId}/feedback/summary`);
}

export function getLearnings(projectId: string, category?: string): Promise<LearningsResponse> {
  const queryString = category !== undefined ? `?category=${category}` : '';
  return getJson<LearningsResponse>(`/api/projects/${projectId}/feedback/learnings${queryString}`);
}

export function aggregateLearnings(projectId: string): Promise<AggregateLearningsResponse> {
  return postJson<Record<string, never>, AggregateLearningsResponse>(
    `/api/projects/${projectId}/feedback/aggregate`,
    {},
  );
}
