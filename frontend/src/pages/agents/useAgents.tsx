import { useState, useCallback, useRef, useEffect } from 'react';
import { message, Modal } from 'antd';
import { agentService, Agent, AgentType } from '../../services/agentService';
import { agentRuntimeService, AgentRuntimeStatus } from '../../services/agentRuntimeService';
import api from '../../services/api';
import { LLMConfig, SelectedModel } from './types';

export const useAgents = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [agentTypes, setAgentTypes] = useState<AgentType[]>([]);
  const [llmConfigs, setLLMConfigs] = useState<LLMConfig[]>([]);
  const [runtimeStatuses, setRuntimeStatuses] = useState<Record<string, AgentRuntimeStatus>>({});
  const [loading, setLoading] = useState(false);
  const [testingAgent, setTestingAgent] = useState<number | null>(null);
  const [activatingAgent, setActivatingAgent] = useState<number | null>(null);
  const initializedRef = useRef(false);

  const loadAgents = useCallback(async () => {
    try {
      setLoading(true);
      const agentsData = await agentService.getAllAgents();
      setAgents(agentsData || []);
      
      if (agentsData && agentsData.length > 0) {
        loadRuntimeStatuses(agentsData.map((a: Agent) => a.id));
      }
    } catch (error) {
      message.error('加载智能体列表失败');
    } finally {
      setLoading(false);
    }
  }, []);

  const loadAgentTypes = useCallback(async () => {
    try {
      const types = await agentService.getAgentTypes();
      setAgentTypes(types || []);
    } catch (error) {
      console.error('加载智能体类型失败', error);
    }
  }, []);

  const loadLLMConfigs = useCallback(async () => {
    try {
      console.log('开始加载llmConfigs');
      const response = await api.get('/llmconfigs');
      console.log('llmConfigs API响应:', response);
      const configs = (response.data || []).filter((c: LLMConfig) => c.isEnabled);
      console.log('过滤后的llmConfigs:', configs);
      console.log('llmConfigs数量:', configs.length);
      setLLMConfigs(configs);
      return configs;
    } catch (error) {
      console.error('加载大模型配置失败', error);
      return [];
    }
  }, []);

  const loadRuntimeStatuses = useCallback(async (agentIds: number[]) => {
    try {
      const statuses: Record<string, AgentRuntimeStatus> = {};
      for (const id of agentIds) {
        try {
          const status = await agentRuntimeService.getStatus(id);
          statuses[id] = status;
        } catch {
          statuses[id] = {
            agentId: id,
            state: 'Uninitialized',
            taskCount: 0,
            isAlive: false
          };
        }
      }
      setRuntimeStatuses(statuses);
    } catch (error) {
      console.error('加载运行时状态失败', error);
    }
  }, []);

  const handleActivate = useCallback(async (agentId: number) => {
    setActivatingAgent(agentId);
    try {
      const status = await agentRuntimeService.activate(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
      message.success('智能体已激活');
      loadAgents();
    } catch (error: unknown) {
      message.error(error.response?.data?.message || '激活失败');
    } finally {
      setActivatingAgent(null);
    }
  }, [loadAgents]);

  const handleTest = useCallback(async (agentId: number, input?: string) => {
    setTestingAgent(agentId);
    try {
      const result = await agentRuntimeService.test(agentId, input);
      if (result.success) {
        message.success(`测试成功 (${result.latencyMs}ms)`);
      } else {
        message.error(result.message);
      }
      const status = await agentRuntimeService.getStatus(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
    } catch (error: unknown) {
      message.error(error.response?.data?.message || '测试失败');
    } finally {
      setTestingAgent(null);
    }
  }, []);

  const handleDelete = useCallback(async (id: number) => {
    Modal.confirm({
      title: '确认删除',
      content: '确定要删除这个智能体吗？',
      onOk: async () => {
        try {
          await agentService.deleteAgent(id);
          message.success('删除成功');
          loadAgents();
        } catch (error) {
          message.error('删除失败');
        }
      },
    });
  }, [loadAgents]);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadAgents();
      loadAgentTypes();
    }
  }, [loadAgents, loadAgentTypes]);

  return {
    agents,
    agentTypes,
    llmConfigs,
    runtimeStatuses,
    loading,
    testingAgent,
    activatingAgent,
    loadAgents,
    loadLLMConfigs,
    handleActivate,
    handleTest,
    handleDelete,
  };
};

