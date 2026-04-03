import api from './api';

export interface AgentRuntimeStatus {
  agentId: number;
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
  getStatus: async (agentId: number): Promise<AgentRuntimeStatus> => {
    const response = await api.get<AgentRuntimeStatus>(`/agentruntime/${agentId}/status`);
    return response.data;
  },

  activate: async (agentId: number): Promise<AgentRuntimeStatus> => {
    const response = await api.post<AgentRuntimeStatus>(`/agentruntime/${agentId}/activate`);
    return response.data;
  },

  test: async (agentId: number, input?: string, llmConfigId?: number, llmModelConfigId?: number): Promise<AgentTestResponse> => {
    const response = await api.post<AgentTestResponse>(`/agentruntime/${agentId}/test`, { 
      input,
      llmConfigId,
      llmModelConfigId
    });
    return response.data;
  },
};
