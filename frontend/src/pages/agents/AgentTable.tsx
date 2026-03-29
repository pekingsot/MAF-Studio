import React from 'react';
import { Table, Button, Tag, Space, Tooltip, Typography, Badge, Popconfirm } from 'antd';
import { EditOutlined, DeleteOutlined, PlayCircleOutlined, PauseCircleOutlined, StopOutlined, ThunderboltOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { Agent } from '../../services/agentService';
import { AgentRuntimeStatus } from '../../services/agentRuntimeService';
import { AgentType } from '../../services/agentService';
import { LLMConfig, AGENT_STATUS_MAP, RUNTIME_STATE_MAP } from './types';

const { Text } = Typography;

interface AgentTableProps {
  agents: Agent[];
  agentTypes: AgentType[];
  llmConfigs: LLMConfig[];
  runtimeStatuses: Record<string, AgentRuntimeStatus>;
  loading: boolean;
  testingAgent: number | null;
  activatingAgent: number | null;
  onEdit: (agent: Agent) => void;
  onDelete: (id: number) => void;
  onActivate: (id: number) => void;
  onSleep: (id: number) => void;
  onDestroy: (id: number) => void;
  onTest: (id: number) => void;
  pageSize: number;
  onPageSizeChange: (size: number) => void;
}

const AgentTable: React.FC<AgentTableProps> = ({
  agents,
  agentTypes,
  runtimeStatuses,
  loading,
  testingAgent,
  activatingAgent,
  onEdit,
  onDelete,
  onActivate,
  onSleep,
  onDestroy,
  onTest,
  pageSize,
  onPageSizeChange,
}) => {
  const columns: ColumnsType<Agent> = [
    {
      title: '头像',
      dataIndex: 'avatar',
      key: 'avatar',
      width: 50,
      align: 'center',
      render: (avatar: string) => (
        <span style={{ fontSize: 24 }}>{avatar || '🤖'}</span>
      ),
    },
    {
      title: '名称',
      dataIndex: 'name',
      key: 'name',
      width: 70,
      render: (name: string) => <Text strong>{name}</Text>,
    },
    {
      title: '类型',
      dataIndex: 'type',
      key: 'type',
      width: 70,
      render: (type: string) => {
        const t = agentTypes.find(at => at.code === type);
        return <Tag color="blue">{t?.name || type}</Tag>;
      },
    },
    {
      title: '主模型',
      key: 'llmConfig',
      width: 120,
      render: (_: unknown, record: Agent) => {
        if (record.primaryModelName && record.llmConfigName) {
          return (
            <Tooltip title={`${record.llmConfigName} - ${record.primaryModelName}`}>
              <Tag color="green">{record.llmConfigName}</Tag>
              <div style={{ fontSize: 11, color: '#666', marginTop: 2 }}>
                {record.primaryModelName}
              </div>
            </Tooltip>
          );
        }
        return <Tag color="red">未配置</Tag>;
      },
    },
    {
      title: '副模型',
      key: 'fallbackModels',
      width: 150,
      render: (_: unknown, record: Agent) => {
        if (!record.fallbackModels || record.fallbackModels.length === 0) {
          return <Text type="secondary" style={{ fontSize: 12 }}>无</Text>;
        }
        return (
          <Space direction="vertical" size={2}>
            {record.fallbackModels.map((fm, index) => (
              <div key={index} style={{ marginBottom: 4 }}>
                <Tag color="blue" style={{ fontSize: 11 }}>
                  {fm.llmConfigName || `配置:${fm.llmConfigId}`}
                </Tag>
                {fm.modelName && (
                  <div style={{ fontSize: 11, color: '#666', marginTop: 2 }}>
                    {fm.modelName}
                  </div>
                )}
              </div>
            ))}
          </Space>
        );
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 80,
      render: (status: string) => {
        const s = AGENT_STATUS_MAP[status] || { color: 'default', label: status };
        return <Tag color={s.color}>{s.label}</Tag>;
      },
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: 150,
      render: (description: string) => (
        <div style={{ 
          whiteSpace: 'pre-wrap', 
          wordBreak: 'break-word',
          maxHeight: 60,
          overflow: 'hidden',
          fontSize: 12,
          color: '#666'
        }}>
          {description || '-'}
        </div>
      ),
    },
    {
      title: '系统提示词',
      key: 'systemPrompt',
      width: 350,
      render: (_: unknown, record: Agent) => {
        const prompt = record.systemPrompt;
        if (!prompt) return <Text type="secondary" style={{ fontSize: 12 }}>-</Text>;
        
        return (
          <Tooltip title={<div style={{ whiteSpace: 'pre-wrap', maxWidth: 400 }}>{prompt}</div>} placement="topLeft">
            <div style={{ 
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              overflow: 'hidden',
              fontSize: 12,
              color: '#666',
              maxHeight: 60,
              lineHeight: '20px',
              display: '-webkit-box',
              WebkitLineClamp: 3,
              WebkitBoxOrient: 'vertical' as const,
            }}>
              {prompt}
            </div>
          </Tooltip>
        );
      },
    },
    {
      title: '运行时状态',
      key: 'runtimeStatus',
      width: 100,
      render: (_: unknown, record: Agent) => {
        const runtimeStatus = runtimeStatuses[record.id];
        const stateInfo = RUNTIME_STATE_MAP[runtimeStatus?.state || 'Uninitialized'];
        return (
          <Space direction="vertical" size={2}>
            <Tag color={stateInfo.color}>
              {stateInfo.label}
            </Tag>
            {runtimeStatus?.isAlive && (
              <Badge status="success" text="运行中" style={{ fontSize: 11 }} />
            )}
          </Space>
        );
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 200,
      fixed: 'right',
      render: (_: unknown, record: Agent) => {
        const runtimeStatus = runtimeStatuses[record.id];
        const isAlive = runtimeStatus?.isAlive;
        const isActivating = activatingAgent === record.id;
        const isTesting = testingAgent === record.id;
        
        return (
          <Space size={0} wrap>
            {isAlive ? (
              <>
                <Tooltip title="测试智能体">
                  <Button 
                    type="link" 
                    size="small" 
                    icon={<ThunderboltOutlined />} 
                    onClick={() => onTest(record.id)}
                    loading={isTesting}
                  >
                    测试
                  </Button>
                </Tooltip>
                <Popconfirm
                  title="确定让智能体休眠？"
                  description="休眠后会释放部分资源，但保留配置"
                  onConfirm={() => onSleep(record.id)}
                  okText="确定"
                  cancelText="取消"
                >
                  <Button type="link" size="small" icon={<PauseCircleOutlined />}>
                    休眠
                  </Button>
                </Popconfirm>
                <Popconfirm
                  title="确定关闭智能体？"
                  description="关闭后会完全释放资源"
                  onConfirm={() => onDestroy(record.id)}
                  okText="确定"
                  cancelText="取消"
                >
                  <Button type="link" size="small" danger icon={<StopOutlined />}>
                    关闭
                  </Button>
                </Popconfirm>
              </>
            ) : (
              <Button 
                type="link" 
                size="small" 
                icon={<PlayCircleOutlined />} 
                onClick={() => onActivate(record.id)}
                loading={isActivating}
              >
                激活
              </Button>
            )}
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
              danger 
              icon={<DeleteOutlined />} 
              onClick={() => onDelete(record.id)}
            >
              删除
            </Button>
          </Space>
        );
      },
    },
  ];

  return (
    <Table
      columns={columns}
      dataSource={agents}
      rowKey="id"
      loading={loading}
      scroll={{ x: 1500 }}
      pagination={{
        current: 1,
        pageSize,
        showSizeChanger: true,
        showQuickJumper: true,
        pageSizeOptions: ['5', '10', '20', '50'],
        showTotal: (total) => `共 ${total} 条`,
        onChange: (_, size) => {
          if (size !== pageSize) {
            onPageSizeChange(size);
          }
        },
      }}
    />
  );
};

export default AgentTable;
