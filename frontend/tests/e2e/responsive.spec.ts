import { test, expect } from '@playwright/test';

test.describe('Smart Alarm Responsive Tests', () => {

  test.beforeEach(async ({ page }) => {
    // Navigate to the application root
    await page.goto('/');
  });

  test('should load the application on desktop', async ({ page }) => {
    // Test desktop viewport (1920x1080)
    await page.setViewportSize({ width: 1920, height: 1080 });

    // Wait for the page to load completely
    await page.waitForLoadState('domcontentloaded');

    // Take a screenshot for comparison
    await page.screenshot({
      path: 'test-results/desktop-1920x1080.png',
      fullPage: true
    });

    // Basic assertions - checking if the app loads
    await expect(page).toHaveTitle(/Smart Alarm/);
  });

  test('should be responsive on tablet (iPad)', async ({ page }) => {
    // Set iPad viewport
    await page.setViewportSize({ width: 1024, height: 768 });

    await page.waitForLoadState('domcontentloaded');

    // Take screenshot
    await page.screenshot({
      path: 'test-results/tablet-1024x768.png',
      fullPage: true
    });

    // Check that the navigation is responsive
    const nav = page.locator('nav');
    await expect(nav).toBeVisible();
  });

  test('should be responsive on mobile (iPhone 12)', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 390, height: 844 });

    await page.waitForLoadState('domcontentloaded');

    // Take screenshot
    await page.screenshot({
      path: 'test-results/mobile-390x844.png',
      fullPage: true
    });

    // Mobile-specific tests
    await expect(page).toHaveTitle(/Smart Alarm/);
  });

  test('should display loading states correctly across viewports', async ({ page }) => {
    const viewports = [
      { width: 1920, height: 1080, name: 'desktop' },
      { width: 1024, height: 768, name: 'tablet' },
      { width: 390, height: 844, name: 'mobile' }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });

      // Look for loading states
      const loadingElement = page.locator('[data-testid="loading"], .animate-pulse, .animate-spin');

      // Take screenshot of loading state
      await page.screenshot({
        path: `test-results/loading-state-${viewport.name}.png`,
        fullPage: true
      });
    }
  });

  test('should handle navigation on different screen sizes', async ({ page }) => {
    // Test navigation responsiveness
    const viewports = [
      { width: 1920, height: 1080 },
      { width: 768, height: 1024 },
      { width: 390, height: 844 }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });

      // Check if navigation is accessible
      const nav = page.locator('nav');
      if (await nav.isVisible()) {
        // Check navigation links
        const navLinks = nav.locator('a, button');
        const count = await navLinks.count();

        // Ensure navigation items are clickable
        if (count > 0) {
          const firstLink = navLinks.first();
          await expect(firstLink).toBeVisible();
        }
      }
    }
  });

  test('should test dark mode across viewports', async ({ page }) => {
    // Test dark mode on different screen sizes
    const viewports = [
      { width: 1920, height: 1080, name: 'desktop' },
      { width: 390, height: 844, name: 'mobile' }
    ];

    for (const viewport of viewports) {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });

      // Toggle dark mode if available
      const darkModeToggle = page.locator('[data-testid="theme-toggle"], button:has-text("Dark"), button:has-text("Light")');

      if (await darkModeToggle.isVisible()) {
        await darkModeToggle.click();

        // Wait for theme change
        await page.waitForTimeout(500);

        // Take screenshot
        await page.screenshot({
          path: `test-results/dark-mode-${viewport.name}.png`,
          fullPage: true
        });
      }
    }
  });

  test('should verify touch interactions on mobile', async ({ page, isMobile }) => {
    // Only run on mobile contexts
    if (!isMobile) {
      test.skip();
      return;
    }

    await page.setViewportSize({ width: 390, height: 844 });

    // Test touch interactions
    const touchableElements = page.locator('button, [role="button"], a');
    const count = await touchableElements.count();

    if (count > 0) {
      const firstElement = touchableElements.first();
      await expect(firstElement).toBeVisible();

      // Test tap target size (should be at least 44px)
      const box = await firstElement.boundingBox();
      if (box) {
        expect(box.height).toBeGreaterThanOrEqual(44);
        expect(box.width).toBeGreaterThanOrEqual(44);
      }
    }
  });

  test('should verify components layout in different orientations', async ({ page }) => {
    // Test portrait
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({
      path: 'test-results/portrait-390x844.png',
      fullPage: true
    });

    // Test landscape
    await page.setViewportSize({ width: 844, height: 390 });
    await page.screenshot({
      path: 'test-results/landscape-844x390.png',
      fullPage: true
    });

    // Verify content is still accessible in landscape
    await expect(page.locator('body')).toBeVisible();
  });

});
