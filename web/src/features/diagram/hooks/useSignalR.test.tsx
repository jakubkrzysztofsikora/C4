import { describe, expect, it, vi, beforeEach, afterEach } from 'vitest';
import { act, createElement } from 'react';
import { createRoot } from 'react-dom/client';
import { HubConnectionState } from '@microsoft/signalr';

declare global {
  var IS_REACT_ACT_ENVIRONMENT: boolean;
}

globalThis.IS_REACT_ACT_ENVIRONMENT = true;

const mockOn = vi.fn();
const mockStart = vi.fn().mockResolvedValue(undefined);
const mockStop = vi.fn().mockResolvedValue(undefined);
const mockInvoke = vi.fn().mockResolvedValue(undefined);

const mockConnection = {
  on: mockOn,
  start: mockStart,
  stop: mockStop,
  invoke: mockInvoke,
  state: HubConnectionState.Connected,
};

vi.mock('../../../shared/api/signalrClient', () => ({
  createDiagramHubConnection: () => mockConnection,
  joinProject: (_conn: unknown, projectId: string) => Promise.resolve(mockInvoke('JoinProject', projectId)),
  leaveProject: (_conn: unknown, projectId: string) => Promise.resolve(mockInvoke('LeaveProject', projectId)),
}));

import { useSignalR } from './useSignalR';

type EventHandler = (...args: unknown[]) => void;

function getRegisteredHandler(eventName: string): EventHandler | undefined {
  const call = (mockOn.mock.calls as Array<[string, EventHandler]>).find(([name]) => name === eventName);
  return call?.[1];
}

type SignalRHarnessProps = {
  projectId: string | undefined;
  onHealthOverlayChanged?: (projectId: string, healthJson: string) => void;
  onDiagramUpdated?: (projectId: string, diagramJson: string) => void;
};

function SignalRHarness({ projectId, onHealthOverlayChanged, onDiagramUpdated }: SignalRHarnessProps) {
  const callbacks = {
    ...(onHealthOverlayChanged !== undefined && { onHealthOverlayChanged }),
    ...(onDiagramUpdated !== undefined && { onDiagramUpdated }),
  };
  useSignalR(projectId, callbacks);
  return createElement('div', null);
}

describe('useSignalR', () => {
  let container: HTMLDivElement;
  let root: ReturnType<typeof createRoot>;

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
    root = createRoot(container);
    mockOn.mockClear();
    mockStart.mockClear();
    mockStop.mockClear();
    mockInvoke.mockClear();
  });

  afterEach(() => {
    document.body.removeChild(container);
  });

  it('connects and joins project on mount', async () => {
    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: 'proj-1' }));
    });

    expect(mockStart).toHaveBeenCalledTimes(1);
    expect(mockInvoke).toHaveBeenCalledWith('JoinProject', 'proj-1');
  });

  it('leaves project and stops connection on unmount', async () => {
    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: 'proj-2' }));
    });

    await act(async () => {
      root.unmount();
    });

    expect(mockInvoke).toHaveBeenCalledWith('LeaveProject', 'proj-2');
    expect(mockStop).toHaveBeenCalledTimes(1);
  });

  it('calls onHealthOverlayChanged callback when event fires', async () => {
    const handler = vi.fn<(projectId: string, healthJson: string) => void>();

    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: 'proj-3', onHealthOverlayChanged: handler }));
    });

    const registeredHandler = getRegisteredHandler('HealthOverlayChanged');
    expect(registeredHandler).toBeDefined();

    act(() => {
      registeredHandler?.('proj-3', '[{"nodeId":"n1","health":"red"}]');
    });

    expect(handler).toHaveBeenCalledWith('proj-3', '[{"nodeId":"n1","health":"red"}]');
  });

  it('calls onDiagramUpdated callback when event fires', async () => {
    const handler = vi.fn<(projectId: string, diagramJson: string) => void>();

    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: 'proj-4', onDiagramUpdated: handler }));
    });

    const registeredHandler = getRegisteredHandler('DiagramUpdated');
    expect(registeredHandler).toBeDefined();

    act(() => {
      registeredHandler?.('proj-4', '{"nodes":[],"edges":[]}');
    });

    expect(handler).toHaveBeenCalledWith('proj-4', '{"nodes":[],"edges":[]}');
  });

  it('does not connect when projectId is undefined', async () => {
    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: undefined }));
    });

    expect(mockStart).not.toHaveBeenCalled();
    expect(mockInvoke).not.toHaveBeenCalled();
  });

  it('registers handlers for both HealthOverlayChanged and DiagramUpdated', async () => {
    await act(async () => {
      root.render(createElement(SignalRHarness, { projectId: 'proj-5' }));
    });

    const registeredEvents = (mockOn.mock.calls as Array<[string, unknown]>).map(([name]) => name);
    expect(registeredEvents).toContain('HealthOverlayChanged');
    expect(registeredEvents).toContain('DiagramUpdated');
  });
});
