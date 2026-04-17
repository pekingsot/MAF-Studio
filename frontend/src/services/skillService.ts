import api from './api';

export interface AgentSkill {
  id: number;
  agent_id: number;
  skill_name: string;
  skill_content: string;
  enabled: boolean;
  priority: number;
  runtime: string;
  entry_point?: string;
  allowed_tools?: string[];
  created_at: string;
  updated_at?: string;
}

export interface SkillDefinition {
  name: string;
  description: string;
  version?: string;
  author?: string;
  allowed_tools: string[];
  tags: string[];
  permissions?: SkillPermissions;
  inputs: SkillInput[];
  outputs: SkillOutput[];
  runtime?: string;
  instructions?: string;
  instructions_length?: number;
}

export interface SkillPermissions {
  network: boolean;
  filesystem: boolean;
  shell: boolean;
  env?: string[];
}

export interface SkillInput {
  name: string;
  type: string;
  required?: boolean;
  default?: string;
  description?: string;
}

export interface SkillOutput {
  name: string;
  type: string;
  description?: string;
}

export interface SkillTemplate {
  id: number;
  name: string;
  description?: string;
  content: string;
  category?: string;
  tags?: string;
  runtime: string;
  usage_count: number;
  is_official: boolean;
  created_at: string;
  updated_at?: string;
}

export interface AddSkillRequest {
  skill_name: string;
  skill_content: string;
  enabled?: boolean;
  priority?: number;
  runtime?: string;
  entry_point?: string;
  allowed_tools?: string[];
  permissions?: Record<string, boolean>;
  parameters?: Record<string, string>;
}

export interface UpdateSkillRequest {
  skill_content?: string;
  enabled?: boolean;
  priority?: number;
  runtime?: string;
  entry_point?: string;
  allowed_tools?: string[];
  permissions?: Record<string, boolean>;
}

export interface CreateTemplateRequest {
  name: string;
  description?: string;
  content: string;
  category?: string;
  tags?: string;
  runtime?: string;
}

export const skillService = {
  getAgentSkills: async (agentId: number): Promise<AgentSkill[]> => {
    const response = await api.get(`/skills/agent/${agentId}`);
    return response.data?.data ?? [];
  },

  getEnabledAgentSkills: async (agentId: number): Promise<any[]> => {
    const response = await api.get(`/skills/agent/${agentId}/enabled`);
    return response.data?.data ?? [];
  },

  addSkillToAgent: async (agentId: number, request: AddSkillRequest): Promise<AgentSkill> => {
    const response = await api.post(`/skills/agent/${agentId}`, request);
    return response.data?.data;
  },

  updateSkill: async (agentId: number, skillId: number, request: UpdateSkillRequest): Promise<AgentSkill> => {
    const response = await api.put(`/skills/agent/${agentId}/${skillId}`, request);
    return response.data?.data;
  },

  deleteSkill: async (agentId: number, skillId: number): Promise<void> => {
    await api.delete(`/skills/agent/${agentId}/${skillId}`);
  },

  addSkillFromTemplate: async (agentId: number, templateId: number, skillName?: string, priority?: number): Promise<AgentSkill> => {
    const response = await api.post(`/skills/agent/${agentId}/from-template/${templateId}`, {
      skill_name: skillName,
      priority,
    });
    return response.data?.data;
  },

  getTemplates: async (category?: string): Promise<SkillTemplate[]> => {
    const params = category ? { category } : {};
    const response = await api.get('/skills/templates', { params });
    return response.data?.data ?? [];
  },

  getTemplate: async (id: number): Promise<SkillTemplate> => {
    const response = await api.get(`/skills/templates/${id}`);
    return response.data?.data;
  },

  createTemplate: async (request: CreateTemplateRequest): Promise<SkillTemplate> => {
    const response = await api.post('/skills/templates', request);
    return response.data?.data;
  },

  parseSkillContent: async (content: string): Promise<SkillDefinition> => {
    const response = await api.post('/skills/parse', { content });
    return response.data?.data;
  },
};
