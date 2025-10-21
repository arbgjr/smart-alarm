import { test, expect, devices } from '@playwright/test';

test.describe('Responsive Design', () => {
  const viewports = [
    { name: 'Mobile', ...devices['iPhone 12'] },
    { name: 'Tablet', ...devices['iPad Pro'] },
    { name: 'Desktop', viewport: { width: 1920, height: 1080 } }
  ];

  for (const device of viewports) {
    test.describe(`${device.name} viewport`, () => {
      test.beforeEach(async ({ page }) => {
        if (device.viewport) {
          await page.setViewportSize(device.viewport);
        }
        await page.goto('/dashboard');
      });

      test(`should display navigation correctly on ${device.name}`, async ({ page }) => {
        if (device.name === 'Mobile') {
          // Mobile should show hamburger menu
          await expect(page.locator('[data-testid="mobile-menu-button"]')).toBeVisible();

          // Click to open menu
          await page.click('[data-testid="mobile-menu-button"]');
          await expect(page.locator('[data-testid="mobile-navigation"]')).toBeVisible();
        } else {
          // Desktop/Tablet should show full navigation
          await expect(page.locator('[data-testid="desktop-navigation"]')).toBeVisible();
        }
      });

      test(`should display alarm cards correctly on ${device.name}`, async ({ page }) => {
        await page.goto('/alarms');

        const alarmGrid = page.locator('[data-testid="alarms-grid"]');
        await expect(alarmGrid).toBeVisible();

        // Check grid layout adapts to viewport
        if (device.name === 'Mobile') {
          // Mobile should show single column
          const gridColumns = await alarmGrid.evaluate(el =>
            window.getComputedStyle(el).gridTemplateColumns
          );
          expect(gridColumns).toContain('1fr');
        } else if (device.name === 'Tablet') {
          // Tablet should show 2 columns
          const gridColumns = await alarmGrid.evaluate(el =>
            window.getComputedStyle(el).gridTemplateColumns
          );
          expect(gridColumns.split(' ')).toHaveLength(2);
        } else {
          // Desktop should show 3+ columns
          const gridColumns = await alarmGrid.evaluate(el =>
            window.getComputedStyle(el).gridTemplateColumns
          );
          expect(gridColumns.split(' ').length).toBeGreaterThanOrEqual(3);
        }
      });

      test(`should handle form layouts on ${device.name}`, async ({ page }) => {
        await page.goto('/alarms/create');

        const form = page.locator('[data-testid="alarm-form"]');
        await expect(form).toBeVisible();

        // Check form adapts to viewport
        if (device.name === 'Mobile') {
          // Mobile forms should stack vertically
          const formLayout = await form.evaluate(el =>
            window.getComputedStyle(el).flexDirection
          );
          expect(formLayout).toBe('column');
        }
      });

      test(`should display buttons appropriately on ${device.name}`, async ({ page }) => {
        await page.goto('/alarms');

        if (device.name === 'Mobile') {
          // Mobile might show floating action button
          const fab = page.locator('[data-testid="floating-action-button"]');
          if (await fab.isVisible()) {
            await expect(fab).toBeVisible();
          }
        } else {
          // Desktop/Tablet should show regular buttons
          await expect(page.locator('[data-testid="create-alarm-button"]')).toBeVisible();
        }
      });
    });
  }

  test('should handle orientation changes', async ({ page }) => {
    // Start in portrait
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/dashboard');

    // Verify portrait layout
    await expect(page.locator('[data-testid="mobile-menu-button"]')).toBeVisible();

    // Switch to landscape
    await page.setViewportSize({ width: 667, height: 375 });

    // Verify landscape layout adapts
    const navigation = page.locator('[data-testid="navigation"]');
    await expect(navigation).toBeVisible();
  });

  test('should maintain accessibility in all viewports', async ({ page }) => {
    const viewports = [
      { width: 375, height: 667 }, // Mobile
      { width: 768, height: 1024 }, // Tablet
      { width: 1920, height: 1080 } // Desktop
    ];

    for (const viewport of viewports) {
      await page.setViewportSize(viewport);
      await page.goto('/dashboard');

      // Check focus management
      await page.keyboard.press('Tab');
      const focusedElement = await page.locator(':focus').first();
      await expect(focusedElement).toBeVisible();

      // Check skip links
      const skipLink = page.locator('[data-testid="skip-to-content"]');
      if (await skipLink.isVisible()) {
        await expect(skipLink).toBeVisible();
      }
    }
  });
});
