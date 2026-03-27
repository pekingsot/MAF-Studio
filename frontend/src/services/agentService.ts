import api from './api';

export interface Agent {
  id: string;
  name: string;
  description?: string;
  type: string;
  systemPrompt?: string;
  avatar?: string;
  userId?: string;
  llmConfigId?: string;
  llmModelConfigId?: string;
  llmConfigName?: string;
  primaryModelName?: string;
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
  fallbackModels?: FallbackModel[];
}

export interface FallbackModel {
  llmConfigId: string;
  llmConfigName?: string;
  llmModelConfigId?: string;
  modelName?: string;
  priority: number;
}

export interface FallbackModelRequest {
  llmConfigId: number;
  llmModelConfigId?: number;
  priority: number;
}

export interface CreateAgentRequest {
  name: string;
  description?: string;
  type: string;
  systemPrompt?: string;
  avatar?: string;
  llmConfigId?: number;
  llmModelConfigId?: number;
  fallbackModels?: FallbackModelRequest[];
}

export interface UpdateAgentRequest {
  name?: string;
  description?: string;
  systemPrompt?: string;
  avatar?: string;
  llmConfigId?: number;
  llmModelConfigId?: number;
  fallbackModels?: FallbackModelRequest[];
}

export interface AgentListResponse {
  agents: Agent[];
  agentTypes: AgentType[];
}

export interface AgentType {
  id: string;
  name: string;
  code: string;
  description?: string;
  icon?: string;
  defaultSystemPrompt?: string;
  isEnabled: boolean;
  sortOrder: number;
}

export const agentService = {
  getAllAgents: async (): Promise<AgentListResponse> => {
    const response = await api.get<AgentListResponse>('/agents');
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
