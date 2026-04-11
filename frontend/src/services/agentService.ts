import api from './api';

export interface Agent {
  id: number;
  name: string;
  description?: string;
  type: string;
  typeName?: string;
  systemPrompt?: string;
  avatar?: string;
  userId?: string;
  llmConfig?: {
    id: number;
    name: string;
    provider: string;
    modelName: string;
  };
  status: 'Inactive' | 'Active' | 'Busy' | 'Error';
  createdAt: string;
  updatedAt?: string;
  lastActiveAt?: string;
  llmConfigs?: LlmConfigInfo[];
}

export interface LlmConfigInfo {
  llmConfigId: number;
  llmConfigName: string;
  llmModelConfigId?: number;
  modelName: string;
  isPrimary: boolean;
  priority: number;
  isValid: boolean;
  lastChecked?: string;
  msg: string;
}

export interface CreateAgentRequest {
  name: string;
  description?: string;
  type: string;
  systemPrompt?: string;
  avatar?: string;
  llmConfigId?: number;
  llmModelConfigId?: number;
  llmConfigs?: string;
}

export interface UpdateAgentRequest {
  name?: string;
  description?: string;
  systemPrompt?: string;
  avatar?: string;
  llmConfigId?: number;
  llmModelConfigId?: number;
  llmConfigs?: string;
}

export interface AgentType {
  id: number;
  name: string;
  code: string;
  description?: string;
  icon?: string;
  defaultSystemPrompt?: string;
  isEnabled: boolean;
  sortOrder: number;
}

export const agentService = {
  getAllAgents: async (): Promise<Agent[]> => {
    const response = await api.get<Agent[]>('/agents');
    return response.data;
  },

  getAgentTypes: async (): Promise<AgentType[]> => {
    const response = await api.get<AgentType[]>('/agents/types');
    return response.data;
  },

  getAgentById: async (id: number): Promise<Agent> => {
    const response = await api.get<Agent>(`/agents/${id}`);
    return response.data;
  },

  createAgent: async (request: CreateAgentRequest): Promise<Agent> => {
    const response = await api.post<Agent>('/agents', request);
    return response.data;
  },

  updateAgent: async (id: number, request: UpdateAgentRequest): Promise<Agent> => {
    const response = await api.put<Agent>(`/agents/${id}`, request);
    return response.data;
  },

  deleteAgent: async (id: number): Promise<void> => {
    await api.delete(`/agents/${id}`);
  },

  updateAgentStatus: async (id: number, status: string): Promise<Agent> => {
    const response = await api.patch<Agent>(`/agents/${id}/status`, { status });
    return response.data;
  },
};
