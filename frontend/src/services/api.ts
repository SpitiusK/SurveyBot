import axios, { type AxiosInstance, AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { getApiBaseUrl } from '@/config/ngrok.config';

// Create axios instance with default config
// API URL is determined by getApiBaseUrl() which checks:
// 1. Environment variable VITE_API_BASE_URL
// 2. ngrok.config.ts BACKEND_NGROK_URL
// 3. Defaults to localhost:5000/api
const api: AxiosInstance = axios.create({
  baseURL: getApiBaseUrl(),
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    'ngrok-skip-browser-warning': 'true', // Bypass ngrok warning page
  },
});

// Request interceptor - Add auth token and ngrok bypass header
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('authToken');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    // Ensure ngrok bypass header is always present
    if (config.headers) {
      config.headers['ngrok-skip-browser-warning'] = 'true';
    }
    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Flag to prevent multiple token refresh attempts
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (reason?: any) => void;
}> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });

  failedQueue = [];
};

// Response interceptor - Handle errors globally with token refresh
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response) {
      const status = error.response.status;

      if (status === 401 && !originalRequest._retry) {
        if (isRefreshing) {
          // If already refreshing, queue this request
          return new Promise((resolve, reject) => {
            failedQueue.push({ resolve, reject });
          })
            .then((token) => {
              if (originalRequest.headers) {
                originalRequest.headers.Authorization = `Bearer ${token}`;
              }
              return api(originalRequest);
            })
            .catch((err) => Promise.reject(err));
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          // Attempt to refresh the token
          const response = await axios.post(
            `${import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'}/auth/refresh`,
            {},
            {
              headers: {
                Authorization: `Bearer ${localStorage.getItem('authToken')}`,
                'ngrok-skip-browser-warning': 'true', // Bypass ngrok warning page
              },
            }
          );

          const { token } = response.data.data;
          localStorage.setItem('authToken', token);

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }

          processQueue(null, token);
          isRefreshing = false;

          return api(originalRequest);
        } catch (refreshError) {
          processQueue(refreshError, null);
          isRefreshing = false;

          // Clear auth and redirect to login
          localStorage.removeItem('authToken');
          localStorage.removeItem('user');
          window.location.href = '/login';

          return Promise.reject(refreshError);
        }
      }

      switch (status) {
        case 403:
          console.error('Forbidden: You do not have permission to access this resource');
          break;
        case 404:
          console.error('Resource not found');
          break;
        case 500:
          console.error('Server error: Please try again later');
          break;
        default:
          console.error(`Error ${status}: ${error.message}`);
      }
    } else if (error.request) {
      console.error('Network error: Please check your connection');
    } else {
      console.error('Request error:', error.message);
    }

    return Promise.reject(error);
  }
);

export default api;

// Helper functions for authentication
export const setAuthToken = (token: string): void => {
  localStorage.setItem('authToken', token);
};

export const getAuthToken = (): string | null => {
  return localStorage.getItem('authToken');
};

export const removeAuthToken = (): void => {
  localStorage.removeItem('authToken');
  localStorage.removeItem('user');
};

export const isAuthenticated = (): boolean => {
  return !!getAuthToken();
};
