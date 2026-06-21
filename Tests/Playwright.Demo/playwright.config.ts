import { defineConfig, devices } from '@playwright/test';

const headless = !(process.env.E2E_HEADLESS === 'false' || process.env.E2E_HEADLESS === '0');
const slowMo = Number.parseInt(process.env.DEMO_SLOW_MO ?? '500', 10);

export default defineConfig({
  testDir: './tests',
  timeout: 180_000,
  expect: { timeout: 90_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list']],
  use: {
    baseURL: process.env.BASE_URL ?? 'http://localhost:5000',
    locale: 'en-US',
    viewport: { width: 1440, height: 900 },
    headless,
    trace: 'on-first-retry',
    video: 'off',
    launchOptions: {
      slowMo: Number.isFinite(slowMo) ? slowMo : 500,
    },
  },
  projects: [
    {
      name: 'chromium-demo',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
