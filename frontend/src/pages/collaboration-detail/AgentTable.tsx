import React, { useState } from 'react';
import { Table, Button, Popconfirm, Select, Input, message, Tag, Space, Tooltip } from 'antd';
import { DeleteOutlined, SaveOutlined, TeamOutlined, UserOutlined, InfoCircleOutlined, EditOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { CollaborationAgent, AGENT_STATUS_COLOR_MAP } from './types';
import { collaborationService } from '../../services/collaborationService';

const { TextArea } = Input;
const { Option } = Select;

interface AgentTableProps {
  agents: CollaborationAgent[];
  collaborationId: number;
  onRemove: (agentId: number) => void;
  onUpdate: () => void;
}

const AgentTable: React.FC<AgentTableProps> = ({ agents, collaborationId, onRemove, onUpdate }) => {
  const [editingAgentId, setEditingAgentId] = useState<number | null>(null);
  const [editingRole, setEditingRole] = useState<string>('');
  const [editingCustomPrompt, setEditingCustomPrompt] = useState<string>('');
  const [saving, setSaving] = useState(false);

  const handleEdit = (agent: CollaborationAgent) => {
    setEditingAgentId(agent.agentId);
    setEditingRole(agent.role || 'Worker');
    // 如果有自定义提示词则使用，否则使用系统提示词
    setEditingCustomPrompt(agent.customPrompt || agent.systemPrompt || '');
  };

  const handleSave = async (agentId: number) => {
    setSaving(true);
    try {
      await collaborationService.updateAgentRole(collaborationId, agentId, {
        role: editingRole,
        customPrompt: editingCustomPrompt,
      });
      message.success('角色更新成功');
      setEditingAgentId(null);
      onUpdate();
    } catch (error: any) {
      message.error(`更新失败: ${error.message}`);
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setEditingAgentId(null);
    setEditingRole('');
    setEditingCustomPrompt('');
  };

  const columns: ColumnsType<CollaborationAgent> = [
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
      render: (role: string, record: CollaborationAgent) => {
        if (editingAgentId === record.agentId) {
          return (
            <Select
              value={editingRole}
              onChange={setEditingRole}
              style={{ width: '100%' }}
            >
              <Option value="Manager">
                <Space>
                  <TeamOutlined />
                  <span>Manager（协调者）</span>
                </Space>
              </Option>
              <Option value="Worker">
                <Space>
                  <UserOutlined />
                  <span>Worker（执行者）</span>
                </Space>
              </Option>
            </Select>
          );
        }

        const isManager = role === 'Manager';
        return (
          <Tag color={isManager ? 'blue' : 'green'} icon={isManager ? <TeamOutlined /> : <UserOutlined />}>
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
      render: (customPrompt: string, record: CollaborationAgent) => {
        if (editingAgentId === record.agentId) {
          return (
            <div>
              <div style={{ marginBottom: 8, padding: 8, backgroundColor: '#e6f7ff', borderRadius: 4, fontSize: 12 }}>
                <div style={{ marginBottom: 4, fontWeight: 500, color: '#1890ff' }}>
                  <InfoCircleOutlined style={{ marginRight: 4 }} />提示词变量说明
                </div>
                <div style={{ marginTop: 8 }}>
                  <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_name}}"}</code>
                  <span style={{ marginLeft: 8, color: '#666' }}>当前智能体名称</span>
                </div>
                <div style={{ marginTop: 4 }}>
                  <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_role}}"}</code>
                  <span style={{ marginLeft: 8, color: '#666' }}>当前智能体角色（Manager/Worker）</span>
                </div>
                <div style={{ marginTop: 4 }}>
                  <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{agent_type}}"}</code>
                  <span style={{ marginLeft: 8, color: '#666' }}>当前智能体类型（如：架构师）</span>
                </div>
                <div style={{ marginTop: 4 }}>
                  <code style={{ backgroundColor: '#f0f0f0', padding: '2px 6px', borderRadius: 2 }}>{"{{members}}"}</code>
                  <span style={{ marginLeft: 8, color: '#666' }}>团队成员列表（名称+类型）</span>
                </div>
              </div>
              <TextArea
                value={editingCustomPrompt}
                onChange={(e) => setEditingCustomPrompt(e.target.value)}
                placeholder="自定义提示词（默认使用系统提示词）"
                rows={4}
                showCount
                maxLength={2000}
              />
            </div>
          );
        }
        
        const displayText = customPrompt || record.systemPrompt || '-';
        const isTruncated = displayText.length > 100;
        const isFromSystemPrompt = !customPrompt && record.systemPrompt;
        
        return (
          <Tooltip title={isTruncated ? displayText : ''}>
            <div style={{ 
              maxHeight: 80, 
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'pre-wrap',
              fontSize: 12,
              color: customPrompt ? '#000' : '#999'
            }}>
              {isTruncated ? `${displayText.substring(0, 100)}...` : displayText}
              {isFromSystemPrompt && (
                <div style={{ fontSize: 10, color: '#999', marginTop: 4 }}>
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
        const color = AGENT_STATUS_COLOR_MAP[status || 'Inactive'];
        const text = status || 'Inactive';
        return (
          <Tag color={color}>
            {text}
          </Tag>
        );
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
      render: (_: unknown, record: CollaborationAgent) => {
        if (editingAgentId === record.agentId) {
          return (
            <Space>
              <Button
                type="primary"
                icon={<SaveOutlined />}
                size="small"
                onClick={() => handleSave(record.agentId)}
                loading={saving}
              >
                保存
              </Button>
              <Button size="small" onClick={handleCancel}>
                取消
              </Button>
            </Space>
          );
        }

        return (
          <Space>
            <Button size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
              编辑角色
            </Button>
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
          </Space>
        );
      },
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16 }}>
        <Tag color="blue" icon={<TeamOutlined />}>Manager（协调者）</Tag>
        <span style={{ marginLeft: 8, color: '#666' }}>
          负责协调Worker Agents，分配任务，合并结果
        </span>
      </div>
      <div style={{ marginBottom: 16 }}>
        <Tag color="green" icon={<UserOutlined />}>Worker（执行者）</Tag>
        <span style={{ marginLeft: 8, color: '#666' }}>
          执行Manager分配的具体任务
        </span>
      </div>
      <div style={{ marginBottom: 16, padding: 12, backgroundColor: '#f6ffed', border: '1px solid #b7eb8f', borderRadius: 4 }}>
        <div style={{ marginBottom: 8, fontWeight: 500, color: '#52c41a' }}>
          <InfoCircleOutlined style={{ marginRight: 4 }} /> 提示词变量说明
        </div>
        <div style={{ fontSize: 12, color: '#666', marginBottom: 8 }}>
          在自定义提示词中可使用以下变量，系统会自动替换：
        </div>
        <div style={{ fontSize: 12, backgroundColor: '#fff', padding: 8, borderRadius: 4, border: '1px solid #d9d9d9' }}>
          <code style={{ color: '#1890ff', fontWeight: 'bold' }}>{"{{members}}"}</code>
          <span style={{ marginLeft: 8, color: '#666' }}>→ 团队成员列表（名称+类型）</span>
        </div>
        <div style={{ fontSize: 11, color: '#999', marginTop: 8 }}>
          示例输出：<br/>
          <span style={{ marginLeft: 56 }}>- abc（项目经理）</span><br/>
          <span style={{ marginLeft: 56 }}>- 123（架构师）</span>
        </div>
      </div>
      <Table
        dataSource={agents}
        columns={columns}
        rowKey="agentId"
        pagination={false}
      />
    </div>
  );
};

export default AgentTable;
