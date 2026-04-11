import React, { useState, useCallback, useMemo } from 'react';
import { Table, Button, Space, Popconfirm, Tag, message, Tooltip } from 'antd';
import { PlayCircleOutlined, EditOutlined, MessageOutlined, DeleteOutlined, GithubOutlined } from '@ant-design/icons';
import type { Key } from 'react';
import { collaborationService } from '../services/collaborationService';

interface Task {
  id: number | string;
  title: string;
  description?: string;
  status: string;
  createdAt: string;
  collaborationId: string;
  gitUrl?: string;
  gitBranch?: string;
  gitToken?: string;
}

interface CollaborationTasksProps {
  collaborationId: string;
  tasks: Task[];
  onCreate: () => void;
  onExecute: (task: Task) => void;
  onEdit: (task: Task) => void;
  onViewHistory: (task: Task) => void;
  onDelete: (task: Task) => void;
  onRefresh: () => void;
}

const CollaborationTasks: React.FC<CollaborationTasksProps> = ({
  collaborationId,
  tasks,
  onCreate,
  onExecute,
  onEdit,
  onViewHistory,
  onDelete,
  onRefresh,
}) => {
  const [selectedRowKeys, setSelectedRowKeys] = useState<Key[]>([]);

  const handleSelectionChange = useCallback((newSelectedRowKeys: Key[]) => {
    setSelectedRowKeys(newSelectedRowKeys);
  }, []);

  const handleBatchDelete = useCallback(async () => {
    if (selectedRowKeys.length === 0) {
      message.warning('请先选择要删除的任务');
      return;
    }

    try {
      const taskIds = selectedRowKeys.map(id => typeof id === 'string' ? parseInt(id) : id) as number[];
      const result = await collaborationService.batchDeleteTasks(taskIds);
      message.success(result.message);
      setSelectedRowKeys([]);
      onRefresh();
    } catch (error) {
      message.error('批量删除失败');
    }
  }, [selectedRowKeys, onRefresh]);

  const columns = useMemo(() => [
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
          Cancelled: 'warning',
        };
        const textMap: Record<string, string> = {
          Pending: '待处理',
          InProgress: '进行中',
          Completed: '已完成',
          Failed: '失败',
          Cancelled: '已关闭',
        };
        return <Tag color={colorMap[status]}>{textMap[status] || status}</Tag>;
      },
    },
    {
      title: 'Git配置',
      key: 'git',
      width: '10%',
      render: (_: any, record: Task) => {
        if (!record.gitUrl) {
          return <Tag color="default">无</Tag>;
        }
        
        return (
          <Tooltip title={
            <div>
              <div>仓库: {record.gitUrl}</div>
              {record.gitBranch && <div>分支: {record.gitBranch}</div>}
              {record.gitToken && <div>已配置访问令牌</div>}
            </div>
          }>
            <Tag color="blue" icon={<GithubOutlined />}>
              {record.gitBranch || 'main'}
            </Tag>
          </Tooltip>
        );
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
  ], [onExecute, onEdit, onViewHistory, onDelete]);

  const rowSelection = {
    selectedRowKeys,
    onChange: handleSelectionChange,
  };

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button
          type="primary"
          onClick={onCreate}
        >
          创建任务
        </Button>
        {selectedRowKeys.length > 0 && (
          <Popconfirm
            title={`确定要删除选中的 ${selectedRowKeys.length} 个任务吗？`}
            onConfirm={handleBatchDelete}
            okText="确定"
            cancelText="取消"
          >
            <Button
              danger
              icon={<DeleteOutlined />}
            >
              批量删除 ({selectedRowKeys.length})
            </Button>
          </Popconfirm>
        )}
      </Space>
      <Table
        dataSource={tasks}
        columns={columns}
        rowKey="id"
        pagination={false}
        rowSelection={rowSelection}
      />
    </div>
  );
};

export default CollaborationTasks;
