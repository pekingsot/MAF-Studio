import React from 'react';
import { Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { CollaborationTask, TASK_STATUS_COLOR_MAP } from './types';

interface TaskTableProps {
  tasks: CollaborationTask[];
}

const TaskTable: React.FC<TaskTableProps> = ({ tasks }) => {
  const columns: ColumnsType<CollaborationTask> = [
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
      render: (status: string) => (
        <span style={{ color: TASK_STATUS_COLOR_MAP[status] }}>{status}</span>
      ),
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
      render: (date: string | null) => (date ? new Date(date).toLocaleString('zh-CN') : '-'),
    },
  ];

  return (
    <Table
      dataSource={tasks}
      columns={columns}
      rowKey="id"
      pagination={false}
      style={{ marginTop: 16 }}
    />
  );
};

export default TaskTable;
