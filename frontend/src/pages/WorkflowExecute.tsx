import { getErrorMessage } from '../utils/errorHandler';
import React, { useState, useEffect } from 'react';
import {
  Card,
  Row,
  Col,
  Button,
  Modal,
  Form,
  Input,
  Select,
  message,
  Space,
  Tag,
  Empty,
  Spin,
} from 'antd';
import {
  PlayCircleOutlined,
  RobotOutlined,
  FileTextOutlined,
  ThunderboltOutlined,
  SwapOutlined,
  MessageOutlined,
} from '@ant-design/icons';
import { useNavigate, useLocation } from 'react-router-dom';
import { workflowTemplateApi } from '../services/workflow-template-api';
import type { WorkflowTemplate, WorkflowDefinition } from '../types/workflow-template';

const { TextArea } = Input;
const { Option } = Select;

/**
 * 工作流执行选择页面
 */
const WorkflowExecute: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const [templates, setTemplates] = useState<WorkflowTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [executeModalVisible, setExecuteModalVisible] = useState(false);
  const [selectedTemplate, setSelectedTemplate] = useState<WorkflowTemplate | null>(null);
  const [form] = Form.useForm();

  const locationState = location.state as { template?: WorkflowTemplate; workflow?: WorkflowDefinition } | undefined;

  useEffect(() => {
    loadTemplates();
  }, []);

  useEffect(() => {
    if (locationState?.template) {
      setSelectedTemplate(locationState.template);
      setExecuteModalVisible(true);
    }
  }, [locationState]);

  /**
   * 加载模板列表
   */
  const loadTemplates = async () => {
    setLoading(true);
    try {
      const data = await workflowTemplateApi.getAll();
      setTemplates(data);
    } catch (error: unknown) {
      message.error(`加载模板失败: ${getErrorMessage(error)}`);
    } finally {
      setLoading(false);
    }
  };

  /**
   * 选择标准模式
   */
  const handleSelectStandardMode = (mode: string) => {
    navigate('/collaboration-workflow', {
      state: { mode },
    });
  };

  /**
   * 选择Magentic模式
   */
  const handleSelectMagenticMode = () => {
    navigate('/magentic-workflow');
  };

  /**
   * 选择自定义模板
   */
  const handleSelectTemplate = (template: WorkflowTemplate) => {
    setSelectedTemplate(template);
    setExecuteModalVisible(true);
  };

  /**
   * 执行工作流
   */
  const handleExecute = async (values: Record<string, unknown>) => {
    if (!selectedTemplate) return;

    try {
      const result = await workflowTemplateApi.execute(selectedTemplate.id, {
        collaborationId: Number(values.collaborationId),
        input: values.input as string,
        parameterValues: values.parameterValues as Record<string, unknown> | undefined,
      });

      if (result.success) {
        message.success('执行成功');
        setExecuteModalVisible(false);
      } else {
        message.error(`执行失败: ${result.error}`);
      }
    } catch (error: unknown) {
      message.error(`执行失败: ${getErrorMessage(error)}`);
    }
  };

  /**
   * 获取来源标签颜色
   */
  const getSourceColor = (source: string) => {
    switch (source) {
      case 'manual':
        return 'blue';
      case 'magentic':
        return 'green';
      case 'magentic_optimized':
        return 'orange';
      default:
        return 'default';
    }
  };

  /**
   * 获取来源标签文本
   */
  const getSourceText = (source: string) => {
    switch (source) {
      case 'manual':
        return '手动创建';
      case 'magentic':
        return 'Magentic生成';
      case 'magentic_optimized':
        return 'Magentic优化';
      default:
        return source;
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Card title="选择工作流模式">
        <Row gutter={[16, 16]}>
          <Col span={6}>
            <Card
              hoverable
              onClick={() => handleSelectStandardMode('sequential')}
              style={{ textAlign: 'center', height: '100%' }}
            >
              <ThunderboltOutlined style={{ fontSize: '48px', color: '#1890ff' }} />
              <h3 style={{ marginTop: '16px' }}>顺序执行</h3>
              <p style={{ color: '#999' }}>按顺序依次执行Agent</p>
            </Card>
          </Col>

          <Col span={6}>
            <Card
              hoverable
              onClick={() => handleSelectStandardMode('concurrent')}
              style={{ textAlign: 'center', height: '100%' }}
            >
              <ThunderboltOutlined style={{ fontSize: '48px', color: '#52c41a' }} />
              <h3 style={{ marginTop: '16px' }}>并发执行</h3>
              <p style={{ color: '#999' }}>多个Agent同时执行</p>
            </Card>
          </Col>

          <Col span={6}>
            <Card
              hoverable
              onClick={() => handleSelectStandardMode('handoffs')}
              style={{ textAlign: 'center', height: '100%' }}
            >
              <SwapOutlined style={{ fontSize: '48px', color: '#722ed1' }} />
              <h3 style={{ marginTop: '16px' }}>移交执行</h3>
              <p style={{ color: '#999' }}>Agent之间移交任务</p>
            </Card>
          </Col>

          <Col span={6}>
            <Card
              hoverable
              onClick={() => handleSelectStandardMode('groupchat')}
              style={{ textAlign: 'center', height: '100%' }}
            >
              <MessageOutlined style={{ fontSize: '48px', color: '#fa8c16' }} />
              <h3 style={{ marginTop: '16px' }}>群聊执行</h3>
              <p style={{ color: '#999' }}>Agent群聊协作</p>
            </Card>
          </Col>
        </Row>

        <Row gutter={[16, 16]} style={{ marginTop: '16px' }}>
          <Col span={12}>
            <Card
              hoverable
              onClick={handleSelectMagenticMode}
              style={{ textAlign: 'center', height: '100%', background: '#f6ffed' }}
            >
              <RobotOutlined style={{ fontSize: '48px', color: '#52c41a' }} />
              <h3 style={{ marginTop: '16px' }}>🤖 Magentic模式</h3>
              <p style={{ color: '#999' }}>
                Manager根据任务动态生成工作流计划
              </p>
              <Tag color="green">智能编排</Tag>
            </Card>
          </Col>

          <Col span={12}>
            <Card
              hoverable
              onClick={() => navigate('/workflow-editor')}
              style={{ textAlign: 'center', height: '100%', background: '#e6f7ff' }}
            >
              <FileTextOutlined style={{ fontSize: '48px', color: '#1890ff' }} />
              <h3 style={{ marginTop: '16px' }}>自定义工作流</h3>
              <p style={{ color: '#999' }}>拖拽创建自定义工作流</p>
              <Tag color="blue">灵活定制</Tag>
            </Card>
          </Col>
        </Row>
      </Card>

      <Card title="自定义模板" style={{ marginTop: '24px' }}>
        <Spin spinning={loading}>
          {templates.length === 0 ? (
            <Empty description="暂无模板" />
          ) : (
            <Row gutter={[16, 16]}>
              {templates.map((template) => (
                <Col span={8} key={template.id}>
                  <Card
                    hoverable
                    onClick={() => handleSelectTemplate(template)}
                    style={{ height: '100%' }}
                  >
                    <div style={{ marginBottom: '8px' }}>
                      <h3 style={{ margin: 0 }}>{template.name}</h3>
                    </div>
                    <div style={{ color: '#999', fontSize: '12px', marginBottom: '8px' }}>
                      {template.description}
                    </div>
                    <Space>
                      <Tag color={getSourceColor(template.source)}>
                        {getSourceText(template.source)}
                      </Tag>
                      <Tag>使用 {template.usageCount} 次</Tag>
                      {template.isPublic && <Tag color="green">公开</Tag>}
                    </Space>
                  </Card>
                </Col>
              ))}
            </Row>
          )}
        </Spin>
      </Card>

      <Modal
        title={`执行工作流: ${selectedTemplate?.name || ''}`}
        open={executeModalVisible}
        onCancel={() => setExecuteModalVisible(false)}
        onOk={() => form.submit()}
        width={600}
      >
        <Form form={form} layout="vertical" onFinish={handleExecute}>
          <Form.Item
            label="协作ID"
            name="collaborationId"
            rules={[{ required: true, message: '请输入协作ID' }]}
          >
            <Input type="number" placeholder="请输入协作ID" />
          </Form.Item>

          <Form.Item
            label="输入内容"
            name="input"
            rules={[{ required: true, message: '请输入内容' }]}
          >
            <TextArea rows={4} placeholder="请输入任务内容" />
          </Form.Item>

          {selectedTemplate?.parameters && Object.keys(selectedTemplate.parameters).length > 0 && (
            <Form.Item label="参数配置">
              {Object.entries(selectedTemplate.parameters).map(([key, param]) => (
                <Form.Item
                  key={key}
                  label={key}
                  name={['parameterValues', key]}
                  rules={[{ required: param.required, message: `请输入${key}` }]}
                >
                  <Input placeholder={param.description} />
                </Form.Item>
              ))}
            </Form.Item>
          )}
        </Form>
      </Modal>
    </div>
  );
};

export default WorkflowExecute;
