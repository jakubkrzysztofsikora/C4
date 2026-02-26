import { useCallback, useState } from 'react';
import { useToast } from '../../../shared/hooks/useToast';
import { submitFeedback as submitFeedbackApi } from '../feedback.api';
import type { SubmitFeedbackRequest, SubmitFeedbackResponse } from '../feedback.types';

interface UseSubmitFeedbackResult {
  submitFeedback: (projectId: string, request: SubmitFeedbackRequest) => Promise<SubmitFeedbackResponse | undefined>;
  isSubmitting: boolean;
  error: Error | undefined;
}

export function useSubmitFeedback(): UseSubmitFeedbackResult {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<Error | undefined>(undefined);
  const { addToast } = useToast();

  const submitFeedback = useCallback(
    async (projectId: string, request: SubmitFeedbackRequest): Promise<SubmitFeedbackResponse | undefined> => {
      setIsSubmitting(true);
      setError(undefined);
      try {
        const response = await submitFeedbackApi(projectId, request);
        addToast('Feedback submitted successfully', 'success');
        return response;
      } catch (err: unknown) {
        const thrownError = err instanceof Error ? err : new Error('Failed to submit feedback');
        setError(thrownError);
        addToast('Failed to submit feedback. Please try again.', 'error');
        return undefined;
      } finally {
        setIsSubmitting(false);
      }
    },
    [addToast],
  );

  return { submitFeedback, isSubmitting, error };
}
