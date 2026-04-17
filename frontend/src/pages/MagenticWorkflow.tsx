import { getErrorMessage } from '../utils/errorHandler';
import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
  Card, Button, Input, message, Spin, Divider, List, Tag, Space, Typography,
  Alert, Select, Avatar, Steps, Modal, Form, Checkbox, Row, Col, InputNumber
} from 'antd';
import {
  PlayCircleOutlined, RobotOutlined, SwapOutlined, CrownOutlined, BulbOutlined,
  EyeOutlined, CheckCircleOutlined, EditOutlined, SaveOutlined, ThunderboltOutlined,
  AppstoreOutlined, ReloadOutlined, MessageOutlined, TeamOutlined
} from '@ant-design/icons';
import ReactFlow, {
  Node, Edge, Background, Controls, MiniMap, useNodesState, useEdgesState
} from 'reactflow';
import 'reactflow/dist/style.css';
import { collaborationWorkflowService, ChatMessageDto } from '../services/collaborationWorkflowService';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { workflowTemplateApi } from '../services/workflow-template-api';
import { nodeTypes } from '../components/workflow/CustomNodes';
import { edgeTypes } from '../components/workflow/CustomEdges';
import type { WorkflowDefinition, WorkflowTemplate } from '../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

type MagenticStep = 'input' | 'source' | 'preview' | 'executing' | 'done';

