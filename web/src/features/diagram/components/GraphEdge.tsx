export function GraphEdge({ traffic }: { traffic: number }) {
  const color = traffic >= 0.8 ? 'green' : traffic >= 0.5 ? 'orange' : 'red';
  return <span style={{ color }}>→</span>;
}
