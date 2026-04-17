import { useState, useEffect, useRef, useCallback } from 'react';
import { message } from 'antd';
import api from '../../services/api';
import { LLMConfig, ProviderInfo, ConnectionStatus } from './types';

export const useLLMConfigs = () => {
  const [configs, setConfigs] = useState<LLMConfig[]>([]);
  const [providers, setProviders] = useState<ProviderInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [connectionStatus, setConnectionStatus] = useState<Record<string, ConnectionStatus>>({});
  const [testingIds, setTestingIds] = useState<Set<number>>(new Set());
  const initializedRef = useRef(false);

  const loadConfigs = useCallback(async () => {
    setLoading(true);
    try {
      const response = await api.get<LLMConfig[]>('/llmconfigs');
      setConfigs(response.data);
    } catch (error) {
      message.error('加载配置失败');
    } finally {
      setLoading(false);
    }
  }, []);

  const loadProviders = useCallback(async () => {
    try {
      const response = await api.get<ProviderInfo[]>('/llmconfigs/providers');
      setProviders(response.data);
    } catch (error) {
      console.error('加载供应商列表失败', error);
    }
  }, []);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadConfigs();
      loadProviders();
    }
  }, [loadConfigs, loadProviders]);

  const handleTestConnection = useCallback(async (configId: number, modelId?: number) => {
    const testId = modelId ? configId * 1000 + modelId : configId;
    setTestingIds((prev) => new Set(prev).add(testId));
    try {
      const url = modelId
        ? `/llmconfigs/${configId}/models/${modelId}/test`
        : `/llmconfigs/${configId}/test`;
      const response = await api.post<ConnectionStatus & { model?: Record<string, unknown> }>(url);
      setConnectionStatus((prev) => ({
        ...prev,
        [testId]: response.data,
      }));
      if (response.data.success) {
        message.success(`连接成功 (${response.data.latencyMs}ms)`);
      } else {
        message.error(`连接失败: ${response.data.message}`);
      }
      
      if (response.data.model) {
        setConfigs((prevConfigs) => {
          return prevConfigs.map((config) => {
            if (config.id === configId) {
              return {
                ...config,
                models: config.models.map((m) =>
                  m.id === modelId ? { ...m, ...response.data.model } : m
                ),
              };
            }
            return config;
          });
        });
      }
    } catch (error) {
      setConnectionStatus((prev) => ({
        ...prev,
        [testId]: { success: false, message: '测试请求失败', latencyMs: 0 },
      }));
      message.error('测试请求失败');
    } finally {
      setTestingIds((prev) => {
        const next = new Set(prev);
        next.delete(testId);
        return next;
      });
    }
  }, []);

  const handleDelete = useCallback(async (id: number) => {
    try {
      await api.delete(`/llmconfigs/${id}`);
      message.success('删除成功');
      loadConfigs();
    } catch (error) {
      message.error('删除失败');
    }
  }, [loadConfigs]);

  const handleSetDefault = useCallback(async (id: number) => {
    try {
      await api.post(`/llmconfigs/${id}/set-default`);
      message.success('设置成功');
      loadConfigs();
    } finally {
      loadConfigs();
    }
  }, [loadConfigs]);

  const handleSubmit = useCallback(async (values: Partial<LLMConfig>, editingConfig: LLMConfig | null) => {
    try {
      if (editingConfig) {
        await api.put(`/llmconfigs/${editingConfig.id}`, values);
        message.success('更新成功');
      } else {
        await api.post('/llmconfigs', values);
        message.success('创建成功');
      }
      loadConfigs();
      return true;
    } catch (error) {
      message.error('操作失败');
      return false;
    }
  }, [loadConfigs]);

  const handleDeleteModel = useCallback(async (configId: number, modelId: number) => {
    try {
      await api.delete(`/llmconfigs/${configId}/models/${modelId}`);
      message.success('删除成功');
      loadConfigs();
    } catch (error) {
      message.error('删除失败');
    }
  }, [loadConfigs]);

  const handleSetModelDefault = useCallback(async (configId: number, modelId: number) => {
    try {
      await api.post(`/llmconfigs/${configId}/models/${modelId}/set-default`);
      message.success('设置成功');
      loadConfigs();
    } catch (error) {
      message.error('设置失败');
    }
  }, [loadConfigs]);

  const handleSubmitModel = useCallback(async (
    parentConfigId: number,
    values: Partial<LLMConfig['models'][0]>,
    editingModel: LLMConfig['models'][0] | null
  ) => {
    try {
      if (editingModel) {
        await api.put(`/llmconfigs/${parentConfigId}/models/${editingModel.id}`, values);
        message.success('更新成功');
        loadConfigs();
      } else {
        await api.post(`/llmconfigs/${parentConfigId}/models`, values);
        message.success('添加成功，正在后台测试连接...');
        loadConfigs();
        
        setTimeout(() => {
          loadConfigs();
        }, 3000);
      }
      return true;
    } catch (error) {
      message.error('操作失败');
      return false;
    }
  }, [loadConfigs]);

  const handleDuplicate = useCallback(async (id: number) => {
    try {
      await api.post(`/llmconfigs/${id}/duplicate`);
      message.success('复制成功');
      loadConfigs();
    } catch (error) {
      message.error('复制失败');
    }
  }, [loadConfigs]);

  return {
    configs,
    providers,
    loading,
    connectionStatus,
    testingIds,
    loadConfigs,
    setConfigs,
    handleTestConnection,
    handleDelete,
    handleSetDefault,
    handleSubmit,
    handleDeleteModel,
    handleSetModelDefault,
    handleSubmitModel,
    handleDuplicate,
  };
};
