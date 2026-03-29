import React from 'react';
import { Modal, Form, Input, Select, Switch, Divider, Row, Col } from 'antd';
import { LLMConfig, ProviderInfo } from './types';

const { Option } = Select;

interface ConfigFormModalProps {
  visible: boolean;
  editingConfig: LLMConfig | null;
  providers: ProviderInfo[];
  onCancel: () => void;
  onSubmit: (values: Partial<LLMConfig>) => Promise<boolean>;
}

const ConfigFormModal: React.FC<ConfigFormModalProps> = ({
  visible,
  editingConfig,
  providers,
  onCancel,
  onSubmit,
}) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    if (visible) {
      if (editingConfig) {
        form.setFieldsValue(editingConfig);
      } else {
        form.resetFields();
        form.setFieldsValue({
          isDefault: false,
          isEnabled: true,
        });
      }
    }
  }, [visible, editingConfig, form]);

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
      title={editingConfig ? '编辑供应商配置' : '新增供应商配置'}
      open={visible}
      onOk={handleOk}
      onCancel={onCancel}
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
                {providers.map((p) => (
                  <Option key={p.id} value={p.id}>
                    {p.displayName}
                  </Option>
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
  );
};

export default ConfigFormModal;
