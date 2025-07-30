import { test, expect } from '@playwright/test';

/**
 * Smart Alarm Responsive Testing Suite
 *
 * This test suite validates the responsive design implementation
 * of the Smart Alarm application across different device types and screen sizes.
 *
 * Test Objectives:
 * 1. Verify responsive layout works on mobile, tablet, and desktop
 * 2. Test loading states display correctly across different viewports
 * 3. Validate navigation adapts to screen size
 * 4. Check touch interactions work on mobile devices
 * 5. Ensure components maintain usability across orientations
 * 6. Verify dark mode works consistently across viewports
 */

test.describe('Smart Alarm Responsive Design Tests', () => {

  const BASE_URL = 'http://localhost:5173';

  // Define test viewports
  const VIEWPORTS = {
    mobile: { width: 390, height: 844 },      // iPhone 12
    tablet: { width: 1024, height: 768 },     // iPad
    desktop: { width: 1920, height: 1080 },   // Desktop Full HD
    smallDesktop: { width: 1366, height: 768 }, // Laptop
  };

  test.beforeEach(async ({ page }) => {
    // Wait for application to be ready
    // Navigate to the application
  await page.goto('http://localhost:3000');
    await page.waitForLoadState('domcontentloaded');

    // Wait for React to hydrate
    await page.waitForSelector('[data-testid="app-container"], body', { timeout: 10000 });
  });

  test('should load application successfully on all viewport sizes', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Verify page loads
      await expect(page).toHaveTitle(/Smart Alarm/);

      // Take screenshot for visual regression testing
      await page.screenshot({
        path: `test-results/screenshots/app-load-${deviceName}-${viewport.width}x${viewport.height}.png`,
        fullPage: true
      });

      // Verify main content is visible
      const mainContent = page.locator('main, #root, [data-testid="app-container"]');
      await expect(mainContent).toBeVisible();
    }
  });

  test('should display navigation responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Check navigation existence
      const navigation = page.locator('nav, [data-testid="navigation"]');

      if (await navigation.isVisible()) {
        // Take screenshot of navigation
        await page.screenshot({
          path: `test-results/screenshots/navigation-${deviceName}.png`,
          clip: await navigation.boundingBox() || undefined
        });

        // Verify navigation items are accessible
        const navItems = navigation.locator('a, button[role="menuitem"]');
        const count = await navItems.count();

        if (count > 0) {
          // Check first nav item is clickable
          const firstNavItem = navItems.first();
          await expect(firstNavItem).toBeVisible();

          // Verify adequate touch target size on mobile
          if (deviceName === 'mobile') {
            const box = await firstNavItem.boundingBox();
            if (box) {
              expect(box.height).toBeGreaterThanOrEqual(44); // iOS HIG minimum
            }
          }
        }
      }
    }
  });

  test('should display loading states correctly across viewports', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for loading components we implemented
      const loadingElements = [
        '[data-testid="skeleton"]',
        '.animate-pulse',
        '.animate-spin',
        '[data-testid="loading-spinner"]',
        '[data-testid="loading-overlay"]'
      ];

      for (const selector of loadingElements) {
        const element = page.locator(selector);

        if (await element.isVisible()) {
          // Verify loading element is properly sized
          const box = await element.boundingBox();
          if (box) {
            expect(box.width).toBeGreaterThan(0);
            expect(box.height).toBeGreaterThan(0);
          }

          // Take screenshot
          await page.screenshot({
            path: `test-results/screenshots/loading-${selector.replace(/\[|\]|"|'/g, '')}-${deviceName}.png`,
            clip: box || undefined
          });
        }
      }
    }
  });

  test('should handle AlarmList component responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for AlarmList component
      const alarmList = page.locator('[data-testid="alarm-list"], .alarm-list');

      if (await alarmList.isVisible()) {
        // Verify list items are properly laid out
        const alarmItems = alarmList.locator('[data-testid="alarm-item"], .alarm-item');
        const itemCount = await alarmItems.count();

        if (itemCount > 0) {
          // Check first alarm item layout
          const firstItem = alarmItems.first();
          const box = await firstItem.boundingBox();

          if (box) {
            // Verify item takes reasonable width based on viewport
            if (deviceName === 'mobile') {
              expect(box.width).toBeLessThanOrEqual(viewport.width - 32); // Account for padding
            } else {
              expect(box.width).toBeGreaterThan(200); // Minimum readable width
            }
          }

          // Screenshot alarm list
          await page.screenshot({
            path: `test-results/screenshots/alarm-list-${deviceName}.png`,
            clip: await alarmList.boundingBox() || undefined
          });
        }
      }
    }
  });

  test('should handle RoutineList component responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for RoutineList component
      const routineList = page.locator('[data-testid="routine-list"], .routine-list');

      if (await routineList.isVisible()) {
        // Similar checks as AlarmList
        const routineItems = routineList.locator('[data-testid="routine-item"], .routine-item');
        const itemCount = await routineItems.count();

        if (itemCount > 0) {
          // Screenshot routine list
          await page.screenshot({
            path: `test-results/screenshots/routine-list-${deviceName}.png`,
            clip: await routineList.boundingBox() || undefined
          });
        }
      }
    }
  });

  test('should test empty states responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for empty state components we implemented
      const emptyStates = [
        '[data-testid="empty-alarm-state"]',
        '[data-testid="empty-routine-state"]',
        '[data-testid="empty-search-state"]',
        '[data-testid="loading-failed-state"]'
      ];

      for (const selector of emptyStates) {
        const element = page.locator(selector);

        if (await element.isVisible()) {
          // Verify empty state layout
          const box = await element.boundingBox();
          if (box) {
            // Empty states should be centered and readable
            expect(box.width).toBeGreaterThan(200);
            expect(box.height).toBeGreaterThan(100);
          }

          // Screenshot empty state
          await page.screenshot({
            path: `test-results/screenshots/empty-state-${selector.replace(/\[|\]|"|'/g, '')}-${deviceName}.png`,
            clip: box || undefined
          });
        }
      }
    }
  });

  test('should verify dark mode works across viewports', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for theme toggle
      const themeToggle = page.locator('[data-testid="theme-toggle"], button:has-text("Dark"), button:has-text("Light")');

      if (await themeToggle.isVisible()) {
        // Click theme toggle
        await themeToggle.click();

        // Wait for theme change
        await page.waitForTimeout(500);

        // Check if dark mode was applied
        const html = page.locator('html');
        const classList = await html.getAttribute('class');

        // Screenshot dark mode
        await page.screenshot({
          path: `test-results/screenshots/dark-mode-${deviceName}.png`,
          fullPage: true
        });

        // Toggle back to light mode for next test
        await themeToggle.click();
        await page.waitForTimeout(500);
      }
    }
  });

  test('should test orientation changes on mobile', async ({ page }) => {
    // Portrait mode (mobile default)
    await page.setViewportSize({ width: 390, height: 844 });

    await page.screenshot({
      path: 'test-results/screenshots/mobile-portrait.png',
      fullPage: true
    });

    // Landscape mode
    await page.setViewportSize({ width: 844, height: 390 });

    // Verify content is still accessible
    const mainContent = page.locator('main, #root, [data-testid="app-container"]');
    await expect(mainContent).toBeVisible();

    await page.screenshot({
      path: 'test-results/screenshots/mobile-landscape.png',
      fullPage: true
    });
  });

  test('should verify touch targets meet accessibility guidelines', async ({ page, isMobile }) => {
    if (!isMobile) {
      test.skip();
      return;
    }

    await page.setViewportSize(VIEWPORTS.mobile);

    // Find all interactive elements
    const interactiveElements = page.locator('button, a, [role="button"], [tabindex="0"]');
    const count = await interactiveElements.count();

    for (let i = 0; i < Math.min(count, 10); i++) { // Test first 10 elements
      const element = interactiveElements.nth(i);

      if (await element.isVisible()) {
        const box = await element.boundingBox();

        if (box) {
          // WCAG AA requires minimum 44x44px touch targets
          expect(box.width).toBeGreaterThanOrEqual(44);
          expect(box.height).toBeGreaterThanOrEqual(44);
        }
      }
    }
  });

  test('should test component showcase responsively', async ({ page }) => {
    // Look for component showcase (if exists)
    const showcase = page.locator('[data-testid="component-showcase"]');

    if (await showcase.isVisible()) {
      for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
        await page.setViewportSize(viewport);

        // Screenshot component showcase
        await page.screenshot({
          path: `test-results/screenshots/component-showcase-${deviceName}.png`,
          clip: await showcase.boundingBox() || undefined
        });
      }
    }
  });

});
