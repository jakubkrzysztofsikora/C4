import { memo, useMemo, useCallback } from 'react';
import { Background, Controls, Edge, Handle, MarkerType, MiniMap, Node, Position, ReactFlow, useStore } from '@xyflow/react';
import '@xyflow/react/dist/style.css';
import { SiPostgresql, SiRedis } from 'react-icons/si';
import { LuActivity, LuBoxes, LuCloud, LuGlobe, LuHardDrive, LuNetwork, LuSquare } from 'react-icons/lu';
import { DiagramData, DiagramNode } from '../types';
import { GroupNode } from './GroupNode';
import { healthColor, riskColor, trafficColor } from '../utils';
import { useRenderingStrategy } from '../hooks/useRenderingStrategy';
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

type LodTier = 'dot' | 'compact' | 'full';

type OverlayMode = 'none' | 'threat' | 'cost' | 'security';

function costColor(cost: number | undefined): string {
  if (cost === undefined) return '#64748b';
  if (cost >= 1.0) return '#b91c1c';
  if (cost >= 0.35) return '#b45309';
  return '#2e8f5e';
}

function securityColor(severity: DiagramNode['securitySeverity']): string {
  if (severity === 'critical') return '#7f1d1d';
  if (severity === 'high') return '#b91c1c';
  if (severity === 'medium') return '#b45309';
  if (severity === 'low') return '#2e8f5e';
  return '#6b7280';
}

function resolveBorderColor(node: DiagramNode, overlayMode: OverlayMode, telemetryKnown: boolean): string {
  if (overlayMode === 'threat') return riskColor(node.riskLevel);
  if (overlayMode === 'cost') return costColor(node.hourlyCostUsd);
  if (overlayMode === 'security') return securityColor(node.securitySeverity);
  return telemetryKnown ? healthColor(node.health) : '#64748b';
}

