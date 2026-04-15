import React from 'react';
import { Card, Button, Tabs, Space } from 'antd';
import { ArrowLeftOutlined, HistoryOutlined, MessageOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useCollaborationDetail } from './useCollaborationDetail';
import CollaborationInfo from './CollaborationInfo';
import AgentTable from './AgentTable';
import TaskTable from './TaskTable';
import ChatHistory from './ChatHistory';
import CollaborationChatPanel from './CollaborationChatPanel';

const CollaborationDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id, collaboration, loading, handleRemoveAgent, loadCollaboration } = useCollaborationDetail();

  if (loading || !collaboration) {
    return <div>加载中...</div>;
  }

  return (
    <div>
      <Space style={{ marginBottom: 16 }}>
        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate('/collaborations')}
        >
          返回列表
        </Button>
        <Button
          type="primary"
          icon={<HistoryOutlined />}
          onClick={() => navigate(`/collaborations/${id}/coordination`)}
        >
          查看协调记录
        </Button>
      </Space>

      <Card title={collaboration.name} loading={loading}>
        <CollaborationInfo collaboration={collaboration} />

        <Tabs
          defaultActiveKey="agents"
          items={[
            {
              key: 'agents',
              label: `智能体 (${collaboration.agents.length})`,
              children: (
                <AgentTable
                  agents={collaboration.agents}
                  collaborationId={Number(id)}
                  onRemove={handleRemoveAgent}
                  onUpdate={() => id && loadCollaboration(id)}
                />
              ),
            },
            {
              key: 'tasks',
              label: `任务 (${collaboration.tasks.length})`,
              children: (
                <TaskTable
                  tasks={collaboration.tasks}
                  agents={collaboration.agents}
                  collaborationId={id || ''}
                  onUpdate={() => id && loadCollaboration(id)}
                />
              ),
            },
            {
              key: 'chat',
              label: '协作过程',
              children: <ChatHistory collaborationId={id || ''} />,
            },
            {
              key: 'livechat',
              label: (
                <span>
                  <MessageOutlined style={{ marginRight: 4 }} />
                  协作聊天
                </span>
              ),
              children: (
                <CollaborationChatPanel
                  collaborationId={id || ''}
                  agents={collaboration.agents}
                />
              ),
            },
          ]}
          style={{ marginTop: 24 }}
        />
      </Card>
    </div>
  );
};

export default CollaborationDetail;
