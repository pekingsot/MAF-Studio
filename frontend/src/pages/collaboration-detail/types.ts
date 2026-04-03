import { Collaboration } from '../../services/collaborationService';

export interface CollaborationAgent {
  agentId: number;
  agentName: string;
  agentType?: string;
  agentStatus?: string;
  agentAvatar?: string;
  role?: string; // Agent在工作流中的角色：Manager（协调者）或 Worker（执行者）
  customPrompt?: string; // Agent的自定义提示词（覆盖系统提示词）
  systemPrompt?: string; // Agent的系统提示词
  joinedAt: string;
}

export interface CollaborationTask {
  id: string;
  title: string;
  description?: string;
  status: string;
  createdAt: string;
  completedAt?: string | null;
}

export interface CollaborationDetailData extends Omit<Collaboration, 'agents' | 'tasks'> {
  agents: CollaborationAgent[];
  tasks: CollaborationTask[];
}

export const STATUS_COLOR_MAP: Record<string, string> = {
  Active: 'green',
  Paused: 'orange',
  Completed: 'blue',
  Cancelled: 'red',
};

export const TASK_STATUS_COLOR_MAP: Record<string, string> = {
  Pending: 'default',
  InProgress: 'processing',
  Completed: 'success',
  Failed: 'error',
};

export const AGENT_STATUS_COLOR_MAP: Record<string, string> = {
  Active: 'green',
  Inactive: 'default',
  Busy: 'orange',
  Error: 'red',
};
