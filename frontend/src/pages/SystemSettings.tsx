import React, { useEffect, useState, useRef } from 'react';
import { Card, Form, Input, Button, message, Divider, Row, Col, Space, Tag } from 'antd';
import { SaveOutlined, ReloadOutlined } from '@ant-design/icons';
import api from '../services/api';

interface SystemConfig {
  id: string;
  key: string;
  value: string;
  description?: string;
  group?: string;
  createdAt: string;
  updatedAt?: string;
}

const SystemSettings: React.FC = () => {
  const [configs, setConfigs] = useState<SystemConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [form] = Form.useForm();
  const initializedRef = useRef(false);

  const loadConfigs = async () => {
    setLoading(true);
    try {
      const response = await api.get<SystemConfig[]>('/systemconfigs');
      setConfigs(response.data);
      
      // 设置表单值
      const formValues: Record<string, string> = {};
      response.data.forEach(config => {
        formValues[config.key] = config.value;
      });
      form.setFieldsValue(formValues);
    } catch (error) {
      message.error('加载配置失败');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadConfigs();
    }
  }, []);

  const handleSave = async (key: string, value: string) => {
    try {
      await api.put(`/systemconfigs/${key}`, { value });
      message.success('保存成功');
      loadConfigs();
    } catch (error) {
      message.error('保存失败');
    }
  };

  const handleSaveAll = async () => {
    try {
      const values = await form.validateFields();
      await api.post('/systemconfigs/batch', values);
      message.success('保存成功');
      loadConfigs();
    } catch (error) {
      message.error('保存失败');
    }
  };

  const handleInitialize = async () => {
    try {
      await api.post('/systemconfigs/initialize');
      message.success('初始化成功');
      loadConfigs();
    } catch (error) {
      message.error('初始化失败');
    }
  };

  const ragConfigs = configs.filter(c => c.group === 'rag');
  const otherConfigs = configs.filter(c => !c.group);

  const renderConfigItem = (config: SystemConfig) => (
    <Form.Item
      key={config.key}
      label={config.description || config.key}
      name={config.key}
      tooltip={config.description}
    >
      <Input 
        placeholder={`请输入${config.description || config.key}`}
        onBlur={(e) => handleSave(config.key, e.target.value)}
      />
    </Form.Item>
  );

  return (
    <div>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2>系统配置</h2>
        <Space>
          <Button icon={<ReloadOutlined />} onClick={handleInitialize}>
            初始化默认配置
          </Button>
          <Button type="primary" icon={<SaveOutlined />} onClick={handleSaveAll}>
            保存全部
          </Button>
        </Space>
      </div>

      <Form form={form} layout="vertical">
        <Card title="RAG配置" style={{ marginBottom: 16 }}>
          <Tag color="blue" style={{ marginBottom: 16 }}>向量化与检索配置</Tag>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="向量化接口地址 (Infinity)"
                name="embedding_endpoint"
                tooltip="Infinity向量化服务地址，用于文本向量化"
              >
                <Input placeholder="http://localhost:7997" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="查询重排序接口地址 (Infinity)"
                name="rerank_endpoint"
                tooltip="Infinity重排序服务地址，用于查询结果重排序"
              >
                <Input placeholder="http://localhost:7997" />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="向量库接口地址 (Qdrant)"
                name="vector_db_endpoint"
                tooltip="Qdrant向量数据库地址"
              >
                <Input placeholder="http://localhost:6333" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                label="向量库集合名称"
                name="vector_db_collection"
                tooltip="Qdrant中的集合名称"
              >
                <Input placeholder="maf_documents" />
              </Form.Item>
            </Col>
          </Row>
        </Card>

        <Card title="文本分割配置" style={{ marginBottom: 16 }}>
          <Tag color="green" style={{ marginBottom: 16 }}>文档处理配置</Tag>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="默认分割方式"
                name="default_split_method"
                tooltip="character: 按字符分割, recursive: 递归分割, separator: 按分隔符分割"
              >
                <Input placeholder="recursive" />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item
                label="默认分块大小"
                name="default_chunk_size"
                tooltip="每个文本块的最大字符数"
              >
                <Input placeholder="500" />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item
                label="默认重叠大小"
                name="default_chunk_overlap"
                tooltip="相邻文本块之间的重叠字符数"
              >
                <Input placeholder="50" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            label="跳过分割的文件扩展名"
            name="skip_split_extensions"
            tooltip="这些文件类型将不进行文本分割，直接作为整体存储"
          >
            <Input.TextArea 
              rows={3}
              placeholder=".xml,.json,.yml,.yaml,.toml,.ini,.conf,.env,.dockerfile,.sh,.bat,.ps1"
            />
          </Form.Item>
          <div style={{ marginTop: 8, color: '#666', fontSize: 12 }}>
            <strong>说明：</strong>用逗号分隔多个扩展名，例如: .xml,.json,.yml
          </div>
        </Card>

        {otherConfigs.length > 0 && (
          <Card title="其他配置">
            <Row gutter={16}>
              {otherConfigs.map(config => (
                <Col span={12} key={config.key}>
                  {renderConfigItem(config)}
                </Col>
              ))}
            </Row>
          </Card>
        )}
      </Form>
    </div>
  );
};

export default SystemSettings;
