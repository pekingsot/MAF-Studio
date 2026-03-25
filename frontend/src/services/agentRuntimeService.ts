import api from './api';

export interface AgentRuntimeStatus {
  agentId: string;
  state: 'Uninitialized' | 'Ready' | 'Busy' | 'Sleeping' | 'Error';
  lastActiveTime?: string;
  taskCount: number;
  lastError?: string;
  isAlive: boolean;
}

export interface AgentTestResponse {
  success: boolean;
  message: string;
  response?: string;
  latencyMs: number;
  state?: string;
}

export const agentRuntimeService = {
  getStatus: async (agentId: string): Promise<AgentRuntimeStatus> => {
    const response = await api.get<AgentRuntimeStatus>(`/agentruntime/${agentId}/status`);
    return response.data;
  },

  activate: async (agentId: string): Promise<AgentRuntimeStatus> => {
    const response = await api.post<AgentRuntimeStatus>(`/agentruntime/${agentId}/activate`);
    return response.data;
  },

  sleep: async (agentId: string): Promise<AgentRuntimeStatus> => {
    const response = await api.post<AgentRuntimeStatus>(`/agentruntime/${agentId}/sleep`);
    return response.data;
  },

  destroy: async (agentId: string): Promise<AgentRuntimeStatus> => {
    const response = await api.post<AgentRuntimeStatus>(`/agentruntime/${agentId}/destroy`);
    return response.data;
  },

  test: async (agentId: string, input?: string): Promise<AgentTestResponse> => {
    const response = await api.post<AgentTestResponse>(`/agentruntime/${agentId}/test`, { input });
    return response.data;
  },

  getActiveAgents: async (): Promise<AgentRuntimeStatus[]> => {
    const response = await api.get<AgentRuntimeStatus[]>('/agentruntime/active');
    return response.data;
  },
};
