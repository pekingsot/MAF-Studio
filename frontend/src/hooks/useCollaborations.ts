import { useState, useEffect, useCallback, useRef } from 'react';
import { message } from 'antd';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';

export const useCollaborations = () => {
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(false);
  
  const pendingRequestsRef = useRef<Map<string, Promise<any>>>(new Map());

  const loadInitialData = useCallback(async () => {
    if (pendingRequestsRef.current.has('initial')) {
      return pendingRequestsRef.current.get('initial');
    }

    setLoading(true);
    try {
      const promise = Promise.all([
        collaborationService.getAllCollaborations(),
        agentService.getAllAgents(),
      ]);
      
      pendingRequestsRef.current.set('initial', promise);
      
      const [collaborationsData, agentsResponse] = await promise;
      
      setCollaborations(collaborationsData);
      setAgents(agentsResponse || []);
    } catch (error) {
      message.error('加载数据失败');
      throw error;
    } finally {
      setLoading(false);
      pendingRequestsRef.current.delete('initial');
    }
  }, []);

  const loadCollaborationAgents = useCallback(async (collaborationId: string) => {
    const cacheKey = `agents_${collaborationId}`;

    if (pendingRequestsRef.current.has(cacheKey)) {
      return pendingRequestsRef.current.get(cacheKey);
    }

    try {
      const promise = collaborationService.getCollaborationAgents(collaborationId);
      pendingRequestsRef.current.set(cacheKey, promise);
      
      const agents = await promise;
      
      setCollaborations(prev => 
        prev.map(c => 
          c.id === collaborationId 
            ? { ...c, agents } 
            : c
        )
      );
      
      return agents;
    } catch (error) {
      message.error('加载智能体失败');
      throw error;
    } finally {
      pendingRequestsRef.current.delete(cacheKey);
    }
  }, []);

  const loadCollaborationTasks = useCallback(async (collaborationId: string) => {
    const cacheKey = `tasks_${collaborationId}`;

    if (pendingRequestsRef.current.has(cacheKey)) {
      return pendingRequestsRef.current.get(cacheKey);
    }

    try {
      const promise = collaborationService.getCollaborationTasks(collaborationId);
      pendingRequestsRef.current.set(cacheKey, promise);
      
      const tasks = await promise;
      
      setCollaborations(prev => 
        prev.map(c => 
          c.id === collaborationId 
            ? { ...c, tasks } 
            : c
        )
      );
      
      return tasks;
    } catch (error) {
      message.error('加载任务失败');
      throw error;
    } finally {
      pendingRequestsRef.current.delete(cacheKey);
    }
  }, []);

  const loadCollaborationData = useCallback(async (collaborationId: string) => {
    await Promise.all([
      loadCollaborationAgents(collaborationId),
      loadCollaborationTasks(collaborationId),
    ]);
  }, [loadCollaborationAgents, loadCollaborationTasks]);

  const refreshCollaboration = useCallback(async (collaborationId: string) => {
    await loadCollaborationData(collaborationId);
  }, [loadCollaborationData]);

  useEffect(() => {
    loadInitialData();
  }, [loadInitialData]);

  return {
    collaborations,
    agents,
    loading,
    loadInitialData,
    loadCollaborationAgents,
    loadCollaborationTasks,
    loadCollaborationData,
    refreshCollaboration,
    setCollaborations,
  };
};
