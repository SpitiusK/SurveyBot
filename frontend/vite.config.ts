import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'
import { getAllowedHosts } from './src/config/ngrok.config'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    open: true,
    // Dynamically load allowed hosts from ngrok.config.ts
    allowedHosts: getAllowedHosts(),
    // Proxy is optional when using ngrok API URL directly
    // Uncomment below if you want to use localhost backend instead
    // proxy: {
    //   '/api': {
    //     target: 'http://localhost:5000',
    //     changeOrigin: true,
    //     secure: false,
    //   },
    // },
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
  },
})
