import React, { useState } from 'react';
import { Table, Button, Tag, Space, Tooltip, Typography, Modal, Input, message, Select } from 'antd';
import { EditOutlined, DeleteOutlined, PlayCircleOutlined, ThunderboltOutlined, SendOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { Agent } from '../../services/agentService';
import { AgentRuntimeStatus, agentRuntimeService } from '../../services/agentRuntimeService';
import { AgentType } from '../../services/agentService';
import { LLMConfig, RUNTIME_STATE_MAP } from './types';

const { Text } = Typography;
const { TextArea } = Input;
const { Option } = Select;

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
  onTest: (id: number, input: string) => void;
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
  onTest,
  pageSize,
  onPageSizeChange,
}) => {
  const [testModalVisible, setTestModalVisible] = useState(false);
  const [testingAgentId, setTestingAgentId] = useState<number | null>(null);
  const [selectedAgentForTest, setSelectedAgentForTest] = useState<Agent | null>(null);
  const [testInput, setTestInput] = useState('');
  const [testResponse, setTestResponse] = useState<string>('');
  const [testLoading, setTestLoading] = useState(false);
  const [selectedModel, setSelectedModel] = useState<string>('primary');

  const handleOpenTestModal = (agent: Agent) => {
    setTestingAgentId(agent.id);
    setSelectedAgentForTest(agent);
    setTestInput('');
    setTestResponse('');
    setSelectedModel('primary');
    setTestModalVisible(true);
  };

  const getModelOptions = (agent: Agent) => {
    const options = [];
    
    // 主模型
    if (agent.llmConfigId && agent.llmModelConfigId) {
      options.push({
        value: 'primary',
        label: `主模型: ${agent.llmConfigName || '配置'} - ${agent.primaryModelName || '模型'}`,
        llmConfigId: agent.llmConfigId,
        llmModelConfigId: agent.llmModelConfigId,
      });
    }
    
    // 副模型
    if (agent.fallbackModels && agent.fallbackModels.length > 0) {
      agent.fallbackModels.forEach((fm, index) => {
        options.push({
          value: `fallback_${index}`,
          label: `副模型${index + 1}: ${fm.llmConfigName || '配置'} - ${fm.modelName || '模型'}`,
          llmConfigId: fm.llmConfigId,
          llmModelConfigId: fm.llmModelConfigId || 0,
        });
      });
    }
    
    return options;
  };

  const handleSendTest = async () => {
    if (!testingAgentId || !testInput.trim()) {
      message.warning('请输入测试内容');
      return;
    }

    setTestLoading(true);
    try {
      const modelOptions = selectedAgentForTest ? getModelOptions(selectedAgentForTest) : [];
      const selectedModelOption = modelOptions.find(m => m.value === selectedModel);
      
      const result = await agentRuntimeService.test(
        testingAgentId, 
        testInput,
        selectedModelOption?.llmConfigId,
        selectedModelOption?.llmModelConfigId
      );
      
      if (result.success) {
        setTestResponse(result.response || '无响应内容');
        message.success(`测试成功 (${result.latencyMs}ms)`);
      } else {
        message.error(result.message);
        setTestResponse(`错误: ${result.message}`);
      }
    } catch (error: any) {
      message.error(error.response?.data?.message || '测试失败');
      setTestResponse(`错误: ${error.response?.data?.message || '测试失败'}`);
    } finally {
      setTestLoading(false);
    }
  };

  const handleCloseTestModal = () => {
    setTestModalVisible(false);
    setTestingAgentId(null);
    setSelectedAgentForTest(null);
    setTestInput('');
    setTestResponse('');
    setSelectedModel('primary');
  };
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
      width: 200,
      render: (_: unknown, record: Agent) => {
        if (!record.fallbackModels || record.fallbackModels.length === 0) {
          return <Text type="secondary" style={{ fontSize: 12 }}>无</Text>;
        }
        return (
          <div>
            {record.fallbackModels.map((fm, index) => (
              <div key={index} style={{ marginBottom: index < record.fallbackModels!.length - 1 ? '8px' : 0, display: 'flex', alignItems: 'center', gap: '8px' }}>
                <Tag color="blue" style={{ margin: 0 }}>{fm.llmConfigName || `配置:${fm.llmConfigId}`}</Tag>
                {fm.modelName && (
                  <span style={{ fontSize: 11, color: '#666' }}>
                    {fm.modelName}
                  </span>
                )}
              </div>
            ))}
          </div>
        );
      },
    },
    {
      title: '系统提示词',
      key: 'systemPrompt',
      width: 300,
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
      title: '状态',
      key: 'status',
      width: 100,
      render: (_: unknown, record: Agent) => {
        const runtimeStatus = runtimeStatuses[record.id];
        const stateInfo = RUNTIME_STATE_MAP[runtimeStatus?.state || 'Uninitialized'];
        const runtimeDesc: Record<string, string> = {
          Uninitialized: '智能体尚未启动，点击"激活"按钮启动',
          Ready: '智能体已启动，随时可以执行任务',
          Busy: '智能体正在执行任务，请等待完成',
          Error: '智能体运行出错，需要检查日志',
        };
        return (
          <Tooltip title={runtimeDesc[runtimeStatus?.state || 'Uninitialized']}>
            <Tag color={stateInfo.color}>
              {stateInfo.label}
            </Tag>
          </Tooltip>
        );
      },
    },
    {
      title: '操作',
      key: 'action',
      width: 180,
      fixed: 'right',
      render: (_: unknown, record: Agent) => {
        const isActivating = activatingAgent === record.id;
        const isTesting = testingAgent === record.id;
        
        return (
          <Space size={0} wrap>
            <Tooltip title="测试大模型连通性">
              <Button 
                type="link" 
                size="small" 
                icon={<PlayCircleOutlined />} 
                onClick={() => onActivate(record.id)}
                loading={isActivating}
              >
                激活
              </Button>
            </Tooltip>
            <Tooltip title="测试智能体">
              <Button 
                type="link" 
                size="small" 
                icon={<ThunderboltOutlined />} 
                onClick={() => handleOpenTestModal(record)}
                loading={testingAgent === record.id}
              >
                测试
              </Button>
            </Tooltip>
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
    <>
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
      
      <Modal
        title="测试智能体"
        open={testModalVisible}
        onCancel={handleCloseTestModal}
        width={700}
        footer={[
          <Button key="cancel" onClick={handleCloseTestModal}>
            关闭
          </Button>,
        ]}
      >
        {selectedAgentForTest && (
          <div style={{ marginBottom: 16 }}>
            <Text strong>选择测试模型：</Text>
            <Select
              value={selectedModel}
              onChange={setSelectedModel}
              style={{ width: '100%', marginTop: 8 }}
              placeholder="请选择要测试的模型"
            >
              {getModelOptions(selectedAgentForTest).map(option => (
                <Option key={option.value} value={option.value}>
                  {option.label}
                </Option>
              ))}
            </Select>
          </div>
        )}
        
        <div style={{ marginBottom: 16 }}>
          <Text strong>输入测试内容：</Text>
          <div style={{ marginTop: 8, display: 'flex', gap: 8 }}>
            <TextArea
              value={testInput}
              onChange={(e) => setTestInput(e.target.value)}
              placeholder="请输入要测试的内容，例如：你好，请介绍一下你自己"
              autoSize={{ minRows: 3, maxRows: 6 }}
              style={{ flex: 1 }}
              onPressEnter={(e) => {
                if (!e.shiftKey) {
                  e.preventDefault();
                  handleSendTest();
                }
              }}
            />
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSendTest}
              loading={testLoading}
              style={{ height: 'auto' }}
            >
              发送
            </Button>
          </div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            提示：按 Enter 发送，Shift+Enter 换行
          </Text>
        </div>

        {testResponse && (
          <div>
            <Text strong>智能体响应：</Text>
            <div 
              style={{ 
                marginTop: 8,
                background: '#f5f5f5', 
                padding: 12, 
                borderRadius: 4,
                maxHeight: 400,
                overflow: 'auto',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word'
              }}
            >
              {testResponse}
            </div>
          </div>
        )}
      </Modal>
    </>
  );
};

export default AgentTable;
