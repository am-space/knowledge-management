import { defineConfig } from '@playwright/test'
import { fileURLToPath } from 'node:url'

const repositoryRoot = fileURLToPath(new URL('../../', import.meta.url))
const databasePath = process.env.KNOWLEDGE_E2E_DATABASE
if (!databasePath) throw new Error('Run scripts/verify.sh --e2e to provision an isolated database.')

export default defineConfig({
  testDir: '.',
  testMatch: '*.spec.js',
  workers: 1,
  forbidOnly: Boolean(process.env.CI),
  retries: 0,
  use: {
    baseURL: 'http://127.0.0.1:5174',
    browserName: 'chromium',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  webServer: [
    {
      command: 'dotnet run --no-build --no-launch-profile --project src/Knowledge.Server --urls http://127.0.0.1:5081',
      cwd: repositoryRoot,
      url: 'http://127.0.0.1:5081/health/ready',
      reuseExistingServer: false,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Production',
        Persistence__Provider: 'Sqlite',
        Persistence__SqliteConnectionString: `Data Source=${databasePath}`,
      },
      gracefulShutdown: { signal: 'SIGTERM', timeout: 5000 },
    },
    {
      command: 'npm run dev --prefix src/Knowledge.Web -- --host 127.0.0.1 --port 5174 --strictPort',
      cwd: repositoryRoot,
      url: 'http://127.0.0.1:5174',
      reuseExistingServer: false,
      env: { KNOWLEDGE_API_PROXY_TARGET: 'http://127.0.0.1:5081' },
      gracefulShutdown: { signal: 'SIGTERM', timeout: 5000 },
    },
  ],
})
