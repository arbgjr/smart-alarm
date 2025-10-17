import { FullConfig } from '@playwright/test';
import { execSync } from 'child_process';
import os from 'os';
import fs from 'fs';
import path from 'path';

// Windows compatibility helpers
const isWindows = os.platform() === 'win32';
const dockerCmd = isWindows ? 'docker.exe' : 'docker';
const dockerComposeCmd = isWindows ? 'docker-compose.exe' : 'docker-compose';

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
      execSync(`${dockerCmd} stop smart-alarm-test-db`, { stdio: 'pipe' });
      console.log('✅ Test database container stopped');
    } catch {
      console.log('ℹ️  Test database container was not running');
    }

    // Clean up any Docker Compose services
    try {
      execSync(`${dockerComposeCmd} -f docker-compose.test.yml down`, { stdio: 'pipe' });
      console.log('✅ Docker Compose services stopped');
    } catch {
      console.log('ℹ️  No Docker Compose services to stop');
    }

    // Clean up any dangling containers
    try {
      execSync(`${dockerCmd} container prune -f`, { stdio: 'pipe' });
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

    // Clean up test artifacts (but keep reports for analysis) - Windows compatible
    try {
      const artifactsDir = path.join(process.cwd(), 'test-results', 'artifacts');
      if (fs.existsSync(artifactsDir)) {
        const files = fs.readdirSync(artifactsDir);
        let cleanedCount = 0;

        for (const file of files.slice(0, 50)) { // Limit to 50 files like the original
          if (file.endsWith('.png') || file.endsWith('.webm')) {
            try {
              fs.unlinkSync(path.join(artifactsDir, file));
              cleanedCount++;
            } catch (error) {
              // Ignore individual file errors
            }
          }
        }

        if (cleanedCount > 0) {
          console.log(`✅ Cleaned up ${cleanedCount} test artifact files`);
        }
      }
    } catch (error) {
      console.warn('⚠️  Could not clean up test artifacts:', error);
    }

    // Clean up temporary test data files - Windows compatible
    try {
      const tempDir = os.tmpdir();
      const tempFiles = fs.readdirSync(tempDir).filter(file => file.startsWith('smart-alarm-test-'));

      for (const file of tempFiles) {
        try {
          const fullPath = path.join(tempDir, file);
          const stat = fs.statSync(fullPath);

          if (stat.isDirectory()) {
            fs.rmSync(fullPath, { recursive: true, force: true });
          } else {
            fs.unlinkSync(fullPath);
          }
        } catch (error) {
          // Ignore individual file errors
        }
      }

      if (tempFiles.length > 0) {
        console.log(`✅ Cleaned up ${tempFiles.length} temporary test files`);
      }
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
