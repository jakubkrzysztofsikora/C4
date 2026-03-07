type KeyboardShortcutsDialogProps = {
  isOpen: boolean;
  onClose: () => void;
};

type ShortcutRow = {
  keys: string[];
  description: string;
};

type ShortcutSection = {
  title: string;
  shortcuts: ShortcutRow[];
};

const SHORTCUT_SECTIONS: ShortcutSection[] = [
  {
    title: 'Diagram',
    shortcuts: [
      { keys: ['1', '2', '3', '4'], description: 'Switch C4 level (Context / Container / Component / Code)' },
      { keys: ['R'], description: 'Reset diagram to full map' },
    ],
  },
  {
    title: 'Navigation',
    shortcuts: [
      { keys: ['⌘K', 'Ctrl+K'], description: 'Open command palette' },
      { keys: ['?'], description: 'Show keyboard shortcuts' },
    ],
  },
];

export function KeyboardShortcutsDialog({ isOpen, onClose }: KeyboardShortcutsDialogProps) {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="shortcuts-overlay" onClick={onClose}>
      <div className="shortcuts-modal" onClick={(e) => e.stopPropagation()}>
        <div className="shortcuts-header">
          <h2>Keyboard Shortcuts</h2>
          <button className="btn btn-sm btn-ghost" onClick={onClose} type="button" aria-label="Close">
            &times;
          </button>
        </div>
        <div className="shortcuts-body">
          {SHORTCUT_SECTIONS.map((section) => (
            <div key={section.title} className="shortcuts-section">
              <div className="shortcuts-section-title">{section.title}</div>
              {section.shortcuts.map((shortcut) => (
                <div key={shortcut.description} className="shortcut-row">
                  <span>{shortcut.description}</span>
                  <span className="shortcut-keys">
                    {shortcut.keys.map((key) => (
                      <kbd key={key}>{key}</kbd>
                    ))}
                  </span>
                </div>
              ))}
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
