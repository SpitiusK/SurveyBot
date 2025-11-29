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
export const BACKEND_NGROK_URL = 'https://df0778be2c16.ngrok-free.app';

/**
 * Frontend ngrok URL (if running frontend on ngrok)
 * Used in Vite config for CORS allowedHosts
 * Leave as empty string if not using ngrok for frontend
 */
export const FRONTEND_NGROK_URL = 'https://27b2352927ab.ngrok-free.app';

/**
 * Get the API base URL based on environment
 *
 * - If accessing via ngrok frontend: use ngrok backend URL
 * - If accessing via localhost: use localhost backend URL
 * - Production: Set via environment variables
 */
export const getApiBaseUrl = (): string => {
  const customUrl = import.meta.env.VITE_API_BASE_URL;

  // Check if we're accessing the frontend via ngrok
  // If so, we must use the ngrok backend URL (localhost won't work from ngrok)
  if (typeof window !== 'undefined') {
    const currentHost = window.location.hostname;

    // If accessing via ngrok, use ngrok backend URL
    if (currentHost.includes('ngrok-free.app') ||
        currentHost.includes('ngrok.app') ||
        currentHost.includes('ngrok.io')) {
      console.log('Detected ngrok access, using ngrok backend URL:', `${BACKEND_NGROK_URL}/api`);
      return `${BACKEND_NGROK_URL}/api`;
    }
  }

  // If VITE_API_BASE_URL is explicitly set to something other than localhost, use it
  if (customUrl && !customUrl.includes('localhost')) {
    return customUrl;
  }

  // Default: use localhost for local development
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
