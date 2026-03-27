import React, { useEffect, useState, useRef } from 'react';
import { Table, Select, Card, Input, Button, Tag, message } from 'antd';
import { SendOutlined } from '@ant-design/icons';
import { agentService, Agent } from '../services/agentService';
import socketService from '../services/socketService';

const { TextArea } = Input;
const { Option } = Select;

const Messages: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [selectedAgent1, setSelectedAgent1] = useState<string>('');
  const [selectedAgent2, setSelectedAgent2] = useState<string>('');
  const [messages, setMessages] = useState<any[]>([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadAgents();
      socketService.connect();
      
      socketService.onMessage((message) => {
        setMessages(prev => [...prev, message]);
      });
    }
    return () => {
      socketService.disconnect();
    };
  }, []);

  useEffect(() => {
    if (selectedAgent1 && selectedAgent2) {
      loadMessages();
      socketService.joinAgentGroup(selectedAgent1);
    }
  }, [selectedAgent1, selectedAgent2]);

  const loadAgents = async () => {
    try {
      setLoading(true);
      const response = await agentService.getAllAgents();
      setAgents(response.agents || []);
    } catch (error) {
      message.error('加载智能体列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadMessages = async () => {
    try {
      setLoading(true);
      const response = await fetch(
        `http://localhost:5000/api/messages/conversation/${selectedAgent1}/${selectedAgent2}`
      );
      const data = await response.json();
      setMessages(data);
    } catch (error) {
      message.error('加载消息失败');
    } finally {
      setLoading(false);
    }
  };

  const handleSendMessage = () => {
    if (!newMessage.trim()) {
      message.warning('请输入消息内容');
      return;
    }

    socketService.sendMessage(
      selectedAgent1,
      selectedAgent2,
      newMessage,
      'Text'
    );
    setNewMessage('');
  };

  const columns = [
    {
      title: '发送者',
      dataIndex: ['fromAgent', 'name'],
      key: 'fromAgent',
    },
    {
      title: '接收者',
      dataIndex: ['toAgent', 'name'],
      key: 'toAgent',
    },
    {
      title: '消息内容',
      dataIndex: 'content',
      key: 'content',
      ellipsis: true,
    },
    {
      title: '类型',
      dataIndex: 'type',
      key: 'type',
      render: (type: string) => {
        const colorMap: Record<string, string> = {
          Text: 'blue',
          Command: 'orange',
          Query: 'green',
          Response: 'purple',
          Error: 'red',
        };
        return <Tag color={colorMap[type]}>{type}</Tag>;
      },
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      render: (status: string) => {
        const colorMap: Record<string, string> = {
          Pending: 'default',
          Processing: 'processing',
          Completed: 'success',
          Failed: 'error',
        };
        return <Tag color={colorMap[status]}>{status}</Tag>;
      },
    },
    {
      title: '发送时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (date: string) => new Date(date).toLocaleString('zh-CN'),
    },
  ];

  return (
    <div>
      <h2 style={{ marginBottom: 24 }}>消息中心</h2>

      <Card style={{ marginBottom: 24 }}>
        <div style={{ display: 'flex', gap: 16, marginBottom: 16 }}>
          <Select
            style={{ flex: 1 }}
            placeholder="选择发送者智能体"
            value={selectedAgent1}
            onChange={setSelectedAgent1}
            loading={loading}
          >
            {agents.map(agent => (
              <Option key={agent.id} value={agent.id}>
                {agent.name} ({agent.type})
              </Option>
            ))}
          </Select>

          <Select
            style={{ flex: 1 }}
            placeholder="选择接收者智能体"
            value={selectedAgent2}
            onChange={setSelectedAgent2}
            loading={loading}
          >
            {agents.map(agent => (
              <Option key={agent.id} value={agent.id}>
                {agent.name} ({agent.type})
              </Option>
            ))}
          </Select>
        </div>

        {selectedAgent1 && selectedAgent2 && (
          <div style={{ display: 'flex', gap: 16 }}>
            <TextArea
              style={{ flex: 1 }}
              rows={3}
              placeholder="输入消息内容..."
              value={newMessage}
              onChange={(e) => setNewMessage(e.target.value)}
              onPressEnter={(e) => {
                if (e.shiftKey) return;
                e.preventDefault();
                handleSendMessage();
              }}
            />
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSendMessage}
              disabled={!newMessage.trim()}
            >
              发送
            </Button>
          </div>
        )}
      </Card>

      <Card title="消息历史">
        <Table
          dataSource={messages}
          columns={columns}
          rowKey="id"
          loading={loading}
          pagination={{ pageSize: 10 }}
        />
      </Card>
    </div>
  );
};

export default Messages;