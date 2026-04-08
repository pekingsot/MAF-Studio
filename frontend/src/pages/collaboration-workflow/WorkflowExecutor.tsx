import React, { useState, useRef, useEffect } from 'react';
import { Card, Button, Input, message, Spin, Divider, List, Tag, Space, Typography, InputNumber, Alert, Select, Avatar, Radio, RadioChangeEvent } from 'antd';
import { PlayCircleOutlined, TeamOutlined, SettingOutlined, MessageOutlined, UserOutlined, RobotOutlined, SwapOutlined, CrownOutlined, BulbOutlined } from '@ant-design/icons';
import { collaborationWorkflowService, CollaborationResult, ChatMessageDto, GroupChatParameters } from '../../services/collaborationWorkflowService';
import { CollaborationAgent } from '../../services/collaborationService';

const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

interface WorkflowExecutorProps {
  collaborationId: number;
  collaborationName: string;
  agents: CollaborationAgent[];
}

const orchestrationModeConfig = {
  roundRobin: {
    label: '轮询模式',
    icon: <SwapOutlined />,
    color: 'blue',
    description: '所有Agent轮流发言，平等参与讨论',
    details: [
      '✅ 每个Agent依次发言',
      '✅ 平等参与，无主次之分',
      '✅ 适合需要收集各方意见的场景'
    ]
  },
  manager: {
    label: '主Agent协调',
    icon: <CrownOutlined />,
    color: 'gold',
    description: 'Manager Agent引导Worker Agents发言',
    details: [
      '✅ Manager首先发言，开启讨论',
      '✅ Manager引导Worker发言',
      '✅ 适合有明确主持人/领导者的场景'
    ]
  },
  intelligent: {
    label: 'AI智能选择',
    icon: <BulbOutlined />,
    color: 'purple',
    description: '使用AI智能选择下一个发言的Agent',
    details: [
      '✅ 根据对话上下文智能选择',
      '✅ 动态调整发言顺序',
      '✅ 适合需要灵活协调的复杂讨论'
    ]
  }
};

