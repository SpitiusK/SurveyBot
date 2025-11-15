import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

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
    allowedHosts: [
      'localhost',
      '127.0.0.1',
      '5167d6c0729b.ngrok-free.app', // Frontend ngrok URL
      '3c6dfc99c860.ngrok-free.app'  // Backend ngrok URL
    ],
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
