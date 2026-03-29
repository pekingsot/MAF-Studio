import React, { useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { Card, Descriptions, Button, Tag, message } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { agentService, Agent } from '../services/agentService';

const AgentDetail: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [agent, setAgent] = useState<Agent | null>(null);
  const [loading, setLoading] = useState(true);
  const initializedRef = useRef(false);

  useEffect(() => {
    if (id && !initializedRef.current) {
      initializedRef.current = true;
      loadAgent(id);
    }
  }, [id]);

  const loadAgent = async (agentId: string) => {
    try {
      setLoading(true);
      const data = await agentService.getAgentById(parseInt(agentId, 10));
      setAgent(data);
    } catch (error) {
      message.error('加载智能体详情失败');
    } finally {
      setLoading(false);
    }
  };

  if (loading || !agent) {
    return <div>加载中...</div>;
  }

  const statusColorMap: Record<string, string> = {
    Active: 'green',
    Inactive: 'default',
    Busy: 'orange',
    Error: 'red',
  };

  return (
    <div>
      <Button
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate('/agents')}
        style={{ marginBottom: 16 }}
      >
        返回列表
      </Button>

      <Card title={agent.name} loading={loading}>
        <Descriptions bordered column={2}>
          <Descriptions.Item label="ID">{agent.id}</Descriptions.Item>
          <Descriptions.Item label="类型">{agent.type}</Descriptions.Item>
          <Descriptions.Item label="状态">
            <Tag color={statusColorMap[agent.status]}>{agent.status}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="创建时间">
            {new Date(agent.createdAt).toLocaleString('zh-CN')}
          </Descriptions.Item>
          <Descriptions.Item label="最后活跃时间">
            {agent.lastActiveAt
              ? new Date(agent.lastActiveAt).toLocaleString('zh-CN')
              : '从未活跃'}
          </Descriptions.Item>
          <Descriptions.Item label="更新时间">
            {agent.updatedAt
              ? new Date(agent.updatedAt).toLocaleString('zh-CN')
              : '未更新'}
          </Descriptions.Item>
          <Descriptions.Item label="描述" span={2}>
            {agent.description || '无描述'}
          </Descriptions.Item>
          <Descriptions.Item label="系统提示词" span={2}>
            <pre style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
              {agent.systemPrompt || '未设置'}
            </pre>
          </Descriptions.Item>
        </Descriptions>
      </Card>
    </div>
  );
};

export default AgentDetail;
