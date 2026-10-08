import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// 开发时把 /api 代理到后端，浏览器视角是同源请求：
// 不用处理 CORS，Cookie 也不会受 SameSite 影响。
// 后端默认监听 http://localhost:15241（launchSettings 的 http profile）。
//
// 走了代理，所以浏览器里页面的 origin 是 5173 而不是 15241 ——
// Google 控制台的「已获授权的 JavaScript 来源」要填 5173。
export default defineConfig({
  plugins: [react(), tailwindcss()],

  resolve: {
    // 与 tsconfig.json 的 paths 保持一致：@/ → src/
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },

  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:15241',
        changeOrigin: true,
      },
    },
  },
})
