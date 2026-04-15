import api from './api';
import type { WorkflowDefinition } from '../types/workflow-template';

export interface ChatMessageDto {
  sender: string;
  content: string;
  role?: string;
  timestamp: string;
  metadata?: Record<string, any>;
}

export interface CollaborationResult {
  success: boolean;
  output: string;
  messages: ChatMessageDto[];
  error?: string;
  metadata?: Record<string, any>;
}

export interface ReviewIterativeParameters {
  maxIterations?: number;
  reviewCriteria?: string;
  approvalKeyword?: string;
  saveVersions?: boolean;
}

export interface GroupChatParameters {
  orchestrationMode: 'roundRobin' | 'manager' | 'intelligent';
  maxIterations?: number;
}

export interface GenerateMagenticPlanResponse {
  success: boolean;
  workflow?: WorkflowDefinition;
  error?: string;
}

async function consumeSSE(
  url: string,
  body: object,
  onMessage: (message: ChatMessageDto) => void
): Promise<void> {
  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${localStorage.getItem('token')}`,
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    throw new Error(`SSE request failed: ${response.status}`);
  }

  const reader = response.body?.getReader();
  const decoder = new TextDecoder();

  if (!reader) {
    throw new Error('No reader available');
  }

  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    const lines = buffer.split('\n');
    buffer = lines.pop() || '';

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.substring(6);
        if (data.trim()) {
          try {
            const message = JSON.parse(data) as ChatMessageDto;
            onMessage(message);
          } catch (e) {
            console.error('Failed to parse SSE message:', data);
          }
        }
      }
    }
  }
}

export const collaborationWorkflowService = {
  executeConcurrent: async (collaborationId: number, input: string): Promise<CollaborationResult> => {
    const response = await api.post<CollaborationResult>(
      `/collaborationworkflow/${collaborationId}/concurrent`,
      { input }
    );
    return response.data;
  },

  executeHandoffs: async (collaborationId: number, input: string): Promise<CollaborationResult> => {
    const response = await api.post<CollaborationResult>(
      `/collaborationworkflow/${collaborationId}/handoffs`,
      { input }
    );
    return response.data;
  },

  executeGroupChat: async (
    collaborationId: number,
    input: string,
    parameters?: GroupChatParameters,
    onMessage?: (message: ChatMessageDto) => void
  ): Promise<void> => {
    const url = `${api.defaults.baseURL}/collaborationworkflow/${collaborationId}/groupchat`;
    await consumeSSE(url, { input, parameters }, onMessage || (() => {}));
  },

  executeReviewIterative: async (
    collaborationId: number,
    input: string,
    parameters?: ReviewIterativeParameters
  ): Promise<CollaborationResult> => {
    const response = await api.post<CollaborationResult>(
      `/collaborationworkflow/${collaborationId}/review-iterative`,
      { input, parameters }
    );
    return response.data;
  },

  generateMagenticPlan: async (
    collaborationId: number,
    task: string
  ): Promise<GenerateMagenticPlanResponse> => {
    const response = await api.post<GenerateMagenticPlanResponse>(
      `/collaborationworkflow/${collaborationId}/magentic/generate`,
      { task }
    );
    return response.data;
  },

  executeMagenticWorkflow: async (
    collaborationId: number,
    workflow: WorkflowDefinition,
    input: string,
    taskId?: number,
    onMessage?: (message: ChatMessageDto) => void
  ): Promise<void> => {
    const url = `${api.defaults.baseURL}/collaborationworkflow/${collaborationId}/magentic/execute`;
    await consumeSSE(url, { workflow, input, taskId }, onMessage || (() => {}));
  },
};
