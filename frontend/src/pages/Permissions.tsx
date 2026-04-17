import { getErrorMessage } from '../utils/errorHandler';
import React, { useState, useEffect } from 'react';
import { Table, Card, Button, Modal, Form, Input, Switch, message, Tag, Space, Popconfirm } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined } from '@ant-design/icons';
import authService from '../services/authService';
import { getApiUrl } from '../config/api';

interface Permission {
  id: number;
  name: string;
  code: string;
  description?: string;
  resource: string;
  action: string;
  isEnabled: boolean;
  createdAt: string;
}

const Permissions: React.FC = () => {
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [editingPermission, setEditingPermission] = useState<Permission | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadPermissions();
  }, []);

  const loadPermissions = async () => {
    try {
      setLoading(true);
      const token = authService.getToken();
      const response = await fetch(getApiUrl('/permissions'), {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('获取权限列表失败');
      }

      const data = await response.json();
      setPermissions(data);
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '加载权限列表失败'));
    } finally {
      setLoading(false);
    }
  };

  const handleCreatePermission = async (values: Record<string, unknown>) => {
    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl('/permissions'), {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(getErrorMessage(error, '创建权限失败'));
      }

      message.success('权限创建成功');
      setModalVisible(false);
      form.resetFields();
      loadPermissions();
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '创建权限失败'));
    }
  };

  const handleUpdatePermission = async (values: Record<string, unknown>) => {
    if (!editingPermission) return;

    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl(`/permissions/${editingPermission.id}`), {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(getErrorMessage(error, '更新权限失败'));
      }

      message.success('权限更新成功');
      setModalVisible(false);
      setEditingPermission(null);
      form.resetFields();
      loadPermissions();
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '更新权限失败'));
    }
  };

  const handleDeletePermission = async (id: number) => {
    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl(`/permissions/${id}`), {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(getErrorMessage(error, '删除权限失败'));
      }

      message.success('权限删除成功');
      loadPermissions();
    } catch (error: unknown) {
      message.error(getErrorMessage(error, '删除权限失败'));
    }
  };

  const showEditModal = (permission: Permission) => {
    setEditingPermission(permission);
    form.setFieldsValue(permission);
    setModalVisible(true);
  };

  const getActionColor = (action: string) => {
    const colorMap: { [key: string]: string } = {
      read: 'blue',
      create: 'green',
      update: 'orange',
      delete: 'red',
      manage: 'purple',
    };
    return colorMap[action] || 'default';
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 150,
    },
    {
      title: '权限名称',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: '权限代码',
      dataIndex: 'code',
      key: 'code',
      render: (text: string) => <Tag color="green">{text}</Tag>,
    },
    {
      title: '资源',
      dataIndex: 'resource',
      key: 'resource',
      render: (text: string) => <Tag color="cyan">{text}</Tag>,
    },
    {
      title: '操作',
      dataIndex: 'action',
      key: 'action',
      render: (text: string) => (
        <Tag color={getActionColor(text)}>{text.toUpperCase()}</Tag>
      ),
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '状态',
      dataIndex: 'isEnabled',
      key: 'isEnabled',
      render: (isEnabled: boolean) => (
        <Tag color={isEnabled ? 'green' : 'red'}>
          {isEnabled ? '启用' : '禁用'}
        </Tag>
      ),
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
      render: (_: unknown, record: Permission) => (
        <Space>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => showEditModal(record)}
          >
            编辑
          </Button>
          <Popconfirm
            title="确定要删除这个权限吗？"
            onConfirm={() => handleDeletePermission(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              danger
              icon={<DeleteOutlined />}
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="权限管理"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditingPermission(null);
              form.resetFields();
              setModalVisible(true);
            }}
          >
            添加权限
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={permissions}
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
        title={editingPermission ? '编辑权限' : '添加权限'}
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          setEditingPermission(null);
          form.resetFields();
        }}
        footer={null}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={editingPermission ? handleUpdatePermission : handleCreatePermission}
        >
          <Form.Item
            label="权限名称"
            name="name"
            rules={[{ required: true, message: '请输入权限名称' }]}
          >
            <Input placeholder="权限名称（如：查看智能体）" />
          </Form.Item>

          <Form.Item
            label="权限代码"
            name="code"
            rules={[
              { required: true, message: '请输入权限代码' },
              { pattern: /^[a-z:]+$/, message: '权限代码只能包含小写字母和冒号' },
            ]}
          >
            <Input placeholder="权限代码（如：agent:read）" disabled={!!editingPermission} />
          </Form.Item>

          <Form.Item
            label="资源"
            name="resource"
            rules={[{ required: true, message: '请输入资源名称' }]}
          >
            <Input placeholder="资源名称（如：agent）" />
          </Form.Item>

          <Form.Item
            label="操作"
            name="action"
            rules={[{ required: true, message: '请输入操作类型' }]}
          >
            <Input placeholder="操作类型（如：read、create、update、delete、manage）" />
          </Form.Item>

          <Form.Item
            label="描述"
            name="description"
          >
            <Input.TextArea rows={3} placeholder="权限描述" />
          </Form.Item>

          {editingPermission && (
            <Form.Item
              label="启用状态"
              name="isEnabled"
              valuePropName="checked"
            >
              <Switch checkedChildren="启用" unCheckedChildren="禁用" />
            </Form.Item>
          )}

          <Form.Item>
            <Space>
              <Button type="primary" htmlType="submit">
                {editingPermission ? '更新' : '创建'}
              </Button>
              <Button onClick={() => {
                setModalVisible(false);
                setEditingPermission(null);
                form.resetFields();
              }}>
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>
    </div>
  );
};

export default Permissions;
