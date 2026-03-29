import { useMemo } from 'react';
import authService from '../services/authService';

export const usePermission = () => {
  const user = useMemo(() => authService.getUser(), []);

  const hasPermission = (permission: string): boolean => {
    return authService.hasPermission(permission);
  };

  const hasAnyPermission = (...permissions: string[]): boolean => {
    return authService.hasAnyPermission(...permissions);
  };

  const hasAllPermissions = (...permissions: string[]): boolean => {
    return authService.hasAllPermissions(...permissions);
  };

  const hasRole = (role: string): boolean => {
    return authService.hasRole(role);
  };

  const hasAnyRole = (...roles: string[]): boolean => {
    return authService.hasAnyRole(...roles);
  };

  const isAdmin = (): boolean => {
    return authService.isAdmin();
  };

  const isSuperAdmin = (): boolean => {
    return authService.isSuperAdmin();
  };

  return {
    user,
    roles: user?.roles || [],
    permissions: user?.permissions || [],
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    hasRole,
    hasAnyRole,
    isAdmin,
    isSuperAdmin,
  };
};
