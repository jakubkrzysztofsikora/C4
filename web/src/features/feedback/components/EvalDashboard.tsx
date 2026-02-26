import { useCallback, useEffect, useState } from 'react';
import { aggregateLearnings, getFeedbackSummary, getLearnings } from '../feedback.api';
import { useToast } from '../../../shared/hooks/useToast';
import type {
  CategoryBreakdownItem,
  FeedbackCategory,
  FeedbackSummary,
  LearningInsight,
} from '../feedback.types';
import { StarRating } from './StarRating';

const CATEGORY_FILTER_OPTIONS: Array<{ value: string; label: string }> = [
  { value: '', label: 'All Categories' },
  { value: 'DiagramLayout', label: 'Diagram Layout' },
  { value: 'NodeClassification', label: 'Node Classification' },
  { value: 'EdgeRelationship', label: 'Edge Relationship' },
  { value: 'ThreatAssessment', label: 'Threat Assessment' },
  { value: 'ArchitectureAnalysis', label: 'Architecture Analysis' },
  { value: 'ResourceClassification', label: 'Resource Classification' },
  { value: 'General', label: 'General' },
];

interface EvalDashboardProps {
  projectId: string;
}

interface DashboardState {
  summary: FeedbackSummary | undefined;
  insights: LearningInsight[];
  isLoadingSummary: boolean;
  isLoadingInsights: boolean;
  isAggregating: boolean;
  summaryError: Error | undefined;
  insightsError: Error | undefined;
}

const INITIAL_STATE: DashboardState = {
  summary: undefined,
  insights: [],
  isLoadingSummary: false,
  isLoadingInsights: false,
  isAggregating: false,
  summaryError: undefined,
  insightsError: undefined,
};

function formatConfidence(confidence: number): string {
  return `${Math.round(confidence * 100)}%`;
}

