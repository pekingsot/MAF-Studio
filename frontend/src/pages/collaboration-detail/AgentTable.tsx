import React from 'react';
import { Table, Button, Popconfirm } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { CollaborationAgent, AGENT_STATUS_COLOR_MAP } from './types';

interface AgentTableProps {
  agents: CollaborationAgent[];
  onRemove: (agentId: number) => void;
}

const AgentTable: React.FC<AgentTableProps> = ({ agents, onRemove }) => {
  const columns: ColumnsType<CollaborationAgent> = [
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
      render: (status: string) => (
        <span style={{ color: AGENT_STATUS_COLOR_MAP[status] }}>{status}</span>
      ),
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
      render: (_: unknown, record: CollaborationAgent) => (
        <Popconfirm
          title="确认移除"
          description="确定要移除这个智能体吗？"
          onConfirm={() => onRemove(record.agentId)}
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

  return (
    <Table
      dataSource={agents}
      columns={columns}
      rowKey="id"
      pagination={false}
      style={{ marginTop: 16 }}
    />
  );
};

export default AgentTable;
