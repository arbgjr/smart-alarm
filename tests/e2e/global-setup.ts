import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting global setup for E2E tests...');

  // Start services if needed
  const baseURL = config.projects[0].use.baseURL || 'http://localhost:3001';

  try {
    // Launch browser to check if services are running
    const browser = await chromium.launch();
    const page = await browser.newPage();

    // Try to connect to the application
    console.log(`📡 Checking if application is running at ${baseURL}...`);

    try {
      await page.goto(baseURL, { timeout: 10000 });
      console.log('✅ Application is running');
    } catch (error) {
      console.log('⚠️  Application not running, tests may fail');
      console.log('💡 Make sure to start the frontend with: npm run dev');
    }

    await browser.close();

    // Setup test data or authentication if needed
    console.log('📝 Setting up test data...');

    // Create test users, clear test database, etc.
    // This would typically involve API calls to setup test state

    console.log('✅ Global setup completed');

  } catch (error) {
    console.error('❌ Global setup failed:', error);
    throw error;
  }
}

export default globalSetup;
