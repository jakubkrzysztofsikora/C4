import { describe, expect, it } from 'vitest';
import { useRenderingStrategy } from './useRenderingStrategy';

describe('useRenderingStrategy', () => {
  it('returns default for nodeCount 0', () => {
    const result = useRenderingStrategy(0);

    expect(result).toBe('default');
  });

  it('returns default for nodeCount 500', () => {
    const result = useRenderingStrategy(500);

    expect(result).toBe('default');
  });

  it('returns optimized for nodeCount 501', () => {
    const result = useRenderingStrategy(501);

    expect(result).toBe('optimized');
  });

  it('returns optimized for nodeCount 2000', () => {
    const result = useRenderingStrategy(2000);

    expect(result).toBe('optimized');
  });

  it('returns aggressive for nodeCount 2001', () => {
    const result = useRenderingStrategy(2001);

    expect(result).toBe('aggressive');
  });

  it('returns aggressive for nodeCount 10000', () => {
    const result = useRenderingStrategy(10000);

    expect(result).toBe('aggressive');
  });
});
