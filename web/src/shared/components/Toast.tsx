import type { Toast as ToastItem, ToastType } from '../hooks/useToast';

interface ToastContainerProps {
  toasts: ToastItem[];
  onRemove: (id: string) => void;
}

interface SingleToastProps {
  toast: ToastItem;
  onRemove: (id: string) => void;
}

function toastTypeClass(type: ToastType): string {
  if (type === 'success') return 'toast-success';
  if (type === 'error') return 'toast-error';
  return 'toast-info';
}

function SingleToast({ toast, onRemove }: SingleToastProps) {
  return (
    <div className={`toast ${toastTypeClass(toast.type)}`} role="alert">
      <span>{toast.message}</span>
      <button
        className="toast-close btn btn-ghost"
        onClick={() => onRemove(toast.id)}
        type="button"
        aria-label="Dismiss notification"
      >
        &times;
      </button>
    </div>
  );
}

export function ToastContainer({ toasts, onRemove }: ToastContainerProps) {
  if (toasts.length === 0) return null;

  return (
    <div className="toast-container" aria-live="polite">
      {toasts.map(toast => (
        <SingleToast key={toast.id} toast={toast} onRemove={onRemove} />
      ))}
    </div>
  );
}
