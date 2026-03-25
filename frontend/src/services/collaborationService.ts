import api from './api';

export interface Collaboration {
  id: string;
  name: string;
  description?: string;
  path?: string;
  gitRepositoryUrl?: string;
  gitBranch?: string;
  gitUsername?: string;
  gitEmail?: string;
  status: 'Active' | 'Paused' | 'Completed' | 'Cancelled';
  createdAt: string;
  updatedAt?: string;
  agents: CollaborationAgent[];
  tasks: CollaborationTask[];
}

export interface CollaborationAgent {
  id: string;
  collaborationId: string;
  agentId: string;
  role?: string;
  joinedAt: string;
  agent: {
    id: string;
    name: string;
    type: string;
    status: string;
  };
}

export interface CollaborationTask {
  id: string;
  collaborationId: string;
  title: string;
  description?: string;
  status: 'Pending' | 'InProgress' | 'Completed' | 'Failed';
  createdAt: string;
  completedAt?: string;
}

export interface CreateCollaborationRequest {
  name: string;
  description?: string;
  path?: string;
  gitRepositoryUrl?: string;
  gitBranch?: string;
  gitUsername?: string;
  gitEmail?: string;
  gitAccessToken?: string;
}

export interface AddAgentRequest {
  agentId: string;
  role?: string;
}

export interface CreateTaskRequest {
  title: string;
  description?: string;
}

export const collaborationService = {
  getAllCollaborations: async (): Promise<Collaboration[]> => {
    const response = await api.get<Collaboration[]>('/collaborations');
    return response.data;
  },

  getCollaborationById: async (id: string): Promise<Collaboration> => {
    const response = await api.get<Collaboration>(`/collaborations/${id}`);
    return response.data;
  },

  createCollaboration: async (request: CreateCollaborationRequest): Promise<Collaboration> => {
    const response = await api.post<Collaboration>('/collaborations', request);
    return response.data;
  },

  updateCollaboration: async (id: string, request: CreateCollaborationRequest): Promise<Collaboration> => {
    const response = await api.put<Collaboration>(`/collaborations/${id}`, request);
    return response.data;
  },

  deleteCollaboration: async (id: string): Promise<void> => {
    await api.delete(`/collaborations/${id}`);
  },

  addAgentToCollaboration: async (id: string, request: AddAgentRequest): Promise<Collaboration> => {
    const response = await api.post<Collaboration>(`/collaborations/${id}/agents`, request);
    return response.data;
  },

  removeAgentFromCollaboration: async (id: string, agentId: string): Promise<void> => {
    await api.delete(`/collaborations/${id}/agents/${agentId}`);
  },

  createTask: async (id: string, request: CreateTaskRequest): Promise<CollaborationTask> => {
    const response = await api.post<CollaborationTask>(`/collaborations/${id}/tasks`, request);
    return response.data;
  },

  updateTaskStatus: async (taskId: string, status: string): Promise<CollaborationTask> => {
    const response = await api.patch<CollaborationTask>(`/collaborations/tasks/${taskId}/status`, { status });
    return response.data;
  },

  deleteTask: async (taskId: string): Promise<void> => {
    await api.delete(`/collaborations/tasks/${taskId}`);
  },
};