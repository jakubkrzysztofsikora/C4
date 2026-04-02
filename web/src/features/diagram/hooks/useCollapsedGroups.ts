import { useCallback, useState } from 'react';

interface UseCollapsedGroupsResult {
  collapsedGroups: Set<string>;
  toggleGroup: (groupId: string) => void;
}

export function useCollapsedGroups(): UseCollapsedGroupsResult {
  const [collapsedGroups, setCollapsedGroups] = useState<Set<string>>(new Set());

  const toggleGroup = useCallback((groupId: string) => {
    setCollapsedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(groupId)) {
        next.delete(groupId);
      } else {
        next.add(groupId);
      }
      return next;
    });
  }, []);

  return { collapsedGroups, toggleGroup };
}
