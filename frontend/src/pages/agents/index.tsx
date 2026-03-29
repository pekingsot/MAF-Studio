import React, { useState, useCallback } from 'react';
import { Button, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { PageHeader } from '../../components/common';
import { agentService } from '../../services/agentService';
import { Agent } from '../../services/agentService';
import { useAgents, useAgentForm } from './useAgents';
import AgentTable from './AgentTable';
import AgentFormModal from './AgentFormModal';
import { AgentFormData } from './types';

const Agents: React.FC = () => {
  const [modalVisible, setModalVisible] = useState(false);
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);
  const [pageSize, setPageSize] = useState(10);

  const {
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
    handleSleep,
    handleDestroy,
    handleTest,
    handleDelete,
  } = useAgents();

  const {
    selectedPrimaryModel,
    setSelectedPrimaryModel,
    selectedFallbackModels,
    setSelectedFallbackModels,
    initFormForCreate,
    initFormForEdit,
    handleMoveUp,
    handleMoveDown,
    handleRemoveFallbackModel,
  } = useAgentForm(llmConfigs, loadLLMConfigs);

  const handleCreate = useCallback(async () => {
    console.log('创建智能体 - 当前llmConfigs:', llmConfigs);
    console.log('创建智能体 - llmConfigs长度:', llmConfigs.length);
    
    setEditingAgent(null);
    initFormForCreate();
    
    if (llmConfigs.length === 0) {
      console.log('创建智能体 - 加载llmConfigs');
      const configs = await loadLLMConfigs();
      console.log('创建智能体 - 加载后的llmConfigs:', configs);
    }
    
    setModalVisible(true);
  }, [llmConfigs, loadLLMConfigs, initFormForCreate]);

  const handleEdit = useCallback(async (agent: Agent) => {
    setEditingAgent(agent);
    
    if (llmConfigs.length === 0) {
      await loadLLMConfigs();
    }
    
    await initFormForEdit(agent);
    setModalVisible(true);
  }, [llmConfigs.length, loadLLMConfigs, initFormForEdit]);

  const handleSubmit = useCallback(async (data: AgentFormData) => {
    console.log('提交数据:', data);
    console.log('当前主模型:', selectedPrimaryModel);
    console.log('当前副模型:', selectedFallbackModels);
    
    try {
      if (editingAgent) {
        await agentService.updateAgent(editingAgent.id, data);
        message.success('更新成功');
      } else {
        await agentService.createAgent(data);
        message.success('创建成功');
      }
      setModalVisible(false);
      loadAgents();
    } catch (error) {
      console.error('提交失败:', error);
      message.error('操作失败');
    }
  }, [editingAgent, loadAgents]);

  const handleTypeChange = useCallback((typeCode: string) => {
    const selectedType = agentTypes.find(t => t.code === typeCode);
    if (selectedType) {
      console.log('Type changed:', selectedType);
    }
  }, [agentTypes]);

  const handleCancel = useCallback(() => {
    setModalVisible(false);
    setEditingAgent(null);
  }, []);

  return (
    <div>
      <PageHeader
        title="智能体管理"
        subTitle="创建和管理您的 AI 智能体"
        extra={
          <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
            创建智能体
          </Button>
        }
      />

      <AgentTable
        agents={agents}
        agentTypes={agentTypes}
        llmConfigs={llmConfigs}
        runtimeStatuses={runtimeStatuses}
        loading={loading}
        testingAgent={testingAgent}
        activatingAgent={activatingAgent}
        pageSize={pageSize}
        onPageSizeChange={setPageSize}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onActivate={handleActivate}
        onSleep={handleSleep}
        onDestroy={handleDestroy}
        onTest={handleTest}
      />

      <AgentFormModal
        visible={modalVisible}
        editingAgent={editingAgent}
        agentTypes={agentTypes}
        llmConfigs={llmConfigs}
        selectedPrimaryModel={selectedPrimaryModel}
        selectedFallbackModels={selectedFallbackModels}
        onPrimaryModelChange={setSelectedPrimaryModel}
        onFallbackModelsChange={setSelectedFallbackModels}
        onMoveUp={handleMoveUp}
        onMoveDown={handleMoveDown}
        onRemoveFallbackModel={handleRemoveFallbackModel}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        onTypeChange={handleTypeChange}
      />
    </div>
  );
};

export default Agents;