function formatDate(isoString: string): string {
  return new Date(isoString).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function maxBarCount(items: CategoryBreakdownItem[]): number {
  return items.reduce((max, item) => Math.max(max, item.count), 1);
}

interface CategoryBarProps {
  item: CategoryBreakdownItem;
  maxCount: number;
}

function CategoryBar({ item, maxCount }: CategoryBarProps) {
  const widthPercent = Math.round((item.count / maxCount) * 100);

  return (
    <div className="eval-category-row">
      <span className="eval-category-name">{item.category}</span>
      <div className="eval-bar-track">
        <div className="eval-bar-fill" style={{ width: `${widthPercent}%` }} />
      </div>
      <span className="eval-category-count">{item.count}</span>
      <span className="eval-category-avg">{item.averageRating.toFixed(1)}</span>
    </div>
  );
}

interface InsightCardProps {
  insight: LearningInsight;
}

function InsightCard({ insight }: InsightCardProps) {
  return (
    <div className="eval-insight-card">
      <div className="eval-insight-header">
        <span className="eval-insight-type">{insight.insightType}</span>
        <span className="eval-insight-confidence">{formatConfidence(insight.confidence)} confidence</span>
      </div>
      <p className="eval-insight-description">{insight.description}</p>
      <div className="eval-insight-meta">
        <span className="eval-insight-category">{insight.category}</span>
        <span className="eval-insight-feedback-count">{insight.feedbackCount} feedback entries</span>
        <span className="eval-insight-date">{formatDate(insight.createdAtUtc)}</span>
      </div>
    </div>
  );
}

export function EvalDashboard({ projectId }: EvalDashboardProps) {
  const [state, setState] = useState<DashboardState>(INITIAL_STATE);
  const [categoryFilter, setCategoryFilter] = useState('');
  const { addToast } = useToast();

  const loadSummary = useCallback(async () => {
    setState((prev) => ({ ...prev, isLoadingSummary: true, summaryError: undefined }));
    try {
      const summary = await getFeedbackSummary(projectId);
      setState((prev) => ({ ...prev, summary, isLoadingSummary: false }));
    } catch (err: unknown) {
      const error = err instanceof Error ? err : new Error('Failed to load feedback summary');
      setState((prev) => ({ ...prev, summaryError: error, isLoadingSummary: false }));
    }
  }, [projectId]);

  const loadInsights = useCallback(
    async (category?: string) => {
      setState((prev) => ({ ...prev, isLoadingInsights: true, insightsError: undefined }));
      try {
        const response = await getLearnings(
          projectId,
          category !== undefined && category.length > 0 ? category : undefined,
        );
        setState((prev) => ({ ...prev, insights: response.insights, isLoadingInsights: false }));
      } catch (err: unknown) {
        const error = err instanceof Error ? err : new Error('Failed to load insights');
        setState((prev) => ({ ...prev, insightsError: error, isLoadingInsights: false }));
      }
    },
    [projectId],
  );

  useEffect(() => {
    void loadSummary();
    void loadInsights();
  }, [loadSummary, loadInsights]);

  useEffect(() => {
    void loadInsights(categoryFilter);
  }, [categoryFilter, loadInsights]);

  async function handleAggregateLearnings() {
    setState((prev) => ({ ...prev, isAggregating: true }));
    try {
      const result = await aggregateLearnings(projectId);
      addToast(`Generated ${result.insightsGenerated} new insights`, 'success');
      void loadInsights(categoryFilter);
    } catch {
      addToast('Failed to aggregate learnings', 'error');
    } finally {
      setState((prev) => ({ ...prev, isAggregating: false }));
    }
  }

  const averageRatingDisplay =
    state.summary !== undefined ? Math.round(state.summary.averageRating) : 0;

  return (
    <section className="fade-in eval-dashboard">
      <div className="eval-dashboard-header">
        <div>
          <h2 className="eval-dashboard-title">Feedback Evaluation Dashboard</h2>
          <p className="subtle eval-dashboard-subtitle">
            Review feedback trends and AI learning insights for this project.
          </p>
        </div>
        <button
          type="button"
          className="btn btn-primary"
          onClick={() => void handleAggregateLearnings()}
          disabled={state.isAggregating}
        >
          {state.isAggregating ? (
            <>
              <span className="spinner spinner-sm" />
              Aggregating...
            </>
          ) : (
            'Aggregate Learnings'
          )}
        </button>
      </div>

      {state.summaryError !== undefined && (
        <div className="card eval-error-card">
          <p style={{ color: 'var(--error)', margin: 0 }}>{state.summaryError.message}</p>
        </div>
      )}

      <div className="eval-summary-grid">
        <div className="card eval-stat-card">
          <span className="eval-stat-label">Total Feedback</span>
          {state.isLoadingSummary ? (
            <div className="skeleton eval-stat-skeleton" />
          ) : (
            <span className="eval-stat-value">{state.summary?.totalCount ?? 0}</span>
          )}
        </div>

        <div className="card eval-stat-card">
          <span className="eval-stat-label">Average Rating</span>
          {state.isLoadingSummary ? (
            <div className="skeleton eval-stat-skeleton" />
          ) : (
            <div className="eval-stat-rating">
              <span className="eval-stat-value">
                {state.summary !== undefined ? state.summary.averageRating.toFixed(1) : '—'}
              </span>
              {state.summary !== undefined && (
                <StarRating rating={averageRatingDisplay} onRate={() => undefined} readonly />
              )}
            </div>
          )}
        </div>
      </div>

      {!state.isLoadingSummary &&
        state.summary !== undefined &&
        state.summary.categoryBreakdown.length > 0 && (
          <div className="card eval-breakdown-card">
            <h3 className="eval-section-title">Category Breakdown</h3>
            <div className="eval-breakdown-header-row">
              <span className="eval-col-label">Category</span>
              <span className="eval-col-label eval-col-bar">Distribution</span>
              <span className="eval-col-label eval-col-count">Count</span>
              <span className="eval-col-label eval-col-avg">Avg</span>
            </div>
            <div className="eval-breakdown-list">
              {state.summary.categoryBreakdown.map((item) => (
                <CategoryBar
                  key={item.category}
                  item={item}
                  maxCount={maxBarCount(state.summary!.categoryBreakdown)}
                />
              ))}
            </div>
          </div>
        )}

      <div className="card eval-insights-card">
        <div className="eval-insights-header">
          <h3 className="eval-section-title">Learning Insights</h3>
          <select
            className="input eval-category-filter"
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
            aria-label="Filter insights by category"
          >
            {CATEGORY_FILTER_OPTIONS.map((opt) => (
              <option key={opt.value} value={opt.value}>
                {opt.label}
              </option>
            ))}
          </select>
        </div>

        {state.insightsError !== undefined && (
          <p style={{ color: 'var(--error)' }}>{state.insightsError.message}</p>
        )}

        {state.isLoadingInsights ? (
          <div className="eval-insights-loading">
            {[1, 2, 3].map((i) => (
              <div key={i} className="skeleton eval-insight-skeleton" />
            ))}
          </div>
        ) : state.insights.length === 0 ? (
          <div className="empty-state eval-empty-state">
            <p className="empty-state-title">No insights yet</p>
            <p className="empty-state-description">
              Collect more feedback and use "Aggregate Learnings" to generate insights.
            </p>
          </div>
        ) : (
          <div className="eval-insights-list">
            {state.insights.map((insight) => (
              <InsightCard key={insight.id} insight={insight} />
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

EvalDashboard.displayName = 'EvalDashboard';

type EvalDashboardCategoryFilterProps = {
  value: FeedbackCategory | '';
  onChange: (value: FeedbackCategory | '') => void;
};

export function EvalDashboardCategoryFilter({ value, onChange }: EvalDashboardCategoryFilterProps) {
  return (
    <select
      className="input"
      value={value}
      onChange={(e) => onChange(e.target.value as FeedbackCategory | '')}
      aria-label="Filter by category"
    >
      {CATEGORY_FILTER_OPTIONS.map((opt) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
  );
}
