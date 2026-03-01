import { Background, Controls, Edge, Handle, MarkerType, MiniMap, Node, Position, ReactFlow } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { SiPostgresql, SiRedis } from 'react-icons/si';
import { LuActivity, LuBoxes, LuCloud, LuGlobe, LuHardDrive, LuNetwork, LuSquare } from 'react-icons/lu';
import { DiagramData, DiagramNode } from '../types';
import { GroupNode } from './GroupNode';
import { healthColor, trafficColor } from '../utils';
import '../diagram.css';

type GroupNodeData = { id: string; label: string; nodeCount: number; x: number; y: number; width: number; height: number };

function iconFor(type: DiagramNode['serviceType']) {
  switch (type) {
    case 'app': return <LuGlobe color="#6ea8fe" />;
    case 'api': return <LuNetwork color="#7ce0c3" />;
    case 'database': return <SiPostgresql color="#73a8ff" />;
    case 'cache': return <SiRedis color="#ff7a7a" />;
    case 'queue': return <LuBoxes color="#ffd27b" />;
    case 'storage': return <LuHardDrive color="#b8a0ff" />;
    case 'monitoring': return <LuActivity color="#4dc4ff" />;
    case 'boundary': return <LuSquare color="#999" />;
    case 'external': return <LuCloud color="#4dc4ff" />;
    default: return <LuGlobe />;
  }
}

function ServiceNode({ data }: { data: { node: DiagramNode } }) {
  const { node } = data;
  return (
    <div className={`service-node c4-${node.level.toLowerCase()}`} style={{ borderColor: healthColor(node.health) }}>
      <Handle type="target" position={Position.Left} />
      <div className="header">
        <div className="title">{iconFor(node.serviceType)} <span>{node.label}</span></div>
        <span className={`badge ${node.health}`}>{node.health.toUpperCase()}</span>
      </div>
      <div style={{ marginTop: 6, display: 'flex', gap: 8, alignItems: 'center' }}>
        <span className="subtle" style={{ fontSize: 12 }}>{node.level}</span>
        {node.drift ? <span className="badge drift">DRIFT</span> : null}
      </div>
      <Handle type="source" position={Position.Right} />
    </div>
  );
}

const nodeTypes = { service: ServiceNode, group: GroupNode };

export function DiagramCanvas({ data, groupNodes = [] }: { data: DiagramData; groupNodes?: GroupNodeData[] }) {
  const groups: Node[] = groupNodes.map((g) => ({
    id: g.id,
    type: 'group',
    position: { x: g.x, y: g.y },
    data: { label: g.label, nodeCount: g.nodeCount },
    style: { width: g.width, height: g.height },
  }));

  const serviceNodes: Node[] = data.nodes.map((node) => ({
    id: node.id,
    type: 'service',
    position: node.position ?? { x: 0, y: 0 },
    data: { node },
  }));

  const nodes = [...groups, ...serviceNodes];

  const edges: Edge[] = data.edges.map((edge) => ({
    id: edge.id,
    source: edge.from,
    target: edge.to,
    markerEnd: { type: MarkerType.ArrowClosed, color: trafficColor(edge.traffic) },
    style: { strokeWidth: 2, stroke: trafficColor(edge.traffic) },
    label: `${Math.round(edge.traffic * 100)}%`,
  }));

  return (
    <div className="diagram-stage">
      <ReactFlow
        fitView
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        minZoom={0.05}
        maxZoom={2}
        defaultViewport={{ x: 0, y: 0, zoom: 0.3 }}
      >
        <Background color="#203357" gap={18} size={1} />
        <MiniMap
          nodeColor={(n) => {
            const nodeData = n.data as { node?: DiagramNode };
            return nodeData.node ? healthColor(nodeData.node.health) : 'var(--border)';
          }}
          nodeStrokeWidth={0}
          pannable
          zoomable
        />
        <Controls />
      </ReactFlow>
    </div>
  );
}
