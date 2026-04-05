import React, { useEffect, useState } from 'react';
import { Table, Avatar, Tag, Empty, Spin, message, Card, Button, Typography, Space, Descriptions, List } from 'antd';
import { UserOutlined, RobotOutlined, TeamOutlined, ArrowLeftOutlined, PlayCircleOutlined, CheckCircleOutlined, ClockCircleOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { collaborationService } from '../../services/collaborationService';

const { Text } = Typography;

interface SessionMetadata {
  workflowMode: string;
  orchestrationMode: string;
  maxIterations: number;
  managerNames: string[];
  workerNames: string[];
  totalAgents: number;
}

interface WorkflowSession {
  id: number;
  collaborationId: number;
  taskId: number | null;
  workflowType: string;
  orchestrationMode: string | null;
  status: string;
  topic: string | null;
  metadata: string | null;
  totalRounds: number;
  totalMessages: number;
  conclusion: string | null;
  errorMessage: string | null;
  startedAt: string;
  completedAt: string | null;
  createdAt: string;
}

interface Message {
  id: number;
  sessionId: number | null;
  collaborationId: number | null;
  taskId: number | null;
  messageType: string | null;
  roundNumber: number | null;
  stepNumber: number | null;
  fromAgentId: number | null;
  fromAgentName: string | null;
  fromAgentRole: string | null;
  toAgentId: number | null;
  content: string;
  thinkingProcess: string | null;
  selectedNextSpeaker: string | null;
  selectionReason: string | null;
  metadata: string | null;
  createdAt: string;
}

interface ChatHistoryProps {
  taskId?: string;
  collaborationId?: string;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ taskId, collaborationId }) => {
  const [sessions, setSessions] = useState<WorkflowSession[]>([]);
  const [selectedSession, setSelectedSession] = useState<WorkflowSession | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingMessages, setLoadingMessages] = useState(false);

  useEffect(() => {
    loadSessions();
  }, [taskId, collaborationId]);

  const loadSessions = async () => {
    setLoading(true);
    try {
      let data: WorkflowSession[] = [];
      if (taskId) {
        data = await collaborationService.getTaskSessions(taskId);
      } else if (collaborationId) {
        data = await collaborationService.getCoordinationSessions(collaborationId);
      }
      setSessions(data);
    } catch (error) {
      message.error('加载执行记录失败');
    } finally {
      setLoading(false);
    }
  };

  const loadMessages = async (sessionId: number) => {
    setLoadingMessages(true);
    try {
      const data = await collaborationService.getSessionMessages(String(sessionId));
      setMessages(data);
    } catch (error) {
      message.error('加载消息记录失败');
    } finally {
      setLoadingMessages(false);
    }
  };

  const handleSelectSession = (session: WorkflowSession) => {
    setSelectedSession(session);
    loadMessages(session.id);
  };

  const handleBack = () => {
    setSelectedSession(null);
    setMessages([]);
  };

  const parseMetadata = (metadataStr: string | null): SessionMetadata | null => {
    if (!metadataStr) return null;
    try {
      return JSON.parse(metadataStr);
    } catch {
      return null;
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return <PlayCircleOutlined style={{ color: '#1890ff' }} />;
      case 'completed':
        return <CheckCircleOutlined style={{ color: '#52c41a' }} />;
      default:
        return <ClockCircleOutlined style={{ color: '#faad14' }} />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return 'processing';
      case 'completed':
        return 'success';
      default:
        return 'warning';
    }
  };

  const getStatusText = (status: string) => {
    switch (status.toLowerCase()) {
      case 'running':
        return '执行中';
      case 'completed':
        return '已完成';
      default:
        return status;
    }
  };

  const getWorkflowTypeDisplay = (session: WorkflowSession, metadata: SessionMetadata | null) => {
    if (session.workflowType === 'Magentic') {
      return { text: '智能工作流', color: 'purple' };
    }
    
    const modeMap: Record<string, string> = {
      'roundrobin': '轮询',
      'manager': '协调者',
      'intelligent': '智能'
    };
    
    const mode = (session.orchestrationMode || metadata?.orchestrationMode || 'RoundRobin').toLowerCase();
    const modeText = modeMap[mode] || '轮询';
    
    return { text: `群聊-${modeText}`, color: 'blue' };
  };

  const getSpeakerIcon = (speakerName: string | null) => {
    if (!speakerName) return <RobotOutlined />;
    if (speakerName.includes('Manager') || speakerName.includes('协调')) {
      return <TeamOutlined />;
    }
    return <RobotOutlined />;
  };

  const getSequenceNumber = (msg: Message) => {
    if (msg.roundNumber) return `第 ${msg.roundNumber} 轮`;
    if (msg.stepNumber) return `第 ${msg.stepNumber} 步`;
    return '';
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (selectedSession) {
    const metadata = parseMetadata(selectedSession.metadata);
    
    return (
      <div>
        <Button 
          type="link" 
          icon={<ArrowLeftOutlined />} 
          onClick={handleBack}
          style={{ marginBottom: 16, paddingLeft: 0 }}
        >
          返回执行记录列表
        </Button>
        
        <Card size="small" style={{ marginBottom: 16 }}>
          <Descriptions size="small" column={2}>
            <Descriptions.Item label="状态">
              <Tag color={getStatusColor(selectedSession.status)}>
                {getStatusText(selectedSession.status)}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="工作流类型">
              <Tag color={getWorkflowTypeDisplay(selectedSession, metadata).color}>
                {getWorkflowTypeDisplay(selectedSession, metadata).text}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="开始时间">
              {new Date(selectedSession.startedAt).toLocaleString('zh-CN')}
            </Descriptions.Item>
            <Descriptions.Item label="结束时间">
              {selectedSession.completedAt ? new Date(selectedSession.completedAt).toLocaleString('zh-CN') : '-'}
            </Descriptions.Item>
            <Descriptions.Item label="总消息数">{selectedSession.totalMessages}</Descriptions.Item>
            {metadata && (
              <>
                <Descriptions.Item label="协调者">
                  {metadata.managerNames?.length > 0 ? metadata.managerNames.join('、') : '-'}
                </Descriptions.Item>
                <Descriptions.Item label="执行者">
                  {metadata.workerNames?.length > 0 ? metadata.workerNames.join('、') : '-'}
                </Descriptions.Item>
              </>
            )}
          </Descriptions>
        </Card>

        {loadingMessages ? (
          <div style={{ textAlign: 'center', padding: '30px' }}>
            <Spin />
          </div>
        ) : messages.length === 0 ? (
          <Empty description="暂无消息记录" image={Empty.PRESENTED_IMAGE_SIMPLE} />
        ) : (
          <List
            itemLayout="horizontal"
            dataSource={messages}
            renderItem={(item) => (
              <List.Item>
                <List.Item.Meta
                  avatar={
                    <Avatar
                      icon={getSpeakerIcon(item.fromAgentName)}
                      style={{ backgroundColor: '#52c41a' }}
                    />
                  }
                  title={
                    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                      <span>{item.fromAgentName || 'System'}</span>
                      {item.fromAgentRole && (
                        <Tag color="blue">{item.fromAgentRole}</Tag>
                      )}
                      {getSequenceNumber(item) && (
                        <Tag>{getSequenceNumber(item)}</Tag>
                      )}
                      <span style={{ fontSize: 12, color: '#999' }}>
                        {new Date(item.createdAt).toLocaleString('zh-CN')}
                      </span>
                    </div>
                  }
                  description={
                    <div style={{ 
                      whiteSpace: 'pre-wrap', 
                      wordBreak: 'break-word',
                      backgroundColor: '#f5f5f5',
                      padding: 12,
                      borderRadius: 8,
                      marginTop: 8
                    }}>
                      {item.content}
                    </div>
                  }
                />
              </List.Item>
            )}
          />
        )}
      </div>
    );
  }

  const columns: ColumnsType<WorkflowSession> = [
    {
      title: '工作流类型',
      dataIndex: 'workflowType',
      key: 'workflowType',
      width: 140,
      render: (_, record) => {
        const metadata = parseMetadata(record.metadata);
        const display = getWorkflowTypeDisplay(record, metadata);
        return (
          <Tag color={display.color}>
            {display.text}
          </Tag>
        );
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => (
        <Tag color={getStatusColor(status)} icon={getStatusIcon(status)}>
          {getStatusText(status)}
        </Tag>
      ),
    },
    {
      title: '协调者',
      key: 'managers',
      width: 150,
      render: (_, record) => {
        const metadata = parseMetadata(record.metadata);
        if (!metadata?.managerNames?.length) return <Text type="secondary">-</Text>;
        return <Text>{metadata.managerNames.join('、')}</Text>;
      },
    },
    {
      title: '执行者',
      key: 'workers',
      ellipsis: true,
      render: (_, record) => {
        const metadata = parseMetadata(record.metadata);
        if (!metadata?.workerNames?.length) return <Text type="secondary">-</Text>;
        const names = metadata.workerNames;
        if (names.length > 4) {
          return <Text>{names.slice(0, 4).join('、')} 等{names.length}人</Text>;
        }
        return <Text>{names.join('、')}</Text>;
      },
    },
    {
      title: '消息数',
      dataIndex: 'totalMessages',
      key: 'totalMessages',
      width: 80,
      align: 'center',
      render: (count: number) => <Text>{count}</Text>,
    },
    {
      title: '执行时间',
      dataIndex: 'startedAt',
      key: 'startedAt',
      width: 170,
      render: (time: string) => new Date(time).toLocaleString('zh-CN'),
    },
  ];

  if (sessions.length === 0) {
    return (
      <Empty
        description="暂无执行记录，请先执行任务"
        image={Empty.PRESENTED_IMAGE_SIMPLE}
      />
    );
  }

  return (
    <Table
      columns={columns}
      dataSource={sessions}
      rowKey="id"
      size="small"
      pagination={{ pageSize: 10, showSizeChanger: false }}
      onRow={(record) => ({
        onClick: () => handleSelectSession(record),
        style: { cursor: 'pointer' },
      })}
    />
  );
};

export default ChatHistory;
