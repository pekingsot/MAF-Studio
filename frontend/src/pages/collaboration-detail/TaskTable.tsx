import { getErrorMessage } from '../../utils/errorHandler';
import React, { useState, useRef, useEffect } from 'react';
import {
  Table, Button, Space, Tag, Modal, Form, Input, Select, message, Popconfirm,
  Drawer, List, Avatar, Spin, Alert, Steps, Card, Row, Col, Divider, Switch, Tooltip,
  Typography
} from 'antd';
import {
  PlusOutlined, EditOutlined, DeleteOutlined, PlayCircleOutlined,
  RobotOutlined, CheckCircleOutlined, StopOutlined, EyeOutlined,
  EyeInvisibleOutlined, AppstoreOutlined, SaveOutlined, ReloadOutlined,
  ThunderboltOutlined, TeamOutlined, CrownOutlined, BulbOutlined,
  ApartmentOutlined, MessageOutlined, SafetyCertificateOutlined
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import ReactFlow, { Background, Controls, MiniMap, useNodesState, useEdgesState, Edge } from 'reactflow';
import 'reactflow/dist/style.css';
import { CollaborationTask, CollaborationAgent, TASK_STATUS_COLOR_MAP } from './types';
import { collaborationService } from '../../services/collaborationService';
import { collaborationWorkflowService, ChatMessageDto } from '../../services/collaborationWorkflowService';
import { workflowTemplateApi } from '../../services/workflow-template-api';
import { nodeTypes } from '../../components/workflow/CustomNodes';
import { edgeTypes } from '../../components/workflow/CustomEdges';
import type { WorkflowDefinition, WorkflowNode, WorkflowTemplate, NodeType as NodeTypeEnum, EdgeType } from '../../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;
const { Text } = Typography;

interface TaskTableProps {
  tasks: CollaborationTask[];
  agents: CollaborationAgent[];
  collaborationId: string;
  onUpdate: () => void;
}

type MagenticStep = 'input' | 'source' | 'preview' | 'executing' | 'done';

const TaskTable: React.FC<TaskTableProps> = ({ tasks, agents, collaborationId, onUpdate }) => {
  const [createModalVisible, setCreateModalVisible] = useState(false);
  const [editModalVisible, setEditModalVisible] = useState(false);
  const [execDrawerVisible, setExecDrawerVisible] = useState(false);
  const [currentTask, setCurrentTask] = useState<CollaborationTask | null>(null);
  const [createForm] = Form.useForm();
  const [editForm] = Form.useForm();
  const [saving, setSaving] = useState(false);

  const [magenticStep, setMagenticStep] = useState<MagenticStep>('input');
  const [taskInput, setTaskInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [generatedWorkflow, setGeneratedWorkflow] = useState<WorkflowDefinition | null>(null);
  const [selectedTemplate, setSelectedTemplate] = useState<WorkflowTemplate | null>(null);
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [templatesLoading, setTemplatesLoading] = useState(false);
  const [flowNodes, setFlowNodes, onFlowNodesChange] = useNodesState([]);
  const [flowEdges, setFlowEdges, onFlowEdgesChange] = useEdgesState([]);
  const [execMessages, setExecMessages] = useState<ChatMessageDto[]>([]);
  const [saveModalVisible, setSaveModalVisible] = useState(false);
  const [saveForm] = Form.useForm();
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const [orchestrationDrawerVisible, setOrchestrationDrawerVisible] = useState(false);
  const [orchestrationTask, setOrchestrationTask] = useState<CollaborationTask | null>(null);
  const [orchestrationNodes, setOrchestrationNodes, onOrchestrationNodesChange] = useNodesState([]);
  const [orchestrationEdges, setOrchestrationEdges, onOrchestrationEdgesChange] = useEdgesState([]);
  const [orchestrationSaving, setOrchestrationSaving] = useState(false);
  const [autoGenerating, setAutoGenerating] = useState(false);
  const [selectedOrchestrationNode, setSelectedOrchestrationNode] = useState<WorkflowNode | null>(null);
  const [createWorkflowType, setCreateWorkflowType] = useState<string>('');
  const [editWorkflowType, setEditWorkflowType] = useState<string>('');

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [execMessages]);

  const handleCreate = async (values: { title: string; description?: string; prompt?: string; agentIds?: number[]; workflowType?: string }) => {
    setSaving(true);
    try {
      const config: Record<string, unknown> = {};
      if (values.workflowType) {
        config.workflowType = values.workflowType;
      }
      await collaborationService.createTask(collaborationId, {
        title: values.title,
        description: values.description,
        prompt: values.prompt,
        agentIds: values.agentIds,
        config: Object.keys(config).length > 0 ? JSON.stringify(config) : undefined,
      });
      message.success('任务创建成功');
      setCreateModalVisible(false);
      createForm.resetFields();
      setCreateWorkflowType('');
      onUpdate();
    } catch (error: unknown) {
      message.error(`创建失败: ${getErrorMessage(error)}`);
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (task: CollaborationTask) => {
    setCurrentTask(task);
    let workflowType = '';
    try {
      const config = task.config ? JSON.parse(task.config) : {};
      workflowType = config.workflowType || '';
    } catch {}
    setEditWorkflowType(workflowType);
    editForm.setFieldsValue({
      title: task.title,
      description: task.description,
      prompt: task.prompt,
      workflowType,
    });
    setEditModalVisible(true);
  };

  const handleUpdate = async (values: { title: string; description?: string; prompt?: string; workflowType?: string }) => {
    if (!currentTask) return;
    setSaving(true);
    try {
      const config: Record<string, unknown> = {};
      if (values.workflowType) {
        config.workflowType = values.workflowType;
      }
      await collaborationService.updateTask(currentTask.id, {
        title: values.title,
        description: values.description,
        prompt: values.prompt,
        config: Object.keys(config).length > 0 ? JSON.stringify(config) : undefined,
      });
      message.success('任务更新成功');
      setEditModalVisible(false);
      editForm.resetFields();
      setEditWorkflowType('');
      onUpdate();
    } catch (error: unknown) {
      message.error(`更新失败: ${getErrorMessage(error)}`);
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (taskId: string) => {
    try {
      await collaborationService.deleteTask(taskId);
      message.success('任务删除成功');
      onUpdate();
    } catch (error: unknown) {
      message.error(`删除失败: ${getErrorMessage(error)}`);
    }
  };

  const handleCancelTask = async (taskId: string) => {
    try {
      await collaborationService.updateTaskStatus(taskId, 'Cancelled');
      message.success('任务已关闭');
      onUpdate();
    } catch (error: unknown) {
      message.error(`关闭失败: ${getErrorMessage(error)}`);
    }
  };

  const handleOpenExecutor = (task: CollaborationTask) => {
    setCurrentTask(task);
    setTaskInput(task.prompt || task.description || task.title);
    setMagenticStep('input');
    setGeneratedWorkflow(null);
    setSelectedTemplate(null);
    setExecMessages([]);
    setFlowNodes([]);
    setFlowEdges([]);

    if (isMagenticTask(task) && hasTaskFlow(task)) {
      try {
        const flow = JSON.parse(task.taskFlow!) as WorkflowDefinition;
        setGeneratedWorkflow(flow);
        convertWorkflowToFlow(flow);
        setMagenticStep('preview');
      } catch {}
    }

    setExecDrawerVisible(true);
  };

  const loadTemplates = async () => {
    setTemplatesLoading(true);
    try {
      const data = await workflowTemplateApi.getAll(true);
      setTemplates(data || []);
    } catch (error: unknown) {
      console.error('加载模板失败:', error);
    } finally {
      setTemplatesLoading(false);
    }
  };

  const convertWorkflowToFlow = (workflow: WorkflowDefinition) => {
    const reactFlowNodes = workflow.nodes.map((node, index) => ({
      id: node.id,
      type: node.type,
      position: { x: 300, y: index * 120 },
      data: node,
    }));

    const reactFlowEdges = workflow.edges.map((edge, index) => ({
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
    if (!taskInput.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    try {
      const response = await collaborationWorkflowService.generateMagenticPlan(
        Number(collaborationId),
        taskInput
      );
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
    setExecMessages([]);
    setLoading(true);

    try {
      const taskId = currentTask?.id ? Number(currentTask.id) : undefined;
      await collaborationWorkflowService.executeMagenticWorkflow(
        Number(collaborationId),
        generatedWorkflow,
        taskInput,
        taskId,
        (msg) => {
          setExecMessages(prev => [...prev, msg]);
        }
      );
      setMagenticStep('done');
      message.success('Magentic工作流执行完成');
      onUpdate();
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
        originalTask: taskInput,
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
    setExecMessages([]);
    setFlowNodes([]);
    setFlowEdges([]);
  };

  const isMagenticTask = (task: CollaborationTask) => {
    try {
      const config = task.config ? JSON.parse(task.config) : {};
      return config.workflowType === 'Magentic' || config.workflowType === 'ReviewIterative';
    } catch {
      return false;
    }
  };

  const hasTaskFlow = (task: CollaborationTask) => {
    if (!task.taskFlow) return false;
    try {
      const flow = JSON.parse(task.taskFlow);
      return flow.nodes && flow.nodes.length > 0;
    } catch {
      return false;
    }
  };

  const handleOpenOrchestration = (task: CollaborationTask) => {
    setOrchestrationTask(task);
    setSelectedOrchestrationNode(null);
    if (task.taskFlow) {
      try {
        const flow = JSON.parse(task.taskFlow) as WorkflowDefinition;
        const reactFlowNodes = flow.nodes.map((node, index) => ({
          id: node.id,
          type: node.type,
          position: { x: 300, y: index * 120 },
          data: node,
        }));
        const reactFlowEdges: Edge[] = [];
        flow.edges.forEach((edge, index) => {
          const targets = Array.isArray(edge.to) ? edge.to : [edge.to];
          targets.forEach((target, targetIndex) => {
            reactFlowEdges.push({
              id: `edge-${index}-${targetIndex}`,
              source: edge.from,
              target: target,
              type: 'custom',
              data: { type: edge.type, description: edge.description },
              animated: edge.type === 'fan-out',
            });
          });
        });
        setOrchestrationNodes(reactFlowNodes);
        setOrchestrationEdges(reactFlowEdges);
      } catch {
        setOrchestrationNodes([]);
        setOrchestrationEdges([]);
      }
    } else {
      setOrchestrationNodes([]);
      setOrchestrationEdges([]);
    }
    setOrchestrationDrawerVisible(true);
  };

  const handleAddOrchestrationNode = (type: string) => {
    const id = `node-${Date.now()}`;
    const newNode: WorkflowNode = {
      id,
      type: type as NodeTypeEnum,
      name: type === 'start' ? '开始' : type === 'agent' ? '新Agent节点' : type === 'aggregator' ? '汇总结果' : type === 'condition' ? '条件判断' : type === 'review' ? '审核节点' : '循环节点',
    };
    const yPos = orchestrationNodes.length > 0
      ? Math.max(...orchestrationNodes.map(n => n.position.y)) + 120
      : 0;
    setOrchestrationNodes([
      ...orchestrationNodes,
      { id, type, position: { x: 300, y: yPos }, data: newNode },
    ]);
  };

  const handleOrchestrationNodeClick = (_: React.MouseEvent, node: { data: WorkflowNode }) => {
    setSelectedOrchestrationNode(node.data as WorkflowNode);
  };

  const handleUpdateOrchestrationNode = (updatedNode: WorkflowNode) => {
    setOrchestrationNodes(nodes =>
      nodes.map(n => n.id === updatedNode.id ? { ...n, data: updatedNode } : n)
    );
    setSelectedOrchestrationNode(updatedNode);
  };

  const handleDeleteOrchestrationNode = (nodeId: string) => {
    setOrchestrationNodes(nodes => nodes.filter(n => n.id !== nodeId));
    setOrchestrationEdges(edges => edges.filter(e => e.source !== nodeId && e.target !== nodeId));
    if (selectedOrchestrationNode?.id === nodeId) {
      setSelectedOrchestrationNode(null);
    }
  };

  const handleAutoGenerateFlow = async () => {
    if (!orchestrationTask) return;
    const collaborationId = orchestrationTask.collaborationId;
    if (!collaborationId) {
      message.error('无法获取协作ID');
      return;
    }
    setAutoGenerating(true);
    try {
      const taskDesc = orchestrationTask.description || orchestrationTask.title || '请生成工作流';
      const result = await collaborationWorkflowService.generateMagenticPlan(
        Number(collaborationId),
        taskDesc
      );
      if (result.success && result.workflow) {
        const flow = result.workflow;
        const reactFlowNodes = flow.nodes.map((node, index) => ({
          id: node.id,
          type: node.type,
          position: { x: 250, y: index * 120 },
          data: node,
        }));
        const reactFlowEdges = flow.edges.map((edge, index) => ({
          id: `edge-${index}`,
          source: edge.from,
          target: Array.isArray(edge.to) ? edge.to[0] : edge.to,
          type: 'custom',
          data: { type: edge.type, description: edge.description },
          animated: edge.type === 'fan-out',
        }));
        setOrchestrationNodes(reactFlowNodes);
        setOrchestrationEdges(reactFlowEdges);
        setSelectedOrchestrationNode(null);
        message.success('流程编排已自动生成，您可以继续人工修改');
      } else {
        message.error(result.error || '自动生成流程编排失败');
      }
    } catch (error: unknown) {
      message.error('自动生成流程编排失败: ' + (getErrorMessage(error)));
    } finally {
      setAutoGenerating(false);
    }
  };

  const handleSaveOrchestration = async () => {
    if (!orchestrationTask) return;
    setOrchestrationSaving(true);
    try {
      const nodes = orchestrationNodes.map(n => n.data as WorkflowNode);
      const edges = orchestrationEdges.map((e, index) => ({
        type: (e.data?.type as EdgeType) || 'sequential',
        from: e.source,
        to: e.target,
        description: e.data?.description as string | undefined,
      }));
      const flow: WorkflowDefinition = { nodes, edges };
      await collaborationService.updateTaskFlow(
        orchestrationTask.id,
        JSON.stringify(flow)
      );
      message.success('流程编排保存成功');
      setOrchestrationDrawerVisible(false);
      onUpdate();
    } catch (error: unknown) {
      message.error(`保存失败: ${getErrorMessage(error)}`);
    } finally {
      setOrchestrationSaving(false);
    }
  };

  const getAgentAvatar = (sender: string) => {
    if (sender === 'System' || sender === 'Aggregator' || sender === 'Condition') {
      return <Avatar icon={<ThunderboltOutlined />} style={{ backgroundColor: '#722ed1' }} />;
    }
    const agent = agents.find(a => a.agentName === sender);
    if (agent?.agentAvatar) {
      return <Avatar src={agent.agentAvatar} />;
    }
    return <Avatar icon={<RobotOutlined />} style={{ backgroundColor: '#1890ff' }} />;
  };

  const getAgentDisplayName = (sender: string) => {
    const agent = agents.find(a => a.agentName === sender);
    return agent?.agentName || sender;
  };

  const renderMessageItem = (msg: ChatMessageDto, index: number) => {
    const msgType = msg.metadata?.type;
    const isSystem = msgType === 'system' || msgType === 'system_complete' || msgType === 'step_start';
    const isError = msgType === 'error';
    const isAggregator = msgType === 'aggregator';
    const isCondition = msgType === 'condition';

    let bgColor = '#fff';
    let borderColor = 'transparent';
    if (isSystem) { bgColor = '#e6f7ff'; borderColor = '#91d5ff'; }
    if (isError) { bgColor = '#fff2f0'; borderColor = '#ffccc7'; }
    if (isAggregator) { bgColor = '#f9f0ff'; borderColor = '#d3adf7'; }
    if (isCondition) { bgColor = '#fff7e6'; borderColor = '#ffd591'; }

    return (
      <List.Item key={index} style={{ border: 'none', padding: '8px 0' }}>
        <div style={{ display: 'flex', width: '100%', gap: '12px' }}>
          <div style={{ flexShrink: 0 }}>{getAgentAvatar(msg.sender)}</div>
          <div style={{ flex: 1, minWidth: 0 }}>
            <div style={{ marginBottom: 4 }}>
              <Text strong style={{ marginRight: 8 }}>{getAgentDisplayName(msg.sender)}</Text>
              {msgType === 'step_start' && <Tag color="blue">步骤 {msg.metadata?.step}</Tag>}
              {msgType === 'agent_response' && <Tag color="green">执行结果</Tag>}
              {isAggregator && <Tag color="purple">汇总</Tag>}
              {isCondition && <Tag color="orange">条件</Tag>}
              {isSystem && <Tag color="blue">系统</Tag>}
              {msgType === 'manager_thinking' && <Tag color="gold">协调者点名</Tag>}
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
            <Text strong style={{ display: 'block', marginBottom: 8 }}>任务描述</Text>
            <TextArea
              value={taskInput}
              onChange={(e) => setTaskInput(e.target.value)}
              placeholder="请详细描述任务内容"
              rows={4}
              style={{ marginBottom: 16 }}
            />
            <div style={{ display: 'flex', justifyContent: 'flex-end' }}>
              <Button
                type="primary"
                onClick={() => setMagenticStep('source')}
                disabled={!taskInput.trim()}
                icon={<CheckCircleOutlined />}
              >
                下一步：选择工作流
              </Button>
            </div>
          </div>
        )}

        {magenticStep === 'source' && (
          <div>
            <Text strong style={{ display: 'block', marginBottom: 12 }}>选择工作流来源</Text>
            <Row gutter={12}>
              <Col span={12}>
                <Card
                  hoverable
                  onClick={handleGenerateWorkflow}
                  style={{
                    textAlign: 'center',
                    background: 'linear-gradient(135deg, #f6ffed 0%, #d9f7be 100%)',
                    border: '2px solid #b7eb8f'
                  }}
                >
                  <RobotOutlined style={{ fontSize: '36px', color: '#52c41a' }} />
                  <h4 style={{ marginTop: 12 }}>协调者自动生成</h4>
                  <Text type="secondary" style={{ fontSize: 12 }}>让Manager Agent根据任务自动规划工作流</Text>
                </Card>
              </Col>
              <Col span={12}>
                <Card
                  hoverable
                  onClick={loadTemplates}
                  style={{
                    textAlign: 'center',
                    background: 'linear-gradient(135deg, #e6f7ff 0%, #bae7ff 100%)',
                    border: '2px solid #91d5ff'
                  }}
                >
                  <AppstoreOutlined style={{ fontSize: '36px', color: '#1890ff' }} />
                  <h4 style={{ marginTop: 12 }}>选择模板</h4>
                  <Text type="secondary" style={{ fontSize: 12 }}>使用已保存的工作流模板</Text>
                </Card>
              </Col>
            </Row>

            {templates.length > 0 && (
              <div style={{ marginTop: 16 }}>
                <Divider>可用模板</Divider>
                <Spin spinning={templatesLoading}>
                  <List
                    grid={{ gutter: 12, column: 1 }}
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
            <Text strong style={{ display: 'block', marginBottom: 12 }}>
              {selectedTemplate ? `模板: ${selectedTemplate.name}` : '协调者生成的工作流'}
            </Text>

            <Alert
              message={selectedTemplate
                ? `已选择模板「${selectedTemplate.name}」`
                : '协调者已根据任务生成工作流，确认后执行'}
              type="success"
              showIcon
              style={{ marginBottom: 12 }}
            />

            <Card size="small" title="工作流图" style={{ marginBottom: 12 }}>
              <div style={{ height: '300px' }}>
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

            <Card size="small" title="工作流节点详情" style={{ marginBottom: 12 }}>
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

        {magenticStep === 'executing' && (
          <div>
            <div style={{ textAlign: 'center', padding: '16px 0' }}>
              <Spin size="large" />
              <div style={{ marginTop: 12 }}>
                <Text>Magentic工作流正在执行中...</Text>
              </div>
            </div>

            {execMessages.length > 0 && (
              <Card size="small" title="执行过程" style={{ maxHeight: '400px', overflowY: 'auto' }}>
                <List dataSource={execMessages} renderItem={renderMessageItem} />
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
              style={{ marginBottom: 12 }}
            />

            {execMessages.length > 0 && (
              <Card size="small" title="执行结果" style={{ maxHeight: '500px', overflowY: 'auto' }}>
                <List dataSource={execMessages} renderItem={renderMessageItem} />
              </Card>
            )}

            <div style={{ marginTop: 16, display: 'flex', justifyContent: 'space-between' }}>
              <Button icon={<ReloadOutlined />} onClick={handleResetMagentic}>
                重新执行
              </Button>
              <Button icon={<SaveOutlined />} onClick={() => setSaveModalVisible(true)}>
                保存为模板
              </Button>
            </div>
          </div>
        )}
      </div>
    );
  };

  const columns: ColumnsType<CollaborationTask> = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 60,
    },
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      width: 180,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => (
        <Tag color={TASK_STATUS_COLOR_MAP[status]}>{status}</Tag>
      ),
    },
    {
      title: '工作流类型',
      key: 'workflowType',
      width: 120,
      render: (_: unknown, record: CollaborationTask) => {
        const wt = (() => {
          try {
            const config = record.config ? JSON.parse(record.config) : {};
            return config.workflowType || '';
          } catch { return ''; }
        })();
        if (wt === 'Magentic' || wt === 'ReviewIterative') return <Tag color="purple"><ThunderboltOutlined /> Magentic</Tag>;
        if (wt === 'Sequential') return <Tag color="blue">顺序执行</Tag>;
        if (wt === 'Concurrent') return <Tag color="green">并发执行</Tag>;
        if (wt === 'GroupChat') return <Tag color="orange">群聊协作</Tag>;
        return <Tag>默认</Tag>;
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '完成时间',
      dataIndex: 'completedAt',
      key: 'completedAt',
      width: 160,
      render: (date: string | null) => (date ? new Date(date).toLocaleString('zh-CN') : '-'),
    },
    {
      title: '操作',
      key: 'action',
      width: 300,
      render: (_: unknown, record: CollaborationTask) => {
        const magentic = isMagenticTask(record);
        const hasFlow = hasTaskFlow(record);
        const canExecute = record.status !== 'Completed' && record.status !== 'Cancelled'
          && (!magentic || hasFlow);

        return (
          <Space size="small">
            {magentic && (
              <Button
                size="small"
                icon={<ApartmentOutlined />}
                onClick={() => handleOpenOrchestration(record)}
              >
                流程编排
              </Button>
            )}
            <Tooltip title={!canExecute && magentic && !hasFlow ? '请先配置流程编排' : '执行Magentic工作流'}>
              <Button
                type="primary"
                icon={<PlayCircleOutlined />}
                size="small"
                onClick={() => handleOpenExecutor(record)}
                disabled={!canExecute}
              >
                执行
              </Button>
            </Tooltip>
            <Button
              size="small"
              icon={<EditOutlined />}
              onClick={() => handleEdit(record)}
            >
              编辑
            </Button>
            {record.status === 'InProgress' && (
              <Popconfirm
                title="确定关闭此任务吗？"
                onConfirm={() => handleCancelTask(record.id)}
              >
                <Button size="small" danger icon={<StopOutlined />}>
                  关闭
                </Button>
              </Popconfirm>
            )}
            <Popconfirm
              title="确定删除此任务吗？"
              onConfirm={() => handleDelete(record.id)}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Space>
        );
      },
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Space>
          <Tag color="default">Pending: {tasks.filter(t => t.status === 'Pending').length}</Tag>
          <Tag color="processing">InProgress: {tasks.filter(t => t.status === 'InProgress').length}</Tag>
          <Tag color="success">Completed: {tasks.filter(t => t.status === 'Completed').length}</Tag>
        </Space>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setCreateModalVisible(true)}
        >
          新建任务
        </Button>
      </div>

      <Table
        dataSource={tasks}
        columns={columns}
        rowKey="id"
        pagination={false}
      />

      <Drawer
        title={
          <Space>
            <ApartmentOutlined />
            <span>流程编排 - {orchestrationTask?.title}</span>
          </Space>
        }
        open={orchestrationDrawerVisible}
        onClose={() => { setOrchestrationDrawerVisible(false); setSelectedOrchestrationNode(null); }}
        width="90vw"
        destroyOnClose
        extra={
          <Space>
            <Button onClick={() => { setOrchestrationDrawerVisible(false); setSelectedOrchestrationNode(null); }}>取消</Button>
            <Button type="primary" icon={<SaveOutlined />} onClick={handleSaveOrchestration} loading={orchestrationSaving}>
              保存编排
            </Button>
          </Space>
        }
      >
        {orchestrationTask && (
          <div>
            <Alert
              message="在此编排Magentic工作流的执行流程。点击节点可编辑属性，拖拽节点调整位置，添加节点构建工作流图。"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
            <Row gutter={16}>
              <Col span={selectedOrchestrationNode ? 14 : 24}>
                <Card size="small" title="添加节点" style={{ marginBottom: 12 }}>
                  <Space wrap>
                    <Button
                      type="primary"
                      size="small"
                      onClick={handleAutoGenerateFlow}
                      icon={<BulbOutlined />}
                      loading={autoGenerating}
                    >
                      自动生成流程编排
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('start')} icon={<PlayCircleOutlined />}>
                      开始节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('agent')} icon={<RobotOutlined />}>
                      Agent节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('aggregator')} icon={<TeamOutlined />}>
                      汇总节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('condition')} icon={<BulbOutlined />}>
                      条件节点
                    </Button>
                    <Button size="small" onClick={() => handleAddOrchestrationNode('review')} icon={<SafetyCertificateOutlined />}>
                      审核节点
                    </Button>
                  </Space>
                </Card>
                <Card size="small" title="工作流图" style={{ marginBottom: 12 }}>
                  <div style={{ height: '450px' }}>
                    <ReactFlow
                      nodes={orchestrationNodes}
                      edges={orchestrationEdges}
                      nodeTypes={nodeTypes}
                      edgeTypes={edgeTypes}
                      onNodesChange={onOrchestrationNodesChange}
                      onEdgesChange={onOrchestrationEdgesChange}
                      onNodeClick={handleOrchestrationNodeClick}
                      fitView
                    >
                      <Background />
                      <Controls />
                      <MiniMap />
                    </ReactFlow>
                  </div>
                </Card>
                <Card size="small" title="任务信息" style={{ marginBottom: 12 }}>
                  <div style={{ marginBottom: 8 }}>
                    <Text strong>任务描述：</Text>
                    <Text>{orchestrationTask.description || orchestrationTask.title}</Text>
                  </div>
                  {orchestrationTask.prompt && (
                    <div style={{ marginBottom: 8 }}>
                      <Text strong>提示词：</Text>
                      <Text>{orchestrationTask.prompt}</Text>
                    </div>
                  )}
                  <div>
                    <Text strong>可用Agent：</Text>
                    <div style={{ marginTop: 4 }}>
                      {agents.map(agent => (
                        <Tag key={agent.agentId} color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ marginBottom: 4 }}>
                          {agent.agentName} ({agent.role || 'Worker'})
                        </Tag>
                      ))}
                    </div>
                  </div>
                </Card>
              </Col>
              {selectedOrchestrationNode && (
                <Col span={10}>
                  <Card
                    size="small"
                    title={
                      <Space>
                        <EditOutlined />
                        <span>节点属性 - {selectedOrchestrationNode.name}</span>
                        <Button
                          type="text"
                          size="small"
                          onClick={() => setSelectedOrchestrationNode(null)}
                        >
                          关闭
                        </Button>
                      </Space>
                    }
                    style={{ position: 'sticky', top: 0 }}
                  >
                    <Form layout="vertical" size="small">
                      <Form.Item label="节点ID">
                        <Input value={selectedOrchestrationNode.id} disabled />
                      </Form.Item>
                      <Form.Item label="节点名称" required>
                        <Input
                          value={selectedOrchestrationNode.name}
                          onChange={(e) => handleUpdateOrchestrationNode({
                            ...selectedOrchestrationNode,
                            name: e.target.value,
                          })}
                          placeholder="请输入节点名称"
                        />
                      </Form.Item>

                      {selectedOrchestrationNode.type === 'agent' && (
                        <>
                          <Form.Item label="选择Agent">
                            <Select
                              value={selectedOrchestrationNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateOrchestrationNode({
                                  ...selectedOrchestrationNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || agent?.agentName || '',
                                });
                              }}
                              placeholder="请选择Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="Agent职业">
                            <Input
                              value={selectedOrchestrationNode.agentRole || ''}
                              disabled
                              placeholder="选择Agent后自动填充"
                            />
                          </Form.Item>
                          <Form.Item label="任务描述/输入模板">
                            <TextArea
                              value={selectedOrchestrationNode.inputTemplate || ''}
                              onChange={(e) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                inputTemplate: e.target.value,
                              })}
                              rows={4}
                              placeholder="使用 {{参数名}} 表示参数，例如：请分析{{topic}}的{{aspect}}"
                            />
                          </Form.Item>
                          <Alert
                            message="参数使用说明"
                            description={
                              <div style={{ fontSize: 12 }}>
                                <div>• {'{{input}}'} = 上游节点的输出结果</div>
                                <div>• {'{{originalInput}}'} / {'{{task}}'} = 原始任务描述</div>
                                <div>• {'{{lastResult}}'} = 最近一个节点的输出</div>
                                <div>• {'{{nodeId}}'} = 引用指定节点的输出（如 {'{{node-1}}'}）</div>
                                <div>• 不写模板时，默认使用上游节点的输出作为输入</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {(selectedOrchestrationNode.type === 'condition' || selectedOrchestrationNode.type === 'loop') && (
                        <>
                          <Form.Item label="条件表达式">
                            <TextArea
                              value={selectedOrchestrationNode.condition || ''}
                              onChange={(e) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                condition: e.target.value,
                              })}
                              rows={3}
                              placeholder="例如：result.Contains(&quot;APPROVED&quot;)"
                            />
                          </Form.Item>
                          <Alert
                            message="C# 条件表达式语法"
                            description={
                              <div style={{ fontSize: 12, fontFamily: 'monospace' }}>
                                <div><b>字符串包含：</b>result.Contains("关键词")</div>
                                <div><b>字符串相等：</b>result.Equals("完成")</div>
                                <div><b>开头匹配：</b>result.StartsWith("错误")</div>
                                <div><b>结尾匹配：</b>result.EndsWith("通过")</div>
                                <div><b>长度判断：</b>result.Length &gt; 100</div>
                                <div><b>空值判断：</b>result.IsNullOrEmpty</div>
                                <div><b>取反：</b>!result.Contains("失败")</div>
                                <div><b>组合：</b>result.Contains("A") &amp;&amp; result.Length &gt; 50</div>
                                <div><b>或：</b>result.Contains("A") || result.Contains("B")</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {selectedOrchestrationNode.type === 'review' && (
                        <>
                          <Form.Item label="选择审核Agent">
                            <Select
                              value={selectedOrchestrationNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateOrchestrationNode({
                                  ...selectedOrchestrationNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || '审核者',
                                });
                              }}
                              placeholder="请选择审核Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="审核角色">
                            <Input
                              value={selectedOrchestrationNode.agentRole || ''}
                              disabled
                              placeholder="选择Agent后自动填充"
                            />
                          </Form.Item>
                          <Form.Item label="审核标准">
                            <TextArea
                              value={selectedOrchestrationNode.reviewCriteria || ''}
                              onChange={(e) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                reviewCriteria: e.target.value,
                              })}
                              rows={3}
                              placeholder="例如：请从代码质量、安全性、可维护性方面审核"
                            />
                          </Form.Item>
                          <Form.Item label="审核通过关键词">
                            <Input
                              value={selectedOrchestrationNode.approvalKeyword || ''}
                              onChange={(e) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                approvalKeyword: e.target.value,
                              })}
                              placeholder="[APPROVED]"
                            />
                          </Form.Item>
                          <Form.Item label="打回目标节点ID">
                            <Select
                              value={selectedOrchestrationNode.rejectTargetNode || undefined}
                              onChange={(val) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                rejectTargetNode: val,
                              })}
                              placeholder="审核不通过时打回到哪个节点"
                              allowClear
                            >
                              {orchestrationNodes
                                .filter(n => n.id !== selectedOrchestrationNode.id && n.data?.type !== 'start')
                                .map(n => (
                                  <Option key={n.id} value={n.id}>
                                    {n.data?.name || n.id}
                                  </Option>
                                ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="最大重试次数">
                            <Select
                              value={selectedOrchestrationNode.maxRetries || 3}
                              onChange={(val) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                maxRetries: val,
                              })}
                            >
                              <Option value={1}>1次</Option>
                              <Option value={2}>2次</Option>
                              <Option value={3}>3次</Option>
                              <Option value={5}>5次</Option>
                              <Option value={10}>10次</Option>
                            </Select>
                          </Form.Item>
                          <Alert
                            message="审核节点说明"
                            description={
                              <div style={{ fontSize: 12 }}>
                                <div>• 审核Agent会审核前序节点的工作成果</div>
                                <div>• 审核通过（回复包含通过关键词）→ 走"通过"路径</div>
                                <div>• 审核不通过 → 打回到目标节点重新执行</div>
                                <div>• 超过最大重试次数后强制通过</div>
                              </div>
                            }
                            type="info"
                            showIcon
                            style={{ marginBottom: 12 }}
                          />
                        </>
                      )}

                      {selectedOrchestrationNode.type === 'start' && (
                        <Alert
                          message="开始节点无需配置参数，工作流将从此节点开始执行。"
                          type="info"
                          showIcon
                        />
                      )}

                      {selectedOrchestrationNode.type === 'aggregator' && (
                        <>
                          <Form.Item label="选择汇总Agent">
                            <Select
                              value={selectedOrchestrationNode.agentId || undefined}
                              onChange={(val) => {
                                const agent = agents.find(a => String(a.agentId) === val);
                                handleUpdateOrchestrationNode({
                                  ...selectedOrchestrationNode,
                                  agentId: val,
                                  agentRole: agent?.agentType || agent?.role || agent?.agentName || '',
                                });
                              }}
                              placeholder="请选择负责汇总的Agent"
                              allowClear
                              showSearch
                              optionFilterProp="children"
                            >
                              {agents.map(agent => (
                                <Option key={agent.agentId} value={String(agent.agentId)}>
                                  <Space>
                                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                                      {agent.agentType || agent.role || 'Worker'}
                                    </Tag>
                                    {agent.agentName}
                                  </Space>
                                </Option>
                              ))}
                            </Select>
                          </Form.Item>
                          <Form.Item label="Agent职业">
                            <Input
                              value={selectedOrchestrationNode.agentRole || ''}
                              disabled
                              placeholder="选择Agent后自动填充"
                            />
                          </Form.Item>
                          <Form.Item label="汇总指令">
                            <TextArea
                              value={selectedOrchestrationNode.inputTemplate || ''}
                              onChange={(e) => handleUpdateOrchestrationNode({
                                ...selectedOrchestrationNode,
                                inputTemplate: e.target.value,
                              })}
                              rows={3}
                              placeholder="请输入汇总指令，例如：请汇总以上所有结果，生成最终报告"
                            />
                          </Form.Item>
                          <Alert
                            message="汇总节点由指定Agent汇总所有上游节点的结果，请选择合适的Agent并填写汇总指令。"
                            type="info"
                            showIcon
                          />
                        </>
                      )}

                      <Divider />
                      <Button
                        danger
                        icon={<DeleteOutlined />}
                        onClick={() => handleDeleteOrchestrationNode(selectedOrchestrationNode.id)}
                        block
                        disabled={selectedOrchestrationNode.type === 'start'}
                      >
                        删除节点
                      </Button>
                    </Form>
                  </Card>
                </Col>
              )}
            </Row>
          </div>
        )}
      </Drawer>

      <Modal
        title="新建任务"
        open={createModalVisible}
        onCancel={() => { setCreateModalVisible(false); createForm.resetFields(); setCreateWorkflowType(''); }}
        onOk={() => createForm.submit()}
        confirmLoading={saving}
      >
        <Form form={createForm} layout="vertical" onFinish={handleCreate}>
          <Form.Item label="任务标题" name="title" rules={[{ required: true, message: '请输入任务标题' }]}>
            <Input placeholder="请输入任务标题" />
          </Form.Item>
          <Form.Item label="任务描述" name="description">
            <TextArea rows={3} placeholder="请输入任务描述" />
          </Form.Item>
          <Form.Item label="工作流类型" name="workflowType">
            <Select
              placeholder="选择工作流类型"
              allowClear
              onChange={(val) => setCreateWorkflowType(val || '')}
            >
              <Option value="Magentic">
                <Space><ThunderboltOutlined style={{ color: '#722ed1' }} /> Magentic智能工作流</Space>
              </Option>
              <Option value="Sequential">
                <Space><PlayCircleOutlined style={{ color: '#1890ff' }} /> 顺序执行</Space>
              </Option>
              <Option value="Concurrent">
                <Space><TeamOutlined style={{ color: '#52c41a' }} /> 并发执行</Space>
              </Option>
              <Option value="GroupChat">
                <Space><MessageOutlined style={{ color: '#fa8c16' }} /> 群聊协作</Space>
              </Option>
            </Select>
          </Form.Item>
          {(createWorkflowType !== 'Magentic' && createWorkflowType !== 'ReviewIterative') && (
            <Form.Item label="任务提示词" name="prompt">
              <TextArea rows={4} placeholder="请输入任务提示词" />
            </Form.Item>
          )}
          {(createWorkflowType === 'Magentic' || createWorkflowType === 'ReviewIterative') && (
            <Alert
              message="Magentic智能工作流的流程编排、参数设置请在任务列表的「流程编排」按钮中配置"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}
          <Form.Item label="指定Agent" name="agentIds">
            <Select
              mode="multiple"
              placeholder="选择参与的Agent（留空则使用协作中所有Agent）"
              optionFilterProp="children"
            >
              {agents.map(agent => (
                <Option key={agent.agentId} value={agent.agentId}>
                  <Space>
                    <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'}>
                      {agent.role || 'Worker'}
                    </Tag>
                    {agent.agentName}
                  </Space>
                </Option>
              ))}
            </Select>
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="编辑任务"
        open={editModalVisible}
        onCancel={() => { setEditModalVisible(false); editForm.resetFields(); setEditWorkflowType(''); }}
        onOk={() => editForm.submit()}
        confirmLoading={saving}
      >
        <Form form={editForm} layout="vertical" onFinish={handleUpdate}>
          <Form.Item label="任务标题" name="title" rules={[{ required: true, message: '请输入任务标题' }]}>
            <Input placeholder="请输入任务标题" />
          </Form.Item>
          <Form.Item label="任务描述" name="description">
            <TextArea rows={3} placeholder="请输入任务描述" />
          </Form.Item>
          <Form.Item label="工作流类型" name="workflowType">
            <Select
              placeholder="选择工作流类型"
              allowClear
              onChange={(val) => setEditWorkflowType(val || '')}
            >
              <Option value="Magentic">
                <Space><ThunderboltOutlined style={{ color: '#722ed1' }} /> Magentic智能工作流</Space>
              </Option>
              <Option value="Sequential">
                <Space><PlayCircleOutlined style={{ color: '#1890ff' }} /> 顺序执行</Space>
              </Option>
              <Option value="Concurrent">
                <Space><TeamOutlined style={{ color: '#52c41a' }} /> 并发执行</Space>
              </Option>
              <Option value="GroupChat">
                <Space><MessageOutlined style={{ color: '#fa8c16' }} /> 群聊协作</Space>
              </Option>
            </Select>
          </Form.Item>
          {(editWorkflowType !== 'Magentic' && editWorkflowType !== 'ReviewIterative') && (
            <Form.Item label="任务提示词" name="prompt">
              <TextArea rows={4} placeholder="请输入任务提示词" />
            </Form.Item>
          )}
          {(editWorkflowType === 'Magentic' || editWorkflowType === 'ReviewIterative') && (
            <Alert
              message="Magentic智能工作流的流程编排、参数设置请在任务列表的「流程编排」按钮中配置"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}
        </Form>
      </Modal>

      <Drawer
        title={
          <Space>
            <RobotOutlined />
            <span>执行Magentic工作流 - {currentTask?.title}</span>
          </Space>
        }
        open={execDrawerVisible}
        onClose={() => setExecDrawerVisible(false)}
        width={720}
        destroyOnClose
      >
        {currentTask && renderMagenticFlow()}
      </Drawer>

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
        </Form>
      </Modal>
    </div>
  );
};

export default TaskTable;
