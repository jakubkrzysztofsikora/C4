import ELK, { ElkNode, ElkExtendedEdge } from 'elkjs/lib/elk.bundled.js';
import { useEffect, useRef, useState } from 'react';
import { DiagramData, DiagramNode } from '../types';

const elk = new ELK();

const C4_DIMENSIONS: Record<DiagramNode['level'], { width: number; height: number }> = {
  Context: { width: 360, height: 140 },
  Container: { width: 280, height: 100 },
  Component: { width: 220, height: 80 },
  Code: { width: 200, height: 72 },
  Unknown: { width: 220, height: 84 },
};

const ELK_OPTIONS = {
  'elk.algorithm': 'layered',
  'elk.direction': 'RIGHT',
  'elk.spacing.nodeNode': '40',
  'elk.layered.spacing.nodeNodeBetweenLayers': '100',
  'elk.edgeRouting': 'ORTHOGONAL',
};

const GROUP_OPTIONS = {
  'elk.padding': '[top=50,left=20,bottom=20,right=20]',
  'elk.algorithm': 'layered',
  'elk.direction': 'RIGHT',
  'elk.spacing.nodeNode': '30',
  'elk.layered.spacing.nodeNodeBetweenLayers': '80',
};

type LayoutResult = {
  layoutedData: DiagramData;
  groupNodes: Array<{ id: string; label: string; nodeCount: number; x: number; y: number; width: number; height: number }>;
  isLayouting: boolean;
};

function groupByResourceGroup(nodes: DiagramNode[]): Map<string, DiagramNode[]> {
  const groups = new Map<string, DiagramNode[]>();
  for (const node of nodes) {
    const key = node.groupKey || node.resourceGroup || '';
    const list = groups.get(key) ?? [];
    list.push(node);
    groups.set(key, list);
  }
  return groups;
}

function buildElkGraph(nodes: DiagramNode[], edges: DiagramData['edges']): ElkNode {
  const grouped = groupByResourceGroup(nodes);
  const nodeIdSet = new Set(nodes.map((n) => n.id));

  const children: ElkNode[] = [];

  for (const [rg, groupNodes] of grouped) {
    if (rg === '' || groupNodes.length < 2) {
      for (const node of groupNodes) {
        const dim = C4_DIMENSIONS[node.level];
        children.push({
          id: node.id,
          width: dim.width,
          height: dim.height,
        });
      }
    } else {
      const groupId = `group-${rg}`;
      const groupChildren: ElkNode[] = groupNodes.map((node) => {
        const dim = C4_DIMENSIONS[node.level];
        return { id: node.id, width: dim.width, height: dim.height };
      });

      children.push({
        id: groupId,
        children: groupChildren,
        layoutOptions: GROUP_OPTIONS,
      });
    }
  }

  const elkEdges: ElkExtendedEdge[] = edges
    .filter((e) => nodeIdSet.has(e.from) && nodeIdSet.has(e.to))
    .map((e) => ({
      id: e.id,
      sources: [e.from],
      targets: [e.to],
    }));

  return {
    id: 'root',
    children,
    edges: elkEdges,
    layoutOptions: ELK_OPTIONS,
  };
}

function extractPositions(
  layoutResult: ElkNode,
  originalNodes: DiagramNode[],
): { nodes: DiagramNode[]; groups: LayoutResult['groupNodes'] } {
  const posMap = new Map<string, { x: number; y: number }>();
  const groups: LayoutResult['groupNodes'] = [];

  for (const child of layoutResult.children ?? []) {
    if (child.children && child.children.length > 0) {
      const gx = child.x ?? 0;
      const gy = child.y ?? 0;

      const rgName = child.id.replace('group-', '');
      groups.push({
        id: child.id,
        label: rgName,
        nodeCount: child.children.length,
        x: gx,
        y: gy,
        width: child.width ?? 300,
        height: child.height ?? 200,
      });

      for (const grandchild of child.children) {
        posMap.set(grandchild.id, {
          x: gx + (grandchild.x ?? 0),
          y: gy + (grandchild.y ?? 0),
        });
      }
    } else {
      posMap.set(child.id, { x: child.x ?? 0, y: child.y ?? 0 });
    }
  }

  const nodes = originalNodes.map((node) => {
    const pos = posMap.get(node.id);
    return pos ? { ...node, position: pos } : node;
  });

  return { nodes, groups };
}

export function useElkLayout(data: DiagramData): LayoutResult {
  const [result, setResult] = useState<LayoutResult>({
    layoutedData: data,
    groupNodes: [],
    isLayouting: false,
  });
  const versionRef = useRef(0);

  useEffect(() => {
    if (data.nodes.length === 0) {
      setResult((prev) => {
        if (!prev.isLayouting && prev.groupNodes.length === 0 && prev.layoutedData.nodes.length === 0 && prev.layoutedData.edges.length === 0) {
          return prev;
        }
        return { layoutedData: data, groupNodes: [], isLayouting: false };
      });
      return;
    }

    const version = ++versionRef.current;
    setResult((prev) => ({ ...prev, isLayouting: true }));

    const elkGraph = buildElkGraph(data.nodes, data.edges);

    elk.layout(elkGraph).then((layoutResult) => {
      if (version !== versionRef.current) return;
      const { nodes, groups } = extractPositions(layoutResult, data.nodes);
      setResult({
        layoutedData: { nodes, edges: data.edges },
        groupNodes: groups,
        isLayouting: false,
      });
    }).catch(() => {
      if (version !== versionRef.current) return;
      setResult({ layoutedData: data, groupNodes: [], isLayouting: false });
    });
  }, [data]);

  return result;
}
