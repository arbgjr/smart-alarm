import { defineConfig, devices } from '@playwright/test';

/**
 * @see https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/e2e',
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html', { outputFolder: 'test-results/playwright-html-report' }],
    ['json', { outputFile: 'test-results/playwright-results.json' }],
    ['line']
  ],
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: process.env.E2E_BASE_URL || 'http://localhost:5173',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',

    /* Capture screenshot on failure */
    screenshot: 'only-on-failure',

    /* Capture video on failure */
    video: 'retain-on-failure',

    /* Global test timeout */
    actionTimeout: 30000,

    /* Ignore HTTPS errors */
    ignoreHTTPSErrors: true,
  },

  /* Configure projects for major browsers and devices */
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },

    {
      name: 'firefox',
      use: { ...devices['Desktop Firefox'] },
    },

    {
      name: 'webkit',
      use: { ...devices['Desktop Safari'] },
    },

    /* Test against mobile viewports. */
    {
      name: 'Mobile Chrome',
      use: { ...devices['Pixel 5'] },
    },
    {
      name: 'Mobile Safari',
      use: { ...devices['iPhone 12'] },
    },

    /* Test against tablet viewports. */
    {
      name: 'iPad',
      use: { ...devices['iPad Pro'] },
    },

    /* Test against different desktop sizes */
    {
      name: 'Desktop 1920x1080',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1920, height: 1080 },
      },
    },
    {
      name: 'Desktop 1366x768',
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1366, height: 768 },
      },
    },
  ],

  /* Run your local dev server before starting the tests */
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },

  /* Test output directories */
  outputDir: 'test-results/artifacts',

  /* Global setup and teardown */
  globalSetup: './tests/e2e/global-setup-simple.ts',
  globalTeardown: './tests/e2e/global-teardown-simple.ts',

  /* Expect options */
  expect: {
    /* Maximum time expect() should wait for the condition to be met */
    timeout: 10000,

    /* Threshold for pixel comparisons */
    threshold: 0.2,
  },

  /* Test timeout */
  timeout: 60000,

  /* Metadata */
  metadata: {
    'test-environment': process.env.NODE_ENV || 'test',
    'base-url': process.env.E2E_BASE_URL || 'http://localhost:5173',
    'backend-url': process.env.E2E_BACKEND_URL || 'http://localhost:5000',
  },
});
