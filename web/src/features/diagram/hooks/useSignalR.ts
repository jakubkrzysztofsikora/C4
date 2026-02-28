import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { createDiagramHubConnection, joinProject, leaveProject } from '../../../shared/api/signalrClient';

type SignalRCallbacks = {
  onHealthOverlayChanged?: (projectId: string, healthJson: string) => void;
  onDiagramUpdated?: (projectId: string, diagramJson: string) => void;
};

type ConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'error';

export function useSignalR(projectId: string | undefined, callbacks: SignalRCallbacks) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbacksRef = useRef<SignalRCallbacks>(callbacks);
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');

  callbacksRef.current = callbacks;

  useEffect(() => {
    if (projectId === undefined) return;

    const connection = createDiagramHubConnection();
    connectionRef.current = connection;

    connection.on('HealthOverlayChanged', (receivedProjectId: string, healthJson: string) => {
      callbacksRef.current.onHealthOverlayChanged?.(receivedProjectId, healthJson);
    });

    connection.on('DiagramUpdated', (receivedProjectId: string, diagramJson: string) => {
      callbacksRef.current.onDiagramUpdated?.(receivedProjectId, diagramJson);
    });

    connection.onclose(() => {
      setStatus('disconnected');
    });

    connection.onreconnecting(() => {
      setStatus('connecting');
    });

    connection.onreconnected(() => {
      setStatus('connected');
      joinProject(connection, projectId).catch(() => {});
    });

    const startAndJoin = async () => {
      setStatus('connecting');
      try {
        await connection.start();
        await joinProject(connection, projectId);
        setStatus('connected');
      } catch (err) {
        console.warn('[SignalR] Connection failed:', err);
        setStatus('error');
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
          // Cleanup errors are non-critical
        }
      };
      void cleanup();
      connectionRef.current = null;
    };
  }, [projectId]);

  return status;
}
