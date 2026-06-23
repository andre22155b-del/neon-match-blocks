import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "node",
    include: ["src/lib/**/*.test.js"],
    coverage: {
      provider: "v8",
      reporter: ["text", "html"],
      include: ["src/lib/**/*.js"],
      exclude: ["src/lib/**/*.test.js"],
      lines: 100,
      functions: 100,
      branches: 100,
      statements: 100,
    },
  },
});
