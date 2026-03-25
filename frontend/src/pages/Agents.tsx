import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Select, Tag, Space, message, Divider, InputNumber, Row, Col, Tooltip, Avatar, Alert, Typography, Popconfirm, Badge } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, InfoCircleOutlined, PlayCircleOutlined, PauseCircleOutlined, StopOutlined, ThunderboltOutlined, ReloadOutlined } from '@ant-design/icons';
import { agentService, Agent } from '../services/agentService';
import { agentRuntimeService, AgentRuntimeStatus } from '../services/agentRuntimeService';
import api from '../services/api';

const { Option } = Select;
const { TextArea } = Input;
const { Text } = Typography;

const AVATAR_OPTIONS = [
  '🤖', '🧠', '💻', '🎯', '📊', '🔬', '🚀', '⚡', '🌟', '🎨',
  '🦾', '🤝', '🔮', '💡', '🎭', '🦸', '🌈', '🎪', '🎠', '🎡'
];

interface AgentType {
  id: string;
  code: string;
  name: string;
  description?: string;
  defaultSystemPrompt?: string;
  defaultTemperature: number;
  defaultMaxTokens: number;
  icon?: string;
  isSystem: boolean;
  isEnabled: boolean;
}

interface LLMConfig {
  id: string;
  name: string;
  provider: string;
  models?: { modelName: string; displayName?: string }[];
  endpoint?: string;
  isEnabled: boolean;
  isDefault: boolean;
}

interface ModelConfig {
  systemPrompt?: string;
  temperature?: number;
  maxTokens?: number;
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

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadAgents();
      loadAgentTypes();
      loadLLMConfigs();
    }
  }, []);

  const loadAgents = async () => {
    try {
      setLoading(true);
      const data = await agentService.getAllAgents();
      setAgents(data);
      loadRuntimeStatuses(data.map(a => a.id));
    } catch (error) {
      message.error('加载智能体列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadAgentTypes = async () => {
    try {
      const response = await api.get('/agenttypes/enabled');
      setAgentTypes(response.data || []);
    } catch (error) {
      console.error('加载智能体类型失败', error);
    }
  };

  const loadLLMConfigs = async () => {
    try {
      const response = await api.get('/llmconfigs');
      setLLMConfigs((response.data || []).filter((c: LLMConfig) => c.isEnabled));
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

  const parseConfiguration = (configuration?: string): ModelConfig => {
    try {
      return configuration ? JSON.parse(configuration) : {};
    } catch (e) {
      return {};
    }
  };

  const handleCreate = () => {
    setEditingAgent(null);
    form.resetFields();
    form.setFieldsValue({
      avatar: '🤖',
      temperature: 0.7,
      maxTokens: 4096,
    });
    setModalVisible(true);
  };

  const handleEdit = (agent: Agent) => {
    setEditingAgent(agent);
    const config = parseConfiguration(agent.configuration);
    
    form.setFieldsValue({
      name: agent.name,
      description: agent.description,
      type: agent.type,
      avatar: agent.avatar || '🤖',
      llmConfigId: agent.llmConfigId,
      systemPrompt: config.systemPrompt,
      temperature: config.temperature || 0.7,
      maxTokens: config.maxTokens || 4096,
    });
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
        temperature: selectedType.defaultTemperature,
        maxTokens: selectedType.defaultMaxTokens,
        avatar: selectedType.icon || '🤖',
      });
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (!values.llmConfigId) {
        message.warning('请先选择大模型配置，如未配置请前往"大模型配置"页面添加');
        return;
      }
      
      const modelConfig: ModelConfig = {
        systemPrompt: values.systemPrompt,
        temperature: values.temperature || 0.7,
        maxTokens: values.maxTokens || 4096,
      };
      
      const agentData = {
        name: values.name,
        description: values.description,
        type: values.type,
        avatar: values.avatar || '🤖',
        configuration: JSON.stringify(modelConfig),
        llmConfigId: values.llmConfigId,
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

  const getLLMInfo = (llmConfigId?: string) => {
    if (!llmConfigId) return null;
    const llm = llmConfigs.find(l => l.id === llmConfigId);
    if (!llm) return null;
    const defaultModel = llm.models?.[0];
    return {
      name: llm.name,
      provider: llm.provider,
      modelName: defaultModel?.displayName || defaultModel?.modelName || '-'
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
      title: '大模型',
      key: 'llmConfig',
      width: 100,
      render: (_: any, record: Agent) => {
        const llmInfo = getLLMInfo(record.llmConfigId);
        return llmInfo ? (
          <Tooltip title={`${llmInfo.provider} - ${llmInfo.modelName}`}>
            <Tag color="green">{llmInfo.name}</Tag>
          </Tooltip>
        ) : (
          <Tag color="red">未配置</Tag>
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
        const config = parseConfiguration(record.configuration);
        const prompt = config.systemPrompt;
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
      />

      <Modal
        title={editingAgent ? '编辑智能体' : '新建智能体'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={700}
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

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                name="llmConfigId"
                label="大模型配置"
                rules={[{ required: true, message: '请选择大模型配置' }]}
              >
                <Select placeholder="选择已配置的大模型" showSearch optionFilterProp="children">
                  {llmConfigs.map(config => {
                    const defaultModel = config.models?.[0];
                    return (
                      <Option key={config.id} value={config.id}>
                        <Space>
                          <span>{config.name}</span>
                          <Tag color="blue">{config.provider}</Tag>
                          {defaultModel && <Tag>{defaultModel.displayName || defaultModel.modelName}</Tag>}
                          {config.isDefault && <Tag color="gold">默认</Tag>}
                        </Space>
                      </Option>
                    );
                  })}
                </Select>
              </Form.Item>
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

          <Row gutter={16}>
            <Col span={12}>
              <Form.Item 
                name="temperature" 
                label="温度参数"
                tooltip="控制回复的随机性，0-1之间，值越小越确定"
              >
                <InputNumber min={0} max={1} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item 
                name="maxTokens" 
                label="最大Token数"
                tooltip="控制回复的最大长度"
              >
                <InputNumber min={100} max={128000} step={100} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>
    </div>
  );
};

export default Agents;
