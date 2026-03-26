import React, { useEffect, useState, useRef } from 'react';
import { Table, Card, Select, Input, DatePicker, Button, Space, Tag, Modal, Descriptions, Statistic, Row, Col, message, Popconfirm, Tooltip } from 'antd';
import { SearchOutlined, ReloadOutlined, DeleteOutlined, ClearOutlined, ExclamationCircleOutlined, BugOutlined, WarningOutlined, InfoCircleOutlined, StopOutlined } from '@ant-design/icons';
import api from '../services/api';
import dayjs from 'dayjs';

const { Option } = Select;
const { RangePicker } = DatePicker;

interface SystemLog {
  id: string;
  level: string;
  category?: string;
  message: string;
  exception?: string;
  stackTrace?: string;
  requestPath?: string;
  requestMethod?: string;
  userId?: string;
  userName?: string;
  ipAddress?: string;
  createdAt: string;
  extraData?: string;
}

interface LogStatistics {
  levelCounts: { level: string; count: number }[];
  dailyCounts: { date: string; count: number }[];
  totalErrors: number;
  periodDays: number;
}

const SystemLogs: React.FC = () => {
  const [logs, setLogs] = useState<SystemLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [statistics, setStatistics] = useState<LogStatistics | null>(null);
  const [levels, setLevels] = useState<string[]>([]);
  const [categories, setCategories] = useState<string[]>([]);
  const [pagination, setPagination] = useState({ current: 1, pageSize: 20, total: 0 });
  const [filters, setFilters] = useState<{
    level?: string;
    category?: string;
    keyword?: string;
    startTime?: string;
    endTime?: string;
  }>({});
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadLogs();
      loadStatistics();
      loadLevels();
      loadCategories();
    }
  }, []);

  const loadLogs = async (page = 1, pageSize = 20) => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      params.append('page', page.toString());
      params.append('pageSize', pageSize.toString());
      
      if (filters.level) params.append('level', filters.level);
      if (filters.category) params.append('category', filters.category);
      if (filters.keyword) params.append('keyword', filters.keyword);
      if (filters.startTime) params.append('startTime', filters.startTime);
      if (filters.endTime) params.append('endTime', filters.endTime);

      const response = await api.get(`/systemlogs?${params.toString()}`);
      setLogs(response.data.data);
      setPagination({
        current: response.data.page,
        pageSize: response.data.pageSize,
        total: response.data.total
      });
    } catch (error) {
      message.error('加载日志失败');
    } finally {
      setLoading(false);
    }
  };

  const loadStatistics = async () => {
    try {
      const response = await api.get('/systemlogs/statistics?days=7');
      setStatistics(response.data);
    } catch (error) {
      console.error('加载统计信息失败', error);
    }
  };

  const loadLevels = async () => {
    try {
      const response = await api.get('/systemlogs/levels');
      setLevels(response.data);
    } catch (error) {
      console.error('加载日志级别失败', error);
    }
  };

  const loadCategories = async () => {
    try {
      const response = await api.get('/systemlogs/categories');
      setCategories(response.data);
    } catch (error) {
      console.error('加载日志类别失败', error);
    }
  };

  const handleSearch = () => {
    setPagination(prev => ({ ...prev, current: 1 }));
    loadLogs(1, pagination.pageSize);
  };

  const handleReset = () => {
    setFilters({});
    setPagination(prev => ({ ...prev, current: 1 }));
    setTimeout(() => loadLogs(1, pagination.pageSize), 100);
  };

  const handleTableChange = (pag: any) => {
    loadLogs(pag.current, pag.pageSize);
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/systemlogs/${id}`);
      message.success('删除成功');
      loadLogs(pagination.current, pagination.pageSize);
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleClear = async (beforeDays?: number) => {
    try {
      const params = beforeDays ? `?beforeDays=${beforeDays}` : '';
      const response = await api.delete(`/systemlogs/clear${params}`);
      message.success(`已清理 ${response.data.deletedCount} 条日志`);
      loadLogs();
      loadStatistics();
    } catch (error) {
      message.error('清理失败');
    }
  };

  const showLogDetail = (log: SystemLog) => {
    Modal.info({
      title: '日志详情',
      width: 800,
      content: (
        <Descriptions column={2} bordered size="small" style={{ marginTop: 16 }}>
          <Descriptions.Item label="级别" span={1}>
            <Tag color={getLevelColor(log.level)}>{log.level}</Tag>
          </Descriptions.Item>
          <Descriptions.Item label="类别" span={1}>{log.category || '-'}</Descriptions.Item>
          <Descriptions.Item label="创建时间" span={2}>{dayjs(log.createdAt).format('YYYY-MM-DD HH:mm:ss')}</Descriptions.Item>
          <Descriptions.Item label="消息" span={2}>
            <div style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>{log.message}</div>
          </Descriptions.Item>
          {log.exception && (
            <Descriptions.Item label="异常信息" span={2}>
              <div style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', color: '#ff4d4f' }}>{log.exception}</div>
            </Descriptions.Item>
          )}
          {log.stackTrace && (
            <Descriptions.Item label="堆栈跟踪" span={2}>
              <div style={{ 
                whiteSpace: 'pre-wrap', 
                wordBreak: 'break-word', 
                fontSize: 11, 
                background: '#f5f5f5', 
                padding: 8, 
                borderRadius: 4,
                maxHeight: 200,
                overflow: 'auto'
              }}>{log.stackTrace}</div>
            </Descriptions.Item>
          )}
          <Descriptions.Item label="请求路径" span={2}>{log.requestPath || '-'}</Descriptions.Item>
          <Descriptions.Item label="请求方法">{log.requestMethod || '-'}</Descriptions.Item>
          <Descriptions.Item label="IP地址">{log.ipAddress || '-'}</Descriptions.Item>
          <Descriptions.Item label="用户">{log.userName || '-'}</Descriptions.Item>
          <Descriptions.Item label="用户ID">{log.userId || '-'}</Descriptions.Item>
        </Descriptions>
      ),
    });
  };

  const getLevelColor = (level: string) => {
    const colors: Record<string, string> = {
      Trace: 'default',
      Debug: 'default',
      Information: 'blue',
      Warning: 'orange',
      Error: 'red',
      Critical: 'magenta',
    };
    return colors[level] || 'default';
  };

  const getLevelIcon = (level: string) => {
    switch (level) {
      case 'Error':
      case 'Critical':
        return <ExclamationCircleOutlined style={{ color: '#ff4d4f' }} />;
      case 'Warning':
        return <WarningOutlined style={{ color: '#faad14' }} />;
      case 'Information':
        return <InfoCircleOutlined style={{ color: '#1890ff' }} />;
      default:
        return <BugOutlined style={{ color: '#999' }} />;
    }
  };

  const columns = [
    {
      title: '级别',
      dataIndex: 'level',
      key: 'level',
      width: 100,
      render: (level: string) => (
        <Tag color={getLevelColor(level)} icon={getLevelIcon(level)}>
          {level}
        </Tag>
      ),
    },
    {
      title: '类别',
      dataIndex: 'category',
      key: 'category',
      width: 200,
      ellipsis: true,
      render: (category: string) => category || '-',
    },
    {
      title: '消息',
      dataIndex: 'message',
      key: 'message',
      ellipsis: true,
      render: (message: string) => (
        <Tooltip title={message}>
          <span style={{ cursor: 'pointer' }}>{message}</span>
        </Tooltip>
      ),
    },
    {
      title: '请求路径',
      dataIndex: 'requestPath',
      key: 'requestPath',
      width: 150,
      ellipsis: true,
      render: (path: string) => path || '-',
    },
    {
      title: '用户',
      dataIndex: 'userName',
      key: 'userName',
      width: 80,
      render: (name: string) => name || '-',
    },
    {
      title: '时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (time: string) => dayjs(time).format('MM-DD HH:mm:ss'),
    },
    {
      title: '操作',
      key: 'action',
      width: 80,
      render: (_: any, record: SystemLog) => (
        <Space size={0}>
          <Button type="link" size="small" onClick={() => showLogDetail(record)}>
            详情
          </Button>
          <Popconfirm
            title="确定删除此日志？"
            onConfirm={() => handleDelete(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" size="small" danger>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <h2 style={{ marginBottom: 16 }}>系统日志</h2>

      {statistics && (
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}>
            <Card size="small">
              <Statistic
                title="近7天错误数"
                value={statistics.totalErrors}
                valueStyle={{ color: statistics.totalErrors > 0 ? '#ff4d4f' : '#52c41a' }}
                prefix={<ExclamationCircleOutlined />}
              />
            </Card>
          </Col>
          {statistics.levelCounts.slice(0, 3).map(item => (
            <Col span={6} key={item.level}>
              <Card size="small">
                <Statistic
                  title={item.level}
                  value={item.count}
                  valueStyle={{ color: getLevelColor(item.level) === 'red' ? '#ff4d4f' : getLevelColor(item.level) === 'orange' ? '#faad14' : undefined }}
                />
              </Card>
            </Col>
          ))}
        </Row>
      )}

      <Card style={{ marginBottom: 16 }}>
        <Space wrap>
          <Select
            allowClear
            placeholder="日志级别"
            style={{ width: 120 }}
            value={filters.level}
            onChange={level => setFilters(prev => ({ ...prev, level }))}
          >
            {levels.map(level => (
              <Option key={level} value={level}>{level}</Option>
            ))}
          </Select>

          <Select
            allowClear
            showSearch
            placeholder="日志类别"
            style={{ width: 200 }}
            value={filters.category}
            onChange={category => setFilters(prev => ({ ...prev, category }))}
            optionFilterProp="children"
          >
            {categories.map(cat => (
              <Option key={cat} value={cat}>{cat}</Option>
            ))}
          </Select>

          <Input
            placeholder="关键词搜索"
            style={{ width: 200 }}
            value={filters.keyword}
            onChange={e => setFilters(prev => ({ ...prev, keyword: e.target.value }))}
            onPressEnter={handleSearch}
          />

          <RangePicker
            showTime
            onChange={(dates) => {
              if (dates && dates[0] && dates[1]) {
                setFilters(prev => ({
                  ...prev,
                  startTime: dates[0]!.toISOString(),
                  endTime: dates[1]!.toISOString()
                }));
              } else {
                setFilters(prev => ({
                  ...prev,
                  startTime: undefined,
                  endTime: undefined
                }));
              }
            }}
          />

          <Button type="primary" icon={<SearchOutlined />} onClick={handleSearch}>
            搜索
          </Button>
          <Button onClick={handleReset}>重置</Button>
          <Button icon={<ReloadOutlined />} onClick={() => loadLogs(pagination.current, pagination.pageSize)}>
            刷新
          </Button>

          <Popconfirm
            title="确定清理7天前的日志？"
            onConfirm={() => handleClear(7)}
            okText="确定"
            cancelText="取消"
          >
            <Button danger icon={<ClearOutlined />}>
              清理7天前日志
            </Button>
          </Popconfirm>
        </Space>
      </Card>

      <Table
        columns={columns}
        dataSource={logs}
        rowKey="id"
        loading={loading}
        pagination={{
          ...pagination,
          showSizeChanger: true,
          showTotal: (total) => `共 ${total} 条`,
        }}
        onChange={handleTableChange}
        size="small"
        scroll={{ x: 1200 }}
      />
    </div>
  );
};

export default SystemLogs;
