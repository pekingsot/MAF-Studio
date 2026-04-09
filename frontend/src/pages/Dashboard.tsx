import React, { useEffect, useState, useRef } from 'react';
import { Row, Col, Card, Statistic, Table, Tag, Badge, Tooltip, Space, Button, List, Typography, Divider, Popover, Descriptions } from 'antd';
import { RobotOutlined, TeamOutlined, MessageOutlined, CheckCircleOutlined, ApiOutlined, CheckCircleFilled, CloseCircleFilled, LoadingOutlined, StarFilled, ClockCircleOutlined, DesktopOutlined, DockerOutlined } from '@ant-design/icons';
import { agentService, Agent } from '../services/agentService';
import { collaborationService, Collaboration } from '../services/collaborationService';
import api from '../services/api';

const { Text } = Typography;

interface EnvironmentInfo {
  dotNetVersion: string;
  gitVersion: string;
  pythonVersion: string;
  nodeVersion: string;
  osInfo: string;
  machineName: string;
  processorCount: number;
  runtime: string;
  osArchitecture: string;
  processArchitecture: string;
  containerized: boolean;
}

interface AgentType {
  id: number;
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

interface LLMModelConfig {
  id: number;
  modelName: string;
  displayName?: string;
  isDefault: boolean;
  isEnabled: boolean;
}

interface LLMTestRecord {
  id: number;
  llmConfigId: number;
  llmModelConfigId?: number;
  provider: string;
  modelName?: string;
  isSuccess: boolean;
  message?: string;
  latencyMs: number;
  testedAt: string;
}

interface LLMConfig {
  id: number;
  name: string;
  provider: string;
  isDefault: boolean;
  isEnabled: boolean;
  models: LLMModelConfig[];
  testRecords?: LLMTestRecord[];
}

interface ConnectionStatus {
  success: boolean;
  message: string;
  latencyMs: number;
}

const Dashboard: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [agentTypes, setAgentTypes] = useState<AgentType[]>([]);
  const [llmConfigs, setLlmConfigs] = useState<LLMConfig[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<Record<string, ConnectionStatus>>({});
  const [testingLLM, setTestingLLM] = useState(false);
  const [loading, setLoading] = useState(true);
  const [environmentInfo, setEnvironmentInfo] = useState<EnvironmentInfo | null>(null);
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadData();
    }
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      const [agentsData, collaborationsData, llmConfigsData, agentTypesData, envInfoData] = await Promise.all([
        agentService.getAllAgents(),
        collaborationService.getAllCollaborations(),
        api.get<LLMConfig[]>('/llmconfigs'),
        api.get<AgentType[]>('/agenttypes/enabled'),
        api.get<EnvironmentInfo>('/system/environment').catch(() => ({ data: null })),
      ]);
      setAgents(agentsData || []);
      setCollaborations(collaborationsData);
      setLlmConfigs(llmConfigsData.data.filter(c => c.isEnabled));
      setAgentTypes(agentTypesData.data || []);
      setEnvironmentInfo(envInfoData.data);
    } catch (error) {
      console.error('Failed to load data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleTestLLMConnections = async () => {
    if (llmConfigs.length === 0) return;
    
    setTestingLLM(true);
    try {
      const response = await api.post<Record<string, ConnectionStatus>>('/llmconfigs/test-all');
      setConnectionStatus(response.data);
      loadData();
    } catch (error) {
      console.error('Failed to test LLM connections:', error);
    } finally {
      setTestingLLM(false);
    }
  };

  const getProviderLabel = (provider: string) => {
    const providers: Record<string, string> = {
      'openai': 'OpenAI',
      'anthropic': 'Anthropic',
      'deepseek': 'DeepSeek',
      'qwen': '通义千问',
      'zhipu': '智谱AI',
      'moonshot': 'Moonshot',
      'baidu': '百度文心',
      'minimax': 'MiniMax',
      'yi': '零一万物',
      'baichuan': '百川智能',
      'openai_compatible': 'OpenAI兼容',
    };
    return providers[provider] || provider;
  };

  const getProviderColor = (provider: string) => {
    const colors: Record<string, string> = {
      'qwen': '#ff6a00',
      'openai': '#10a37f',
      'deepseek': '#0066ff',
      'zhipu': '#1e88e5',
      'anthropic': '#d97706',
      'moonshot': '#7c3aed',
      'baidu': '#2932e1',
      'minimax': '#e11d48',
      'yi': '#0ea5e9',
      'baichuan': '#f97316',
      'openai_compatible': '#6b7280',
    };
    return colors[provider] || '#1890ff';
  };

  const activeAgents = agents.filter(a => a.status === 'Active').length;
  const activeCollaborations = collaborations.filter(c => c.status === 'Active').length;
  const totalTasks = collaborations.reduce((sum, c) => sum + c.tasks.length, 0);
  const completedTasks = collaborations.reduce((sum, c) => 
    sum + c.tasks.filter(t => t.status === 'Completed').length, 0);

  const agentColumns = [
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '类型',
      dataIndex: 'type',
      key: 'type',
      render: (type: string) => {
        const t = agentTypes.find(at => at.code === type);
        return <Tag color="blue">{t?.name || type}</Tag>;
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
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
  ];

  const collaborationColumns = [
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '智能体数量',
      dataIndex: 'agents',
      key: 'agents',
      width: 100,
      render: (agents: any[]) => agents.length,
    },
    {
      title: '任务数量',
      dataIndex: 'tasks',
      key: 'tasks',
      width: 100,
      render: (tasks: any[]) => tasks.length,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => {
        const statusMap: Record<string, { color: string; label: string }> = {
          Active: { color: 'green', label: '活跃' },
          Paused: { color: 'orange', label: '已暂停' },
          Completed: { color: 'blue', label: '已完成' },
          Cancelled: { color: 'red', label: '已取消' },
        };
        const s = statusMap[status] || { color: 'default', label: status };
        return <Tag color={s.color}>{s.label}</Tag>;
      },
    },
  ];

  const getLastTestTime = (config: LLMConfig) => {
    if (config.testRecords && config.testRecords.length > 0) {
      const lastRecord = config.testRecords[0];
      return new Date(lastRecord.testedAt).toLocaleString();
    }
    return null;
  };

  const getLastTestStatus = (config: LLMConfig) => {
    if (config.testRecords && config.testRecords.length > 0) {
      const lastRecord = config.testRecords[0];
      return {
        success: lastRecord.isSuccess,
        latencyMs: lastRecord.latencyMs,
        message: lastRecord.message
      };
    }
    return null;
  };

  const getAvailableModelCount = (config: LLMConfig) => {
    if (!config.testRecords) return 0;
    const testedModelIds = new Set(config.testRecords.filter(r => r.isSuccess).map(r => r.modelName));
    return testedModelIds.size;
  };

  const renderModelPopover = (models: LLMModelConfig[]) => {
    if (!models || models.length === 0) {
      return <Text type="secondary">暂无模型</Text>;
    }
    return (
      <div style={{ maxWidth: 300 }}>
        {models.map((model, index) => (
          <div key={model.id} style={{ padding: '4px 0', borderBottom: index < models.length - 1 ? '1px solid #f0f0f0' : 'none' }}>
            <Space>
              {model.isDefault && <StarFilled style={{ color: '#faad14', fontSize: 12 }} />}
              <Text>{model.displayName || model.modelName}</Text>
              {!model.isEnabled && <Tag color="red" style={{ fontSize: 10 }}>禁用</Tag>}
            </Space>
          </div>
        ))}
      </div>
    );
  };

  const totalModelCount = llmConfigs.reduce((sum, c) => sum + (c.models?.length || 0), 0);
  const totalAvailableCount = llmConfigs.reduce((sum, c) => sum + getAvailableModelCount(c), 0);

  return (
    <div>
      <h2 style={{ marginBottom: 24 }}>仪表盘</h2>
      
      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={6}>
          <Card>
            <Statistic
              title="智能体总数"
              value={agents.length}
              prefix={<RobotOutlined />}
              loading={loading}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="活跃智能体"
              value={activeAgents}
              prefix={<RobotOutlined />}
              loading={loading}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="团队总数"
              value={collaborations.length}
              prefix={<TeamOutlined />}
              loading={loading}
            />
          </Card>
        </Col>
        <Col span={6}>
          <Card>
            <Statistic
              title="已完成任务"
              value={completedTasks}
              suffix={`/ ${totalTasks}`}
              prefix={<CheckCircleOutlined />}
              loading={loading}
            />
          </Card>
        </Col>
      </Row>

      {environmentInfo && (
        <Row gutter={16} style={{ marginBottom: 24 }}>
          <Col span={24}>
            <Card 
              title={
                <Space>
                  {environmentInfo.containerized ? <DockerOutlined /> : <DesktopOutlined />}
                  <span>运行环境</span>
                  {environmentInfo.containerized && <Tag color="blue">Docker 容器</Tag>}
                </Space>
              }
              size="small"
            >
              <Descriptions column={4} size="small">
                <Descriptions.Item label=".NET 版本">{environmentInfo.dotNetVersion}</Descriptions.Item>
                <Descriptions.Item label="Git 版本">{environmentInfo.gitVersion}</Descriptions.Item>
                <Descriptions.Item label="Python 版本">{environmentInfo.pythonVersion}</Descriptions.Item>
                <Descriptions.Item label="Node 版本">{environmentInfo.nodeVersion}</Descriptions.Item>
                <Descriptions.Item label="操作系统">{environmentInfo.osInfo}</Descriptions.Item>
                <Descriptions.Item label="机器名">{environmentInfo.machineName}</Descriptions.Item>
                <Descriptions.Item label="CPU 核心数">{environmentInfo.processorCount}</Descriptions.Item>
                <Descriptions.Item label="运行时">{environmentInfo.runtime}</Descriptions.Item>
                <Descriptions.Item label="系统架构">{environmentInfo.osArchitecture}</Descriptions.Item>
                <Descriptions.Item label="进程架构">{environmentInfo.processArchitecture}</Descriptions.Item>
              </Descriptions>
            </Card>
          </Col>
        </Row>
      )}

      <Row gutter={16} style={{ marginBottom: 24 }}>
        <Col span={24}>
          <Card 
            title={
              <Space>
                <ApiOutlined />
                <span>模型供应商</span>
                <Tag color="blue">{llmConfigs.length} 个供应商</Tag>
                <Popover 
                  content={renderModelPopover(llmConfigs.flatMap(c => c.models || []))} 
                  title="已配置模型"
                  trigger="hover"
                >
                  <Tag color="cyan" style={{ cursor: 'pointer' }}>{totalModelCount} 个模型</Tag>
                </Popover>
                <Tag color="green">{totalAvailableCount} 个可用</Tag>
              </Space>
            }
            extra={
              <Button 
                type="primary" 
                size="small" 
                icon={<ApiOutlined />}
                loading={testingLLM}
                onClick={handleTestLLMConnections}
                disabled={llmConfigs.length === 0}
              >
                测试全部连通性
              </Button>
            }
            loading={loading}
          >
            {llmConfigs.length === 0 ? (
              <div style={{ textAlign: 'center', color: '#999', padding: '20px 0' }}>
                暂无已启用的大模型配置
              </div>
            ) : (
              <List
                grid={{ gutter: 16, column: 3 }}
                dataSource={llmConfigs}
                renderItem={(config) => {
                  const status = connectionStatus[config.id];
                  const lastTest = getLastTestStatus(config);
                  const lastTestTime = getLastTestTime(config);
                  const providerColor = getProviderColor(config.provider);
                  const modelCount = config.models?.length || 0;
                  const availableCount = getAvailableModelCount(config);

                  return (
                    <List.Item>
                      <Card 
                        size="small"
                        style={{ 
                          borderColor: lastTest ? (lastTest.success ? '#52c41a' : '#ff4d4f') : '#d9d9d9',
                          borderWidth: 2
                        }}
                      >
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                          <div style={{ flex: 1 }}>
                            <div style={{ marginBottom: 8 }}>
                              <Space>
                                {config.isDefault && <StarFilled style={{ color: '#faad14' }} />}
                                <Text strong style={{ fontSize: 15 }}>{config.name}</Text>
                              </Space>
                            </div>
                            
                            <div style={{ marginBottom: 8 }}>
                              <Tag 
                                color={providerColor} 
                                style={{ 
                                  fontWeight: 'bold', 
                                  fontSize: 13,
                                  padding: '2px 8px'
                                }}
                              >
                                {getProviderLabel(config.provider)}
                              </Tag>
                            </div>

                            <Space size={4}>
                              <Popover 
                                content={renderModelPopover(config.models || [])} 
                                title="模型列表"
                                trigger="hover"
                              >
                                <Tag color="blue" style={{ cursor: 'pointer' }}>{modelCount} 模型</Tag>
                              </Popover>
                              {availableCount > 0 && (
                                <Tag color="green">{availableCount} 可用</Tag>
                              )}
                            </Space>
                          </div>

                          <div style={{ textAlign: 'right', minWidth: 80 }}>
                            {testingLLM ? (
                              <Space direction="vertical" size={2}>
                                <LoadingOutlined spin style={{ fontSize: 20, color: '#1890ff' }} />
                                <Text type="secondary" style={{ fontSize: 11 }}>测试中...</Text>
                              </Space>
                            ) : status ? (
                              <Space direction="vertical" size={2}>
                                <Tooltip title={`${status.message} (${status.latencyMs}ms)`}>
                                  {status.success ? (
                                    <CheckCircleFilled style={{ fontSize: 24, color: '#52c41a' }} />
                                  ) : (
                                    <CloseCircleFilled style={{ fontSize: 24, color: '#ff4d4f' }} />
                                  )}
                                </Tooltip>
                                <Text type={status.success ? 'success' : 'danger'} style={{ fontSize: 12 }}>
                                  {status.success ? `${status.latencyMs}ms` : '失败'}
                                </Text>
                              </Space>
                            ) : lastTest ? (
                              <Space direction="vertical" size={2}>
                                <Tooltip title={`${lastTest.message || ''} (${lastTest.latencyMs}ms)`}>
                                  {lastTest.success ? (
                                    <CheckCircleFilled style={{ fontSize: 24, color: '#52c41a' }} />
                                  ) : (
                                    <CloseCircleFilled style={{ fontSize: 24, color: '#ff4d4f' }} />
                                  )}
                                </Tooltip>
                                <Text type={lastTest.success ? 'success' : 'danger'} style={{ fontSize: 12 }}>
                                  {lastTest.success ? `${lastTest.latencyMs}ms` : '失败'}
                                </Text>
                              </Space>
                            ) : (
                              <Badge status="default" text="未测试" />
                            )}

                            {lastTestTime && (
                              <div style={{ marginTop: 4, fontSize: 10, color: '#999' }}>
                                <ClockCircleOutlined style={{ marginRight: 2 }} />
                                {lastTestTime}
                              </div>
                            )}
                          </div>
                        </div>
                      </Card>
                    </List.Item>
                  );
                }}
              />
            )}
          </Card>
        </Col>
      </Row>

      <Row gutter={16}>
        <Col span={12}>
          <Card title="智能体列表" loading={loading} style={{ height: 380 }}>
            <Table
              dataSource={agents}
              columns={agentColumns}
              rowKey="id"
              pagination={{ pageSize: 5, showSizeChanger: false }}
              size="small"
            />
          </Card>
        </Col>
        <Col span={12}>
          <Card title="团队列表" loading={loading} style={{ height: 380 }}>
            <Table
              dataSource={collaborations}
              columns={collaborationColumns}
              rowKey="id"
              pagination={{ pageSize: 5, showSizeChanger: false }}
              size="small"
            />
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Dashboard;
