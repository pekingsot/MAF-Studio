import React from 'react';
import { Card, Button, Tabs } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useCollaborationDetail } from './useCollaborationDetail';
import CollaborationInfo from './CollaborationInfo';
import AgentTable from './AgentTable';
import TaskTable from './TaskTable';
import ChatHistory from './ChatHistory';

const CollaborationDetail: React.FC = () => {
  const navigate = useNavigate();
  const { id, collaboration, loading, handleRemoveAgent, loadCollaboration } = useCollaborationDetail();

  if (loading || !collaboration) {
    return <div>加载中...</div>;
  }

  return (
    <div>
      <Button
        icon={<ArrowLeftOutlined />}
        onClick={() => navigate('/collaborations')}
        style={{ marginBottom: 16 }}
      >
        返回列表
      </Button>

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
              children: <TaskTable tasks={collaboration.tasks} />,
            },
            {
              key: 'chat',
              label: '协作过程',
              children: <ChatHistory collaborationId={id || ''} />,
            },
          ]}
          style={{ marginTop: 24 }}
        />
      </Card>
    </div>
  );
};

export default CollaborationDetail;
