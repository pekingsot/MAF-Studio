/**
 * 工作流节点类型
 */
export enum NodeType {
  START = 'start',
  AGENT = 'agent',
  AGGREGATOR = 'aggregator',
  CONDITION = 'condition',
  LOOP = 'loop',
  REVIEW = 'review',
}

/**
 * 工作流边类型
 */
export enum EdgeType {
  SEQUENTIAL = 'sequential',
  FAN_OUT = 'fan-out',
  FAN_IN = 'fan-in',
  CONDITIONAL = 'conditional',
  LOOP = 'loop',
  APPROVED = 'approved',
  REJECTED = 'rejected',
}

/**
 * 工作流节点定义
 */
export interface WorkflowNode {
  id: string;
  type: NodeType;
  name: string;
  agentRole?: string;
  agentId?: string;
  inputTemplate?: string;
  condition?: string;
  parameters?: Record<string, any>;
  approvalKeyword?: string;
  rejectTargetNode?: string;
  maxRetries?: number;
  reviewCriteria?: string;
}

/**
 * 工作流边定义
 */
export interface WorkflowEdge {
  type: EdgeType;
  from: string;
  to: string | string[];
  condition?: string;
  description?: string;
}

/**
 * 工作流定义
 */
export interface WorkflowDefinition {
  nodes: WorkflowNode[];
  edges: WorkflowEdge[];
}

/**
 * 参数定义
 */
export interface ParameterDefinition {
  type: 'string' | 'number' | 'boolean' | 'array';
  description?: string;
  default?: string | number | boolean | unknown[] | Record<string, unknown> | null;
  required?: boolean;
}

/**
 * 工作流模板
 */
export interface WorkflowTemplate {
  id: number;
  name: string;
  description?: string;
  category?: string;
  tags: string[];
  workflow: WorkflowDefinition;
  parameters?: Record<string, ParameterDefinition>;
  createdBy?: number;
  isPublic: boolean;
  usageCount: number;
  source: 'manual' | 'magentic' | 'magentic_optimized';
  originalTask?: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * 创建工作流模板请求
 */
export interface CreateWorkflowTemplateRequest {
  name: string;
  description?: string;
  category?: string;
  tags?: string[];
  workflow: WorkflowDefinition;
  parameters?: Record<string, ParameterDefinition>;
  isPublic?: boolean;
  source?: 'manual' | 'magentic' | 'magentic_optimized';
  originalTask?: string;
}

/**
 * 更新工作流模板请求
 */
export interface UpdateWorkflowTemplateRequest {
  name?: string;
  description?: string;
  category?: string;
  tags?: string[];
  workflow?: WorkflowDefinition;
  parameters?: Record<string, ParameterDefinition>;
  isPublic?: boolean;
}

/**
 * 执行工作流模板请求
 */
export interface ExecuteTemplateRequest {
  collaborationId: number;
  input: string;
  parameterValues?: Record<string, any>;
}

/**
 * 生成Magentic计划请求
 */
export interface GenerateMagenticPlanRequest {
  collaborationId: number;
  task: string;
}

/**
 * 生成Magentic计划响应
 */
export interface GenerateMagenticPlanResponse {
  success: boolean;
  workflow?: WorkflowDefinition;
  error?: string;
}

/**
 * 保存Magentic计划请求
 */
export interface SaveMagenticPlanRequest {
  name: string;
  description?: string;
  category?: string;
  tags?: string[];
  workflow: WorkflowDefinition;
  parameters?: Record<string, ParameterDefinition>;
  isPublic?: boolean;
  enableLearning?: boolean;
  originalTask?: string;
}

/**
 * 查找匹配模板请求
 */
export interface FindMatchingTemplateRequest {
  task: string;
}

/**
 * 协作结果
 */
export interface CollaborationResult {
  success: boolean;
  output?: string;
  messages?: ChatMessageDto[];
  error?: string;
  metadata?: Record<string, any>;
}

/**
 * 聊天消息DTO
 */
export interface ChatMessageDto {
  sender: string;
  content: string;
  timestamp: string;
}
