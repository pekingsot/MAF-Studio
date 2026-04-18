import { getErrorMessage } from '../../utils/errorHandler';
import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
  Card, Button, Input, message, Spin, Divider, List, Tag, Space, Typography,
  InputNumber, Alert, Select, Avatar, Radio, RadioChangeEvent, Switch, Steps,
  Modal, Form, Checkbox, Row, Col, Empty, Tooltip
} from 'antd';
import {
  PlayCircleOutlined, TeamOutlined, SettingOutlined, MessageOutlined,
  UserOutlined, RobotOutlined, SwapOutlined, CrownOutlined, BulbOutlined,
  EyeOutlined, EyeInvisibleOutlined, CheckCircleOutlined, EditOutlined,
  SaveOutlined, ThunderboltOutlined, AppstoreOutlined, ReloadOutlined
} from '@ant-design/icons';
import ReactFlow, {
  Node, Edge, Background, Controls, MiniMap, useNodesState, useEdgesState
} from 'reactflow';
import 'reactflow/dist/style.css';
import { collaborationWorkflowService, ChatMessageDto, GroupChatParameters } from '../../services/collaborationWorkflowService';
import { CollaborationAgent } from '../../services/collaborationService';
import { workflowTemplateApi } from '../../services/workflow-template-api';
import { nodeTypes } from '../../components/workflow/CustomNodes';
import { edgeTypes } from '../../components/workflow/CustomEdges';
import type { WorkflowDefinition, WorkflowTemplate } from '../../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

interface WorkflowExecutorProps {
  collaborationId: number;
  collaborationName: string;
  agents: CollaborationAgent[];
}

type MagenticStep = 'input' | 'source' | 'preview' | 'executing' | 'done';

const orchestrationModeConfig = {
  roundRobin: {
    label: '轮询模式',
    icon: <SwapOutlined />,
    color: 'blue',
    description: '所有Agent轮流发言，平等参与讨论',
  },
  manager: {
    label: '主Agent协调',
    icon: <CrownOutlined />,
    color: 'gold',
    description: 'Manager Agent引导Worker Agents发言',
  },
  intelligent: {
    label: 'AI智能选择',
    icon: <BulbOutlined />,
    color: 'purple',
    description: '使用AI智能选择下一个发言的Agent',
  },
};

