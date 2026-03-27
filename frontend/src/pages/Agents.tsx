import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Select, Tag, Space, message, Divider, Row, Col, Tooltip, Alert, Typography, Popconfirm, Badge, Transfer, Cascader } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, InfoCircleOutlined, PlayCircleOutlined, PauseCircleOutlined, StopOutlined, ThunderboltOutlined, ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';
import { agentService, Agent, FallbackModel, FallbackModelRequest, AgentType } from '../services/agentService';
import { agentRuntimeService, AgentRuntimeStatus } from '../services/agentRuntimeService';
import api from '../services/api';

const { Option } = Select;
const { TextArea } = Input;
const { Text } = Typography;

const AVATAR_OPTIONS = [
  '🤖', '🧠', '💻', '🎯', '📊', '🔬', '🚀', '⚡', '🌟', '🎨',
  '🦾', '🤝', '🔮', '💡', '🎭', '🦸', '🌈', '🎪', '🎠', '🎡'
];

interface LLMConfig {
  id: string;
  name: string;
  provider: string;
  models?: LlmModel[];
  endpoint?: string;
  isEnabled: boolean;
  isDefault: boolean;
}

interface LlmModel {
  id: string;
  modelName: string;
  displayName?: string;
  description?: string;
  temperature: number;
  maxTokens: number;
  contextWindow: number;
  isDefault: boolean;
  isEnabled: boolean;
  sortOrder: number;
}

interface SelectedModel {
  llmConfigId: string;
  llmConfigName: string;
  llmModelConfigId: string;
  modelName: string;
  provider: string;
}

