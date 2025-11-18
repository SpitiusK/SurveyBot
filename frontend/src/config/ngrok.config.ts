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
 * Backend API ngrok URL
 * Used for all API requests to the backend
 *
 * Set to your ngrok URL when using remote access:
 * Example: https://abc123def45.ngrok-free.app
 */
export const BACKEND_NGROK_URL = 'https://3c6dfc99c860.ngrok-free.app';

/**
 * Frontend ngrok URL (if running frontend on ngrok)
 * Used in Vite config for CORS allowedHosts
 * Leave as empty string if not using ngrok for frontend
 */
export const FRONTEND_NGROK_URL = 'https://5167d6c0729b.ngrok-free.app';

/**
 * Get the API base URL based on environment
 *
 * - Development (localhost): http://localhost:5000/api
 * - Development (ngrok): https://ngrok-url/api
 * - Production: Set via environment variables
 */
export const getApiBaseUrl = (): string => {
  const env = import.meta.env.MODE;
  const customUrl = import.meta.env.VITE_API_BASE_URL;

  // If VITE_API_BASE_URL is explicitly set, use it
  if (customUrl && customUrl !== 'http://localhost:5000/api') {
    return customUrl;
  }

  // Development: prefer localhost, fallback to ngrok
  if (env === 'development') {
    // Try localhost first
    try {
      const httpUrl = 'http://localhost:5000/api';
      // This would need actual health check, but for now default to localhost
      return httpUrl;
    } catch {
      // Fallback to ngrok if localhost unavailable
      return `${BACKEND_NGROK_URL}/api`;
    }
  }

  // Production: use environment variable or ngrok
  return customUrl || `${BACKEND_NGROK_URL}/api`;
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
