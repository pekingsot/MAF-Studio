import React from 'react';
import { Modal, Form, Input, InputNumber, Divider, Row, Col, Switch } from 'antd';
import { LLMModelConfig } from './types';

interface ModelFormModalProps {
  visible: boolean;
  editingModel: LLMModelConfig | null;
  onCancel: () => void;
  onSubmit: (values: Partial<LLMModelConfig>) => Promise<boolean>;
}

const ModelFormModal: React.FC<ModelFormModalProps> = ({
  visible,
  editingModel,
  onCancel,
  onSubmit,
}) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    if (visible) {
      if (editingModel) {
        form.setFieldsValue(editingModel);
      } else {
        form.resetFields();
        form.setFieldsValue({
          temperature: 0.7,
          maxTokens: 4096,
          contextWindow: 8192,
          isDefault: false,
          isEnabled: true,
        });
      }
    }
  }, [visible, editingModel, form]);

  const handleOk = async () => {
    try {
      const values = await form.validateFields();
      const success = await onSubmit(values);
      if (success) {
        onCancel();
      }
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  return (
    <Modal
      title={editingModel ? '编辑模型配置' : '添加模型配置'}
      open={visible}
      onOk={handleOk}
      onCancel={onCancel}
      width={700}
    >
      <Form form={form} layout="vertical">
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
            <Form.Item label="频率惩罚" name="frequencyPenalty" tooltip="频率惩罚系数">
              <InputNumber min={-2} max={2} step={0.1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col span={8}>
            <Form.Item label="存在惩罚" name="presencePenalty" tooltip="存在惩罚系数">
              <InputNumber min={-2} max={2} step={0.1} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>

        <Form.Item label="停止序列" name="stopSequences" tooltip="遇到这些序列时停止生成，用逗号分隔">
          <Input placeholder="例如: ###, END" />
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
  );
};

export default ModelFormModal;
