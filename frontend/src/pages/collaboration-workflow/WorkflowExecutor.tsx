import React, { useState } from 'react';
import { Card, Button, Input, Select, message, Spin, Divider, List, Tag, Space, Typography } from 'antd';
import { PlayCircleOutlined, ThunderboltOutlined, SwapOutlined, TeamOutlined } from '@ant-design/icons';
import { collaborationWorkflowService, CollaborationResult } from '../../services/collaborationWorkflowService';

const { TextArea } = Input;
const { Option } = Select;
const { Title, Text } = Typography;

interface WorkflowExecutorProps {
  collaborationId: number;
  collaborationName: string;
}

const WorkflowExecutor: React.FC<WorkflowExecutorProps> = ({ collaborationId, collaborationName }) => {
  const [workflowType, setWorkflowType] = useState<'sequential' | 'concurrent' | 'handoffs' | 'groupchat'>('sequential');
  const [input, setInput] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CollaborationResult | null>(null);

  const workflowIcons = {
    sequential: <PlayCircleOutlined />,
    concurrent: <ThunderboltOutlined />,
    handoffs: <SwapOutlined />,
    groupchat: <TeamOutlined />,
  };

  const workflowNames = {
    sequential: '顺序执行',
    concurrent: '并发执行',
    handoffs: '任务移交',
    groupchat: '群聊协作',
  };

  const workflowDescriptions = {
    sequential: '多个Agent按顺序依次执行任务',
    concurrent: '多个Agent同时执行任务，最后合并结果',
    handoffs: 'Agent之间相互移交任务',
    groupchat: '多个Agent进行群聊协作',
  };

  const handleExecute = async () => {
    if (!input.trim()) {
      message.warning('请输入任务内容');
      return;
    }

    setLoading(true);
    setResult(null);

    try {
      let response: CollaborationResult;

      switch (workflowType) {
        case 'sequential':
          response = await collaborationWorkflowService.executeSequential(collaborationId, input);
          break;
        case 'concurrent':
          response = await collaborationWorkflowService.executeConcurrent(collaborationId, input);
          break;
        case 'handoffs':
          response = await collaborationWorkflowService.executeHandoffs(collaborationId, input);
          break;
        case 'groupchat':
          await collaborationWorkflowService.executeGroupChat(collaborationId, input);
          response = {
            success: true,
            output: '群聊工作流已启动，请查看实时消息流',
            messages: [],
          };
          break;
        default:
          throw new Error('未知的工作流类型');
      }

      setResult(response);

      if (response.success) {
        message.success('工作流执行成功');
      } else {
        message.error(`工作流执行失败: ${response.error}`);
      }
    } catch (error: any) {
      message.error(`执行失败: ${error.message}`);
      setResult({
        success: false,
        output: '',
        messages: [],
        error: error.message,
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <Card title={`协作工作流 - ${collaborationName}`}>
        <Space direction="vertical" style={{ width: '100%' }} size="large">
          <div>
            <Title level={5}>选择工作流类型</Title>
            <Select
              value={workflowType}
              onChange={setWorkflowType}
              style={{ width: '100%' }}
            >
              {Object.entries(workflowNames).map(([key, name]) => (
                <Option key={key} value={key}>
                  <Space>
                    {workflowIcons[key as keyof typeof workflowIcons]}
                    <span>{name}</span>
                  </Space>
                </Option>
              ))}
            </Select>
            <Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
              {workflowDescriptions[workflowType]}
            </Text>
          </div>

          <Divider />

          <div>
            <Title level={5}>任务输入</Title>
            <TextArea
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="请输入任务内容..."
              rows={6}
              style={{ marginBottom: 16 }}
            />
            <Button
              type="primary"
              icon={workflowIcons[workflowType]}
              onClick={handleExecute}
              loading={loading}
              size="large"
              block
            >
              执行工作流
            </Button>
          </div>

          {loading && (
            <div style={{ textAlign: 'center', padding: '40px 0' }}>
              <Spin size="large" />
              <div style={{ marginTop: 16 }}>
                <Text>正在执行工作流，请稍候...</Text>
              </div>
            </div>
          )}

          {result && !loading && (
            <>
              <Divider />
              <div>
                <Title level={5}>
                  执行结果
                  <Tag color={result.success ? 'success' : 'error'} style={{ marginLeft: 8 }}>
                    {result.success ? '成功' : '失败'}
                  </Tag>
                </Title>

                {result.output && (
                  <Card size="small" title="最终输出" style={{ marginBottom: 16 }}>
                    <Text>{result.output}</Text>
                  </Card>
                )}

                {result.error && (
                  <Card size="small" title="错误信息" style={{ marginBottom: 16 }}>
                    <Text type="danger">{result.error}</Text>
                  </Card>
                )}

                {result.messages && result.messages.length > 0 && (
                  <Card size="small" title="执行过程">
                    <List
                      dataSource={result.messages}
                      renderItem={(msg, index) => (
                        <List.Item key={index}>
                          <List.Item.Meta
                            avatar={<Tag color="blue">{msg.sender}</Tag>}
                            description={
                              <>
                                <Text>{msg.content}</Text>
                                <br />
                                <Text type="secondary" style={{ fontSize: 12 }}>
                                  {new Date(msg.timestamp).toLocaleString()}
                                </Text>
                              </>
                            }
                          />
                        </List.Item>
                      )}
                    />
                  </Card>
                )}
              </div>
            </>
          )}
        </Space>
      </Card>
    </div>
  );
};

export default WorkflowExecutor;
