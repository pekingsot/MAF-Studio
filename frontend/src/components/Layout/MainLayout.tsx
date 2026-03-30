import React, { useState } from 'react';
import { Layout, Menu, theme, Avatar, Dropdown, Space } from 'antd';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  DashboardOutlined,
  RobotOutlined,
  TeamOutlined,
  MessageOutlined,
  CommentOutlined,
  UserOutlined,
  LogoutOutlined,
  HistoryOutlined,
  ApiOutlined,
  ExperimentOutlined,
  SettingOutlined,
  AppstoreAddOutlined,
  BugOutlined,
  SafetyOutlined,
  UserAddOutlined,
  LockOutlined,
} from '@ant-design/icons';
import { useAuth } from '../../contexts/AuthContext';

const { Header, Sider } = Layout;
const { SubMenu } = Menu;

interface MainLayoutProps {
  children: React.ReactNode;
}

const MainLayout: React.FC<MainLayoutProps> = ({ children }) => {
  const [collapsed, setCollapsed] = useState(false);
  const {
    token: { colorBgContainer },
  } = theme.useToken();
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout, isAdmin } = useAuth();

  const getSelectedKeys = () => {
    return [location.pathname];
  };

  const getOpenKeys = () => {
    const path = location.pathname;
    if (['/rag-test', '/rag-settings'].includes(path)) {
      return ['rag'];
    }
    if (['/agents', '/agent-types'].includes(path)) {
      return ['agent-management'];
    }
    if (['/llm-configs', '/logs'].includes(path)) {
      return ['system'];
    }
    if (['/users', '/roles', '/permissions'].includes(path)) {
      return ['permission-management'];
    }
    return [];
  };

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const userMenuItems = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: '个人信息',
      onClick: () => navigate('/profile'),
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: '退出登录',
      onClick: handleLogout,
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible collapsed={collapsed} onCollapse={setCollapsed}>
        <div style={{
          height: 64,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          color: 'white',
          fontSize: collapsed ? '16px' : '20px',
          fontWeight: 'bold',
          borderBottom: '1px solid rgba(255, 255, 255, 0.1)',
        }}>
          {collapsed ? 'MAF' : 'MAF Studio'}
        </div>
        <Menu
          theme="dark"
          selectedKeys={getSelectedKeys()}
          defaultOpenKeys={getOpenKeys()}
          mode="inline"
        >
          <Menu.Item key="/" icon={<DashboardOutlined />}>
            <Link to="/">仪表盘</Link>
          </Menu.Item>
          <Menu.Item key="/llm-configs" icon={<ApiOutlined />}>
            <Link to="/llm-configs">大模型配置</Link>
          </Menu.Item>
          <SubMenu key="agent-management" icon={<RobotOutlined />} title="智能体管理">
            <Menu.Item key="/agents" icon={<RobotOutlined />}>
              <Link to="/agents">智能体列表</Link>
            </Menu.Item>
            <Menu.Item key="/agent-types" icon={<AppstoreAddOutlined />}>
              <Link to="/agent-types">类型管理</Link>
            </Menu.Item>
          </SubMenu>
          <Menu.Item key="/collaborations" icon={<TeamOutlined />}>
            <Link to="/collaborations">协作管理</Link>
          </Menu.Item>
          <Menu.Item key="/collaboration-workflow" icon={<TeamOutlined />}>
            <Link to="/collaboration-workflow">协作工作流</Link>
          </Menu.Item>
          <Menu.Item key="/workflow-templates" icon={<AppstoreAddOutlined />}>
            <Link to="/workflow-templates">工作流模板</Link>
          </Menu.Item>
          <Menu.Item key="/workflow-execute" icon={<TeamOutlined />}>
            <Link to="/workflow-execute">执行工作流</Link>
          </Menu.Item>
          <Menu.Item key="/magentic-workflow" icon={<RobotOutlined />}>
            <Link to="/magentic-workflow">Magentic工作流</Link>
          </Menu.Item>
          <Menu.Item key="/skill-management" icon={<AppstoreAddOutlined />}>
            <Link to="/skill-management">Skill管理</Link>
          </Menu.Item>
          <Menu.Item key="/chat" icon={<CommentOutlined />}>
            <Link to="/chat">协作聊天</Link>
          </Menu.Item>
          <Menu.Item key="/messages" icon={<MessageOutlined />}>
            <Link to="/messages">消息中心</Link>
          </Menu.Item>
          <SubMenu key="rag" icon={<ExperimentOutlined />} title="RAG功能">
            <Menu.Item key="/rag-test">
              <Link to="/rag-test">RAG测试</Link>
            </Menu.Item>
            <Menu.Item key="/rag-settings">
              <Link to="/rag-settings">RAG配置</Link>
            </Menu.Item>
          </SubMenu>
          <SubMenu key="system" icon={<SettingOutlined />} title="系统管理">
            <Menu.Item key="/logs" icon={<HistoryOutlined />}>
              <Link to="/logs">操作日志</Link>
            </Menu.Item>
            <Menu.Item key="/system-logs" icon={<BugOutlined />}>
              <Link to="/system-logs">系统日志</Link>
            </Menu.Item>
          </SubMenu>
          {isAdmin && (
            <SubMenu key="permission-management" icon={<SafetyOutlined />} title="权限管理">
              <Menu.Item key="/users" icon={<UserAddOutlined />}>
                <Link to="/users">用户管理</Link>
              </Menu.Item>
              <Menu.Item key="/roles" icon={<TeamOutlined />}>
                <Link to="/roles">角色管理</Link>
              </Menu.Item>
              <Menu.Item key="/permissions" icon={<LockOutlined />}>
                <Link to="/permissions">权限管理</Link>
              </Menu.Item>
            </SubMenu>
          )}
        </Menu>
      </Sider>
      <Layout>
        <Header style={{ padding: 0, background: colorBgContainer }}>
          <div style={{
            padding: '0 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            height: '100%',
          }}>
            <h1 style={{ margin: 0, fontSize: '18px' }}>
              多智能体协作平台
            </h1>
            <Dropdown menu={{ items: userMenuItems }} placement="bottomRight">
              <Space style={{ cursor: 'pointer' }}>
                <Avatar 
                  style={{ backgroundColor: '#667eea' }}
                  icon={<UserOutlined />}
                >
                  {user?.username?.charAt(0).toUpperCase()}
                </Avatar>
                <span>{user?.username}</span>
              </Space>
            </Dropdown>
          </div>
        </Header>
        {children}
      </Layout>
    </Layout>
  );
};

export default MainLayout;
