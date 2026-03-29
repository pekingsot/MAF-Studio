import React, { useState, useEffect, useRef } from 'react';
import { Card, Form, Input, Button, Avatar, message, Divider, Space, Typography, Row, Col } from 'antd';
import { UserOutlined, MailOutlined, LockOutlined, SaveOutlined } from '@ant-design/icons';
import authService from '../services/authService';

const { Title, Text } = Typography;

interface UserProfile {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  roles: string[];
  permissions: string[];
  createdAt?: string;
}

const Profile: React.FC = () => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(false);
  const [passwordForm] = Form.useForm();
  const initializedRef = useRef(false);

  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true;
      loadUserProfile();
    }
  }, []);

  const loadUserProfile = () => {
    const currentUser = authService.getUser();
    if (currentUser) {
      setUser(currentUser as UserProfile);
    }
  };

  const handleUpdateProfile = async (values: any) => {
    try {
      setLoading(true);
      message.success('个人信息更新成功');
      loadUserProfile();
    } catch (error: any) {
      message.error(error.message || '更新失败');
    } finally {
      setLoading(false);
    }
  };

  const handleChangePassword = async (values: any) => {
    if (values.newPassword !== values.confirmPassword) {
      message.error('两次输入的密码不一致');
      return;
    }

    try {
      setLoading(true);
      message.success('密码修改成功');
      passwordForm.resetFields();
    } catch (error: any) {
      message.error(error.message || '修改失败');
    } finally {
      setLoading(false);
    }
  };

  if (!user) {
    return <div>加载中...</div>;
  }

  return (
    <div style={{ padding: '24px' }}>
      <Title level={2}>个人信息</Title>
      <Divider />
      
      <Row gutter={24}>
        <Col span={8}>
          <Card>
            <div style={{ textAlign: 'center' }}>
              <Avatar 
                size={100} 
                icon={<UserOutlined />}
                style={{ 
                  backgroundColor: '#667eea',
                  marginBottom: 16
                }}
              >
                {user.username?.charAt(0).toUpperCase()}
              </Avatar>
              <Title level={4}>{user.username}</Title>
              <Text type="secondary">{user.email}</Text>
              <Divider />
              <Space direction="vertical" style={{ width: '100%' }}>
                <Text>
                  角色: <Text strong>{user.roles?.map(r => r === 'SUPER_ADMIN' ? '超级管理员' : r === 'ADMIN' ? '管理员' : '普通用户').join(', ') || '普通用户'}</Text>
                </Text>
                {user.createdAt && (
                  <Text>
                    注册时间: {new Date(user.createdAt).toLocaleDateString()}
                  </Text>
                )}
              </Space>
            </div>
          </Card>
        </Col>
        
        <Col span={16}>
          <Card title="基本信息">
            <Form
              layout="vertical"
              initialValues={{
                username: user.username,
                email: user.email,
              }}
              onFinish={handleUpdateProfile}
            >
              <Form.Item
                label="用户名"
                name="username"
                rules={[{ required: true, message: '请输入用户名' }]}
              >
                <Input prefix={<UserOutlined />} disabled />
              </Form.Item>
              
              <Form.Item
                label="邮箱"
                name="email"
                rules={[
                  { required: true, message: '请输入邮箱' },
                  { type: 'email', message: '请输入有效的邮箱地址' }
                ]}
              >
                <Input prefix={<MailOutlined />} />
              </Form.Item>
              
              <Form.Item>
                <Button 
                  type="primary" 
                  htmlType="submit" 
                  icon={<SaveOutlined />}
                  loading={loading}
                >
                  保存修改
                </Button>
              </Form.Item>
            </Form>
          </Card>
          
          <Card title="修改密码" style={{ marginTop: 24 }}>
            <Form
              form={passwordForm}
              layout="vertical"
              onFinish={handleChangePassword}
            >
              <Form.Item
                label="当前密码"
                name="currentPassword"
                rules={[{ required: true, message: '请输入当前密码' }]}
              >
                <Input.Password prefix={<LockOutlined />} />
              </Form.Item>
              
              <Form.Item
                label="新密码"
                name="newPassword"
                rules={[
                  { required: true, message: '请输入新密码' },
                  { min: 6, message: '密码至少6个字符' }
                ]}
              >
                <Input.Password prefix={<LockOutlined />} />
              </Form.Item>
              
              <Form.Item
                label="确认新密码"
                name="confirmPassword"
                rules={[{ required: true, message: '请确认新密码' }]}
              >
                <Input.Password prefix={<LockOutlined />} />
              </Form.Item>
              
              <Form.Item>
                <Button 
                  type="primary" 
                  htmlType="submit"
                  loading={loading}
                >
                  修改密码
                </Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>
      </Row>
    </div>
  );
};

export default Profile;