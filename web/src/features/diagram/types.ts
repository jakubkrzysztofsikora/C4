export type ServiceType = 'app' | 'api' | 'database' | 'queue' | 'cache' | 'storage' | 'monitoring' | 'external' | 'boundary';
export type TelemetryStatus = 'known' | 'unknown';
export type RiskLevel = 'low' | 'medium' | 'high' | 'critical';
export type TrafficState = 'green' | 'yellow' | 'red' | 'unknown';
export type DiffStatus = 'added' | 'removed' | 'unchanged';

export type DiagramNode = {
  id: string;
  label: string;
  externalResourceId?: string;
  level: 'Context' | 'Container' | 'Component';
  health: 'green' | 'yellow' | 'red' | 'unknown';
  telemetryStatus?: TelemetryStatus;
  requestRate?: number;
  errorRate?: number;
  p95LatencyMs?: number;
  riskLevel?: RiskLevel;
  hourlyCostUsd?: number;
  drift?: boolean;
  serviceType: ServiceType;
  position?: { x: number; y: number };
  parentId?: string;
  environment?: string;
  resourceGroup?: string;
  domain?: string;
  isInfrastructure?: boolean;
  classificationSource?: string;
  classificationConfidence?: number;
  groupKey?: string;
  diffStatus?: DiffStatus;
};

export type DiagramEdge = {
  id: string;
  from: string;
  to: string;
  traffic: number;
  trafficState?: TrafficState;
  requestRate?: number;
  errorRate?: number;
  p95LatencyMs?: number;
  protocol?: string;
  diffStatus?: DiffStatus;
};
export type DiagramData = { nodes: DiagramNode[]; edges: DiagramEdge[] };
