import api, { setTokens, clearTokens } from './api';

export interface User {
  id: string;
  username: string;
  email: string;
  avatar?: string;
  roles: string[];
  permissions: string[];
  createdAt?: string;
}

export interface AuthResponse {
  message: string;
  user: User;
  token: string;
  refreshToken?: string;
}

const TOKEN_KEY = 'token';
const USER_KEY = 'user';
const REFRESH_TOKEN_KEY = 'refreshToken';

class AuthService {
  async register(username: string, email: string, password: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/register', {
      username,
      email,
      password,
    });
    this.setSession(response.data);
    return response.data;
  }

  async login(username: string, password: string): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/auth/login', {
      username,
      password,
    });
    this.setSession(response.data);
    return response.data;
  }

  private setSession(data: AuthResponse): void {
    setTokens(data.token, data.refreshToken);
    this.setUser(data.user);
  }

  async getCurrentUser(): Promise<User> {
    const token = this.getToken();
    if (!token) {
      throw new Error('未登录');
    }

    const response = await api.get<User>('/auth/me');
    this.setUser(response.data);
    return response.data;
  }

  logout(): void {
    clearTokens();
  }

  setToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  setUser(user: User): void {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  }

  getUser(): User | null {
    const userStr = localStorage.getItem(USER_KEY);
    return userStr ? JSON.parse(userStr) : null;
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  isAdmin(): boolean {
    const user = this.getUser();
    return user?.roles?.includes('SUPER_ADMIN') || user?.roles?.includes('ADMIN') || false;
  }

  isSuperAdmin(): boolean {
    const user = this.getUser();
    return user?.roles?.includes('SUPER_ADMIN') || false;
  }

  hasPermission(permission: string): boolean {
    const user = this.getUser();
    return user?.permissions?.includes(permission) || false;
  }

  hasAnyPermission(...permissions: string[]): boolean {
    const user = this.getUser();
    return permissions.some(p => user?.permissions?.includes(p)) || false;
  }

  hasAllPermissions(...permissions: string[]): boolean {
    const user = this.getUser();
    return permissions.every(p => user?.permissions?.includes(p)) || false;
  }

  hasRole(role: string): boolean {
    const user = this.getUser();
    return user?.roles?.includes(role) || false;
  }

  hasAnyRole(...roles: string[]): boolean {
    const user = this.getUser();
    return roles.some(r => user?.roles?.includes(r)) || false;
  }
}

export default new AuthService();
