import { chromium, FullConfig } from '@playwright/test';
import path from 'path';
import { fileURLToPath } from 'url';
import os from 'os';

// ES module compatibility
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Windows compatibility helpers
const isWindows = os.platform() === 'win32';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting E2E test setup (simplified)...');

  try {
    // Skip Docker setup and use in-memory/mock data for tests
    console.log('📦 Using simplified test setup without Docker dependencies');

    // Wait for frontend to be ready
    console.log('⏳ Waiting for frontend to be ready...');
    await waitForFrontend();

    // Set up test data in browser storage
    console.log('👤 Setting up test data...');
    await setupTestData();

    console.log('✅ E2E test setup completed successfully');

  } catch (error) {
    console.error('❌ E2E test setup failed:', error);
    throw error;
  }
}

async function waitForFrontend(): Promise<void> {
  const frontendUrl = process.env.E2E_BASE_URL || 'http://localhost:5173';
  let attempts = 0;
  const maxAttempts = 30;

  console.log(`⏳ Waiting for frontend at ${frontendUrl}...`);

  while (attempts < maxAttempts) {
    try {
      const response = await fetch(frontendUrl, {
        signal: AbortSignal.timeout(5000),
        headers: { 'Accept': 'text/html' }
      });

      if (response.ok || response.status === 404) { // 404 is ok for SPA root
        console.log(`✅ Frontend is ready`);
        return;
      }
    } catch (error) {
      attempts++;
      if (attempts < maxAttempts) {
        console.log(`⏳ Frontend not ready, retrying... (${attempts}/${maxAttempts})`);
        await new Promise(resolve => setTimeout(resolve, 2000));
      }
    }
  }

  throw new Error(`Frontend failed to start within timeout`);
}

async function setupTestData(): Promise<void> {
  try {
    // Set up test data in localStorage for offline tests
    const browser = await chromium.launch();
    const page = await browser.newPage();

    try {
      await page.goto(process.env.E2E_BASE_URL || 'http://localhost:5173');

      // Set up test authentication state
      await page.evaluate(() => {
        const testAuthState = {
          state: {
            user: {
              id: 'test-user-id',
              email: 'testuser@example.com',
              name: 'Test User',
              role: 'user'
            },
            token: 'test-jwt-token',
            isAuthenticated: true,
            refreshToken: 'test-refresh-token'
          },
          version: 0
        };

        localStorage.setItem('smart-alarm-auth', JSON.stringify(testAuthState));

        // Set up test alarms
        const testAlarms = {
          state: {
            alarms: [
              {
                id: 'test-alarm-1',
                name: 'Morning Alarm',
                time: '07:00',
                triggerTime: '07:00',
                isEnabled: true,
                isRecurring: true,
                daysOfWeek: ['monday', 'tuesday', 'wednesday', 'thursday', 'friday'],
                description: 'Daily morning alarm',
                userId: 'test-user-id',
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString()
              },
              {
                id: 'test-alarm-2',
                name: 'Weekend Alarm',
                time: '09:00',
                triggerTime: '09:00',
                isEnabled: false,
                isRecurring: true,
                daysOfWeek: ['saturday', 'sunday'],
                description: 'Weekend morning alarm',
                userId: 'test-user-id',
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString()
              }
            ],
            totalCount: 2,
            currentPage: 1,
            totalPages: 1,
            pageSize: 10,
            filters: {},
            isLoading: false,
            error: null,
            lastSync: new Date().toISOString()
          },
          version: 0
        };

        localStorage.setItem('smart-alarm-alarms', JSON.stringify(testAlarms));

        // Set up test settings
        const testSettings = {
          state: {
            theme: 'light',
            notifications: {
              enabled: true,
              sound: true,
              vibration: true
            },
            language: 'en',
            timezone: 'UTC'
          },
          version: 0
        };

        localStorage.setItem('smart-alarm-settings', JSON.stringify(testSettings));

        // Enable ML data collection for testing
        localStorage.setItem('ml-data-collection-consent', 'true');

        console.log('✅ Test data initialized in localStorage');
      });

    } finally {
      await browser.close();
    }

    console.log('✅ Test data setup completed');

  } catch (error) {
    console.error('Failed to setup test data:', error);
    throw error;
  }
}

export default globalSetup;
