export function GroupNode({ data }: { data: { label: string; nodeCount: number } }) {
  return (
    <div className="group-node">
      <div className="group-header">{data.label} ({data.nodeCount})</div>
    </div>
  );
}
