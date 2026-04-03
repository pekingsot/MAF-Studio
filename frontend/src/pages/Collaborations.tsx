import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Tag, Space, message, Tabs, Select, Popconfirm, Divider, Row, Col, Alert, Radio, InputNumber, Typography, Card, Tooltip } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, UserOutlined, FolderOutlined, GithubOutlined, BranchesOutlined, PlayCircleOutlined, EyeOutlined, MessageOutlined, SettingOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';
import { useNavigate } from 'react-router-dom';
import ChatHistory from './collaboration-detail/ChatHistory';

const { Option } = Select;
const { Title, Text } = Typography;

const Collaborations: React.FC = () => {
  const navigate = useNavigate();
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [addAgentModalVisible, setAddAgentModalVisible] = useState(false);
  const [createTaskModalVisible, setCreateTaskModalVisible] = useState(false);
  const [editAgentModalVisible, setEditAgentModalVisible] = useState(false);
  const [chatHistoryModalVisible, setChatHistoryModalVisible] = useState(false);
  const [executeTaskModalVisible, setExecuteTaskModalVisible] = useState(false);
  const [selectedTask, setSelectedTask] = useState<any>(null);
  const [selectedCollaboration, setSelectedCollaboration] = useState<Collaboration | null>(null);
  const [executeForm] = Form.useForm();
  const [editingAgent, setEditingAgent] = useState<any>(null);
  const [form] = Form.useForm();
  const [addAgentForm] = Form.useForm();
  const [createTaskForm] = Form.useForm();
  const [editAgentForm] = Form.useForm();
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null);
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
    form.resetFields();
    setModalVisible(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      await collaborationService.createCollaboration(values);
      message.success('创建成功');
      setModalVisible(false);
      loadData();
    } catch (error) {
      message.error('创建失败');
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
    setCreateTaskModalVisible(true);
  };

  const handleSubmitCreateTask = async () => {
    try {
      const values = await createTaskForm.validateFields();
      if (selectedCollaboration) {
        await collaborationService.createTask(selectedCollaboration.id, values);
        message.success('创建成功');
        setCreateTaskModalVisible(false);
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

  const handleDeleteTask = async (taskId: string) => {
    try {
      await collaborationService.deleteTask(taskId);
      message.success('删除成功');
      loadData();
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleExecuteTask = (task: any) => {
    // 找到任务对应的协作
    const collaboration = collaborations.find(c => c.id === task.collaborationId);
    
    if (!collaboration) {
      message.error('找不到对应的协作');
      return;
    }

    // 检查协作中的Agent角色配置
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
            <p style={{ marginTop: 16 }}>请先在协作中添加必要的Agent角色。</p>
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
      maxIterations: 10
    });
    setExecuteTaskModalVisible(true);
  };

  const handleConfirmExecuteTask = async () => {
    try {
      const values = await executeForm.validateFields();
      
      message.loading({ content: '正在启动任务...', key: 'executeTask' });
      
      const input = selectedTask.description || selectedTask.title;
      
      let result;
      if (values.workflowType === 'magentic') {
        result = await collaborationService.executeSequentialWorkflow(
          selectedTask.collaborationId,
          input
        );
      } else {
        result = await collaborationService.executeGroupChatWorkflow(
          selectedTask.collaborationId,
          input
        );
      }
      
      message.success({ content: '任务已启动！', key: 'executeTask' });
      setExecuteTaskModalVisible(false);
      loadData();
    } catch (error) {
      message.error({ content: '启动任务失败', key: 'executeTask' });
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
      title: '名称',
      dataIndex: 'name',
      key: 'name',
      width: '15%',
      ellipsis: true,
    },
    {
      title: '路径',
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
            title="确定要删除这个协作项目吗？"
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
            icon={<MessageOutlined />}
            onClick={() => handleViewChatHistory(record)}
          >
            协作过程
          </Button>
          <Popconfirm
            title="确定要删除这个任务吗？"
            onConfirm={() => handleDeleteTask(record.id)}
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
        <h2>协作管理</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          创建协作项目
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
        title={selectedCollaboration ? '编辑协作项目' : '创建协作项目'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={700}
      >
        <Form form={form} layout="vertical">
          <Form.Item
            label="名称"
            name="name"
            rules={[{ required: true, message: '请输入项目名称' }]}
          >
            <Input placeholder="请输入项目名称" />
          </Form.Item>
          <Form.Item 
            label="工作目录" 
            name="path"
            tooltip="项目的代码目录路径，用于智能体执行任务"
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
                  // 过滤掉已经在协作中的智能体
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
              message="所有智能体都已在协作中"
              description="该协作已经添加了所有可用的智能体，请先创建新的智能体或移除现有智能体。"
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

          <Form.Item
            label="自定义提示词"
            name="customPrompt"
            tooltip="自动填充系统提示词，您可以根据项目需求修改此提示词"
          >
            <Input.TextArea 
              rows={6} 
              placeholder="自动填充系统提示词，您可以根据项目需求修改..." 
              showCount
              maxLength={2000}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="创建任务"
        open={createTaskModalVisible}
        onOk={handleSubmitCreateTask}
        onCancel={() => setCreateTaskModalVisible(false)}
        width={600}
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
            <Input.TextArea rows={4} placeholder="请输入任务描述" />
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

          <Form.Item
            label="自定义提示词"
            name="customPrompt"
            tooltip="自定义提示词用于覆盖系统提示词，Manager会根据自定义提示词分配任务"
          >
            <Input.TextArea 
              rows={6} 
              placeholder="自定义提示词（默认使用系统提示词）" 
              showCount
              maxLength={2000}
            />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="协作过程"
        open={chatHistoryModalVisible}
        onCancel={() => setChatHistoryModalVisible(false)}
        footer={null}
        width={800}
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
            <ChatHistory collaborationId={selectedTask.collaborationId} />
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
                  
                  return null;
                }}
              </Form.Item>
            </Form>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default Collaborations;