import { getErrorMessage, getAxiosErrorData } from '../utils/errorHandler';
import React, { useState, useRef, useMemo, useCallback } from 'react';
import { Table, Button, Modal, Form, Input, Tag, Space, message, Tabs, Select, Popconfirm, Row, Col, Alert, Radio, InputNumber, Typography, Tooltip, Transfer, Collapse, Switch } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, UserOutlined, FolderOutlined, EyeOutlined, EyeInvisibleOutlined, SwapOutlined, CrownOutlined, BulbOutlined, InfoCircleOutlined, MailOutlined, ApartmentOutlined, DashboardOutlined, QuestionCircleOutlined, StopOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration, CollaborationTask, CollaborationAgent } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';
import { useNavigate } from 'react-router-dom';
import ChatHistory from './collaboration-detail/ChatHistory';
import { getApiUrl } from '../config/api';
import { useCollaborations } from '../hooks/useCollaborations';
import CollaborationTasks, { Task as CollaborationTaskItem } from '../components/CollaborationTasks';

const { Option } = Select;
const { Text } = Typography;

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
  }
};

const Collaborations: React.FC = () => {
  const navigate = useNavigate();
  const {
    collaborations,
    agents,
    loading,
    loadInitialData,
    loadCollaborationData,
    refreshCollaboration,
    setCollaborations,
  } = useCollaborations();
  
  const [modalVisible, setModalVisible] = useState(false);
  const [addAgentModalVisible, setAddAgentModalVisible] = useState(false);
  const [createTaskModalVisible, setCreateTaskModalVisible] = useState(false);
  const [editTaskModalVisible, setEditTaskModalVisible] = useState(false);
  const [editAgentModalVisible, setEditAgentModalVisible] = useState(false);
  const [chatHistoryModalVisible, setChatHistoryModalVisible] = useState(false);
  const [executionModalVisible, setExecutionModalVisible] = useState(false);
  const [executionMessages, setExecutionMessages] = useState<any[]>([]);
  const [showManagerThinking, setShowManagerThinking] = useState(false);
  const [isExecuting, setIsExecuting] = useState(false);
  const [selectedTask, setSelectedTask] = useState<any>(null);
  const [editingTask, setEditingTask] = useState<any>(null);
  const [selectedCollaboration, setSelectedCollaboration] = useState<Collaboration | null>(null);
  const [editingAgent, setEditingAgent] = useState<any>(null);
  const [form] = Form.useForm();
  const [addAgentForm] = Form.useForm();
  const [createTaskForm] = Form.useForm();
  const [editTaskForm] = Form.useForm();
  const [editAgentForm] = Form.useForm();
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null);
  const [selectedTaskAgents, setSelectedTaskAgents] = useState<string[]>([]);
  const [editTaskAgents, setEditTaskAgents] = useState<string[]>([]);
  const [taskWorkflowType, setTaskWorkflowType] = useState<string>('GroupChat');
  const [taskOrchestrationMode, setTaskOrchestrationMode] = useState<string>('Manager');
  const [taskManagerAgentId, setTaskManagerAgentId] = useState<number | null>(null);
  const [taskManagerCustomPrompt, setTaskManagerCustomPrompt] = useState<string>('');
  const [taskMaxIterations, setTaskMaxIterations] = useState<number>(10);
  const [taskWorkflowPlanId, setTaskWorkflowPlanId] = useState<number | null>(null);
  const [taskMaxAttempts, setTaskMaxAttempts] = useState<number>(5);
  const [taskThresholds, setTaskThresholds] = useState<string>('');
  const initializedRef = useRef(false);

  const handleCreate = () => {
    setSelectedCollaboration(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (values.smtp && values.smtp.server) {
        values.config = JSON.stringify({ smtp: values.smtp });
      }
      delete values.smtp;
      
      if (selectedCollaboration) {
        await collaborationService.updateCollaboration(selectedCollaboration.id, values);
        message.success('更新成功');
        setModalVisible(false);
        setSelectedCollaboration(null);
        await loadCollaborationData(selectedCollaboration.id);
      } else {
        const newCollab = await collaborationService.createCollaboration(values);
        message.success('创建成功');
        setModalVisible(false);
        setSelectedCollaboration(null);
        await loadInitialData();
      }
    } catch (error) {
      message.error(selectedCollaboration ? '更新失败' : '创建失败');
    }
  };

  const handleAddAgent = (collaboration: Collaboration) => {
    setSelectedCollaboration(collaboration);
    addAgentForm.resetFields();
    setSelectedAgentId(null);
    setAddAgentModalVisible(true);
  };

  const handleSubmitAddAgent = async () => {
    try {
      const values = await addAgentForm.validateFields();
      if (selectedCollaboration) {
        await collaborationService.addAgentToCollaboration(selectedCollaboration.id, values);
        message.success('添加成功');
        setAddAgentModalVisible(false);
        await refreshCollaboration(selectedCollaboration.id);
      }
    } catch (error: unknown) {
      const errorMessage = getAxiosErrorData(error).data?.message || getErrorMessage(error, '添加失败');
      message.error(errorMessage);
    }
  };

  const handleAgentSelect = (agentId: number) => {
    setSelectedAgentId(agentId);
    const selectedAgent = agents.find(a => a.id === agentId);
    
    const customPrompt = selectedAgent?.systemPrompt || '';
    
    addAgentForm.setFieldsValue({ 
      agentId,
      customPrompt 
    });
  };

  const handleCreateTask = (collaboration: Collaboration) => {
    setSelectedCollaboration(collaboration);
    createTaskForm.resetFields();
    setSelectedTaskAgents([]);
    
    const defaultManager = collaboration.agents?.find(a => a.role === 'Manager');
    const defaultMode = defaultManager ? 'Manager' : 'RoundRobin';
    
    setTaskOrchestrationMode(defaultMode);
    setTaskManagerAgentId(defaultManager?.agentId || null);
    setTaskManagerCustomPrompt('');
    setTaskMaxIterations(10);
    setCreateTaskModalVisible(true);
  };

  const handleCloseCreateTask = () => {
    createTaskForm.resetFields();
    setSelectedTaskAgents([]);
    setTaskWorkflowType('GroupChat');
    setTaskOrchestrationMode('RoundRobin');
    setTaskManagerAgentId(null);
    setTaskManagerCustomPrompt('');
    setTaskMaxIterations(10);
    setTaskWorkflowPlanId(null);
    setTaskMaxAttempts(5);
    setTaskThresholds('');
    setCreateTaskModalVisible(false);
  };

  const handleSubmitCreateTask = async () => {
    try {
      const values = await createTaskForm.validateFields();
      
      const workerAgents = selectedTaskAgents
        .filter(key => Number(key) !== taskManagerAgentId)
        .map(key => ({ agentId: Number(key) }));
      
      const config: Record<string, unknown> = {
        workflowType: taskWorkflowType,
        orchestrationMode: taskOrchestrationMode,
        maxIterations: taskMaxIterations,
        managerAgentId: taskManagerAgentId,
        managerCustomPrompt: taskManagerCustomPrompt || undefined,
        workerAgents: workerAgents.length > 0 ? workerAgents : undefined
      };

      values.config = JSON.stringify(config);
      
      if (selectedCollaboration) {
        await collaborationService.createTask(selectedCollaboration.id, values);
        message.success('创建成功');
        handleCloseCreateTask();
        await refreshCollaboration(selectedCollaboration.id);
      }
    } catch (error) {
      message.error('创建失败');
    }
  };

  const handleDeleteCollaboration = async (id: string) => {
    try {
      await collaborationService.deleteCollaboration(id);
      message.success('删除成功');
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleRemoveAgent = async (collaborationId: string, agentId: number) => {
    try {
      await collaborationService.removeAgentFromCollaboration(collaborationId, agentId);
      message.success('移除成功');
      await refreshCollaboration(collaborationId);
    } catch (error) {
      message.error('移除失败');
    }
  };

  const handleEditTask = async (task: CollaborationTaskItem) => {
    const collaboration = collaborations.find(c => c.id === task.collaborationId);
    if (!collaboration) {
      message.error('找不到对应的团队');
      return;
    }
    
    setSelectedCollaboration(collaboration);
    setEditingTask(task);
    editTaskForm.setFieldsValue({
      title: task.title,
      description: task.description,
      prompt: task.prompt,
      gitUrl: task.gitUrl,
      gitBranch: task.gitBranch,
      gitToken: task.gitToken,
    });
    
    if (task.config) {
      try {
        const config = JSON.parse(task.config);
        setTaskWorkflowType(config.workflowType || 'GroupChat');
        setTaskOrchestrationMode(config.orchestrationMode || 'RoundRobin');
        setTaskManagerAgentId(config.managerAgentId || null);
        setTaskManagerCustomPrompt(config.managerCustomPrompt || '');
        setTaskMaxIterations(config.maxIterations || 10);
        setTaskWorkflowPlanId(config.workflowPlanId || null);
        setTaskMaxAttempts(config.maxAttempts || 5);
        setTaskThresholds(config.thresholds ? JSON.stringify(config.thresholds, null, 2) : '');
        
        const allAgentIds: string[] = [];
        if (config.managerAgentId) {
          allAgentIds.push(config.managerAgentId.toString());
        }
        if (config.workerAgents && config.workerAgents.length > 0) {
          allAgentIds.push(...config.workerAgents.map((w: { agentId: number }) => w.agentId.toString()));
        }
        setEditTaskAgents(allAgentIds);
      } catch {
        setTaskWorkflowType('GroupChat');
        setTaskOrchestrationMode('RoundRobin');
        setTaskManagerAgentId(null);
        setTaskManagerCustomPrompt('');
        setTaskMaxIterations(10);
        setTaskWorkflowPlanId(null);
        setTaskMaxAttempts(5);
        setTaskThresholds('');
        setEditTaskAgents([]);
      }
    } else {
      setTaskWorkflowType('GroupChat');
      setTaskOrchestrationMode('RoundRobin');
      setTaskManagerAgentId(null);
      setTaskManagerCustomPrompt('');
      setTaskMaxIterations(10);
      setTaskWorkflowPlanId(null);
      setTaskMaxAttempts(5);
      setTaskThresholds('');
      setEditTaskAgents([]);
    }
    
    setEditTaskModalVisible(true);
  };

  const handleCloseEditTask = () => {
    editTaskForm.resetFields();
    setEditTaskAgents([]);
    setEditingTask(null);
    setTaskWorkflowType('GroupChat');
    setTaskOrchestrationMode('RoundRobin');
    setTaskManagerAgentId(null);
    setTaskManagerCustomPrompt('');
    setTaskMaxIterations(10);
    setTaskWorkflowPlanId(null);
    setTaskMaxAttempts(5);
    setTaskThresholds('');
    setEditTaskModalVisible(false);
  };

  const handleSubmitEditTask = async () => {
    try {
      const values = await editTaskForm.validateFields();
      
      const workerAgents = editTaskAgents
        .filter(key => Number(key) !== taskManagerAgentId)
        .map(key => ({ agentId: Number(key) }));
      
      const config: Record<string, unknown> = {
        workflowType: taskWorkflowType,
        orchestrationMode: taskOrchestrationMode,
        maxIterations: taskMaxIterations,
        managerAgentId: taskManagerAgentId,
        managerCustomPrompt: taskManagerCustomPrompt || undefined,
        workerAgents: workerAgents.length > 0 ? workerAgents : undefined
      };

      values.config = JSON.stringify(config);
      
      await collaborationService.updateTask(editingTask.id, values);
      message.success('任务更新成功');
      handleCloseEditTask();
      await refreshCollaboration(editingTask.collaborationId);
    } catch (error) {
      message.error('更新任务失败');
    }
  };

  const handleDeleteTask = async (task: CollaborationTaskItem) => {
    try {
      await collaborationService.deleteTask(String(task.id));
      message.success('任务删除成功');
      await refreshCollaboration(task.collaborationId);
    } catch (error) {
      message.error('删除任务失败');
    }
  };

  const handleExecuteTask = (task: CollaborationTaskItem) => {
    const collaboration = collaborations.find(c => c.id === task.collaborationId);
    
    if (!collaboration) {
      message.error('找不到对应的团队');
      return;
    }

    const agents = collaboration.agents || [];
    
    let config: Record<string, unknown> = {
      workflowType: 'GroupChat',
      orchestrationMode: 'RoundRobin',
      maxIterations: 10
    };
    
    if (task.config) {
      try {
        config = { ...config, ...JSON.parse(task.config) };
      } catch {}
    }

    const hasManager = agents.some(agent => agent.role === 'Manager');
    const hasWorker = agents.some(agent => agent.role === 'Worker');

    if (config.workflowType === 'GroupChat' 
        && config.orchestrationMode === 'Manager'
        && !hasManager) {
      Modal.warning({
        title: '缺少协调者',
        content: (
          <div>
            <p>协调者模式需要一个Manager角色的Agent来引导讨论。</p>
            <p style={{ marginTop: 16 }}>请在团队中添加一个Manager角色的Agent。</p>
          </div>
        ),
        okText: '去添加',
        onOk: () => {
          setSelectedCollaboration(collaboration);
          addAgentForm.resetFields();
          setSelectedAgentId(null);
          setAddAgentModalVisible(true);
        }
      });
      return;
    }

    if (!hasWorker) {
      Modal.warning({
        title: '缺少执行者',
        content: (
          <div>
            <p>执行任务需要至少一个Worker角色的Agent。</p>
            <p style={{ marginTop: 16 }}>请在团队中添加Worker角色的Agent。</p>
          </div>
        ),
        okText: '去添加',
        onOk: () => {
          setSelectedCollaboration(collaboration);
          addAgentForm.resetFields();
          setSelectedAgentId(null);
          setAddAgentModalVisible(true);
        }
      });
      return;
    }

    setSelectedTask(task);
    setSelectedCollaboration(collaboration);
    setExecutionMessages([]);
    setExecutionModalVisible(true);
    setIsExecuting(true);
    
    executeTaskWithConfig(task, collaboration, config);
  };

  const executeTaskWithConfig = async (task: CollaborationTaskItem, collaboration: Collaboration, config: Record<string, unknown>) => {
    const input = task.description || task.title;
    const token = localStorage.getItem('token');
    
    try {
      const workflowType = config.workflowType || 'GroupChat';
      let url: string;
      let body: Record<string, unknown>;

      if (workflowType === 'Magentic' || workflowType === 'ReviewIterative') {
        url = getApiUrl(`/collaborationworkflow/${task.collaborationId}/review-iterative`);
        body = {
          input,
          parameters: {
            maxIterations: config.maxIterations
          }
        };
      } else {
        url = getApiUrl(`/collaborationworkflow/${task.collaborationId}/groupchat`);
        body = {
          input,
          parameters: {
            orchestrationMode: config.orchestrationMode,
            maxIterations: config.maxIterations
          },
          taskId: task.id
        };
      }
      
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(body)
      });
      
      if (!response.ok) {
        const errorData = await response.json();
        message.error(errorData.error || '执行失败');
        setIsExecuting(false);
        return;
      }
      
      const reader = response.body?.getReader();
      const decoder = new TextDecoder();
      
      if (!reader) {
        message.error('无法读取响应流');
        setIsExecuting(false);
        return;
      }
      
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        
        const chunk = decoder.decode(value, { stream: true });
        const lines = chunk.split('\n');
        
        for (const line of lines) {
          if (line.startsWith('data: ')) {
            try {
              const data = JSON.parse(line.slice(6));
              if (data.sender && data.content) {
                setExecutionMessages(prev => [...prev, {
                  sender: data.sender,
                  content: data.content,
                  timestamp: data.timestamp || new Date().toISOString(),
                  role: data.role || 'assistant',
                  metadata: data.metadata
                }]);
              }
            } catch {}
          }
        }
      }
      
      message.success('任务执行完成！');
    } catch (error) {
      message.error('任务执行失败');
    } finally {
      setIsExecuting(false);
    }
  };

  const handleViewChatHistory = (task: CollaborationTaskItem) => {
    setSelectedTask(task);
    setChatHistoryModalVisible(true);
  };

  const handleEditAgent = (collaboration: Collaboration, agent: CollaborationAgent) => {
    setSelectedCollaboration(collaboration);
    setEditingAgent(agent);
    editAgentForm.setFieldsValue({
      role: agent.role || 'Worker',
      customPrompt: agent.customPrompt || agent.systemPrompt || '',
    });
    setEditAgentModalVisible(true);
  };

  const handleSubmitEditAgent = async () => {
    try {
      const values = await editAgentForm.validateFields();
      if (selectedCollaboration && editingAgent) {
        await collaborationService.updateAgentRole(
          Number(selectedCollaboration.id),
          editingAgent.agentId,
          values
        );
        message.success('更新成功');
        setEditAgentModalVisible(false);
        await refreshCollaboration(selectedCollaboration.id);
      }
    } catch (error) {
      message.error('更新失败');
    }
  };

  const columns = [
    {
      title: '团队名称',
      dataIndex: 'name',
      key: 'name',
      width: '15%',
      ellipsis: true,
    },
    {
      title: '团队工作目录',
      dataIndex: 'path',
      key: 'path',
      width: '15%',
      ellipsis: true,
      render: (path: string) => path || '-',
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: '20%',
      ellipsis: true,
    },
    {
      title: '智能体',
      dataIndex: 'agents',
      key: 'agents',
      width: '8%',
      render: (agents: CollaborationAgent[]) => agents.length,
    },
    {
      title: '任务',
      dataIndex: 'tasks',
      key: 'tasks',
      width: '8%',
      render: (tasks: CollaborationTask[]) => tasks.length,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: '8%',
      render: (status: string) => {
        const colorMap: Record<string, string> = {
          Active: 'green',
          Paused: 'orange',
          Completed: 'blue',
          Cancelled: 'red',
        };
        return <Tag color={colorMap[status]}>{status}</Tag>;
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: '12%',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: '14%',
      render: (_: unknown, record: Collaboration) => (
        <Space size="small" wrap>
          <Button
            type="link"
            size="small"
            icon={<EyeOutlined />}
            onClick={() => navigate(`/collaborations/${record.id}`)}
          >
            详情
          </Button>
          <Button
            type="link"
            size="small"
            icon={<EditOutlined />}
            onClick={() => {
              setSelectedCollaboration(record);
              setModalVisible(true);
              
              // 解析配置 JSON 字符串
              let formValues: Record<string, unknown> = { ...record };
              if (record.config) {
                try {
                  const config = JSON.parse(record.config);
                  formValues.smtp = config.smtp || {};
                } catch (e) {
                  console.error('解析配置失败:', e);
                }
              }
              form.setFieldsValue(formValues);
            }}
          >
            编辑
          </Button>
          <Popconfirm
            title="确定要删除这个团队吗？"
            onConfirm={() => handleDeleteCollaboration(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" size="small" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const expandedRowRender = useCallback((record: Collaboration) => {
    return (
      <Tabs
        defaultActiveKey="agents"
        items={[
          {
            key: 'agents',
            label: '智能体',
            children: (
              <div>
                <Button
                  type="primary"
                  icon={<TeamOutlined />}
                  onClick={() => handleAddAgent(record)}
                  style={{ marginBottom: 16 }}
                >
                  添加智能体
                </Button>
                <Table
                  dataSource={record.agents}
                  columns={[
                    {
                      title: '智能体名称',
                      dataIndex: 'agentName',
                      key: 'agentName',
                      width: 150,
                    },
                    {
                      title: '工作流角色',
                      dataIndex: 'role',
                      key: 'role',
                      width: 180,
                      render: (role: string) => {
                        const isManager = role === 'Manager';
                        return (
                          <Tag color={isManager ? 'blue' : 'green'}>
                            {isManager ? 'Manager（协调者）' : 'Worker（执行者）'}
                          </Tag>
                        );
                      },
                    },
                    {
                      title: '自定义提示词',
                      dataIndex: 'customPrompt',
                      key: 'customPrompt',
                      width: 300,
                      render: (customPrompt: string, agentRecord: CollaborationAgent) => {
                        const displayText = customPrompt || agentRecord.systemPrompt || '-';
                        const isFromSystemPrompt = !customPrompt && agentRecord.systemPrompt;
                        const truncatedText = displayText.length > 50 ? `${displayText.substring(0, 50)}...` : displayText;
                        
                        return (
                          <Tooltip title={displayText} placement="topLeft">
                            <div>
                              <div style={{ fontSize: 12, cursor: 'pointer' }}>
                                {truncatedText}
                              </div>
                              {isFromSystemPrompt && (
                                <div style={{ fontSize: 10, color: '#999' }}>
                                  (来自系统提示词)
                                </div>
                              )}
                            </div>
                          </Tooltip>
                        );
                      },
                    },
                    {
                      title: '类型',
                      dataIndex: 'agentType',
                      key: 'agentType',
                      width: 120,
                    },
                    {
                      title: '状态',
                      dataIndex: 'agentStatus',
                      key: 'agentStatus',
                      width: 100,
                      render: (status: string) => {
                        const colorMap: Record<string, string> = {
                          Active: 'green',
                          Inactive: 'default',
                          Busy: 'orange',
                          Error: 'red',
                        };
                        return <Tag color={colorMap[status] || 'default'}>{status || 'Inactive'}</Tag>;
                      },
                    },
                    {
                      title: '加入时间',
                      dataIndex: 'joinedAt',
                      key: 'joinedAt',
                      width: 160,
                      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
                    },
                    {
                      title: '操作',
                      key: 'action',
                      width: 150,
                      render: (_: unknown, agentRecord: CollaborationAgent) => (
                        <Space>
                          <Button 
                            type="link" 
                            icon={<EditOutlined />}
                            onClick={() => handleEditAgent(record, agentRecord)}
                          >
                            编辑
                          </Button>
                          <Popconfirm
                            title="确定要移除这个智能体吗？"
                            onConfirm={() => handleRemoveAgent(record.id, agentRecord.agentId)}
                            okText="确定"
                            cancelText="取消"
                          >
                            <Button type="link" danger icon={<DeleteOutlined />}>
                              移除
                            </Button>
                          </Popconfirm>
                        </Space>
                      ),
                    },
                  ]}
                  rowKey="agentId"
                  pagination={false}
                />
              </div>
            ),
          },
          {
            key: 'tasks',
            label: '任务',
            children: (
              <CollaborationTasks
                collaborationId={record.id}
                tasks={record.tasks}
                agents={record.agents}
                onCreate={() => handleCreateTask(record)}
                onExecute={handleExecuteTask}
                onEdit={handleEditTask}
                onViewHistory={handleViewChatHistory}
                onDelete={handleDeleteTask}
                onRefresh={() => refreshCollaboration(record.id)}
              />
            ),
          },
        ]}
      />
    );
  }, [
    handleAddAgent,
    handleRemoveAgent,
    handleEditAgent,
    handleCreateTask,
    handleExecuteTask,
    handleEditTask,
    handleViewChatHistory,
    handleDeleteTask,
    refreshCollaboration,
  ]);

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>团队管理</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          创建团队
        </Button>
      </div>

      <Table
        dataSource={collaborations}
        columns={columns}
        rowKey="id"
        loading={loading}
        expandable={{
          expandedRowRender,
          expandRowByClick: true,
          onExpand: async (expanded, record) => {
            if (expanded && (!record.agents || record.agents.length === 0)) {
              await loadCollaborationData(record.id);
            }
          },
        }}
      />

      <Modal
        title={selectedCollaboration ? '编辑团队' : '创建团队'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => {
          setModalVisible(false);
          setSelectedCollaboration(null);
          form.resetFields();
        }}
        width={700}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label="团队名称"
            name="name"
            rules={[{ required: true, message: '请输入团队名称' }]}
          >
            <Input placeholder="请输入团队名称" />
          </Form.Item>
          <Form.Item 
            label="团队工作目录" 
            name="path"
            tooltip="团队的代码目录路径，用于智能体执行任务"
          >
            <Input 
              placeholder="例如: /home/user/projects/myapp" 
              prefix={<FolderOutlined />}
            />
          </Form.Item>
          <Form.Item label="描述" name="description">
            <Input.TextArea rows={3} placeholder="请输入描述" />
          </Form.Item>
          
          <Collapse 
            style={{ marginBottom: 16 }}
            items={[
              {
                key: '1',
                label: (
                  <Space>
                    <MailOutlined />
                    <span>SMTP邮件配置</span>
                  </Space>
                ),
                children: (
                  <>
                    <Alert
                      message="配置SMTP后，智能体可以使用简化的邮件发送功能"
                      description="配置后，智能体可以直接调用 SendEmail(toEmail, subject, body) 发送邮件，无需每次传递SMTP参数。"
                      type="info"
                      showIcon
                      style={{ marginBottom: 16 }}
                    />
                    <Row gutter={16}>
                      <Col span={12}>
                        <Form.Item 
                          label="SMTP服务器" 
                          name={['smtp', 'server']}
                          tooltip="例如: smtp.qq.com"
                        >
                          <Input placeholder="smtp.qq.com" />
                        </Form.Item>
                      </Col>
                      <Col span={12}>
                        <Form.Item 
                          label="端口" 
                          name={['smtp', 'port']}
                          tooltip="SSL加密通常使用465或587端口"
                        >
                          <InputNumber 
                            placeholder="587" 
                            style={{ width: '100%' }}
                            min={1}
                            max={65535}
                          />
                        </Form.Item>
                      </Col>
                    </Row>
                    <Row gutter={16}>
                      <Col span={12}>
                        <Form.Item 
                          label="用户名" 
                          name={['smtp', 'username']}
                          tooltip="邮箱地址"
                        >
                          <Input placeholder="your@email.com" />
                        </Form.Item>
                      </Col>
                      <Col span={12}>
                        <Form.Item 
                          label="密码/授权码" 
                          name={['smtp', 'password']}
                          tooltip="邮箱密码或授权码"
                        >
                          <Input.Password placeholder="授权码" />
                        </Form.Item>
                      </Col>
                    </Row>
                    <Row gutter={16}>
                      <Col span={12}>
                        <Form.Item 
                          label="发件人邮箱" 
                          name={['smtp', 'fromEmail']}
                          tooltip="通常与用户名相同"
                        >
                          <Input placeholder="your@email.com" />
                        </Form.Item>
                      </Col>
                      <Col span={12}>
                        <Form.Item 
                          label="启用SSL" 
                          name={['smtp', 'enableSsl']}
                          valuePropName="checked"
                          tooltip="建议启用SSL加密"
                          initialValue={true}
                        >
                          <Switch />
                        </Form.Item>
                      </Col>
                    </Row>
                    <Row>
                      <Col span={24}>
                        <Button 
                          type="dashed" 
                          icon={<MailOutlined />}
                          onClick={async () => {
                            try {
                              const values = await form.validateFields();
                              
                              if (!values.smtp || !values.smtp.server) {
                                message.warning('请先填写SMTP配置');
                                return;
                              }
                              
                              const config = JSON.stringify({ smtp: values.smtp });
                              
                              message.loading({ content: '正在发送测试邮件...', key: 'testEmail' });
                              
                              const token = localStorage.getItem('token');
                              const response = await fetch(`/api/collaborations/test-email`, {
                                method: 'POST',
                                headers: {
                                  'Content-Type': 'application/json',
                                  'Authorization': `Bearer ${token}`
                                },
                                body: JSON.stringify({ smtp: values.smtp })
                              });
                              
                              if (!response.ok) {
                                const errorText = await response.text();
                                throw new Error(errorText || `HTTP ${response.status}: ${response.statusText}`);
                              }
                              
                              const result = await response.json();
                              
                              if (result.success) {
                                message.success({ content: result.message, key: 'testEmail' });
                              } else {
                                message.error({ content: result.message, key: 'testEmail' });
                              }
                            } catch (error: unknown) {
                              message.error({ content: getErrorMessage(error, '测试邮件发送失败'), key: 'testEmail' });
                            }
                          }}
                        >
                          发送测试邮件
                        </Button>
                      </Col>
                    </Row>
                  </>
                ),
              },
            ]}
          />
        </Form>
      </Modal>

      <Modal
        title="添加智能体"
        open={addAgentModalVisible}
        onOk={handleSubmitAddAgent}
        onCancel={() => setAddAgentModalVisible(false)}
        width={700}
      >
        <Form form={addAgentForm} layout="vertical">
          <Form.Item
            label="智能体"
            name="agentId"
            rules={[{ required: true, message: '请选择智能体' }]}
          >
            <Select 
              placeholder="请选择智能体"
              onChange={handleAgentSelect}
              showSearch
              filterOption={(input, option) =>
                (option?.children as unknown as string)?.toLowerCase().includes(input.toLowerCase())
              }
            >
              {agents
                .filter(agent => {
                  // 过滤掉已经在团队中的智能体
                  const isInCollaboration = selectedCollaboration?.agents?.some(
                    (ca: { agentId: number }) => ca.agentId === agent.id
                  );
                  return !isInCollaboration;
                })
                .map(agent => (
                  <Select.Option key={agent.id} value={agent.id}>
                    {agent.name} ({agent.type})
                  </Select.Option>
                ))
              }
            </Select>
          </Form.Item>

          {agents.filter(agent => {
            const isInCollaboration = selectedCollaboration?.agents?.some(
              (ca: { agentId: number }) => ca.agentId === agent.id
            );
            return !isInCollaboration;
          }).length === 0 && (
            <Alert
              message="所有智能体都已在团队中"
              description="该团队已经添加了所有可用的智能体，请先创建新的智能体或移除现有智能体。"
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}

          {selectedAgentId && (
            <>
              {(() => {
                const selectedAgent = agents.find(a => a.id === selectedAgentId);
                if (!selectedAgent) return null;
                
                return (
                    <Alert
                      message="智能体信息"
                      description={
                        <div>
                          <p><strong>类型：</strong>{selectedAgent.type}</p>
                          <p><strong>状态：</strong>
                            <Tag color={
                              selectedAgent.status === 'Active' ? 'green' :
                              selectedAgent.status === 'Busy' ? 'orange' :
                              selectedAgent.status === 'Error' ? 'red' : 'default'
                            }>
                              {selectedAgent.status || 'Inactive'}
                            </Tag>
                          </p>
                          {selectedAgent.systemPrompt && (
                            <div>
                              <strong>系统提示词：</strong>
                              <div style={{ 
                                maxHeight: 100, 
                                overflow: 'auto', 
                                backgroundColor: '#f5f5f5', 
                                padding: 8, 
                                borderRadius: 4,
                                marginTop: 8,
                                fontSize: 12,
                                whiteSpace: 'pre-wrap'
                              }}>
                                {selectedAgent.systemPrompt}
                              </div>
                              <div style={{ marginTop: 8, color: '#1890ff', fontSize: 12 }}>
                                ℹ️ 系统提示词已自动填充到下方"专业描述"字段，您可以根据项目需求修改
                              </div>
                            </div>
                          )}
                        </div>
                      }
                      type="info"
                      showIcon
                      style={{ marginBottom: 16 }}
                    />
                  );
              })()}
            </>
          )}

          <Form.Item
            label="工作流角色"
            name="role"
            rules={[{ required: true, message: '请选择角色' }]}
            initialValue="Worker"
            tooltip="Manager负责协调Worker Agents（仅GroupChat模式需要），Worker负责执行具体任务。Magentic模式会自动创建协调者，所有成员都是Worker。"
          >
            <Select placeholder="请选择角色">
              <Select.Option value="Worker">
                <Space>
                  <UserOutlined />
                  <span>Worker（执行者）- 推荐</span>
                </Space>
              </Select.Option>
              <Select.Option value="Manager">
                <Space>
                  <TeamOutlined />
                  <span>Manager（协调者）- 仅GroupChat模式</span>
                </Space>
              </Select.Option>
            </Select>
          </Form.Item>

          <Alert
            message="角色说明"
            description={
              <div>
                <p><strong>Worker（执行者）：</strong>执行具体业务任务，适用于所有工作流模式</p>
                <p><strong>Manager（协调者）：</strong>仅用于GroupChat模式，负责协调Worker Agents</p>
                <p style={{ color: '#1890ff', marginTop: 8 }}>
                  <InfoCircleOutlined style={{ marginRight: 4 }} />
                  Magentic模式会自动创建独立的协调者（MagenticManager），所有成员都作为Worker参与
                </p>
              </div>
            }
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />

          <div style={{ marginBottom: 16 }}>
            <div style={{ marginBottom: 4, fontWeight: 500 }}>
              自定义提示词
              <Tooltip title="自动填充系统提示词，您可以根据项目需求修改此提示词">
                <InfoCircleOutlined style={{ marginLeft: 4, color: '#999', fontSize: 12 }} />
              </Tooltip>
            </div>
            <div style={{ marginBottom: 8, padding: 8, backgroundColor: '#e6f7ff', borderRadius: 4, fontSize: 12 }}>
              <div style={{ marginBottom: 4, fontWeight: 500, color: '#1890ff' }}>
                <InfoCircleOutlined style={{ marginRight: 4 }} />提示词变量说明
              </div>
              <div style={{ marginTop: 8 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_name}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体名称</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_role}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体角色（Manager/Worker）</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_type}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体类型（如：架构师）</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{members}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>团队成员列表（名称+类型）</span>
              </div>
            </div>
            <Form.Item name="customPrompt" noStyle>
              <Input.TextArea 
                rows={6} 
                placeholder="自动填充系统提示词，您可以根据项目需求修改..." 
                showCount
                maxLength={2000}
              />
            </Form.Item>
          </div>
        </Form>
      </Modal>

      <Modal
        title="创建任务"
        open={createTaskModalVisible}
        onOk={handleSubmitCreateTask}
        onCancel={handleCloseCreateTask}
        destroyOnClose
        width={800}
      >
        <Form form={createTaskForm} layout="vertical">
          <Form.Item
            label="标题"
            name="title"
            rules={[{ required: true, message: '请输入任务标题' }]}
          >
            <Input placeholder="请输入任务标题" />
          </Form.Item>
          
          <Form.Item label="描述" name="description">
            <Input.TextArea rows={2} placeholder="请输入任务描述" />
          </Form.Item>
          
          <Form.Item 
            label="任务提示词" 
            name="prompt"
            tooltip="任务的具体要求和目标，Agent会根据此提示词执行任务"
          >
            <Input.TextArea 
              rows={4} 
              placeholder="请输入任务提示词，详细描述任务要求和目标" 
              showCount
              maxLength={2000}
            />
          </Form.Item>
          
          <Form.Item 
            label={<span><TeamOutlined style={{ color: '#1890ff', marginRight: 4 }} />队员（Worker类型）</span>}
            required
            tooltip="至少选择一个Worker类型的Agent"
            validateStatus={selectedTaskAgents.length === 0 ? 'error' : ''}
            help={selectedTaskAgents.length === 0 ? '请至少选择一个Worker' : ''}
          >
            <Transfer
              dataSource={selectedCollaboration?.agents?.filter(agent => agent.role === 'Worker').map(agent => ({
                key: agent.agentId.toString(),
                title: agent.agentName,
              })) || []}
              titles={['可选Worker', '已选Worker']}
              targetKeys={selectedTaskAgents}
              onChange={(targetKeys) => {
                setSelectedTaskAgents(targetKeys as string[]);
              }}
              render={item => item.title}
              listStyle={{ width: 280, height: 180 }}
              selectAllLabels={['全选', '全选']}
            />
          </Form.Item>
          
          <Collapse 
            defaultActiveKey={['execution']} 
            style={{ marginBottom: 16 }}
            items={[
              {
                key: 'execution',
                label: '执行配置',
                children: (
                  <>
                    <Form.Item label="工作流类型">
                      <Radio.Group 
                        value={taskWorkflowType} 
                        onChange={(e) => {
                          setTaskWorkflowType(e.target.value);
                          setTaskOrchestrationMode('Manager');
                        }}
                      >
                        <Space direction="vertical">
                          <Radio value="GroupChat">
                            <Space>
                              <TeamOutlined style={{ color: '#1890ff' }} />
                              <span>群聊协作</span>
                              <Text type="secondary" style={{ fontSize: 12 }}>协调者引导Worker协作讨论</Text>
                            </Space>
                          </Radio>
                          <Radio value="Magentic">
                            <Space>
                              <BulbOutlined style={{ color: '#722ed1' }} />
                              <span>Magentic智能工作流</span>
                              <Tooltip title={
                                <div>
                                  <p><strong>框架将使用所选的协调者，负责：</strong></p>
                                  <ul style={{ marginTop: 8, marginBottom: 8, paddingLeft: 20 }}>
                                    <li><strong>规划：</strong>分析任务，制定执行计划，维护任务账本</li>
                                    <li><strong>分派：</strong>根据当前进度决定由哪个Worker执行下一步</li>
                                    <li><strong>反思：</strong>检查Worker的产出是否达标，维护进度账本</li>
                                  </ul>
                                  <p style={{ marginTop: 8, color: '#1890ff' }}>所有团队成员都作为Worker参与执行，不需要指定Manager Agent</p>
                                  <p style={{ marginTop: 8, color: '#722ed1' }}>流程编排、参数设置请在任务列表的「流程编排」按钮中配置</p>
                                </div>
                              }>
                                <QuestionCircleOutlined style={{ color: '#1890ff', cursor: 'pointer' }} />
                              </Tooltip>
                            </Space>
                          </Radio>
                        </Space>
                      </Radio.Group>
                    </Form.Item>

                    <Form.Item 
                      label={<span><CrownOutlined style={{ color: '#faad14', marginRight: 4 }} />协调者（Manager类型）</span>}
                      required
                    >
                      <Row gutter={16}>
                        <Col span={12}>
                          <Select
                            placeholder="请选择协调者（Manager类型）"
                            value={taskManagerAgentId}
                            onChange={(value) => setTaskManagerAgentId(value)}
                            style={{ width: '100%' }}
                          >
                            {selectedCollaboration?.agents?.filter(agent => agent.role === 'Manager').map(agent => (
                              <Option key={agent.agentId} value={agent.agentId}>
                                <Space>
                                  <CrownOutlined style={{ color: '#faad14' }} />
                                  {agent.agentName}
                                  <Tag color="gold">Manager</Tag>
                                </Space>
                              </Option>
                            ))}
                          </Select>
                        </Col>
                        <Col span={12}>
                          <Space>
                            <Text>最大迭代次数：</Text>
                            <InputNumber 
                              min={1} 
                              max={50} 
                              value={taskMaxIterations}
                              onChange={(value) => setTaskMaxIterations(value || 10)}
                              style={{ width: 100 }}
                            />
                            <Tooltip title="Agent发言的最大轮次，超过此轮次后工作流将自动停止">
                              <QuestionCircleOutlined style={{ color: '#ff4d4f', cursor: 'pointer' }} />
                            </Tooltip>
                          </Space>
                        </Col>
                      </Row>
                    </Form.Item>
                    
                    <Form.Item 
                      label={
                        <Space>
                          <span>自定义协调者提示词</span>
                          <Tooltip title={
                            <div>
                              <p><strong>协调者的提示词将影响整个工作流的流转逻辑，请仔细编写！</strong></p>
                              <p style={{ marginTop: 8 }}>协调者负责：</p>
                              <ul style={{ marginTop: 4, marginBottom: 4, paddingLeft: 20 }}>
                                <li>分析当前讨论内容</li>
                                <li>决定下一个发言的Agent</li>
                                <li>确保讨论不偏离主题</li>
                                <li>引导工作流顺利完成</li>
                              </ul>
                              <p style={{ marginTop: 8, color: '#ff4d4f' }}>⚠️ 此字段为必填项，不能为空！</p>
                            </div>
                          }>
                            <QuestionCircleOutlined style={{ color: '#ff4d4f', cursor: 'pointer' }} />
                          </Tooltip>
                        </Space>
                      }
                      required
                      rules={[{ required: true, message: '请输入自定义协调者提示词' }]}
                    >
                      <Input.TextArea
                        rows={8}
                        placeholder={`自定义协调者提示词，将覆盖协调者Agent的默认提示词。

示例：
你是一个群聊协调者，负责引导讨论。

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论不偏离主题
4. 引导工作流顺利完成

注意事项：
- 根据任务需求和Agent专长选择合适的发言者
- 确保讨论有序进行，避免重复发言
- 在合适的时机总结讨论成果`}
                        value={taskManagerCustomPrompt}
                        onChange={(e) => setTaskManagerCustomPrompt(e.target.value)}
                      />
                    </Form.Item>

                    {taskWorkflowType === 'Magentic' && (
                      <Alert
                        message="Magentic智能工作流的流程编排、阈值标准、最大尝试次数等参数请在任务列表的「流程编排」按钮中配置"
                        type="info"
                        showIcon
                        style={{ marginBottom: 16 }}
                      />
                    )}
                  </>
                )
              }
            ]}
          />
          
          <Collapse 
            defaultActiveKey={[]} 
            style={{ marginBottom: 16 }}
            items={[
              {
                key: 'git',
                label: 'Git配置（可选）',
                children: (
                  <>
                    <Row gutter={16}>
                      <Col span={16}>
                        <Form.Item 
                          label="Git仓库地址" 
                          name="gitUrl"
                          tooltip="Git仓库的完整URL，例如：https://github.com/user/repo.git"
                        >
                          <Input placeholder="https://github.com/user/repo.git" />
                        </Form.Item>
                      </Col>
                      <Col span={8}>
                        <Form.Item 
                          label="目标分支" 
                          name="gitBranch"
                          tooltip="任务执行时使用的Git分支"
                        >
                          <Input placeholder="main" />
                        </Form.Item>
                      </Col>
                    </Row>
                    
                    <Form.Item 
                      label="Git访问令牌" 
                      name="gitToken"
                      tooltip="私有仓库需要提供访问令牌，公开仓库可留空"
                    >
                      <Input placeholder="ghp_xxxxxxxxxxxx" />
                    </Form.Item>
                  </>
                )
              }
            ]}
          />
        </Form>
      </Modal>

      <Modal
        title="编辑任务"
        open={editTaskModalVisible}
        onOk={handleSubmitEditTask}
        onCancel={handleCloseEditTask}
        destroyOnClose
        width={800}
      >
        <Form form={editTaskForm} layout="vertical">
          <Form.Item
            label="标题"
            name="title"
            rules={[{ required: true, message: '请输入任务标题' }]}
          >
            <Input placeholder="请输入任务标题" />
          </Form.Item>
          
          <Form.Item label="描述" name="description">
            <Input.TextArea rows={2} placeholder="请输入任务描述" />
          </Form.Item>
          
          <Form.Item 
            label="任务提示词" 
            name="prompt"
            tooltip="任务的具体要求和目标，Agent会根据此提示词执行任务"
          >
            <Input.TextArea 
              rows={4} 
              placeholder="请输入任务提示词，详细描述任务要求和目标" 
              showCount
              maxLength={2000}
            />
          </Form.Item>
          
          <Form.Item 
            label={<span><TeamOutlined style={{ color: '#1890ff', marginRight: 4 }} />队员（Worker类型）</span>}
            required
            tooltip="至少选择一个Worker类型的Agent"
            validateStatus={editTaskAgents.length === 0 ? 'error' : ''}
            help={editTaskAgents.length === 0 ? '请至少选择一个Worker' : ''}
          >
            <Transfer
              dataSource={selectedCollaboration?.agents?.filter(agent => agent.role === 'Worker').map(agent => ({
                key: agent.agentId.toString(),
                title: agent.agentName,
              })) || []}
              titles={['可选Worker', '已选Worker']}
              targetKeys={editTaskAgents}
              onChange={(targetKeys) => {
                setEditTaskAgents(targetKeys as string[]);
              }}
              render={item => item.title}
              listStyle={{ width: 280, height: 180 }}
              selectAllLabels={['全选', '全选']}
            />
          </Form.Item>
          
          <Collapse 
            defaultActiveKey={['execution']} 
            style={{ marginBottom: 16 }}
            items={[
              {
                key: 'execution',
                label: '执行配置',
                children: (
                  <>
                    <Form.Item label="工作流类型">
                      <Radio.Group 
                        value={taskWorkflowType} 
                        onChange={(e) => {
                          setTaskWorkflowType(e.target.value);
                          setTaskOrchestrationMode('Manager');
                        }}
                      >
                        <Space direction="vertical">
                          <Radio value="GroupChat">
                            <Space>
                              <TeamOutlined style={{ color: '#1890ff' }} />
                              <span>群聊协作</span>
                              <Text type="secondary" style={{ fontSize: 12 }}>协调者引导Worker协作讨论</Text>
                            </Space>
                          </Radio>
                          <Radio value="Magentic">
                            <Space>
                              <BulbOutlined style={{ color: '#722ed1' }} />
                              <span>Magentic智能工作流</span>
                              <Tooltip title={
                                <div>
                                  <p><strong>框架将使用所选的协调者，负责：</strong></p>
                                  <ul style={{ marginTop: 8, marginBottom: 8, paddingLeft: 20 }}>
                                    <li><strong>规划：</strong>分析任务，制定执行计划，维护任务账本</li>
                                    <li><strong>分派：</strong>根据当前进度决定由哪个Worker执行下一步</li>
                                    <li><strong>反思：</strong>检查Worker的产出是否达标，维护进度账本</li>
                                  </ul>
                                  <p style={{ marginTop: 8, color: '#1890ff' }}>所有团队成员都作为Worker参与执行，不需要指定Manager Agent</p>
                                  <p style={{ marginTop: 8, color: '#722ed1' }}>流程编排、参数设置请在任务列表的「流程编排」按钮中配置</p>
                                </div>
                              }>
                                <QuestionCircleOutlined style={{ color: '#1890ff', cursor: 'pointer' }} />
                              </Tooltip>
                            </Space>
                          </Radio>
                        </Space>
                      </Radio.Group>
                    </Form.Item>

                    <Form.Item 
                      label={<span><CrownOutlined style={{ color: '#faad14', marginRight: 4 }} />协调者（Manager类型）</span>}
                      required
                    >
                      <Row gutter={16}>
                        <Col span={12}>
                          <Select
                            placeholder="请选择协调者（Manager类型）"
                            value={taskManagerAgentId}
                            onChange={(value) => setTaskManagerAgentId(value)}
                            style={{ width: '100%' }}
                          >
                            {selectedCollaboration?.agents?.filter(agent => agent.role === 'Manager').map(agent => (
                              <Option key={agent.agentId} value={agent.agentId}>
                                <Space>
                                  <CrownOutlined style={{ color: '#faad14' }} />
                                  {agent.agentName}
                                  <Tag color="gold">Manager</Tag>
                                </Space>
                              </Option>
                            ))}
                          </Select>
                        </Col>
                        <Col span={12}>
                          <Space>
                            <Text>最大迭代次数：</Text>
                            <InputNumber 
                              min={1} 
                              max={50} 
                              value={taskMaxIterations}
                              onChange={(value) => setTaskMaxIterations(value || 10)}
                              style={{ width: 100 }}
                            />
                            <Tooltip title="Agent发言的最大轮次，超过此轮次后工作流将自动停止">
                              <QuestionCircleOutlined style={{ color: '#ff4d4f', cursor: 'pointer' }} />
                            </Tooltip>
                          </Space>
                        </Col>
                      </Row>
                    </Form.Item>
                    
                    <Form.Item 
                      label={
                        <Space>
                          <span>自定义协调者提示词</span>
                          <Tooltip title={
                            <div>
                              <p><strong>协调者的提示词将影响整个工作流的流转逻辑，请仔细编写！</strong></p>
                              <p style={{ marginTop: 8 }}>协调者负责：</p>
                              <ul style={{ marginTop: 4, marginBottom: 4, paddingLeft: 20 }}>
                                <li>分析当前讨论内容</li>
                                <li>决定下一个发言的Agent</li>
                                <li>确保讨论不偏离主题</li>
                                <li>引导工作流顺利完成</li>
                              </ul>
                              <p style={{ marginTop: 8, color: '#ff4d4f' }}>⚠️ 此字段为必填项，不能为空！</p>
                            </div>
                          }>
                            <QuestionCircleOutlined style={{ color: '#ff4d4f', cursor: 'pointer' }} />
                          </Tooltip>
                        </Space>
                      }
                      required
                      rules={[{ required: true, message: '请输入自定义协调者提示词' }]}
                    >
                      <Input.TextArea
                        rows={8}
                        placeholder={`自定义协调者提示词，将覆盖协调者Agent的默认提示词。

示例：
你是一个群聊协调者，负责引导讨论。

你的职责：
1. 分析当前讨论内容
2. 决定下一个发言的Agent
3. 确保讨论不偏离主题
4. 引导工作流顺利完成

注意事项：
- 根据任务需求和Agent专长选择合适的发言者
- 确保讨论有序进行，避免重复发言
- 在合适的时机总结讨论成果`}
                        value={taskManagerCustomPrompt}
                        onChange={(e) => setTaskManagerCustomPrompt(e.target.value)}
                      />
                    </Form.Item>

                    {taskWorkflowType === 'Magentic' && (
                      <Alert
                        message="Magentic智能工作流的流程编排、阈值标准、最大尝试次数等参数请在任务列表的「流程编排」按钮中配置"
                        type="info"
                        showIcon
                        style={{ marginBottom: 16 }}
                      />
                    )}
                  </>
                )
              }
            ]}
          />
          
          <Collapse 
            defaultActiveKey={[]} 
            style={{ marginBottom: 16 }}
            items={[
              {
                key: 'git',
                label: 'Git配置（可选）',
                children: (
                  <>
                    <Row gutter={16}>
                      <Col span={16}>
                        <Form.Item 
                          label="Git仓库地址" 
                          name="gitUrl"
                          tooltip="Git仓库的完整URL，例如：https://github.com/user/repo.git"
                        >
                          <Input placeholder="https://github.com/user/repo.git" />
                        </Form.Item>
                      </Col>
                      <Col span={8}>
                        <Form.Item 
                          label="目标分支" 
                          name="gitBranch"
                          tooltip="任务执行时使用的Git分支"
                        >
                          <Input placeholder="main" />
                        </Form.Item>
                      </Col>
                    </Row>
                    
                    <Form.Item 
                      label="Git访问令牌" 
                      name="gitToken"
                      tooltip="私有仓库需要提供访问令牌，公开仓库可留空"
                    >
                      <Input placeholder="ghp_xxxxxxxxxxxx" />
                    </Form.Item>
                  </>
                )
              }
            ]}
          />
        </Form>
      </Modal>

      <Modal
        title="编辑智能体角色"
        open={editAgentModalVisible}
        onOk={handleSubmitEditAgent}
        onCancel={() => setEditAgentModalVisible(false)}
        width={700}
      >
        <Form form={editAgentForm} layout="vertical">
          {editingAgent && (
            <Alert
              message={`编辑智能体：${editingAgent.agentName}`}
              description={
                <div>
                  <p><strong>类型：</strong>{editingAgent.agentType}</p>
                  <p><strong>状态：</strong>
                    <Tag color={
                      editingAgent.agentStatus === 'Active' ? 'green' :
                      editingAgent.agentStatus === 'Busy' ? 'orange' :
                      editingAgent.agentStatus === 'Error' ? 'red' : 'default'
                    }>
                      {editingAgent.agentStatus || 'Inactive'}
                    </Tag>
                  </p>
                  {editingAgent.systemPrompt && (
                    <div>
                      <strong>系统提示词：</strong>
                      <div style={{ 
                        maxHeight: 100, 
                        overflow: 'auto', 
                        backgroundColor: '#f5f5f5', 
                        padding: 8, 
                        borderRadius: 4,
                        marginTop: 8,
                        fontSize: 12,
                        whiteSpace: 'pre-wrap'
                      }}>
                        {editingAgent.systemPrompt}
                      </div>
                    </div>
                  )}
                </div>
              }
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
          )}

          <Form.Item
            label="工作流角色"
            name="role"
            rules={[{ required: true, message: '请选择角色' }]}
            tooltip="Manager负责协调Worker Agents，Worker负责执行具体任务"
          >
            <Select placeholder="请选择角色">
              <Select.Option value="Manager">
                <Space>
                  <TeamOutlined />
                  <span>Manager（协调者）</span>
                </Space>
              </Select.Option>
              <Select.Option value="Worker">
                <Space>
                  <UserOutlined />
                  <span>Worker（执行者）</span>
                </Space>
              </Select.Option>
            </Select>
          </Form.Item>

          <div style={{ marginBottom: 16 }}>
            <div style={{ marginBottom: 4, fontWeight: 500 }}>
              自定义提示词
              <Tooltip title="自定义提示词用于覆盖系统提示词，Manager会根据自定义提示词分配任务">
                <InfoCircleOutlined style={{ marginLeft: 4, color: '#999', fontSize: 12 }} />
              </Tooltip>
            </div>
            <div style={{ marginBottom: 8, padding: 8, backgroundColor: '#e6f7ff', borderRadius: 4, fontSize: 12 }}>
              <div style={{ marginBottom: 4, fontWeight: 500, color: '#1890ff' }}>
                <InfoCircleOutlined style={{ marginRight: 4 }} />提示词变量说明
              </div>
              <div style={{ marginTop: 8 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_name}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体名称</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_role}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体角色（Manager/Worker）</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_type}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>当前智能体类型（如：架构师）</span>
              </div>
              <div style={{ marginTop: 4 }}>
                <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{members}}"}</code>
                <span style={{ marginLeft: 8, color: '#666' }}>团队成员列表（名称+类型）</span>
              </div>
            </div>
            <Form.Item name="customPrompt" noStyle>
              <Input.TextArea 
                rows={6} 
                placeholder="自定义提示词（默认使用系统提示词）" 
                showCount
                maxLength={2000}
              />
            </Form.Item>
          </div>
        </Form>
      </Modal>

      <Modal
        title="团队协作过程"
        open={chatHistoryModalVisible}
        onCancel={() => setChatHistoryModalVisible(false)}
        footer={null}
        width={800}
        destroyOnClose
      >
        {selectedTask && (
          <div>
            <Alert
              message={`任务：${selectedTask.title}`}
              description={selectedTask.description || '无描述'}
              type="info"
              showIcon
              style={{ marginBottom: 16 }}
            />
            <ChatHistory taskId={String(selectedTask.id)} />
          </div>
        )}
      </Modal>

      <Modal
        title={
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', paddingRight: 40 }}>
            <span>执行过程</span>
            <Space>
              <Text type="secondary" style={{ fontSize: 14 }}>显示系统点名提示</Text>
              <Switch
                checked={showManagerThinking}
                onChange={setShowManagerThinking}
                checkedChildren={<EyeOutlined />}
                unCheckedChildren={<EyeInvisibleOutlined />}
              />
            </Space>
          </div>
        }
        open={executionModalVisible}
        onCancel={() => {
          if (!isExecuting) {
            setExecutionModalVisible(false);
          }
        }}
        footer={isExecuting ? [
          <Button key="stop" danger onClick={() => {
            setIsExecuting(false);
            setExecutionModalVisible(false);
            message.info('任务已停止');
          }}>
            停止执行
          </Button>
        ] : [
          <Button key="close" onClick={() => setExecutionModalVisible(false)}>
            关闭
          </Button>
        ]}
        width={900}
      >
        <div style={{ maxHeight: 600, overflowY: 'auto' }}>
          {executionMessages.length === 0 && isExecuting && (
            <div style={{ textAlign: 'center', padding: '50px' }}>
              <Text>正在启动任务...</Text>
            </div>
          )}
          
          {executionMessages.filter(msg => {
            if (!showManagerThinking && msg.metadata?.type === 'manager_thinking') {
              return false;
            }
            return true;
          }).map((msg, index) => {
            let agentsInfo: CollaborationAgent[] = [];
            
            if (msg.role === 'system' && msg.metadata) {
              try {
                const metadata = typeof msg.metadata === 'string' ? JSON.parse(msg.metadata) : msg.metadata;
                agentsInfo = metadata.agents || [];
              } catch (e) {
                console.error('Failed to parse metadata:', e);
              }
            }
            
            return (
              <div key={index} style={{ 
                marginBottom: 16, 
                padding: 12, 
                backgroundColor: msg.metadata?.type === 'manager_thinking' ? '#fffbe6' : '#f5f5f5', 
                borderRadius: 8,
                border: msg.metadata?.type === 'manager_thinking' ? '1px solid #ffe58f' : 'none'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
                  <Tag color={msg.role === 'system' ? 'purple' : 'blue'}>
                    {msg.sender || 'Agent'}
                  </Tag>
                  {msg.metadata?.type === 'manager_thinking' && (
                    <Tag color="gold">协调者点名</Tag>
                  )}
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {msg.timestamp ? new Date(msg.timestamp).toLocaleString('zh-CN') : ''}
                  </Text>
                </div>
                <div style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                  {msg.content}
                </div>
                {agentsInfo.length > 0 && (
                  <div style={{ marginTop: 12 }}>
                    <Text strong style={{ display: 'block', marginBottom: 8 }}>🤖 参与Agent：</Text>
                    <Space wrap>
                      {agentsInfo.map((agent: CollaborationAgent) => (
                        <Tooltip 
                          key={agent.agentId}
                          title={
                            <div>
                              <div><strong>角色：</strong>{agent.role}</div>
                              <div><strong>类型：</strong>{agent.agentType || '未设置'}</div>
                              <div><strong>模型：</strong>{agent.agentName}</div>
                              <div style={{ marginTop: 8 }}><strong>最终提示词：</strong></div>
                              <div style={{ 
                                maxWidth: 400, 
                                maxHeight: 300, 
                                overflow: 'auto',
                                whiteSpace: 'pre-wrap',
                                wordBreak: 'break-word'
                              }}>
                                {agent.customPrompt || agent.systemPrompt || ''}
                              </div>
                            </div>
                          }
                          placement="topLeft"
                          overlayStyle={{ maxWidth: 600 }}
                        >
                          <Tag 
                            color={agent.role === 'Manager' ? 'gold' : 'blue'}
                            icon={agent.role === 'Manager' ? <CrownOutlined /> : <UserOutlined />}
                            style={{ cursor: 'pointer' }}
                          >
                            {agent.agentName}
                            {agent.role === 'Manager' && ' (协调者)'}
                          </Tag>
                        </Tooltip>
                      ))}
                    </Space>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      </Modal>
    </div>
  );
};

export default Collaborations;