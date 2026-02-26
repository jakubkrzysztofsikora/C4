import { useState } from 'react';
import { useSubmitFeedback } from '../hooks/useSubmitFeedback';
import type { NodeCorrection } from '../feedback.types';
import type { ServiceType } from '../../diagram/types';
import { StarRating } from './StarRating';

const C4_LEVELS: readonly ('Context' | 'Container' | 'Component')[] = ['Context', 'Container', 'Component'];
const SERVICE_TYPE_OPTIONS: readonly ServiceType[] = ['app', 'api', 'database', 'queue', 'cache', 'external'];

type C4Level = (typeof C4_LEVELS)[number];

interface NodeFeedbackNode {
  id: string;
  label: string;
  level: string;
  serviceType: string;
}

interface NodeFeedbackDialogProps {
  projectId: string;
  node: NodeFeedbackNode;
  onClose: () => void;
  isOpen: boolean;
}

interface CorrectionState {
  name: string;
  level: string;
  serviceType: string;
}

function buildNodeCorrection(
  node: NodeFeedbackNode,
  corrections: CorrectionState,
): NodeCorrection | undefined {
  const hasNameChange = corrections.name.trim().length > 0 && corrections.name.trim() !== node.label;
  const hasLevelChange = corrections.level.length > 0 && corrections.level !== node.level;
  const hasServiceTypeChange = corrections.serviceType.length > 0 && corrections.serviceType !== node.serviceType;

  if (!hasNameChange && !hasLevelChange && !hasServiceTypeChange) return undefined;

  return {
    ...(hasNameChange && {
      originalName: node.label,
      correctedName: corrections.name.trim(),
    }),
    ...(hasLevelChange && {
      originalLevel: node.level,
      correctedLevel: corrections.level,
    }),
    ...(hasServiceTypeChange && {
      originalServiceType: node.serviceType,
      correctedServiceType: corrections.serviceType,
    }),
  };
}

export function NodeFeedbackDialog({
  projectId,
  node,
  onClose,
  isOpen,
}: NodeFeedbackDialogProps) {
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [corrections, setCorrections] = useState<CorrectionState>({
    name: '',
    level: '',
    serviceType: '',
  });
  const { submitFeedback, isSubmitting } = useSubmitFeedback();

  if (!isOpen) return null;

  function updateCorrection(field: keyof CorrectionState, value: string) {
    setCorrections((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit() {
    if (rating === 0) return;
    const trimmedComment = comment.trim();
    const nodeCorrection = buildNodeCorrection(node, corrections);
    const result = await submitFeedback(projectId, {
      targetType: 'GraphNode',
      targetId: node.id,
      category: 'NodeClassification',
      rating,
      ...(trimmedComment.length > 0 && { comment: trimmedComment }),
      ...(nodeCorrection !== undefined && { nodeCorrection }),
    });
    if (result !== undefined) {
      setRating(0);
      setComment('');
      setCorrections({ name: '', level: '', serviceType: '' });
      onClose();
    }
  }

  return (
    <div className="feedback-panel-overlay" role="dialog" aria-modal="true" aria-label="Node feedback">
      <div className="feedback-panel feedback-panel-wide">
        <div className="feedback-panel-header">
          <h3 className="feedback-panel-title">Node Feedback</h3>
          <button
            type="button"
            className="btn btn-ghost feedback-panel-close"
            onClick={onClose}
            aria-label="Close node feedback dialog"
          >
            &times;
          </button>
        </div>

        <div className="feedback-panel-body">
          <div className="feedback-node-current">
            <h4 className="feedback-section-label">Current Classification</h4>
            <div className="feedback-node-info">
              <div className="feedback-node-field">
                <span className="feedback-node-field-label">Name</span>
                <span className="feedback-node-field-value">{node.label}</span>
              </div>
              <div className="feedback-node-field">
                <span className="feedback-node-field-label">C4 Level</span>
                <span className="feedback-node-field-value">{node.level}</span>
              </div>
              <div className="feedback-node-field">
                <span className="feedback-node-field-label">Service Type</span>
                <span className="feedback-node-field-value">{node.serviceType}</span>
              </div>
            </div>
          </div>

          <div className="feedback-corrections">
            <h4 className="feedback-section-label">Suggested Corrections (optional)</h4>

            <div className="form-group">
              <label className="form-label" htmlFor="node-corrected-name">
                Corrected Name
              </label>
              <input
                id="node-corrected-name"
                className="input"
                type="text"
                placeholder={node.label}
                value={corrections.name}
                onChange={(e) => updateCorrection('name', e.target.value)}
                disabled={isSubmitting}
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="node-corrected-level">
                Corrected C4 Level
              </label>
              <select
                id="node-corrected-level"
                className="input"
                value={corrections.level}
                onChange={(e) => updateCorrection('level', e.target.value)}
                disabled={isSubmitting}
              >
                <option value="">No change</option>
                {C4_LEVELS.filter((l): l is C4Level => l !== node.level).map((level) => (
                  <option key={level} value={level}>
                    {level}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="node-corrected-service-type">
                Corrected Service Type
              </label>
              <select
                id="node-corrected-service-type"
                className="input"
                value={corrections.serviceType}
                onChange={(e) => updateCorrection('serviceType', e.target.value)}
                disabled={isSubmitting}
              >
                <option value="">No change</option>
                {SERVICE_TYPE_OPTIONS.filter((t) => t !== node.serviceType).map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="form-group">
            <label className="form-label">Rating</label>
            <StarRating rating={rating} onRate={setRating} />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="node-feedback-comment">
              Comment (optional)
            </label>
            <textarea
              id="node-feedback-comment"
              className="input feedback-textarea"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="Describe the classification issue..."
              rows={3}
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
