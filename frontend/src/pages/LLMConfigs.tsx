import React, { useEffect, useState, useRef } from 'react';
import { Table, Button, Modal, Form, Input, Select, Tag, Space, message, Divider, InputNumber, Row, Col, Switch, Popconfirm, Tooltip, Collapse, Badge, Card, Descriptions, List, Typography } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, StarOutlined, StarFilled, ApiOutlined, CheckCircleOutlined, CloseCircleOutlined, LoadingOutlined, SettingOutlined, HistoryOutlined, ClockCircleOutlined } from '@ant-design/icons';
import api from '../services/api';

const { Option } = Select;
const { Panel } = Collapse;
const { Text } = Typography;

interface ConnectionStatus {
  success: boolean;
  message: string;
  latencyMs: number;
}

interface LLMModelConfig {
  id: string;
  modelName: string;
  displayName?: string;
  temperature: number;
  maxTokens: number;
  contextWindow: number;
  topP?: number;
  frequencyPenalty?: number;
  presencePenalty?: number;
  stopSequences?: string;
  isDefault: boolean;
  isEnabled: boolean;
  sortOrder: number;
  createdAt: string;
}

interface LLMTestRecord {
  id: string;
  llmConfigId: string;
  llmModelConfigId?: string;
  provider: string;
  modelName?: string;
  isSuccess: boolean;
  message?: string;
  latencyMs: number;
  testedAt: string;
}

interface LLMConfig {
  id: string;
  name: string;
  provider: string;
  apiKey: string;
  endpoint?: string;
  isDefault: boolean;
  isEnabled: boolean;
  createdAt: string;
  updatedAt?: string;
  models: LLMModelConfig[];
  testRecords?: LLMTestRecord[];
}

interface ProviderInfo {
  id: string;
  displayName: string;
  defaultEndpoint: string;
  defaultModel: string;
}

