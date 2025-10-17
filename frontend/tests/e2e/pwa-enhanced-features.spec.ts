import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';

test.describe('Enhanced PWA Features', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);

    // Mock authentication
    await page.addInitScript(() => {
      localStorage.setItem('smart-alarm-auth', JSON.stringify({
        state: {
          token: 'mock-jwt-token',
          user: {
            id: '1',
            email: 'test@example.com',
            name: 'Test User'
          }
        }
      }));
    });

    await page.goto('/dashboard');
  });

  test('should display PWA status panel with comprehensive information', async ({ page }) => {
    // Navigate to settings to see PWA status
    await page.goto('/settings');

    // Check that PWA status panel is visible
    await expect(page.locator('[data-testid="pwa-status-panel"]')).toBeVisible();

    // Check status items
    await expect(page.locator('text=App Installation')).toBeVisible();
    await expect(page.locator('text=Offline Support')).toBeVisible();
    await expect(page.locator('text=Push Notifications')).toBeVisible();
    await expect(page.locator('text=Background Sync')).toBeVisible();

    // Check device information section
    await expect(page.locator('text=Device Information')).toBeVisible();
    await expect(page.locator('text=Device Type:')).toBeVisible();
    await expect(page.locator('text=Screen Size:')).toBeVisible();
    await expect(page.locator('text=Orientation:')).toBeVisible();
  });

  test('should show install prompt with enhanced UI', async ({ page }) => {
    // Mock beforeinstallprompt event
    await page.addInitScript(() => {
      setTimeout(() => {
        const event = new Event('beforeinstallprompt');
        (event as any).prompt = () => Promise.resolve();
        (event as any).userChoice = Promise.resolve({ outcome: 'accepted' });
        window.dispatchEvent(event);
      }, 1000);
    });

    // Check that enhanced install prompt appears
    await expect(page.locator('[data-testid="install-prompt"]')).toBeVisible({ timeout: 5000 });

    // Check prompt content
    await expect(page.locator('text=Install Smart Alarm')).toBeVisible();
    await expect(page.locator('text=Install the app for a better experience')).toBeVisible();

    // Check install button
    await expect(page.locator('button:has-text("Install")')).toBeVisible();

    // Check dismiss options
    await expect(page.locator('button:has-text("Not now")')).toBeVisible();
  });

  test('should display offline indicator when network is unavailable', async ({ page, context }) => {
    // Go offline
    await context.setOffline(true);

    // Check that offline indicator appears
    await expect(page.locator('[data-testid="offline-indicator"]')).toBeVisible({ timeout: 5000 });

    // Check offline message
    await expect(page.locator('text=You\'re offline')).toBeVisible();
    await expect(page.locator('text=Some features may be limited')).toBeVisible();

    // Check retry button
    await expect(page.locator('button:has-text("Retry")')).toBeVisible();

    // Go back online
    await context.setOffline(false);

    // Check online indicator
    await expect(page.locator('text=Back online!')).toBeVisible({ timeout: 5000 });
  });

  test('should handle PWA installation flow', async ({ page }) => {
    // Mock PWA capabilities
    await page.addInitScript(() => {
      // Mock beforeinstallprompt
      let installPrompt: any = null;

      setTimeout(() => {
        const event = new Event('beforeinstallprompt');
        (event as any).prompt = () => Promise.resolve();
        (event as any).userChoice = Promise.resolve({ outcome: 'accepted' });
        installPrompt = event;
        window.dispatchEvent(event);
      }, 1000);

      // Mock installation success
      setTimeout(() => {
        window.dispatchEvent(new Event('appinstalled'));
      }, 3000);
    });

    // Wait for install prompt
    await expect(page.locator('[data-testid="install-prompt"]')).toBeVisible({ timeout: 5000 });

    // Click install
    await page.locator('button:has-text("Install")').click();

    // Check loading state
    await expect(page.locator('text=Installing...')).toBeVisible({ timeout: 2000 });

    // Check success state
    await expect(page.locator('text=Installing...')).toBeHidden({ timeout: 5000 });
  });

  test('should show update notification when available', async ({ page }) => {
    // Mock service worker update
    await page.addInitScript(() => {
      setTimeout(() => {
        // Simulate update available
        window.dispatchEvent(new CustomEvent('sw-update-available'));
      }, 2000);
    });

    // Check for update notification
    await expect(page.locator('text=Update Available')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=A new version is ready to install')).toBeVisible();

    // Click update button
    await page.locator('button:has-text("Update Now")').click();

    // Should trigger update process
    await page.waitForTimeout(1000);
  });

  test('should handle notification permissions', async ({ page, context }) => {
    // Grant notification permission
    await context.grantPermissions(['notifications']);

    // Navigate to PWA status
    await page.goto('/settings');

    // Check notification status
    await expect(page.locator('text=Push Notifications')).toBeVisible();

    // Should show enabled status
    const notificationSection = page.locator('text=Push Notifications').locator('..');
    await expect(notificationSection.locator('[data-testid="status-success"]')).toBeVisible();
  });

  test('should display responsive layout components', async ({ page }) => {
    // Test ResponsiveLayout component
    await page.setViewportSize({ width: 375, height: 667 });

    // Check mobile layout
    await expect(page.locator('[data-testid="responsive-layout"]')).toBeVisible();

    // Test tablet layout
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(500);

    // Check tablet adaptations
    await expect(page.locator('[data-testid="responsive-layout"]')).toBeVisible();

    // Test desktop layout
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.waitForTimeout(500);

    // Check desktop layout
    await expect(page.locator('[data-testid="responsive-layout"]')).toBeVisible();
  });

  test('should handle offline actions banner', async ({ page, context }) => {
    // Mock offline actions
    await page.addInitScript(() => {
      localStorage.setItem('offline-actions', JSON.stringify([
        {
          id: '1',
          type: 'create-alarm',
          description: 'Create new alarm',
          timestamp: new Date().toISOString()
        },
        {
          id: '2',
          type: 'update-alarm',
          description: 'Update existing alarm',
          timestamp: new Date().toISOString()
        }
      ]));
    });

    // Go offline
    await context.setOffline(true);

    // Reload page to trigger offline state
    await page.reload();

    // Check offline actions banner
    await expect(page.locator('[data-testid="offline-actions-banner"]')).toBeVisible({ timeout: 5000 });

    // Check banner content
    await expect(page.locator('text=Offline Actions Pending')).toBeVisible();
    await expect(page.locator('text=2 actions that will be synced')).toBeVisible();

    // Check action items
    await expect(page.locator('text=Create new alarm')).toBeVisible();
    await expect(page.locator('text=Update existing alarm')).toBeVisible();

    // Test clear actions
    await page.locator('button:has-text("Clear All")').click();
    await expect(page.locator('[data-testid="offline-actions-banner"]')).toBeHidden();
  });

  test('should work in standalone mode', async ({ page }) => {
    // Mock standalone display mode
    await page.addInitScript(() => {
      Object.defineProperty(window, 'matchMedia', {
        value: (query: string) => ({
          matches: query === '(display-mode: standalone)',
          addEventListener: () => {},
          removeEventListener: () => {}
        })
      });
    });

    await page.reload();

    // Check PWA-specific indicators
    await expect(page.locator('[data-testid="pwa-installed-indicator"]')).toBeVisible();
  });

  test('should handle background sync capabilities', async ({ page }) => {
    // Mock background sync support
    await page.addInitScript(() => {
      Object.defineProperty(window, 'ServiceWorkerRegistration', {
        value: {
          prototype: {
            sync: {
              register: () => Promise.resolve()
            }
          }
        }
      });
    });

    // Navigate to PWA status
    await page.goto('/settings');

    // Check background sync status
    await expect(page.locator('text=Background Sync')).toBeVisible();

    // Should show supported status
    const syncSection = page.locator('text=Background Sync').locator('..');
    await expect(syncSection.locator('[data-testid="status-success"]')).toBeVisible();
  });

  test('should display device-specific icons and information', async ({ page }) => {
    // Test mobile device detection
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/settings');

    // Check mobile-specific elements
    await expect(page.locator('[data-testid="device-mobile-icon"]')).toBeVisible();

    // Test tablet device detection
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.reload();

    // Check tablet-specific elements
    await expect(page.locator('[data-testid="device-tablet-icon"]')).toBeVisible();

    // Test desktop device detection
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.reload();

    // Check desktop-specific elements
    await expect(page.locator('[data-testid="device-desktop-icon"]')).toBeVisible();
  });

  test('should handle PWA shortcuts', async ({ page }) => {
    // Mock PWA shortcuts (these would be defined in manifest.json)
    await page.addInitScript(() => {
      // Simulate shortcut navigation
      if (window.location.search.includes('action=create')) {
        // Mock shortcut behavior
        console.log('PWA shortcut activated');
      }
    });

    // Test shortcut URL
    await page.goto('/alarms?action=create');

    // Should handle shortcut appropriately
    await expect(page.locator('text=Create Alarm')).toBeVisible();
  });

  test('should cache resources for offline use', async ({ page, context }) => {
    // Navigate to different pages to trigger caching
    await page.goto('/dashboard');
    await page.waitForTimeout(1000);

    await page.goto('/alarms');
    await page.waitForTimeout(1000);

    await page.goto('/settings');
    await page.waitForTimeout(1000);

    // Go offline
    await context.setOffline(true);

    // Navigate back to cached pages
    await page.goto('/dashboard');
    await expect(page.locator('text=Smart Alarm')).toBeVisible();

    await page.goto('/alarms');
    await expect(page.locator('text=Alarm Management')).toBeVisible();
  });
});
