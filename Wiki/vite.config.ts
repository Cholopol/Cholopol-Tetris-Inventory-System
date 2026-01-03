import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const isProd = mode === 'production'
  
  return {
    plugins: [react()],
    base: isProd ? '/Cholopol-Tetris-Inventory-System/' : '/',
    build: {
      outDir: 'dist',
      assetsDir: 'assets'
    }
  }
})
