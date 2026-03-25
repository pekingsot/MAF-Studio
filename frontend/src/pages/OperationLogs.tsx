import React, { useEffect, useState, useRef } from 'react';
import { Card, Table, Tag, Space, Typography, DatePicker, Select, Button, Divider } from 'antd';
import { HistoryOutlined, ReloadOutlined } from '@ant-design/icons';
import api from '../services/api';
import dayjs from 'dayjs';

const { Title, Text } = Typography;
const { Option } = Select;
const { RangePicker } = DatePicker;

interface OperationLog {
  id: string;
  userId: string;
  operation: string;
  module: string;
  description: string;
  details: string;
  ipAddress: string;
  createdAt: string;
  user: {
    username: string;
  };
}

const OperationLogs: React.FC = () => {
  const [logs, setLogs] = useState<OperationLog[]>([]);
  const [loading, setLoading] = useState(false);
  const [module, setModule] = useState<string>('');
  const [operation, setOperation] = useState<string>('');
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadLogs();
    }
  }, []);

  const loadLogs = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams();
      if (module) params.append('module', module);
      if (operation) params.append('operation', operation);
      
      const response = await api.get(`/logs?${params}`);
      setLogs(response.data);
    } catch (error) {
      console.error('加载日志失败:', error);
    } finally {
      setLoading(false);
    }
  };

  const getOperationColor = (operation: string) => {
    const colors: Record<string, string> = {
      '创建': 'green',
      '修改': 'blue',
      '删除': 'red',
      '登录': 'cyan',
      '登出': 'orange',
      '查看': 'default',
    };
    return colors[operation] || 'default';
  };

  const getModuleColor = (module: string) => {
    const colors: Record<string, string> = {
      '智能体': 'purple',
      '协作': 'geekblue',
      '消息': 'cyan',
      '用户': 'gold',
      '系统': 'default',
    };
    return colors[module] || 'default';
  };

  const columns = [
    {
      title: '时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 180,
      render: (date: string) => dayjs(date).format('YYYY-MM-DD HH:mm:ss'),
    },
    {
      title: '用户',
      dataIndex: ['user', 'username'],
      key: 'username',
      width: 120,
    },
    {
      title: '模块',
      dataIndex: 'module',
      key: 'module',
      width: 100,
      render: (module: string) => <Tag color={getModuleColor(module)}>{module}</Tag>,
    },
    {
      title: '操作',
      dataIndex: 'operation',
      key: 'operation',
      width: 100,
      render: (operation: string) => <Tag color={getOperationColor(operation)}>{operation}</Tag>,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: 'IP地址',
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 140,
      render: (ip: string) => ip || '-',
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Title level={2}>操作日志</Title>
      <Divider />
      
      <Card>
        <Space style={{ marginBottom: 16 }}>
          <Select
            style={{ width: 150 }}
            placeholder="选择模块"
            allowClear
            value={module || undefined}
            onChange={setModule}
          >
            <Option value="智能体">智能体</Option>
            <Option value="协作">协作</Option>
            <Option value="消息">消息</Option>
            <Option value="用户">用户</Option>
            <Option value="系统">系统</Option>
          </Select>
          
          <Select
            style={{ width: 150 }}
            placeholder="选择操作"
            allowClear
            value={operation || undefined}
            onChange={setOperation}
          >
            <Option value="创建">创建</Option>
            <Option value="修改">修改</Option>
            <Option value="删除">删除</Option>
            <Option value="登录">登录</Option>
            <Option value="登出">登出</Option>
            <Option value="查看">查看</Option>
          </Select>
          
          <Button 
            type="primary" 
            icon={<ReloadOutlined />}
            onClick={loadLogs}
            loading={loading}
          >
            刷新
          </Button>
        </Space>
        
        <Table
          columns={columns}
          dataSource={logs}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 20,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条记录`,
          }}
        />
      </Card>
    </div>
  );
};

export default OperationLogs;