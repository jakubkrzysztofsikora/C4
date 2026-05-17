import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { createDiagramHubConnection, joinProject, leaveProject } from '../../../shared/api/signalrClient';

export type GraphDeltaPayload = {
  readonly addedNodes: ReadonlyArray<GraphDeltaNodePayload>;
  readonly removedNodeIds: ReadonlyArray<string>;
  readonly updatedNodes: ReadonlyArray<GraphDeltaNodePayload>;
  readonly addedEdges: ReadonlyArray<GraphDeltaEdgePayload>;
  readonly removedEdgeIds: ReadonlyArray<string>;
};

export type GraphDeltaNodePayload = {
  readonly id: string;
  readonly name: string;
  readonly level: string;
  readonly health: string | null;
  readonly serviceType: string | null;
  readonly technology: string | null;
  readonly resourceGroup: string | null;
  readonly environment: string | null;
  readonly domain: string | null;
};

export type GraphDeltaEdgePayload = {
  readonly id: string;
  readonly sourceNodeId: string;
  readonly targetNodeId: string;
  readonly protocol: string | null;
  readonly trafficState: string | null;
};

type SignalRCallbacks = {
  onHealthOverlayChanged?: (projectId: string, healthJson: string) => void;
  onDiagramUpdated?: (projectId: string, diagramJson: string) => void;
  onGraphDelta?: (projectId: string, delta: GraphDeltaPayload) => void;
};

export type SignalRConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export type SignalRState = {
  status: SignalRConnectionStatus;
  lastConnectedAt?: number;
  lastMessageAt?: number;
  lastError: string | undefined;
};

export function useSignalR(projectId: string | undefined, callbacks: SignalRCallbacks): SignalRState {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbacksRef = useRef<SignalRCallbacks>(callbacks);
  const [state, setState] = useState<SignalRState>({ status: 'disconnected', lastError: undefined });

  callbacksRef.current = callbacks;

  useEffect(() => {
    if (projectId === undefined) return;

    const connection = createDiagramHubConnection();
    connectionRef.current = connection;

    connection.on('HealthOverlayChanged', (receivedProjectId: string, healthJson: string) => {
      setState((prev) => ({ ...prev, lastMessageAt: Date.now() }));
      callbacksRef.current.onHealthOverlayChanged?.(receivedProjectId, healthJson);
    });

    connection.on('DiagramUpdated', (receivedProjectId: string, diagramJson: string) => {
      setState((prev) => ({ ...prev, lastMessageAt: Date.now() }));
      callbacksRef.current.onDiagramUpdated?.(receivedProjectId, diagramJson);
    });

    connection.on('GraphDelta', (receivedProjectId: string, deltaJson: string) => {
      setState((prev) => ({ ...prev, lastMessageAt: Date.now() }));
      try {
        const delta = JSON.parse(deltaJson) as GraphDeltaPayload;
        callbacksRef.current.onGraphDelta?.(receivedProjectId, delta);
      } catch {
        // Malformed delta — ignore silently; DiagramUpdated fallback covers this case.
      }
    });

    connection.onclose((error) => {
      setState((prev) => ({
        ...prev,
        status: 'disconnected',
        lastError: error?.message,
      }));
    });

    connection.onreconnecting((error) => {
      setState((prev) => ({
        ...prev,
        status: 'connecting',
        lastError: error?.message,
      }));
    });

    connection.onreconnected(() => {
      setState((prev) => ({
        ...prev,
        status: 'connected',
        lastConnectedAt: Date.now(),
      }));
      joinProject(connection, projectId).catch(() => {});
    });

    const startAndJoin = async () => {
      setState((prev) => ({ ...prev, status: 'connecting' }));
      try {
        await connection.start();
        await joinProject(connection, projectId);
        setState((prev) => ({
          ...prev,
          status: 'connected',
          lastConnectedAt: Date.now(),
        }));
      } catch (err) {
        const message = err instanceof Error ? err.message : 'SignalR connection failed';
        setState((prev) => ({ ...prev, status: 'error', lastError: message }));
      }
    };

    void startAndJoin();

    return () => {
      const cleanup = async () => {
        try {
          if (connection.state !== HubConnectionState.Disconnected) {
            await leaveProject(connection, projectId);
            await connection.stop();
          }
        } catch {
          // Cleanup errors are non-critical.
        }
      };
      void cleanup();
      connectionRef.current = null;
    };
  }, [projectId]);

  return state;
}
