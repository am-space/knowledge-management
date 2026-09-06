import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/health': process.env.KNOWLEDGE_API_PROXY_TARGET ?? 'http://localhost:5080',
      '/api': process.env.KNOWLEDGE_API_PROXY_TARGET ?? 'http://localhost:5080',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/testSetup.ts',
  },
})
