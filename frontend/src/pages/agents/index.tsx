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
    handleTest,
    handleDelete,
  } = useAgents();

  const {
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
    console.log('当前选中的模型:', selectedModels);
    
    const submitData = {
      ...data,
      llmConfigs: buildLlmConfigsRequest(),
    };
    
    try {
      if (editingAgent) {
        await agentService.updateAgent(editingAgent.id, submitData);
        message.success('更新成功');
      } else {
        await agentService.createAgent(submitData);
        message.success('创建成功');
      }
      setModalVisible(false);
      loadAgents();
    } catch (error) {
      console.error('提交失败:', error);
      message.error('操作失败');
    }
  }, [editingAgent, loadAgents, selectedModels, buildLlmConfigsRequest]);

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
        onTest={handleTest}
      />

      <AgentFormModal
        visible={modalVisible}
        editingAgent={editingAgent}
        agentTypes={agentTypes}
        llmConfigs={llmConfigs}
        selectedModels={selectedModels}
        onAddModel={handleAddModel}
        onSetPrimary={handleSetPrimary}
        onMoveUp={handleMoveUp}
        onMoveDown={handleMoveDown}
        onRemoveModel={handleRemoveModel}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        onTypeChange={handleTypeChange}
        onRefreshModels={loadLLMConfigs}
      />
    </div>
  );
};

export default Agents;