const WorkflowExecutor: React.FC<WorkflowExecutorProps> = ({ collaborationId, collaborationName, agents }) => {
  const [workflowType, setWorkflowType] = useState<'magentic' | 'groupchat'>('magentic');
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CollaborationResult | null>(null);
  const [maxIterations, setMaxIterations] = useState(10);
  const [orchestrationMode, setOrchestrationMode] = useState<'roundRobin' | 'manager' | 'intelligent'>('manager');
  const [chatMessages, setChatMessages] = useState<ChatMessageDto[]>([]);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [chatMessages]);

  const getAgentInfo = (sender: string) => {
    const agent = agents.find(a => a.agentName === sender || `_${a.agentId}` === sender);
    return agent;
  };

  const getAgentAvatar = (sender: string) => {
    const agent = getAgentInfo(sender);
    if (agent?.agentAvatar) {
      return <Avatar src={agent.agentAvatar} />;
    }
    return <Avatar icon={<RobotOutlined />} style={{ backgroundColor: '#1890ff' }} />;
  };

  const getAgentDisplayName = (sender: string) => {
    const agent = getAgentInfo(sender);
    return agent?.agentName || sender;
  };

  const handleExecute = async () => {
    if (!input.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    setResult(null);
    setChatMessages([]);

    try {
      let response: CollaborationResult;

      if (workflowType === 'magentic') {
        response = await collaborationWorkflowService.executeReviewIterative(
          collaborationId,
          input,
          { maxIterations }
        );
        setResult(response);
        if (response.success) {
          message.success('工作流执行成功');
        } else {
          message.error(`工作流执行失败: ${response.error}`);
        }
      } else {
        const parameters: GroupChatParameters = {
          orchestrationMode,
          maxIterations
        };
        await collaborationWorkflowService.executeGroupChat(
          collaborationId, 
          input,
          parameters,
          (msg) => {
            setChatMessages(prev => [...prev, msg]);
          }
        );
        message.success('群聊工作流执行完成');
      }
    } catch (error: any) {
      message.error(`执行失败: ${error.message}`);
      setResult({
        success: false,
        output: '',
        messages: [],
        error: error.message,
      });
    } finally {
      setLoading(false);
    }
  };

  const workflowDescriptions = {
    magentic: {
      title: 'Magentic智能工作流',
      icon: <TeamOutlined />,
      description: 'Manager Agent根据任务动态协调Worker Agents执行任务',
      features: [
        '✅ 自动决定执行顺序（顺序/并发）',
        '✅ 动态分配任务给最合适的Agent',
        '✅ 智能合并和优化结果',
        '✅ 自动处理错误和重试',
      ],
      useCases: '适合有明确目标的任务，如：开发功能、分析问题、设计方案',
    },
    groupchat: {
      title: '群聊协作',
      icon: <MessageOutlined />,
      description: '多个Agents平等对话，自由交流想法',
      features: [
        '✅ 去中心化，无Manager协调',
        '✅ 自由讨论，头脑风暴',
        '✅ 观点碰撞，创意生成',
        '✅ 多轮对话，达成共识',
      ],
      useCases: '适合开放性任务，如：头脑风暴、创意讨论、方案评审',
    },
  };

  return (
    <div>
      <Card title={`协作工作流 - ${collaborationName}`}>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div>
            <Title level={5}>选择工作流模式</Title>
            <Select
              value={workflowType}
              onChange={setWorkflowType}
              style={{ width: '100%' }}
            >
              <Option value="magentic">
                <Space>
                  <TeamOutlined />
                  <span>Magentic智能工作流</span>
                  <Tag color="blue">中心化协调</Tag>
                </Space>
              </Option>
              <Option value="groupchat">
                <Space>
                  <MessageOutlined />
                  <span>群聊协作</span>
                  <Tag color="green">去中心化对话</Tag>
                </Space>
              </Option>
            </Select>
          </div>

          <Alert
            message={workflowDescriptions[workflowType].title}
            description={
              <div>
                <p>{workflowDescriptions[workflowType].description}</p>
                <br />
                {workflowDescriptions[workflowType].features.map((feature, index) => (
                  <p key={index}>{feature}</p>
                ))}
                <br />
                <p><strong>适用场景：</strong>{workflowDescriptions[workflowType].useCases}</p>
              </div>
            }
            type="info"
            showIcon
            icon={workflowDescriptions[workflowType].icon}
          />

          <Divider />

          {workflowType === 'magentic' && (
            <div>
              <Title level={5}>
                <SettingOutlined /> 配置参数
              </Title>
              <Space direction="vertical" style={{ width: '100%' }} size="middle">
                <div>
                  <Text>最大迭代次数</Text>
                  <InputNumber
                    value={maxIterations}
                    onChange={(value) => setMaxIterations(value || 10)}
                    min={1}
                    max={50}
                    style={{ width: '100%', marginTop: 8 }}
                    placeholder="默认10次"
                  />
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    Manager最多进行多少轮迭代决策
                  </Text>
                </div>
              </Space>
            </div>
          )}

          {workflowType === 'groupchat' && (
            <div>
              <Title level={5}>
                <MessageOutlined /> 群聊配置
              </Title>
              <Space direction="vertical" style={{ width: '100%' }} size="middle">
                <div>
                  <Text>协调模式</Text>
                  <Radio.Group 
                    value={orchestrationMode} 
                    onChange={(e: RadioChangeEvent) => setOrchestrationMode(e.target.value)}
                    style={{ width: '100%', marginTop: 8 }}
                  >
                    <Space direction="vertical" style={{ width: '100%' }}>
                      {(Object.keys(orchestrationModeConfig) as Array<keyof typeof orchestrationModeConfig>).map((key) => {
                        const config = orchestrationModeConfig[key];
                        return (
                          <Radio key={key} value={key}>
                            <Space>
                              <Tag color={config.color} icon={config.icon}>
                                {config.label}
                              </Tag>
                              <Text type="secondary">{config.description}</Text>
                            </Space>
                          </Radio>
                        );
                      })}
                    </Space>
                  </Radio.Group>
                  <Card 
                    size="small" 
                    style={{ marginTop: 12, backgroundColor: '#fafafa' }}
                  >
                    <Space direction="vertical" size={0}>
                      {orchestrationModeConfig[orchestrationMode].details.map((detail, index) => (
                        <Text key={index} style={{ fontSize: 12 }}>{detail}</Text>
                      ))}
                    </Space>
                  </Card>
                </div>
                
                <div>
                  <Text>最大迭代次数</Text>
                  <InputNumber
                    value={maxIterations}
                    onChange={(value) => setMaxIterations(value || 10)}
                    min={1}
                    max={50}
                    style={{ width: '100%', marginTop: 8 }}
                    placeholder="默认10次"
                  />
                </div>
                
                <div>
                  <Text>参与Agent（共 {agents.length} 个）</Text>
                  <div style={{ marginTop: 8 }}>
                    {agents.map((agent) => (
                      <Tag key={agent.agentId} color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ marginBottom: 4 }}>
                        {agent.agentName}
                        {agent.role && ` (${agent.role})`}
                      </Tag>
                    ))}
                  </div>
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {orchestrationMode === 'manager' || orchestrationMode === 'intelligent'
                      ? '标记为 Manager 角色的Agent将作为协调者'
                      : '所有Agent将轮流发言'}
                  </Text>
                </div>
              </Space>
            </div>
          )}

          <Divider />

          <div>
            <Title level={5}>任务输入</Title>
            <TextArea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder={
                workflowType === 'magentic'
                  ? "请输入任务内容，例如：\n• 开发一个用户登录功能\n• 分析这段代码的性能问题\n• 设计一个电商系统的架构"
                  : "请输入讨论主题，例如：\n• 如何提高系统的可扩展性？\n• 新产品应该具备哪些核心功能？\n• 代码审查的最佳实践是什么？"
              }
              rows={6}
              style={{ marginBottom: 16 }}
            />
            <Button
              type="primary"
              icon={<PlayCircleOutlined />}
              onClick={handleExecute}
              loading={loading}
              size="large"
              block
            >
              执行工作流
            </Button>
          </div>

          {loading && (
            <div style={{ textAlign: 'center', padding: '40px 0' }}>
              <Spin size="large" />
              <div style={{ marginTop: 16 }}>
                <Text>
                  {workflowType === 'magentic'
                    ? 'Manager Agent正在协调Worker Agents执行任务...'
                    : 'Agents正在进行群聊讨论...'}
                </Text>
              </div>
            </div>
          )}

          {workflowType === 'groupchat' && chatMessages.length > 0 && (
            <>
              <Divider />
              <div>
                <Title level={5}>
                  <MessageOutlined /> 协作对话
                </Title>
                <Card 
                  size="small" 
                  style={{ 
                    maxHeight: '500px', 
                    overflowY: 'auto',
                    backgroundColor: '#f5f5f5'
                  }}
                >
                  <List
                    dataSource={chatMessages}
                    renderItem={(msg, index) => (
                      <List.Item key={index} style={{ border: 'none', padding: '8px 0' }}>
                        <div style={{ display: 'flex', width: '100%', gap: '12px' }}>
                          <div style={{ flexShrink: 0 }}>
                            {getAgentAvatar(msg.sender)}
                          </div>
                          <div style={{ flex: 1, minWidth: 0 }}>
                            <div style={{ marginBottom: 4 }}>
                              <Text strong style={{ marginRight: 8 }}>
                                {getAgentDisplayName(msg.sender)}
                              </Text>
                              <Text type="secondary" style={{ fontSize: 12 }}>
                                {new Date(msg.timestamp).toLocaleTimeString()}
                              </Text>
                            </div>
                            <div 
                              style={{ 
                                backgroundColor: '#fff', 
                                padding: '8px 12px', 
                                borderRadius: 8,
                                display: 'inline-block',
                                maxWidth: '100%',
                                wordBreak: 'break-word'
                              }}
                            >
                              <Text style={{ whiteSpace: 'pre-wrap' }}>{msg.content}</Text>
                            </div>
                          </div>
                        </div>
                      </List.Item>
                    )}
                  />
                  <div ref={messagesEndRef} />
                </Card>
              </div>
            </>
          )}

          {result && !loading && (
            <>
              <Divider />
              <div>
                <Title level={5}>
                  执行结果
                  <Tag color={result.success ? 'success' : 'error'} style={{ marginLeft: 8 }}>
                    {result.success ? '成功' : '失败'}
                  </Tag>
                </Title>

                {result.metadata && workflowType === 'magentic' && (
                  <Card size="small" title="执行统计" style={{ marginBottom: 16 }}>
                    <Space direction="vertical" style={{ width: '100%' }}>
                      <div>
                        <Text strong>迭代次数：</Text>
                        <Text>{result.metadata.iterations || 0} 次</Text>
                      </div>
                      <div>
                        <Text strong>工作流模式：</Text>
                        <Tag color="blue">{result.metadata.pattern || 'Magentic'}</Tag>
                      </div>
                    </Space>
                  </Card>
                )}

                {result.output && (
                  <Card size="small" title="最终输出" style={{ marginBottom: 16 }}>
                    <Text style={{ whiteSpace: 'pre-wrap' }}>{result.output}</Text>
                  </Card>
                )}

                {result.error && (
                  <Card size="small" title="错误信息" style={{ marginBottom: 16 }}>
                    <Text type="danger">{result.error}</Text>
                  </Card>
                )}

                {result.messages && result.messages.length > 0 && (
                  <Card size="small" title="执行过程">
                    <List
                      dataSource={result.messages}
                      renderItem={(msg, index) => {
                        const iteration = msg.metadata?.iteration;
                        const step = msg.metadata?.step;
                        
                        let stepText = '';
                        let stepColor = 'blue';
                        
                        if (step === 'create') {
                          stepText = `第${iteration}轮 - 编写`;
                          stepColor = 'green';
                        } else if (step === 'review') {
                          stepText = `第${iteration}轮 - 审阅`;
                          stepColor = 'orange';
                        } else if (step === 'approved') {
                          stepText = '审阅通过';
                          stepColor = 'success';
                        } else if (step === 'max_iterations_reached') {
                          stepText = '达到最大迭代次数';
                          stepColor = 'error';
                        }
                        
                        return (
                          <List.Item key={index}>
                            <List.Item.Meta
                              avatar={
                                <Space direction="vertical" size={0}>
                                  <Tag color="blue">{msg.sender}</Tag>
                                  {stepText && <Tag color={stepColor}>{stepText}</Tag>}
                                </Space>
                              }
                              description={
                                <>
                                  <Text style={{ whiteSpace: 'pre-wrap' }}>{msg.content}</Text>
                                  <br />
                                  <Text type="secondary" style={{ fontSize: 12 }}>
                                    {new Date(msg.timestamp).toLocaleString()}
                                  </Text>
                                </>
                              }
                            />
                          </List.Item>
                        );
                      }}
                    />
                  </Card>
                )}
              </div>
            </>
          )}
        </Space>
      </Card>
    </div>
  );
};

export default WorkflowExecutor;