const Agents: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);
  const [agentTypes, setAgentTypes] = useState<AgentType[]>([]);
  const [llmConfigs, setLLMConfigs] = useState<LLMConfig[]>([]);
  const [pageSize, setPageSize] = useState(10);
  const [runtimeStatuses, setRuntimeStatuses] = useState<Record<string, AgentRuntimeStatus>>({});
  const [testingAgent, setTestingAgent] = useState<string | null>(null);
  const [activatingAgent, setActivatingAgent] = useState<string | null>(null);
  const [form] = Form.useForm();
  const initializedRef = useRef(false);
  
  const [selectedFallbackModels, setSelectedFallbackModels] = useState<SelectedModel[]>([]);
  const [selectedPrimaryModel, setSelectedPrimaryModel] = useState<SelectedModel | null>(null);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadAgents();
    }
  }, []);

  const loadAgents = async () => {
    try {
      setLoading(true);
      const response = await agentService.getAllAgents();
      console.log('[DEBUG] loadAgents - response:', response);
      
      setAgents(response.agents || []);
      setAgentTypes(response.agentTypes || []);
      
      if (response.agents && response.agents.length > 0) {
        console.log('[DEBUG] loadAgents - first agent fallbackModels:', response.agents[0]?.fallbackModels);
        loadRuntimeStatuses(response.agents.map((a: Agent) => a.id));
      }
    } catch (error) {
      message.error('加载智能体列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadLLMConfigs = async () => {
    try {
      const response = await api.get('/llmconfigs');
      const configs = (response.data || []).filter((c: LLMConfig) => c.isEnabled);
      setLLMConfigs(configs);
    } catch (error) {
      console.error('加载大模型配置失败', error);
    }
  };

  const loadRuntimeStatuses = async (agentIds: string[]) => {
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
  };

  const handleActivate = async (agentId: string) => {
    setActivatingAgent(agentId);
    try {
      const status = await agentRuntimeService.activate(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
      message.success('智能体已激活');
      loadAgents();
    } catch (error: any) {
      message.error(error.response?.data?.message || '激活失败');
    } finally {
      setActivatingAgent(null);
    }
  };

  const handleSleep = async (agentId: string) => {
    try {
      const status = await agentRuntimeService.sleep(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
      message.success('智能体已休眠');
      loadAgents();
    } catch (error: any) {
      message.error(error.response?.data?.message || '休眠失败');
    }
  };

  const handleDestroy = async (agentId: string) => {
    try {
      const status = await agentRuntimeService.destroy(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
      message.success('智能体已关闭');
      loadAgents();
    } catch (error: any) {
      message.error(error.response?.data?.message || '关闭失败');
    }
  };

  const handleTest = async (agentId: string) => {
    setTestingAgent(agentId);
    try {
      const result = await agentRuntimeService.test(agentId);
      if (result.success) {
        message.success(`测试成功 (${result.latencyMs}ms)`);
        Modal.info({
          title: '智能体响应',
          content: (
            <div>
              <p><strong>状态:</strong> {result.state}</p>
              <p><strong>延迟:</strong> {result.latencyMs}ms</p>
              <p><strong>响应:</strong></p>
              <div style={{ 
                background: '#f5f5f5', 
                padding: 12, 
                borderRadius: 4,
                maxHeight: 300,
                overflow: 'auto'
              }}>
                {result.response}
              </div>
            </div>
          ),
          width: 600,
        });
      } else {
        message.error(result.message);
      }
      const status = await agentRuntimeService.getStatus(agentId);
      setRuntimeStatuses(prev => ({ ...prev, [agentId]: status }));
    } catch (error: any) {
      message.error(error.response?.data?.message || '测试失败');
    } finally {
      setTestingAgent(null);
    }
  };

  const handleCreate = async () => {
    setEditingAgent(null);
    form.resetFields();
    form.setFieldsValue({
      avatar: '🤖',
    });
    setSelectedFallbackModels([]);
    setSelectedPrimaryModel(null);
    
    if (llmConfigs.length === 0) {
      await loadLLMConfigs();
    }
    
    setModalVisible(true);
  };

  const handleEdit = async (agent: Agent) => {
    console.log('[DEBUG] handleEdit - agent:', agent);
    console.log('[DEBUG] handleEdit - llmConfigs:', llmConfigs);
    
    setEditingAgent(agent);
    form.setFieldsValue({
      name: agent.name,
      description: agent.description,
      type: agent.type,
      avatar: agent.avatar || '🤖',
      systemPrompt: agent.systemPrompt,
    });
    
    let currentLlmConfigs = llmConfigs;
    if (llmConfigs.length === 0) {
      const response = await api.get('/llmconfigs');
      currentLlmConfigs = (response.data || []).filter((c: LLMConfig) => c.isEnabled);
      setLLMConfigs(currentLlmConfigs);
    }
    
    if (agent.llmConfigId && agent.llmModelConfigId) {
      console.log('[DEBUG] handleEdit - llmConfigId:', agent.llmConfigId, 'llmModelConfigId:', agent.llmModelConfigId);
      
      const config = currentLlmConfigs.find(c => c.id === agent.llmConfigId);
      const model = config?.models?.find(m => m.id === agent.llmModelConfigId);
      
      console.log('[DEBUG] handleEdit - config:', config, 'model:', model);
      
      if (config && model) {
        setSelectedPrimaryModel({
          llmConfigId: config.id,
          llmConfigName: config.name,
          llmModelConfigId: model.id,
          modelName: model.displayName || model.modelName,
          provider: config.provider,
        });
      } else {
        setSelectedPrimaryModel({
          llmConfigId: agent.llmConfigId,
          llmConfigName: agent.llmConfigName || '',
          llmModelConfigId: agent.llmModelConfigId,
          modelName: agent.primaryModelName || '',
          provider: '',
        });
      }
    } else {
      setSelectedPrimaryModel(null);
    }
    
    if (agent.fallbackModels && agent.fallbackModels.length > 0) {
      console.log('[DEBUG] handleEdit - fallbackModels:', agent.fallbackModels);
      
      const fallbackConfigs: SelectedModel[] = agent.fallbackModels.map(fm => {
        const config = currentLlmConfigs.find(c => c.id === fm.llmConfigId);
        const model = config?.models?.find(m => m.id === fm.llmModelConfigId);
        
        console.log('[DEBUG] handleEdit - fallback config:', config, 'model:', model);
        
        if (config && model) {
          return {
            llmConfigId: config.id,
            llmConfigName: config.name,
            llmModelConfigId: model.id,
            modelName: model.displayName || model.modelName,
            provider: config.provider,
          };
        } else {
          return {
            llmConfigId: fm.llmConfigId,
            llmConfigName: fm.llmConfigName || '',
            llmModelConfigId: fm.llmModelConfigId || '',
            modelName: fm.modelName || '',
            provider: '',
          };
        }
      }).filter(m => m.llmConfigId && m.llmModelConfigId) as SelectedModel[];
      
      console.log('[DEBUG] handleEdit - fallbackConfigs:', fallbackConfigs);
      
      setSelectedFallbackModels(fallbackConfigs);
    } else {
      setSelectedFallbackModels([]);
    }
    
    setModalVisible(true);
  };

  const handleDelete = async (id: string) => {
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
  };

  const handleTypeChange = (typeCode: string) => {
    const selectedType = agentTypes.find(t => t.code === typeCode);
    if (selectedType) {
      form.setFieldsValue({
        systemPrompt: selectedType.defaultSystemPrompt,
        avatar: selectedType.icon || '🤖',
      });
    }
  };

  const handleMoveUp = (index: number) => {
    if (index > 0) {
      const newModels = [...selectedFallbackModels];
      [newModels[index - 1], newModels[index]] = [newModels[index], newModels[index - 1]];
      setSelectedFallbackModels(newModels);
    }
  };

  const handleMoveDown = (index: number) => {
    if (index < selectedFallbackModels.length - 1) {
      const newModels = [...selectedFallbackModels];
      [newModels[index], newModels[index + 1]] = [newModels[index + 1], newModels[index]];
      setSelectedFallbackModels(newModels);
    }
  };

  const handleRemoveFallbackModel = (modelId: string) => {
    setSelectedFallbackModels(selectedFallbackModels.filter(m => `${m.llmConfigId}_${m.llmModelConfigId}` !== modelId));
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (!selectedPrimaryModel) {
        message.warning('请选择主模型');
        return;
      }
      
      const fallbackModels: FallbackModelRequest[] = selectedFallbackModels.map((model, index) => ({
        llmConfigId: parseInt(model.llmConfigId),
        llmModelConfigId: parseInt(model.llmModelConfigId),
        priority: index + 1,
      }));
      
      const agentData = {
        name: values.name,
        description: values.description,
        type: values.type,
        avatar: values.avatar || '🤖',
        systemPrompt: values.systemPrompt,
        llmConfigId: parseInt(selectedPrimaryModel.llmConfigId),
        llmModelConfigId: parseInt(selectedPrimaryModel.llmModelConfigId),
        fallbackModels: fallbackModels.length > 0 ? fallbackModels : undefined,
      };
      
      if (editingAgent) {
        await agentService.updateAgent(editingAgent.id, agentData);
        message.success('更新成功');
      } else {
        await agentService.createAgent(agentData);
        message.success('创建成功');
      }
      setModalVisible(false);
      loadAgents();
    } catch (error) {
      message.error('操作失败');
    }
  };

  const getLLMInfo = (llmConfigId?: string, llmModelConfigId?: string) => {
    if (!llmConfigId) return null;
    const llm = llmConfigs.find(l => l.id === llmConfigId);
    if (!llm) return null;
    
    let modelName = '-';
    if (llmModelConfigId) {
      const model = llm.models?.find(m => m.id === llmModelConfigId);
      modelName = model?.displayName || model?.modelName || '-';
    } else {
      const defaultModel = llm.models?.[0];
      modelName = defaultModel?.displayName || defaultModel?.modelName || '-';
    }
    
    return {
      name: llm.name,
      provider: llm.provider,
      modelName
    };
  };

  const columns = [
    {
      title: '头像',
      dataIndex: 'avatar',
      key: 'avatar',
      width: 50,
      align: 'center' as const,
      render: (avatar: string) => (
        <span style={{ fontSize: 24 }}>{avatar || '🤖'}</span>
      ),
    },
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
      width: 70,
      render: (name: string) => <Text strong>{name}</Text>,
    },
    {
      title: '类型',
      dataIndex: 'type',
      key: 'type',
      width: 70,
      render: (type: string) => {
        const t = agentTypes.find(at => at.code === type);
        return <Tag color="blue">{t?.name || type}</Tag>;
      },
    },
    {
      title: '主模型',
      key: 'llmConfig',
      width: 120,
      render: (_: any, record: Agent) => {
        if (record.primaryModelName && record.llmConfigName) {
          return (
            <Tooltip title={`${record.llmConfigName} - ${record.primaryModelName}`}>
              <Tag color="green">{record.llmConfigName}</Tag>
              <div style={{ fontSize: 11, color: '#666', marginTop: 2 }}>
                {record.primaryModelName}
              </div>
            </Tooltip>
          );
        }
        return <Tag color="red">未配置</Tag>;
      },
    },
    {
      title: '副模型',
      key: 'fallbackModels',
      width: 150,
      render: (_: any, record: Agent) => {
        if (!record.fallbackModels || record.fallbackModels.length === 0) {
          return <Text type="secondary" style={{ fontSize: 12 }}>无</Text>;
        }
        return (
          <Space direction="vertical" size={2}>
            {record.fallbackModels.map((fm, index) => (
              <div key={index} style={{ marginBottom: 4 }}>
                <Tag color="blue" style={{ fontSize: 11 }}>
                  {fm.llmConfigName || `配置:${fm.llmConfigId}`}
                </Tag>
                {fm.modelName && (
                  <div style={{ fontSize: 11, color: '#666', marginTop: 2 }}>
                    {fm.modelName}
                  </div>
                )}
              </div>
            ))}
          </Space>
        );
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (status: string) => {
        const statusMap: Record<string, { color: string; label: string }> = {
          Active: { color: 'green', label: '活跃' },
          Inactive: { color: 'default', label: '未激活' },
          Busy: { color: 'orange', label: '忙碌' },
          Error: { color: 'red', label: '错误' },
        };
        const s = statusMap[status] || { color: 'default', label: status };
        return <Tag color={s.color}>{s.label}</Tag>;
      },
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: 150,
      render: (description: string) => (
        <div style={{ 
          whiteSpace: 'pre-wrap', 
          wordBreak: 'break-word',
          maxHeight: 60,
          overflow: 'hidden',
          fontSize: 12,
          color: '#666'
        }}>
          {description || '-'}
        </div>
      ),
    },
    {
      title: '系统提示词',
      key: 'systemPrompt',
      width: 350,
      render: (_: any, record: Agent) => {
        const prompt = record.systemPrompt;
        if (!prompt) return <Text type="secondary" style={{ fontSize: 12 }}>-</Text>;
        
        return (
          <Tooltip title={<div style={{ whiteSpace: 'pre-wrap', maxWidth: 400 }}>{prompt}</div>} placement="topLeft">
            <div style={{ 
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              overflow: 'hidden',
              fontSize: 12,
              color: '#666',
              maxHeight: 60,
              lineHeight: '20px',
              display: '-webkit-box',
              WebkitLineClamp: 3,
              WebkitBoxOrient: 'vertical' as const,
            }}>
              {prompt}
            </div>
          </Tooltip>
        );
      },
    },
    {
      title: '运行时状态',
      key: 'runtimeStatus',
      width: 100,
      render: (_: any, record: Agent) => {
        const runtimeStatus = runtimeStatuses[record.id];
        const stateColors: Record<string, string> = {
          Uninitialized: 'default',
          Ready: 'green',
          Busy: 'orange',
          Sleeping: 'purple',
          Error: 'red',
        };
        const stateLabels: Record<string, string> = {
          Uninitialized: '未初始化',
          Ready: '就绪',
          Busy: '忙碌',
          Sleeping: '休眠',
          Error: '错误',
        };
        return (
          <Space direction="vertical" size={2}>
            <Tag color={stateColors[runtimeStatus?.state || 'Uninitialized']}>
              {stateLabels[runtimeStatus?.state || 'Uninitialized']}
            </Tag>
            {runtimeStatus?.isAlive && (
              <Badge status="success" text="运行中" style={{ fontSize: 11 }} />
            )}
          </Space>
        );
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      fixed: 'right' as const,
      render: (_: any, record: Agent) => {
        const runtimeStatus = runtimeStatuses[record.id];
        const isAlive = runtimeStatus?.isAlive;
        const isActivating = activatingAgent === record.id;
        const isTesting = testingAgent === record.id;
        
        return (
          <Space size={0} wrap>
            {isAlive ? (
              <>
                <Tooltip title="测试智能体">
                  <Button 
                    type="link" 
                    size="small" 
                    icon={<ThunderboltOutlined />} 
                    onClick={() => handleTest(record.id)}
                    loading={isTesting}
                  >
                    测试
                  </Button>
                </Tooltip>
                <Popconfirm
                  title="确定让智能体休眠？"
                  description="休眠后会释放部分资源，但保留配置"
                  onConfirm={() => handleSleep(record.id)}
                  okText="确定"
                  cancelText="取消"
                >
                  <Button type="link" size="small" icon={<PauseCircleOutlined />}>
                    休眠
                  </Button>
                </Popconfirm>
                <Popconfirm
                  title="确定关闭智能体？"
                  description="关闭后会完全释放资源"
                  onConfirm={() => handleDestroy(record.id)}
                  okText="确定"
                  cancelText="取消"
                >
                  <Button type="link" size="small" danger icon={<StopOutlined />}>
                    关闭
                  </Button>
                </Popconfirm>
              </>
            ) : (
              <Button 
                type="link" 
                size="small" 
                icon={<PlayCircleOutlined />} 
                onClick={() => handleActivate(record.id)}
                loading={isActivating}
                style={{ color: '#52c41a' }}
              >
                激活
              </Button>
            )}
            <Button type="link" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
              编辑
            </Button>
            <Button type="link" size="small" danger onClick={() => handleDelete(record.id)}>
              删除
            </Button>
          </Space>
        );
      },
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2 style={{ margin: 0 }}>智能体管理</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          新建智能体
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={agents}
        rowKey="id"
        loading={loading}
        pagination={{ 
          pageSize: pageSize,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
          showTotal: (total) => `共 ${total} 条`,
          onShowSizeChange: (_current, size) => setPageSize(size)
        }}
        size="small"
        scroll={{ x: 1500 }}
      />

      <Modal
        title={editingAgent ? '编辑智能体' : '新建智能体'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={900}
        okText="保存"
        cancelText="取消"
      >
        {llmConfigs.length === 0 && (
          <Alert
            message="未配置大模型"
            description="请先前往【大模型配置】页面添加大模型配置，否则无法创建智能体"
            type="warning"
            showIcon
            style={{ marginBottom: 16 }}
          />
        )}
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col span={6}>
              <Form.Item name="avatar" label="头像">
                <Select placeholder="选择头像">
                  {AVATAR_OPTIONS.map(emoji => (
                    <Option key={emoji} value={emoji}>
                      <span style={{ fontSize: 20 }}>{emoji}</span>
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col span={9}>
              <Form.Item
                name="name"
                label="智能体名称"
                rules={[{ required: true, message: '请输入智能体名称' }]}
              >
                <Input placeholder="请输入智能体名称" />
              </Form.Item>
            </Col>
            <Col span={9}>
              <Form.Item
                name="type"
                label="智能体类型"
                rules={[{ required: true, message: '请选择智能体类型' }]}
              >
                <Select placeholder="选择类型" onChange={handleTypeChange}>
                  {agentTypes.map(type => (
                    <Option key={type.code} value={type.code}>
                      <Space>
                        <span>{type.icon}</span>
                        <span>{type.name}</span>
                        {type.description && (
                          <Tooltip title={type.description}>
                            <InfoCircleOutlined style={{ color: '#999' }} />
                          </Tooltip>
                        )}
                      </Space>
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="description" label="描述">
            <Input.TextArea rows={2} placeholder="请输入智能体描述" />
          </Form.Item>

          <Divider orientation="left">主模型配置</Divider>
          
          <Row gutter={16}>
            <Col span={24}>
              <Form.Item
                label="主模型"
                required
                tooltip="选择大模型配置和具体模型"
              >
                <Cascader
                  value={selectedPrimaryModel ? [selectedPrimaryModel.llmConfigId, selectedPrimaryModel.llmModelConfigId] : undefined}
                  onChange={(value, selectedOptions) => {
                    console.log('[DEBUG] Cascader onChange - value:', value);
                    if (value && value.length === 2) {
                      const config = llmConfigs.find(c => c.id === value[0]);
                      const model = config?.models?.find(m => m.id === value[1]);
                      console.log('[DEBUG] Cascader onChange - config:', config, 'model:', model);
                      if (config && model) {
                        setSelectedPrimaryModel({
                          llmConfigId: config.id,
                          llmConfigName: config.name,
                          llmModelConfigId: model.id,
                          modelName: model.displayName || model.modelName,
                          provider: config.provider,
                        });
                      }
                    } else {
                      setSelectedPrimaryModel(null);
                    }
                  }}
                  options={llmConfigs.map(config => ({
                    value: config.id,
                    label: (
                      <Space>
                        <span>{config.name}</span>
                        <Tag color="blue">{config.provider}</Tag>
                        {config.isDefault && <Tag color="gold">默认</Tag>}
                      </Space>
                    ),
                    children: config.models?.filter(m => m.isEnabled).map(model => ({
                      value: model.id,
                      label: (
                        <Space>
                          <span>{model.displayName || model.modelName}</span>
                          {model.isDefault && <Tag color="green">默认</Tag>}
                        </Space>
                      ),
                    })) || [],
                  }))}
                  placeholder="请选择大模型配置和具体模型"
                  showSearch
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
          </Row>

          <Divider orientation="left">副模型配置（故障转移）</Divider>

          <Row gutter={16}>
            <Col span={24}>
              <Form.Item
                label="添加副模型"
                tooltip="选择大模型配置和具体模型作为副模型，用于主模型故障时自动切换"
              >
                <Cascader
                  onChange={(value, selectedOptions) => {
                    if (value && value.length === 2) {
                      const config = llmConfigs.find(c => c.id === value[0]);
                      const model = config?.models?.find(m => m.id === value[1]);
                      if (config && model) {
                        const newModel: SelectedModel = {
                          llmConfigId: config.id,
                          llmConfigName: config.name,
                          llmModelConfigId: model.id,
                          modelName: model.displayName || model.modelName,
                          provider: config.provider,
                        };
                        
                        const isDuplicate = selectedFallbackModels.some(
                          m => m.llmConfigId === newModel.llmConfigId && 
                               m.llmModelConfigId === newModel.llmModelConfigId
                        );
                        
                        if (isDuplicate) {
                          message.warning('该模型已在副模型列表中');
                          return;
                        }
                        
                        setSelectedFallbackModels([...selectedFallbackModels, newModel]);
                      }
                    }
                  }}
                  options={llmConfigs.map(config => ({
                    value: config.id,
                    label: (
                      <Space>
                        <span>{config.name}</span>
                        <Tag color="blue">{config.provider}</Tag>
                        {config.isDefault && <Tag color="gold">默认</Tag>}
                      </Space>
                    ),
                    children: config.models?.filter(m => m.isEnabled).map(model => ({
                      value: model.id,
                      label: (
                        <Space>
                          <span>{model.displayName || model.modelName}</span>
                          {model.isDefault && <Tag color="green">默认</Tag>}
                        </Space>
                      ),
                    })) || [],
                  }))}
                  placeholder="请选择大模型配置和具体模型"
                  showSearch
                  style={{ width: '100%' }}
                  value={undefined}
                />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={24}>
              <div style={{ marginBottom: 8 }}>
                <Text strong>已选副模型（按优先级排序）</Text>
              </div>
              <div style={{ 
                border: '1px solid #d9d9d9', 
                borderRadius: 4, 
                padding: 8, 
                minHeight: 200,
                maxHeight: 300,
                overflowY: 'auto'
              }}>
                {selectedFallbackModels.length === 0 ? (
                  <Text type="secondary">未选择副模型</Text>
                ) : (
                  selectedFallbackModels.map((model, index) => (
                    <div 
                      key={`${model.llmConfigId}_${model.llmModelConfigId}`}
                      style={{ 
                        padding: '8px 12px', 
                        marginBottom: 4, 
                        background: '#e6f7ff',
                        borderRadius: 4,
                        display: 'flex',
                        justifyContent: 'space-between',
                        alignItems: 'center'
                      }}
                    >
                      <Space direction="vertical" size={0}>
                        <Space>
                          <Tag color="orange">{index + 1}</Tag>
                          <span>{model.llmConfigName}</span>
                          <Tag color="blue">{model.provider}</Tag>
                        </Space>
                        <Space>
                          <span style={{ color: '#666' }}>{model.modelName}</span>
                        </Space>
                      </Space>
                      <Space>
                        <Button 
                          size="small" 
                          icon={<ArrowUpOutlined />} 
                          onClick={() => handleMoveUp(index)}
                          disabled={index === 0}
                        />
                        <Button 
                          size="small" 
                          icon={<ArrowDownOutlined />} 
                          onClick={() => handleMoveDown(index)}
                          disabled={index === selectedFallbackModels.length - 1}
                        />
                        <Button 
                          size="small" 
                          danger
                          onClick={() => handleRemoveFallbackModel(`${model.llmConfigId}_${model.llmModelConfigId}`)}
                        >
                          移除
                        </Button>
                      </Space>
                    </div>
                  ))
                )}
              </div>
            </Col>
          </Row>

          <Divider orientation="left">提示词配置</Divider>

          <Form.Item 
            name="systemPrompt" 
            label="系统提示词"
            tooltip="选择智能体类型后会自动填充默认提示词，您可以根据需要修改"
          >
            <TextArea 
              rows={6} 
              placeholder="请输入系统提示词，定义智能体的角色和行为..."
            />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Agents;
