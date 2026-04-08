import api from './api';
import type {
  WorkflowTemplate,
  CreateWorkflowTemplateRequest,
  UpdateWorkflowTemplateRequest,
  ExecuteTemplateRequest,
  GenerateMagenticPlanRequest,
  GenerateMagenticPlanResponse,
  SaveMagenticPlanRequest,
  FindMatchingTemplateRequest,
  CollaborationResult,
} from '../types/workflow-template';

/**
 * 工作流模板API服务
 */
export const workflowTemplateApi = {
  /**
   * 获取所有工作流模板
   */
  getAll: async (isPublic?: boolean, category?: string): Promise<WorkflowTemplate[]> => {
    const params = new URLSearchParams();
    if (isPublic !== undefined) params.append('isPublic', String(isPublic));
    if (category) params.append('category', category);
    
    const response = await api.get(`/workflow-templates?${params.toString()}`);
    return response.data;
  },

  /**
   * 根据ID获取工作流模板
   */
  getById: async (id: number): Promise<WorkflowTemplate> => {
    const response = await api.get(`/workflow-templates/${id}`);
    return response.data;
  },

  /**
   * 搜索工作流模板
   */
  search: async (keyword: string, tags?: string[]): Promise<WorkflowTemplate[]> => {
    const params = new URLSearchParams();
    params.append('keyword', keyword);
    if (tags && tags.length > 0) params.append('tags', tags.join(','));
    
    const response = await api.get(`/workflow-templates/search?${params.toString()}`);
    return response.data;
  },

  /**
   * 创建工作流模板
   */
  create: async (request: CreateWorkflowTemplateRequest): Promise<WorkflowTemplate> => {
    const response = await api.post('/workflow-templates', request);
    return response.data;
  },

  /**
   * 更新工作流模板
   */
  update: async (id: number, request: UpdateWorkflowTemplateRequest): Promise<WorkflowTemplate> => {
    const response = await api.put(`/workflow-templates/${id}`, request);
    return response.data;
  },

  /**
   * 删除工作流模板
   */
  delete: async (id: number): Promise<void> => {
    await api.delete(`/workflow-templates/${id}`);
  },

  /**
   * 执行工作流模板
   */
  execute: async (id: number, request: ExecuteTemplateRequest): Promise<CollaborationResult> => {
    const response = await api.post(`/workflow-templates/${id}/execute`, request);
    return response.data;
  },

  /**
   * 生成Magentic计划
   */
  generateMagenticPlan: async (request: GenerateMagenticPlanRequest): Promise<GenerateMagenticPlanResponse> => {
    const response = await api.post('/workflow-templates/magentic/generate', request);
    return response.data;
  },

  /**
   * 保存Magentic计划为模板
   */
  saveMagenticPlan: async (request: SaveMagenticPlanRequest): Promise<WorkflowTemplate> => {
    const response = await api.post('/workflow-templates/magentic/save', request);
    return response.data;
  },

  /**
   * 查找匹配的模板
   */
  findMatchingTemplate: async (request: FindMatchingTemplateRequest): Promise<WorkflowTemplate | null> => {
    const response = await api.post('/workflow-templates/match', request);
    return response.data;
  },
};
