import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
// @ts-expect-error type error without @types/node package
import process from "node:process";
const host = process.env.TAURI_DEV_HOST;

// Custom plugin to handle Unity WebGL gzip headers
const unityWebGLPlugin = () => ({
  name: 'unity-webgl-headers',
  configureServer(server: any) {
    server.middlewares.use((req: any, res: any, next: any) => {
      if (req.url && req.url.endsWith('.gz')) {
        res.setHeader('Content-Encoding', 'gzip');
        if (req.url.endsWith('.wasm.gz')) {
          res.setHeader('Content-Type', 'application/wasm');
        } else if (req.url.endsWith('.js.gz')) {
          res.setHeader('Content-Type', 'application/javascript');
        } else if (req.url.endsWith('.data.gz')) {
          res.setHeader('Content-Type', 'application/octet-stream');
        }
      }
      next();
    });
  }
});

// https://vite.dev/config/
export default defineConfig(() => ({
  plugins: [react(), unityWebGLPlugin()],

  // Vite options tailored for Tauri development and only applied in `tauri dev` or `tauri build`
  //
  optimizeDeps: {
    exclude: [
      '@mediapipe/holistic',
      '@mediapipe/camera_utils',
      '@mediapipe/drawing_utils',
    ],
  },
  // 1. prevent Vite from obscuring rust errors
  clearScreen: false,
  // 2. tauri expects a fixed port, fail if that port is not available
  server: {
    port: 1420,
    strictPort: true,
    host: host || false,
    hmr: host
      ? {
          protocol: "ws",
          host,
          port: 1421,
        }
      : undefined,
    watch: {
      // 3. tell Vite to ignore watching `src-tauri`
      ignored: ["**/src-tauri/**"],
    },
  },
}));
