import React, { useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Card, Descriptions, Button, Tabs, message, Space, Popconfirm, Table } from 'antd';
import { ArrowLeftOutlined, DeleteOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { collaborationService, Collaboration } from '../services/collaborationService';
import { agentService, Agent } from '../services/agentService';

const CollaborationDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [collaboration, setCollaboration] = useState<Collaboration | null>(null);
  const [loading, setLoading] = useState(true);
  const initializedRef = useRef(false);

  useEffect(() => {
    if (id && !initializedRef.current) {
      initializedRef.current = true;
      loadCollaboration(id);
    }
  }, [id]);

  const loadCollaboration = async (collaborationId: string) => {
    try {
      setLoading(true);
      const data = await collaborationService.getCollaborationById(collaborationId);
      setCollaboration(data);
    } catch (error) {
      message.error('加载协作详情失败');
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveAgent = async (agentId: string) => {
    if (id && collaboration) {
      try {
        await collaborationService.removeAgentFromCollaboration(id, agentId);
        message.success('移除成功');
        loadCollaboration(id);
      } catch (error) {
        message.error('移除失败');
      }
    }
  };

  const handleUpdateTaskStatus = async (taskId: string, status: string) => {
    try {
      await collaborationService.updateTaskStatus(taskId, status);
      message.success('更新成功');
      if (id) {
        loadCollaboration(id);
      }
    } catch (error) {
      message.error('更新失败');
    }
  };

  if (loading || !collaboration) {
    return <div>加载中...</div>;
  }

  const statusColorMap: Record<string, string> = {
    Active: 'green',
    Paused: 'orange',
    Completed: 'blue',
    Cancelled: 'red',
  };

  const taskStatusColorMap: Record<string, string> = {
    Pending: 'default',
    InProgress: 'processing',
    Completed: 'success',
    Failed: 'error',
  };

  const agentColumns = [
    {
      title: '智能体名称',
      dataIndex: ['agent', 'name'],
      key: 'agentName',
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
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
        return <span style={{ color: colorMap[status] }}>{status}</span>;
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
      render: (_: any, record: any) => (
        <Popconfirm
          title="确认移除"
          description="确定要移除这个智能体吗？"
          onConfirm={() => handleRemoveAgent(record.agentId)}
          okText="确定"
          cancelText="取消"
        >
          <Button danger icon={<DeleteOutlined />} size="small">
            移除
          </Button>
        </Popconfirm>
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
      render: (status: string) => {
        return <span style={{ color: taskStatusColorMap[status] }}>{status}</span>;
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
    {
      title: '完成时间',
      dataIndex: 'completedAt',
      key: 'completedAt',
      render: (date: string | null) => date ? new Date(date).toLocaleString('zh-CN') : '-',
    },
  ];

  return (
    <div>
      <Button
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate('/collaborations')}
        style={{ marginBottom: 16 }}
      >
        返回列表
      </Button>

      <Card title={collaboration.name} loading={loading}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="ID">{collaboration.id}</Descriptions.Item>
          <Descriptions.Item label="状态">
            <span style={{ color: statusColorMap[collaboration.status] }}>
              {collaboration.status}
            </span>
          </Descriptions.Item>
          <Descriptions.Item label="智能体数量">
            {collaboration.agents.length}
          </Descriptions.Item>
          <Descriptions.Item label="任务数量">
            {collaboration.tasks.length}
          </Descriptions.Item>
          <Descriptions.Item label="创建时间">
            {new Date(collaboration.createdAt).toLocaleString('zh-CN')}
          </Descriptions.Item>
          <Descriptions.Item label="更新时间">
            {collaboration.updatedAt
              ? new Date(collaboration.updatedAt).toLocaleString('zh-CN')
              : '未更新'}
          </Descriptions.Item>
          <Descriptions.Item label="描述" span={2}>
            {collaboration.description || '无描述'}
          </Descriptions.Item>
        </Descriptions>

        <Tabs
          defaultActiveKey="agents"
          items={[
            {
              key: 'agents',
              label: `智能体 (${collaboration.agents.length})`,
              children: (
                <Table
                  dataSource={collaboration.agents}
                  columns={agentColumns}
                  rowKey="id"
                  pagination={false}
                  style={{ marginTop: 16 }}
                />
              ),
            },
            {
              key: 'tasks',
              label: `任务 (${collaboration.tasks.length})`,
              children: (
                <Table
                  dataSource={collaboration.tasks}
                  columns={taskColumns}
                  rowKey="id"
                  pagination={false}
                  style={{ marginTop: 16 }}
                />
              ),
            },
          ]}
          style={{ marginTop: 24 }}
        />
      </Card>
    </div>
  );
};

export default CollaborationDetail;