const ServiceNode = memo(function ServiceNode({ data }: { data: { node: DiagramNode; overlayMode: OverlayMode } }) {
  const { node, overlayMode } = data;
  const telemetryKnown = node.telemetryStatus === 'known';
  const borderColor = resolveBorderColor(node, overlayMode, telemetryKnown);

  const lodTier: LodTier = useStore((s) => {
    const z = s.transform[2];
    if (z < 0.2) return 'dot';
    if (z < 0.5) return 'compact';
    return 'full';
  });

  const title = useMemo(() => [
    `Name: ${node.label}`,
    `C4 level: ${node.level}`,
    `Service type: ${node.serviceType}`,
    `Technology: ${node.technology ?? 'unknown'}`,
    `Environment: ${node.environment ?? 'unknown'}`,
    `Domain: ${node.domain ?? 'General'}`,
    (node.tags ?? []).length > 0 ? `Tags: ${(node.tags ?? []).join(', ')}` : undefined,
    `Classification: ${node.classificationSource ?? 'n/a'} (${(node.classificationConfidence ?? 0).toFixed(2)})`,
    typeof node.securityFindingCount === 'number' ? `Security findings: ${node.securityFindingCount}` : undefined,
    node.securitySeverity ? `Security severity: ${node.securitySeverity}` : undefined,
    `Telemetry: ${node.telemetryStatus ?? 'unknown'}`,
    typeof node.requestRate === 'number' ? `Request rate: ${node.requestRate.toFixed(2)} rps` : undefined,
    typeof node.errorRate === 'number' ? `Error rate: ${(node.errorRate * 100).toFixed(2)}%` : undefined,
    typeof node.p95LatencyMs === 'number' ? `p95 latency: ${node.p95LatencyMs.toFixed(0)}ms` : undefined,
    typeof node.hourlyCostUsd === 'number' ? `Estimated cost: $${node.hourlyCostUsd.toFixed(2)}/hr` : undefined,
    node.riskLevel !== undefined ? `Risk: ${node.riskLevel}` : undefined,
    node.externalResourceId !== undefined ? `Resource: ${node.externalResourceId}` : undefined,
  ].filter(Boolean).join('\n'), [node]);

  if (lodTier === 'dot') {
    return (
      <div className="service-node-dot" style={{ background: borderColor }}>
        <Handle type="target" position={Position.Left} />
        <Handle type="source" position={Position.Right} />
      </div>
    );
  }

  if (lodTier === 'compact') {
    return (
      <div
        className={`service-node-compact c4-${node.level.toLowerCase()}${node.diffStatus === 'added' ? ' diff-added' : ''}${node.diffStatus === 'removed' ? ' diff-removed' : ''}`}
        style={{ borderColor }}
      >
        <Handle type="target" position={Position.Left} />
        <div className="header">
          <div className="title">{iconFor(node.serviceType)} <span>{node.label}</span></div>
          <span className={`badge ${telemetryKnown ? node.health : 'unknown'}`}>
            {telemetryKnown ? node.health.toUpperCase() : 'UNKNOWN'}
          </span>
        </div>
        <Handle type="source" position={Position.Right} />
      </div>
    );
  }

  return (
    <div
      className={`service-node c4-${node.level.toLowerCase()}${node.diffStatus === 'added' ? ' diff-added' : ''}${node.diffStatus === 'removed' ? ' diff-removed' : ''}`}
      style={{ borderColor }}
      title={title}
    >
      <Handle type="target" position={Position.Left} />
      <div className="header">
        <div className="title">{iconFor(node.serviceType)} <span>{node.label}</span></div>
        <span className={`badge ${telemetryKnown ? node.health : 'unknown'}`}>{telemetryKnown ? node.health.toUpperCase() : 'UNKNOWN'}</span>
      </div>
      <div style={{ marginTop: 6, display: 'flex', gap: 8, alignItems: 'center', flexWrap: 'wrap' }}>
        <span className="subtle" style={{ fontSize: 12 }}>{node.level}</span>
        {node.drift ? <span className="badge drift">DRIFT</span> : null}
        {overlayMode === 'security' ? (
          <span
            className="badge"
            style={{ borderColor: securityColor(node.securitySeverity), color: securityColor(node.securitySeverity) }}
          >
            SEC {(node.securitySeverity ?? 'none').toUpperCase()}
          </span>
        ) : null}
        {node.riskLevel !== undefined ? (
          <span className="badge" style={{ borderColor: riskColor(node.riskLevel), color: riskColor(node.riskLevel) }}>
            RISK {node.riskLevel.toUpperCase()}
          </span>
        ) : null}
      </div>
      <Handle type="source" position={Position.Right} />
    </div>
  );
});

const nodeTypes = { service: ServiceNode, group: GroupNode };

type RenderingStrategy = 'default' | 'optimized' | 'aggressive';

