import { useEffect, useRef } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { createDiagramHubConnection, joinProject, leaveProject } from '../../../shared/api/signalrClient';

type SignalRCallbacks = {
  onHealthOverlayChanged?: (projectId: string, healthJson: string) => void;
  onDiagramUpdated?: (projectId: string, diagramJson: string) => void;
};

export function useSignalR(projectId: string | undefined, callbacks: SignalRCallbacks) {
  const connectionRef = useRef<HubConnection | null>(null);
  const callbacksRef = useRef<SignalRCallbacks>(callbacks);

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

    const startAndJoin = async () => {
      await connection.start();
      await joinProject(connection, projectId);
    };

    void startAndJoin();

    return () => {
      const cleanup = async () => {
        if (connection.state !== HubConnectionState.Disconnected) {
          await leaveProject(connection, projectId);
          await connection.stop();
        }
      };
      void cleanup();
      connectionRef.current = null;
    };
  }, [projectId]);
}
