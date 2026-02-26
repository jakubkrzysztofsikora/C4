import { Background, Controls, Edge, MarkerType, MiniMap, Node, ReactFlow } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { SiPostgresql, SiRedis, SiDocker } from 'react-icons/si';
import { LuBoxes, LuCloud, LuGlobe, LuNetwork } from 'react-icons/lu';
import { DiagramData, DiagramNode } from '../types';
import { healthColor, trafficColor } from '../utils';
import '../diagram.css';

function iconFor(type: DiagramNode['serviceType']) {
  switch (type) {
    case 'app': return <SiDocker color="#6ea8fe" />;
    case 'api': return <LuNetwork color="#7ce0c3" />;
    case 'database': return <SiPostgresql color="#73a8ff" />;
    case 'cache': return <SiRedis color="#ff7a7a" />;
    case 'queue': return <LuBoxes color="#ffd27b" />;
    case 'external': return <LuCloud color="#4dc4ff" />;
    default: return <LuGlobe />;
  }
}

function ServiceNode({ data }: { data: { node: DiagramNode } }) {
  const { node } = data;
  return (
    <div className="service-node" style={{ borderColor: healthColor(node.health) }}>
      <div className="header">
        <div className="title">{iconFor(node.serviceType)} <span>{node.label}</span></div>
        <span className={`badge ${node.health}`}>{node.health.toUpperCase()}</span>
      </div>
      <div style={{ marginTop: 6, display: 'flex', gap: 8, alignItems: 'center' }}>
        <span className="subtle" style={{ fontSize: 12 }}>{node.level}</span>
        {node.drift ? <span className="badge drift">DRIFT</span> : null}
      </div>
    </div>
  );
}

const nodeTypes = { service: ServiceNode };

export function DiagramCanvas({ data }: { data: DiagramData }) {
  const nodes: Node[] = data.nodes.map((node) => ({
    id: node.id,
    type: 'service',
    position: node.position ?? { x: 0, y: 0 },
    data: { node }
  }));

  const edges: Edge[] = data.edges.map((edge) => ({
    id: edge.id,
    source: edge.from,
    target: edge.to,
    markerEnd: { type: MarkerType.ArrowClosed, color: trafficColor(edge.traffic) },
    style: { strokeWidth: 2, stroke: trafficColor(edge.traffic) },
    label: `${Math.round(edge.traffic * 100)}%`
  }));

  return (
    <div className="diagram-stage">
      <ReactFlow fitView nodes={nodes} edges={edges} nodeTypes={nodeTypes}>
        <Background color="#203357" gap={18} size={1} />
        <MiniMap nodeColor={(n) => healthColor((n.data as { node: DiagramNode }).node.health)} />
        <Controls />
      </ReactFlow>
    </div>
  );
}
