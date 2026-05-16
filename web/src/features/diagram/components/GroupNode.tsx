import { memo } from 'react';

interface GroupNodeData {
  label: string;
  nodeCount: number;
  collapsed: boolean;
  onToggle: (groupId: string) => void;
  groupId: string;
}

export const GroupNode = memo(function GroupNode({ data }: { data: GroupNodeData }) {
  const { label, nodeCount, collapsed, onToggle, groupId } = data;

  return (
    <div
      className={`group-node${collapsed ? ' group-node--collapsed' : ''}`}
      role="button"
      tabIndex={0}
      aria-expanded={!collapsed}
      onClick={(e) => { e.stopPropagation(); onToggle(groupId); }}
      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); e.stopPropagation(); onToggle(groupId); } }}
    >
      <div className="group-header">
        <span className="group-toggle">{collapsed ? '▸' : '▾'}</span>
        {label} ({nodeCount})
      </div>
      {collapsed && (
        <div className="group-collapsed-info">{nodeCount} nodes hidden</div>
      )}
    </div>
  );
});
