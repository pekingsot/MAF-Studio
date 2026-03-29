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
  const [fallbackSelectorValue, setFallbackSelectorValue] = useState<string | undefined>(undefined);

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
      
      if (!selectedPrimaryModel) {
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
    }
  };

  const handlePrimaryModelSelect = (val: string | undefined) => {
    if (!val) {
      onPrimaryModelChange(null);
      return;
    }
    const [configIdStr, modelIdStr] = val.split('|');
    const configId = parseInt(configIdStr, 10);
    const modelId = parseInt(modelIdStr, 10);
    const config = llmConfigs.find(c => c.id === configId);
    const model = config?.models?.find(m => m.id === modelId);
    
    if (config && model) {
      onPrimaryModelChange({
        llmConfigId: config.id,
        llmConfigName: config.name,
        llmModelConfigId: model.id,
        modelName: model.displayName || model.modelName,
        provider: config.provider,
      });
    }
  };

  const handleFallbackModelSelect = (val: string | undefined) => {
    if (!val) return;
    
    const [configIdStr, modelIdStr] = val.split('|');
    const configId = parseInt(configIdStr, 10);
    const modelId = parseInt(modelIdStr, 10);
    const config = llmConfigs.find(c => c.id === configId);
    const model = config?.models?.find(m => m.id === modelId);
    
    if (config && model) {
      const exists = selectedFallbackModels.some(
        m => m.llmConfigId === configId && m.llmModelConfigId === modelId
      );
      const isPrimary = selectedPrimaryModel?.llmConfigId === configId && selectedPrimaryModel?.llmModelConfigId === modelId;
      
      if (!exists && !isPrimary && selectedFallbackModels.length < 3) {
        onFallbackModelsChange([...selectedFallbackModels, {
          llmConfigId: config.id,
          llmConfigName: config.name,
          llmModelConfigId: model.id,
          modelName: model.displayName || model.modelName,
          provider: config.provider,
        }]);
      }
    }
    setFallbackSelectorValue(undefined);
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
              副模型 <span style={{ color: '#999', fontWeight: 'normal', fontSize: 12 }}>（故障转移，主模型失败时按顺序尝试）</span>
            </span>
          }
        >
          <Space direction="vertical" style={{ width: '100%' }} size="middle">
            {selectedFallbackModels.length > 0 && (
              <List
                size="small"
                bordered
                dataSource={selectedFallbackModels}
                renderItem={(model, index) => (
                  <List.Item
                    actions={[
                      <Button
                        key="up"
                        size="small"
                        type="text"
                        icon={<ArrowUpOutlined />}
                        onClick={() => onMoveUp(index)}
                        disabled={index === 0}
                      />,
                      <Button
                        key="down"
                        size="small"
                        type="text"
                        icon={<ArrowDownOutlined />}
                        onClick={() => onMoveDown(index)}
                        disabled={index === selectedFallbackModels.length - 1}
                      />,
                      <Button
                        key="delete"
                        size="small"
                        type="text"
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => onRemoveFallbackModel(`${model.llmConfigId}_${model.llmModelConfigId}`)}
                      />,
                    ]}
                  >
                    <Space>
                      <Tag color="orange">{index + 1}</Tag>
                      <Tag color="blue">{model.llmConfigName}</Tag>
                      <span>{model.modelName}</span>
                    </Space>
                  </List.Item>
                )}
                style={{ marginBottom: 8 }}
              />
            )}
            
            {selectedFallbackModels.length < 3 && (
              <Select
                style={{ width: '100%' }}
                placeholder="添加副模型"
                value={fallbackSelectorValue}
                onChange={handleFallbackModelSelect}
                allowClear
                showSearch
                optionFilterProp="children"
              >
                {getAvailableModelsForFallback().map(config => (
                  <Select.OptGroup key={config.id} label={`${config.name} (${config.provider})`}>
                    {config.models?.map(model => (
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
            )}
          </Space>
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default AgentFormModal;
