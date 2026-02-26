import type { DiagramNode } from './types';

export function healthColor(health: DiagramNode['health']): string {
  return health === 'green' ? '#2e8f5e' : health === 'yellow' ? '#9d7c35' : '#9e3a3a';
}

export function trafficColor(traffic: number): string {
  return traffic >= 0.8 ? '#2e8f5e' : traffic >= 0.5 ? '#9d7c35' : '#9e3a3a';
}
