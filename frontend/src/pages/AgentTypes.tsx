import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Select, Tag, Space, message, Switch, InputNumber, Divider, Row, Col, Tooltip, Typography } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, LockOutlined } from '@ant-design/icons';
import api from '../services/api';

const { Option } = Select;
const { TextArea } = Input;
const { Text } = Typography;

interface AgentType {
  id: string;
  code: string;
  name: string;
  description?: string;
  defaultSystemPrompt?: string;
  defaultTemperature: number;
  defaultMaxTokens: number;
  icon?: string;
  isSystem: boolean;
  isEnabled: boolean;
  sortOrder: number;
  createdAt: string;
}

const ICON_OPTIONS = [
  '🤖', '🧠', '💻', '🎯', '📊', '🔬', '🚀', '⚡', '🌟', '🎨',
  '🦾', '🤝', '🔮', '💡', '🎭', '🦸', '🌈', '🎪', '🎠', '🎡',
  '🔧', '📝', '🔍', '📈', '🗂️', '💬', '🎓', '🏆', '🔑', '📱',
  '✍️', '🔒', '🏗️', '📋', '👨‍💼'
];

const AgentTypes: React.FC = () => {
  const [types, setTypes] = useState<AgentType[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingType, setEditingType] = useState<AgentType | null>(null);
  const [pageSize, setPageSize] = useState(10);
  const [form] = Form.useForm();
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadTypes();
    }
  }, []);

  const loadTypes = async () => {
    try {
      setLoading(true);
      const response = await api.get('/agenttypes');
      setTypes(response.data || []);
    } catch (error) {
      message.error('加载智能体类型失败');
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = () => {
    setEditingType(null);
    form.resetFields();
    form.setFieldsValue({
      icon: '🤖',
      defaultTemperature: 0.7,
      defaultMaxTokens: 4096,
      isEnabled: true,
      sortOrder: 0,
    });
    setModalVisible(true);
  };

  const handleEdit = (type: AgentType) => {
    setEditingType(type);
    form.setFieldsValue(type);
    setModalVisible(true);
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/agenttypes/${id}`);
      message.success('删除成功');
      loadTypes();
    } catch (error: any) {
      if (error.response?.status === 400) {
        message.error(error.response.data || '系统内置类型不能删除');
      } else {
        message.error('删除失败');
      }
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      
      if (editingType) {
        await api.put(`/agenttypes/${editingType.id}`, values);
        message.success('更新成功');
      } else {
        await api.post('/agenttypes', values);
        message.success('创建成功');
      }
      setModalVisible(false);
      loadTypes();
    } catch (error) {
      message.error('操作失败');
    }
  };

  const handleToggleEnabled = async (id: string, enabled: boolean) => {
    try {
      await api.patch(`/agenttypes/${id}/enable`, { isEnabled: enabled });
      message.success(enabled ? '已启用' : '已禁用');
      loadTypes();
    } catch (error) {
      message.error('操作失败');
    }
  };

  const columns = [
    {
      title: '图标',
      dataIndex: 'icon',
      key: 'icon',
      width: 60,
      align: 'center' as const,
      render: (icon: string) => <span style={{ fontSize: 24 }}>{icon || '🤖'}</span>,
    },
    {
      title: '类型名称',
      dataIndex: 'name',
      key: 'name',
      width: 140,
      render: (name: string, record: AgentType) => (
        <Space>
          <Text strong>{name}</Text>
          {record.isSystem && (
            <Tooltip title="系统内置类型，不可删除">
              <LockOutlined style={{ color: '#faad14', fontSize: 12 }} />
            </Tooltip>
          )}
        </Space>
      ),
    },
    {
      title: '编码',
      dataIndex: 'code',
      key: 'code',
      width: 130,
      render: (code: string) => <Text type="secondary" style={{ fontSize: 12 }}>{code}</Text>,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      width: 200,
      render: (description: string) => (
        <div style={{ 
          whiteSpace: 'pre-wrap', 
          wordBreak: 'break-word',
          maxHeight: 60,
          overflow: 'hidden',
          fontSize: 12,
          color: '#666'
        }}>
          {description || '-'}
        </div>
      ),
    },
    {
      title: '默认提示词',
      dataIndex: 'defaultSystemPrompt',
      key: 'defaultSystemPrompt',
      width: 300,
      render: (prompt: string) => {
        if (!prompt) return <Text type="secondary" style={{ fontSize: 12 }}>-</Text>;
        return (
          <Tooltip title={<div style={{ whiteSpace: 'pre-wrap', maxWidth: 400 }}>{prompt}</div>} placement="topLeft">
            <div style={{ 
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word',
              overflow: 'hidden',
              fontSize: 12,
              color: '#666',
              maxHeight: 60,
              lineHeight: '20px',
              display: '-webkit-box',
              WebkitLineClamp: 3,
              WebkitBoxOrient: 'vertical' as const,
            }}>
              {prompt}
            </div>
          </Tooltip>
        );
      },
    },
    {
      title: '参数',
      key: 'defaults',
      width: 100,
      render: (_: any, record: AgentType) => (
        <div style={{ fontSize: 12 }}>
          <div>温度: {record.defaultTemperature}</div>
          <div>Tokens: {record.defaultMaxTokens}</div>
        </div>
      ),
    },
    {
      title: '状态',
      dataIndex: 'isEnabled',
      key: 'isEnabled',
      width: 80,
      render: (enabled: boolean, record: AgentType) => (
        <Switch
          checked={enabled}
          onChange={(checked) => handleToggleEnabled(record.id, checked)}
          checkedChildren="启用"
          unCheckedChildren="禁用"
          size="small"
        />
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 120,
      fixed: 'right' as const,
      render: (_: any, record: AgentType) => (
        <Space size={0}>
          <Button type="link" size="small" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
            编辑
          </Button>
          {!record.isSystem && (
            <Button type="link" size="small" danger onClick={() => handleDelete(record.id)}>
              删除
            </Button>
          )}
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2 style={{ margin: 0 }}>智能体类型管理</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          新建类型
        </Button>
      </div>

      <Table
        columns={columns}
        dataSource={types}
        rowKey="id"
        loading={loading}
        pagination={{ 
          pageSize: pageSize,
          showSizeChanger: true,
          pageSizeOptions: ['10', '20', '50', '100'],
          showTotal: (total) => `共 ${total} 条`,
          onShowSizeChange: (_current, size) => setPageSize(size)
        }}
        scroll={{ x: 1100 }}
        size="small"
      />

      <Modal
        title={editingType ? '编辑智能体类型' : '新建智能体类型'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={700}
        okText="保存"
        cancelText="取消"
      >
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col span={6}>
              <Form.Item name="icon" label="图标">
                <Select placeholder="选择图标">
                  {ICON_OPTIONS.map(icon => (
                    <Option key={icon} value={icon}>
                      <span style={{ fontSize: 20 }}>{icon}</span>
                    </Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
            <Col span={9}>
              <Form.Item
                name="code"
                label="类型编码"
                rules={[{ required: true, message: '请输入类型编码' }]}
                tooltip="唯一标识，如: product_manager, frontend_dev"
              >
                <Input placeholder="例如: product_manager" disabled={editingType?.isSystem} />
              </Form.Item>
            </Col>
            <Col span={9}>
              <Form.Item
                name="name"
                label="类型名称"
                rules={[{ required: true, message: '请输入类型名称' }]}
              >
                <Input placeholder="例如: 产品经理" />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item name="description" label="描述">
            <Input.TextArea rows={2} placeholder="请输入类型描述" />
          </Form.Item>

          <Divider orientation="left">默认配置</Divider>

          <Form.Item 
            name="defaultSystemPrompt" 
            label="默认系统提示词"
            tooltip="创建智能体时自动填充此提示词，用户可以修改"
          >
            <TextArea 
              rows={6} 
              placeholder="请输入默认系统提示词，定义智能体的角色和行为..."
            />
          </Form.Item>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item 
                name="defaultTemperature" 
                label="默认温度"
                tooltip="控制回复的随机性，0-1之间"
              >
                <InputNumber min={0} max={1} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item 
                name="defaultMaxTokens" 
                label="默认最大Token数"
                tooltip="控制回复的最大长度"
              >
                <InputNumber min={100} max={128000} step={100} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item 
                name="sortOrder" 
                label="排序"
                tooltip="数字越小越靠前"
              >
                <InputNumber min={0} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item 
            name="isEnabled" 
            label="是否启用"
            valuePropName="checked"
          >
            <Switch checkedChildren="启用" unCheckedChildren="禁用" />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default AgentTypes;
