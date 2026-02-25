import { useMemo } from 'react';
import { DiagramData, DiagramNode } from '../types';

const NODE_WIDTH = 280;
const NODE_HEIGHT = 160;
const COLUMN_GAP = 60;
const ROW_GAP = 80;

function assignLayers(nodes: DiagramNode[], edges: DiagramData['edges']): Map<string, number> {
  const inDegree = new Map<string, number>(nodes.map(n => [n.id, 0]));
  const successors = new Map<string, string[]>(nodes.map(n => [n.id, []]));

  for (const edge of edges) {
    if (inDegree.has(edge.to)) inDegree.set(edge.to, (inDegree.get(edge.to) ?? 0) + 1);
    if (successors.has(edge.from)) successors.get(edge.from)!.push(edge.to);
  }

  const layer = new Map<string, number>(nodes.map(n => [n.id, 0]));
  const processed = new Set<string>();
  let current = nodes.map(n => n.id).filter(id => inDegree.get(id) === 0);
  current.forEach(id => processed.add(id));

  while (current.length > 0) {
    const next: string[] = [];
    for (const id of current) {
      for (const neighbor of (successors.get(id) ?? [])) {
        layer.set(neighbor, Math.max(layer.get(neighbor) ?? 0, (layer.get(id) ?? 0) + 1));
        if (!processed.has(neighbor)) {
          processed.add(neighbor);
          next.push(neighbor);
        }
      }
    }
    current = next;
  }

  let nextLayer = layer.size > 0 ? Math.max(...layer.values()) + 1 : 0;
  for (const node of nodes) {
    if (!processed.has(node.id)) layer.set(node.id, nextLayer++);
  }

  return layer;
}

export function useGraphLayout(data: DiagramData): DiagramData {
  return useMemo(() => {
    const { nodes, edges } = data;
    if (nodes.length === 0) return data;

    const layerMap = assignLayers(nodes, edges);

    const layerGroups = new Map<number, string[]>();
    for (const [id, layer] of layerMap) {
      if (!layerGroups.has(layer)) layerGroups.set(layer, []);
      layerGroups.get(layer)!.push(id);
    }

    const maxLayerSize = Math.max(...[...layerGroups.values()].map(g => g.length));
    const canvasWidth = maxLayerSize * (NODE_WIDTH + COLUMN_GAP) - COLUMN_GAP;

    const positionMap = new Map<string, { x: number; y: number }>();
    for (const [layer, ids] of layerGroups) {
      const layerWidth = ids.length * (NODE_WIDTH + COLUMN_GAP) - COLUMN_GAP;
      const offsetX = (canvasWidth - layerWidth) / 2;
      ids.forEach((id, i) => {
        positionMap.set(id, {
          x: offsetX + i * (NODE_WIDTH + COLUMN_GAP),
          y: layer * (NODE_HEIGHT + ROW_GAP)
        });
      });
    }

    return {
      ...data,
      nodes: nodes.map(n => ({ ...n, ...positionMap.get(n.id) }))
    };
  }, [data]);
}
