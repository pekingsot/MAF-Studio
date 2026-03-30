import api from './api';

export interface WorkflowRequest {
  input: string;
}

export interface ChatMessageDto {
  sender: string;
  content: string;
  role: string;
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

export interface ReviewIterativeRequest {
  input: string;
  parameters?: ReviewIterativeParameters;
}

export const collaborationWorkflowService = {
  executeSequential: async (collaborationId: number, input: string): Promise<CollaborationResult> => {
    const response = await api.post<CollaborationResult>(
      `/collaborationworkflow/${collaborationId}/sequential`,
      { input }
    );
    return response.data;
  },

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

  executeGroupChat: async (collaborationId: number, input: string): Promise<void> => {
    const response = await fetch(
      `${api.defaults.baseURL}/collaborationworkflow/${collaborationId}/groupchat`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
        body: JSON.stringify({ input }),
      }
    );

    if (!response.ok) {
      throw new Error('Failed to execute group chat');
    }

    const reader = response.body?.getReader();
    const decoder = new TextDecoder();

    if (!reader) {
      throw new Error('No reader available');
    }

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      const chunk = decoder.decode(value);
      const lines = chunk.split('\n');

      for (const line of lines) {
        if (line.startsWith('data: ')) {
          const data = line.substring(6);
          if (data.trim()) {
            try {
              const message = JSON.parse(data);
              console.log('Group chat message:', message);
            } catch (e) {
              console.error('Failed to parse message:', data);
            }
          }
        }
      }
    }
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
};
