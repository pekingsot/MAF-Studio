import { Collaboration } from '../../services/collaborationService';

export interface CollaborationAgent {
  id: string;
  agentId: number;
  agent: {
    id: number;
    name: string;
    type: string;
    status: string;
  };
  role: string;
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
