import React, { useEffect, useState, useRef, useCallback } from 'react';
import { Card, Input, Button, List, Avatar, Tag, Space, Select, message, Divider, Typography, Spin, Empty } from 'antd';
import { SendOutlined, RobotOutlined, UserOutlined, MessageOutlined, LoadingOutlined, TeamOutlined } from '@ant-design/icons';
import { agentService, Agent } from '../services/agentService';
import { collaborationService, Collaboration } from '../services/collaborationService';
import socketService from '../services/socketService';
import api from '../services/api';

const { TextArea } = Input;
const { Text, Title } = Typography;

interface ChatMessage {
  id: string;
  fromAgentId: string;
  fromAgentName: string;
  fromAgentAvatar?: string;
  toAgentId?: string;
  toAgentName?: string;
  toAgentAvatar?: string;
  content: string;
  type: string;
  timestamp: Date;
  isSystem?: boolean;
}

const CollaborationChat: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [selectedCollaboration, setSelectedCollaboration] = useState<string>('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState('');
  const [connected, setConnected] = useState(false);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [hasMoreHistory, setHasMoreHistory] = useState(true);
  const [page, setPage] = useState(1);
  const [totalMessages, setTotalMessages] = useState(0);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const [initialLoadDone, setInitialLoadDone] = useState(false);
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadAgents();
      loadCollaborations();
      connectSocket();
    }
    return () => {
      disconnectSocket();
    };
  }, []);

  useEffect(() => {
    if (selectedCollaboration) {
      loadCollaborationMessages(selectedCollaboration, 1);
    }
  }, [selectedCollaboration]);

  useEffect(() => {
    if (initialLoadDone) {
      scrollToBottom();
    }
  }, [messages, initialLoadDone]);

  const loadAgents = async () => {
    try {
      const data = await agentService.getAllAgents();
      setAgents(data);
    } catch (error) {
      message.error('加载智能体列表失败');
    }
  };

  const loadCollaborations = async () => {
    try {
      const data = await collaborationService.getAllCollaborations();
      setCollaborations(data);
      if (data.length > 0 && !selectedCollaboration) {
        setSelectedCollaboration(data[0].id);
      }
    } catch (error) {
      message.error('加载协作项目失败');
    }
  };

  const loadCollaborationMessages = async (collaborationId: string, pageNum: number = 1, before?: Date) => {
    if (!collaborationId) return;
    
    try {
      setLoadingHistory(true);
      const params = new URLSearchParams({
        page: pageNum.toString(),
        pageSize: '20',
      });
      
      if (before) {
        params.append('before', before.toISOString());
      }

      const response = await api.get(`/messages/collaboration/${collaborationId}?${params}`);
      const { messages: historyMessages, total } = response.data;

      const formattedMessages: ChatMessage[] = historyMessages.map((msg: any) => ({
        id: msg.id,
        fromAgentId: msg.fromAgentId,
        fromAgentName: msg.fromAgent?.name || '未知智能体',
        fromAgentAvatar: msg.fromAgent?.avatar,
        toAgentId: msg.toAgentId,
        toAgentName: msg.toAgent?.name,
        toAgentAvatar: msg.toAgent?.avatar,
        content: msg.content,
        type: msg.type?.toLowerCase() || 'text',
        timestamp: new Date(msg.createdAt),
      }));

      if (pageNum === 1) {
        setMessages(formattedMessages.reverse());
        setInitialLoadDone(true);
      } else {
        setMessages(prev => [...formattedMessages.reverse(), ...prev]);
      }

      setTotalMessages(total);
      setHasMoreHistory(pageNum * 20 < total);
      setPage(pageNum);
    } catch (error) {
      console.error('加载消息失败:', error);
      message.error('加载消息失败');
    } finally {
      setLoadingHistory(false);
    }
  };

  const connectSocket = () => {
    socketService.connect();
    setConnected(true);

    socketService.onMessage((message: any) => {
      if (message.collaborationId === selectedCollaboration) {
        const newMessage: ChatMessage = {
          id: message.id || Date.now().toString(),
          fromAgentId: message.fromAgentId,
          fromAgentName: message.fromAgentName || getAgentName(message.fromAgentId),
          toAgentId: message.toAgentId,
          toAgentName: message.toAgentName || getAgentName(message.toAgentId),
          content: message.content,
          type: message.type || 'text',
          timestamp: new Date(message.createdAt || Date.now()),
        };
        setMessages(prev => [...prev, newMessage]);
      }
    });
  };

  const disconnectSocket = () => {
    socketService.disconnect();
    setConnected(false);
  };

  const getAgentName = (agentId: string) => {
    const agent = agents.find(a => a.id === agentId);
    return agent?.name || '未知智能体';
  };

  const getAgentAvatar = (agentId: string) => {
    const agent = agents.find(a => a.id === agentId);
    return agent?.avatar || '🤖';
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const container = e.currentTarget;
    if (container.scrollTop === 0 && hasMoreHistory && !loadingHistory && selectedCollaboration) {
      const oldestMessage = messages[0];
      if (oldestMessage) {
        loadCollaborationMessages(selectedCollaboration, page + 1, oldestMessage.timestamp);
      }
    }
  }, [hasMoreHistory, loadingHistory, messages, page, selectedCollaboration]);

  const handleCollaborationChange = (value: string) => {
    setSelectedCollaboration(value);
    setMessages([]);
    setPage(1);
    setHasMoreHistory(true);
    setInitialLoadDone(false);
  };

  const handleSendMessage = () => {
    if (!selectedCollaboration) {
      message.warning('请先选择协作项目');
      return;
    }
    
    if (!inputMessage.trim()) {
      message.warning('请输入消息内容');
      return;
    }

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      fromAgentId: 'user',
      fromAgentName: '用户',
      content: inputMessage,
      type: 'text',
      timestamp: new Date(),
    };

    setMessages(prev => [...prev, userMessage]);
    setInputMessage('');
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const getMessageTypeColor = (type: string) => {
    const colors: { [key: string]: string } = {
      text: 'blue',
      command: 'orange',
      query: 'green',
      response: 'purple',
      error: 'red',
      system: 'default',
    };
    return colors[type] || 'default';
  };

  const getAgentColor = (agentId: string) => {
    const colors = ['#1890ff', '#52c41a', '#faad14', '#f5222d', '#722ed1', '#13c2c2'];
    const index = agents.findIndex(a => a.id === agentId);
    return colors[index % colors.length];
  };

  const selectedCollab = collaborations.find(c => c.id === selectedCollaboration);

  return (
    <div style={{ height: 'calc(100vh - 120px)', display: 'flex', flexDirection: 'column' }}>
      <Card 
        title={
          <Space>
            <MessageOutlined />
            <span>协作聊天窗口</span>
            <Tag color={connected ? 'success' : 'error'}>
              {connected ? '已连接' : '未连接'}
            </Tag>
          </Space>
        }
        style={{ height: '100%', display: 'flex', flexDirection: 'column' }}
        bodyStyle={{ flex: 1, display: 'flex', flexDirection: 'column', padding: '16px' }}
      >
        <div style={{ marginBottom: 16 }}>
          <Space wrap>
            <Text>选择协作项目:</Text>
            <Select
              style={{ width: 300 }}
              placeholder="选择协作项目"
              value={selectedCollaboration}
              onChange={handleCollaborationChange}
              suffixIcon={<TeamOutlined />}
            >
              {collaborations.map(collab => (
                <Select.Option key={collab.id} value={collab.id}>
                  <Space>
                    <TeamOutlined />
                    {collab.name}
                    <Tag color="blue">{collab.agents?.length || 0} 智能体</Tag>
                  </Space>
                </Select.Option>
              ))}
            </Select>
            {selectedCollab && (
              <>
                <Tag color="green">{selectedCollab.status}</Tag>
                {selectedCollab.gitRepositoryUrl && (
                  <Tag color="purple">Git已配置</Tag>
                )}
              </>
            )}
          </Space>
        </div>

        <Divider style={{ margin: '12px 0' }} />

        <div 
          ref={messagesContainerRef}
          onScroll={handleScroll}
          style={{ flex: 1, overflow: 'auto', marginBottom: 16, padding: '8px', background: '#fafafa', borderRadius: '8px' }}
        >
          {loadingHistory && (
            <div style={{ textAlign: 'center', padding: '16px' }}>
              <Spin indicator={<LoadingOutlined spin />} />
              <Text type="secondary" style={{ marginLeft: 8 }}>加载历史消息...</Text>
            </div>
          )}
          
          {!selectedCollaboration ? (
            <Empty 
              description="请选择协作项目" 
              style={{ marginTop: 100 }}
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            />
          ) : messages.length === 0 && !loadingHistory ? (
            <Empty 
              description="暂无消息" 
              style={{ marginTop: 100 }}
              image={Empty.PRESENTED_IMAGE_SIMPLE}
            />
          ) : (
            <>
              {hasMoreHistory && messages.length > 0 && (
                <div style={{ textAlign: 'center', padding: '8px', color: '#999' }}>
                  <Text type="secondary">向上滚动加载更多消息</Text>
                </div>
              )}
              <List
                dataSource={messages}
                renderItem={(msg) => (
                  <List.Item style={{ border: 'none', padding: '8px 0' }}>
                    <div style={{ width: '100%' }}>
                      {msg.isSystem ? (
                        <div style={{ textAlign: 'center', color: '#999', fontSize: '12px' }}>
                          <Text type="secondary">{msg.content}</Text>
                        </div>
                      ) : (
                        <div style={{ display: 'flex', alignItems: 'flex-start' }}>
                          <Avatar 
                            size={40}
                            style={{ 
                              backgroundColor: msg.fromAgentId === 'user' ? '#1890ff' : getAgentColor(msg.fromAgentId),
                              marginRight: 8 
                            }}
                            icon={msg.fromAgentId === 'user' ? <UserOutlined /> : undefined}
                          >
                            {msg.fromAgentId !== 'user' && (msg.fromAgentAvatar || getAgentAvatar(msg.fromAgentId) || '🤖')}
                          </Avatar>
                          <div style={{ flex: 1 }}>
                            <Space>
                              <Text strong>{msg.fromAgentName}</Text>
                              {msg.toAgentName && (
                                <>
                                  <Text type="secondary">→</Text>
                                  <Text strong>{msg.toAgentName}</Text>
                                </>
                              )}
                              <Tag color={getMessageTypeColor(msg.type)}>{msg.type}</Tag>
                              <Text type="secondary" style={{ fontSize: '12px' }}>
                                {new Date(msg.timestamp).toLocaleTimeString()}
                              </Text>
                            </Space>
                            <div style={{ marginTop: 4, padding: '8px 12px', background: '#fff', borderRadius: '8px', boxShadow: '0 1px 2px rgba(0,0,0,0.1)' }}>
                              <Text>{msg.content}</Text>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  </List.Item>
                )}
              />
              <div ref={messagesEndRef} />
            </>
          )}
        </div>

        <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
          <Text type="secondary" style={{ fontSize: 12 }}>
            共 {totalMessages} 条消息
          </Text>
          <TextArea
            value={inputMessage}
            onChange={(e) => setInputMessage(e.target.value)}
            onKeyPress={handleKeyPress}
            placeholder="输入消息... (Enter发送, Shift+Enter换行)"
            autoSize={{ minRows: 2, maxRows: 4 }}
            style={{ flex: 1 }}
            disabled={!selectedCollaboration}
          />
          <Button 
            type="primary" 
            icon={<SendOutlined />} 
            onClick={handleSendMessage}
            style={{ height: 'auto' }}
            disabled={!selectedCollaboration}
          >
            发送
          </Button>
        </div>
      </Card>
    </div>
  );
};

export default CollaborationChat;
