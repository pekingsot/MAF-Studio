import { getErrorMessage } from '../../utils/errorHandler';
import React, { useEffect, useState } from 'react';
import { Modal, Form, Input, Select, Divider, Row, Col, Tag, Button, Space, List, Tooltip, Checkbox, message, Popover } from 'antd';
import { ArrowUpOutlined, ArrowDownOutlined, DeleteOutlined, StarOutlined, ReloadOutlined } from '@ant-design/icons';
import { Agent, AgentType } from '../../services/agentService';
import { LLMConfig, SelectedModel, AVATAR_OPTIONS, AgentFormData, LlmConfigInfo } from './types';
import api from '../../services/api';

const { Option } = Select;
const { TextArea } = Input;

interface AgentFormModalProps {
  visible: boolean;
  editingAgent: Agent | null;
  agentTypes: AgentType[];
  llmConfigs: LLMConfig[];
  selectedModels: SelectedModel[];
  onAddModel: (model: SelectedModel) => void;
  onSetPrimary: (index: number) => void;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
  onRemoveModel: (modelId: string) => void;
  onCancel: () => void;
  onSubmit: (data: AgentFormData) => Promise<void>;
  onTypeChange: (typeCode: string) => void;
  onRefreshModels: () => Promise<void>;
}

const AgentFormModal: React.FC<AgentFormModalProps> = ({
  visible,
  editingAgent,
  agentTypes,
  llmConfigs,
  selectedModels,
  onAddModel,
  onSetPrimary,
  onMoveUp,
  onMoveDown,
  onRemoveModel,
  onCancel,
  onSubmit,
  onTypeChange,
  onRefreshModels,
}) => {
  const [form] = Form.useForm();
  const avatarValue = Form.useWatch('avatar', form);
  const [showOnlyAvailable, setShowOnlyAvailable] = useState(true);
  const [testing, setTesting] = useState(false);
  const [avatarPopoverVisible, setAvatarPopoverVisible] = useState(false);

  useEffect(() => {
    console.log('AgentFormModal - visible变化:', visible);
    console.log('AgentFormModal - editingAgent:', editingAgent);
    console.log('AgentFormModal - selectedModels:', selectedModels);
    console.log('AgentFormModal - llmConfigs:', llmConfigs);
    console.log('AgentFormModal - llmConfigs数量:', llmConfigs?.length);
  }, [visible, editingAgent, selectedModels, llmConfigs]);

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
      console.log('选中的模型:', selectedModels);
      
      if (selectedModels.length === 0) {
        Modal.error({
          title: '请选择模型',
          content: '创建智能体必须至少选择一个模型',
        });
        return;
      }
      
      const primaryModel = selectedModels.find(m => m.isPrimary);
      if (!primaryModel) {
        Modal.error({
          title: '请设置主模型',
          content: '请在已选模型中设置一个主模型',
        });
        return;
      }
      
      await onSubmit({
        name: values.name,
        description: values.description,
        type: values.type,
        avatar: values.avatar || '🤖',
        systemPrompt: values.systemPrompt,
        llmConfigId: primaryModel.llmConfigId,
        llmModelConfigId: primaryModel.llmModelConfigId,
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

  const handleModelSelect = (val: string | undefined) => {
    console.log('模型选择值:', val);
    
    if (!val) {
      return;
    }
    
    const [configIdStr, modelIdStr] = val.split('|');
    const configId = parseInt(configIdStr, 10);
    const modelId = parseInt(modelIdStr, 10);
    const config = llmConfigs.find(c => c.id === configId);
    const model = config?.models?.find(m => m.id === modelId);
    
    console.log('模型 - 解析ID:', { configId, modelId });
    console.log('模型 - 找到配置:', config);
    console.log('模型 - 找到模型:', model);
    
    if (config && model) {
      const exists = selectedModels.some(
        m => m.llmConfigId === configId && m.llmModelConfigId === modelId
      );
      
      console.log('模型 - 是否已存在:', exists);
      console.log('模型 - 当前数量:', selectedModels.length);
      
      if (!exists && selectedModels.length < 20) {
        const newModel: SelectedModel = {
          llmConfigId: config.id,
          llmConfigName: config.name,
          llmModelConfigId: model.id,
          modelName: model.displayName || model.modelName,
          provider: config.provider,
          isPrimary: selectedModels.length === 0,
        };
        
        console.log('模型 - 添加新模型:', newModel);
        onAddModel(newModel);
      } else if (exists) {
        Modal.warning({
          title: '模型已存在',
          content: '该模型已在已选模型列表中',
        });
      } else if (selectedModels.length >= 20) {
        Modal.warning({
          title: '已达上限',
          content: '最多只能选择20个模型',
        });
      }
    } else {
      console.error('模型 - 未找到配置或模型');
    }
  };

  const getAvailableModels = () => {
    return llmConfigs.map(config => ({
      ...config,
      models: config.models?.filter(m => {
        if (!m.isEnabled) return false;
        if (showOnlyAvailable && m.availabilityStatus === 0) return false;
        const isAlreadySelected = selectedModels.some(
          sm => sm.llmConfigId === config.id && sm.llmModelConfigId === m.id
        );
        return !isAlreadySelected;
      })
    })).filter(config => config.models && config.models.length > 0);
  };

  const handleTestAllModels = async () => {
    try {
      setTesting(true);
      message.loading({ content: '正在批量测试所有模型...', key: 'testAllModels', duration: 0 });
      
      const response = await api.post('/llmconfigs/test-all-models');
      const results = response.data.results || [];
      
      const successCount = results.filter((r: { success: boolean }) => r.success).length;
      const failedCount = results.filter((r: { success: boolean }) => !r.success).length;
      
      message.success({ 
        content: `测试完成！成功: ${successCount}, 失败: ${failedCount}`, 
        key: 'testAllModels',
        duration: 3
      });
      
      await onRefreshModels();
    } catch (error: unknown) {
      console.error('批量测试失败:', error);
      message.error({ 
        content: `批量测试失败: ${getErrorMessage(error)}`, 
        key: 'testAllModels' 
      });
    } finally {
      setTesting(false);
    }
  };

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
              <Popover
                open={avatarPopoverVisible}
                onOpenChange={setAvatarPopoverVisible}
                trigger="click"
                placement="bottomLeft"
                content={
                  <div style={{ 
                    width: 280, 
                    maxHeight: 400, 
                    overflowY: 'auto',
                    overflowX: 'hidden',
                    display: 'grid',
                    gridTemplateColumns: 'repeat(5, 1fr)',
                    gap: 8,
                    padding: 8,
                    scrollbarWidth: 'none',
                    msOverflowStyle: 'none',
                  }}>
                    <style>{`
                      div::-webkit-scrollbar {
                        display: none;
                      }
                    `}</style>
                    {AVATAR_OPTIONS.map(avatar => (
                      <div
                        key={avatar}
                        onClick={() => {
                          form.setFieldValue('avatar', avatar);
                          setAvatarPopoverVisible(false);
                        }}
                        style={{
                          width: 48,
                          height: 48,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontSize: 32,
                          cursor: 'pointer',
                          borderRadius: 4,
                          border: avatarValue === avatar ? '2px solid #1890ff' : '1px solid #d9d9d9',
                          background: avatarValue === avatar ? '#e6f7ff' : '#fafafa',
                          transition: 'all 0.3s',
                        }}
                        onMouseEnter={(e) => {
                          if (avatarValue !== avatar) {
                            e.currentTarget.style.background = '#f0f0f0';
                          }
                        }}
                        onMouseLeave={(e) => {
                          if (avatarValue !== avatar) {
                            e.currentTarget.style.background = '#fafafa';
                          }
                        }}
                      >
                        {avatar}
                      </div>
                    ))}
                  </div>
                }
              >
                <div style={{ 
                  width: '100%',
                  height: 40,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  fontSize: 28,
                  border: '1px solid #d9d9d9',
                  borderRadius: 4,
                  cursor: 'pointer',
                  background: '#fafafa',
                }}>
                  {avatarValue || '🤖'}
                </div>
              </Popover>
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
              大模型选择 <span style={{ color: '#999', fontWeight: 'normal', fontSize: 12 }}>（第一个为主模型，不参与排序；其他模型可自由排序）</span>
            </span>
          }
          required
        >
          <Row gutter={16}>
            <Col span={12}>
              <div style={{ marginBottom: 8, fontWeight: 500, display: 'flex', alignItems: 'center', gap: 12 }}>
                <span>可选模型</span>
                <Checkbox
                  checked={showOnlyAvailable}
                  onChange={(e) => setShowOnlyAvailable(e.target.checked)}
                >
                  只展示可用模型
                </Checkbox>
                <Button
                  type="link"
                  size="small"
                  icon={<ReloadOutlined />}
                  onClick={handleTestAllModels}
                  loading={testing}
                  style={{ padding: 0 }}
                >
                  重新测试
                </Button>
              </div>
              <div style={{ 
                border: '1px solid #d9d9d9', 
                borderRadius: 4, 
                height: 300, 
                overflow: 'auto',
                padding: 8 
              }}>
                {getAvailableModels().length === 0 ? (
                  <div style={{ color: '#999', textAlign: 'center', padding: 20 }}>
                    暂无可选模型
                  </div>
                ) : (
                  getAvailableModels().map(config => (
                    <div key={config.id} style={{ marginBottom: 8 }}>
                      <div style={{ fontWeight: 500, marginBottom: 4, color: '#1890ff' }}>
                        {config.name} ({config.provider})
                      </div>
                      {config.models?.map(model => {
                        const isUnavailable = model.availabilityStatus === 0;
                        
                        return (
                          <Tooltip
                            key={`${config.id}|${model.id}`}
                            title={isUnavailable ? `不可用: ${model.testResult || '模型验证失败'}` : ''}
                          >
                            <div
                              style={{
                                padding: '8px 12px',
                                cursor: isUnavailable ? 'not-allowed' : 'pointer',
                                borderRadius: 4,
                                marginBottom: 4,
                                background: isUnavailable ? '#f5f5f5' : '#fafafa',
                                opacity: isUnavailable ? 0.5 : 1,
                                transition: 'all 0.3s',
                              }}
                              onClick={() => {
                                if (isUnavailable) return;
                                const val = `${config.id}|${model.id}`;
                                console.log('点击模型:', val);
                                handleModelSelect(val);
                              }}
                              onMouseEnter={(e) => {
                                if (!isUnavailable) {
                                  e.currentTarget.style.background = '#e6f7ff';
                                }
                              }}
                              onMouseLeave={(e) => {
                                e.currentTarget.style.background = isUnavailable ? '#f5f5f5' : '#fafafa';
                              }}
                            >
                              <Space>
                                <Tag color="blue" style={{ margin: 0 }}>{config.name}</Tag>
                                <span style={{ 
                                  textDecoration: isUnavailable ? 'line-through' : 'none',
                                  color: isUnavailable ? '#999' : 'inherit'
                                }}>
                                  {model.displayName || model.modelName}
                                </span>
                                {isUnavailable && (
                                  <Tag color="red" style={{ margin: 0, fontSize: 11 }}>
                                    不可用
                                  </Tag>
                                )}
                              </Space>
                            </div>
                          </Tooltip>
                        );
                      })}
                    </div>
                  ))
                )}
              </div>
            </Col>
            <Col span={12}>
              <div style={{ marginBottom: 8, fontWeight: 500 }}>
                已选模型 ({selectedModels.length}/20)
              </div>
              <div style={{ 
                border: '1px solid #d9d9d9', 
                borderRadius: 4, 
                height: 300, 
                overflow: 'auto',
                padding: 8,
                background: selectedModels.length === 0 ? '#fafafa' : '#fff'
              }}>
                {selectedModels.length === 0 ? (
                  <div style={{ color: '#999', textAlign: 'center', padding: 40 }}>
                    请从左侧选择模型
                  </div>
                ) : (
                  selectedModels.map((model, index) => {
                    const isPrimary = index === 0;
                    
                    return (
                      <div
                        key={`${model.llmConfigId}|${model.llmModelConfigId}`}
                        style={{
                          padding: '10px 12px',
                          marginBottom: 8,
                          background: isPrimary ? '#f6ffed' : '#f0f5ff',
                          border: isPrimary ? '1px solid #b7eb8f' : '1px solid #adc6ff',
                          borderRadius: 4,
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'space-between',
                        }}
                      >
                        <Space size={4} wrap>
                          <Tag color="orange">{isPrimary ? 0 : index}</Tag>
                          <Tag color="blue">{model.llmConfigName}</Tag>
                          {isPrimary && (
                            <Tag color="green" style={{ display: 'inline-flex', alignItems: 'center' }}>
                              <StarOutlined style={{ fontSize: 10, marginRight: 4, color: '#ff4d4f' }} />
                              主模型
                            </Tag>
                          )}
                          <span style={{ fontWeight: 500 }}>{model.modelName}</span>
                        </Space>
                        <Space size="small">
                          {!isPrimary && (
                            <>
                              <Tooltip title="设为主模型">
                                <Button
                                  size="small"
                                  type="text"
                                  icon={<StarOutlined />}
                                  onClick={() => onSetPrimary(index)}
                                />
                              </Tooltip>
                              <Button
                                size="small"
                                type="text"
                                icon={<ArrowUpOutlined />}
                                onClick={() => onMoveUp(index)}
                                disabled={index === 1}
                              />
                              <Button
                                size="small"
                                type="text"
                                icon={<ArrowDownOutlined />}
                                onClick={() => onMoveDown(index)}
                                disabled={index === selectedModels.length - 1}
                              />
                            </>
                          )}
                          <Button
                            size="small"
                            type="text"
                            danger
                            icon={<DeleteOutlined />}
                            onClick={() => onRemoveModel(`${model.llmConfigId}_${model.llmModelConfigId}`)}
                          />
                        </Space>
                      </div>
                    );
                  })
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
