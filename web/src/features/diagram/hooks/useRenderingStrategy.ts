type RenderingStrategy = 'default' | 'optimized' | 'aggressive';

export function useRenderingStrategy(nodeCount: number): RenderingStrategy {
  if (nodeCount > 2000) return 'aggressive';
  if (nodeCount > 500) return 'optimized';
  return 'default';
}
