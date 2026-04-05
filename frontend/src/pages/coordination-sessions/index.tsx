import React, { useEffect, useState } from 'react';
import { Table, Card, Tag, Button, Space, Typography, Empty, Spin, message } from 'antd';
import { HistoryOutlined, EyeOutlined, ArrowLeftOutlined, SwapOutlined, CrownOutlined, BulbOutlined } from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import { coordinationService, CoordinationSession } from '../../services/coordinationService';

const { Title, Text } = Typography;

const orchestrationModeConfig: Record<string, { label: string; icon: React.ReactNode; color: string }> = {
  RoundRobin: {
    label: '轮询模式',
    icon: <SwapOutlined />,
    color: 'blue',
  },
  Manager: {
    label: '主Agent协调',
    icon: <CrownOutlined />,
    color: 'gold',
  },
  Intelligent: {
    label: 'AI智能选择',
    icon: <BulbOutlined />,
    color: 'purple',
  }
};

const statusConfig: Record<string, { label: string; color: string }> = {
  running: { label: '进行中', color: 'processing' },
  completed: { label: '已完成', color: 'success' },
  failed: { label: '失败', color: 'error' },
  cancelled: { label: '已取消', color: 'default' }
};

const CoordinationSessions: React.FC = () => {
  const { collaborationId } = useParams<{ collaborationId: string }>();
  const navigate = useNavigate();
  const [sessions, setSessions] = useState<CoordinationSession[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadSessions();
  }, [collaborationId]);

  const loadSessions = async () => {
    if (!collaborationId) return;
    
    try {
      setLoading(true);
      const data = await coordinationService.getSessions(Number(collaborationId));
      setSessions(data);
    } catch (error) {
      message.error('加载协调会话列表失败');
    } finally {
      setLoading(false);
    }
  };

  const columns = [
    {
      title: '主题',
      dataIndex: 'topic',
      key: 'topic',
      ellipsis: true,
      render: (topic: string) => topic || '-'
    },
    {
      title: '协调模式',
      dataIndex: 'orchestrationMode',
      key: 'orchestrationMode',
      width: 140,
      render: (mode: string) => {
        const config = orchestrationModeConfig[mode] || { label: mode, icon: null, color: 'default' };
        return (
          <Tag icon={config.icon} color={config.color}>
            {config.label}
          </Tag>
        );
      }
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (status: string) => {
        const config = statusConfig[status] || { label: status, color: 'default' };
        return <Tag color={config.color}>{config.label}</Tag>;
      }
    },
    {
      title: '轮次',
      dataIndex: 'totalRounds',
      key: 'totalRounds',
      width: 80,
      render: (rounds: number) => rounds || 0
    },
    {
      title: '消息数',
      dataIndex: 'totalMessages',
      key: 'totalMessages',
      width: 80,
      render: (messages: number) => messages || 0
    },
    {
      title: '开始时间',
      dataIndex: 'startTime',
      key: 'startTime',
      width: 170,
      render: (time: string) => time ? new Date(time).toLocaleString('zh-CN') : '-'
    },
    {
      title: '结束时间',
      dataIndex: 'endTime',
      key: 'endTime',
      width: 170,
      render: (time: string) => time ? new Date(time).toLocaleString('zh-CN') : '-'
    },
    {
      title: '操作',
      key: 'action',
      width: 100,
      render: (_: any, record: CoordinationSession) => (
        <Button
          type="link"
          size="small"
          icon={<EyeOutlined />}
          onClick={() => navigate(`/collaborations/${collaborationId}/coordination/${record.id}`)}
        >
          详情
        </Button>
      )
    }
  ];

  return (
    <Card>
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <Space style={{ justifyContent: 'space-between', width: '100%' }}>
          <Space>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate(`/collaborations/${collaborationId}`)}
            >
              返回协作详情
            </Button>
            <Title level={4} style={{ margin: 0 }}>
              <HistoryOutlined style={{ marginRight: 8 }} />
              协调会话记录
            </Title>
          </Space>
        </Space>

        <Spin spinning={loading}>
          {sessions.length === 0 && !loading ? (
            <Empty description="暂无协调会话记录" />
          ) : (
            <Table
              columns={columns}
              dataSource={sessions}
              rowKey="id"
              pagination={{
                pageSize: 10,
                showSizeChanger: true,
                showTotal: (total) => `共 ${total} 条记录`
              }}
            />
          )}
        </Spin>
      </Space>
    </Card>
  );
};

export default CoordinationSessions;
