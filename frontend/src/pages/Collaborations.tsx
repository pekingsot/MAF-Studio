import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Tag, Space, message, Tabs, Select, Popconfirm, Divider, Row, Col, Alert, Radio, InputNumber, Typography, Card, Tooltip, Transfer } from 'antd';
import type { RadioChangeEvent } from 'antd';
import type { TransferProps } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, UserOutlined, FolderOutlined, GithubOutlined, BranchesOutlined, PlayCircleOutlined, EyeOutlined, MessageOutlined, SettingOutlined, SwapOutlined, CrownOutlined, BulbOutlined, InfoCircleOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';
import { useNavigate } from 'react-router-dom';
import ChatHistory from './collaboration-detail/ChatHistory';

const { Option } = Select;
const { Title, Text } = Typography;

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
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [addAgentModalVisible, setAddAgentModalVisible] = useState(false);
  const [createTaskModalVisible, setCreateTaskModalVisible] = useState(false);
  const [editTaskModalVisible, setEditTaskModalVisible] = useState(false);
  const [editAgentModalVisible, setEditAgentModalVisible] = useState(false);
  const [chatHistoryModalVisible, setChatHistoryModalVisible] = useState(false);
  const [executeTaskModalVisible, setExecuteTaskModalVisible] = useState(false);
  const [executionModalVisible, setExecutionModalVisible] = useState(false);
  const [executionMessages, setExecutionMessages] = useState<any[]>([]);
  const [isExecuting, setIsExecuting] = useState(false);
  const [selectedTask, setSelectedTask] = useState<any>(null);
  const [editingTask, setEditingTask] = useState<any>(null);
  const [selectedCollaboration, setSelectedCollaboration] = useState<Collaboration | null>(null);
  const [executeForm] = Form.useForm();
  const [editingAgent, setEditingAgent] = useState<any>(null);
  const [form] = Form.useForm();
  const [addAgentForm] = Form.useForm();
  const [createTaskForm] = Form.useForm();
  const [editTaskForm] = Form.useForm();
  const [editAgentForm] = Form.useForm();
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null);
  const [selectedTaskAgents, setSelectedTaskAgents] = useState<string[]>([]);
  const [editTaskAgents, setEditTaskAgents] = useState<string[]>([]);
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
      const [collaborationsData, agentsResponse] = await Promise.all([
        collaborationService.getAllCollaborations(),
        agentService.getAllAgents(),
      ]);
      setCollaborations(collaborationsData);
      setAgents(agentsResponse || []);
    } catch (error) {
      message.error('加载数据失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setSelectedCollaboration(null);
    form.resetFields();
    setModalVisible(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (selectedCollaboration) {
        // 编辑模式
        await collaborationService.updateCollaboration(selectedCollaboration.id, values);
        message.success('更新成功');
      } else {
        // 创建模式
        await collaborationService.createCollaboration(values);
        message.success('创建成功');
      }
      
      setModalVisible(false);
      setSelectedCollaboration(null);
      loadData();
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
        loadData();
      }
    } catch (error: any) {
      const errorMessage = error?.response?.data?.message || error?.message || '添加失败';
      message.error(errorMessage);
    }
  };

  const handleAgentSelect = (agentId: number) => {
    setSelectedAgentId(agentId);
    const selectedAgent = agents.find(a => a.id === agentId);
    
    // 自动填充系统提示词到自定义提示词
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
    setCreateTaskModalVisible(true);
  };

  const handleCloseCreateTask = () => {
    createTaskForm.resetFields();
    setSelectedTaskAgents([]);
    setCreateTaskModalVisible(false);
  };

  const handleSubmitCreateTask = async () => {
    try {
      const values = await createTaskForm.validateFields();
      values.agentIds = selectedTaskAgents.map(key => Number(key));
      if (selectedCollaboration) {
        await collaborationService.createTask(selectedCollaboration.id, values);
        message.success('创建成功');
        handleCloseCreateTask();
        loadData();
      }
    } catch (error) {
      message.error('创建失败');
    }
  };

  const handleDeleteCollaboration = async (id: string) => {
    try {
      await collaborationService.deleteCollaboration(id);
      message.success('删除成功');
      loadData();
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleRemoveAgent = async (collaborationId: string, agentId: number) => {
    try {
      await collaborationService.removeAgentFromCollaboration(collaborationId, agentId);
      message.success('移除成功');
      loadData();
    } catch (error) {
      message.error('移除失败');
    }
  };

  const handleEditTask = async (task: any) => {
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
      gitUrl: task.gitUrl,
      gitBranch: task.gitBranch,
      gitToken: '',
    });
    
    try {
      const agentIds = await collaborationService.getTaskAgents(task.id);
      setEditTaskAgents(agentIds.map(id => id.toString()));
    } catch (error) {
      setEditTaskAgents([]);
    }
    
    setEditTaskModalVisible(true);
  };

  const handleCloseEditTask = () => {
    editTaskForm.resetFields();
    setEditTaskAgents([]);
    setEditingTask(null);
    setEditTaskModalVisible(false);
  };

  const handleSubmitEditTask = async () => {
    try {
      const values = await editTaskForm.validateFields();
      values.agentIds = editTaskAgents.map(key => Number(key));
      
      await collaborationService.updateTask(editingTask.id, values);
      message.success('任务更新成功');
      handleCloseEditTask();
      loadData();
    } catch (error) {
      message.error('更新任务失败');
    }
  };

  const handleExecuteTask = (task: any) => {
    // 找到任务对应的团队
    const collaboration = collaborations.find(c => c.id === task.collaborationId);
    
    if (!collaboration) {
      message.error('找不到对应的团队');
      return;
    }

    // 检查团队中的Agent角色配置
    const agents = collaboration.agents || [];
    const hasManager = agents.some(agent => agent.role === 'Manager');
    const hasWorker = agents.some(agent => agent.role === 'Worker');

    // 如果没有必要的角色，显示提示
    if (!hasManager || !hasWorker) {
      Modal.warning({
        title: '缺少必要的角色',
        content: (
          <div>
            <p>执行任务需要以下角色：</p>
            <ul>
              {!hasManager && <li>❌ 缺少Manager（协调者）</li>}
              {hasManager && <li>✅ 已有Manager（协调者）</li>}
              {!hasWorker && <li>❌ 缺少Worker（执行者）</li>}
              {hasWorker && <li>✅ 已有Worker（执行者）</li>}
            </ul>
            <p style={{ marginTop: 16 }}>请先在团队中添加必要的Agent角色。</p>
          </div>
        ),
        okText: '去添加',
        onOk: () => {
          // 直接打开添加智能体的弹窗
          setSelectedCollaboration(collaboration);
          addAgentForm.resetFields();
          setSelectedAgentId(null);
          setAddAgentModalVisible(true);
        }
      });
      return;
    }

    // 角色配置完整，打开执行弹窗
    setSelectedTask(task);
    executeForm.setFieldsValue({
      workflowType: 'magentic',
      maxIterations: 10,
      orchestrationMode: 'manager'
    });
    setExecuteTaskModalVisible(true);
  };

  const handleConfirmExecuteTask = async () => {
    try {
      const values = await executeForm.validateFields();
      
      setExecutionMessages([]);
      setExecutionModalVisible(true);
      setExecuteTaskModalVisible(false);
      setIsExecuting(true);
      
      const input = selectedTask.description || selectedTask.title;
      const token = localStorage.getItem('token');
      
      if (values.workflowType === 'magentic') {
        const url = `http://localhost:5000/api/collaborationworkflow/${selectedTask.collaborationId}/review-iterative`;
        
        const response = await fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({ input })
        });
        
        if (!response.ok) {
          const errorData = await response.json();
          message.error(errorData.error || '执行失败');
          setIsExecuting(false);
          return;
        }
        
        const result = await response.json();
        
        if (result.success) {
          if (result.messages && result.messages.length > 0) {
            setExecutionMessages(result.messages.map((msg: any) => ({
              sender: msg.sender,
              content: msg.content,
              timestamp: msg.timestamp
            })));
          }
          message.success('任务执行完成！');
        } else {
          message.error(result.error || '任务执行失败');
        }
        
        setIsExecuting(false);
        loadData();
      } else {
        const url = `http://localhost:5000/api/collaborationworkflow/${selectedTask.collaborationId}/groupchat`;
        
        const parameters = {
          orchestrationMode: values.orchestrationMode || 'manager',
          maxIterations: values.maxIterations || 10
        };
        
        const response = await fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
          },
          body: JSON.stringify({ input, parameters, taskId: selectedTask.id })
        });
        
        const reader = response.body?.getReader();
        const decoder = new TextDecoder();
        
        if (!reader) {
          message.error('无法获取响应流');
          setIsExecuting(false);
          return;
        }
        
        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          
          const chunk = decoder.decode(value, { stream: true });
          const lines = chunk.split('\n');
          
          for (const line of lines) {
            if (line.startsWith('data:')) {
              const jsonStr = line.substring(5).trim();
              if (jsonStr) {
                try {
                  const message = JSON.parse(jsonStr);
                  setExecutionMessages(prev => [...prev, message]);
                } catch (e) {
                  console.error('解析消息失败:', e);
                }
              }
            }
          }
        }
        
        message.success('任务执行完成！');
        setIsExecuting(false);
        loadData();
      }
    } catch (error) {
      message.error('启动任务失败');
      setIsExecuting(false);
    }
  };

  const handleViewChatHistory = (task: any) => {
    setSelectedTask(task);
    setChatHistoryModalVisible(true);
  };

  const handleEditAgent = (collaboration: Collaboration, agent: any) => {
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
        loadData();
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
      render: (agents: any[]) => agents.length,
    },
    {
      title: '任务',
      dataIndex: 'tasks',
      key: 'tasks',
      width: '8%',
      render: (tasks: any[]) => tasks.length,
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
      render: (_: any, record: Collaboration) => (
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
              form.setFieldsValue(record);
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

  const taskColumns = [
    {
      title: '标题',
      dataIndex: 'title',
      key: 'title',
      width: '20%',
      ellipsis: true,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: '30%',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: '10%',
      render: (status: string) => {
        const colorMap: Record<string, string> = {
          Pending: 'default',
          InProgress: 'processing',
          Completed: 'success',
          Failed: 'error',
        };
        const textMap: Record<string, string> = {
          Pending: '待处理',
          InProgress: '进行中',
          Completed: '已完成',
          Failed: '失败',
        };
        return <Tag color={colorMap[status]}>{textMap[status] || status}</Tag>;
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: '15%',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      width: '25%',
      render: (_: any, record: any) => (
        <Space size="small" wrap>
          <Button 
            type="link" 
            size="small"
            icon={<PlayCircleOutlined />}
            onClick={() => handleExecuteTask(record)}
          >
            执行
          </Button>
          <Button 
            type="link" 
            size="small"
            icon={<EditOutlined />}
            onClick={() => handleEditTask(record)}
          >
            编辑
          </Button>
          <Button 
            type="link" 
            size="small"
            icon={<MessageOutlined />}
            onClick={() => handleViewChatHistory(record)}
          >
            团队协作过程
          </Button>
        </Space>
      ),
    },
  ];

  const expandedRowRender = (record: Collaboration) => {
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
                      render: (customPrompt: string, agentRecord: any) => {
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
                      render: (_: any, agentRecord: any) => (
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
              <div>
                <Button
                  type="primary"
                  onClick={() => handleCreateTask(record)}
                  style={{ marginBottom: 16 }}
                >
                  创建任务
                </Button>
                <Table
                  dataSource={record.tasks}
                  columns={taskColumns}
                  rowKey="id"
                  pagination={false}
                />
              </div>
            ),
          },
        ]}
      />
    );
  };

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
          
          <Divider orientation="left">
            <GithubOutlined /> Git配置
          </Divider>
          
          <Alert
            message="Git配置说明"
            description="配置Git仓库信息后，智能体可以进行代码提交操作。访问令牌将加密存储。"
            type="info"
            showIcon
            style={{ marginBottom: 16 }}
          />
          
          <Row gutter={16}>
            <Col span={16}>
              <Form.Item 
                label="Git仓库地址" 
                name="gitRepositoryUrl"
                tooltip="支持HTTPS或SSH地址，例如: https://github.com/user/repo.git"
              >
                <Input 
                  placeholder="https://github.com/user/repo.git" 
                  prefix={<GithubOutlined />}
                />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item 
                label="分支" 
                name="gitBranch"
                tooltip="默认为main分支"
              >
                <Input 
                  placeholder="main" 
                  prefix={<BranchesOutlined />}
                />
              </Form.Item>
            </Col>
          </Row>
          
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item 
                label="Git用户名" 
                name="gitUsername"
                tooltip="用于提交代码时的用户名"
              >
                <Input placeholder="请输入Git用户名" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item 
                label="Git邮箱" 
                name="gitEmail"
                tooltip="用于提交代码时的邮箱"
              >
                <Input placeholder="请输入Git邮箱" type="email" />
              </Form.Item>
            </Col>
          </Row>
          
          <Form.Item 
            label="访问令牌" 
            name="gitAccessToken"
            tooltip="GitHub: Personal Access Token; GitLab: Access Token; Gitee: 私人令牌"
          >
            <Input.Password placeholder="请输入访问令牌" />
          </Form.Item>
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
                    (ca: any) => ca.agentId === agent.id
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
              (ca: any) => ca.agentId === agent.id
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
        width={750}
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
            label={<span>选择队员 <span style={{color: '#999', fontSize: 12}}>(不选择则使用团队所有成员)</span></span>}
          >
            <Transfer
              dataSource={selectedCollaboration?.agents?.map(agent => ({
                key: agent.agentId.toString(),
                title: agent.agentName,
              })) || []}
              titles={['可选队员', '已选队员']}
              targetKeys={selectedTaskAgents}
              onChange={(targetKeys) => {
                setSelectedTaskAgents(targetKeys as string[]);
              }}
              render={item => item.title}
              listStyle={{ width: 280, height: 180 }}
              selectAllLabels={['全选', '全选']}
            />
          </Form.Item>
          
          <Divider style={{ margin: '12px 0' }}>Git配置</Divider>
          
          <Row gutter={16}>
            <Col span={18}>
              <Form.Item 
                label={<span>Git仓库地址 <span style={{color: '#999', fontSize: 12}}>(支持 GitHub、GitLab、Gitea)</span></span>}
                name="gitUrl"
              >
                <Input placeholder="https://github.com/user/repo.git" />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item label={<span>目标分支 <span style={{color: '#999', fontSize: 12}}>(默认main)</span></span>} name="gitBranch">
                <Input placeholder="main" />
              </Form.Item>
            </Col>
          </Row>
          
          <Form.Item 
            label={<span>访问令牌 <span style={{color: '#999', fontSize: 12}}>(安全存储)</span></span>}
            name="gitToken"
          >
            <Input.Password placeholder="ghp_xxx" />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="编辑任务"
        open={editTaskModalVisible}
        onOk={handleSubmitEditTask}
        onCancel={handleCloseEditTask}
        destroyOnClose
        width={750}
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
            label={<span>选择队员 <span style={{color: '#999', fontSize: 12}}>(不选择则使用团队所有成员)</span></span>}
          >
            <Transfer
              dataSource={selectedCollaboration?.agents?.map(agent => ({
                key: agent.agentId.toString(),
                title: agent.agentName,
              })) || []}
              titles={['可选队员', '已选队员']}
              targetKeys={editTaskAgents}
              onChange={(targetKeys) => {
                setEditTaskAgents(targetKeys as string[]);
              }}
              render={item => item.title}
              listStyle={{ width: 280, height: 180 }}
              selectAllLabels={['全选', '全选']}
            />
          </Form.Item>
          
          <Divider style={{ margin: '12px 0' }}>Git配置</Divider>
          
          <Row gutter={16}>
            <Col span={18}>
              <Form.Item 
                label={<span>Git仓库地址 <span style={{color: '#999', fontSize: 12}}>(支持 GitHub、GitLab、Gitea)</span></span>}
                name="gitUrl"
              >
                <Input placeholder="https://github.com/user/repo.git" />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item label={<span>目标分支 <span style={{color: '#999', fontSize: 12}}>(默认main)</span></span>} name="gitBranch">
                <Input placeholder="main" />
              </Form.Item>
            </Col>
          </Row>
          
          <Form.Item 
            label={
              <span>
                访问令牌 
                {editingTask?.hasGitToken ? (
                  <span style={{color: '#52c41a', fontSize: 12}}> (已设置，留空保持不变，填写则更新)</span>
                ) : (
                  <span style={{color: '#999', fontSize: 12}}> (未设置)</span>
                )}
              </span>
            }
            name="gitToken"
          >
            <Input.Password placeholder={editingTask?.hasGitToken ? "留空保持原令牌不变" : "请输入访问令牌"} />
          </Form.Item>
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
        title="执行任务"
        open={executeTaskModalVisible}
        onOk={handleConfirmExecuteTask}
        onCancel={() => setExecuteTaskModalVisible(false)}
        okText="执行工作流"
        cancelText="取消"
        width={700}
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

            {(() => {
              const collaboration = collaborations.find(c => c.id === selectedTask.collaborationId);
              if (!collaboration) return null;

              const agents = collaboration.agents || [];
              const managers = agents.filter(agent => agent.role === 'Manager');
              const workers = agents.filter(agent => agent.role === 'Worker');

              return (
                <Card size="small" title="参与Agent" style={{ marginBottom: 16 }}>
                  <Space direction="vertical" style={{ width: '100%' }}>
                    <div>
                      <Text strong>Manager（协调者）：</Text>
                      <div style={{ marginTop: 4 }}>
                        {managers.length > 0 ? (
                          managers.map(agent => (
                            <Tag key={agent.agentId} color="blue" style={{ marginBottom: 4 }}>
                              {agent.agentName}
                            </Tag>
                          ))
                        ) : (
                          <Text type="secondary">无</Text>
                        )}
                      </div>
                    </div>
                    <div>
                      <Text strong>Worker（执行者）：</Text>
                      <div style={{ marginTop: 4 }}>
                        {workers.length > 0 ? (
                          workers.map(agent => (
                            <Tag key={agent.agentId} color="green" style={{ marginBottom: 4 }}>
                              {agent.agentName}
                            </Tag>
                          ))
                        ) : (
                          <Text type="secondary">无</Text>
                        )}
                      </div>
                    </div>
                  </Space>
                </Card>
              );
            })()}

            <Form
              form={executeForm}
              layout="vertical"
            >
              <Form.Item
                label="选择工作流模式"
                name="workflowType"
                rules={[{ required: true, message: '请选择工作流类型' }]}
              >
                <Select style={{ width: '100%' }}>
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
                      <Tag color="green">主持人协调</Tag>
                    </Space>
                  </Option>
                </Select>
              </Form.Item>

              <Form.Item shouldUpdate>
                {({ getFieldValue }) => {
                  const workflowType = getFieldValue('workflowType');
                  
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
                      description: '主持人Agent协调多个Agents进行对话讨论',
                      features: [
                        '✅ 主持人控制对话流程',
                        '✅ 决定何时结束讨论',
                        '✅ 总结和归纳观点',
                        '✅ 多轮对话，达成共识',
                      ],
                      useCases: '适合开放性任务，如：头脑风暴、创意讨论、方案评审',
                    },
                  };

                  const currentWorkflow = workflowDescriptions[workflowType as keyof typeof workflowDescriptions];

                  return currentWorkflow ? (
                    <Alert
                      message={currentWorkflow.title}
                      description={
                        <div>
                          <p>{currentWorkflow.description}</p>
                          <br />
                          {currentWorkflow.features.map((feature, index) => (
                            <p key={index} style={{ margin: '4px 0' }}>{feature}</p>
                          ))}
                          <br />
                          <p><strong>适用场景：</strong>{currentWorkflow.useCases}</p>
                        </div>
                      }
                      type="info"
                      showIcon
                      icon={currentWorkflow.icon}
                      style={{ marginBottom: 16 }}
                    />
                  ) : null;
                }}
              </Form.Item>

              <Form.Item shouldUpdate>
                {({ getFieldValue }) => {
                  const workflowType = getFieldValue('workflowType');
                  
                  if (workflowType === 'magentic') {
                    return (
                      <div>
                        <Title level={5}>
                          <SettingOutlined /> 配置参数
                        </Title>
                        <Form.Item
                          label="最大迭代次数"
                          name="maxIterations"
                          tooltip="Manager最多进行多少轮迭代决策"
                        >
                          <InputNumber
                            min={1}
                            max={50}
                            style={{ width: '100%' }}
                            placeholder="默认10次"
                          />
                        </Form.Item>
                      </div>
                    );
                  }
                  
                  if (workflowType === 'groupchat') {
                    return (
                      <div>
                        <Title level={5}>
                          <MessageOutlined /> 群聊配置
                        </Title>
                        <Form.Item
                          label="协调模式"
                          name="orchestrationMode"
                          tooltip="选择Agent发言的协调方式"
                        >
                          <Radio.Group style={{ width: '100%' }}>
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
                        </Form.Item>
                        <Form.Item
                          label="最大迭代次数"
                          name="maxIterations"
                          tooltip="群聊最多进行多少轮对话"
                        >
                          <InputNumber
                            min={1}
                            max={50}
                            style={{ width: '100%' }}
                            placeholder="默认10次"
                          />
                        </Form.Item>
                      </div>
                    );
                  }
                  
                  return null;
                }}
              </Form.Item>
            </Form>
          </div>
        )}
      </Modal>

      <Modal
        title="执行过程"
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
          
          {executionMessages.map((msg, index) => (
            <div key={index} style={{ 
              marginBottom: 16, 
              padding: 12, 
              backgroundColor: '#f5f5f5', 
              borderRadius: 8 
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
                <Tag color={msg.role === 'system' ? 'purple' : 'blue'}>
                  {msg.sender || 'Agent'}
                </Tag>
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {msg.timestamp ? new Date(msg.timestamp).toLocaleString('zh-CN') : ''}
                </Text>
              </div>
              <div style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
                {msg.content}
              </div>
            </div>
          ))}
        </div>
      </Modal>
    </div>
  );
};

export default Collaborations;