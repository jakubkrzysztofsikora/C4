import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';
const TOKEN_STORAGE_KEY = 'c4_token';

export function createDiagramHubConnection(): HubConnection {
  const token = localStorage.getItem(TOKEN_STORAGE_KEY);
  return new HubConnectionBuilder()
    .withUrl(`${API_BASE_URL}/hubs/diagram`, {
      accessTokenFactory: () => token ?? ''
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build();
}

export async function joinProject(connection: HubConnection, projectId: string): Promise<void> {
  await connection.invoke('JoinProject', projectId);
}

export async function leaveProject(connection: HubConnection, projectId: string): Promise<void> {
  await connection.invoke('LeaveProject', projectId);
}
