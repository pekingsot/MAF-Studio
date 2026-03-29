import React, { useState, useCallback } from 'react';
import { Button, Modal, List, Tag, Space, Typography, message } from 'antd';
import { PlusOutlined, CheckCircleOutlined, CloseCircleOutlined } from '@ant-design/icons';
import { useLLMConfigs } from './useLLMConfigs';
import LLMConfigTable from './LLMConfigTable';
import ModelList from './ModelList';
import ConfigFormModal from './ConfigFormModal';
import ModelFormModal from './ModelFormModal';
import BatchAddModelsModal from './BatchAddModelsModal';
import { LLMConfig, LLMModelConfig } from './types';

const { Text } = Typography;

const LLMConfigs: React.FC = () => {
  const {
    configs,
    providers,
    loading,
    connectionStatus,
    testingIds,
    handleTestConnection,
    handleDelete,
    handleSetDefault,
    handleSubmit,
    handleDeleteModel,
    handleSetModelDefault,
    handleSubmitModel,
    handleDuplicate,
    loadConfigs,
    setConfigs,
  } = useLLMConfigs();

  const [modalVisible, setModalVisible] = useState(false);
  const [modelModalVisible, setModelModalVisible] = useState(false);
  const [batchAddModalVisible, setBatchAddModalVisible] = useState(false);
  const [editingConfig, setEditingConfig] = useState<LLMConfig | null>(null);
  const [editingModel, setEditingModel] = useState<LLMModelConfig | null>(null);
  const [parentConfigId, setParentConfigId] = useState<number | null>(null);

  const handleCreate = useCallback(() => {
    setEditingConfig(null);
    setModalVisible(true);
  }, []);

  const handleEdit = useCallback((config: LLMConfig) => {
    setEditingConfig(config);
    setModalVisible(true);
  }, []);

  const handleCreateModel = useCallback((configId: number) => {
    setParentConfigId(configId);
    setEditingModel(null);
    setModelModalVisible(true);
  }, []);

  const handleEditModel = useCallback((configId: number, model: LLMModelConfig) => {
    setParentConfigId(configId);
    setEditingModel(model);
    setModelModalVisible(true);
  }, []);

  const handleBatchAddModels = useCallback((configId: number) => {
    setParentConfigId(configId);
    setBatchAddModalVisible(true);
  }, []);

  const handleTestAllModels = useCallback(async (configId: number) => {
    const hide = message.loading('正在并行测试所有模型...', 0);
    try {
      const response = await fetch(`/api/llmconfigs/${configId}/test-all`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (!response.ok) {
        throw new Error('批量测试失败');
      }

      const data = await response.json();
      const successCount = data.results.filter((r: any) => r.success).length;
      const totalCount = data.results.length;
      
      hide();
      message.success(`批量测试完成：${successCount}/${totalCount} 个模型测试成功`);
      
      if (data.results && data.results.length > 0) {
        setConfigs((prevConfigs) => {
          return prevConfigs.map((config) => {
            if (config.id === configId) {
              const updatedModels = config.models.map((m) => {
                const testResult = data.results.find((r: any) => r.modelId === m.id);
                if (testResult && testResult.model) {
                  return { ...m, ...testResult.model };
                }
                return m;
              });
              return { ...config, models: updatedModels };
            }
            return config;
          });
        });
      }
    } catch (error: any) {
      hide();
      message.error(error.message || '批量测试失败');
    }
  }, []);

  const renderModelList = useCallback((config: LLMConfig) => (
    <div style={{ padding: '0 24px' }}>
      <ModelList
        config={config}
        connectionStatus={connectionStatus}
        testingIds={testingIds}
        onTest={handleTestConnection}
        onEdit={handleEditModel}
        onSetDefault={handleSetModelDefault}
        onDelete={handleDeleteModel}
        onAddModel={handleCreateModel}
      />
    </div>
  ), [connectionStatus, testingIds, handleTestConnection, handleEditModel, handleSetModelDefault, handleDeleteModel, handleCreateModel]);

  const onSubmitConfig = useCallback(async (values: Partial<LLMConfig>) => {
    return handleSubmit(values, editingConfig);
  }, [handleSubmit, editingConfig]);

  const onSubmitModel = useCallback(async (values: Partial<LLMModelConfig>) => {
    if (!parentConfigId) return false;
    return handleSubmitModel(parentConfigId, values, editingModel);
  }, [parentConfigId, handleSubmitModel, editingModel]);

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>大模型配置</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          新增供应商配置
        </Button>
      </div>

      <LLMConfigTable
        configs={configs}
        loading={loading}
        providers={providers}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onSetDefault={handleSetDefault}
        onAddModel={handleCreateModel}
        onBatchAddModels={handleBatchAddModels}
        onTestAllModels={handleTestAllModels}
        onDuplicate={handleDuplicate}
        renderModelList={renderModelList}
      />

      <ConfigFormModal
        visible={modalVisible}
        editingConfig={editingConfig}
        providers={providers}
        onCancel={() => setModalVisible(false)}
        onSubmit={onSubmitConfig}
      />

      <ModelFormModal
        visible={modelModalVisible}
        editingModel={editingModel}
        onCancel={() => setModelModalVisible(false)}
        onSubmit={onSubmitModel}
      />

      {parentConfigId && (
        <BatchAddModelsModal
          visible={batchAddModalVisible}
          configId={parentConfigId}
          onCancel={() => setBatchAddModalVisible(false)}
          onSuccess={() => {
            loadConfigs();
            setTimeout(() => {
              loadConfigs();
            }, 5000);
          }}
        />
      )}
    </div>
  );
};

export default LLMConfigs;