const WorkflowExecutor: React.FC<WorkflowExecutorProps> = ({ collaborationId, collaborationName, agents }) => {
  const [workflowType, setWorkflowType] = useState<'magentic' | 'groupchat'>('magentic');

  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<any>(null);
  const [maxIterations, setMaxIterations] = useState(10);
  const [orchestrationMode, setOrchestrationMode] = useState<'roundRobin' | 'manager' | 'intelligent'>('manager');
  const [chatMessages, setChatMessages] = useState<ChatMessageDto[]>([]);
  const [showManagerThinking, setShowManagerThinking] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const [magenticStep, setMagenticStep] = useState<MagenticStep>('input');
  const [generatedWorkflow, setGeneratedWorkflow] = useState<WorkflowDefinition | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<WorkflowTemplate | null>(null);
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [flowNodes, setFlowNodes, onFlowNodesChange] = useNodesState([]);
  const [flowEdges, setFlowEdges, onFlowEdgesChange] = useEdgesState([]);
  const [magenticMessages, setMagenticMessages] = useState<ChatMessageDto[]>([]);
  const [saveModalVisible, setSaveModalVisible] = useState(false);
  const [saveForm] = Form.useForm();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [chatMessages, magenticMessages]);

  const loadTemplates = useCallback(async () => {
    setTemplatesLoading(true);
    try {
      const data = await workflowTemplateApi.getAll(true);
      setTemplates(data || []);
    } catch (error: unknown) {
      console.error('加载模板失败:', error);
    } finally {
      setTemplatesLoading(false);
    }
  }, []);

  useEffect(() => {
    if (workflowType === 'magentic' && magenticStep === 'source') {
      loadTemplates();
    }
  }, [workflowType, magenticStep, loadTemplates]);

  const getAgentInfo = (sender: string) => {
    const agent = agents.find(a => a.agentName === sender || `_${a.agentId}` === sender);
    return agent;
  };

  const getAgentAvatar = (sender: string) => {
    const agent = getAgentInfo(sender);
    if (agent?.agentAvatar) {
      return <Avatar src={agent.agentAvatar} />;
    }
    if (sender === 'System' || sender === 'Aggregator' || sender === 'Condition') {
      return <Avatar icon={<ThunderboltOutlined />} style={{ backgroundColor: '#722ed1' }} />;
    }
    return <Avatar icon={<RobotOutlined />} style={{ backgroundColor: '#1890ff' }} />;
  };

  const getAgentDisplayName = (sender: string) => {
    const agent = getAgentInfo(sender);
    return agent?.agentName || sender;
  };

  const convertWorkflowToFlow = (workflow: WorkflowDefinition) => {
    const agentNodes = workflow.nodes.filter(n => n.type === 'agent');
    const spacing = 250;

    const reactFlowNodes: Node[] = workflow.nodes.map((node, index) => {
      const agentIndex = agentNodes.indexOf(node);
      const isAgent = node.type === 'agent';

      let x = 300;
      let y = index * 120;

      if (isAgent) {
        const fanOutEdges = workflow.edges.filter(e => e.type === 'fan-out' && e.from === node.id);
        if (fanOutEdges.length > 0) {
          const targets = fanOutEdges.flatMap(e => Array.isArray(e.to) ? e.to : [e.to]);
          const centerIndex = targets.length / 2;
          x = 300;
        }
      }

      return {
        id: node.id,
        type: node.type,
        position: { x, y },
        data: node,
      };
    });

    const reactFlowEdges: Edge[] = workflow.edges.map((edge, index) => ({
      id: `edge-${index}`,
      source: edge.from,
      target: Array.isArray(edge.to) ? edge.to[0] : edge.to,
      type: 'custom',
      data: { type: edge.type, description: edge.description },
      animated: edge.type === 'fan-out',
    }));

    setFlowNodes(reactFlowNodes);
    setFlowEdges(reactFlowEdges);
  };

  const handleGenerateWorkflow = async () => {
    if (!input.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    try {
      const response = await collaborationWorkflowService.generateMagenticPlan(collaborationId, input);
      if (response.success && response.workflow) {
        setGeneratedWorkflow(response.workflow);
        convertWorkflowToFlow(response.workflow);
        setMagenticStep('preview');
        message.success('工作流生成成功');
      } else {
        message.error(`生成失败: ${response.error || '未知错误'}`);
      }
    } catch (error: unknown) {
      message.error(`生成失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectTemplate = (template: WorkflowTemplate) => {
    setSelectedTemplate(template);
    setGeneratedWorkflow(template.workflow);
    convertWorkflowToFlow(template.workflow);
    setMagenticStep('preview');
    message.success(`已选择模板: ${template.name}`);
  };

  const handleExecuteMagentic = async () => {
    if (!generatedWorkflow) return;

    setMagenticStep('executing');
    setMagenticMessages([]);
    setLoading(true);

    try {
      await collaborationWorkflowService.executeMagenticWorkflow(
        collaborationId,
        generatedWorkflow,
        input,
        undefined,
        (msg) => {
          setMagenticMessages(prev => [...prev, msg]);
        }
      );
      setMagenticStep('done');
      message.success('Magentic工作流执行完成');
    } catch (error: unknown) {
      message.error(`执行失败: ${getErrorMessage(error)}`);
      setMagenticStep('preview');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveAsTemplate = async (values: { name: string; description?: string; category?: string; tags?: string; isPublic?: boolean; enableLearning?: boolean }) => {
    if (!generatedWorkflow) return;

    try {
      await workflowTemplateApi.saveMagenticPlan({
        name: values.name,
        description: values.description,
        category: values.category,
        tags: values.tags?.split(',').map((t: string) => t.trim()),
        workflow: generatedWorkflow,
        isPublic: values.isPublic || false,
        enableLearning: values.enableLearning || false,
        originalTask: input,
      });
      message.success('保存为模板成功');
      setSaveModalVisible(false);
    } catch (error: unknown) {
      message.error(`保存失败: ${getErrorMessage(error)}`);
    }
  };

  const handleResetMagentic = () => {
    setMagenticStep('input');
    setGeneratedWorkflow(null);
    setSelectedTemplate(null);
    setMagenticMessages([]);
    setFlowNodes([]);
    setFlowEdges([]);
  };

  const handleExecuteGroupChat = async () => {
    if (!input.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    setResult(null);
    setChatMessages([]);

    try {
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
    } catch (error: unknown) {
      message.error(`执行失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  const renderMessageItem = (msg: ChatMessageDto, index: number) => {
    const msgType = msg.metadata?.type;
    const isSystem = msgType === 'system' || msgType === 'system_complete' || msgType === 'step_start';
    const isError = msgType === 'error';
    const isWarning = msgType === 'warning';
    const isCondition = msgType === 'condition';
    const isAggregator = msgType === 'aggregator';

    let bgColor = '#fff';
    let borderColor = 'transparent';
    if (isSystem) { bgColor = '#e6f7ff'; borderColor = '#91d5ff'; }
    if (isError) { bgColor = '#fff2f0'; borderColor = '#ffccc7'; }
    if (isWarning) { bgColor = '#fffbe6'; borderColor = '#ffe58f'; }
    if (isCondition) { bgColor = '#fff7e6'; borderColor = '#ffd591'; }
    if (isAggregator) { bgColor = '#f9f0ff'; borderColor = '#d3adf7'; }

    return (
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
              {msgType === 'step_start' && <Tag color="blue" style={{ marginRight: 8 }}>步骤 {msg.metadata?.step}</Tag>}
              {msgType === 'agent_response' && <Tag color="green" style={{ marginRight: 8 }}>执行结果</Tag>}
              {isAggregator && <Tag color="purple" style={{ marginRight: 8 }}>汇总</Tag>}
              {isCondition && <Tag color="orange" style={{ marginRight: 8 }}>条件</Tag>}
              {isSystem && <Tag color="blue" style={{ marginRight: 8 }}>系统</Tag>}
              {msgType === 'manager_thinking' && <Tag color="gold" style={{ marginRight: 8 }}>协调者点名</Tag>}
              <Text type="secondary" style={{ fontSize: 12 }}>
                {new Date(msg.timestamp).toLocaleTimeString()}
              </Text>
            </div>
            <div style={{
              backgroundColor: bgColor,
              padding: '8px 12px',
              borderRadius: 8,
              display: 'inline-block',
              maxWidth: '100%',
              wordBreak: 'break-word',
              border: borderColor !== 'transparent' ? `1px solid ${borderColor}` : 'none'
            }}>
              <Text style={{ whiteSpace: 'pre-wrap' }}>{msg.content}</Text>
            </div>
          </div>
        </div>
      </List.Item>
    );
  };

  const renderMagenticFlow = () => {
    const stepItems = [
      { title: '任务输入', icon: <EditOutlined /> },
      { title: '选择工作流', icon: <AppstoreOutlined /> },
      { title: '预览确认', icon: <EyeOutlined /> },
      { title: '执行', icon: <PlayCircleOutlined /> },
    ];

    const currentStepIndex = magenticStep === 'input' ? 0
      : magenticStep === 'source' ? 1
      : magenticStep === 'preview' ? 2
      : magenticStep === 'executing' ? 3
      : 3;

    return (
      <div>
        <Steps current={currentStepIndex} items={stepItems} size="small" style={{ marginBottom: 24 }} />

        {magenticStep === 'input' && (
          <div>
            <Title level={5}>📝 输入任务描述</Title>
            <TextArea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="请详细描述任务，例如：&#10;• 开发一个用户登录功能&#10;• 分析这段代码的性能问题&#10;• 设计一个电商系统的架构"
              rows={5}
              style={{ marginBottom: 16 }}
            />
            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="primary"
                onClick={() => setMagenticStep('source')}
                disabled={!input.trim()}
                icon={<CheckCircleOutlined />}
              >
                下一步：选择工作流
              </Button>
            </div>
          </div>
        )}

        {magenticStep === 'source' && (
          <div>
            <Title level={5}>🎯 选择工作流来源</Title>
            <Alert
              message="你可以选择一个已有的模板工作流，或者让协调者自动生成工作流"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Row gutter={16}>
              <Col span={12}>
                <Card
                  hoverable
                  onClick={handleGenerateWorkflow}
                  style={{
                    textAlign: 'center',
                    height: '100%',
                    background: 'linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%)',
                    border: '2px solid #b7eb8f'
                  }}
                >
                  <RobotOutlined style={{ fontSize: '48px', color: '#52c41a' }} />
                  <h3 style={{ marginTop: '16px' }}>🤖 协调者自动生成</h3>
                  <p style={{ color: '#666' }}>让Manager Agent根据任务自动规划工作流</p>
                  <Tag color="green">智能编排</Tag>
                </Card>
              </Col>
              <Col span={12}>
                <Card
                  hoverable
                  style={{
                    textAlign: 'center',
                    height: '100%',
                    background: 'linear-gradient(135deg, #e6f7ff 0%, #bae7ff 100%)',
                    border: '2px solid #91d5ff'
                  }}
                >
                  <AppstoreOutlined style={{ fontSize: '48px', color: '#1890ff' }} />
                  <h3 style={{ marginTop: '16px' }}>📋 选择模板</h3>
                  <p style={{ color: '#666' }}>使用已保存的工作流模板</p>
                  <Tag color="blue">模板复用</Tag>
                </Card>
              </Col>
            </Row>

            {templates.length > 0 && (
              <div style={{ marginTop: 16 }}>
                <Divider>可用模板</Divider>
                <Spin spinning={templatesLoading}>
                  <List
                    grid={{ gutter: 12, column: 2 }}
                    dataSource={templates}
                    renderItem={(template) => (
                      <List.Item>
                        <Card
                          hoverable
                          size="small"
                          onClick={() => handleSelectTemplate(template)}
                          style={{ border: selectedTemplate?.id === template.id ? '2px solid #1890ff' : undefined }}
                        >
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                            <div>
                              <Text strong>{template.name}</Text>
                              <br />
                              <Text type="secondary" style={{ fontSize: 12 }}>
                                {template.description || '无描述'}
                              </Text>
                            </div>
                            <Space>
                              <Tag color={template.source === 'magentic' ? 'green' : 'blue'}>
                                {template.source === 'magentic' ? 'AI生成' : '手动'}
                              </Tag>
                              <Tag>使用 {template.usageCount} 次</Tag>
                            </Space>
                          </div>
                        </Card>
                      </List.Item>
                    )}
                  />
                </Spin>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 16 }}>
              <Button onClick={() => setMagenticStep('input')}>上一步</Button>
            </div>
          </div>
        )}

        {magenticStep === 'preview' && generatedWorkflow && (
          <div>
            <Title level={5}>
              {selectedTemplate ? `📋 模板: ${selectedTemplate.name}` : '🤖 协调者生成的工作流'}
            </Title>

            <Alert
              message={selectedTemplate
                ? `已选择模板「${selectedTemplate.name}」，确认后可直接执行或编辑`
                : '协调者已根据任务生成工作流，确认后可直接执行或编辑'}
              type="success"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Card size="small" title="工作流图" style={{ marginBottom: 16 }}>
              <div style={{ height: '350px' }}>
                <ReactFlow
                  nodes={flowNodes}
                  edges={flowEdges}
                  nodeTypes={nodeTypes}
                  edgeTypes={edgeTypes}
                  onNodesChange={onFlowNodesChange}
                  onEdgesChange={onFlowEdgesChange}
                  fitView
                >
                  <Background />
                  <Controls />
                  <MiniMap />
                </ReactFlow>
              </div>
            </Card>

            <Card size="small" title="工作流节点详情" style={{ marginBottom: 16 }}>
              <List
                size="small"
                dataSource={generatedWorkflow.nodes.filter(n => n.type === 'agent')}
                renderItem={(node, index) => (
                  <List.Item>
                    <List.Item.Meta
                      avatar={<Avatar icon={<RobotOutlined />} style={{ backgroundColor: '#1890ff' }} />}
                      title={
                        <Space>
                          <Text strong>{node.agentRole || node.name}</Text>
                          <Tag color="blue">步骤 {index + 1}</Tag>
                        </Space>
                      }
                      description={node.inputTemplate || '执行分配的任务'}
                    />
                  </List.Item>
                )}
              />
            </Card>

            <Space style={{ width: '100%', justifyContent: 'space-between', display: 'flex' }}>
              <Button onClick={() => { setMagenticStep('source'); setSelectedTemplate(null); }}>
                上一步
              </Button>
              <Space>
                <Button
                  icon={<SaveOutlined />}
                  onClick={() => setSaveModalVisible(true)}
                >
                  保存为模板
                </Button>
                <Button
                  type="primary"
                  icon={<PlayCircleOutlined />}
                  onClick={handleExecuteMagentic}
                  loading={loading}
                >
                  执行工作流
                </Button>
              </Space>
            </Space>
          </div>
        )}

        {magenticStep === 'executing' && (
          <div>
            <div style={{ textAlign: 'center', padding: '20px 0' }}>
              <Spin size="large" />
              <div style={{ marginTop: 16 }}>
                <Text>🤖 Magentic工作流正在执行中...</Text>
              </div>
            </div>

            {magenticMessages.length > 0 && (
              <Card
                size="small"
                title="执行过程"
                style={{ maxHeight: '500px', overflowY: 'auto' }}
              >
                <List
                  dataSource={magenticMessages}
                  renderItem={renderMessageItem}
                />
                <div ref={messagesEndRef} />
              </Card>
            )}
          </div>
        )}

        {magenticStep === 'done' && (
          <div>
            <Alert
              message="工作流执行完成"
              description="Magentic工作流已成功执行完毕"
              type="success"
              showIcon
              style={{ marginBottom: 16 }}
            />

            {magenticMessages.length > 0 && (
              <Card
                size="small"
                title="执行结果"
                style={{ maxHeight: '600px', overflowY: 'auto' }}
              >
                <List
                  dataSource={magenticMessages}
                  renderItem={renderMessageItem}
                />
              </Card>
            )}

            <div style={{ marginTop: 16, display: 'flex', justifyContent: 'space-between' }}>
              <Button icon={<ReloadOutlined />} onClick={handleResetMagentic}>
                重新执行
              </Button>
              <Space>
                <Button icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>
                  保存为模板
                </Button>
              </Space>
            </div>
          </div>
        )}
      </div>
    );
  };

  const renderGroupChatFlow = () => (
    <div>
      <div style={{ marginBottom: 16 }}>
        <Title level={5}><MessageOutlined /> 群聊配置</Title>
        <Radio.Group
          value={orchestrationMode}
          onChange={(e: RadioChangeEvent) => setOrchestrationMode(e.target.value)}
          style={{ width: '100%' }}
        >
          <Space direction="vertical" style={{ width: '100%' }}>
            {(Object.keys(orchestrationModeConfig) as Array<keyof typeof orchestrationModeConfig>).map((key) => {
              const config = orchestrationModeConfig[key];
              return (
                <Radio key={key} value={key}>
                  <Space>
                    <Tag color={config.color} icon={config.icon}>{config.label}</Tag>
                    <Text type="secondary">{config.description}</Text>
                  </Space>
                </Radio>
              );
            })}
          </Space>
        </Radio.Group>
      </div>

      <div style={{ marginBottom: 16 }}>
        <Text>最大迭代次数</Text>
        <InputNumber
          value={maxIterations}
          onChange={(value) => setMaxIterations(value || 10)}
          min={1} max={50}
          style={{ width: '100%', marginTop: 8 }}
        />
      </div>

      <div style={{ marginBottom: 16 }}>
        <Text>参与Agent（共 {agents.length} 个）</Text>
        <div style={{ marginTop: 8 }}>
          {agents.map((agent) => (
            <Tag key={agent.agentId} color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ marginBottom: 4 }}>
              {agent.agentName}{agent.role && ` (${agent.role})`}
            </Tag>
          ))}
        </div>
      </div>

      <Divider />

      <TextArea
        value={input}
        onChange={(e) => setInput(e.target.value)}
        placeholder="请输入讨论主题，例如：&#10;• 如何提高系统的可扩展性？&#10;• 新产品应该具备哪些核心功能？"
        rows={5}
        style={{ marginBottom: 16 }}
      />

      <Button
        type="primary"
        icon={<PlayCircleOutlined />}
        onClick={handleExecuteGroupChat}
        loading={loading}
        size="large"
        block
      >
        执行群聊工作流
      </Button>

      {loading && (
        <div style={{ textAlign: 'center', padding: '20px 0' }}>
          <Spin size="large" />
          <div style={{ marginTop: 16 }}>
            <Text>Agents正在进行群聊讨论...</Text>
          </div>
        </div>
      )}

      {chatMessages.length > 0 && (
        <>
          <Divider />
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <Title level={5} style={{ margin: 0 }}>
                <MessageOutlined /> 协作对话
              </Title>
              <Space>
                <Text type="secondary">显示系统点名提示</Text>
                <Switch
                  checked={showManagerThinking}
                  onChange={setShowManagerThinking}
                  checkedChildren={<EyeOutlined />}
                  unCheckedChildren={<EyeInvisibleOutlined />}
                />
              </Space>
            </div>
            <Card size="small" style={{ maxHeight: '500px', overflowY: 'auto', backgroundColor: '#f5f5f5' }}>
              <List
                dataSource={chatMessages.filter(msg => {
                  if (!showManagerThinking && msg.metadata?.type === 'manager_thinking') return false;
                  return true;
                })}
                renderItem={renderMessageItem}
              />
              <div ref={messagesEndRef} />
            </Card>
          </div>
        </>
      )}
    </div>
  );

  return (
    <div>
      <Card title={`协作工作流 - ${collaborationName}`}>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div>
            <Title level={5}>选择工作流模式</Title>
            <Select
              value={workflowType}
              onChange={(val) => {
                setWorkflowType(val);
                if (val === 'magentic') handleResetMagentic();
              }}
              style={{ width: '100%' }}
            >
              <Option value="magentic">
                <Space>
                  <TeamOutlined />
                  <span>Magentic智能工作流</span>
                  <Tag color="blue">协调者编排</Tag>
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

          {workflowType === 'magentic' ? (
            <Alert
              message="Magentic智能工作流"
              description={
                <div>
                  <p>协调者根据任务自动生成工作流（DAG），你可以预览、编辑后执行。</p>
                  <Space size={4} wrap>
                    <Tag color="green">✅ 自动生成工作流</Tag>
                    <Tag color="blue">✅ 可预览和编辑</Tag>
                    <Tag color="purple">✅ 可保存为模板</Tag>
                    <Tag color="orange">✅ 支持模板复用</Tag>
                  </Space>
                </div>
              }
              type="info"
              showIcon
              icon={<RobotOutlined />}
            />
          ) : (
            <Alert
              message="群聊协作"
              description="多个Agents平等对话，自由交流想法，适合头脑风暴和开放性讨论"
              type="info"
              showIcon
              icon={<MessageOutlined />}
            />
          )}

          <Divider />

          {workflowType === 'magentic' ? renderMagenticFlow() : renderGroupChatFlow()}
        </Space>
      </Card>

      <Modal
        title="保存为模板"
        open={saveModalVisible}
        onCancel={() => setSaveModalVisible(false)}
        onOk={() => saveForm.submit()}
      >
        <Form form={saveForm} layout="vertical" onFinish={handleSaveAsTemplate}>
          <Form.Item label="模板名称" name="name" rules={[{ required: true, message: '请输入模板名称' }]}>
            <Input placeholder="请输入模板名称" />
          </Form.Item>
          <Form.Item label="模板描述" name="description">
            <TextArea rows={3} placeholder="请输入模板描述" />
          </Form.Item>
          <Form.Item label="分类" name="category">
            <Select placeholder="请选择分类">
              <Option value="research">研究分析</Option>
              <Option value="writing">写作创作</Option>
              <Option value="coding">编程开发</Option>
              <Option value="analysis">数据分析</Option>
            </Select>
          </Form.Item>
          <Form.Item label="标签" name="tags">
            <Input placeholder="多个标签用逗号分隔" />
          </Form.Item>
          <Form.Item name="isPublic" valuePropName="checked">
            <Checkbox>公开模板</Checkbox>
          </Form.Item>
          <Form.Item name="enableLearning" valuePropName="checked">
            <Checkbox>让Magentic学习（类似任务自动使用此模板）</Checkbox>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowExecutor;
