import api, { setAuthToken, removeAuthToken } from './api';
import type { ApiResponse, LoginDto, AuthResponse, User } from '@/types';

class AuthService {
  private basePath = '/auth';

  // Login
  async login(dto: LoginDto): Promise<AuthResponse> {
    const response = await api.post<ApiResponse<AuthResponse>>(
      `${this.basePath}/login`,
      dto
    );
    const authData = response.data.data!;

    // Store token and user data
    setAuthToken(authData.token);
    localStorage.setItem('user', JSON.stringify(authData.user));

    return authData;
  }

  // Logout
  logout(): void {
    removeAuthToken();
  }

  // Get current user from localStorage
  getCurrentUser(): User | null {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      try {
        return JSON.parse(userStr);
      } catch {
        return null;
      }
    }
    return null;
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    const token = localStorage.getItem('authToken');
    const user = this.getCurrentUser();
    return !!(token && user);
  }

  // Refresh token (if endpoint exists)
  async refreshToken(): Promise<AuthResponse> {
    const response = await api.post<ApiResponse<AuthResponse>>(
      `${this.basePath}/refresh`
    );
    const authData = response.data.data!;

    // Update token and user data
    setAuthToken(authData.token);
    localStorage.setItem('user', JSON.stringify(authData.user));

    return authData;
  }
}

export default new AuthService();
