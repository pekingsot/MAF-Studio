import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Tag, Space, message, Tabs, Select, Popconfirm, Divider, Row, Col, Alert } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, TeamOutlined, FolderOutlined, GithubOutlined, BranchesOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';

const Collaborations: React.FC = () => {
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [agents, setAgents] = useState<Agent[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [addAgentModalVisible, setAddAgentModalVisible] = useState(false);
  const [createTaskModalVisible, setCreateTaskModalVisible] = useState(false);
  const [selectedCollaboration, setSelectedCollaboration] = useState<Collaboration | null>(null);
  const [form] = Form.useForm();
  const [addAgentForm] = Form.useForm();
  const [createTaskForm] = Form.useForm();
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
      setAgents(agentsResponse.agents || []);
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
    } catch (error) {
      message.error('添加失败');
    }
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

  const handleUpdateTaskStatus = async (taskId: string, status: string) => {
    try {
      await collaborationService.updateTaskStatus(taskId, status);
      message.success('更新成功');
      loadData();
    } catch (error) {
      message.error('更新失败');
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

  const handleRemoveAgent = async (collaborationId: string, agentId: string) => {
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

  const columns = [
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
    },
    {
      title: '路径',
      dataIndex: 'path',
      key: 'path',
      render: (path: string) => path || '-',
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '智能体数量',
      dataIndex: 'agents',
      key: 'agents',
      render: (agents: any[]) => agents.length,
    },
    {
      title: '任务数量',
      dataIndex: 'tasks',
      key: 'tasks',
      render: (tasks: any[]) => tasks.length,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
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
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: Collaboration) => (
        <Space>
          <Button
            type="link"
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
            <Button type="link" danger icon={<DeleteOutlined />}>
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
      render: (status: string, record: any) => {
        const colorMap: Record<string, string> = {
          Pending: 'default',
          InProgress: 'processing',
          Completed: 'success',
          Failed: 'error',
        };
        return (
          <Select
            value={status}
            style={{ width: 120 }}
            onChange={(value) => handleUpdateTaskStatus(record.id, value)}
          >
            <Select.Option value="Pending">待处理</Select.Option>
            <Select.Option value="InProgress">进行中</Select.Option>
            <Select.Option value="Completed">已完成</Select.Option>
            <Select.Option value="Failed">失败</Select.Option>
          </Select>
        );
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: any) => (
        <Popconfirm
          title="确定要删除这个任务吗？"
          onConfirm={() => handleDeleteTask(record.id)}
          okText="确定"
          cancelText="取消"
        >
          <Button type="link" danger icon={<DeleteOutlined />}>
            删除
          </Button>
        </Popconfirm>
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
                      dataIndex: ['agent', 'name'],
                      key: 'agentName',
                    },
                    {
                      title: '类型',
                      dataIndex: ['agent', 'type'],
                      key: 'agentType',
                    },
                    {
                      title: '状态',
                      dataIndex: ['agent', 'status'],
                      key: 'agentStatus',
                      render: (status: string) => {
                        const colorMap: Record<string, string> = {
                          Active: 'green',
                          Inactive: 'default',
                          Busy: 'orange',
                          Error: 'red',
                        };
                        return <Tag color={colorMap[status]}>{status}</Tag>;
                      },
                    },
                    {
                      title: '加入时间',
                      dataIndex: 'joinedAt',
                      key: 'joinedAt',
                      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
                    },
                    {
                      title: '操作',
                      key: 'action',
                      render: (_: any, agentRecord: any) => (
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
                      ),
                    },
                  ]}
                  rowKey="id"
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
        width={600}
      >
        <Form form={addAgentForm} layout="vertical">
          <Form.Item
            label="智能体"
            name="agentId"
            rules={[{ required: true, message: '请选择智能体' }]}
          >
            <Select placeholder="请选择智能体">
              {agents.map(agent => (
                <Select.Option key={agent.id} value={agent.id}>
                  {agent.name} ({agent.type})
                </Select.Option>
              ))}
            </Select>
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
    </div>
  );
};

export default Collaborations;