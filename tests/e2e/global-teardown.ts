import { FullConfig } from '@playwright/test';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting global teardown for E2E tests...');

  try {
    // Cleanup test data
    console.log('🗑️  Cleaning up test data...');

    // Remove test users, clear test database, etc.
    // This would typically involve API calls to cleanup test state

    // Stop services if they were started by the test suite
    console.log('🛑 Stopping test services...');

    // If we started any services in global-setup, stop them here

    console.log('✅ Global teardown completed');

  } catch (error) {
    console.error('❌ Global teardown failed:', error);
    // Don't throw here as it might mask test failures
  }
}

export default globalTeardown;
