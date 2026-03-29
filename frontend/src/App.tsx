import React, { Suspense, lazy } from 'react';
import { Routes, Route, Navigate, useNavigate, useLocation } from 'react-router-dom';
import { Layout, Spin } from 'antd';
import MainLayout from './components/Layout/MainLayout';
import { useAuth } from './contexts/AuthContext';

const { Content } = Layout;

const lazyLoad = (Component: React.LazyExoticComponent<React.FC>) => (
  <Suspense
    fallback={
      <div className="flex-center" style={{ height: '100%', padding: 100 }}>
        <Spin size="large" tip="加载中..." />
      </div>
    }
  >
    <Component />
  </Suspense>
);

const Dashboard = lazy(() => import('./pages/Dashboard'));
const Agents = lazy(() => import('./pages/Agents'));
const AgentDetail = lazy(() => import('./pages/AgentDetail'));
const AgentTypes = lazy(() => import('./pages/AgentTypes'));
const Collaborations = lazy(() => import('./pages/Collaborations'));
const CollaborationDetail = lazy(() => import('./pages/CollaborationDetail'));
const Messages = lazy(() => import('./pages/Messages'));
const CollaborationChat = lazy(() => import('./pages/CollaborationChat'));
const Login = lazy(() => import('./pages/Login'));
const Profile = lazy(() => import('./pages/Profile'));
const Settings = lazy(() => import('./pages/Settings'));
const OperationLogs = lazy(() => import('./pages/OperationLogs'));
const LLMConfigs = lazy(() => import('./pages/LLMConfigs'));
const RagTest = lazy(() => import('./pages/RagTest'));
const RagSettings = lazy(() => import('./pages/RagSettings'));
const SystemLogs = lazy(() => import('./pages/SystemLogs'));
const Users = lazy(() => import('./pages/Users'));
const Roles = lazy(() => import('./pages/Roles'));
const Permissions = lazy(() => import('./pages/Permissions'));

function App() {
  const { isAuthenticated, loading } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  React.useEffect(() => {
    if (isAuthenticated && location.pathname === '/login') {
      navigate('/');
    }
  }, [isAuthenticated, location.pathname, navigate]);

  if (loading) {
    return (
      <div className="flex-center" style={{
        height: '100vh',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="/login" element={lazyLoad(Login)} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <MainLayout>
      <Content style={{ padding: '24px', minHeight: 'calc(100vh - 64px)' }}>
        <Routes>
          <Route path="/" element={lazyLoad(Dashboard)} />
          <Route path="/agents" element={lazyLoad(Agents)} />
          <Route path="/agents/:id" element={lazyLoad(AgentDetail)} />
          <Route path="/agent-types" element={lazyLoad(AgentTypes)} />
          <Route path="/collaborations" element={lazyLoad(Collaborations)} />
          <Route path="/collaborations/:id" element={lazyLoad(CollaborationDetail)} />
          <Route path="/messages" element={lazyLoad(Messages)} />
          <Route path="/chat" element={lazyLoad(CollaborationChat)} />
          <Route path="/profile" element={lazyLoad(Profile)} />
          <Route path="/settings" element={lazyLoad(Settings)} />
          <Route path="/logs" element={lazyLoad(OperationLogs)} />
          <Route path="/llm-configs" element={lazyLoad(LLMConfigs)} />
          <Route path="/rag-test" element={lazyLoad(RagTest)} />
          <Route path="/rag-settings" element={lazyLoad(RagSettings)} />
          <Route path="/system-logs" element={lazyLoad(SystemLogs)} />
          <Route path="/users" element={lazyLoad(Users)} />
          <Route path="/roles" element={lazyLoad(Roles)} />
          <Route path="/permissions" element={lazyLoad(Permissions)} />
          <Route path="/login" element={<Navigate to="/" replace />} />
        </Routes>
      </Content>
    </MainLayout>
  );
}

export default App;
