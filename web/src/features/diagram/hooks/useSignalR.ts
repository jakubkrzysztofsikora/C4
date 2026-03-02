import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { createDiagramHubConnection, joinProject, leaveProject } from '../../../shared/api/signalrClient';

type SignalRCallbacks = {
  onHealthOverlayChanged?: (projectId: string, healthJson: string) => void;
  onDiagramUpdated?: (projectId: string, diagramJson: string) => void;
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
