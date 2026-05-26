import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

function resolveApiTarget(): string {
  const candidates = [
    process.env.SERVER_HTTPS,
    process.env.SERVER_HTTP,
    process.env.services__lancamentos__https__0,
    process.env.services__lancamentos__http__0,
    process.env.SERVICES__LANCAMENTOS__HTTPS__0,
    process.env.SERVICES__LANCAMENTOS__HTTP__0,
  ];

  for (const value of candidates) {
    if (value && value.trim().length > 0) {
      return value;
    }
  }

  // Aspire may inject indexed endpoint env vars with different index values.
  for (const [key, value] of Object.entries(process.env)) {
    if (!value || value.trim().length === 0) continue;

    if (/services__lancamentos__https__/i.test(key)) {
      return value;
    }
  }

  for (const [key, value] of Object.entries(process.env)) {
    if (!value || value.trim().length === 0) continue;

    if (/services__lancamentos__http__/i.test(key)) {
      return value;
    }
  }

  // Local fallback for direct frontend runs outside Aspire orchestration.
  return "http://localhost:5317";
}

// https://vite.dev/config/
export default defineConfig(() => {
  const apiTarget = resolveApiTarget();

  return {
    plugins: [react()],
    server: {
      proxy: {
        // Proxy API calls to the lancamentos service.
        "/api": {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  };
});
