import { chromium, FullConfig } from '@playwright/test';

async function globalSetup(config: FullConfig) {
  console.log('🚀 Starting E2E test environment setup...');

  // Check if the application is running
  const baseURL = config.projects[0].use.baseURL || 'http://localhost:5000';

  try {
    const browser = await chromium.launch();
    const page = await browser.newPage();

    // Try to connect to the application
    console.log(`🔍 Checking if application is running at ${baseURL}...`);

    const response = await page.goto(`${baseURL}/health`, {
      waitUntil: 'networkidle',
      timeout: 10000
    });

    if (response?.ok()) {
      console.log('✅ Application is running and healthy');
    } else {
      console.log('⚠️  Application health check failed, but continuing with tests');
    }

    await browser.close();
  } catch (error) {
    console.log('⚠️  Could not connect to application. Make sure it\'s running at', baseURL);
    console.log('   You can start it with: dotnet run --project src/SmartAlarm.Api');
  }

  // Set up test data or authentication if needed
  console.log('🔧 Setting up test environment...');

  // Store any global test data
  process.env.E2E_TEST_TIMESTAMP = new Date().toISOString();

  console.log('✅ E2E test environment setup complete');
}

export default globalSetup;
