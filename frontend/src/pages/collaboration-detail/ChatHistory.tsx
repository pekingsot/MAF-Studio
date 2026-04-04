import React, { useEffect, useState } from 'react';
import { List, Avatar, Tag, Empty, Spin, message } from 'antd';
import { UserOutlined, RobotOutlined, TeamOutlined } from '@ant-design/icons';
import { collaborationService } from '../../services/collaborationService';

interface ChatMessage {
  id: number;
  fromAgentId?: number;
  toAgentId?: number;
  collaborationId: number;
  content: string;
  senderType: string | number;
  senderName?: string;
  userId?: string;
  isStreaming: boolean;
  createdAt: string;
}

interface ChatHistoryProps {
  collaborationId: string;
}

const ChatHistory: React.FC<ChatHistoryProps> = ({ collaborationId }) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadMessages();
  }, [collaborationId]);

  const loadMessages = async () => {
    setLoading(true);
    try {
      const data = await collaborationService.getCollaborationMessages(collaborationId);
      setMessages(data);
    } catch (error) {
      message.error('加载聊天记录失败');
    } finally {
      setLoading(false);
    }
  };

  const getSenderIcon = (senderType: string | number) => {
    const type = typeof senderType === 'string' ? senderType.toLowerCase() : senderType;
    
    if (type === 'user' || type === 0) {
      return <UserOutlined />;
    } else if (type === 'agent' || type === 1) {
      return <RobotOutlined />;
    } else if (type === 'system' || type === 2) {
      return <TeamOutlined />;
    }
    return <UserOutlined />;
  };

  const getSenderColor = (senderType: string | number) => {
    const type = typeof senderType === 'string' ? senderType.toLowerCase() : senderType;
    
    if (type === 'user' || type === 0) {
      return '#1890ff';
    } else if (type === 'agent' || type === 1) {
      return '#52c41a';
    } else if (type === 'system' || type === 2) {
      return '#722ed1';
    }
    return '#1890ff';
  };

  const getSenderLabel = (senderType: string | number) => {
    const type = typeof senderType === 'string' ? senderType.toLowerCase() : senderType;
    
    if (type === 'user' || type === 0) {
      return '用户';
    } else if (type === 'agent' || type === 1) {
      return '智能体';
    } else if (type === 'system' || type === 2) {
      return '系统';
    }
    return '未知';
  };

  if (loading) {
    return (
      <div style={{ textAlign: 'center', padding: '50px' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (messages.length === 0) {
    return (
      <Empty
        description="暂无聊天记录"
        image={Empty.PRESENTED_IMAGE_SIMPLE}
      />
    );
  }

  return (
    <List
      itemLayout="horizontal"
      dataSource={messages}
      renderItem={(item) => (
        <List.Item>
          <List.Item.Meta
            avatar={
              <Avatar
                icon={getSenderIcon(item.senderType)}
                style={{ backgroundColor: getSenderColor(item.senderType) }}
              />
            }
            title={
              <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <span>{item.senderName || getSenderLabel(item.senderType)}</span>
                <Tag color={getSenderColor(item.senderType)}>
                  {getSenderLabel(item.senderType)}
                </Tag>
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
  );
};

export default ChatHistory;
