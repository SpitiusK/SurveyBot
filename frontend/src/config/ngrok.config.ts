/**
 * Centralized ngrok Configuration
 *
 * This file contains all ngrok URLs used in the application.
 * Update these URLs when your ngrok session expires and you get new URLs.
 *
 * How to get new ngrok URLs:
 * 1. Run: ngrok http 5000 (for backend on port 5000)
 * 2. Copy the HTTPS URL from the output (e.g., https://abc123.ngrok-free.app)
 * 3. Update the URLs below
 * 4. No other changes needed - all imports reference these URLs
 *
 * @see https://ngrok.com/docs for ngrok documentation
 */

/**
 * Safely get environment variable (works in both browser and Node.js build context)
 * import.meta.env is undefined during Vite config loading in Node.js
 */
const getEnvVar = (key: string): string | undefined => {
  try {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return (import.meta as any)?.env?.[key];
  } catch {
    return undefined;
  }
};

/**
 * Backend API ngrok URL (for Vite dev server CORS allowedHosts only)
 *
 * NOTE: This is NOT used for API calls anymore!
 * API calls use relative /api path, which nginx proxies to the backend.
 * This constant is only used for Vite dev server CORS configuration.
 *
 * The ngrok URL here can be stale - it won't affect production functionality.
 */
export const BACKEND_NGROK_URL = getEnvVar('VITE_BACKEND_NGROK_URL') ||
  'placeholder.ngrok-free.app';

/**
 * Frontend ngrok URL (for Vite dev server CORS allowedHosts only)
 *
 * NOTE: This is NOT critical for production.
 * Only used in Vite dev server config for allowedHosts.
 */
export const FRONTEND_NGROK_URL = getEnvVar('VITE_FRONTEND_NGROK_URL') ||
  'placeholder.ngrok-free.app';

/**
 * Get the API base URL based on environment
 *
 * Priority:
 * 1. Docker deployment: Use relative /api path (nginx proxies to backend)
 * 2. ngrok access: Use relative /api path (nginx proxies to backend)
 * 3. Explicit env var: Use VITE_API_BASE_URL if set
 * 4. Local development: Use localhost:5000/api
 *
 * Key insight: nginx is configured to proxy /api/* to the backend container,
 * so we don't need separate ngrok URLs - relative paths work for both
 * localhost and ngrok access!
 */
export const getApiBaseUrl = (): string => {
  const customUrl = getEnvVar('VITE_API_BASE_URL');

  // For Docker deployment (VITE_API_BASE_URL=/api):
  // Use relative /api path - nginx proxies to backend container
  // This works for both localhost:3000 and ngrok access!
  if (customUrl === '/api') {
    console.log('Using nginx proxy for API (relative /api path)');
    return '/api';
  }

  // For ngrok access without Docker's /api env var:
  // Still use relative path - nginx handles the routing
  if (typeof window !== 'undefined') {
    const currentHost = window.location.hostname;

    if (currentHost.includes('ngrok-free.app') ||
        currentHost.includes('ngrok.app') ||
        currentHost.includes('ngrok.io')) {
      // Check if explicit ngrok URL is configured via env var (advanced use case)
      const ngrokUrl = getEnvVar('VITE_BACKEND_NGROK_URL');
      if (ngrokUrl) {
        console.log('Using explicit ngrok backend URL:', `${ngrokUrl}/api`);
        return `${ngrokUrl}/api`;
      }
      // Default: use relative path (nginx proxy) - works automatically!
      console.log('Detected ngrok access, using nginx proxy (relative /api path)');
      return '/api';
    }
  }

  // If VITE_API_BASE_URL is explicitly set to something other than localhost, use it
  if (customUrl && !customUrl.includes('localhost')) {
    return customUrl;
  }

  // Default: use localhost for local development without Docker
  return 'http://localhost:5000/api';
};

/**
 * Get allowed hosts for Vite server
 * Used for development environment CORS
 */
export const getAllowedHosts = (): string[] => {
  const hosts = [
    'localhost',
    '127.0.0.1',
  ];

  // Add ngrok URLs if configured
  if (BACKEND_NGROK_URL) {
    const backendDomain = BACKEND_NGROK_URL.replace('https://', '').replace('http://', '');
    if (backendDomain) hosts.push(backendDomain);
  }

  if (FRONTEND_NGROK_URL) {
    const frontendDomain = FRONTEND_NGROK_URL.replace('https://', '').replace('http://', '');
    if (frontendDomain) hosts.push(frontendDomain);
  }

  return hosts;
};

/**
 * Configuration validation
 * Checks if ngrok URLs are properly configured
 */
export const validateNgrokConfig = (): { valid: boolean; message: string } => {
  if (BACKEND_NGROK_URL && !BACKEND_NGROK_URL.startsWith('https://')) {
    return {
      valid: false,
      message: 'Backend ngrok URL must start with https://',
    };
  }

  if (FRONTEND_NGROK_URL && !FRONTEND_NGROK_URL.startsWith('https://')) {
    return {
      valid: false,
      message: 'Frontend ngrok URL must start with https://',
    };
  }

  return {
    valid: true,
    message: 'ngrok configuration is valid',
  };
};

/**
 * Log configuration on startup (development only)
 * Helps debug API URL configuration
 */
if (typeof window !== 'undefined' && getEnvVar('DEV')) {
  console.log('='.repeat(50));
  console.log('Frontend API Configuration');
  console.log('='.repeat(50));
  console.log('VITE_API_BASE_URL:', getEnvVar('VITE_API_BASE_URL') || '(not set)');
  console.log('Resolved API base URL:', getApiBaseUrl());
  console.log('Environment mode:', getEnvVar('MODE'));
  console.log('='.repeat(50));
}
