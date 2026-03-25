import React, { useEffect, useState } from 'react';
import { Routes, Route, Navigate, useNavigate, useLocation } from 'react-router-dom';
import { Layout, Spin } from 'antd';
import MainLayout from './components/Layout/MainLayout';
import Dashboard from './pages/Dashboard';
import Agents from './pages/Agents';
import AgentDetail from './pages/AgentDetail';
import AgentTypes from './pages/AgentTypes';
import Collaborations from './pages/Collaborations';
import CollaborationDetail from './pages/CollaborationDetail';
import Messages from './pages/Messages';
import CollaborationChat from './pages/CollaborationChat';
import Login from './pages/Login';
import Profile from './pages/Profile';
import Settings from './pages/Settings';
import OperationLogs from './pages/OperationLogs';
import LLMConfigs from './pages/LLMConfigs';
import RagTest from './pages/RagTest';
import RagSettings from './pages/RagSettings';
import SystemLogs from './pages/SystemLogs';
import authService from './services/authService';

const { Content } = Layout;

function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const checkAuth = () => {
      const authenticated = authService.isAuthenticated();
      setIsAuthenticated(authenticated);
      setLoading(false);
    };

    checkAuth();

    const handleStorageChange = () => {
      checkAuth();
    };

    window.addEventListener('storage', handleStorageChange);
    
    const interval = setInterval(checkAuth, 1000);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      clearInterval(interval);
    };
  }, []);

  useEffect(() => {
    if (isAuthenticated && location.pathname === '/login') {
      navigate('/');
    }
  }, [isAuthenticated, location.pathname, navigate]);

  if (loading) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    );
  }

  return (
    <MainLayout>
      <Content style={{ padding: '24px', minHeight: 'calc(100vh - 64px)' }}>
        <Routes>
          <Route path="/" element={<Dashboard />} />
          <Route path="/agents" element={<Agents />} />
          <Route path="/agents/:id" element={<AgentDetail />} />
          <Route path="/agent-types" element={<AgentTypes />} />
          <Route path="/collaborations" element={<Collaborations />} />
          <Route path="/collaborations/:id" element={<CollaborationDetail />} />
          <Route path="/messages" element={<Messages />} />
          <Route path="/chat" element={<CollaborationChat />} />
          <Route path="/profile" element={<Profile />} />
          <Route path="/settings" element={<Settings />} />
          <Route path="/logs" element={<OperationLogs />} />
          <Route path="/llm-configs" element={<LLMConfigs />} />
          <Route path="/rag-test" element={<RagTest />} />
          <Route path="/rag-settings" element={<RagSettings />} />
          <Route path="/system-logs" element={<SystemLogs />} />
          <Route path="/login" element={<Navigate to="/" replace />} />
        </Routes>
      </Content>
    </MainLayout>
  );
}

export default App;