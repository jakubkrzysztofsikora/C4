import { useState } from 'react';
import { useSubmitFeedback } from '../hooks/useSubmitFeedback';
import type { FeedbackCategory, FeedbackTargetType } from '../feedback.types';
import { StarRating } from './StarRating';

const CATEGORY_LABELS: Record<FeedbackCategory, string> = {
  DiagramLayout: 'Diagram Layout',
  NodeClassification: 'Node Classification',
  EdgeRelationship: 'Edge Relationship',
  ThreatAssessment: 'Threat Assessment',
  ArchitectureAnalysis: 'Architecture Analysis',
  ResourceClassification: 'Resource Classification',
  General: 'General',
};

interface FeedbackPanelProps {
  projectId: string;
  targetType: FeedbackTargetType;
  targetId: string;
  category: FeedbackCategory;
  onClose: () => void;
  isOpen: boolean;
}

export function FeedbackPanel({
  projectId,
  targetType,
  targetId,
  category,
  onClose,
  isOpen,
}: FeedbackPanelProps) {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const { submitFeedback, isSubmitting } = useSubmitFeedback();

  if (!isOpen) return null;

  async function handleSubmit() {
    if (rating === 0) return;
    const trimmedComment = comment.trim();
    const result = await submitFeedback(projectId, {
      targetType,
      targetId,
      category,
      rating,
      ...(trimmedComment.length > 0 && { comment: trimmedComment }),
    });
    if (result !== undefined) {
      setRating(0);
      setComment('');
      onClose();
    }
  }

  return (
    <div className="feedback-panel-overlay" role="dialog" aria-modal="true" aria-label="Submit feedback">
      <div className="feedback-panel">
        <div className="feedback-panel-header">
          <h3 className="feedback-panel-title">Submit Feedback</h3>
          <button
            type="button"
            className="btn btn-ghost feedback-panel-close"
            onClick={onClose}
            aria-label="Close feedback panel"
          >
            &times;
          </button>
        </div>

        <div className="feedback-panel-body">
          <div className="form-group">
            <span className="form-label">Category</span>
            <span className="feedback-category-badge">{CATEGORY_LABELS[category]}</span>
          </div>

          <div className="form-group">
            <label className="form-label">Rating</label>
            <StarRating rating={rating} onRate={setRating} />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="feedback-comment">
              Comment (optional)
            </label>
            <textarea
              id="feedback-comment"
              className="input feedback-textarea"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Describe what could be improved..."
              rows={4}
              disabled={isSubmitting}
            />
          </div>
        </div>

        <div className="feedback-panel-footer">
          <button
            type="button"
            className="btn"
            onClick={onClose}
            disabled={isSubmitting}
          >
            Cancel
          </button>
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => void handleSubmit()}
            disabled={isSubmitting || rating === 0}
          >
            {isSubmitting ? (
              <>
                <span className="spinner spinner-sm" />
                Submitting...
              </>
            ) : (
              'Submit Feedback'
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
