import React, { useState, useEffect } from 'react';
import { Card, Table, Button, Modal, message, Space, Tag } from 'antd';
import { PlayCircleOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration } from '../../services/collaborationService';
import WorkflowExecutor from './WorkflowExecutor';

const CollaborationWorkflowPage: React.FC = () => {
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [loading, setLoading] = useState(false);
  const [executorVisible, setExecutorVisible] = useState(false);
  const [selectedCollaboration, setSelectedCollaboration] = useState<Collaboration | null>(null);

  useEffect(() => {
    loadCollaborations();
  }, []);

  const loadCollaborations = async () => {
    setLoading(true);
    try {
      const data = await collaborationService.getAllCollaborations();
      setCollaborations(data);
    } catch (error: any) {
      message.error(`加载协作列表失败: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const handleExecute = (collaboration: Collaboration) => {
    setSelectedCollaboration(collaboration);
    setExecutorVisible(true);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 80,
    },
    {
      title: '协作名称',
      dataIndex: 'name',
      key: 'name',
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
        const colorMap: Record<string, string> = {
          Active: 'green',
          Paused: 'orange',
          Completed: 'blue',
          Cancelled: 'red',
        };
        return <Tag color={colorMap[status] || 'default'}>{status}</Tag>;
      },
    },
    {
      title: 'Agent数量',
      key: 'agentCount',
      render: (_: any, record: Collaboration) => record.agents?.length || 0,
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: Collaboration) => (
        <Space>
          <Button
            type="primary"
            icon={<PlayCircleOutlined />}
            onClick={() => handleExecute(record)}
          >
            执行工作流
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Card title="协作工作流管理">
        <Table
          columns={columns}
          dataSource={collaborations}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条记录`,
          }}
        />
      </Card>

      <Modal
        title="执行协作工作流"
        open={executorVisible}
        onCancel={() => {
          setExecutorVisible(false);
          setSelectedCollaboration(null);
        }}
        footer={null}
        width={800}
      >
        {selectedCollaboration && (
          <WorkflowExecutor
            collaborationId={parseInt(selectedCollaboration.id)}
            collaborationName={selectedCollaboration.name}
          />
        )}
      </Modal>
    </div>
  );
};

export default CollaborationWorkflowPage;
