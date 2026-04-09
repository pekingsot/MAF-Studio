import React, { useState, useEffect } from 'react';
import { Table, Card, Button, Modal, Form, Input, Switch, message, Tag, Space, Popconfirm, Transfer } from 'antd';
import { PlusOutlined, EditOutlined, DeleteOutlined, SafetyOutlined } from '@ant-design/icons';
import authService from '../services/authService';
import { getApiUrl } from '../config/api';

interface Role {
  id: number;
  name: string;
  code: string;
  description?: string;
  isSystem: boolean;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

interface Permission {
  id: number;
  name: string;
  code: string;
  description?: string;
  resource: string;
  action: string;
}

const Roles: React.FC = () => {
  const [roles, setRoles] = useState<Role[]>([]);
  const [permissions, setPermissions] = useState<Permission[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalVisible, setModalVisible] = useState(false);
  const [permissionModalVisible, setPermissionModalVisible] = useState(false);
  const [selectedRole, setSelectedRole] = useState<Role | null>(null);
  const [rolePermissions, setRolePermissions] = useState<string[]>([]);
  const [editingRole, setEditingRole] = useState<Role | null>(null);
  const [form] = Form.useForm();

  useEffect(() => {
    loadRoles();
    loadPermissions();
  }, []);

  const loadRoles = async () => {
    try {
      setLoading(true);
      const token = authService.getToken();
      const response = await fetch(getApiUrl('/roles'), {
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
    } finally {
      setLoading(false);
    }
  };

  const loadPermissions = async () => {
    try {
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
    } catch (error: any) {
      message.error(error.message || '加载权限列表失败');
    }
  };

  const handleCreateRole = async (values: any) => {
    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl('/roles'), {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || '创建角色失败');
      }

      message.success('角色创建成功');
      setModalVisible(false);
      form.resetFields();
      loadRoles();
    } catch (error: any) {
      message.error(error.message || '创建角色失败');
    }
  };

  const handleUpdateRole = async (values: any) => {
    if (!editingRole) return;

    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl(`/roles/${editingRole.id}`), {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(values),
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || '更新角色失败');
      }

      message.success('角色更新成功');
      setModalVisible(false);
      setEditingRole(null);
      form.resetFields();
      loadRoles();
    } catch (error: any) {
      message.error(error.message || '更新角色失败');
    }
  };

  const handleDeleteRole = async (id: number) => {
    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl(`/roles/${id}`), {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || '删除角色失败');
      }

      message.success('角色删除成功');
      loadRoles();
    } catch (error: any) {
      message.error(error.message || '删除角色失败');
    }
  };

  const handleManagePermissions = async (role: Role) => {
    setSelectedRole(role);
    try {
      const token = authService.getToken();
      const response = await fetch(getApiUrl(`/roles/${role.id}/permissions`), {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });

      if (!response.ok) {
        throw new Error('获取角色权限失败');
      }

      const rolePermissionsData = await response.json();
      const rolePermissionCodes = rolePermissionsData.map((p: Permission) => p.code);
      setRolePermissions(rolePermissionCodes);
      setPermissionModalVisible(true);
    } catch (error: any) {
      message.error(error.message || '获取角色权限失败');
    }
  };

  const handlePermissionChange = async (targetKeys: any[]) => {
    if (!selectedRole) return;

    try {
      const token = authService.getToken();
      
      // 获取当前角色的权限
      const currentPermissionsResponse = await fetch(getApiUrl(`/roles/${selectedRole.id}/permissions`), {
        headers: {
          'Authorization': `Bearer ${token}`,
        },
      });
      const currentPermissions = await currentPermissionsResponse.json();
      const currentPermissionCodes = currentPermissions.map((p: Permission) => p.code);

      // 计算需要添加和删除的权限
      const permissionsToAdd = targetKeys.filter(code => !currentPermissionCodes.includes(code));
      const permissionsToRemove = currentPermissionCodes.filter((code: string) => !targetKeys.includes(code));

      // 添加新权限
      for (const code of permissionsToAdd) {
        const permission = permissions.find(p => p.code === code);
        if (permission) {
          await fetch(getApiUrl(`/roles/${selectedRole.id}/permissions/${permission.id}`), {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          });
        }
      }

      // 删除旧权限
      for (const code of permissionsToRemove) {
        const permission = permissions.find(p => p.code === code);
        if (permission) {
          await fetch(getApiUrl(`/roles/${selectedRole.id}/permissions/${permission.id}`), {
            method: 'DELETE',
            headers: {
              'Authorization': `Bearer ${token}`,
            },
          });
        }
      }

      message.success('权限更新成功');
      setPermissionModalVisible(false);
      loadRoles();
    } catch (error: any) {
      message.error(error.message || '更新权限失败');
    }
  };

  const showEditModal = (role: Role) => {
    setEditingRole(role);
    form.setFieldsValue(role);
    setModalVisible(true);
  };

  const columns = [
    {
      title: 'ID',
      dataIndex: 'id',
      key: 'id',
      width: 150,
    },
    {
      title: '角色名称',
      dataIndex: 'name',
      key: 'name',
      render: (text: string) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: '角色代码',
      dataIndex: 'code',
      key: 'code',
      render: (text: string) => <Tag color="green">{text}</Tag>,
    },
    {
      title: '描述',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '系统角色',
      dataIndex: 'isSystem',
      key: 'isSystem',
      render: (isSystem: boolean) => (
        <Tag color={isSystem ? 'red' : 'default'}>
          {isSystem ? '是' : '否'}
        </Tag>
      ),
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
      render: (_: any, record: Role) => (
        <Space>
          <Button
            type="link"
            icon={<SafetyOutlined />}
            onClick={() => handleManagePermissions(record)}
          >
            管理权限
          </Button>
          <Button
            type="link"
            icon={<EditOutlined />}
            onClick={() => showEditModal(record)}
            disabled={record.isSystem}
          >
            编辑
          </Button>
          <Popconfirm
            title="确定要删除这个角色吗？"
            onConfirm={() => handleDeleteRole(record.id)}
            okText="确定"
            cancelText="取消"
          >
            <Button
              type="link"
              danger
              icon={<DeleteOutlined />}
              disabled={record.isSystem}
            >
              删除
            </Button>
          </Popconfirm>
        </Space>
      ),
    },
  ];

  const transferDataSource = permissions.map(permission => ({
    key: permission.code,
    title: permission.name,
    description: permission.description,
  }));

  return (
    <div style={{ padding: '24px' }}>
      <Card
        title="角色管理"
        extra={
          <Button
            type="primary"
            icon={<PlusOutlined />}
            onClick={() => {
              setEditingRole(null);
              form.resetFields();
              setModalVisible(true);
            }}
          >
            添加角色
          </Button>
        }
      >
        <Table
          columns={columns}
          dataSource={roles}
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
        title={editingRole ? '编辑角色' : '添加角色'}
        open={modalVisible}
        onCancel={() => {
          setModalVisible(false);
          setEditingRole(null);
          form.resetFields();
        }}
        footer={null}
      >
        <Form
          form={form}
          layout="vertical"
          onFinish={editingRole ? handleUpdateRole : handleCreateRole}
        >
          <Form.Item
            label="角色名称"
            name="name"
            rules={[{ required: true, message: '请输入角色名称' }]}
          >
            <Input placeholder="角色名称" />
          </Form.Item>

          <Form.Item
            label="角色代码"
            name="code"
            rules={[
              { required: true, message: '请输入角色代码' },
              { pattern: /^[A-Z_]+$/, message: '角色代码只能包含大写字母和下划线' },
            ]}
          >
            <Input placeholder="角色代码（如：ADMIN）" disabled={!!editingRole} />
          </Form.Item>

          <Form.Item
            label="描述"
            name="description"
          >
            <Input.TextArea rows={3} placeholder="角色描述" />
          </Form.Item>

          {editingRole && (
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
                {editingRole ? '更新' : '创建'}
              </Button>
              <Button onClick={() => {
                setModalVisible(false);
                setEditingRole(null);
                form.resetFields();
              }}>
                取消
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`管理角色权限 - ${selectedRole?.name}`}
        open={permissionModalVisible}
        onCancel={() => setPermissionModalVisible(false)}
        footer={null}
        width={700}
      >
        <Transfer
          dataSource={transferDataSource}
          titles={['可用权限', '已分配权限']}
          targetKeys={rolePermissions}
          onChange={handlePermissionChange}
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

export default Roles;
