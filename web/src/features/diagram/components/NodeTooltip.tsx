export function NodeTooltip({ label, health }: { label: string; health: string }) {
  return <small>{label} · health: {health}</small>;
}