export const useAgentForm = (
  llmConfigs: LLMConfig[],
  loadLLMConfigs: () => Promise<LLMConfig[]>
) => {
  const [selectedModels, setSelectedModels] = useState<SelectedModel[]>([]);

  const initFormForCreate = useCallback(() => {
    setSelectedModels([]);
  }, []);

  const initFormForEdit = useCallback(async (agent: Agent) => {
    let currentLlmConfigs = llmConfigs;
    if (llmConfigs.length === 0) {
      currentLlmConfigs = await loadLLMConfigs();
    }
    
    const models: SelectedModel[] = [];
    
    if (agent.llmConfigs && agent.llmConfigs.length > 0) {
      agent.llmConfigs.forEach((lc, index) => {
        models.push({
          llmConfigId: lc.llmConfigId,
          llmConfigName: lc.llmConfigName,
          llmModelConfigId: lc.llmModelConfigId || 0,
          modelName: lc.modelName,
          provider: '',
          isPrimary: lc.isPrimary,
        });
      });
    }
    
    setSelectedModels(models);
  }, [llmConfigs, loadLLMConfigs]);

  const handleAddModel = useCallback((model: SelectedModel) => {
    setSelectedModels(prev => {
      const exists = prev.some(m => 
        m.llmConfigId === model.llmConfigId && 
        m.llmModelConfigId === model.llmModelConfigId
      );
      
      if (exists) {
        return prev;
      }
      
      if (prev.length === 0) {
        return [{ ...model, isPrimary: true }];
      }
      
      return [...prev, { ...model, isPrimary: false }];
    });
  }, []);

  const handleSetPrimary = useCallback((index: number) => {
    setSelectedModels(prev => {
      const newModels = [...prev];
      const [selectedModel] = newModels.splice(index, 1);
      newModels.unshift({ ...selectedModel, isPrimary: true });
      
      return newModels.map((m, i) => ({
        ...m,
        isPrimary: i === 0,
      }));
    });
  }, []);

  const handleMoveUp = useCallback((index: number) => {
    if (index > 1) {
      setSelectedModels(prev => {
        const newModels = [...prev];
        [newModels[index - 1], newModels[index]] = [newModels[index], newModels[index - 1]];
        return newModels;
      });
    }
  }, []);

  const handleMoveDown = useCallback((index: number) => {
    setSelectedModels(prev => {
      if (index > 0 && index < prev.length - 1) {
        const newModels = [...prev];
        [newModels[index], newModels[index + 1]] = [newModels[index + 1], newModels[index]];
        return newModels;
      }
      return prev;
    });
  }, []);

  const handleRemoveModel = useCallback((modelId: string) => {
    setSelectedModels(prev => {
      const filtered = prev.filter(m => `${m.llmConfigId}_${m.llmModelConfigId}` !== modelId);
      
      if (filtered.length > 0 && !filtered.some(m => m.isPrimary)) {
        filtered[0].isPrimary = true;
      }
      
      return filtered;
    });
  }, []);

  const buildLlmConfigsRequest = useCallback((): string => {
    const llmConfigs = selectedModels.map((model, index) => ({
      llmConfigId: model.llmConfigId,
      llmConfigName: model.llmConfigName,
      llmModelConfigId: model.llmModelConfigId,
      modelName: model.modelName,
      isPrimary: index === 0,
      priority: index === 0 ? 0 : index,
      isValid: true,
      msg: '',
    }));
    
    return JSON.stringify(llmConfigs);
  }, [selectedModels]);

  return {
    selectedModels,
    setSelectedModels,
    initFormForCreate,
    initFormForEdit,
    handleAddModel,
    handleSetPrimary,
    handleMoveUp,
    handleMoveDown,
    handleRemoveModel,
    buildLlmConfigsRequest,
  };
};
