import React, { useState, useRef, useEffect, useCallback } from 'react';
import { Card, Select, Tag, Avatar, Space, Spin, Empty, Mentions, Tooltip, Button, message as antMessage, Tabs } from 'antd';
import { SendOutlined, RobotOutlined, UserOutlined, TeamOutlined, LoadingOutlined, CommentOutlined, HolderOutlined, MessageOutlined } from '@ant-design/icons';
import { collaborationService, Collaboration, CollaborationAgent } from '../services/collaborationService';

interface ChatMsg {
  id: string | number;
  fromAgentId?: number;
  fromAgentName: string;
  fromAgentRole?: string;
  fromAgentType?: string;
  fromAgentAvatar?: string;
  modelName?: string;
  llmConfigName?: string;
  content: string;
  type: 'user' | 'agent' | 'system';
  timestamp: Date;
  isMentioned?: boolean;
  senderType?: string;
}

const MIN_MESSAGE_HEIGHT = 300;

const CollaborationChat: React.FC = () => {
  const [collaborations, setCollaborations] = useState<Collaboration[]>([]);
  const [selectedId, setSelectedId] = useState<string>('');
  const [agents, setAgents] = useState<CollaborationAgent[]>([]);
  const [sending, setSending] = useState(false);
  const [thinkingAgent, setThinkingAgent] = useState<string>('');
  const [hasMoreHistory, setHasMoreHistory] = useState(false);
  const [loadingHistory, setLoadingHistory] = useState(false);
  const [loadingCollabs, setLoadingCollabs] = useState(true);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const messagesContainerRef = useRef<HTMLDivElement>(null);
  const isInitialLoadRef = useRef(true);
  const chatPanelRef = useRef<HTMLDivElement>(null);
  const [chatPanelHeight, setChatPanelHeight] = useState<number | null>(null);
  const dragStartInfoRef = useRef<{ startY: number; startHeight: number } | null>(null);
  const [activeTabKey, setActiveTabKey] = useState<string>('group');
  const [privateTabs, setPrivateTabs] = useState<{ key: string; agent: CollaborationAgent }[]>([]);
  const [groupMessages, setGroupMessages] = useState<ChatMsg[]>([]);
  const [privateMessagesMap, setPrivateMessagesMap] = useState<Record<string, ChatMsg[]>>({});
  const [inputValueMap, setInputValueMap] = useState<Record<string, string>>({ group: '' });
  const [mentionedIdsMap, setMentionedIdsMap] = useState<Record<string, string[]>>({ group: [] });

  const currentInputValue = inputValueMap[activeTabKey] ?? '';
  const currentMentionedIds = mentionedIdsMap[activeTabKey] ?? [];
  const currentMessages = activeTabKey === 'group' ? groupMessages : (privateMessagesMap[activeTabKey] ?? []);

  const setInputValue = (val: string) => {
    setInputValueMap(prev => ({ ...prev, [activeTabKey]: val }));
  };
  const setMentionedIds = (ids: string[]) => {
    setMentionedIdsMap(prev => ({ ...prev, [activeTabKey]: ids }));
  };
  const setCurrentMessages = (updater: (prev: ChatMsg[]) => ChatMsg[]) => {
    if (activeTabKey === 'group') {
      setGroupMessages(updater);
    } else {
      setPrivateMessagesMap(prev => ({
        ...prev,
        [activeTabKey]: updater(prev[activeTabKey] ?? [])
      }));
    }
  };

  const isPrivateTab = activeTabKey !== 'group';
  const activePrivateAgent = isPrivateTab ? privateTabs.find(t => t.key === activeTabKey)?.agent : null;

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
      loadRecentHistory('group');
    }
  }, [selectedId]);

  useEffect(() => {
    if (selectedId && activeTabKey !== 'group') {
      loadRecentHistory(activeTabKey);
    }
  }, [activeTabKey]);

  const loadAgents = async () => {
    try {
      const data = await collaborationService.getCollaborationAgents(selectedId);
      setAgents(data);
    } catch {}
  };

  const mapMessage = (m: any): ChatMsg => ({
    id: m.id,
    fromAgentId: m.from_agent_id ?? m.fromAgentId,
    fromAgentName: m.from_agent_name ?? m.fromAgentName ?? '我',
    fromAgentRole: m.from_agent_role ?? m.fromAgentRole,
    fromAgentType: m.from_agent_type ?? m.fromAgentType,
    fromAgentAvatar: m.from_agent_avatar ?? m.fromAgentAvatar,
    modelName: m.model_name ?? m.modelName,
    llmConfigName: m.llm_config_name ?? m.llmConfigName,
    content: m.content,
    type: (m.sender_type ?? m.senderType) === 'Agent' ? 'agent' : 'user',
    timestamp: new Date(m.timestamp),
    isMentioned: m.is_mentioned ?? m.isMentioned,
  });

  const loadRecentHistory = useCallback(async (tabKey?: string) => {
    if (!selectedId) return;
    const key = tabKey ?? activeTabKey;
    try {
      isInitialLoadRef.current = true;
      let msgType: string | undefined;
      let toId: number | undefined;
      if (key !== 'group') {
        msgType = 'private';
        const tab = privateTabs.find(t => t.key === key);
        toId = tab?.agent.agentId;
      }
      const res = await collaborationService.getChatHistory(selectedId, 10, undefined, msgType, toId);
      if (res.success && res.data) {
        const historyMsgs = res.data.map(mapMessage);
        if (key === 'group') {
          setGroupMessages(historyMsgs);
        } else {
          setPrivateMessagesMap(prev => ({ ...prev, [key]: historyMsgs }));
        }
        setHasMoreHistory(res.hasMore ?? false);
      }
    } catch {}
  }, [selectedId, activeTabKey, privateTabs]);

  useEffect(() => {
    if (isInitialLoadRef.current && currentMessages.length > 0) {
      const container = messagesContainerRef.current;
      if (container) {
        container.scrollTop = container.scrollHeight;
      }
      isInitialLoadRef.current = false;
    }
  }, [currentMessages]);

  const loadOlderHistory = async () => {
    if (loadingHistory || currentMessages.length === 0) return;
    const oldestId = currentMessages[0]?.id;
    if (!oldestId || typeof oldestId !== 'number') return;

    setLoadingHistory(true);
    const container = messagesContainerRef.current;
    const prevScrollHeight = container?.scrollHeight ?? 0;

    try {
      let msgType: string | undefined;
      let toId: number | undefined;
      if (isPrivateTab && activePrivateAgent) {
        msgType = 'private';
        toId = activePrivateAgent.agentId;
      }
      const res = await collaborationService.getChatHistory(selectedId, 10, oldestId, msgType, toId);
      if (res.success && res.data && res.data.length > 0) {
        const olderMsgs = res.data.map(mapMessage);
        setCurrentMessages(prev => [...olderMsgs, ...prev]);
        setHasMoreHistory(res.hasMore ?? false);

        requestAnimationFrame(() => {
          if (container) {
            const newScrollHeight = container.scrollHeight;
            container.scrollTop = newScrollHeight - prevScrollHeight;
          }
        });
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
    if (text.includes('@所有人')) {
      for (const agent of agents) {
        if (!ids.includes(String(agent.agentId))) {
          ids.push(String(agent.agentId));
        }
      }
      return ids;
    }
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
    setMentionedIds(extractMentionedIds(val));
  };

  const openPrivateTab = (agent: CollaborationAgent) => {
    const tabKey = `private-${agent.agentId}`;
    const existing = privateTabs.find(t => t.key === tabKey);
    if (!existing) {
      setPrivateTabs(prev => [...prev, { key: tabKey, agent }]);
      setInputValueMap(prev => ({ ...prev, [tabKey]: '' }));
      setMentionedIdsMap(prev => ({ ...prev, [tabKey]: [] }));
    }
    setActiveTabKey(tabKey);
  };

  const closePrivateTab = (tabKey: string) => {
    setPrivateTabs(prev => prev.filter(t => t.key !== tabKey));
    setPrivateMessagesMap(prev => {
      const next = { ...prev };
      delete next[tabKey];
      return next;
    });
    setInputValueMap(prev => {
      const next = { ...prev };
      delete next[tabKey];
      return next;
    });
    setMentionedIdsMap(prev => {
      const next = { ...prev };
      delete next[tabKey];
      return next;
    });
    if (activeTabKey === tabKey) {
      setActiveTabKey('group');
    }
  };

  const handleSend = async () => {
    const content = currentInputValue.trim();
    if (!content || sending || !selectedId) return;

    const isPrivate = isPrivateTab && activePrivateAgent !== null;

    let thinkingName: string;
    if (isPrivate) {
      thinkingName = activePrivateAgent!.agentName;
    } else {
      const ids = [...currentMentionedIds];
      if (ids.length === 0) {
        thinkingName = agents.find(a => a.role === 'Manager')?.agentName || 'Agent';
      } else if (ids.length === 1) {
        const mentionedAgent = agents.find(a => String(a.agentId) === ids[0]);
        thinkingName = mentionedAgent?.agentName || 'Agent';
      } else {
        const mentionedNames = ids
          .map(id => agents.find(a => String(a.agentId) === id)?.agentName)
          .filter(Boolean);
        thinkingName = mentionedNames.join('、');
      }
    }

    const userMsg: ChatMsg = {
      id: `user-${Date.now()}`,
      fromAgentName: '我',
      content,
      type: 'user',
      timestamp: new Date(),
    };

    setCurrentMessages(prev => [...prev, userMsg]);
    setInputValue('');
    setMentionedIds([]);
    setSending(true);
    setThinkingAgent(thinkingName);

    try {
      let response;
      if (isPrivate) {
        response = await collaborationService.sendChatMessage(
          selectedId,
          content,
          undefined,
          'private',
          activePrivateAgent!.agentId
        );
      } else {
        const ids = [...currentMentionedIds];
        response = await collaborationService.sendChatMessage(
          selectedId,
          content,
          ids.length > 0 ? ids : undefined
        );
      }

      if (response.success && response.data) {
        const results = Array.isArray(response.data) ? response.data : [response.data];
        const agentMsgs: ChatMsg[] = results.map((d: any) => ({
          id: d.fromAgentId ?? `agent-${Date.now()}-${Math.random()}`,
          fromAgentId: d.fromAgentId,
          fromAgentName: d.fromAgentName,
          fromAgentRole: d.fromAgentRole,
          fromAgentType: d.fromAgentType,
          fromAgentAvatar: d.fromAgentAvatar,
          modelName: d.modelName,
          llmConfigName: d.llmConfigName,
          content: d.content,
          type: 'agent' as const,
          timestamp: new Date(d.timestamp),
          isMentioned: d.isMentioned ?? false,
        }));
        setCurrentMessages(prev => [...prev, ...agentMsgs]);
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
    const newInput = currentInputValue + mention;
    setInputValue(newInput);
    const idStr = String(agent.agentId);
    if (!currentMentionedIds.includes(idStr)) {
      setMentionedIds([...currentMentionedIds, idStr]);
    }
  };

  const handleDragMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    const card = chatPanelRef.current?.closest('.ant-card') as HTMLElement | null;
    if (!card) return;
    const startHeight = card.offsetHeight;
    const startY = e.clientY;
    dragStartInfoRef.current = { startY, startHeight };

    const handleMouseMove = (moveEvent: MouseEvent) => {
      const info = dragStartInfoRef.current;
      if (!info) return;
      const delta = moveEvent.clientY - info.startY;
      const newHeight = Math.max(400, info.startHeight + delta);
      setChatPanelHeight(newHeight);
    };

    const handleMouseUp = () => {
      dragStartInfoRef.current = null;
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.cursor = 'ns-resize';
    document.body.style.userSelect = 'none';
  }, []);

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
      const isUrl = msg.fromAgentAvatar.startsWith('http') || msg.fromAgentAvatar.startsWith('/');
      if (isUrl) {
        return <Avatar size={40} src={msg.fromAgentAvatar} style={{ marginRight: 8, flexShrink: 0 }} />;
      }
      return <Avatar size={40} style={{ marginRight: 8, flexShrink: 0, backgroundColor: getAvatarColor(msg.fromAgentName), fontSize: 20 }}>{msg.fromAgentAvatar}</Avatar>;
    }
    return <Avatar size={40} icon={<RobotOutlined />} style={{ backgroundColor: getAvatarColor(msg.fromAgentName), marginRight: 8, flexShrink: 0 }} />;
  };

  const getAgentTooltip = (agent: CollaborationAgent) => (
    <div style={{ maxWidth: 300 }}>
      <div><strong>{agent.agentName}</strong></div>
      {agent.agentType && <div>类型：{agent.agentType}</div>}
      {agent.role && <div>角色：{agent.role === 'Manager' ? '协调者' : agent.role}</div>}
      {agent.agentStatus && <div>状态：{agent.agentStatus}</div>}
      {agent.customPrompt && (
        <div style={{ marginTop: 4, borderTop: '1px solid #eee', paddingTop: 4 }}>
          <div style={{ color: '#999', fontSize: 11 }}>自定义提示词：</div>
          <div style={{ fontSize: 11, maxHeight: 120, overflow: 'auto', whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
            {agent.customPrompt.length > 200 ? agent.customPrompt.substring(0, 200) + '...' : agent.customPrompt}
          </div>
        </div>
      )}
    </div>
  );

  const renderModelTag = (msg: ChatMsg) => {
    if (!msg.modelName && !msg.llmConfigName) return null;
    const label = msg.llmConfigName
      ? (msg.modelName ? `${msg.llmConfigName}/${msg.modelName}` : msg.llmConfigName)
      : msg.modelName;
    return (
      <Tag color="geekblue" style={{ fontSize: 10, lineHeight: '16px', padding: '0 4px', margin: 0 }}>
        {label}
      </Tag>
    );
  };

  const renderMessageList = () => (
    <>
      {loadingHistory && (
        <div style={{ textAlign: 'center', padding: 8, color: '#999', fontSize: 12 }}>
          <LoadingOutlined /> 加载历史消息...
        </div>
      )}
      {!selectedId ? (
        <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="请先选择一个团队" />
      ) : currentMessages.length === 0 && !loadingHistory ? (
        <Empty
          image={Empty.PRESENTED_IMAGE_SIMPLE}
          description={
            isPrivateTab ? (
              <span>
                <MessageOutlined /> 与 {activePrivateAgent?.agentName ?? 'Agent'} 私聊
                <br />
                <span style={{ fontSize: 12, color: '#999' }}>直接发消息即可</span>
              </span>
            ) : (
              <span>
                <TeamOutlined /> 开始聊天
                <br />
                <span style={{ fontSize: 12, color: '#999' }}>
                  直接发消息由协调者回答，@某个人则指定Agent回答，@所有人让所有Agent回答
                </span>
              </span>
            )
          }
        />
      ) : (
        currentMessages.map(msg => (
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
                  {renderModelTag(msg)}
                  {msg.isMentioned && (
                    <Tag color="purple" style={{ fontSize: 10, lineHeight: '16px', padding: '0 4px', margin: 0 }}>@</Tag>
                  )}
                  <span style={{ fontSize: 11, color: '#bbb' }}>
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
    </>
  );

  const renderInputArea = () => (
    <div style={{ display: 'flex', gap: 8, flexShrink: 0 }}>
      {isPrivateTab ? (
        <textarea
          value={currentInputValue}
          onChange={(e) => setInputValue(e.target.value)}
          placeholder={`与 ${activePrivateAgent?.agentName ?? 'Agent'} 私聊中...`}
          style={{
            flex: 1,
            resize: 'none',
            border: '1px solid #d9d9d9',
            borderRadius: 6,
            padding: '8px 12px',
            fontSize: 14,
            lineHeight: 1.6,
            fontFamily: 'inherit',
            outline: 'none',
          }}
          rows={4}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
              e.preventDefault();
              handleSend();
            }
          }}
        />
      ) : (
        <Mentions
          value={currentInputValue}
          onChange={handleMentionChange}
          placeholder="输入消息，@ 提及指定Agent，@所有人 让所有Agent回答..."
          style={{ flex: 1 }}
          rows={4}
          onPressEnter={(e) => {
            if (!e.shiftKey) {
              e.preventDefault();
              handleSend();
            }
          }}
          options={[
            {
              value: '所有人',
              label: (
                <Space>
                  <Tag color="red" style={{ fontSize: 11 }}>全部</Tag>
                  <span>所有人</span>
                </Space>
              ),
            },
            ...agents.map(agent => ({
              value: agent.agentName,
              label: (
                <Space>
                  <Tag color={agent.role === 'Manager' ? 'gold' : 'blue'} style={{ fontSize: 11 }}>
                    {agent.role === 'Manager' ? '协调者' : agent.agentType || agent.role || 'Worker'}
                  </Tag>
                  <span>{agent.agentName}</span>
                </Space>
              ),
            })),
          ]}
          prefix="@"
        />
      )}
      <Button
        type="primary"
        icon={<SendOutlined />}
        onClick={handleSend}
        loading={sending}
        disabled={!selectedId}
        style={{ height: 54, width: 54 }}
      />
    </div>
  );

  const tabItems = [
    {
      key: 'group',
      label: <span><CommentOutlined /> 群聊</span>,
      closable: false,
      children: null,
    },
    ...privateTabs.map(tab => ({
      key: tab.key,
      label: <span><MessageOutlined /> {tab.agent.agentName}</span>,
      closable: true,
      children: null,
    })),
  ];

  return (
    <div style={{
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      bottom: 0,
      padding: 12,
      display: 'flex',
      gap: 12,
      background: '#f5f5f5',
    }}>
      <div style={{ width: 240, flexShrink: 0, display: 'flex', flexDirection: 'column' }}>
        <Card
          title={<span><TeamOutlined /> 选择团队</span>}
          style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}
          bodyStyle={{ flex: 1, padding: 12, overflow: 'auto' }}
        >
          <Select
            value={selectedId || undefined}
            onChange={(val) => {
              setSelectedId(val);
              setGroupMessages([]);
              setPrivateMessagesMap({});
              setInputValueMap({ group: '' });
              setMentionedIdsMap({ group: [] });
              setPrivateTabs([]);
              setActiveTabKey('group');
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
                      gap: 6,
                      padding: '6px 8px',
                      borderRadius: 6,
                      marginBottom: 4,
                      transition: 'background 0.2s',
                      background: activeTabKey === `private-${agent.agentId}` ? '#e6f4ff' : 'transparent',
                      border: activeTabKey === `private-${agent.agentId}` ? '1px solid #1677ff' : '1px solid transparent',
                    }}
                  >
                    {agent.agentAvatar ? (
                      (() => {
                        const isUrl = agent.agentAvatar.startsWith('http') || agent.agentAvatar.startsWith('/');
                        return isUrl
                          ? <Avatar size={28} src={agent.agentAvatar} />
                          : <Avatar size={28} style={{ backgroundColor: getAvatarColor(agent.agentName), fontSize: 14 }}>{agent.agentAvatar}</Avatar>;
                      })()
                    ) : (
                      <Avatar size={28} icon={<RobotOutlined />} style={{ backgroundColor: getAvatarColor(agent.agentName) }} />
                    )}
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ fontSize: 13, fontWeight: 500, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                        {agent.agentName}
                      </div>
                      <div style={{ fontSize: 11, color: '#999' }}>
                        {agent.agentType || 'Assistant'}
                      </div>
                    </div>
                    <Button
                      type={activeTabKey === `private-${agent.agentId}` ? 'primary' : 'default'}
                      size="small"
                      icon={<MessageOutlined />}
                      onClick={(e) => {
                        e.stopPropagation();
                        openPrivateTab(agent);
                      }}
                      style={{ fontSize: 11, padding: '0 6px', height: 24 }}
                    >
                      私聊
                    </Button>
                  </div>
                </Tooltip>
              ))}
            </div>
          )}
        </Card>
      </div>

      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0 }}>
        <Card
          title={null}
          style={{
            display: 'flex',
            flexDirection: 'column',
            overflow: 'hidden',
            height: chatPanelHeight ?? undefined,
            flex: chatPanelHeight == null ? '1 1 0%' : 'none',
            minHeight: 400,
          }}
          bodyStyle={{ flex: 1, display: 'flex', flexDirection: 'column', padding: 0, minHeight: 0, overflow: 'hidden' }}
        >
          <Tabs
            type="editable-card"
            activeKey={activeTabKey}
            onChange={(key) => setActiveTabKey(key)}
            onEdit={(targetKey, action) => {
              if (action === 'remove' && typeof targetKey === 'string') {
                closePrivateTab(targetKey);
              }
            }}
            hideAdd
            items={tabItems}
            style={{ padding: '0 12px', marginBottom: 0 }}
          />
          <div
            ref={chatPanelRef}
            style={{ flex: 1, display: 'flex', flexDirection: 'column', minHeight: 0, overflow: 'hidden', padding: '0 12px 12px' }}
          >
            <div
              ref={messagesContainerRef}
              onScroll={handleScroll}
              style={{
                flex: '1 1 0%',
                minHeight: MIN_MESSAGE_HEIGHT,
                overflowY: 'auto',
                padding: '12px',
                border: '1px solid #f0f0f0',
                borderRadius: 8,
                background: '#fafafa',
              }}
            >
              {renderMessageList()}
            </div>

            <div
              onMouseDown={handleDragMouseDown}
              style={{
                height: 6,
                cursor: 'ns-resize',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
                userSelect: 'none',
                margin: '4px 0',
                borderTop: '1px solid #e8e8e8',
              }}
            >
              <HolderOutlined style={{ color: '#bbb', fontSize: 10 }} />
            </div>

            {renderInputArea()}
          </div>
        </Card>
      </div>
    </div>
  );
};

export default CollaborationChat;
