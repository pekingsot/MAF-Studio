import api from './api';

export interface Agent {
  id: string;
  name: string;
  description?: string;
  type: string;
  configuration: string;
  avatar?: string;
  userId?: string;
  llmConfigId?: string;
  llmConfig?: {
    id: string;
    name: string;
    provider: string;
    modelName: string;
  };
  status: 'Inactive' | 'Active' | 'Busy' | 'Error';
  createdAt: string;
  updatedAt?: string;
  lastActiveAt?: string;
}

export interface CreateAgentRequest {
  name: string;
  description?: string;
  type: string;
  configuration?: string;
  avatar?: string;
  llmConfigId?: string;
}

export interface UpdateAgentRequest {
  name?: string;
  description?: string;
  configuration?: string;
  avatar?: string;
  llmConfigId?: string;
}

export const agentService = {
  getAllAgents: async (): Promise<Agent[]> => {
    const response = await api.get<Agent[]>('/agents');
    return response.data;
  },

  getAgentById: async (id: string): Promise<Agent> => {
    const response = await api.get<Agent>(`/agents/${id}`);
    return response.data;
  },

  createAgent: async (request: CreateAgentRequest): Promise<Agent> => {
    const response = await api.post<Agent>('/agents', request);
    return response.data;
  },

  updateAgent: async (id: string, request: UpdateAgentRequest): Promise<Agent> => {
    const response = await api.put<Agent>(`/agents/${id}`, request);
    return response.data;
  },

  deleteAgent: async (id: string): Promise<void> => {
    await api.delete(`/agents/${id}`);
  },

  updateAgentStatus: async (id: string, status: string): Promise<Agent> => {
    const response = await api.patch<Agent>(`/agents/${id}/status`, { status });
    return response.data;
  },
};