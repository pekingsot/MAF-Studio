import api from './api';

export interface CoordinationSession {
  id: number;
  collaborationId: number;
  workflowExecutionId: number | null;
  orchestrationMode: string;
  status: string;
  topic: string | null;
  startTime: string;
  endTime: string | null;
  totalRounds: number;
  totalMessages: number;
  conclusion: string | null;
  createdAt: string;
}

export interface CoordinationRound {
  id: number;
  sessionId: number;
  roundNumber: number;
  speakerAgentId: number | null;
  speakerName: string;
  speakerRole: string | null;
  messageContent: string;
  messageId: number | null;
  thinkingProcess: string | null;
  selectedNextSpeaker: string | null;
  selectionReason: string | null;
  createdAt: string;
}

export interface CoordinationParticipant {
  id: number;
  sessionId: number;
  agentId: number;
  agentName: string;
  agentRole: string | null;
  isManager: boolean;
  speakCount: number;
  totalTokens: number;
  joinedAt: string;
}

export interface CoordinationSessionDetail {
  session: CoordinationSession;
  rounds: CoordinationRound[];
  participants: CoordinationParticipant[];
}

export const coordinationService = {
  getSessions: async (collaborationId: number, limit: number = 20): Promise<CoordinationSession[]> => {
    const response = await api.get(`/coordination/collaboration/${collaborationId}/sessions`, {
      params: { limit }
    });
    return response.data;
  },

  getSession: async (sessionId: number): Promise<CoordinationSession> => {
    const response = await api.get(`/coordination/sessions/${sessionId}`);
    return response.data;
  },

  getSessionDetail: async (sessionId: number): Promise<CoordinationSessionDetail> => {
    const response = await api.get(`/coordination/sessions/${sessionId}/detail`);
    return response.data;
  },

  getRounds: async (sessionId: number): Promise<CoordinationRound[]> => {
    const response = await api.get(`/coordination/sessions/${sessionId}/rounds`);
    return response.data;
  },

  getParticipants: async (sessionId: number): Promise<CoordinationParticipant[]> => {
    const response = await api.get(`/coordination/sessions/${sessionId}/participants`);
    return response.data;
  }
};
