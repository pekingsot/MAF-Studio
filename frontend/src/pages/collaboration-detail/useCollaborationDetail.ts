import { useState, useEffect, useRef, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { message } from 'antd';
import { collaborationService } from '../../services/collaborationService';
import { CollaborationDetailData } from './types';

export const useCollaborationDetail = () => {
  const { id } = useParams<{ id: string }>();
  const [collaboration, setCollaboration] = useState<CollaborationDetailData | null>(null);
  const [loading, setLoading] = useState(true);
  const initializedRef = useRef(false);

  const loadCollaboration = useCallback(async (collaborationId: string) => {
    try {
      setLoading(true);
      const data = await collaborationService.getCollaborationById(collaborationId);
      setCollaboration(data as CollaborationDetailData);
    } catch (error) {
      message.error('加载协作详情失败');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (id && !initializedRef.current) {
      initializedRef.current = true;
      loadCollaboration(id);
    }
  }, [id, loadCollaboration]);

  const handleRemoveAgent = useCallback(async (agentId: number) => {
    if (id && collaboration) {
      try {
        await collaborationService.removeAgentFromCollaboration(id, agentId);
        message.success('移除成功');
        loadCollaboration(id);
      } catch (error) {
        message.error('移除失败');
      }
    }
  }, [id, collaboration, loadCollaboration]);

  const handleUpdateTaskStatus = useCallback(async (taskId: string, status: string) => {
    try {
      await collaborationService.updateTaskStatus(taskId, status);
      message.success('更新成功');
      if (id) {
        loadCollaboration(id);
      }
    } catch (error) {
      message.error('更新失败');
    }
  }, [id, loadCollaboration]);

  return {
    id,
    collaboration,
    loading,
    loadCollaboration,
    handleRemoveAgent,
    handleUpdateTaskStatus,
  };
};
