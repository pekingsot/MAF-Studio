import React from 'react';
import { Card, Button, Tabs } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useCollaborationDetail } from './useCollaborationDetail';
import CollaborationInfo from './CollaborationInfo';
import AgentTable from './AgentTable';
import TaskTable from './TaskTable';

const CollaborationDetail: React.FC = () => {
  const navigate = useNavigate();
  const { collaboration, loading, handleRemoveAgent } = useCollaborationDetail();

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
                  onRemove={handleRemoveAgent}
                />
              ),
            },
            {
              key: 'tasks',
              label: `任务 (${collaboration.tasks.length})`,
              children: <TaskTable tasks={collaboration.tasks} />,
            },
          ]}
          style={{ marginTop: 24 }}
        />
      </Card>
    </div>
  );
};

export default CollaborationDetail;
