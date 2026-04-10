import React, { memo } from 'react';
import { Table, Button, Space, Popconfirm, Tag } from 'antd';
import { PlayCircleOutlined, EditOutlined, MessageOutlined, DeleteOutlined } from '@ant-design/icons';
import type { Key } from 'react';

interface Task {
  id: number | string;
  title: string;
  description?: string;
  status: string;
  createdAt: string;
  collaborationId: string;
}

interface TaskTableProps {
  tasks: Task[];
  selectedRowKeys: (number | string)[];
  onSelectionChange: (selectedRowKeys: Key[]) => void;
  onExecute: (task: Task) => void;
  onEdit: (task: Task) => void;
  onViewHistory: (task: Task) => void;
  onDelete: (task: Task) => void;
}

const TaskTable: React.FC<TaskTableProps> = memo(({
  tasks,
  selectedRowKeys,
  onSelectionChange,
  onExecute,
  onEdit,
  onViewHistory,
  onDelete,
}) => {
  const rowSelection = {
    selectedRowKeys,
    onChange: onSelectionChange,
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: '6%',
    },
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
      width: '32%',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: '8%',
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
      width: '19%',
      render: (_: any, record: Task) => (
        <Space size="small" wrap>
          <Button 
            type="link" 
            size="small"
            icon={<PlayCircleOutlined />}
            onClick={() => onExecute(record)}
          >
            执行
          </Button>
          <Button 
            type="link" 
            size="small"
            icon={<EditOutlined />}
            onClick={() => onEdit(record)}
          >
            编辑
          </Button>
          <Button 
            type="link" 
            size="small"
            icon={<MessageOutlined />}
            onClick={() => onViewHistory(record)}
          >
            团队协作过程
          </Button>
          <Popconfirm
            title="确定删除此任务吗？"
            description="删除后将无法恢复"
            onConfirm={() => onDelete(record)}
            okText="确定"
            cancelText="取消"
          >
            <Button 
              type="link" 
              size="small"
              danger
              icon={<DeleteOutlined />}
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <Table
      dataSource={tasks}
      columns={columns}
      rowKey="id"
      pagination={false}
      rowSelection={rowSelection}
    />
  );
});

TaskTable.displayName = 'TaskTable';

export default TaskTable;
