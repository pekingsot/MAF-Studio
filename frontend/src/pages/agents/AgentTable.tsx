import React, { useState, useMemo } from 'react';
import { Table, Button, Tag, Space, Tooltip, Typography, Modal, Input, message, Select } from 'antd';
import { EditOutlined, DeleteOutlined, PlayCircleOutlined, ThunderboltOutlined, SendOutlined, StarOutlined, StarFilled, ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { Agent } from '../../services/agentService';
import { AgentRuntimeStatus, agentRuntimeService } from '../../services/agentRuntimeService';
import { AgentType } from '../../services/agentService';
import { LLMConfig, RUNTIME_STATE_MAP } from './types';
import { agentService } from '../../services/agentService';

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
  const [updatedLlmConfigs, setUpdatedLlmConfigs] = useState<Record<number, any[]>>({});
  const [expandedAgents, setExpandedAgents] = useState<Set<number>>(new Set());

  const mergedAgents = useMemo(() => {
    return agents.map(agent => {
      if (updatedLlmConfigs[agent.id]) {
        return { ...agent, llmConfigs: updatedLlmConfigs[agent.id] };
      }
      return agent;
    });
  }, [agents, updatedLlmConfigs]);

  const handleSetPrimaryModel = async (agentId: number, llmConfigId: number, llmModelConfigId: number) => {
    try {
      const agent = mergedAgents.find(a => a.id === agentId);
      if (!agent || !agent.llmConfigs) {
        message.error('智能体信息不存在');
        return;
      }

      const updatedConfigs = agent.llmConfigs.map(lc => ({
        ...lc,
        isPrimary: lc.llmConfigId === llmConfigId && lc.llmModelConfigId === llmModelConfigId,
      }));

      const primaryModelIndex = updatedConfigs.findIndex(lc => lc.isPrimary);
      if (primaryModelIndex === -1) {
        message.error('未找到主模型');
        return;
      }

      const primaryModel = updatedConfigs[primaryModelIndex];
      const reorderedConfigs = [
        primaryModel,
        ...updatedConfigs.filter((_, index) => index !== primaryModelIndex)
      ].map((lc, index) => ({
        ...lc,
        priority: index === 0 ? 0 : index,
      }));

      await agentService.updateAgent(agentId, {
        name: agent.name,
        description: agent.description,
        systemPrompt: agent.systemPrompt,
        avatar: agent.avatar,
        llmConfigId: primaryModel.llmConfigId,
        llmModelConfigId: primaryModel.llmModelConfigId || 0,
        llmConfigs: JSON.stringify(reorderedConfigs),
      });

      setUpdatedLlmConfigs(prev => ({
        ...prev,
        [agentId]: reorderedConfigs,
      }));

      message.success('主模型设置成功');
    } catch (error: any) {
      console.error('设置主模型失败:', error);
      message.error(`设置主模型失败: ${error.message || '未知错误'}`);
    }
  };

  const handleMoveUp = async (agentId: number, index: number) => {
    try {
      const agent = mergedAgents.find(a => a.id === agentId);
      if (!agent || !agent.llmConfigs || index <= 1) {
        return;
      }

      const newConfigs = [...agent.llmConfigs];
      [newConfigs[index - 1], newConfigs[index]] = [newConfigs[index], newConfigs[index - 1]];

      const reorderedConfigs = newConfigs.map((lc, idx) => ({
        ...lc,
        priority: idx === 0 ? 0 : idx,
      }));

      await agentService.updateAgent(agentId, {
        name: agent.name,
        description: agent.description,
        systemPrompt: agent.systemPrompt,
        avatar: agent.avatar,
        llmConfigId: reorderedConfigs[0].llmConfigId,
        llmModelConfigId: reorderedConfigs[0].llmModelConfigId || 0,
        llmConfigs: JSON.stringify(reorderedConfigs),
      });

      setUpdatedLlmConfigs(prev => ({
        ...prev,
        [agentId]: reorderedConfigs,
      }));

      message.success('上移成功');
    } catch (error: any) {
      console.error('上移失败:', error);
      message.error(`上移失败: ${error.message || '未知错误'}`);
    }
  };

  const handleMoveDown = async (agentId: number, index: number) => {
    try {
      const agent = mergedAgents.find(a => a.id === agentId);
      if (!agent || !agent.llmConfigs || index <= 0 || index >= agent.llmConfigs.length - 1) {
        return;
      }

      const newConfigs = [...agent.llmConfigs];
      [newConfigs[index], newConfigs[index + 1]] = [newConfigs[index + 1], newConfigs[index]];

      const reorderedConfigs = newConfigs.map((lc, idx) => ({
        ...lc,
        priority: idx === 0 ? 0 : idx,
      }));

      await agentService.updateAgent(agentId, {
        name: agent.name,
        description: agent.description,
        systemPrompt: agent.systemPrompt,
        avatar: agent.avatar,
        llmConfigId: reorderedConfigs[0].llmConfigId,
        llmModelConfigId: reorderedConfigs[0].llmModelConfigId || 0,
        llmConfigs: JSON.stringify(reorderedConfigs),
      });

      setUpdatedLlmConfigs(prev => ({
        ...prev,
        [agentId]: reorderedConfigs,
      }));

      message.success('下移成功');
    } catch (error: any) {
      console.error('下移失败:', error);
      message.error(`下移失败: ${error.message || '未知错误'}`);
    }
  };

  const handleOpenTestModal = (agent: Agent) => {
    setTestingAgentId(agent.id);
    setSelectedAgentForTest(agent);
    setTestInput('');
    setTestResponse('');
    setSelectedModel('primary');
    setTestModalVisible(true);
  };

  const getModelOptions = (agent: Agent) => {
    const options: Array<{
      value: string;
      label: string;
      llmConfigId: number;
      llmModelConfigId: number;
    }> = [];
    
    if (agent.llmConfigs && agent.llmConfigs.length > 0) {
      agent.llmConfigs.forEach((lc, index) => {
        options.push({
          value: lc.isPrimary ? 'primary' : `fallback_${index}`,
          label: `${lc.isPrimary ? '主模型' : `副模型${index + 1}`}: ${lc.llmConfigName} - ${lc.modelName}`,
          llmConfigId: lc.llmConfigId,
          llmModelConfigId: lc.llmModelConfigId || 0,
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
      width: 120,
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
      title: '大模型配置',
      key: 'llmConfigs',
      width: 280,
      render: (_: unknown, record: Agent) => {
        const models: Array<{
          llmConfigName: string;
          modelName: string;
          isPrimary: boolean;
          isValid: boolean;
          msg: string;
        }> = [];
        
        if (record.llmConfigs && record.llmConfigs.length > 0) {
          record.llmConfigs.forEach(lc => {
            models.push({
              llmConfigName: lc.llmConfigName,
              modelName: lc.modelName,
              isPrimary: lc.isPrimary,
              isValid: lc.isValid,
              msg: lc.msg,
            });
          });
        }
        
        if (models.length === 0) {
          return <Tag color="red">未配置</Tag>;
        }
        
        const isExpanded = expandedAgents.has(record.id);
        const defaultShowCount = 3;
        const displayModels = isExpanded ? models : models.slice(0, defaultShowCount);
        const hasMore = models.length > defaultShowCount;
        
        return (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
            {displayModels.map((model, index) => {
              const msgDisplay = model.msg.length > 20 
                ? `${model.msg.substring(0, 20)}...` 
                : model.msg;
              
              const tagColor = model.isPrimary 
                ? 'green' 
                : (model.isValid ? 'blue' : 'red');
              
              return (
                <React.Fragment key={index}>
                  <div style={{ 
                    display: 'flex', 
                    alignItems: 'center', 
                    gap: '8px',
                    padding: '4px 0',
                  }}>
                    <Tooltip title={model.isPrimary ? '主模型' : '点击设为主模型'}>
                      {model.isPrimary ? (
                        <StarFilled 
                          style={{ 
                            color: '#ff4d4f', 
                            fontSize: 14,
                            cursor: 'default',
                          }}
                        />
                      ) : (
                        <StarOutlined 
                          style={{ 
                            color: '#1890ff', 
                            fontSize: 14,
                            cursor: 'pointer',
                          }}
                          onClick={() => {
                            if (record.llmConfigs) {
                              const llmConfig = record.llmConfigs[index];
                              handleSetPrimaryModel(record.id, llmConfig.llmConfigId, llmConfig.llmModelConfigId || 0);
                            }
                          }}
                        />
                      )}
                    </Tooltip>
                    {model.isPrimary ? (
                      <span style={{ fontSize: 12, color: '#ff4d4f', fontWeight: 500 }}>主模型</span>
                    ) : (
                      <>
                        <Tooltip title="上移">
                          <ArrowUpOutlined 
                            style={{ 
                              fontSize: 12, 
                              color: '#999',
                              cursor: index === 1 ? 'not-allowed' : 'pointer',
                              opacity: index === 1 ? 0.3 : 1,
                            }}
                            onClick={() => {
                              if (index > 1) {
                                handleMoveUp(record.id, index);
                              }
                            }}
                          />
                        </Tooltip>
                        <Tooltip title="下移">
                          <ArrowDownOutlined 
                            style={{ 
                              fontSize: 12, 
                              color: '#999',
                              cursor: record.llmConfigs && index === record.llmConfigs.length - 1 ? 'not-allowed' : 'pointer',
                              opacity: record.llmConfigs && index === record.llmConfigs.length - 1 ? 0.3 : 1,
                            }}
                            onClick={() => {
                              if (record.llmConfigs && index < record.llmConfigs.length - 1) {
                                handleMoveDown(record.id, index);
                              }
                            }}
                          />
                        </Tooltip>
                      </>
                    )}
                    <Tag color={tagColor} style={{ margin: 0, minWidth: 60 }}>
                      {model.llmConfigName}
                    </Tag>
                    <span style={{ fontSize: 12, color: '#666' }}>
                      {model.modelName}
                    </span>
                    {model.msg && (
                      <Tooltip title={model.msg}>
                        <span style={{ 
                          fontSize: 11, 
                          color: model.isValid ? '#52c41a' : '#ff4d4f',
                          marginLeft: '32px',
                          maxWidth: 100,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                        }}>
                          {msgDisplay}
                        </span>
                      </Tooltip>
                    )}
                  </div>
                  {model.isPrimary && models.length > 1 && (
                    <div style={{ 
                      borderBottom: '1px solid #1890ff', 
                      margin: '4px 0',
                      width: '100%',
                    }} />
                  )}
                </React.Fragment>
              );
            })}
            {hasMore && (
              <Button 
                type="link" 
                size="small"
                style={{ padding: 0, height: 'auto', fontSize: 12 }}
                onClick={() => {
                  setExpandedAgents(prev => {
                    const newSet = new Set(prev);
                    if (newSet.has(record.id)) {
                      newSet.delete(record.id);
                    } else {
                      newSet.add(record.id);
                    }
                    return newSet;
                  });
                }}
              >
                {isExpanded ? '收起' : `查看更多 (${models.length - defaultShowCount}个)`}
              </Button>
            )}
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
          <Tooltip 
            overlayStyle={{ maxWidth: 800 }}
            title={<div style={{ whiteSpace: 'pre-wrap' }}>{prompt}</div>} 
            placement="topLeft"
          >
            <div style={{ 
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              overflow: 'hidden',
              fontSize: 12,
              color: '#666',
              lineHeight: '20px',
              maxHeight: '200px',
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
      width: 80,
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
        dataSource={mergedAgents}
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
