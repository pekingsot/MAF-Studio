import api from './api';

export interface Skill {
  id: string;
  name: string;
  description: string;
  version: string;
  author: string;
  path: string;
  tags: string[];
  dependencies: string[];
  entryPoint?: string;
  runtime?: string;
  parameters: Record<string, string>;
  loadedAt: string;
}

export interface LoadSkillRequest {
  path: string;
}

export interface ExecuteSkillRequest {
  parameters?: Record<string, any>;
}

export const skillService = {
  getAllSkills: async (): Promise<Skill[]> => {
    const response = await api.get<Skill[]>('/skills');
    return response.data;
  },

  loadSkill: async (path: string): Promise<Skill> => {
    const response = await api.post<Skill>('/skills/load', { path });
    return response.data;
  },

  loadAllSkills: async (): Promise<Skill[]> => {
    const response = await api.post<Skill[]>('/skills/load-all');
    return response.data;
  },

  getSkill: async (skillId: string): Promise<Skill> => {
    const response = await api.get<Skill>(`/skills/${skillId}`);
    return response.data;
  },

  unloadSkill: async (skillId: string): Promise<void> => {
    await api.delete(`/skills/${skillId}`);
  },

  executeSkill: async (skillId: string, parameters?: Record<string, any>): Promise<any> => {
    const response = await api.post(`/skills/${skillId}/execute`, { parameters });
    return response.data;
  },
};
