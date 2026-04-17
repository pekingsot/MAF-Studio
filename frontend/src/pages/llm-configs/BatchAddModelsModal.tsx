import { getErrorMessage } from '../../utils/errorHandler';
import React, { useState } from 'react';
import { Modal, Form, Input, InputNumber, Switch, message } from 'antd';
import { LLMModelConfig } from './types';

interface BatchAddModelsModalProps {
  visible: boolean;
  configId: number;
  onCancel: () => void;
  onSuccess: () => void;
}

const BatchAddModelsModal: React.FC<BatchAddModelsModalProps> = ({
  visible,
  configId,
  onCancel,
  onSuccess,
}) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      setLoading(true);

      const response = await fetch(`/api/llmconfigs/${configId}/models/batch`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        throw new Error('批量添加失败');
      }

      const data = await response.json();
      message.success(data.message + '，正在后台测试连接...');
      form.resetFields();
      onSuccess();
      onCancel();
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '批量添加失败'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal
      title="批量添加模型"
      open={visible}
      onOk={handleSubmit}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      confirmLoading={loading}
      width={600}
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          temperature: 0.7,
          maxTokens: 4096,
          contextWindow: 64000,
          isEnabled: true,
        }}
      >
        <Form.Item
          label="模型名称（每行一个）"
          name="modelNames"
          rules={[{ required: true, message: '请输入模型名称' }]}
          extra="每行输入一个模型名称，模型名称和显示名称将保持一致"
        >
          <Input.TextArea
            rows={10}
            placeholder="qwen-max&#10;qwen-plus&#10;qwen-turbo&#10;..."
          />
        </Form.Item>

        <Form.Item label="温度" name="temperature">
          <InputNumber min={0} max={2} step={0.1} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item label="最大Token数" name="maxTokens">
          <InputNumber min={1} max={1000000} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item label="上下文窗口" name="contextWindow">
          <InputNumber min={1} max={1000000} style={{ width: '100%' }} />
        </Form.Item>

        <Form.Item label="启用状态" name="isEnabled" valuePropName="checked">
          <Switch />
        </Form.Item>
      </Form>
    </Modal>
  );
};

export default BatchAddModelsModal;
