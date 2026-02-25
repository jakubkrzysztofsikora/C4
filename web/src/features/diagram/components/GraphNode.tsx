import { DiagramNode } from '../types';

export function GraphNode({ node, onSelect }: { node: DiagramNode; onSelect: (id: string) => void }) {
  return (
    <button onClick={() => onSelect(node.id)} style={{ border: `2px solid ${node.health}`, padding: 8, background: node.drift ? '#fff3cd' : 'white' }}>
      {node.label}
    </button>
  );
}
