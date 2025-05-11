import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
      react(),
      tailwindcss(),
  ],
    server: {
        allowedHosts: [
            '3e7e-2a01-5a8-31c-495-f88c-869-1569-fd04.ngrok-free.app',
            'localhost',
        ],
    }
})
