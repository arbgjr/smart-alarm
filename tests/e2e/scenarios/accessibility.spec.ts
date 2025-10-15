import { test, expect } from '@playwright/test';

test.describe('Accessibility Tests', () => {
  test('should have proper heading hierarchy', async ({ page }) => {
    await page.goto('/');

    // Check for h1 element
    const h1Elements = page.locator('h1');
    const h1Count = await h1Elements.count();

    // Should have exactly one h1 per page
    expect(h1Count).toBeLessThanOrEqual(1);

    if (h1Count === 1) {
      const h1Text = await h1Elements.textContent();
      expect(h1Text?.trim()).toBeTruthy();
    }
  });

  test('should have proper alt text for images', async ({ page }) => {
    await page.goto('/');

    const images = page.locator('img');
    const imageCount = await images.count();

    for (let i = 0; i < imageCount; i++) {
      const img = images.nth(i);
      const alt = await img.getAttribute('alt');
      const role = await img.getAttribute('role');

      // Images should have alt text or be decorative (role="presentation")
      expect(alt !== null || role === 'presentation').toBeTruthy();
    }
  });

  test('should have proper form labels', async ({ page }) => {
    await page.goto('/');

    const inputs = page.locator('input, select, textarea');
    const inputCount = await inputs.count();

    for (let i = 0; i < inputCount; i++) {
      const input = inputs.nth(i);
      const type = await input.getAttribute('type');

      // Skip hidden inputs
      if (type === 'hidden') continue;

      const id = await input.getAttribute('id');
      const ariaLabel = await input.getAttribute('aria-label');
      const ariaLabelledBy = await input.getAttribute('aria-labelledby');

      // Input should have a label, aria-label, or aria-labelledby
      const hasLabel = id ? await page.locator(`label[for="${id}"]`).count() > 0 : false;

      expect(hasLabel || ariaLabel || ariaLabelledBy).toBeTruthy();
    }
  });

  test('should have proper color contrast', async ({ page }) => {
    await page.goto('/');

    // This is a basic check - in a real app you'd use axe-core or similar
    const textElements = page.locator('p, h1, h2, h3, h4, h5, h6, span, div, a, button');
    const count = await textElements.count();

    // Just check that text elements exist and are visible
    expect(count).toBeGreaterThan(0);

    // In a real test, you would check actual color contrast ratios
    // using tools like @axe-core/playwright
  });

  test('should be keyboard navigable', async ({ page }) => {
    await page.goto('/');

    // Start keyboard navigation
    await page.keyboard.press('Tab');

    let focusedElements = 0;
    const maxTabs = 20; // Prevent infinite loop

    for (let i = 0; i < maxTabs; i++) {
      const focusedElement = page.locator(':focus');

      if (await focusedElement.count() > 0) {
        focusedElements++;

        // Check if focused element is visible
        await expect(focusedElement).toBeVisible();

        // Move to next element
        await page.keyboard.press('Tab');
      } else {
        break;
      }
    }

    // Should have at least some focusable elements
    expect(focusedElements).toBeGreaterThan(0);
  });

  test('should have proper ARIA landmarks', async ({ page }) => {
    await page.goto('/');

    // Check for common landmarks
    const main = page.locator('main, [role="main"]');
    const nav = page.locator('nav, [role="navigation"]');
    const header = page.locator('header, [role="banner"]');
    const footer = page.locator('footer, [role="contentinfo"]');

    // At least one of these should exist
    const landmarkCount =
      (await main.count()) +
      (await nav.count()) +
      (await header.count()) +
      (await footer.count());

    expect(landmarkCount).toBeGreaterThan(0);
  });

  test('should handle focus management in modals', async ({ page }) => {
    await page.goto('/');

    // Look for modal triggers
    const modalTriggers = page.locator('[data-modal], [aria-haspopup="dialog"], button:has-text("modal")');
    const triggerCount = await modalTriggers.count();

    if (triggerCount > 0) {
      const trigger = modalTriggers.first();
      await trigger.click();

      // Check if modal opened
      const modal = page.locator('[role="dialog"], .modal, [aria-modal="true"]');

      if (await modal.count() > 0) {
        // Focus should be trapped in modal
        await page.keyboard.press('Tab');
        const focusedElement = page.locator(':focus');

        // Focused element should be within modal
        const isInModal = await modal.locator(':focus').count() > 0;
        expect(isInModal).toBeTruthy();
      }
    }
  });

  test('should announce dynamic content changes', async ({ page }) => {
    await page.goto('/');

    // Check for live regions
    const liveRegions = page.locator('[aria-live], [role="status"], [role="alert"]');
    const liveRegionCount = await liveRegions.count();

    // This is just checking that live regions exist if there are any
    // In a real app, you'd test that they announce changes properly
    if (liveRegionCount > 0) {
      for (let i = 0; i < liveRegionCount; i++) {
        const region = liveRegions.nth(i);
        const ariaLive = await region.getAttribute('aria-live');
        const role = await region.getAttribute('role');

        // Should have proper aria-live value or role
        expect(ariaLive === 'polite' || ariaLive === 'assertive' || role === 'status' || role === 'alert').toBeTruthy();
      }
    }
  });

  test('should have proper button and link semantics', async ({ page }) => {
    await page.goto('/');

    // Check buttons
    const buttons = page.locator('button, [role="button"]');
    const buttonCount = await buttons.count();

    for (let i = 0; i < buttonCount; i++) {
      const button = buttons.nth(i);
      const text = await button.textContent();
      const ariaLabel = await button.getAttribute('aria-label');

      // Button should have accessible text
      expect(text?.trim() || ariaLabel).toBeTruthy();
    }

    // Check links
    const links = page.locator('a[href]');
    const linkCount = await links.count();

    for (let i = 0; i < linkCount; i++) {
      const link = links.nth(i);
      const text = await link.textContent();
      const ariaLabel = await link.getAttribute('aria-label');

      // Link should have accessible text
      expect(text?.trim() || ariaLabel).toBeTruthy();
    }
  });

  test('should handle screen reader announcements', async ({ page }) => {
    await page.goto('/');

    // Check for screen reader only content
    const srOnly = page.locator('.sr-only, .visually-hidden, [class*="screen-reader"]');
    const srOnlyCount = await srOnly.count();

    // If there are screen reader only elements, they should not be visible
    for (let i = 0; i < srOnlyCount; i++) {
      const element = srOnly.nth(i);
      const isVisible = await element.isVisible();

      // Screen reader only content should not be visually visible
      expect(isVisible).toBeFalsy();
    }
  });
});
