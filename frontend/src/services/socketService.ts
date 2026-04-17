import { io, Socket } from 'socket.io-client';

const SOCKET_URL = process.env.REACT_APP_SOCKET_URL || 'http://localhost:5000';

class SocketService {
  private socket: Socket | null = null;

  connect(): Socket {
    if (!this.socket) {
      this.socket = io(SOCKET_URL, {
        transports: ['websocket'],
        reconnection: true,
      });

      this.socket.on('connect', () => {
        console.log('Connected to server');
      });

      this.socket.on('disconnect', () => {
        console.log('Disconnected from server');
      });

      this.socket.on('connect_error', (error) => {
        console.error('Connection error:', error);
      });
    }

    return this.socket;
  }

  disconnect(): void {
    if (this.socket) {
      this.socket.disconnect();
      this.socket = null;
    }
  }

  getSocket(): Socket | null {
    return this.socket;
  }

  joinAgentGroup(agentId: number): void {
    if (this.socket) {
      this.socket.emit('JoinAgentGroup', agentId);
    }
  }

  leaveAgentGroup(agentId: number): void {
    if (this.socket) {
      this.socket.emit('LeaveAgentGroup', agentId);
    }
  }

  sendMessage(fromAgentId: number, toAgentId: number, content: string, type: string): void {
    if (this.socket) {
      this.socket.emit('SendMessage', fromAgentId, toAgentId, content, type);
    }
  }

  broadcastMessage(fromAgentId: number, content: string): void {
    if (this.socket) {
      this.socket.emit('BroadcastMessage', fromAgentId, content);
    }
  }

  updateAgentStatus(agentId: number, status: string): void {
    if (this.socket) {
      this.socket.emit('UpdateAgentStatus', agentId, status);
    }
  }

  onMessage(callback: (message: Record<string, unknown>) => void): void {
    if (this.socket) {
      this.socket.on('ReceiveMessage', callback);
    }
  }

  onAgentJoined(callback: (agentId: number) => void): void {
    if (this.socket) {
      this.socket.on('AgentJoined', callback);
    }
  }

  onAgentLeft(callback: (agentId: number) => void): void {
    if (this.socket) {
      this.socket.on('AgentLeft', callback);
    }
  }

  onAgentStatusUpdated(callback: (agent: Record<string, unknown>) => void): void {
    if (this.socket) {
      this.socket.on('AgentStatusUpdated', callback);
    }
  }

  onBroadcastReceived(callback: (fromAgentId: number, content: string) => void): void {
    if (this.socket) {
      this.socket.on('BroadcastReceived', callback);
    }
  }
}

export default new SocketService();