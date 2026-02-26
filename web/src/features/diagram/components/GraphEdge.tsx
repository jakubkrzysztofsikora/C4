import { trafficColor } from '../utils';

export function GraphEdge({ traffic }: { traffic: number }) {
  return <span style={{ color: trafficColor(traffic) }}>→</span>;
}
