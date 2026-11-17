import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    open: true,
  },
  css: {
    modules: {
      // Clases legibles en DevTools: "Hero_title__a1b2"
      generateScopedName: '[name]_[local]__[hash:base64:4]',
    },
  },
})
