import React from 'react';
import { render, screen } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '../AuthContext';
import { I18nProvider } from '../I18nContext';
import PermissionGuard from '../../components/PermissionGuard';

const mockUser = {
  id: '1',
  username: 'testuser',
  email: 'test@example.com',
  roles: ['admin'],
  permissions: ['agent:read', 'agent:create'],
};

const renderWithProviders = (
  component: React.ReactElement,
  { user = mockUser }: { user?: typeof mockUser | null } = {}
) => {
  if (user) {
    localStorage.setItem('token', 'mock-token');
    localStorage.setItem('user', JSON.stringify(user));
  }

  return render(
    <BrowserRouter>
      <I18nProvider>
        <AuthProvider>
          {component}
        </AuthProvider>
      </I18nProvider>
    </BrowserRouter>
  );
};

describe('PermissionGuard 组件测试', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('权限检查', () => {
    it('有权限时应该显示子组件', () => {
      renderWithProviders(
        <PermissionGuard permission="agent:read">
          <div data-testid="protected-content">受保护内容</div>
        </PermissionGuard>
      );

      expect(screen.getByTestId('protected-content')).toBeInTheDocument();
    });

    it('无权限时应该隐藏子组件', () => {
      renderWithProviders(
        <PermissionGuard permission="agent:delete">
          <div data-testid="protected-content">受保护内容</div>
        </PermissionGuard>
      );

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
    });

    it('无权限时应该显示 fallback 内容', () => {
      renderWithProviders(
        <PermissionGuard
          permission="agent:delete"
          fallback={<div data-testid="fallback">无权限</div>}
        >
          <div data-testid="protected-content">受保护内容</div>
        </PermissionGuard>
      );

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
      expect(screen.getByTestId('fallback')).toBeInTheDocument();
    });
  });

  describe('角色检查', () => {
    it('有角色时应该显示子组件', () => {
      renderWithProviders(
        <PermissionGuard role="admin">
          <div data-testid="protected-content">管理员内容</div>
        </PermissionGuard>
      );

      expect(screen.getByTestId('protected-content')).toBeInTheDocument();
    });

    it('无角色时应该隐藏子组件', () => {
      renderWithProviders(
        <PermissionGuard role="super_admin">
          <div data-testid="protected-content">超级管理员内容</div>
        </PermissionGuard>
      );

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
    });
  });

  describe('多权限检查', () => {
    it('requireAll=true 时应该检查所有权限', () => {
      renderWithProviders(
        <PermissionGuard
          permissions={['agent:read', 'agent:create']}
          requireAll
        >
          <div data-testid="protected-content">需要全部权限</div>
        </PermissionGuard>
      );

      expect(screen.getByTestId('protected-content')).toBeInTheDocument();
    });

    it('requireAll=false 时只需一个权限', () => {
      renderWithProviders(
        <PermissionGuard
          permissions={['agent:read', 'agent:delete']}
          requireAll={false}
        >
          <div data-testid="protected-content">只需一个权限</div>
        </PermissionGuard>
      );

      expect(screen.getByTestId('protected-content')).toBeInTheDocument();
    });
  });

  describe('未登录状态', () => {
    it('未登录时应该隐藏内容', () => {
      renderWithProviders(
        <PermissionGuard permission="agent:read">
          <div data-testid="protected-content">受保护内容</div>
        </PermissionGuard>,
        { user: null }
      );

      expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
    });
  });
});
