import React, { useEffect, useState, useRef, useCallback } from 'react';
import { Card, Input, Button, List, Avatar, Tag, Space, Select, message, Divider, Typography, Spin, Empty, Mentions } from 'antd';
import { SendOutlined, RobotOutlined, UserOutlined, MessageOutlined, LoadingOutlined, TeamOutlined } from '@ant-design/icons';
import { agentService, Agent } from '../services/agentService';
import { collaborationService, Collaboration, CollaborationAgent } from '../services/collaborationService';
import socketService from '../services/socketService';
import api from '../services/api';

const { TextArea } = Input;
const { Text, Title } = Typography;
const { Option } = Mentions;

interface ChatMessage {
  id: string;
  fromAgentId?: string;
  fromAgentName: string;
  fromAgentAvatar?: string;
  toAgentId?: string;
  toAgentName?: string;
  toAgentAvatar?: string;
  content: string;
  type: string;
  timestamp: Date;
  isSystem?: boolean;
  senderType?: 'User' | 'Agent';
  senderName?: string;
  isStreaming?: boolean;
}

const CollaborationChat: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [collaborationAgents, setCollaborationAgents] = useState<CollaborationAgent[]>([]);
  const [selectedCollaboration, setSelectedCollaboration] = useState<string>('');
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState('');
  const [connected, setConnected] = useState(false);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [sending, setSending] = useState(false);
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
      loadCollaborationAgents(selectedCollaboration);
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

  const loadCollaborationAgents = async (collaborationId: string) => {
    try {
      const data = await collaborationService.getCollaborationAgents(collaborationId);
      setCollaborationAgents(data);
    } catch (error) {
      console.error('加载协作智能体失败:', error);
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
        fromAgentName: msg.senderType === 'User' ? (msg.senderName || '用户') : (msg.fromAgentName || '智能体'),
        fromAgentAvatar: msg.fromAgentAvatar,
        toAgentId: msg.toAgentId,
        toAgentName: msg.toAgentName,
        toAgentAvatar: msg.toAgentAvatar,
        content: msg.content,
        type: msg.type?.toLowerCase() || 'text',
        timestamp: new Date(msg.createdAt),
        senderType: msg.senderType,
        senderName: msg.senderName,
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
    setCollaborationAgents([]);
  };

  const handleSendMessage = async () => {
    if (!selectedCollaboration) {
      message.warning('请先选择协作项目');
      return;
    }
    
    if (!inputMessage.trim()) {
      message.warning('请输入消息内容');
      return;
    }

    const mentionRegex = /@([^\s@]+)/g;
    const mentions = inputMessage.match(mentionRegex) || [];
    const mentionedNames = mentions.map(m => m.substring(1).trim().toLowerCase());
    
    const mentionedAgentIds = collaborationAgents
      .filter(ca => {
        const agentName = (ca.agent?.name || '').toLowerCase();
        return mentionedNames.includes(agentName);
      })
      .map(ca => ca.agentId);

    const displayContent = inputMessage.replace(/@([^\s@]+)/g, (match, name) => {
      const agent = collaborationAgents.find(ca => 
        (ca.agent?.name || '').toLowerCase() === name.toLowerCase()
      );
      return agent ? `@${agent.agent?.name}` : match;
    });

    const userMessage: ChatMessage = {
      id: Date.now().toString(),
      fromAgentId: undefined,
      fromAgentName: '用户',
      content: displayContent,
      type: 'text',
      timestamp: new Date(),
      senderType: 'User',
      senderName: '用户',
    };

    setMessages(prev => [...prev, userMessage]);
    setInputMessage('');
    setSending(true);

    try {
      const token = localStorage.getItem('token');
      
      const response = await fetch(`${api.defaults.baseURL}/collaborations/${selectedCollaboration}/chat/stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          content: displayContent,
          mentionedAgentIds: mentionedAgentIds.length > 0 ? mentionedAgentIds : undefined
        })
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const reader = response.body?.getReader();
      const decoder = new TextDecoder();

      if (!reader) {
        throw new Error('Response body is null');
      }

      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.startsWith('event: ')) {
            const eventType = line.substring(7);
            continue;
          }
          
          if (line.startsWith('data: ')) {
            const data = line.substring(6);
            
            try {
              const parsed = JSON.parse(data);
              console.log('Received event:', parsed);
              
              if (parsed.messageId && parsed.agentId) {
                if (parsed.content !== undefined && !parsed.fullContent) {
                  setMessages(prev => {
                    const existing = prev.find(msg => msg.id === parsed.messageId);
                    if (existing) {
                      return prev.map(msg => {
                        if (msg.id === parsed.messageId) {
                          return {
                            ...msg,
                            content: msg.content + parsed.content
                          };
                        }
                        return msg;
                      });
                    } else {
                      const agentMessage: ChatMessage = {
                        id: parsed.messageId,
                        fromAgentId: parsed.agentId,
                        fromAgentName: parsed.agentName,
                        content: parsed.content,
                        type: 'response',
                        timestamp: new Date(),
                        senderType: 'Agent',
                        isStreaming: true,
                      };
                      return [...prev, agentMessage];
                    }
                  });
                } else if (parsed.fullContent !== undefined) {
                  setMessages(prev => prev.map(msg => {
                    if (msg.id === parsed.messageId) {
                      return {
                        ...msg,
                        content: parsed.fullContent,
                        isStreaming: false,
                        timestamp: new Date(parsed.timestamp)
                      };
                    }
                    return msg;
                  }));
                }
              } else if (parsed.error) {
                message.error(parsed.error);
              } else if (parsed.collaborationId) {
                setSending(false);
              }
            } catch (e) {
              console.error('Failed to parse SSE data:', e);
            }
          }
        }
      }

      setSending(false);
    } catch (error: any) {
      console.error('发送消息失败:', error);
      message.error(error.message || '发送消息失败');
      setSending(false);
    }
  };

  const renderMessageContent = (content: string) => {
    const mentionRegex = /@([^\s@]+)/g;
    const parts: React.ReactNode[] = [];
    let lastIndex = 0;
    let match;

    while ((match = mentionRegex.exec(content)) !== null) {
      if (match.index > lastIndex) {
        parts.push(content.substring(lastIndex, match.index));
      }
      
      const mentionedName = match[1];
      const agent = collaborationAgents.find(
        ca => (ca.agent?.name || '').toLowerCase() === mentionedName.toLowerCase()
      );
      
      if (agent) {
        parts.push(
          <span
            key={match.index}
            style={{
              backgroundColor: '#e6f7ff',
              color: '#1890ff',
              padding: '2px 6px',
              borderRadius: '4px',
              fontWeight: 500,
            }}
          >
            @{agent.agent?.name}
          </span>
        );
      } else {
        parts.push(match[0]);
      }
      
      lastIndex = match.index + match[0].length;
    }

    if (lastIndex < content.length) {
      parts.push(content.substring(lastIndex));
    }

    return parts.length > 0 ? <>{parts}</> : <Text>{content}</Text>;
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

        {collaborationAgents.length > 0 && (
          <div style={{ marginBottom: 16 }}>
            <Space wrap>
              <Text strong>参与智能体:</Text>
              {collaborationAgents.map(ca => (
                <Tag 
                  key={ca.agentId} 
                  color="blue" 
                  icon={<RobotOutlined />}
                  style={{ cursor: 'pointer' }}
                  onClick={() => {
                    const agentName = ca.agent?.name || '未知';
                    setInputMessage(prev => {
                      if (prev.includes(`@${agentName}`)) return prev;
                      return prev + `@${agentName} `;
                    });
                  }}
                >
                  @{ca.agent?.name || '未知智能体'}
                  {ca.role && ` (${ca.role})`}
                </Tag>
              ))}
            </Space>
            <Text type="secondary" style={{ display: 'block', marginTop: 4, fontSize: 12 }}>
              💡 输入 @ 或点击智能体标签可以指定回复，不@则所有智能体依次回复
            </Text>
          </div>
        )}

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
                      ) : msg.senderType === 'User' ? (
                        <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'flex-end', gap: '8px' }}>
                          <div style={{ maxWidth: '70%' }}>
                            <div style={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                              <Text type="secondary" style={{ fontSize: '12px' }}>
                                {new Date(msg.timestamp).toLocaleTimeString()}
                              </Text>
                              <Text strong style={{ color: '#1890ff' }}>{msg.fromAgentName}</Text>
                            </div>
                            <div style={{ 
                              padding: '10px 14px', 
                              background: 'linear-gradient(135deg, #1890ff 0%, #096dd9 100%)', 
                              color: '#fff',
                              borderRadius: '16px 16px 4px 16px', 
                              boxShadow: '0 2px 8px rgba(24, 144, 255, 0.3)',
                              wordBreak: 'break-word'
                            }}>
                              {renderMessageContent(msg.content)}
                            </div>
                          </div>
                          <Avatar 
                            size={40}
                            style={{ 
                              background: 'linear-gradient(135deg, #1890ff 0%, #096dd9 100%)',
                              boxShadow: '0 2px 8px rgba(24, 144, 255, 0.3)'
                            }}
                            icon={<UserOutlined />}
                          />
                        </div>
                      ) : (
                        <div style={{ display: 'flex', alignItems: 'flex-start', gap: '8px' }}>
                          <Avatar 
                            size={40}
                            style={{ 
                              background: `linear-gradient(135deg, ${getAgentColor(msg.fromAgentId || '')} 0%, ${getAgentColor(msg.fromAgentId || '')}dd 100%)`,
                              boxShadow: `0 2px 8px ${getAgentColor(msg.fromAgentId || '')}40`
                            }}
                          >
                            {msg.fromAgentAvatar || getAgentAvatar(msg.fromAgentId || '') || '🤖'}
                          </Avatar>
                          <div style={{ maxWidth: '70%' }}>
                            <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '4px' }}>
                              <Text strong style={{ color: getAgentColor(msg.fromAgentId || '') }}>{msg.fromAgentName}</Text>
                              {msg.toAgentName && (
                                <>
                                  <Text type="secondary">→</Text>
                                  <Text strong>{msg.toAgentName}</Text>
                                </>
                              )}
                              <Tag color={getMessageTypeColor(msg.type)} style={{ margin: 0 }}>{msg.type}</Tag>
                              <Text type="secondary" style={{ fontSize: '12px' }}>
                                {new Date(msg.timestamp).toLocaleTimeString()}
                              </Text>
                            </div>
                            <div style={{ 
                              padding: '10px 14px', 
                              background: '#fff', 
                              borderRadius: '16px 16px 16px 4px', 
                              boxShadow: '0 2px 8px rgba(0,0,0,0.08)',
                              wordBreak: 'break-word'
                            }}>
                              {renderMessageContent(msg.content)}
                              {msg.isStreaming && <span className="typing-cursor">▊</span>}
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

        <div style={{ display: 'flex', gap: '8px', alignItems: 'flex-start' }}>
          <Text type="secondary" style={{ fontSize: 12, paddingTop: 8 }}>
            共 {totalMessages} 条消息
          </Text>
          <Mentions
            value={inputMessage}
            onChange={(val) => setInputMessage(val)}
            style={{ flex: 1 }}
            placeholder={collaborationAgents.length > 0 
              ? "输入消息... 输入@选择智能体，不@则所有智能体依次回复" 
              : "请先在协作项目中添加智能体"}
            autoSize={{ minRows: 2, maxRows: 4 }}
            disabled={!selectedCollaboration || collaborationAgents.length === 0 || sending}
            prefix="@"
            onKeyPress={(e) => {
              if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                handleSendMessage();
              }
            }}
          >
            {collaborationAgents.map(ca => (
              <Option key={ca.agentId} value={ca.agent?.name || '未知'}>
                <Space>
                  <RobotOutlined />
                  <span>{ca.agent?.name || '未知智能体'}</span>
                  {ca.role && <Tag color="blue" style={{ marginLeft: 4 }}>{ca.role}</Tag>}
                </Space>
              </Option>
            ))}
          </Mentions>
          <Button 
            type="primary" 
            icon={sending ? <LoadingOutlined /> : <SendOutlined />} 
            onClick={handleSendMessage}
            style={{ height: 40 }}
            disabled={!selectedCollaboration || collaborationAgents.length === 0 || sending}
            loading={sending}
          >
            发送
          </Button>
        </div>
      </Card>
      
      <style>{`
        .typing-cursor {
          animation: blink 1s infinite;
          font-weight: bold;
        }
        @keyframes blink {
          0%, 50% { opacity: 1; }
          51%, 100% { opacity: 0; }
        }
      `}</style>
    </div>
  );
};

export default CollaborationChat;
