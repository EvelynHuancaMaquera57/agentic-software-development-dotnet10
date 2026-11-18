import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

// URL de la API de Snap en desarrollo (perfil "http" de src/Properties/launchSettings.json).
const apiTarget = "http://localhost:5080";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
    // El navegador solo habla con Vite (un único origen, sin CORS). Vite reenvía la API.
    // changeOrigin: la API recibe Host: localhost:5080, así las shortUrl apuntan a la API y no a Vite.
    proxy: {
      "/api": { target: apiTarget, changeOrigin: true },
      "/auth": { target: apiTarget, changeOrigin: true },
    },
  },
  test: {
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
    restoreMocks: true,
    unstubGlobals: true,
  },
});
