import React from 'react';
import authService from '../services/authService';

interface PermissionGuardProps {
  permission?: string;
  permissions?: string[];
  requireAll?: boolean;
  role?: string;
  roles?: string[];
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

const PermissionGuard: React.FC<PermissionGuardProps> = ({
  permission,
  permissions,
  requireAll = false,
  role,
  roles,
  children,
  fallback = null,
}) => {
  const hasAccess = () => {
    if (role) {
      return authService.hasRole(role);
    }

    if (roles && roles.length > 0) {
      return authService.hasAnyRole(...roles);
    }

    if (permission) {
      return authService.hasPermission(permission);
    }

    if (permissions && permissions.length > 0) {
      return requireAll
        ? authService.hasAllPermissions(...permissions)
        : authService.hasAnyPermission(...permissions);
    }

    return true;
  };

  return hasAccess() ? <>{children}</> : <>{fallback}</>;
};

export default PermissionGuard;
