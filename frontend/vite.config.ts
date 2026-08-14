import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) return undefined
          if (/[\\/]node_modules[\\/](react|react-dom|react-router-dom|scheduler)[\\/]/.test(id)) return 'react-vendor'
          if (id.includes('recharts') || id.includes('d3-')) return 'charts'
          if (id.includes('@microsoft/signalr')) return 'realtime'
          if (id.includes('@tanstack/react-query')) return 'query'
          if (id.includes('@radix-ui') || id.includes('cmdk') || id.includes('lucide-react') || id.includes('sonner')) return 'ui-vendor'
          if (id.includes('date-fns')) return 'date-utils'
          return undefined
        },
      },
    },
  },
  server: {
    host: '0.0.0.0',
    port: 5173,
  },
})
