import { FullConfig } from '@playwright/test';
import os from 'os';
import fs from 'fs';
import path from 'path';

async function globalTeardown(config: FullConfig) {
  console.log('🧹 Starting E2E test teardown (simplified)...');

  try {
    // Clean up test files
    await cleanupTestFiles();

    console.log('✅ E2E test teardown completed successfully');

  } catch (error) {
    console.error('❌ E2E test teardown failed:', error);
    // Don't throw error in teardown to avoid masking test failures
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

        for (const file of files.slice(0, 50)) { // Limit to 50 files
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
      if (fs.existsSync(tempDir)) {
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
      }
    } catch (error) {
      console.warn('⚠️  Could not clean up temporary files:', error);
    }

  } catch (error) {
    console.error('Failed to cleanup test files:', error);
  }
}

export default globalTeardown;
