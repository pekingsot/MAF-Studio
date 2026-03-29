import React from 'react';
import { render, screen, waitFor, act } from '@testing-library/react';
import { AuthProvider, useAuth } from '../AuthContext';

const TestComponent: React.FC = () => {
  const { user, isAuthenticated, loading, login, logout, hasPermission, hasRole } = useAuth();
  
  return (
    <div>
      <div data-testid="loading">{loading.toString()}</div>
      <div data-testid="authenticated">{isAuthenticated.toString()}</div>
      <div data-testid="username">{user?.username || '未登录'}</div>
      <div data-testid="role">{user?.roles?.[0] || '无角色'}</div>
      <button onClick={() => login('testuser', 'password')}>登录</button>
      <button onClick={logout}>登出</button>
      <div data-testid="has-permission">{hasPermission('agent:read').toString()}</div>
      <div data-testid="has-role">{hasRole('admin').toString()}</div>
    </div>
  );
};

const renderWithProvider = (component: React.ReactElement) => {
  return render(
    <AuthProvider>
      {component}
    </AuthProvider>
  );
};

describe('AuthContext 测试', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('初始状态', () => {
    it('初始状态应该是未认证', async () => {
      renderWithProvider(<TestComponent />);
      
      await waitFor(() => {
        expect(screen.getByTestId('loading')).toHaveTextContent('false');
      });
      
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
      expect(screen.getByTestId('username')).toHaveTextContent('未登录');
    });
  });

  describe('登录功能', () => {
    it('登录成功后应该更新用户状态', async () => {
      const mockUser = {
        id: '1',
        username: 'testuser',
        email: 'test@example.com',
        roles: ['admin'],
        permissions: ['agent:read', 'agent:create'],
      };

      localStorage.setItem('token', 'mock-token');
      localStorage.setItem('user', JSON.stringify(mockUser));

      renderWithProvider(<TestComponent />);
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
      });
      
      expect(screen.getByTestId('username')).toHaveTextContent('testuser');
      expect(screen.getByTestId('role')).toHaveTextContent('admin');
    });
  });

  describe('权限检查', () => {
    it('应该正确检查用户权限', async () => {
      const mockUser = {
        id: '1',
        username: 'testuser',
        email: 'test@example.com',
        roles: ['admin'],
        permissions: ['agent:read', 'agent:create'],
      };

      localStorage.setItem('token', 'mock-token');
      localStorage.setItem('user', JSON.stringify(mockUser));

      renderWithProvider(<TestComponent />);
      
      await waitFor(() => {
        expect(screen.getByTestId('has-permission')).toHaveTextContent('true');
      });
    });

    it('应该正确检查用户角色', async () => {
      const mockUser = {
        id: '1',
        username: 'testuser',
        email: 'test@example.com',
        roles: ['admin'],
        permissions: [],
      };

      localStorage.setItem('token', 'mock-token');
      localStorage.setItem('user', JSON.stringify(mockUser));

      renderWithProvider(<TestComponent />);
      
      await waitFor(() => {
        expect(screen.getByTestId('has-role')).toHaveTextContent('true');
      });
    });
  });

  describe('登出功能', () => {
    it('登出后应该清除用户状态', async () => {
      const mockUser = {
        id: '1',
        username: 'testuser',
        email: 'test@example.com',
        roles: ['admin'],
        permissions: [],
      };

      localStorage.setItem('token', 'mock-token');
      localStorage.setItem('user', JSON.stringify(mockUser));

      renderWithProvider(<TestComponent />);
      
      await waitFor(() => {
        expect(screen.getByTestId('authenticated')).toHaveTextContent('true');
      });
      
      const logoutButton = screen.getByText('登出');
      await act(async () => {
        logoutButton.click();
      });
      
      expect(screen.getByTestId('authenticated')).toHaveTextContent('false');
      expect(screen.getByTestId('username')).toHaveTextContent('未登录');
      expect(localStorage.getItem('token')).toBeNull();
    });
  });
});

describe('useAuth Hook 错误处理', () => {
  it('在 AuthProvider 外使用应该抛出错误', () => {
    const consoleError = console.error;
    console.error = jest.fn();

    expect(() => {
      render(<TestComponent />);
    }).toThrow('useAuth 必须在 AuthProvider 内部使用');

    console.error = consoleError;
  });
});
