import { test, expect } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

test.describe('Accessibility Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the page under test
    await page.goto('/dashboard');
  });

  test('should not have any automatically detectable accessibility issues', async ({ page }) => {
    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });

  test('should have proper heading hierarchy', async ({ page }) => {
    // Check that headings follow proper hierarchy (h1 -> h2 -> h3, etc.)
    const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();

    let previousLevel = 0;
    for (const heading of headings) {
      const tagName = await heading.evaluate(el => el.tagName.toLowerCase());
      const currentLevel = parseInt(tagName.charAt(1));

      // First heading should be h1, subsequent headings shouldn't skip levels
      if (previousLevel === 0) {
        expect(currentLevel).toBe(1);
      } else {
        expect(currentLevel).toBeLessThanOrEqual(previousLevel + 1);
      }

      previousLevel = currentLevel;
    }
  });

  test('should have proper form labels', async ({ page }) => {
    await page.goto('/alarms/create');

    // Check that all form inputs have associated labels
    const inputs = await page.locator('input, select, textarea').all();

    for (const input of inputs) {
      const id = await input.getAttribute('id');
      const ariaLabel = await input.getAttribute('aria-label');
      const ariaLabelledBy = await input.getAttribute('aria-labelledby');

      if (id) {
        // Check for associated label
        const label = page.locator(`label[for="${id}"]`);
        const hasLabel = await label.count() > 0;

        expect(hasLabel || ariaLabel || ariaLabelledBy).toBeTruthy();
      } else {
        // Input should have aria-label or aria-labelledby
        expect(ariaLabel || ariaLabelledBy).toBeTruthy();
      }
    }
  });

  test('should have proper button accessibility', async ({ page }) => {
    // Check that all buttons have accessible names
    const buttons = await page.locator('button, [role="button"]').all();

    for (const button of buttons) {
      const text = await button.textContent();
      const ariaLabel = await button.getAttribute('aria-label');
      const ariaLabelledBy = await button.getAttribute('aria-labelledby');
      const title = await button.getAttribute('title');

      // Button should have accessible name
      expect(text?.trim() || ariaLabel || ariaLabelledBy || title).toBeTruthy();
    }
  });

  test('should support keyboard navigation', async ({ page }) => {
    // Test tab navigation
    await page.keyboard.press('Tab');

    let focusedElement = await page.locator(':focus').first();
    await expect(focusedElement).toBeVisible();

    // Continue tabbing through interactive elements
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab');
      focusedElement = await page.locator(':focus').first();

      // Focused element should be visible and interactive
      await expect(focusedElement).toBeVisible();

      const tagName = await focusedElement.evaluate(el => el.tagName.toLowerCase());
      const role = await focusedElement.getAttribute('role');
      const tabIndex = await focusedElement.getAttribute('tabindex');

      // Should be a focusable element
      const isFocusable = ['a', 'button', 'input', 'select', 'textarea'].includes(tagName) ||
                         role === 'button' || role === 'link' ||
                         (tabIndex && parseInt(tabIndex) >= 0);

      expect(isFocusable).toBeTruthy();
    }
  });

  test('should have proper color contrast', async ({ page }) => {
    // Run axe-core with color contrast rules
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa', 'wcag21aa'])
      .analyze();

    // Filter for color contrast violations
    const colorContrastViolations = accessibilityScanResults.violations.filter(
      violation => violation.id === 'color-contrast'
    );

    expect(colorContrastViolations).toEqual([]);
  });

  test('should have proper ARIA attributes', async ({ page }) => {
    // Check for proper ARIA usage
    const accessibilityScanResults = await new AxeBuilder({ page })
      .withTags(['wcag2a', 'wcag2aa'])
      .analyze();

    // Filter for ARIA-related violations
    const ariaViolations = accessibilityScanResults.violations.filter(
      violation => violation.id.includes('aria')
    );

    expect(ariaViolations).toEqual([]);
  });

  test('should support screen reader navigation', async ({ page }) => {
    // Check for landmarks
    const landmarks = await page.locator('[role="main"], [role="navigation"], [role="banner"], [role="contentinfo"], main, nav, header, footer').count();
    expect(landmarks).toBeGreaterThan(0);

    // Check for skip links
    const skipLinks = await page.locator('a[href^="#"]').first();
    if (await skipLinks.count() > 0) {
      const href = await skipLinks.getAttribute('href');
      const target = page.locator(href!);
      await expect(target).toBeVisible();
    }
  });

  test('should handle focus management in modals', async ({ page }) => {
    // Navigate to a page that might have modals
    await page.goto('/alarms');

    // Try to open a modal (if create alarm opens a modal)
    const createButton = page.locator('[data-testid="create-alarm-button"]');
    if (await createButton.isVisible()) {
      await createButton.click();

      // Check if modal opened
      const modal = page.locator('[role="dialog"], [data-testid="modal"]');
      if (await modal.isVisible()) {
        // Focus should be trapped in modal
        const focusedElement = await page.locator(':focus').first();

        // Focused element should be within modal
        const isWithinModal = await modal.locator(':focus').count() > 0;
        expect(isWithinModal).toBeTruthy();

        // Escape key should close modal
        await page.keyboard.press('Escape');
        await expect(modal).not.toBeVisible();
      }
    }
  });

  test('should provide proper error messages', async ({ page }) => {
    await page.goto('/alarms/create');

    // Submit form without filling required fields
    const submitButton = page.locator('[data-testid="save-alarm-button"]');
    if (await submitButton.isVisible()) {
      await submitButton.click();

      // Check for error messages
      const errorMessages = await page.locator('[role="alert"], .error, [data-testid*="error"]').all();

      for (const error of errorMessages) {
        // Error should be visible and have text
        await expect(error).toBeVisible();
        const text = await error.textContent();
        expect(text?.trim()).toBeTruthy();
      }
    }
  });

  test('should support high contrast mode', async ({ page }) => {
    // Simulate high contrast mode
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.reload();

    // Run accessibility scan in high contrast mode
    const accessibilityScanResults = await new AxeBuilder({ page }).analyze();

    expect(accessibilityScanResults.violations).toEqual([]);
  });
});
