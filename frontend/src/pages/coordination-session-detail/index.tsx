import React, { useEffect, useState } from 'react';
import { Card, Descriptions, Tag, Button, Space, Typography, Empty, Spin, message, Timeline, Avatar, Row, Col, Statistic, Divider } from 'antd';
import { ArrowLeftOutlined, SwapOutlined, CrownOutlined, BulbOutlined, UserOutlined, MessageOutlined, ClockCircleOutlined, TeamOutlined } from '@ant-design/icons';
import { useNavigate, useParams } from 'react-router-dom';
import { coordinationService, CoordinationSessionDetail as SessionDetail, CoordinationRound, CoordinationParticipant } from '../../services/coordinationService';

const { Title, Text, Paragraph } = Typography;

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

const CoordinationSessionDetailPage: React.FC = () => {
  const { collaborationId, sessionId } = useParams<{ collaborationId: string; sessionId: string }>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [detail, setDetail] = useState<SessionDetail | null>(null);

  useEffect(() => {
    loadDetail();
  }, [sessionId]);

  const loadDetail = async () => {
    if (!sessionId) return;
    
    try {
      setLoading(true);
      const data = await coordinationService.getSessionDetail(Number(sessionId));
      setDetail(data);
    } catch (error) {
      message.error('加载协调会话详情失败');
    } finally {
      setLoading(false);
    }
  };

  const formatDuration = (startTime: string, endTime: string | null) => {
    if (!endTime) return '进行中';
    const start = new Date(startTime).getTime();
    const end = new Date(endTime).getTime();
    const duration = Math.floor((end - start) / 1000);
    
    if (duration < 60) return `${duration}秒`;
    if (duration < 3600) return `${Math.floor(duration / 60)}分${duration % 60}秒`;
    return `${Math.floor(duration / 3600)}小时${Math.floor((duration % 3600) / 60)}分`;
  };

  const getAvatarColor = (name: string) => {
    const colors = ['#1890ff', '#52c41a', '#faad14', '#eb2f96', '#722ed1', '#13c2c2'];
    let hash = 0;
    for (let i = 0; i < name.length; i++) {
      hash = name.charCodeAt(i) + ((hash << 5) - hash);
    }
    return colors[Math.abs(hash) % colors.length];
  };

  const renderRoundItem = (round: CoordinationRound) => {
    const avatarColor = getAvatarColor(round.speakerName);
    
    return (
      <Timeline.Item
        key={round.id}
        dot={
          <Avatar
            size="small"
            style={{ backgroundColor: avatarColor }}
            icon={<UserOutlined />}
          >
            {round.speakerName.charAt(0)}
          </Avatar>
        }
      >
        <Card size="small" style={{ marginBottom: 8 }}>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Space>
              <Text strong>{round.speakerName}</Text>
              {round.speakerRole && (
                <Tag color={round.speakerRole === 'Manager' ? 'gold' : 'blue'}>
                  {round.speakerRole}
                </Tag>
              )}
              <Text type="secondary" style={{ fontSize: 12 }}>
                第 {round.roundNumber} 轮
              </Text>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {new Date(round.createdAt).toLocaleTimeString('zh-CN')}
              </Text>
            </Space>
            <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
              {round.messageContent}
            </Paragraph>
            {round.thinkingProcess && (
              <Text type="secondary" style={{ fontSize: 12, fontStyle: 'italic' }}>
                思考过程: {round.thinkingProcess}
              </Text>
            )}
            {round.selectedNextSpeaker && (
              <Text type="secondary" style={{ fontSize: 12 }}>
                选择下一位发言者: {round.selectedNextSpeaker}
                {round.selectionReason && ` (原因: ${round.selectionReason})`}
              </Text>
            )}
          </Space>
        </Card>
      </Timeline.Item>
    );
  };

  if (loading) {
    return (
      <Card>
        <Spin />
      </Card>
    );
  }

  if (!detail) {
    return (
      <Card>
        <Empty description="协调会话不存在" />
      </Card>
    );
  }

  const { session, rounds, participants } = detail;
  const modeConfig = orchestrationModeConfig[session.orchestrationMode] || { label: session.orchestrationMode, icon: null, color: 'default' };
  const statusConf = statusConfig[session.status] || { label: session.status, color: 'default' };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size="large">
      <Card>
        <Space style={{ justifyContent: 'space-between', width: '100%' }}>
          <Space>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate(`/collaborations/${collaborationId}/coordination`)}
            >
              返回列表
            </Button>
            <Title level={4} style={{ margin: 0 }}>
              协调会话详情
            </Title>
          </Space>
          <Tag icon={statusConf.color === 'success' ? null : undefined} color={statusConf.color}>
            {statusConf.label}
          </Tag>
        </Space>
      </Card>

      <Card title="基本信息">
        <Descriptions column={3} bordered size="small">
          <Descriptions.Item label="协调模式">
            <Tag icon={modeConfig.icon} color={modeConfig.color}>
              {modeConfig.label}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="状态">
            <Tag color={statusConf.color}>{statusConf.label}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="持续时间">
            {formatDuration(session.startTime, session.endTime)}
          </Descriptions.Item>
          <Descriptions.Item label="开始时间">
            {new Date(session.startTime).toLocaleString('zh-CN')}
          </Descriptions.Item>
          <Descriptions.Item label="结束时间">
            {session.endTime ? new Date(session.endTime).toLocaleString('zh-CN') : '-'}
          </Descriptions.Item>
          <Descriptions.Item label="总轮次">
            {session.totalRounds} 轮
          </Descriptions.Item>
          <Descriptions.Item label="主题" span={3}>
            {session.topic || '-'}
          </Descriptions.Item>
          {session.conclusion && (
            <Descriptions.Item label="结论" span={3}>
              <Paragraph style={{ margin: 0, whiteSpace: 'pre-wrap' }}>
                {session.conclusion}
              </Paragraph>
            </Descriptions.Item>
          )}
        </Descriptions>
      </Card>

      <Card title="参与者统计">
        <Row gutter={16}>
          {participants.map((participant) => (
            <Col span={Math.min(6, 24 / participants.length)} key={participant.id}>
              <Card size="small">
                <Space direction="vertical" style={{ width: '100%' }}>
                  <Space>
                    <Avatar style={{ backgroundColor: getAvatarColor(participant.agentName) }}>
                      {participant.agentName.charAt(0)}
                    </Avatar>
                    <Space direction="vertical" size={0}>
                      <Text strong>{participant.agentName}</Text>
                      {participant.agentRole && (
                        <Tag color={participant.isManager ? 'gold' : 'blue'} style={{ margin: 0 }}>
                          {participant.agentRole}
                        </Tag>
                      )}
                    </Space>
                  </Space>
                  <Divider style={{ margin: '8px 0' }} />
                  <Row gutter={8}>
                    <Col span={12}>
                      <Statistic
                        title="发言次数"
                        value={participant.speakCount}
                        prefix={<MessageOutlined />}
                        valueStyle={{ fontSize: 16 }}
                      />
                    </Col>
                    <Col span={12}>
                      <Statistic
                        title="Token数"
                        value={participant.totalTokens}
                        prefix={<TeamOutlined />}
                        valueStyle={{ fontSize: 16 }}
                      />
                    </Col>
                  </Row>
                </Space>
              </Card>
            </Col>
          ))}
        </Row>
      </Card>

      <Card title="协调过程">
        {rounds.length === 0 ? (
          <Empty description="暂无协调记录" />
        ) : (
          <Timeline>
            {rounds.map(renderRoundItem)}
          </Timeline>
        )}
      </Card>
    </Space>
  );
};

export default CoordinationSessionDetailPage;
