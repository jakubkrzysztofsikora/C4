import dagre from '@dagrejs/dagre';
import { useMemo } from 'react';
import { DiagramData } from '../types';

const NODE_WIDTH = 280;
const NODE_HEIGHT = 100;

export function useGraphLayout(data: DiagramData): DiagramData {
  return useMemo(() => {
    if (data.nodes.length === 0) return data;

    const graph = new dagre.graphlib.Graph();
    graph.setDefaultEdgeLabel(() => ({}));
    graph.setGraph({ rankdir: 'LR', nodesep: 60, ranksep: 120 });

    data.nodes.forEach((node) => {
      graph.setNode(node.id, { width: NODE_WIDTH, height: NODE_HEIGHT });
    });

    data.edges.forEach((edge) => {
      graph.setEdge(edge.from, edge.to);
    });

    dagre.layout(graph);

    const nodes = data.nodes.map((node) => {
      const { x, y } = graph.node(node.id);
      return { ...node, position: { x: x - NODE_WIDTH / 2, y: y - NODE_HEIGHT / 2 } };
    });

    return { ...data, nodes };
  }, [data]);
}
