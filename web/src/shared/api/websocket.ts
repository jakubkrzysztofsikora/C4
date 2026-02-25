export type MessageHandler = (event: MessageEvent<string>) => void;

export class WebSocketManager {
  private readonly socket: WebSocket;

  public constructor(url: string) {
    this.socket = new WebSocket(url);
  }

  public onMessage(handler: MessageHandler) {
    this.socket.addEventListener('message', handler);
    return () => this.socket.removeEventListener('message', handler);
  }

  public close() {
    this.socket.close();
  }
}
