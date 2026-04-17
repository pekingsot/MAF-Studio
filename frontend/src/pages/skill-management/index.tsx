import React, { useState, useEffect } from 'react';
import { Card, Select, Space, Empty, message } from 'antd';
import { ThunderboltOutlined } from '@ant-design/icons';
import AgentSkillPanel from './AgentSkillPanel';
import { agentService, Agent } from '../../services/agentService';

const { Option } = Select;

const SkillManagementPage: React.FC = () => {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [selectedAgentId, setSelectedAgentId] = useState<number | null>(null);
  const [selectedAgentName, setSelectedAgentName] = useState('');

  useEffect(() => {
    loadAgents();
  }, []);

  const loadAgents = async () => {
    try {
      const data = await agentService.getAllAgents();
      setAgents(data);
    } catch (error: unknown) {
      message.error('加载Agent列表失败');
    }
  };

  return (
    <div>
      <Card title="Agent 技能管理" style={{ marginBottom: 16 }}>
        <Space>
          <ThunderboltOutlined />
          <span>选择 Agent：</span>
          <Select
            style={{ width: 300 }}
            placeholder="请选择要配置技能的 Agent"
            onChange={(value) => {
              const agent = agents.find(a => a.id === value);
              setSelectedAgentId(value);
              setSelectedAgentName(agent?.name || '');
            }}
            value={selectedAgentId || undefined}
            showSearch
            optionFilterProp="children"
          >
            {agents.map(agent => (
              <Option key={agent.id} value={agent.id}>
                {agent.name} - {agent.type}
              </Option>
            ))}
          </Select>
        </Space>
      </Card>

      {selectedAgentId ? (
        <AgentSkillPanel agentId={selectedAgentId} agentName={selectedAgentName} />
      ) : (
        <Card>
          <Empty description="请先选择一个 Agent 来管理其技能" />
        </Card>
      )}
    </div>
  );
};

export default SkillManagementPage;