const LLMConfigs: React.FC = () => {
  const [configs, setConfigs] = useState<LLMConfig[]>([]);
  const [providers, setProviders] = useState<ProviderInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [modelModalVisible, setModelModalVisible] = useState(false);
  const [testRecordModalVisible, setTestRecordModalVisible] = useState(false);
  const [editingConfig, setEditingConfig] = useState<LLMConfig | null>(null);
  const [editingModel, setEditingModel] = useState<LLMModelConfig | null>(null);
  const [parentConfigId, setParentConfigId] = useState<string | null>(null);
  const [form] = Form.useForm();
  const [modelForm] = Form.useForm();
  const [connectionStatus, setConnectionStatus] = useState<Record<string, ConnectionStatus>>({});
  const [testingIds, setTestingIds] = useState<Set<string>>(new Set());
  const [testRecords, setTestRecords] = useState<LLMTestRecord[]>([]);
  const initializedRef = useRef(false);

  const loadConfigs = async () => {
    setLoading(true);
    try {
      const response = await api.get<LLMConfig[]>('/llmconfigs');
      setConfigs(response.data);
    } catch (error) {
      message.error('加载配置失败');
    } finally {
      setLoading(false);
    }
  };

  const loadProviders = async () => {
    try {
      const response = await api.get<ProviderInfo[]>('/llmconfigs/providers');
      setProviders(response.data);
    } catch (error) {
      console.error('加载供应商列表失败', error);
    }
  };

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadConfigs();
      loadProviders();
    }
  }, []);

  const handleTestConnection = async (configId: string, modelId?: string) => {
    const testId = modelId ? `${configId}-${modelId}` : configId;
    setTestingIds(prev => new Set(prev).add(testId));
    try {
      const url = modelId 
        ? `/llmconfigs/${configId}/models/${modelId}/test`
        : `/llmconfigs/${configId}/test`;
      const response = await api.post<ConnectionStatus>(url);
      setConnectionStatus(prev => ({
        ...prev,
        [testId]: response.data
      }));
      if (response.data.success) {
        message.success(`连接成功 (${response.data.latencyMs}ms)`);
      } else {
        message.error(`连接失败: ${response.data.message}`);
      }
      loadConfigs();
    } catch (error) {
      setConnectionStatus(prev => ({
        ...prev,
        [testId]: { success: false, message: '测试请求失败', latencyMs: 0 }
      }));
      message.error('测试请求失败');
    } finally {
      setTestingIds(prev => {
        const next = new Set(prev);
        next.delete(testId);
        return next;
      });
    }
  };

  const handleViewTestRecords = async (configId: string) => {
    try {
      const response = await api.get<LLMTestRecord[]>(`/llmconfigs/${configId}/test-records?limit=20`);
      setTestRecords(response.data);
      setTestRecordModalVisible(true);
    } catch (error) {
      message.error('加载测试记录失败');
    }
  };

  const handleCreate = () => {
    setEditingConfig(null);
    form.resetFields();
    form.setFieldsValue({
      isDefault: false,
      isEnabled: true,
    });
    setModalVisible(true);
  };

  const handleEdit = (config: LLMConfig) => {
    setEditingConfig(config);
    form.setFieldsValue(config);
    setModalVisible(true);
  };

  const handleDelete = async (id: string) => {
    try {
      await api.delete(`/llmconfigs/${id}`);
      message.success('删除成功');
      loadConfigs();
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleSetDefault = async (id: string) => {
    try {
      await api.post(`/llmconfigs/${id}/set-default`);
      message.success('设置成功');
      loadConfigs();
    } finally {
      loadConfigs();
    }
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editingConfig) {
        await api.put(`/llmconfigs/${editingConfig.id}`, values);
        message.success('更新成功');
      } else {
        await api.post('/llmconfigs', values);
        message.success('创建成功');
      }
      setModalVisible(false);
      loadConfigs();
    } catch (error) {
      message.error('操作失败');
    }
  };

  const handleCreateModel = (configId: string) => {
    setParentConfigId(configId);
    setEditingModel(null);
    modelForm.resetFields();
    modelForm.setFieldsValue({
      temperature: 0.7,
      maxTokens: 4096,
      contextWindow: 8192,
      isDefault: false,
      isEnabled: true,
    });
    setModelModalVisible(true);
  };

  const handleEditModel = (configId: string, model: LLMModelConfig) => {
    setParentConfigId(configId);
    setEditingModel(model);
    modelForm.setFieldsValue(model);
    setModelModalVisible(true);
  };

  const handleDeleteModel = async (configId: string, modelId: string) => {
    try {
      await api.delete(`/llmconfigs/${configId}/models/${modelId}`);
      message.success('删除成功');
      loadConfigs();
    } catch (error) {
      message.error('删除失败');
    }
  };

  const handleSetModelDefault = async (configId: string, modelId: string) => {
    try {
      await api.post(`/llmconfigs/${configId}/models/${modelId}/set-default`);
      message.success('设置成功');
      loadConfigs();
    } catch (error) {
      message.error('设置失败');
    }
  };

  const handleSubmitModel = async () => {
    try {
      const values = await modelForm.validateFields();
      if (editingModel) {
        await api.put(`/llmconfigs/${parentConfigId}/models/${editingModel.id}`, values);
        message.success('更新成功');
      } else {
        await api.post(`/llmconfigs/${parentConfigId}/models`, values);
        message.success('添加成功');
      }
      setModelModalVisible(false);
      loadConfigs();
    } catch (error) {
      message.error('操作失败');
    }
  };

  const getProviderDisplayName = (providerId: string) => {
    const provider = providers.find(p => p.id === providerId);
    return provider?.displayName || providerId;
  };

  const getProviderColor = (providerId: string) => {
    const colors: Record<string, string> = {
      'qwen': '#ff6a00',
      'openai': '#10a37f',
      'deepseek': '#0066ff',
      'zhipu': '#1e88e5',
      'anthropic': '#d97706',
      'openai_compatible': '#6b7280',
    };
    return colors[providerId] || '#1890ff';
  };

  const getModelTestStatus = (config: LLMConfig, model: LLMModelConfig) => {
    const testId = `${config.id}-${model.id}`;
    if (connectionStatus[testId]) {
      return {
        success: connectionStatus[testId].success,
        latencyMs: connectionStatus[testId].latencyMs,
        message: connectionStatus[testId].message,
        testedAt: new Date().toISOString()
      };
    }
    if (config.testRecords) {
      const record = config.testRecords.find(r => r.llmModelConfigId === model.id);
      if (record) {
        return {
          success: record.isSuccess,
          latencyMs: record.latencyMs,
          message: record.message,
          testedAt: record.testedAt
        };
      }
    }
    return null;
  };

  const renderModelList = (config: LLMConfig) => {
    if (!config.models || config.models.length === 0) {
      return (
        <div style={{ padding: '12px 0', color: '#999' }}>
          暂无模型配置，请添加至少一个模型
          <Button type="link" onClick={() => handleCreateModel(config.id)}>
            立即添加
          </Button>
        </div>
      );
    }

    return (
      <List
        dataSource={config.models}
        renderItem={(model) => {
          const testId = `${config.id}-${model.id}`;
          const isTesting = testingIds.has(testId);
          const testStatus = getModelTestStatus(config, model);

          return (
            <List.Item
              style={{ paddingRight: 0 }}
              actions={[
                <Space key="actions" size={0} style={{ minWidth: 320, justifyContent: 'flex-start', display: 'flex' }}>
                  <Button 
                    type="link" 
                    icon={<ApiOutlined />} 
                    onClick={() => handleTestConnection(config.id, model.id)}
                    loading={isTesting}
                  >
                    测试
                  </Button>
                  <Button type="link" onClick={() => handleEditModel(config.id, model)}>
                    编辑
                  </Button>
                  {!model.isDefault && (
                    <Button type="link" onClick={() => handleSetModelDefault(config.id, model.id)}>
                      设为默认
                    </Button>
                  )}
                  {config.models.length > 1 && (
                    <Popconfirm
                      title="确定要删除这个模型吗？"
                      onConfirm={() => handleDeleteModel(config.id, model.id)}
                      okText="确定"
                      cancelText="取消"
                    >
                      <Button type="link" danger>
                        删除
                      </Button>
                    </Popconfirm>
                  )}
                </Space>
              ]}
            >
              <List.Item.Meta
                avatar={
                  model.isDefault ? (
                    <StarFilled style={{ color: '#faad14', fontSize: 18 }} />
                  ) : null
                }
                title={
                  <Space>
                    <Text strong>{model.displayName || model.modelName}</Text>
                    <Text type="secondary" style={{ fontSize: 12 }}>{model.modelName}</Text>
                    {!model.isEnabled && <Tag color="red">禁用</Tag>}
                    {model.isDefault && <Tag color="gold">默认</Tag>}
                  </Space>
                }
                description={
                  <Space split={<Divider type="vertical" />}>
                    <span>温度: {model.temperature}</span>
                    <span>最大Token: {model.maxTokens}</span>
                    <span>窗口: {model.contextWindow.toLocaleString()}</span>
                    {isTesting ? (
                      <Tag icon={<LoadingOutlined spin />} color="processing">测试中</Tag>
                    ) : testStatus ? (
                      <Tooltip title={
                        <div>
                          <div>{testStatus.message || (testStatus.success ? '连接成功' : '连接失败')}</div>
                          {testStatus.testedAt && <div>测试时间: {new Date(testStatus.testedAt).toLocaleString()}</div>}
                        </div>
                      }>
                        <Tag 
                          icon={testStatus.success ? <CheckCircleOutlined /> : <CloseCircleOutlined />} 
                          color={testStatus.success ? 'success' : 'error'}
                        >
                          {testStatus.success ? `${testStatus.latencyMs}ms` : '失败'}
                        </Tag>
                      </Tooltip>
                    ) : (
                      <Tag color="default">未测试</Tag>
                    )}
                  </Space>
                }
              />
            </List.Item>
          );
        }}
      />
    );
  };

  const columns = [
    {
      title: '配置名称',
      dataIndex: 'name',
      key: 'name',
      width: 200,
      render: (name: string, record: LLMConfig) => (
        <Space>
          {record.isDefault && <StarFilled style={{ color: '#faad14' }} />}
          <Text strong>{name}</Text>
        </Space>
      ),
    },
    {
      title: '供应商',
      dataIndex: 'provider',
      key: 'provider',
      width: 180,
      render: (provider: string) => {
        const color = getProviderColor(provider);
        return (
          <Tag color={color} style={{ fontWeight: 'bold' }}>
            {getProviderDisplayName(provider)}
          </Tag>
        );
      },
    },
    {
      title: '模型数量',
      key: 'modelCount',
      width: 100,
      render: (_: any, record: LLMConfig) => (
        <Badge count={record.models?.length || 0} showZero color="blue" />
      ),
    },
    {
      title: '状态',
      dataIndex: 'isEnabled',
      key: 'isEnabled',
      width: 80,
      render: (enabled: boolean) => (
        <Tag color={enabled ? 'green' : 'red'}>{enabled ? '启用' : '禁用'}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'action',
      width: 320,
      render: (_: any, record: LLMConfig) => (
        <Space size={0} style={{ justifyContent: 'flex-start', display: 'flex' }}>
          <Button type="link" icon={<PlusOutlined />} onClick={() => handleCreateModel(record.id)}>
            添加模型
          </Button>
          <Button type="link" icon={<HistoryOutlined />} onClick={() => handleViewTestRecords(record.id)}>
            测试记录
          </Button>
          <Button type="link" icon={<EditOutlined />} onClick={() => handleEdit(record)}>
            编辑
          </Button>
          {!record.isDefault && (
            <Button type="link" icon={<StarOutlined />} onClick={() => handleSetDefault(record.id)}>
              设为默认
            </Button>
          )}
          <Popconfirm
            title="确定要删除这个配置吗？"
            onConfirm={() => handleDelete(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button type="link" danger icon={<DeleteOutlined />}>
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>大模型配置</h2>
        <Button type="primary" icon={<PlusOutlined />} onClick={handleCreate}>
          新增供应商配置
        </Button>
      </div>

      <Table
        dataSource={configs}
        columns={columns}
        rowKey="id"
        loading={loading}
        expandable={{
          expandedRowRender: (record) => (
            <div style={{ padding: '0 24px' }}>
              <Divider orientation="left" style={{ margin: '12px 0' }}>
                模型配置 ({record.models?.length || 0})
              </Divider>
              {renderModelList(record)}
              {record.models && record.models.length > 0 && (
                <Button 
                  type="dashed" 
                  icon={<PlusOutlined />} 
                  onClick={() => handleCreateModel(record.id)}
                  style={{ marginTop: 12 }}
                >
                  添加更多模型
                </Button>
              )}
            </div>
          ),
          rowExpandable: (record) => true,
        }}
      />

      <Modal
        title={editingConfig ? '编辑供应商配置' : '新增供应商配置'}
        open={modalVisible}
        onOk={handleSubmit}
        onCancel={() => setModalVisible(false)}
        width={600}
      >
        <Form form={form} layout="vertical">
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="配置名称"
                name="name"
                rules={[{ required: true, message: '请输入配置名称' }]}
              >
                <Input placeholder="例如: 阿里云千问" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="模型供应商"
                name="provider"
                rules={[{ required: true, message: '请选择供应商' }]}
              >
                <Select placeholder="请选择供应商">
                  {providers.map(p => (
                    <Option key={p.id} value={p.id}>{p.displayName}</Option>
                  ))}
                </Select>
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="API Endpoint" name="endpoint">
            <Input placeholder="可选，自定义API地址" />
          </Form.Item>

          <Form.Item
            label="API Key"
            name="apiKey"
            rules={[{ required: true, message: '请输入API Key' }]}
          >
            <Input.Password placeholder="请输入API Key" />
          </Form.Item>

          <Divider>状态设置</Divider>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item label="设为默认供应商" name="isDefault" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="启用" name="isEnabled" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>

      <Modal
        title={editingModel ? '编辑模型配置' : '添加模型配置'}
        open={modelModalVisible}
        onOk={handleSubmitModel}
        onCancel={() => setModelModalVisible(false)}
        width={700}
      >
        <Form form={modelForm} layout="vertical">
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="模型名称"
                name="modelName"
                rules={[{ required: true, message: '请输入模型名称' }]}
              >
                <Input placeholder="例如: qwen-turbo, gpt-4" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="显示名称" name="displayName">
                <Input placeholder="可选，便于识别的名称" />
              </Form.Item>
            </Col>
          </Row>

          <Divider>模型参数</Divider>
          <Row gutter={16}>
            <Col span={8}>
              <Form.Item label="Temperature" name="temperature" tooltip="控制输出的随机性，0-2之间">
                <InputNumber min={0} max={2} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item label="Max Tokens" name="maxTokens" tooltip="最大输出Token数">
                <InputNumber min={1} max={100000} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item label="上下文窗口" name="contextWindow" tooltip="模型上下文窗口大小">
                <InputNumber min={1} max={1000000} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Row gutter={16}>
            <Col span={8}>
              <Form.Item label="Top P" name="topP" tooltip="核采样参数">
                <InputNumber min={0} max={1} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item label="频率惩罚" name="frequencyPenalty" tooltip="降低重复词的频率">
                <InputNumber min={-2} max={2} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item label="存在惩罚" name="presencePenalty" tooltip="降低重复话题的出现">
                <InputNumber min={-2} max={2} step={0.1} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>

          <Form.Item label="停止词序列" name="stopSequences" tooltip="遇到这些词时停止生成，JSON数组格式">
            <Input placeholder='例如: ["###", "END"]' />
          </Form.Item>

          <Divider>状态设置</Divider>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item label="设为默认模型" name="isDefault" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="启用" name="isEnabled" valuePropName="checked">
                <Switch />
              </Form.Item>
            </Col>
          </Row>
        </Form>
      </Modal>

      <Modal
        title="测试记录"
        open={testRecordModalVisible}
        onCancel={() => setTestRecordModalVisible(false)}
        footer={null}
        width={800}
      >
        <Table
          dataSource={testRecords}
          rowKey="id"
          size="small"
          pagination={{ pageSize: 10 }}
          columns={[
            {
              title: '模型',
              dataIndex: 'modelName',
              key: 'modelName',
              render: (name: string) => name || '-',
            },
            {
              title: '状态',
              dataIndex: 'isSuccess',
              key: 'isSuccess',
              render: (success: boolean) => (
                <Tag icon={success ? <CheckCircleOutlined /> : <CloseCircleOutlined />} color={success ? 'success' : 'error'}>
                  {success ? '成功' : '失败'}
                </Tag>
              ),
            },
            {
              title: '延迟',
              dataIndex: 'latencyMs',
              key: 'latencyMs',
              render: (ms: number) => `${ms}ms`,
            },
            {
              title: '消息',
              dataIndex: 'message',
              key: 'message',
              ellipsis: true,
            },
            {
              title: '测试时间',
              dataIndex: 'testedAt',
              key: 'testedAt',
              render: (time: string) => new Date(time).toLocaleString(),
            },
          ]}
        />
      </Modal>
    </div>
  );
};

export default LLMConfigs;
