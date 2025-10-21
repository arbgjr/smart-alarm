import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login');

  // Check if we're already logged in
  const isLoggedIn = await page.locator('[data-testid="user-menu"]').isVisible().catch(() => false);

  if (isLoggedIn) {
    console.log('Already authenticated, skipping login');
    await page.context().storageState({ path: authFile });
    return;
  }

  // Fill login form
  await page.fill('[data-testid="email-input"]', 'test@example.com');
  await page.fill('[data-testid="password-input"]', 'TestPassword123!');

  // Click login button
  await page.click('[data-testid="login-button"]');

  // Wait for successful login
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible({ timeout: 10000 });

  // Save authentication state
  await page.context().storageState({ path: authFile });
});
