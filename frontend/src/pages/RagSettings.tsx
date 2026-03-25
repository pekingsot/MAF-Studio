import React, { useEffect, useState, useRef } from 'react';
import { Card, Form, Input, Button, message, Divider, Row, Col, Space, Tag, InputNumber, Select } from 'antd';
import { SaveOutlined, ReloadOutlined } from '@ant-design/icons';
import api from '../services/api';

const { Option } = Select;
const { TextArea } = Input;

interface SystemConfig {
  id: string;
  key: string;
  value: string;
  description?: string;
  group?: string;
}

const RagSettings: React.FC = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const initializedRef = useRef(false);

  const defaultSkipExtensions = '.xml,.json,.yml,.yaml,.dockerfile,.sh,.bat,.cmd,.ps1,.ini,.conf,.properties';

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadConfigs();
    }
  }, []);

  const loadConfigs = async () => {
    setLoading(true);
    try {
      const response = await api.get('/systemconfigs');
      const configs: SystemConfig[] = response.data;
      
      const configMap: Record<string, string> = {};
      configs.forEach((config) => {
        configMap[config.key.toLowerCase()] = config.value;
      });

      form.setFieldsValue({
        vectorizationEndpoint: configMap['vectorization_endpoint'] || '',
        rerankEndpoint: configMap['rerank_endpoint'] || '',
        vectorDbEndpoint: configMap['vector_db_endpoint'] || '',
        vectorDbCollection: configMap['vector_db_collection'] || 'rag_documents',
        defaultSplitMethod: configMap['default_split_method'] || 'recursive',
        defaultChunkSize: parseInt(configMap['default_chunk_size']) || 500,
        defaultChunkOverlap: parseInt(configMap['default_chunk_overlap']) || 50,
        skipExtensions: configMap['skip_extensions'] || defaultSkipExtensions,
      });
      message.success('配置加载成功');
    } catch (error) {
      message.error('配置加载失败');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    setSaving(true);
    try {
      const values = await form.validateFields();
      
      const configs = [
        { key: 'vectorization_endpoint', value: values.vectorizationEndpoint || '', description: '向量化接口地址' },
        { key: 'rerank_endpoint', value: values.rerankEndpoint || '', description: '重排序接口地址' },
        { key: 'vector_db_endpoint', value: values.vectorDbEndpoint || '', description: '向量库接口地址' },
        { key: 'vector_db_collection', value: values.vectorDbCollection || 'rag_documents', description: '向量库集合名称' },
        { key: 'default_split_method', value: values.defaultSplitMethod || 'recursive', description: '默认分割方式' },
        { key: 'default_chunk_size', value: String(values.defaultChunkSize || 500), description: '默认分块大小' },
        { key: 'default_chunk_overlap', value: String(values.defaultChunkOverlap || 50), description: '默认分块重叠' },
        { key: 'skip_extensions', value: values.skipExtensions || defaultSkipExtensions, description: '跳过分割的文件扩展名' },
      ];

      await api.post('/systemconfigs/batch', { Configs: configs });
      message.success('配置保存成功');
    } catch (error) {
      message.error('配置保存失败');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Card 
        title="RAG配置" 
        extra={
          <Space>
            <Button icon={<ReloadOutlined />} onClick={loadConfigs} loading={loading}>
              刷新
            </Button>
            <Button type="primary" icon={<SaveOutlined />} onClick={handleSave} loading={saving}>
              保存配置
            </Button>
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Divider orientation="left">向量数据库配置</Divider>
          <Row gutter={24}>
            <Col span={12}>
              <Form.Item
                name="vectorDbEndpoint"
                label="向量库接口地址"
                tooltip="Qdrant向量数据库的API地址"
              >
                <Input placeholder="http://localhost:6333" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="vectorDbCollection"
                label="向量库集合名称"
                tooltip="存储文档向量的集合名称"
              >
                <Input placeholder="rag_documents" />
              </Form.Item>
            </Col>
          </Row>

          <Divider orientation="left">向量化服务配置</Divider>
          <Row gutter={24}>
            <Col span={12}>
              <Form.Item
                name="vectorizationEndpoint"
                label="向量化接口地址"
                tooltip="Infinity或其他向量化服务的API地址"
              >
                <Input placeholder="http://localhost:8000/embed" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item
                name="rerankEndpoint"
                label="重排序接口地址"
                tooltip="Infinity或其他重排序服务的API地址"
              >
                <Input placeholder="http://localhost:8000/rerank" />
              </Form.Item>
            </Col>
          </Row>

          <Divider orientation="left">文本分割配置</Divider>
          <Row gutter={24}>
            <Col span={8}>
              <Form.Item
                name="defaultSplitMethod"
                label="默认分割方式"
                tooltip="文本分割的默认方式"
              >
                <Select placeholder="选择分割方式">
                  <Option value="recursive">
                    <Space>
                      <Tag color="blue">递归分割</Tag>
                      <span style={{ color: '#999', fontSize: '12px' }}>推荐，按分隔符层级分割</span>
                    </Space>
                  </Option>
                  <Option value="character">
                    <Space>
                      <Tag color="green">字符分割</Tag>
                      <span style={{ color: '#999', fontSize: '12px' }}>按固定字符数分割</span>
                    </Space>
                  </Option>
                  <Option value="separator">
                    <Space>
                      <Tag color="orange">分隔符分割</Tag>
                      <span style={{ color: '#999', fontSize: '12px' }}>按指定分隔符分割</span>
                    </Space>
                  </Option>
                </Select>
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="defaultChunkSize"
                label="默认分块大小"
                tooltip="每个文本块的最大字符数"
              >
                <InputNumber min={100} max={10000} style={{ width: '100%' }} placeholder="500" />
              </Form.Item>
            </Col>
            <Col span={8}>
              <Form.Item
                name="defaultChunkOverlap"
                label="默认分块重叠"
                tooltip="相邻文本块的重叠字符数"
              >
                <InputNumber min={0} max={1000} style={{ width: '100%' }} placeholder="50" />
              </Form.Item>
            </Col>
          </Row>

          <Divider orientation="left">文件处理配置</Divider>
          <Row gutter={24}>
            <Col span={24}>
              <Form.Item
                name="skipExtensions"
                label="跳过分割的文件扩展名"
                tooltip="这些扩展名的文件将不做文本分割，直接存储原文"
                help="多个扩展名用逗号分隔，例如：.xml,.json,.yml,.yaml,.dockerfile,.sh"
              >
                <TextArea
                  rows={3}
                  placeholder={defaultSkipExtensions}
                />
              </Form.Item>
            </Col>
          </Row>

          <Divider orientation="left">支持的文件类型</Divider>
          <Row gutter={24}>
            <Col span={24}>
              <Space wrap>
                <Tag color="blue">PDF文档</Tag>
                <Tag color="green">Word文档</Tag>
                <Tag color="orange">Excel表格</Tag>
                <Tag color="purple">Markdown</Tag>
                <Tag color="cyan">TXT文本</Tag>
                <Tag color="geekblue">CSV数据</Tag>
                <Tag color="magenta">HTML网页</Tag>
                <Tag color="volcano">JSON数据</Tag>
                <Tag color="gold">XML配置</Tag>
                <Tag color="lime">YAML配置</Tag>
                <Tag>代码文件</Tag>
              </Space>
            </Col>
          </Row>
        </Form>
      </Card>
    </div>
  );
};

export default RagSettings;