const MagenticWorkflow: React.FC = () => {
  const [currentStep, setCurrentStep] = useState<MagenticStep>('input');
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [selectedCollaborationId, setSelectedCollaborationId] = useState<number | null>(null);
  const [taskInput, setTaskInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [generatedWorkflow, setGeneratedWorkflow] = useState<WorkflowDefinition | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<WorkflowTemplate | null>(null);
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [flowNodes, setFlowNodes, onFlowNodesChange] = useNodesState([]);
  const [flowEdges, setFlowEdges, onFlowEdgesChange] = useEdgesState([]);
  const [executionMessages, setExecutionMessages] = useState<ChatMessageDto[]>([]);
  const [saveModalVisible, setSaveModalVisible] = useState(false);
  const [saveForm] = Form.useForm();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadCollaborations();
  }, []);

  useEffect(() => {
    scrollToBottom();
  }, [executionMessages]);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadCollaborations = async () => {
    try {
      const data = await collaborationService.getAllCollaborations();
      setCollaborations(data || []);
    } catch (error: unknown) {
      console.error('加载协作列表失败:', error);
    }
  };

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
    if (currentStep === 'source') {
      loadTemplates();
    }
  }, [currentStep, loadTemplates]);

  const convertWorkflowToFlow = (workflow: WorkflowDefinition) => {
    const reactFlowNodes: Node[] = workflow.nodes.map((node, index) => ({
      id: node.id,
      type: node.type,
      position: { x: 300, y: index * 120 },
      data: node,
    }));

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
    if (!selectedCollaborationId) {
      message.warning('请先选择协作');
      return;
    }
    if (!taskInput.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    try {
      const response = await collaborationWorkflowService.generateMagenticPlan(
        selectedCollaborationId,
        taskInput
      );
      if (response.success && response.workflow) {
        setGeneratedWorkflow(response.workflow);
        convertWorkflowToFlow(response.workflow);
        setCurrentStep('preview');
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
    setCurrentStep('preview');
    message.success(`已选择模板: ${template.name}`);
  };

  const handleExecuteMagentic = async () => {
    if (!generatedWorkflow || !selectedCollaborationId) return;

    setCurrentStep('executing');
    setExecutionMessages([]);
    setLoading(true);

    try {
      await collaborationWorkflowService.executeMagenticWorkflow(
        selectedCollaborationId,
        generatedWorkflow,
        taskInput,
        undefined,
        (msg) => {
          setExecutionMessages(prev => [...prev, msg]);
        }
      );
      setCurrentStep('done');
      message.success('Magentic工作流执行完成');
    } catch (error: unknown) {
      message.error(`执行失败: ${getErrorMessage(error)}`);
      setCurrentStep('preview');
    } finally {
      setLoading(false);
    }
  };

  const handleSaveAsTemplate = async (values: Record<string, unknown>) => {
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
        originalTask: taskInput,
      });
      message.success('保存为模板成功');
      setSaveModalVisible(false);
    } catch (error: unknown) {
      message.error(`保存失败: ${getErrorMessage(error)}`);
    }
  };

  const handleReset = () => {
    setCurrentStep('input');
    setGeneratedWorkflow(null);
    setSelectedTemplate(null);
    setExecutionMessages([]);
    setFlowNodes([]);
    setFlowEdges([]);
  };

  const getAgentAvatar = (sender: string) => {
    if (sender === 'System' || sender === 'Aggregator' || sender === 'Condition') {
      return <Avatar icon={<ThunderboltOutlined />} style={{ backgroundColor: '#722ed1' }} />;
    }
    return <Avatar icon={<RobotOutlined />} style={{ backgroundColor: '#1890ff' }} />;
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
          <div style={{ flexShrink: 0 }}>{getAgentAvatar(msg.sender)}</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ marginBottom: 4 }}>
              <Text strong style={{ marginRight: 8 }}>{msg.sender}</Text>
              {msgType === 'step_start' && <Tag color="blue" style={{ marginRight: 8 }}>步骤 {msg.metadata?.step}</Tag>}
              {msgType === 'agent_response' && <Tag color="green" style={{ marginRight: 8 }}>执行结果</Tag>}
              {isAggregator && <Tag color="purple" style={{ marginRight: 8 }}>汇总</Tag>}
              {isCondition && <Tag color="orange" style={{ marginRight: 8 }}>条件</Tag>}
              {isSystem && <Tag color="blue" style={{ marginRight: 8 }}>系统</Tag>}
              <Text type="secondary" style={{ fontSize: 12 }}>
                {new Date(msg.timestamp).toLocaleTimeString()}
              </Text>
            </div>
            <div style={{
              backgroundColor: bgColor, padding: '8px 12px', borderRadius: 8,
              display: 'inline-block', maxWidth: '100%', wordBreak: 'break-word',
              border: borderColor !== 'transparent' ? `1px solid ${borderColor}` : 'none'
            }}>
              <Text style={{ whiteSpace: 'pre-wrap' }}>{msg.content}</Text>
            </div>
          </div>
        </div>
      </List.Item>
    );
  };

  const stepItems = [
    { title: '任务输入', icon: <EditOutlined /> },
    { title: '选择工作流', icon: <AppstoreOutlined /> },
    { title: '预览确认', icon: <EyeOutlined /> },
    { title: '执行', icon: <PlayCircleOutlined /> },
  ];

  const currentStepIndex = currentStep === 'input' ? 0
    : currentStep === 'source' ? 1
    : currentStep === 'preview' ? 2
    : 3;

  return (
    <div style={{ padding: '24px' }}>
      <Card title="🤖 Magentic 智能工作流">
        <Steps current={currentStepIndex} items={stepItems} size="small" style={{ marginBottom: 24 }} />

        {currentStep === 'input' && (
          <div>
            <Title level={5}>📝 输入任务描述</Title>
            <div style={{ marginBottom: 16 }}>
              <Text>选择协作</Text>
              <Select
                value={selectedCollaborationId ?? undefined}
                onChange={(val) => setSelectedCollaborationId(val)}
                placeholder="请选择协作"
                style={{ width: '100%', marginTop: 8 }}
                showSearch
                optionFilterProp="children"
              >
                {collaborations.map((c) => (
                  <Option key={c.id} value={parseInt(c.id)}>
                    <Space>
                      <TeamOutlined />
                      <span>{c.name}</span>
                      <Tag color={c.status === 'Active' ? 'green' : 'default'}>{c.status}</Tag>
                      <Text type="secondary">({c.agents?.length || 0} Agents)</Text>
                    </Space>
                  </Option>
                ))}
              </Select>
            </div>

            <TextArea
              value={taskInput}
              onChange={(e) => setTaskInput(e.target.value)}
              placeholder="请详细描述任务，例如：&#10;• 研究ResNet-50、BERT、GPT-2三个AI模型的能效，并生成分析报告&#10;• 开发一个用户登录功能&#10;• 设计一个电商系统的架构"
              rows={5}
              style={{ marginBottom: 16 }}
            />

            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="primary"
                onClick={() => setCurrentStep('source')}
                disabled={!taskInput.trim() || !selectedCollaborationId}
                icon={<CheckCircleOutlined />}
              >
                下一步：选择工作流
              </Button>
            </div>
          </div>
        )}

        {currentStep === 'source' && (
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
                    textAlign: 'center', height: '100%',
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
                    textAlign: 'center', height: '100%',
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
              <Button onClick={() => setCurrentStep('input')}>上一步</Button>
            </div>
          </div>
        )}

        {currentStep === 'preview' && generatedWorkflow && (
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
              <Button onClick={() => { setCurrentStep('source'); setSelectedTemplate(null); }}>
                上一步
              </Button>
              <Space>
                <Button icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>
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

        {currentStep === 'executing' && (
          <div>
            <div style={{ textAlign: 'center', padding: '20px 0' }}>
              <Spin size="large" />
              <div style={{ marginTop: 16 }}>
                <Text>🤖 Magentic工作流正在执行中...</Text>
              </div>
            </div>

            {executionMessages.length > 0 && (
              <Card size="small" title="执行过程" style={{ maxHeight: '500px', overflowY: 'auto' }}>
                <List dataSource={executionMessages} renderItem={renderMessageItem} />
                <div ref={messagesEndRef} />
              </Card>
            )}
          </div>
        )}

        {currentStep === 'done' && (
          <div>
            <Alert
              message="工作流执行完成"
              description="Magentic工作流已成功执行完毕"
              type="success"
              showIcon
              style={{ marginBottom: 16 }}
            />

            {executionMessages.length > 0 && (
              <Card size="small" title="执行结果" style={{ maxHeight: '600px', overflowY: 'auto' }}>
                <List dataSource={executionMessages} renderItem={renderMessageItem} />
              </Card>
            )}

            <div style={{ marginTop: 16, display: 'flex', justifyContent: 'space-between' }}>
              <Button icon={<ReloadOutlined />} onClick={handleReset}>重新执行</Button>
              <Space>
                <Button icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>保存为模板</Button>
              </Space>
            </div>
          </div>
        )}
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

export default MagenticWorkflow;
