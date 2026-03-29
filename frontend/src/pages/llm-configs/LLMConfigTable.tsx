import React from 'react';
import { Table, Button, Space, Tag, Popconfirm, Tooltip } from 'antd';
import { EditOutlined, DeleteOutlined, StarOutlined, StarFilled, PlusOutlined, CopyOutlined, AppstoreAddOutlined, ThunderboltOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { LLMConfig, PROVIDER_COLORS } from './types';

interface LLMConfigTableProps {
  configs: LLMConfig[];
  loading: boolean;
  providers: { id: string; displayName: string }[];
  onEdit: (config: LLMConfig) => void;
  onDelete: (id: number) => void;
  onSetDefault: (id: number) => void;
  onAddModel: (configId: number) => void;
  onBatchAddModels: (configId: number) => void;
  onTestAllModels: (configId: number) => void;
  onDuplicate: (id: number) => void;
  renderModelList: (config: LLMConfig) => React.ReactNode;
}

const LLMConfigTable: React.FC<LLMConfigTableProps> = ({
  configs,
  loading,
  providers,
  onEdit,
  onDelete,
  onSetDefault,
  onAddModel,
  onBatchAddModels,
  onTestAllModels,
  onDuplicate,
  renderModelList,
}) => {
  const getProviderDisplayName = (providerId: string) => {
    const provider = providers.find((p) => p.id === providerId);
    return provider?.displayName || providerId;
  };

  const getProviderColor = (providerId: string) => {
    return PROVIDER_COLORS[providerId] || '#1890ff';
  };

  const columns: ColumnsType<LLMConfig> = [
    {
      title: '配置名称',
      dataIndex: 'name',
      key: 'name',
      width: 200,
      render: (name: string, record: LLMConfig) => (
        <Space>
          {record.isDefault && <StarFilled style={{ color: '#faad14' }} />}
          <span style={{ fontWeight: 500 }}>{name}</span>
        </Space>
      ),
    },
    {
      title: '供应商',
      dataIndex: 'provider',
      key: 'provider',
      width: 180,
      render: (provider: string) => {
        const color = getProviderColor(provider);
        return (
          <Tag color={color} style={{ fontWeight: 'bold' }}>
            {getProviderDisplayName(provider)}
          </Tag>
        );
      },
    },
    {
      title: '模型数量',
      key: 'modelCount',
      width: 100,
      render: (_: unknown, record: LLMConfig) => (
        <Tag color="blue">{record.models?.length || 0}</Tag>
      ),
    },
    {
      title: '状态',
      dataIndex: 'isEnabled',
      key: 'isEnabled',
      width: 80,
      render: (enabled: boolean) => (
        <Tag color={enabled ? 'green' : 'red'}>{enabled ? '启用' : '禁用'}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 480,
      render: (_: unknown, record: LLMConfig) => (
        <Space size={0}>
          <Button type="link" icon={<PlusOutlined />} onClick={() => onAddModel(record.id)}>
            添加模型
          </Button>
          <Button type="link" icon={<AppstoreAddOutlined />} onClick={() => onBatchAddModels(record.id)}>
            批量添加
          </Button>
          <Button type="link" icon={<ThunderboltOutlined />} onClick={() => onTestAllModels(record.id)}>
            统一测试
          </Button>
          <Button type="link" icon={<EditOutlined />} onClick={() => onEdit(record)}>
            编辑
          </Button>
          <Popconfirm
            title="确定要复制这个配置吗？"
            description="将复制配置及其所有模型"
            onConfirm={() => onDuplicate(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" icon={<CopyOutlined />}>
              复制
            </Button>
          </Popconfirm>
          {!record.isDefault && (
            <Button type="link" icon={<StarOutlined />} onClick={() => onSetDefault(record.id)}>
              设为默认
            </Button>
          )}
          <Popconfirm
            title="确定要删除这个配置吗？"
            onConfirm={() => onDelete(record.id)}
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

  return (
    <Table
      dataSource={configs}
      columns={columns}
      rowKey="id"
      loading={loading}
      expandable={{
        expandedRowRender: (record) => renderModelList(record),
        rowExpandable: () => true,
      }}
    />
  );
};

export default LLMConfigTable;
