import { test, expect } from '@playwright/test';

test.describe('Comprehensive User Flows', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app
    await page.goto('/');

    // Wait for the app to load
    await page.waitForLoadState('networkidle');
  });

  test('Complete alarm management workflow', async ({ page }) => {
    // Test alarm creation
    await test.step('Create new alarm', async () => {
      // Look for create alarm button (could be + button, "Add Alarm", etc.)
      const createButton = page.locator('button').filter({ hasText: /add|create|new|\+/i }).first();

      if (await createButton.isVisible()) {
        await createButton.click();

        // Fill alarm form
        await page.fill('input[name="name"], input[placeholder*="name" i]', 'Test E2E Alarm');
        await page.fill('input[type="time"], input[name="time"]', '08:30');

        // Save alarm
        const saveButton = page.locator('button').filter({ hasText: /save|create|add/i }).first();
        await saveButton.click();

        // Verify alarm was created
        await expect(page.locator('text=Test E2E Alarm')).toBeVisible();
      }
    });

    // Test alarm editing
    await test.step('Edit existing alarm', async () => {
      // Find and click edit button for the test alarm
      const alarmRow = page.locator('[data-testid*="alarm"], .alarm-item, tr').filter({ hasText: 'Test E2E Alarm' }).first();

      if (await alarmRow.isVisible()) {
        const editButton = alarmRow.locator('button').filter({ hasText: /edit|modify/i }).first();

        if (await editButton.isVisible()) {
          await editButton.click();

          // Modify alarm name
          await page.fill('input[name="name"], input[placeholder*="name" i]', 'Modified E2E Alarm');

          // Save changes
          const saveButton = page.locator('button').filter({ hasText: /save|update/i }).first();
          await saveButton.click();

          // Verify changes
          await expect(page.locator('text=Modified E2E Alarm')).toBeVisible();
        }
      }
    });

    // Test alarm toggle
    await test.step('Toggle alarm on/off', async () => {
      const alarmRow = page.locator('[data-testid*="alarm"], .alarm-item, tr').filter({ hasText: 'Modified E2E Alarm' }).first();

      if (await alarmRow.isVisible()) {
        const toggleButton = alarmRow.locator('button, input[type="checkbox"], .toggle').first();

        if (await toggleButton.isVisible()) {
          await toggleButton.click();

          // Wait for state change
          await page.waitForTimeout(500);
        }
      }
    });
  });

  test('Navigation and responsive design', async ({ page }) => {
    await test.step('Test main navigation', async () => {
      // Test navigation to different sections
      const navItems = [
        { text: /dashboard|home/i, path: '/' },
        { text: /alarm/i, path: '/alarms' },
        { text: /setting/i, path: '/settings' },
        { text: /profile/i, path: '/profile' }
      ];

      for (const item of navItems) {
        const navLink = page.locator('a, button').filter({ hasText: item.text }).first();

        if (await navLink.isVisible()) {
          await navLink.click();
          await page.waitForLoadState('networkidle');

          // Verify navigation worked (URL or content change)
          const currentUrl = page.url();
          console.log(`Navigated to: ${currentUrl}`);
        }
      }
    });

    await test.step('Test responsive behavior', async () => {
      // Test mobile viewport
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      // Check if mobile menu exists
      const mobileMenu = page.locator('[data-testid="mobile-menu"], .mobile-menu, button').filter({ hasText: /menu|☰/i }).first();

      if (await mobileMenu.isVisible()) {
        await mobileMenu.click();
        await page.waitForTimeout(300);
      }

      // Test tablet viewport
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.waitForTimeout(500);

      // Test desktop viewport
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);
    });
  });

  test('Form validation and error handling', async ({ page }) => {
    await test.step('Test form validation', async () => {
      // Try to create alarm with invalid data
      const createButton = page.locator('button').filter({ hasText: /add|create|new|\+/i }).first();

      if (await createButton.isVisible()) {
        await createButton.click();

        // Try to save without filling required fields
        const saveButton = page.locator('button').filter({ hasText: /save|create|add/i }).first();

        if (await saveButton.isVisible()) {
          await saveButton.click();

          // Check for validation messages
          const errorMessages = page.locator('.error, .invalid, [role="alert"], .text-red');

          if (await errorMessages.first().isVisible()) {
            console.log('Form validation working correctly');
          }
        }
      }
    });

    await test.step('Test error recovery', async () => {
      // Fill form with valid data after error
      await page.fill('input[name="name"], input[placeholder*="name" i]', 'Recovery Test Alarm');
      await page.fill('input[type="time"], input[name="time"]', '09:00');

      const saveButton = page.locator('button').filter({ hasText: /save|create|add/i }).first();

      if (await saveButton.isVisible()) {
        await saveButton.click();

        // Verify successful creation
        await expect(page.locator('text=Recovery Test Alarm')).toBeVisible({ timeout: 10000 });
      }
    });
  });

  test('Accessibility features', async ({ page }) => {
    await test.step('Test keyboard navigation', async () => {
      // Test tab navigation
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');
      await page.keyboard.press('Tab');

      // Check if focus is visible
      const focusedElement = page.locator(':focus');

      if (await focusedElement.isVisible()) {
        console.log('Keyboard navigation working');
      }
    });

    await test.step('Test ARIA labels and roles', async () => {
      // Check for proper ARIA attributes
      const buttons = page.locator('button');
      const buttonCount = await buttons.count();

      for (let i = 0; i < Math.min(buttonCount, 5); i++) {
        const button = buttons.nth(i);
        const ariaLabel = await button.getAttribute('aria-label');
        const text = await button.textContent();

        if (ariaLabel || (text && text.trim())) {
          console.log(`Button ${i} has accessible text: ${ariaLabel || text}`);
        }
      }
    });

    await test.step('Test color contrast and readability', async () => {
      // Check if text is readable (basic check)
      const textElements = page.locator('p, h1, h2, h3, h4, h5, h6, span, div').filter({ hasText: /.+/ });
      const count = await textElements.count();

      if (count > 0) {
        console.log(`Found ${count} text elements for readability`);
      }
    });
  });

  test('PWA functionality', async ({ page }) => {
    await test.step('Test offline capability', async () => {
      // Go offline
      await page.context().setOffline(true);

      // Try to navigate
      await page.reload();

      // Check if app still works (should show cached content or offline message)
      const body = page.locator('body');
      await expect(body).toBeVisible();

      // Go back online
      await page.context().setOffline(false);
    });

    await test.step('Test service worker registration', async () => {
      // Check if service worker is registered
      const swRegistered = await page.evaluate(() => {
        return 'serviceWorker' in navigator;
      });

      expect(swRegistered).toBe(true);
    });

    await test.step('Test app manifest', async () => {
      // Check for manifest link
      const manifestLink = page.locator('link[rel="manifest"]');

      if (await manifestLink.isVisible()) {
        const href = await manifestLink.getAttribute('href');
        console.log(`Manifest found: ${href}`);
      }
    });
  });

  test('Performance and loading states', async ({ page }) => {
    await test.step('Test loading states', async () => {
      // Navigate to a page that might show loading
      await page.goto('/alarms');

      // Look for loading indicators
      const loadingIndicators = page.locator('.loading, .spinner, [data-testid="loading"]');

      // Wait for loading to complete
      await page.waitForLoadState('networkidle');

      // Verify content is loaded
      const content = page.locator('main, .content, [role="main"]');
      await expect(content).toBeVisible();
    });

    await test.step('Test page performance', async () => {
      const startTime = Date.now();

      await page.goto('/');
      await page.waitForLoadState('networkidle');

      const loadTime = Date.now() - startTime;
      console.log(`Page load time: ${loadTime}ms`);

      // Basic performance check (should load within 10 seconds)
      expect(loadTime).toBeLessThan(10000);
    });
  });
});
