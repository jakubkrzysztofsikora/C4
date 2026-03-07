import { type DiagramNode } from '../types';

function healthClass(health: string): string {
  if (health === 'healthy' || health === 'green') return 'health-green';
  if (health === 'degraded' || health === 'yellow') return 'health-yellow';
  if (health === 'critical' || health === 'red') return 'health-red';
  return 'health-unknown';
}

export function GraphNode({ node, onSelect }: { node: DiagramNode; onSelect: (id: string) => void }) {
  const classes = ['graph-node-btn', healthClass(node.health), node.drift ? 'drift' : ''].filter(Boolean).join(' ');
  return (
    <button className={classes} onClick={() => onSelect(node.id)} type="button">
      <span className="graph-node-label">{node.label}</span>
      <span className={`badge ${healthClass(node.health).replace('health-', '')}`}>
        {node.health}
      </span>
    </button>
  );
}
