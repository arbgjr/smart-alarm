import { test, expect } from '@playwright/test';

test.describe('Basic Playwright Setup Test', () => {

  test('should verify Playwright is working with external site', async ({ page }) => {
    // Test with a reliable external site
    await page.goto('https://example.com');

    // Verify page loads
    await expect(page).toHaveTitle(/Example Domain/);

    // Take screenshot
    await page.screenshot({
      path: 'test-results/playwright-working.png',
      fullPage: true
    });
  });

  test('should test different viewport sizes', async ({ page }) => {
    await page.goto('https://example.com');

    // Test mobile viewport
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({
      path: 'test-results/example-mobile.png',
      fullPage: true
    });

    // Test desktop viewport
    await page.setViewportSize({ width: 1920, height: 1080 });
    await page.screenshot({
      path: 'test-results/example-desktop.png',
      fullPage: true
    });

    // Verify content is still visible
    await expect(page.locator('h1')).toBeVisible();
  });

});