export function DiagramCanvas({
  data,
  groupNodes = [],
  overlayMode = 'none',
  isLayouting = false,
  collapsedGroups,
  onToggleGroup,
}: {
  data: DiagramData;
  groupNodes?: GroupNodeData[];
  overlayMode?: OverlayMode;
  isLayouting?: boolean;
  collapsedGroups: Set<string>;
  onToggleGroup: (groupId: string) => void;
}) {
  const strategy: RenderingStrategy = useRenderingStrategy(data.nodes.length);
  const toggleGroup = onToggleGroup;

  const collapsedGroupNodeIds = useMemo<Set<string>>(() => {
    const result = new Set<string>();
    for (const node of data.nodes) {
      const rg = node.groupKey || node.resourceGroup || '';
      if (rg !== '' && collapsedGroups.has(`group-${rg}`)) {
        result.add(node.id);
      }
    }
    return result;
  }, [data.nodes, collapsedGroups]);

  const nodes = useMemo(() => {
    const groups: Node[] = groupNodes.map((g) => ({
      id: g.id,
      type: 'group',
      position: { x: g.x, y: g.y },
      data: {
        label: g.label,
        nodeCount: g.nodeCount,
        collapsed: collapsedGroups.has(g.id),
        onToggle: toggleGroup,
        groupId: g.id,
      },
      style: { width: g.width, height: g.height },
    }));

    const serviceNodes: Node[] = data.nodes
      .filter((node) => !collapsedGroupNodeIds.has(node.id))
      .map((node) => ({
        id: node.id,
        type: 'service',
        position: node.position ?? { x: 0, y: 0 },
        data: { node, overlayMode },
      }));

    return [...groups, ...serviceNodes];
  }, [groupNodes, data.nodes, overlayMode, collapsedGroups, collapsedGroupNodeIds, toggleGroup]);

  const canvasLod: 'minimal' | 'normal' = useStore((s) => {
    const z = s.transform[2];
    return z < 0.3 ? 'minimal' : 'normal';
  });

  const edges = useMemo(() => {
    const hideLabels = canvasLod === 'minimal' || data.edges.length > 500;

    return data.edges
      .filter((edge) => !collapsedGroupNodeIds.has(edge.from) && !collapsedGroupNodeIds.has(edge.to))
      .map((edge) => {
        const stroke = trafficColor(edge.traffic, edge.trafficState);
        const trafficLabel = edge.trafficLabel ?? (edge.trafficState === 'unknown' ? 'N/A' : `${Math.round(edge.traffic * 100)}%`);

        if (hideLabels) {
          return {
            id: edge.id,
            source: edge.from,
            target: edge.to,
            style: { strokeWidth: 1, stroke },
          };
        }

        const title = [
          `Traffic state: ${edge.trafficState ?? 'unknown'}`,
          `Traffic score: ${trafficLabel}`,
          typeof edge.requestRate === 'number' ? `Request rate: ${edge.requestRate.toFixed(2)} rps` : undefined,
          typeof edge.errorRate === 'number' ? `Error rate: ${(edge.errorRate * 100).toFixed(2)}%` : undefined,
          typeof edge.p95LatencyMs === 'number' ? `p95 latency: ${edge.p95LatencyMs.toFixed(0)}ms` : undefined,
          edge.protocol ? `Protocol: ${edge.protocol}` : undefined,
        ].filter(Boolean).join(' | ');

        return {
          id: edge.id,
          source: edge.from,
          target: edge.to,
          markerEnd: { type: MarkerType.ArrowClosed, color: stroke },
          style: {
            strokeWidth: edge.diffStatus === 'added' || edge.diffStatus === 'removed' ? 3 : 2,
            stroke,
            strokeDasharray: edge.diffStatus === 'removed' ? '6 4' : edge.telemetrySource === 'service-health.derived' ? '5 3' : undefined,
          },
          label: trafficLabel,
          data: { title },
          ariaLabel: title,
        };
      });
  }, [data.edges, canvasLod, collapsedGroupNodeIds]);

  const miniMapNodeColor = useCallback((n: Node) => {
    const nodeData = n.data as { node?: DiagramNode };
    if (!nodeData.node) return 'var(--border)';
    const telemetryKnown = nodeData.node.telemetryStatus === 'known';
    return resolveBorderColor(nodeData.node, overlayMode, telemetryKnown);
  }, [overlayMode]);

  return (
    <div className="diagram-stage">
      {isLayouting && (
        <div className="layout-loading-overlay">
          <span className="layout-loading-text">Computing layout…</span>
        </div>
      )}
      <ReactFlow
        fitView
        nodes={nodes}
        edges={edges}
        nodeTypes={nodeTypes}
        onlyRenderVisibleElements={strategy !== 'default'}
        minZoom={0.05}
        maxZoom={2}
        defaultViewport={{ x: 0, y: 0, zoom: 0.3 }}
      >
        <Background color="#203357" gap={18} size={1} />
        {strategy !== 'aggressive' && (
          <MiniMap
            nodeColor={miniMapNodeColor}
            nodeStrokeWidth={0}
            pannable
            zoomable
          />
        )}
        <Controls />
      </ReactFlow>
    </div>
  );
}
