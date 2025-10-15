import { FullConfig } from '@playwright/test';
import { execSync } from 'child_process';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting E2E test teardown...');

  try {
    // Clean up Docker containers
    await cleanupContainers();
    
    // Clean up test files
    await cleanupTestFiles();
    
    // Clean up backend services
    await cleanupBackendServices();
    
    console.log('✅ E2E test teardown completed successfully');

  } catch (error) {
    console.error('❌ E2E test teardown failed:', error);
    // Don't throw error in teardown to avoid masking test failures
  }
}

async function cleanupContainers(): Promise<void> {
  try {
    console.log('🐳 Cleaning up Docker containers...');
    
    // Stop and remove test database container
    try {
      execSync('docker stop smart-alarm-test-db', { stdio: 'pipe' });
      console.log('✅ Test database container stopped');
    } catch {
      console.log('ℹ️  Test database container was not running');
    }

    // Clean up any Docker Compose services
    try {
      execSync('docker-compose -f docker-compose.test.yml down', { stdio: 'pipe' });
      console.log('✅ Docker Compose services stopped');
    } catch {
      console.log('ℹ️  No Docker Compose services to stop');
    }

    // Clean up any dangling containers
    try {
      execSync('docker container prune -f', { stdio: 'pipe' });
      console.log('✅ Cleaned up dangling containers');
    } catch (error) {
      console.warn('⚠️  Could not clean up dangling containers:', error);
    }

  } catch (error) {
    console.error('Failed to cleanup containers:', error);
  }
}

async function cleanupTestFiles(): Promise<void> {
  try {
    console.log('📁 Cleaning up test files...');
    
    // Clean up test artifacts (but keep reports for analysis)
    try {
      execSync('find test-results/artifacts -type f -name "*.png" -o -name "*.webm" | head -50 | xargs rm -f', { stdio: 'pipe' });
      console.log('✅ Cleaned up test artifact files');
    } catch (error) {
      console.warn('⚠️  Could not clean up test artifacts:', error);
    }

    // Clean up temporary test data files
    try {
      execSync('rm -rf /tmp/smart-alarm-test-*', { stdio: 'pipe' });
      console.log('✅ Cleaned up temporary test files');
    } catch (error) {
      console.warn('⚠️  Could not clean up temporary files:', error);
    }

  } catch (error) {
    console.error('Failed to cleanup test files:', error);
  }
}

async function cleanupBackendServices(): Promise<void> {
  try {
    console.log('🔧 Cleaning up backend services...');
    
    // If we started backend services, clean them up
    // This is more relevant if we were managing the backend process directly
    
    // Clean up any test-specific environment variables
    delete process.env.E2E_TEST_RUNNING;
    delete process.env.NODE_ENV;
    
    console.log('✅ Backend services cleanup completed');

  } catch (error) {
    console.error('Failed to cleanup backend services:', error);
  }
}

export default globalTeardown;