import { useEffect, useMemo } from 'react';
import { WebSocketManager, type MessageHandler } from '../api/websocket';

export function useWebSocket(url: string, handler: MessageHandler) {
  const manager = useMemo(() => new WebSocketManager(url), [url]);

  useEffect(() => {
    const unsubscribe = manager.onMessage(handler);
    return () => {
      unsubscribe();
      manager.close();
    };
  }, [handler, manager]);
}
