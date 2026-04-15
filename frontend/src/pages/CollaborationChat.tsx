import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Card, Select, Tag, Avatar, Space, Spin, Empty, Mentions, Tooltip, Button, message as antMessage, Row, Col } from 'antd';
import { SendOutlined, RobotOutlined, UserOutlined, TeamOutlined, LoadingOutlined, CommentOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration, CollaborationAgent } from '../services/collaborationService';

interface ChatMsg {
  id: string | number;
  fromAgentId?: number;
  fromAgentName: string;
  fromAgentRole?: string;
  fromAgentType?: string;
  fromAgentAvatar?: string;
  modelName?: string;
  content: string;
  type: 'user' | 'agent' | 'system';
  timestamp: Date;
  isMentioned?: boolean;
  senderType?: string;
}

const CollaborationChat: React.FC = () => {
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [agents, setAgents] = useState<CollaborationAgent[]>([]);
  const [messages, setMessages] = useState<ChatMsg[]>([]);
  const [inputValue, setInputValue] = useState('');
  const [sending, setSending] = useState(false);
  const [thinkingAgent, setThinkingAgent] = useState<string>('');
  const [hasMoreHistory, setHasMoreHistory] = useState(false);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [loadingCollabs, setLoadingCollabs] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const [mentionedAgentIds, setMentionedAgentIds] = useState<string[]>([]);

  useEffect(() => {
    loadCollaborations();
  }, []);

  const loadCollaborations = async () => {
    try {
      setLoadingCollabs(true);
      const data = await collaborationService.getAllCollaborations();
      setCollaborations(data);
      if (data.length > 0 && !selectedId) {
        setSelectedId(String(data[0].id));
      }
    } catch {
      antMessage.error('加载团队列表失败');
    } finally {
      setLoadingCollabs(false);
    }
  };

  useEffect(() => {
    if (selectedId) {
      loadAgents();
      loadRecentHistory();
    }
  }, [selectedId]);

  const loadAgents = async () => {
    try {
      const data = await collaborationService.getCollaborationAgents(selectedId);
      setAgents(data);
    } catch {}
  };

  const loadRecentHistory = useCallback(async () => {
    if (!selectedId) return;
    try {
      const res = await collaborationService.getChatHistory(selectedId, 20);
      if (res.success && res.data) {
        const historyMsgs: ChatMsg[] = res.data.map((m: any) => ({
          id: m.id,
          fromAgentId: m.from_agent_id ?? m.fromAgentId,
          fromAgentName: m.from_agent_name ?? m.fromAgentName ?? '我',
          fromAgentRole: m.from_agent_role ?? m.fromAgentRole,
          fromAgentType: m.from_agent_type ?? m.fromAgentType,
          fromAgentAvatar: m.from_agent_avatar ?? m.fromAgentAvatar,
          modelName: m.model_name ?? m.modelName,
          content: m.content,
          type: (m.sender_type ?? m.senderType) === 'Agent' ? 'agent' : 'user',
          timestamp: new Date(m.timestamp),
          isMentioned: m.is_mentioned ?? m.isMentioned,
        }));
        setMessages(historyMsgs);
        setHasMoreHistory(res.hasMore ?? false);
      }
    } catch {}
  }, [selectedId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const loadOlderHistory = async () => {
    if (loadingHistory || messages.length === 0) return;
    const oldestId = messages[0]?.id;
    if (!oldestId || typeof oldestId !== 'number') return;

    setLoadingHistory(true);
    try {
      const res = await collaborationService.getChatHistory(selectedId, 20, oldestId);
      if (res.success && res.data && res.data.length > 0) {
        const olderMsgs: ChatMsg[] = res.data.map((m: any) => ({
          id: m.id,
          fromAgentId: m.from_agent_id ?? m.fromAgentId,
          fromAgentName: m.from_agent_name ?? m.fromAgentName ?? '我',
          fromAgentRole: m.from_agent_role ?? m.fromAgentRole,
          fromAgentType: m.from_agent_type ?? m.fromAgentType,
          fromAgentAvatar: m.from_agent_avatar ?? m.fromAgentAvatar,
          modelName: m.model_name ?? m.modelName,
          content: m.content,
          type: (m.sender_type ?? m.senderType) === 'Agent' ? 'agent' : 'user',
          timestamp: new Date(m.timestamp),
        }));
        setMessages(prev => [...olderMsgs, ...prev]);
        setHasMoreHistory(res.hasMore ?? false);
      } else {
        setHasMoreHistory(false);
      }
    } catch {} finally {
      setLoadingHistory(false);
    }
  };

  const handleScroll = () => {
    const container = messagesContainerRef.current;
    if (container && container.scrollTop < 50 && hasMoreHistory && !loadingHistory) {
      loadOlderHistory();
    }
  };

  const extractMentionedIds = (text: string): string[] => {
    const ids: string[] = [];
    for (const agent of agents) {
      if (text.includes(`@${agent.agentName}`)) {
        if (!ids.includes(String(agent.agentId))) {
          ids.push(String(agent.agentId));
        }
      }
    }
    return ids;
  };

  const handleMentionChange = (val: string) => {
    setInputValue(val);
    setMentionedAgentIds(extractMentionedIds(val));
  };

  const handleSend = async () => {
    const content = inputValue.trim();
    if (!content || sending || !selectedId) return;

    const currentMentionedIds = [...mentionedAgentIds];
    const mentionedAgent = currentMentionedIds.length > 0
      ? agents.find(a => String(a.agentId) === currentMentionedIds[0])
      : null;

    const userMsg: ChatMsg = {
      id: `user-${Date.now()}`,
      fromAgentName: '我',
      content,
      type: 'user',
      timestamp: new Date(),
    };

    setMessages(prev => [...prev, userMsg]);
    setInputValue('');
    setMentionedAgentIds([]);
    setSending(true);
    setThinkingAgent(mentionedAgent?.agentName || agents.find(a => a.role === 'Manager')?.agentName || 'Agent');

    try {
      const response = await collaborationService.sendChatMessage(
        selectedId,
        content,
        currentMentionedIds.length > 0 ? currentMentionedIds : undefined
      );

      if (response.success && response.data) {
        const d = response.data;
        const agentMsg: ChatMsg = {
          id: d.fromAgentId ?? `agent-${Date.now()}`,
          fromAgentId: d.fromAgentId,
          fromAgentName: d.fromAgentName,
          fromAgentRole: d.fromAgentRole,
          fromAgentType: d.fromAgentType,
          fromAgentAvatar: d.fromAgentAvatar,
          modelName: d.modelName,
          content: d.content,
          type: 'agent',
          timestamp: new Date(d.timestamp),
          isMentioned: d.isMentioned,
        };
        setMessages(prev => [...prev, agentMsg]);
      } else {
        antMessage.error(response.message || '发送失败');
      }
    } catch (error: any) {
      antMessage.error(error?.response?.data?.message || '聊天请求失败');
    } finally {
      setSending(false);
      setThinkingAgent('');
    }
  };

  const handleTagClick = (agent: CollaborationAgent) => {
    const mention = `@${agent.agentName} `;
    const newInput = inputValue + mention;
    setInputValue(newInput);
    const idStr = String(agent.agentId);
    if (!mentionedAgentIds.includes(idStr)) {
      setMentionedAgentIds(prev => [...prev, idStr]);
    }
  };

  const getRoleColor = (role?: string) => {
    if (role === 'Manager') return 'gold';
    return 'blue';
  };

  const getAvatarColor = (name: string) => {
    const colors = ['#1677ff', '#52c41a', '#fa8c16', '#722ed1', '#eb2f96', '#13c2c2'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  };

  const renderAvatar = (msg: ChatMsg) => {
    if (msg.type === 'user') {
      return <Avatar size={40} icon={<UserOutlined />} style={{ marginLeft: 8, backgroundColor: '#87d068', flexShrink: 0 }} />;
    }
    if (msg.fromAgentAvatar) {
      return <Avatar size={40} src={msg.fromAgentAvatar} style={{ marginRight: 8, flexShrink: 0 }} />;
    }
    return <Avatar size={40} icon={<RobotOutlined />} style={{ backgroundColor: getAvatarColor(msg.fromAgentName), marginRight: 8, flexShrink: 0 }} />;
  };

  const getAgentTooltip = (agent: CollaborationAgent) => (
    <div>
      <div><strong>{agent.agentName}</strong></div>
      {agent.agentType && <div>类型：{agent.agentType}</div>}
      {agent.role && <div>角色：{agent.role === 'Manager' ? '协调者' : agent.role}</div>}
      {agent.agentStatus && <div>状态：{agent.agentStatus}</div>}
    </div>
  );

  return (
    <div style={{ padding: 24, height: 'calc(100vh - 120px)', display: 'flex', flexDirection: 'column' }}>
      <Row gutter={16} style={{ flex: 1, minHeight: 0 }}>
        <Col span={6}>
          <Card
            title={<span><TeamOutlined /> 选择团队</span>}
            style={{ height: '100%', overflow: 'auto' }}
            bodyStyle={{ padding: 12 }}
          >
            <Select
              value={selectedId || undefined}
              onChange={(val) => {
                setSelectedId(val);
                setMessages([]);
                setMentionedAgentIds([]);
              }}
              placeholder="选择团队"
              style={{ width: '100%', marginBottom: 16 }}
              loading={loadingCollabs}
              options={collaborations.map(c => ({
                value: String(c.id),
                label: c.name,
              }))}
            />

            {agents.length > 0 && (
              <div>
                <div style={{ fontSize: 13, color: '#666', marginBottom: 8 }}>团队成员</div>
                {agents.map(agent => (
                  <Tooltip key={agent.agentId} title={getAgentTooltip(agent)} placement="right">
                    <div
                      style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: 8,
                        padding: '6px 8px',
                        borderRadius: 6,
                        cursor: 'pointer',
                        marginBottom: 4,
                        transition: 'background 0.2s',
                      }}
                      onClick={() => handleTagClick(agent)}
                      onMouseEnter={(e) => { e.currentTarget.style.background = '#f0f0f0'; }}
                      onMouseLeave={(e) => { e.currentTarget.style.background = 'transparent'; }}
                    >
                      {agent.agentAvatar ? (
                        <Avatar size={28} src={agent.agentAvatar} />
                      ) : (
                        <Avatar size={28} icon={<RobotOutlined />} style={{ backgroundColor: getAvatarColor(agent.agentName) }} />
                      )}
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{ fontSize: 13, fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                          {agent.agentName}
                        </div>
                        <div style={{ fontSize: 11, color: '#999' }}>
                          {agent.role === 'Manager' ? '协调者' : agent.agentType || agent.role || 'Worker'}
                        </div>
                      </div>
                      <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ fontSize: 10, margin: 0 }}>
                        {agent.role === 'Manager' ? '协调者' : agent.agentType || 'Worker'}
                      </Tag>
                    </div>
                  </Tooltip>
                ))}
              </div>
            )}
          </Card>
        </Col>

        <Col span={18}>
          <Card
            title={<span><CommentOutlined /> 协作聊天</span>}
            style={{ height: '100%', display: 'flex', flexDirection: 'column' }}
            bodyStyle={{ flex: 1, display: 'flex', flexDirection: 'column', padding: 12, minHeight: 0 }}
          >
            <div
              ref={messagesContainerRef}
              onScroll={handleScroll}
              style={{
                flex: 1,
                overflowY: 'auto',
                padding: '12px',
                border: '1px solid #f0f0f0',
                borderRadius: 8,
                marginBottom: 12,
                background: '#fafafa',
              }}
            >
              {loadingHistory && (
                <div style={{ textAlign: 'center', padding: 8, color: '#999', fontSize: 12 }}>
                  <LoadingOutlined /> 加载历史消息...
                </div>
              )}
              {!selectedId ? (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description="请先选择一个团队"
                />
              ) : messages.length === 0 && !loadingHistory ? (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description={
                    <span>
                      <TeamOutlined /> 开始聊天
                      <br />
                      <span style={{ fontSize: 12, color: '#999' }}>
                        直接发消息由协调者回答，@某个人则指定Agent回答
                      </span>
                    </span>
                  }
                />
              ) : (
                messages.map(msg => (
                  <div
                    key={msg.id}
                    style={{
                      display: 'flex',
                      justifyContent: msg.type === 'user' ? 'flex-end' : 'flex-start',
                      marginBottom: 12,
                    }}
                  >
                    {msg.type !== 'user' && renderAvatar(msg)}
                    <div style={{ maxWidth: '70%' }}>
                      {msg.type !== 'user' && (
                        <div style={{ marginBottom: 2, display: 'flex', alignItems: 'center', gap: 6, flexWrap: 'wrap' }}>
                          <span style={{ fontWeight: 500, fontSize: 13 }}>{msg.fromAgentName}</span>
                          {msg.fromAgentRole && (
                            <Tag color={getRoleColor(msg.fromAgentRole)} style={{ fontSize: 11, lineHeight: '18px', padding: '0 4px', margin: 0 }}>
                              {msg.fromAgentRole === 'Manager' ? '协调者' : msg.fromAgentRole}
                            </Tag>
                          )}
                          {msg.fromAgentType && (
                            <Tag style={{ fontSize: 11, lineHeight: '18px', padding: '0 4px', margin: 0 }}>
                              {msg.fromAgentType}
                            </Tag>
                          )}
                          {msg.modelName && (
                            <Tag color="geekblue" style={{ fontSize: 10, lineHeight: '16px', padding: '0 4px', margin: 0 }}>
                              {msg.modelName}
                            </Tag>
                          )}
                          {msg.isMentioned && (
                            <Tag color="purple" style={{ fontSize: 10, lineHeight: '16px', padding: '0 4px', margin: 0 }}>@</Tag>
                          )}
                          <span style={{ fontSize: 11, color: '#bbb', marginLeft: 'auto' }}>
                            {msg.timestamp.toLocaleTimeString()}
                          </span>
                        </div>
                      )}
                      <div
                        style={{
                          padding: '8px 12px',
                          borderRadius: msg.type === 'user' ? '12px 12px 2px 12px' : '12px 12px 12px 2px',
                          background: msg.type === 'user' ? '#1677ff' : '#fff',
                          color: msg.type === 'user' ? '#fff' : '#333',
                          boxShadow: '0 1px 2px rgba(0,0,0,0.06)',
                          fontSize: 14,
                          lineHeight: 1.6,
                          whiteSpace: 'pre-wrap',
                          wordBreak: 'break-word',
                        }}
                      >
                        {msg.content}
                      </div>
                      {msg.type === 'user' && (
                        <div style={{ fontSize: 11, color: '#999', marginTop: 2, textAlign: 'right' }}>
                          {msg.timestamp.toLocaleTimeString()}
                        </div>
                      )}
                    </div>
                    {msg.type === 'user' && renderAvatar(msg)}
                  </div>
                ))
              )}
              {sending && thinkingAgent && (
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, padding: '4px 0' }}>
                  <Spin size="small" />
                  <span style={{ color: '#999', fontSize: 13 }}>{thinkingAgent} 正在思考...</span>
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            <div style={{ display: 'flex', gap: 8 }}>
              <Mentions
                value={inputValue}
                onChange={handleMentionChange}
                placeholder="输入消息，@ 提及指定Agent..."
                style={{ flex: 1 }}
                rows={2}
                onPressEnter={(e) => {
                  if (!e.shiftKey) {
                    e.preventDefault();
                    handleSend();
                  }
                }}
                options={agents.map(agent => ({
                  value: agent.agentName,
                  label: (
                    <Space>
                      <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ fontSize: 11 }}>
                        {agent.role === 'Manager' ? '协调者' : agent.agentType || agent.role || 'Worker'}
                      </Tag>
                      <span>{agent.agentName}</span>
                    </Space>
                  ),
                }))}
                prefix="@"
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                loading={sending}
                disabled={!selectedId}
                style={{ height: 54, width: 54 }}
              />
            </div>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default CollaborationChat;
