type NodeTooltipProps = {
  label: string;
  health: string;
  nodeType?: string;
  requestRate?: number;
  errorRate?: number;
  p95Latency?: number;
};

function healthBadgeClass(health: string): string {
  if (health === 'healthy' || health === 'green') return 'green';
  if (health === 'degraded' || health === 'yellow') return 'yellow';
  if (health === 'critical' || health === 'red') return 'red';
  return 'unknown';
}

export function NodeTooltip({ label, health, nodeType, requestRate, errorRate, p95Latency }: NodeTooltipProps) {
  const hasTelemetry = requestRate !== undefined || errorRate !== undefined || p95Latency !== undefined;

  return (
    <div className="node-tooltip">
      <div className="node-tooltip-header">
        <strong className="node-tooltip-name">{label}</strong>
        <span className={`badge ${healthBadgeClass(health)}`}>{health}</span>
      </div>
      {nodeType !== undefined && (
        <div className="node-tooltip-type">{nodeType}</div>
      )}
      {hasTelemetry ? (
        <div className="node-tooltip-metrics">
          {requestRate !== undefined && (
            <div className="node-tooltip-metric">
              <span className="node-tooltip-metric-label">Requests</span>
              <span className="node-tooltip-metric-value">{requestRate.toLocaleString()}/min</span>
            </div>
          )}
          {errorRate !== undefined && (
            <div className="node-tooltip-metric">
              <span className="node-tooltip-metric-label">Error rate</span>
              <span className="node-tooltip-metric-value">{(errorRate * 100).toFixed(1)}%</span>
            </div>
          )}
          {p95Latency !== undefined && (
            <div className="node-tooltip-metric">
              <span className="node-tooltip-metric-label">P95 latency</span>
              <span className="node-tooltip-metric-value">{p95Latency}ms</span>
            </div>
          )}
        </div>
      ) : (
        <div className="node-tooltip-no-telemetry">No telemetry data</div>
      )}
    </div>
  );
}
