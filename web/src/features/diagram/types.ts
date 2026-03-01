export type ServiceType = 'app' | 'api' | 'database' | 'queue' | 'cache' | 'external';

export type DiagramNode = {
  id: string;
  label: string;
  level: 'Context' | 'Container' | 'Component';
  health: 'green' | 'yellow' | 'red';
  drift?: boolean;
  serviceType: ServiceType;
  position?: { x: number; y: number };
  parentId?: string;
  environment?: string;
};

export type DiagramEdge = { id: string; from: string; to: string; traffic: number };
export type DiagramData = { nodes: DiagramNode[]; edges: DiagramEdge[] };
