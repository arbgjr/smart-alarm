import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';

test.describe('PWA Functionality', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('test@example.com', 'password123');
  });

  test('should display PWA install prompt', async ({ page, context }) => {
    // Mock beforeinstallprompt event
    await page.addInitScript(() => {
      // Simulate PWA install prompt
      setTimeout(() => {
        const event = new Event('beforeinstallprompt');
        (event as any).prompt = () => Promise.resolve();
        (event as any).userChoice = Promise.resolve({ outcome: 'accepted' });
        window.dispatchEvent(event);
      }, 1000);
    });

    // Navigate to dashboard
    await page.goto('/dashboard');

    // Check that install prompt appears
    await expect(page.locator('text=Install Smart Alarm')).toBeVisible({ timeout: 5000 });

    // Check prompt content
    await expect(page.locator('text=Offline access to your alarms')).toBeVisible();
    await expect(page.locator('text=Native app experience')).toBeVisible();
    await expect(page.locator('text=Faster loading and performance')).toBeVisible();
  });

  test('should handle PWA installation flow', async ({ page }) => {
    // Mock PWA installation
    await page.addInitScript(() => {
      let installPrompt: any = null;

      // Mock beforeinstallprompt
      setTimeout(() => {
        const event = new Event('beforeinstallprompt');
        (event as any).prompt = () => {
          return Promise.resolve();
        };
        (event as any).userChoice = Promise.resolve({ outcome: 'accepted' });
        installPrompt = event;
        window.dispatchEvent(event);
      }, 1000);

      // Mock successful installation
      setTimeout(() => {
        window.dispatchEvent(new Event('appinstalled'));
      }, 3000);
    });

    await page.goto('/dashboard');

    // Wait for install prompt
    await expect(page.locator('text=Install Smart Alarm')).toBeVisible({ timeout: 5000 });

    // Click install button
    await page.locator('[data-testid="install-app-button"]').click();

    // Check for permissions step
    await expect(page.locator('text=App Installed Successfully!')).toBeVisible({ timeout: 5000 });

    // Enable notifications
    await page.locator('[data-testid="enable-notifications-button"]').click();

    // Check for completion step
    await expect(page.locator('text=You\'re All Set!')).toBeVisible({ timeout: 5000 });
  });

  test('should show offline ready notification', async ({ page }) => {
    // Mock service worker registration and offline ready state
    await page.addInitScript(() => {
      // Mock service worker
      Object.defineProperty(navigator, 'serviceWorker', {
        value: {
          register: () => Promise.resolve({
            installing: null,
            waiting: null,
            active: { state: 'activated' },
            addEventListener: () => {},
            removeEventListener: () => {}
          }),
          ready: Promise.resolve({
            installing: null,
            waiting: null,
            active: { state: 'activated' }
          })
        }
      });
    });

    await page.goto('/dashboard');

    // Check for offline ready notification
    await expect(page.locator('text=Ready for Offline Use')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('text=Smart Alarm is now available offline!')).toBeVisible();
  });

  test('should show update available notification', async ({ page }) => {
    // Mock service worker update
    await page.addInitScript(() => {
      setTimeout(() => {
        // Simulate update available
        const event = new CustomEvent('sw-update-available');
        window.dispatchEvent(event);
      }, 2000);
    });

    await page.goto('/dashboard');

    // Check for update notification
    await expect(page.locator('text=Update Available')).toBeVisible({ timeout: 5000 });
    await expect(page.locator('text=A new version of Smart Alarm is ready to install')).toBeVisible();

    // Click update button
    await page.locator('[data-testid="update-now-button"]').click();

    // Should trigger update process
    await page.waitForTimeout(1000);
  });

  test('should handle offline state', async ({ page, context }) => {
    await page.goto('/dashboard');

    // Go offline
    await context.setOffline(true);

    // Check offline indicator
    await expect(page.locator('[data-testid="offline-indicator"]')).toBeVisible({ timeout: 5000 });

    // Try to perform an action that requires network
    await page.locator('[data-testid="refresh-button"]').click();

    // Should show offline message or cached data
    await expect(page.locator('text=You are currently offline')).toBeVisible();

    // Go back online
    await context.setOffline(false);

    // Check online indicator
    await expect(page.locator('[data-testid="online-indicator"]')).toBeVisible({ timeout: 5000 });
  });

  test('should display PWA status component', async ({ page }) => {
    await page.goto('/dashboard');

    // Check that PWA status is visible
    await expect(page.locator('[data-testid="pwa-status"]')).toBeVisible();

    // Check status indicators
    await expect(page.locator('[data-testid="connection-status"]')).toBeVisible();
    await expect(page.locator('[data-testid="notification-status"]')).toBeVisible();

    // Check capabilities
    await expect(page.locator('text=Push Support')).toBeVisible();
    await expect(page.locator('text=Background Sync')).toBeVisible();
  });

  test('should handle notification permissions', async ({ page, context }) => {
    // Grant notification permission
    await context.grantPermissions(['notifications']);

    await page.goto('/dashboard');

    // Check that notification status shows enabled
    const notificationStatus = page.locator('[data-testid="notification-status"]');
    await expect(notificationStatus.locator('text=Enabled')).toBeVisible();

    // Test notification
    await page.locator('[data-testid="test-notification-button"]').click();

    // Should show notification (can't easily test actual notification, but can test the call)
    await page.waitForTimeout(1000);
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

    await page.goto('/dashboard');

    // Check that PWA-specific UI is shown
    await expect(page.locator('[data-testid="pwa-installed-indicator"]')).toBeVisible();
  });

  test('should handle background sync', async ({ page }) => {
    // Mock background sync capability
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

    await page.goto('/dashboard');

    // Trigger an action that should use background sync
    await page.locator('[data-testid="create-alarm-button"]').click();

    // Fill and submit form
    await page.locator('[data-testid="alarm-name-input"]').fill('Background Sync Test');
    await page.locator('[data-testid="alarm-time-input"]').fill('08:00');
    await page.locator('[data-testid="save-alarm-button"]').click();

    // Should handle the sync in the background
    await page.waitForTimeout(1000);
  });

  test('should be responsive across different devices', async ({ page }) => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');

    // Check mobile layout
    await expect(page.locator('[data-testid="mobile-layout"]')).toBeVisible();

    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.reload();

    // Check tablet layout
    await expect(page.locator('[data-testid="tablet-layout"]')).toBeVisible();

    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.reload();

    // Check desktop layout
    await expect(page.locator('[data-testid="desktop-layout"]')).toBeVisible();
  });

  test('should handle touch interactions', async ({ page }) => {
    // Set mobile viewport with touch
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');

    // Test touch interactions
    const alarmCard = page.locator('[data-testid="alarm-card"]').first();

    if (await alarmCard.isVisible()) {
      // Test tap
      await alarmCard.tap();

      // Test swipe gesture (if implemented)
      await alarmCard.hover();
      await page.mouse.down();
      await page.mouse.move(100, 0);
      await page.mouse.up();
    }
  });

  test('should handle orientation changes', async ({ page }) => {
    // Start in portrait
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');

    // Check portrait layout
    await expect(page.locator('[data-testid="portrait-layout"]')).toBeVisible();

    // Change to landscape
    await page.setViewportSize({ width: 667, height: 375 });

    // Check landscape layout
    await expect(page.locator('[data-testid="landscape-layout"]')).toBeVisible();
  });

  test('should cache resources for offline use', async ({ page, context }) => {
    await page.goto('/dashboard');

    // Wait for service worker to cache resources
    await page.waitForTimeout(2000);

    // Go offline
    await context.setOffline(true);

    // Navigate to different pages
    await page.goto('/alarms');

    // Should still load from cache
    await expect(page.locator('text=Alarm Management')).toBeVisible();

    // Navigate to settings
    await page.goto('/settings');

    // Should still load from cache
    await expect(page.locator('text=Settings')).toBeVisible();
  });
});
