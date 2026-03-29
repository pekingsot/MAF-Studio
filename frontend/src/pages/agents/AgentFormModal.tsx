import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, Divider, Row, Col, Tag, Button, Space, List } from 'antd';
import { ArrowUpOutlined, ArrowDownOutlined, DeleteOutlined } from '@ant-design/icons';
import { Agent, AgentType } from '../../services/agentService';
import { LLMConfig, SelectedModel, AVATAR_OPTIONS, AgentFormData } from './types';

const { Option } = Select;
const { TextArea } = Input;

interface AgentFormModalProps {
  visible: boolean;
  editingAgent: Agent | null;
  agentTypes: AgentType[];
  llmConfigs: LLMConfig[];
  selectedPrimaryModel: SelectedModel | null;
  selectedFallbackModels: SelectedModel[];
  onPrimaryModelChange: (model: SelectedModel | null) => void;
  onFallbackModelsChange: (models: SelectedModel[]) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
  onRemoveFallbackModel: (modelId: string) => void;
  onCancel: () => void;
  onSubmit: (data: AgentFormData) => Promise<void>;
  onTypeChange: (typeCode: string) => void;
}

const AgentFormModal: React.FC<AgentFormModalProps> = ({
  visible,
  editingAgent,
  agentTypes,
  llmConfigs,
  selectedPrimaryModel,
  selectedFallbackModels,
  onPrimaryModelChange,
  onFallbackModelsChange,
  onMoveUp,
  onMoveDown,
  onRemoveFallbackModel,
  onCancel,
  onSubmit,
  onTypeChange,
}) => {
  const [form] = Form.useForm();

  useEffect(() => {
    console.log('AgentFormModal - visible变化:', visible);
    console.log('AgentFormModal - editingAgent:', editingAgent);
    console.log('AgentFormModal - selectedPrimaryModel:', selectedPrimaryModel);
    console.log('AgentFormModal - selectedFallbackModels:', selectedFallbackModels);
    console.log('AgentFormModal - llmConfigs:', llmConfigs);
    console.log('AgentFormModal - llmConfigs数量:', llmConfigs?.length);
  }, [visible, editingAgent, selectedPrimaryModel, selectedFallbackModels, llmConfigs]);

  useEffect(() => {
    if (visible) {
      if (editingAgent) {
        form.setFieldsValue({
          name: editingAgent.name,
          description: editingAgent.description,
          type: editingAgent.type,
          avatar: editingAgent.avatar || '🤖',
          systemPrompt: editingAgent.systemPrompt,
        });
      } else {
        form.resetFields();
        form.setFieldsValue({
          avatar: '🤖',
        });
      }
    }
  }, [visible, editingAgent, form]);

  const handleTypeChange = (typeCode: string) => {
    onTypeChange(typeCode);
    const selectedType = agentTypes.find(t => t.code === typeCode);
    if (selectedType?.defaultSystemPrompt) {
      form.setFieldsValue({
        systemPrompt: selectedType.defaultSystemPrompt,
      });
    }
  };

  const handleOk = async () => {
    try {
      const values = await form.validateFields();
      
      console.log('表单值:', values);
      console.log('选中的主模型:', selectedPrimaryModel);
      console.log('选中的副模型:', selectedFallbackModels);
      
      if (!selectedPrimaryModel) {
        Modal.error({
          title: '请选择主模型',
          content: '创建智能体必须选择一个主模型',
        });
        return;
      }
      
      if (!selectedPrimaryModel.llmConfigId || !selectedPrimaryModel.llmModelConfigId) {
        Modal.error({
          title: '主模型信息不完整',
          content: '请重新选择主模型',
        });
        return;
      }
      
      await onSubmit({
        name: values.name,
        description: values.description,
        type: values.type,
        avatar: values.avatar || '🤖',
        systemPrompt: values.systemPrompt,
        llmConfigId: selectedPrimaryModel.llmConfigId,
        llmModelConfigId: selectedPrimaryModel.llmModelConfigId,
        fallbackModels: selectedFallbackModels.map((model, index) => ({
          llmConfigId: model.llmConfigId,
          llmModelConfigId: model.llmModelConfigId,
          priority: index + 1,
        })),
      });
      
      form.resetFields();
    } catch (error) {
      console.error('表单验证失败:', error);
      Modal.error({
        title: '提交失败',
        content: '请检查表单填写是否正确',
      });
    }
  };

  const handlePrimaryModelSelect = (val: string | undefined) => {
    console.log('主模型选择值:', val);
    console.log('当前llmConfigs:', llmConfigs);
    console.log('llmConfigs长度:', llmConfigs?.length);
    
    if (!val) {
      onPrimaryModelChange(null);
      return;
    }
    const [configIdStr, modelIdStr] = val.split('|');
    const configId = parseInt(configIdStr, 10);
    const modelId = parseInt(modelIdStr, 10);
    
    console.log('解析后的ID - configId:', configId, 'modelId:', modelId);
    console.log('llmConfigs中的所有ID:', llmConfigs?.map(c => c.id));
    
    const config = llmConfigs.find(c => c.id === configId);
    const model = config?.models?.find(m => m.id === modelId);
    
    console.log('找到的配置:', config);
    console.log('找到的模型:', model);
    
    if (config && model) {
      const selectedModel = {
        llmConfigId: config.id,
        llmConfigName: config.name,
        llmModelConfigId: model.id,
        modelName: model.displayName || model.modelName,
        provider: config.provider,
      };
      
      console.log('设置的主模型:', selectedModel);
      onPrimaryModelChange(selectedModel);
    } else {
      console.error('未找到配置或模型');
      console.error('configId:', configId, '类型:', typeof configId);
      console.error('llmConfigs中的第一个配置ID:', llmConfigs?.[0]?.id, '类型:', typeof llmConfigs?.[0]?.id);
    }
  };

  const handleFallbackModelSelect = (val: string | undefined) => {
    console.log('副模型选择值:', val);
    
    if (!val) {
      return;
    }
    
    const [configIdStr, modelIdStr] = val.split('|');
    const configId = parseInt(configIdStr, 10);
    const modelId = parseInt(modelIdStr, 10);
    const config = llmConfigs.find(c => c.id === configId);
    const model = config?.models?.find(m => m.id === modelId);
    
    console.log('副模型 - 解析ID:', { configId, modelId });
    console.log('副模型 - 找到配置:', config);
    console.log('副模型 - 找到模型:', model);
    
    if (config && model) {
      const exists = selectedFallbackModels.some(
        m => m.llmConfigId === configId && m.llmModelConfigId === modelId
      );
      const isPrimary = selectedPrimaryModel?.llmConfigId === configId && selectedPrimaryModel?.llmModelConfigId === modelId;
      
      console.log('副模型 - 是否已存在:', exists);
      console.log('副模型 - 是否是主模型:', isPrimary);
      console.log('副模型 - 当前数量:', selectedFallbackModels.length);
      
      if (!exists && !isPrimary && selectedFallbackModels.length < 20) {
        const newModel = {
          llmConfigId: config.id,
          llmConfigName: config.name,
          llmModelConfigId: model.id,
          modelName: model.displayName || model.modelName,
          provider: config.provider,
        };
        
        console.log('副模型 - 添加新模型:', newModel);
        onFallbackModelsChange([...selectedFallbackModels, newModel]);
      } else if (exists) {
        Modal.warning({
          title: '模型已存在',
          content: '该模型已在副模型列表中',
        });
      } else if (isPrimary) {
        Modal.warning({
          title: '不能选择主模型',
          content: '副模型不能与主模型相同',
        });
      } else if (selectedFallbackModels.length >= 20) {
        Modal.warning({
          title: '已达上限',
          content: '最多只能选择20个副模型',
        });
      }
    } else {
      console.error('副模型 - 未找到配置或模型');
    }
  };

  const getAvailableModelsForFallback = () => {
    return llmConfigs.map(config => ({
      ...config,
      models: config.models?.filter(m => {
        if (!m.isEnabled) return false;
        const isPrimary = selectedPrimaryModel?.llmConfigId === config.id && selectedPrimaryModel?.llmModelConfigId === m.id;
        const isAlreadyFallback = selectedFallbackModels.some(
          fm => fm.llmConfigId === config.id && fm.llmModelConfigId === m.id
        );
        return !isPrimary && !isAlreadyFallback;
      })
    })).filter(config => config.models && config.models.length > 0);
  };

  const primaryModelValue = selectedPrimaryModel 
    ? `${selectedPrimaryModel.llmConfigId}|${selectedPrimaryModel.llmModelConfigId}` 
    : undefined;

  return (
    <Modal
      title={editingAgent ? '编辑智能体' : '创建智能体'}
      open={visible}
      onOk={handleOk}
      onCancel={onCancel}
      width={800}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
      >
        <Row gutter={16}>
          <Col span={12}>
            <Form.Item
              name="name"
              label="名称"
              rules={[{ required: true, message: '请输入智能体名称' }]}
            >
              <Input placeholder="请输入智能体名称" />
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item
              name="type"
              label="类型"
              rules={[{ required: true, message: '请选择类型' }]}
            >
              <Select placeholder="选择类型" onChange={handleTypeChange}>
                {agentTypes.map(type => (
                  <Option key={type.code} value={type.code}>
                    {type.icon} {type.name}
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </Col>
          <Col span={6}>
            <Form.Item name="avatar" label="头像">
              <Select placeholder="选择头像">
                {AVATAR_OPTIONS.map(avatar => (
                  <Option key={avatar} value={avatar}>
                    <span style={{ fontSize: 20 }}>{avatar}</span>
                  </Option>
                ))}
              </Select>
            </Form.Item>
          </Col>
        </Row>

        <Form.Item name="description" label="描述">
          <TextArea rows={2} placeholder="请输入智能体描述" />
        </Form.Item>

        <Form.Item name="systemPrompt" label="系统提示词">
          <TextArea rows={4} placeholder="请输入系统提示词" />
        </Form.Item>

        <Divider>模型配置</Divider>

        <Form.Item 
          label={
            <span>
              主模型 <span style={{ color: '#999', fontWeight: 'normal', fontSize: 12 }}>（智能体默认使用的模型）</span>
            </span>
          } 
          required
        >
          <Select
            style={{ width: '100%' }}
            placeholder="选择主模型"
            value={primaryModelValue}
            onChange={handlePrimaryModelSelect}
            allowClear
            showSearch
            optionFilterProp="children"
          >
            {llmConfigs.map(config => (
              <Select.OptGroup key={config.id} label={`${config.name} (${config.provider})`}>
                {config.models?.filter(m => m.isEnabled).map(model => (
                  <Option key={`${config.id}|${model.id}`} value={`${config.id}|${model.id}`}>
                    <Space>
                      <Tag color="blue" style={{ margin: 0 }}>{config.name}</Tag>
                      {model.displayName || model.modelName}
                    </Space>
                  </Option>
                ))}
              </Select.OptGroup>
            ))}
          </Select>
        </Form.Item>

        <Form.Item 
          label={
            <span>
              副模型 <span style={{ color: '#999', fontWeight: 'normal', fontSize: 12 }}>（故障转移，主模型失败时按顺序尝试，最多20个）</span>
            </span>
          }
        >
          <Row gutter={16}>
            <Col span={12}>
              <div style={{ marginBottom: 8, fontWeight: 500 }}>可选模型</div>
              <div style={{ 
                border: '1px solid #d9d9d9', 
                borderRadius: 4, 
                height: 300, 
                overflow: 'auto',
                padding: 8 
              }}>
                {getAvailableModelsForFallback().length === 0 ? (
                  <div style={{ color: '#999', textAlign: 'center', padding: 20 }}>
                    暂无可选模型
                  </div>
                ) : (
                  getAvailableModelsForFallback().map(config => (
                    <div key={config.id} style={{ marginBottom: 8 }}>
                      <div style={{ fontWeight: 500, marginBottom: 4, color: '#1890ff' }}>
                        {config.name} ({config.provider})
                      </div>
                      {config.models?.map(model => (
                        <div
                          key={`${config.id}|${model.id}`}
                          style={{
                            padding: '8px 12px',
                            cursor: 'pointer',
                            borderRadius: 4,
                            marginBottom: 4,
                            background: '#fafafa',
                            transition: 'all 0.3s',
                          }}
                          onClick={() => {
                            const val = `${config.id}|${model.id}`;
                            console.log('点击副模型:', val);
                            handleFallbackModelSelect(val);
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.background = '#e6f7ff';
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.background = '#fafafa';
                          }}
                        >
                          <Space>
                            <Tag color="blue" style={{ margin: 0 }}>{config.name}</Tag>
                            <span>{model.displayName || model.modelName}</span>
                          </Space>
                        </div>
                      ))}
                    </div>
                  ))
                )}
              </div>
            </Col>
            <Col span={12}>
              <div style={{ marginBottom: 8, fontWeight: 500 }}>
                已选模型 ({selectedFallbackModels.length}/20)
              </div>
              <div style={{ 
                border: '1px solid #d9d9d9', 
                borderRadius: 4, 
                height: 300, 
                overflow: 'auto',
                padding: 8,
                background: selectedFallbackModels.length === 0 ? '#fafafa' : '#fff'
              }}>
                {selectedFallbackModels.length === 0 ? (
                  <div style={{ color: '#999', textAlign: 'center', padding: 40 }}>
                    请从左侧选择模型
                  </div>
                ) : (
                  selectedFallbackModels.map((model, index) => (
                    <div
                      key={`${model.llmConfigId}|${model.llmModelConfigId}`}
                      style={{
                        padding: '8px 12px',
                        marginBottom: 8,
                        background: '#f0f5ff',
                        border: '1px solid #adc6ff',
                        borderRadius: 4,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'space-between',
                      }}
                    >
                      <Space>
                        <Tag color="orange">{index + 1}</Tag>
                        <Tag color="blue">{model.llmConfigName}</Tag>
                        <span>{model.modelName}</span>
                      </Space>
                      <Space size="small">
                        <Button
                          size="small"
                          type="text"
                          icon={<ArrowUpOutlined />}
                          onClick={() => onMoveUp(index)}
                          disabled={index === 0}
                        />
                        <Button
                          size="small"
                          type="text"
                          icon={<ArrowDownOutlined />}
                          onClick={() => onMoveDown(index)}
                          disabled={index === selectedFallbackModels.length - 1}
                        />
                        <Button
                          size="small"
                          type="text"
                          danger
                          icon={<DeleteOutlined />}
                          onClick={() => onRemoveFallbackModel(`${model.llmConfigId}_${model.llmModelConfigId}`)}
                        />
                      </Space>
                    </div>
                  ))
                )}
              </div>
            </Col>
          </Row>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default AgentFormModal;
