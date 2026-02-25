import { DiagramData } from '../types';

export function useGraphLayout(data: DiagramData): DiagramData {
  return {
    ...data,
    nodes: data.nodes
  };
}
