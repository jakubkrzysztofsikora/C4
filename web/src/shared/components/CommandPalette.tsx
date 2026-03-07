import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { MdDashboard, MdBusiness, MdCloud, MdHub, MdSearch } from 'react-icons/md';

type CommandItem = {
  label: string;
  path: string;
  icon: React.ReactNode;
};

const COMMAND_ITEMS: CommandItem[] = [
  { label: 'Dashboard', path: '/', icon: <MdDashboard size={18} /> },
  { label: 'Organizations', path: '/organizations', icon: <MdBusiness size={18} /> },
  { label: 'Subscriptions', path: '/subscriptions', icon: <MdCloud size={18} /> },
  { label: 'Diagram', path: '/diagram', icon: <MdHub size={18} /> },
];

type CommandPaletteProps = {
  isOpen: boolean;
  onOpen: () => void;
  onClose: () => void;
};

export function CommandPalette({ isOpen, onOpen, onClose }: CommandPaletteProps) {
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);

  const filteredItems = COMMAND_ITEMS.filter((item) =>
    item.label.toLowerCase().includes(query.toLowerCase())
  );

  const close = useCallback(() => {
    setQuery('');
    setActiveIndex(0);
    onClose();
  }, [onClose]);

  const activateItem = useCallback(
    (item: CommandItem) => {
      navigate(item.path);
      close();
    },
    [navigate, close]
  );

  useEffect(() => {
    if (isOpen) {
      inputRef.current?.focus();
    }
  }, [isOpen]);

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    if (!isOpen) return;

    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        close();
        return;
      }

      if (e.key === 'ArrowDown') {
        e.preventDefault();
        if (filteredItems.length === 0) {
          return;
        }
        setActiveIndex((prev) => Math.min(prev + 1, filteredItems.length - 1));
        return;
      }

      if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActiveIndex((prev) => Math.max(prev - 1, 0));
        return;
      }

      if (e.key === 'Enter') {
        const item = filteredItems[activeIndex];
        if (item !== undefined) {
          activateItem(item);
        }
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, close, filteredItems, activeIndex, activateItem]);

  return (
    <>
      <button className="command-palette-trigger" onClick={onOpen} type="button" aria-label="Open command palette">
        <MdSearch size={16} />
        <span>Search...</span>
        <kbd>⌘K</kbd>
      </button>
      {isOpen && (
        <div className="command-palette-overlay" onClick={close}>
          <div className="command-palette-modal" role="dialog" aria-modal="true" aria-label="Command palette" onClick={(e) => e.stopPropagation()}>
            <div className="command-palette-input-wrapper">
              <MdSearch size={20} />
              <input
                ref={inputRef}
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Search..."
                aria-label="Search resources, nodes, and services"
              />
            </div>
            <div className="command-palette-results">
              {filteredItems.map((item, index) => (
                <button
                  key={item.path}
                  className={`command-palette-item${index === activeIndex ? ' active' : ''}`}
                  onClick={() => activateItem(item)}
                  type="button"
                  onMouseEnter={() => setActiveIndex(index)}
                >
                  <span className="command-palette-item-icon">{item.icon}</span>
                  {item.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
    </>
  );
}
