import { test, expect } from '@playwright/test';

test.describe('Visual Regression Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Ensure consistent viewport for visual tests
    await page.setViewportSize({ width: 1280, height: 720 });
  });

  test('dashboard page should match visual baseline', async ({ page }) => {
    await page.goto('/dashboard');

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');

    // Hide dynamic content that changes between runs
    await page.addStyleTag({
      content: `
        [data-testid="current-time"],
        [data-testid="last-updated"],
        .timestamp {
          visibility: hidden !important;
        }
      `
    });

    // Take screenshot
    await expect(page).toHaveScreenshot('dashboard-page.png');
  });

  test('alarms list should match visual baseline', async ({ page }) => {
    await page.goto('/alarms');

    // Wait for alarms to load
    await page.waitForSelector('[data-testid="alarms-list"]');
    await page.waitForLoadState('networkidle');

    // Hide dynamic timestamps
    await page.addStyleTag({
      content: `
        .timestamp,
        [data-testid="last-triggered"],
        [data-testid="next-trigger"] {
          visibility: hidden !important;
        }
      `
    });

    await expect(page).toHaveScreenshot('alarms-list.png');
  });

  test('create alarm form should match visual baseline', async ({ page }) => {
    await page.goto('/alarms/create');

    // Wait for form to load
    await page.waitForSelector('[data-testid="alarm-form"]');

    await expect(page).toHaveScreenshot('create-alarm-form.png');
  });

  test('login page should match visual baseline', async ({ page }) => {
    await page.goto('/login');

    // Wait for form to load
    await page.waitForSelector('[data-testid="login-form"]');

    await expect(page).toHaveScreenshot('login-page.png');
  });

  test('mobile dashboard should match visual baseline', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Hide dynamic content
    await page.addStyleTag({
      content: `
        [data-testid="current-time"],
        [data-testid="last-updated"],
        .timestamp {
          visibility: hidden !important;
        }
      `
    });

    await expect(page).toHaveScreenshot('mobile-dashboard.png');
  });

  test('tablet alarms view should match visual baseline', async ({ page }) => {
    // Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });

    await page.goto('/alarms');
    await page.waitForSelector('[data-testid="alarms-list"]');
    await page.waitForLoadState('networkidle');

    // Hide dynamic content
    await page.addStyleTag({
      content: `
        .timestamp,
        [data-testid="last-triggered"],
        [data-testid="next-trigger"] {
          visibility: hidden !important;
        }
      `
    });

    await expect(page).toHaveScreenshot('tablet-alarms.png');
  });

  test('dark mode dashboard should match visual baseline', async ({ page }) => {
    // Enable dark mode
    await page.emulateMedia({ colorScheme: 'dark' });

    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Hide dynamic content
    await page.addStyleTag({
      content: `
        [data-testid="current-time"],
        [data-testid="last-updated"],
        .timestamp {
          visibility: hidden !important;
        }
      `
    });

    await expect(page).toHaveScreenshot('dark-mode-dashboard.png');
  });

  test('error states should match visual baseline', async ({ page }) => {
    await page.goto('/alarms/create');

    // Trigger validation errors
    await page.click('[data-testid="save-alarm-button"]');

    // Wait for error messages to appear
    await page.waitForSelector('[data-testid="error-message"]', { timeout: 5000 });

    await expect(page).toHaveScreenshot('form-validation-errors.png');
  });

  test('loading states should match visual baseline', async ({ page }) => {
    // Intercept API calls to simulate loading
    await page.route('**/api/alarms', route => {
      // Delay response to capture loading state
      setTimeout(() => {
        route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify([])
        });
      }, 2000);
    });

    await page.goto('/alarms');

    // Capture loading state
    await page.waitForSelector('[data-testid="loading-spinner"]', { timeout: 1000 });
    await expect(page).toHaveScreenshot('loading-state.png');
  });

  test('modal dialogs should match visual baseline', async ({ page }) => {
    await page.goto('/alarms');

    // Open delete confirmation modal (if it exists)
    const deleteButton = page.locator('[data-testid="delete-alarm-button"]').first();
    if (await deleteButton.isVisible()) {
      await deleteButton.click();

      // Wait for modal to appear
      await page.waitForSelector('[data-testid="confirm-dialog"]');

      await expect(page).toHaveScreenshot('delete-confirmation-modal.png');
    }
  });

  test('empty states should match visual baseline', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/alarms', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    await page.goto('/alarms');
    await page.waitForLoadState('networkidle');

    // Wait for empty state to appear
    await page.waitForSelector('[data-testid="empty-state"]', { timeout: 5000 });

    await expect(page).toHaveScreenshot('empty-alarms-state.png');
  });
});
