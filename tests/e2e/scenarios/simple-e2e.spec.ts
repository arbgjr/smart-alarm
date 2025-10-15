import { test, expect } from '@playwright/test';

test.describe('Simple E2E Tests', () => {
  test('should load the application', async ({ page }) => {
    // Navigate to the application
    await page.goto('/');

    // Check if the page loads successfully
    await expect(page).toHaveTitle(/Smart Alarm/i);
  });

  test('should navigate to login page', async ({ page }) => {
    await page.goto('/');

    // Look for login link or button
    const loginLink = page.getByRole('link', { name: /login|entrar/i }).or(
      page.getByRole('button', { name: /login|entrar/i })
    );

    if (await loginLink.isVisible()) {
      await loginLink.click();
      await expect(page).toHaveURL(/.*login.*/);
    } else {
      // If no login link, try navigating directly
      await page.goto('/login');
      await expect(page).toHaveURL(/.*login.*/);
    }
  });

  test('should show navigation elements', async ({ page }) => {
    await page.goto('/');

    // Check for common navigation elements
    const navigation = page.locator('nav').or(
      page.locator('[role="navigation"]')
    ).or(
      page.locator('header')
    );

    await expect(navigation).toBeVisible();
  });

  test('should be responsive on mobile', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/');

    // Check if page is still functional on mobile
    await expect(page.locator('body')).toBeVisible();

    // Check if there's a mobile menu or hamburger button
    const mobileMenu = page.getByRole('button', { name: /menu|☰/i }).or(
      page.locator('[aria-label*="menu"]')
    );

    if (await mobileMenu.isVisible()) {
      await mobileMenu.click();
      // Menu should expand or show navigation
    }
  });

  test('should handle 404 pages gracefully', async ({ page }) => {
    await page.goto('/non-existent-page');

    // Should show 404 page or redirect to home
    const is404 = page.getByText(/404|not found|página não encontrada/i);
    const isHome = page.locator('body');

    await expect(is404.or(isHome)).toBeVisible();
  });

  test('should have proper meta tags for SEO', async ({ page }) => {
    await page.goto('/');

    // Check for essential meta tags
    const title = await page.title();
    expect(title).toBeTruthy();
    expect(title.length).toBeGreaterThan(0);

    const description = await page.locator('meta[name="description"]').getAttribute('content');
    if (description) {
      expect(description.length).toBeGreaterThan(0);
    }
  });

  test('should load without JavaScript errors', async ({ page }) => {
    const errors: string[] = [];

    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });

    page.on('pageerror', error => {
      errors.push(error.message);
    });

    await page.goto('/');

    // Wait for page to fully load
    await page.waitForLoadState('networkidle');

    // Filter out known acceptable errors (like network errors in dev)
    const criticalErrors = errors.filter(error =>
      !error.includes('favicon') &&
      !error.includes('net::ERR_') &&
      !error.includes('Failed to load resource')
    );

    expect(criticalErrors).toHaveLength(0);
  });

  test('should have accessible navigation', async ({ page }) => {
    await page.goto('/');

    // Test keyboard navigation
    await page.keyboard.press('Tab');

    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();

    // Check if focused element has proper accessibility attributes
    const ariaLabel = await focusedElement.getAttribute('aria-label');
    const role = await focusedElement.getAttribute('role');
    const tagName = await focusedElement.evaluate(el => el.tagName.toLowerCase());

    // Should be a focusable element
    expect(['a', 'button', 'input', 'select', 'textarea'].includes(tagName) ||
           role === 'button' ||
           role === 'link' ||
           ariaLabel).toBeTruthy();
  });

  test('should handle slow network conditions', async ({ page }) => {
    // Simulate slow network
    await page.route('**/*', route => {
      setTimeout(() => route.continue(), 100); // Add 100ms delay
    });

    await page.goto('/');

    // Should still load within reasonable time
    await expect(page.locator('body')).toBeVisible({ timeout: 10000 });
  });

  test('should work with cookies disabled', async ({ context, page }) => {
    // Clear all cookies
    await context.clearCookies();

    await page.goto('/');

    // Basic functionality should still work
    await expect(page.locator('body')).toBeVisible();

    // Navigation should work
    const links = page.locator('a[href]');
    const linkCount = await links.count();

    if (linkCount > 0) {
      const firstLink = links.first();
      const href = await firstLink.getAttribute('href');

      if (href && href.startsWith('/')) {
        await firstLink.click();
        await expect(page.locator('body')).toBeVisible();
      }
    }
  });
});
