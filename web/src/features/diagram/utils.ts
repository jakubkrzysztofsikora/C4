import type { DiagramNode, TrafficState, RiskLevel } from './types';

export function healthColor(health: DiagramNode['health']): string {
  if (health === 'green') return '#2e8f5e';
  if (health === 'yellow') return '#9d7c35';
  if (health === 'red') return '#9e3a3a';
  return '#64748b';
}

export function trafficColor(traffic: number, state?: TrafficState): string {
  if (state === 'unknown') return '#64748b';
  if (state === 'green') return '#2e8f5e';
  if (state === 'yellow') return '#9d7c35';
  if (state === 'red') return '#9e3a3a';
  return traffic >= 0.8 ? '#2e8f5e' : traffic >= 0.5 ? '#9d7c35' : '#9e3a3a';
}

export function riskColor(risk: RiskLevel | undefined): string {
  if (risk === 'critical') return '#7f1d1d';
  if (risk === 'high') return '#b91c1c';
  if (risk === 'medium') return '#b45309';
  if (risk === 'low') return '#166534';
  return '#334155';
}
