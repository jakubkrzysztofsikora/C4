import { describe, expect, it } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useCollapsedGroups } from './useCollapsedGroups';

describe('useCollapsedGroups', () => {
  it('initial state is empty Set', () => {
    const { result } = renderHook(() => useCollapsedGroups());

    expect(result.current.collapsedGroups.size).toBe(0);
  });

  it('toggleGroup adds a group ID', () => {
    const { result } = renderHook(() => useCollapsedGroups());

    act(() => {
      result.current.toggleGroup('group-rg1');
    });

    expect(result.current.collapsedGroups.has('group-rg1')).toBe(true);
  });

  it('toggleGroup removes an already-present group ID', () => {
    const { result } = renderHook(() => useCollapsedGroups());

    act(() => {
      result.current.toggleGroup('group-rg1');
    });
    act(() => {
      result.current.toggleGroup('group-rg1');
    });

    expect(result.current.collapsedGroups.has('group-rg1')).toBe(false);
  });

  it('multiple toggles work correctly', () => {
    const { result } = renderHook(() => useCollapsedGroups());

    act(() => {
      result.current.toggleGroup('group-a');
    });
    act(() => {
      result.current.toggleGroup('group-b');
    });
    act(() => {
      result.current.toggleGroup('group-a');
    });

    expect(result.current.collapsedGroups.has('group-a')).toBe(false);
    expect(result.current.collapsedGroups.has('group-b')).toBe(true);
    expect(result.current.collapsedGroups.size).toBe(1);
  });

  it('produces a new Set reference on each toggle', () => {
    const { result } = renderHook(() => useCollapsedGroups());
    const before = result.current.collapsedGroups;

    act(() => {
      result.current.toggleGroup('group-rg1');
    });

    expect(result.current.collapsedGroups).not.toBe(before);
  });
});
