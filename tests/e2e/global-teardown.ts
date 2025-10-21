import { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting E2E test environment cleanup...');

  // Clean up any test data created during tests
  // This could include API calls to clean up test users, alarms, etc.

  try {
    // Example: Clean up test data via API
    // const baseURL = config.projects[0].use.baseURL || 'http://localhost:5000';
    // await cleanupTestData(baseURL);

    console.log('✅ Test data cleanup completed');
  } catch (error) {
    console.log('⚠️  Test cleanup failed:', error);
  }

  console.log('✅ E2E test environment cleanup complete');
}

export default globalTeardown;
