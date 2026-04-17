import { getErrorMessage } from '../utils/errorHandler';
import React, { useState } from 'react';
import { Card, Form, Input, Button, Switch, Divider, Typography, message, Space, Row, Col, Select } from 'antd';
import { SaveOutlined, SettingOutlined, DatabaseOutlined, SafetyOutlined } from '@ant-design/icons';

const { Title, Text } = Typography;
const { Option } = Select;

const Settings: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [systemForm] = Form.useForm();
  const [modelForm] = Form.useForm();

  const handleSaveSystemSettings = async (values: Record<string, unknown>) => {
    try {
      setLoading(true);
      console.log('System settings:', values);
      message.success('系统设置保存成功');
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '保存失败'));
    } finally {
      setLoading(false);
    }
  };

  const handleSaveModelSettings = async (values: Record<string, unknown>) => {
    try {
      setLoading(true);
      console.log('Model settings:', values);
      message.success('模型设置保存成功');
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '保存失败'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: '24px' }}>
      <Title level={2}>系统设置</Title>
      <Divider />
      
      <Row gutter={24}>
        <Col span={12}>
          <Card 
            title={
              <Space>
                <SettingOutlined />
                <span>基础设置</span>
              </Space>
            }
          >
            <Form
              form={systemForm}
              layout="vertical"
              initialValues={{
                siteName: 'MAF Studio',
                language: 'zh-CN',
                enableRegister: true,
                enableEmailNotify: false,
              }}
              onFinish={handleSaveSystemSettings}
            >
              <Form.Item
                label="站点名称"
                name="siteName"
                rules={[{ required: true, message: '请输入站点名称' }]}
              >
                <Input />
              </Form.Item>
              
              <Form.Item
                label="系统语言"
                name="language"
              >
                <Select>
                  <Option value="zh-CN">简体中文</Option>
                  <Option value="en-US">English</Option>
                </Select>
              </Form.Item>
              
              <Form.Item
                label="允许用户注册"
                name="enableRegister"
                valuePropName="checked"
              >
                <Switch checkedChildren="开启" unCheckedChildren="关闭" />
              </Form.Item>
              
              <Form.Item
                label="启用邮件通知"
                name="enableEmailNotify"
                valuePropName="checked"
              >
                <Switch checkedChildren="开启" unCheckedChildren="关闭" />
              </Form.Item>
              
              <Form.Item>
                <Button 
                  type="primary" 
                  htmlType="submit"
                  icon={<SaveOutlined />}
                  loading={loading}
                >
                  保存设置
                </Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>
        
        <Col span={12}>
          <Card 
            title={
              <Space>
                <DatabaseOutlined />
                <span>模型配置</span>
              </Space>
            }
          >
            <Form
              form={modelForm}
              layout="vertical"
              initialValues={{
                defaultProvider: 'openai',
                defaultModel: 'gpt-4',
                maxTokens: 4096,
                temperature: 0.7,
              }}
              onFinish={handleSaveModelSettings}
            >
              <Form.Item
                label="默认提供商"
                name="defaultProvider"
              >
                <Select>
                  <Option value="openai">OpenAI</Option>
                  <Option value="azure">Azure OpenAI</Option>
                  <Option value="anthropic">Anthropic</Option>
                  <Option value="local">本地模型</Option>
                </Select>
              </Form.Item>
              
              <Form.Item
                label="默认模型"
                name="defaultModel"
              >
                <Select>
                  <Option value="gpt-4">GPT-4</Option>
                  <Option value="gpt-4-turbo">GPT-4 Turbo</Option>
                  <Option value="gpt-3.5-turbo">GPT-3.5 Turbo</Option>
                  <Option value="claude-3">Claude 3</Option>
                </Select>
              </Form.Item>
              
              <Form.Item
                label="最大Token数"
                name="maxTokens"
              >
                <Input type="number" />
              </Form.Item>
              
              <Form.Item
                label="温度参数"
                name="temperature"
              >
                <Input type="number" step="0.1" min="0" max="2" />
              </Form.Item>
              
              <Form.Item>
                <Button 
                  type="primary" 
                  htmlType="submit"
                  icon={<SaveOutlined />}
                  loading={loading}
                >
                  保存设置
                </Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>
      </Row>
      
      <Row gutter={24} style={{ marginTop: 24 }}>
        <Col span={12}>
          <Card 
            title={
              <Space>
                <SafetyOutlined />
                <span>安全设置</span>
              </Space>
            }
          >
            <Form layout="vertical">
              <Form.Item label="JWT密钥有效期">
                <Input defaultValue="7天" disabled />
              </Form.Item>
              
              <Form.Item label="密码强度要求">
                <Text>至少6个字符</Text>
              </Form.Item>
              
              <Form.Item label="登录失败锁定">
                <Text>5次失败后锁定15分钟</Text>
              </Form.Item>
            </Form>
          </Card>
        </Col>
        
        <Col span={12}>
          <Card title="系统信息">
            <Space direction="vertical" style={{ width: '100%' }}>
              <div>
                <Text strong>版本: </Text>
                <Text>1.0.0</Text>
              </div>
              <div>
                <Text strong>数据库: </Text>
                <Text>PostgreSQL</Text>
              </div>
              <div>
                <Text strong>运行环境: </Text>
                <Text>Development</Text>
              </div>
              <div>
                <Text strong>后端框架: </Text>
                <Text>ASP.NET Core</Text>
              </div>
              <div>
                <Text strong>前端框架: </Text>
                <Text>React + Ant Design</Text>
              </div>
            </Space>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Settings;