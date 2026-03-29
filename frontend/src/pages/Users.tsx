import React, { useState, useEffect } from 'react';
import { Table, Card, Button, Modal, Form, Input, Select, message, Tag, Space, Popconfirm, Transfer } from 'antd';
import { UserOutlined, MailOutlined, PlusOutlined, DeleteOutlined, TeamOutlined } from '@ant-design/icons';
import authService from '../services/authService';

const { Option } = Select;

interface User {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  role: string;
  createdAt: string;
  updatedAt: string;
}

interface Role {
  id: number;
  name: string;
  code: string;
  description?: string;
}

const Users: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [roles, setRoles] = useState<Role[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [roleModalVisible, setRoleModalVisible] = useState(false);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [userRoles, setUserRoles] = useState<string[]>([]);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [form] = Form.useForm();

  useEffect(() => {
    loadUsers();
    loadRoles();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const token = authService.getToken();
      const response = await fetch('http://localhost:5000/api/users', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('获取用户列表失败');
      }

      const data = await response.json();
      setUsers(data);
    } catch (error: any) {
      message.error(error.message || '加载用户列表失败');
    } finally {
      setLoading(false);
    }
  };

  const loadRoles = async () => {
    try {
      const token = authService.getToken();
      const response = await fetch('http://localhost:5000/api/roles', {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('获取角色列表失败');
      }

      const data = await response.json();
      setRoles(data);
    } catch (error: any) {
      message.error(error.message || '加载角色列表失败');
    }
  };

  const handleManageRoles = async (user: User) => {
    setSelectedUser(user);
    try {
      const token = authService.getToken();
      const response = await fetch(`http://localhost:5000/api/users/${user.id}/roles`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('获取用户角色失败');
      }

      const userRolesData = await response.json();
      const userRoleCodes = userRolesData.map((r: Role) => r.code);
      setUserRoles(userRoleCodes);
      setAvailableRoles(roles.map(r => r.code));
      setRoleModalVisible(true);
    } catch (error: any) {
      message.error(error.message || '获取用户角色失败');
    }
  };

  const handleRoleChange = async (targetKeys: any[]) => {
    if (!selectedUser) return;

    try {
      const token = authService.getToken();
      
      // 获取当前用户的角色
      const currentRolesResponse = await fetch(`http://localhost:5000/api/users/${selectedUser.id}/roles`, {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      const currentRoles = await currentRolesResponse.json();
      const currentRoleCodes = currentRoles.map((r: Role) => r.code);

      // 计算需要添加和删除的角色
      const rolesToAdd = targetKeys.filter(code => !currentRoleCodes.includes(code));
      const rolesToRemove = currentRoleCodes.filter((code: string) => !targetKeys.includes(code));

      // 添加新角色
      for (const code of rolesToAdd) {
        const role = roles.find(r => r.code === code);
        if (role) {
          await fetch(`http://localhost:5000/api/users/${selectedUser.id}/roles/${role.id}`, {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          });
        }
      }

      // 删除旧角色
      for (const code of rolesToRemove) {
        const role = roles.find(r => r.code === code);
        if (role) {
          await fetch(`http://localhost:5000/api/users/${selectedUser.id}/roles/${role.id}`, {
            method: 'DELETE',
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          });
        }
      }

      message.success('角色更新成功');
      setRoleModalVisible(false);
      loadUsers();
    } catch (error: any) {
      message.error(error.message || '更新角色失败');
    }
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 280,
      ellipsis: true,
    },
    {
      title: '用户名',
      dataIndex: 'username',
      key: 'username',
      render: (text: string) => (
        <Space>
          <UserOutlined />
          {text}
        </Space>
      ),
    },
    {
      title: '邮箱',
      dataIndex: 'email',
      key: 'email',
      render: (text: string) => (
        <Space>
          <MailOutlined />
          {text}
        </Space>
      ),
    },
    {
      title: '角色',
      dataIndex: 'role',
      key: 'role',
      render: (role: string) => {
        const color = role === 'admin' ? 'red' : 'blue';
        const text = role === 'admin' ? '管理员' : '普通用户';
        return <Tag color={color}>{text}</Tag>;
      },
    },
    {
      title: '创建时间',
      dataIndex: 'createdAt',
      key: 'createdAt',
      render: (text: string) => new Date(text).toLocaleString(),
    },
    {
      title: '操作',
      key: 'action',
      render: (_: any, record: User) => (
        <Space>
          <Button
            type="link"
            icon={<TeamOutlined />}
            onClick={() => handleManageRoles(record)}
          >
            管理角色
          </Button>
        </Space>
      ),
    },
  ];

  const transferDataSource = roles.map(role => ({
    key: role.code,
    title: role.name,
    description: role.description,
  }));

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="用户管理"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => setModalVisible(true)}
          >
            添加用户
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={users}
          rowKey="id"
          loading={loading}
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `共 ${total} 条记录`,
          }}
        />
      </Card>

      <Modal
        title="添加用户"
        open={modalVisible}
        onCancel={() => setModalVisible(false)}
        footer={null}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={async (values) => {
            try {
              await authService.register(values.username, values.email, values.password);
              message.success('用户创建成功');
              setModalVisible(false);
              form.resetFields();
              loadUsers();
            } catch (error: any) {
              message.error(error.message || '创建用户失败');
            }
          }}
        >
          <Form.Item
            label="用户名"
            name="username"
            rules={[{ required: true, message: '请输入用户名' }]}
          >
            <Input prefix={<UserOutlined />} placeholder="用户名" />
          </Form.Item>

          <Form.Item
            label="邮箱"
            name="email"
            rules={[
              { required: true, message: '请输入邮箱' },
              { type: 'email', message: '请输入有效的邮箱地址' },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder="邮箱" />
          </Form.Item>

          <Form.Item
            label="密码"
            name="password"
            rules={[
              { required: true, message: '请输入密码' },
              { min: 6, message: '密码至少6个字符' },
            ]}
          >
            <Input.Password placeholder="密码" />
          </Form.Item>

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                创建
              </Button>
              <Button onClick={() => setModalVisible(false)}>取消</Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`管理用户角色 - ${selectedUser?.username}`}
        open={roleModalVisible}
        onCancel={() => setRoleModalVisible(false)}
        footer={null}
        width={700}
      >
        <Transfer
          dataSource={transferDataSource}
          titles={['可用角色', '已分配角色']}
          targetKeys={userRoles}
          onChange={handleRoleChange}
          render={item => `${item.title} (${item.key})`}
          listStyle={{
            width: 300,
            height: 400,
          }}
          showSearch
          filterOption={(inputValue, item) =>
            item.title.toLowerCase().includes(inputValue.toLowerCase()) ||
            item.key.toLowerCase().includes(inputValue.toLowerCase())
          }
        />
      </Modal>
    </div>
  );
};

export default Users;
