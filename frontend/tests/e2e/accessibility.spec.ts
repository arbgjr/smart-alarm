import { test, expect } from '@playwright/test';

test.describe('Accessibility Tests', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
  });

  test('Keyboard navigation', async ({ page }) => {
    await test.step('Tab navigation works correctly', async () => {
      // Start tabbing through the page
      const focusableElements = [];

      for (let i = 0; i < 10; i++) {
        await page.keyboard.press('Tab');

        const focusedElement = page.locator(':focus');

        if (await focusedElement.isVisible()) {
          const tagName = await focusedElement.evaluate(el => el.tagName.toLowerCase());
          const role = await focusedElement.getAttribute('role');
          const ariaLabel = await focusedElement.getAttribute('aria-label');
          const text = await focusedElement.textContent();

          focusableElements.push({
            tagName,
            role,
            ariaLabel,
            text: text?.trim().substring(0, 50)
          });
        }
      }

      console.log('Focusable elements found:', focusableElements);
      expect(focusableElements.length).toBeGreaterThan(0);
    });

    await test.step('Skip links work', async () => {
      // Look for skip links (usually hidden until focused)
      await page.keyboard.press('Tab');

      const skipLink = page.locator('a').filter({ hasText: /skip to|skip navigation/i }).first();

      if (await skipLink.isVisible()) {
        await skipLink.click();

        // Verify focus moved to main content
        const focusedElement = page.locator(':focus');
        const isInMainContent = await focusedElement.evaluate(el => {
          const main = document.querySelector('main, [role="main"], #main-content');
          return main ? main.contains(el) : false;
        });

        expect(isInMainContent).toBe(true);
      }
    });

    await test.step('Enter and Space keys activate buttons', async () => {
      const buttons = page.locator('button').first();

      if (await buttons.isVisible()) {
        await buttons.focus();

        // Test Enter key
        await page.keyboard.press('Enter');
        await page.waitForTimeout(500);

        // Test Space key
        await page.keyboard.press('Space');
        await page.waitForTimeout(500);
      }
    });
  });

  test('ARIA attributes and semantic HTML', async ({ page }) => {
    await test.step('Check for proper heading hierarchy', async () => {
      const headings = await page.locator('h1, h2, h3, h4, h5, h6').all();
      const headingLevels = [];

      for (const heading of headings) {
        const tagName = await heading.evaluate(el => el.tagName.toLowerCase());
        const text = await heading.textContent();
        headingLevels.push({ level: tagName, text: text?.trim() });
      }

      console.log('Heading hierarchy:', headingLevels);

      // Should have at least one h1
      const h1Count = headingLevels.filter(h => h.level === 'h1').length;
      expect(h1Count).toBeGreaterThanOrEqual(1);
    });

    await test.step('Check for proper ARIA labels', async () => {
      // Check buttons have accessible names
      const buttons = await page.locator('button').all();

      for (let i = 0; i < Math.min(buttons.length, 10); i++) {
        const button = buttons[i];
        const ariaLabel = await button.getAttribute('aria-label');
        const ariaLabelledBy = await button.getAttribute('aria-labelledby');
        const text = await button.textContent();

        const hasAccessibleName = ariaLabel || ariaLabelledBy || (text && text.trim());

        if (!hasAccessibleName) {
          console.warn(`Button ${i} lacks accessible name`);
        }
      }
    });

    await test.step('Check for proper form labels', async () => {
      const inputs = await page.locator('input, select, textarea').all();

      for (let i = 0; i < Math.min(inputs.length, 10); i++) {
        const input = inputs[i];
        const id = await input.getAttribute('id');
        const ariaLabel = await input.getAttribute('aria-label');
        const ariaLabelledBy = await input.getAttribute('aria-labelledby');

        let hasLabel = false;

        if (id) {
          const label = page.locator(`label[for="${id}"]`);
          hasLabel = await label.isVisible();
        }

        hasLabel = hasLabel || !!ariaLabel || !!ariaLabelledBy;

        if (!hasLabel) {
          console.warn(`Input ${i} lacks proper label`);
        }
      }
    });

    await test.step('Check for proper landmarks', async () => {
      const landmarks = [
        { selector: 'main, [role="main"]', name: 'main' },
        { selector: 'nav, [role="navigation"]', name: 'navigation' },
        { selector: 'header, [role="banner"]', name: 'banner' },
        { selector: 'footer, [role="contentinfo"]', name: 'contentinfo' }
      ];

      for (const landmark of landmarks) {
        const element = page.locator(landmark.selector).first();

        if (await element.isVisible()) {
          console.log(`✅ ${landmark.name} landmark found`);
        } else {
          console.warn(`⚠️  ${landmark.name} landmark missing`);
        }
      }
    });
  });

  test('Color contrast and visual accessibility', async ({ page }) => {
    await test.step('Check for focus indicators', async () => {
      const focusableElements = page.locator('button, a, input, select, textarea').first();

      if (await focusableElements.isVisible()) {
        await focusableElements.focus();

        // Check if element has visible focus (this is a basic check)
        const hasFocus = await focusableElements.evaluate(el => {
          const styles = window.getComputedStyle(el);
          return styles.outline !== 'none' || styles.boxShadow !== 'none';
        });

        console.log('Focus indicator present:', hasFocus);
      }
    });

    await test.step('Check for sufficient text size', async () => {
      const textElements = page.locator('p, span, div, a, button').filter({ hasText: /.+/ });
      const count = await textElements.count();

      if (count > 0) {
        const fontSize = await textElements.first().evaluate(el => {
          const styles = window.getComputedStyle(el);
          return parseFloat(styles.fontSize);
        });

        // Minimum recommended font size is 16px
        expect(fontSize).toBeGreaterThanOrEqual(14);
        console.log(`Text size: ${fontSize}px`);
      }
    });

    await test.step('Check for proper alt text on images', async () => {
      const images = await page.locator('img').all();

      for (let i = 0; i < images.length; i++) {
        const img = images[i];
        const alt = await img.getAttribute('alt');
        const role = await img.getAttribute('role');

        // Images should have alt text or be marked as decorative
        const hasProperAlt = alt !== null || role === 'presentation';

        if (!hasProperAlt) {
          console.warn(`Image ${i} lacks proper alt text`);
        }
      }
    });
  });

  test('Screen reader compatibility', async ({ page }) => {
    await test.step('Check for proper live regions', async () => {
      const liveRegions = page.locator('[aria-live], [role="status"], [role="alert"]');
      const count = await liveRegions.count();

      console.log(`Found ${count} live regions for screen reader announcements`);
    });

    await test.step('Check for proper form error announcements', async () => {
      // Try to trigger form validation
      const form = page.locator('form').first();

      if (await form.isVisible()) {
        const submitButton = form.locator('button[type="submit"], input[type="submit"]').first();

        if (await submitButton.isVisible()) {
          await submitButton.click();

          // Look for error messages with proper ARIA
          const errorMessages = page.locator('[role="alert"], .error[aria-live], [aria-describedby]');
          const errorCount = await errorMessages.count();

          console.log(`Found ${errorCount} accessible error messages`);
        }
      }
    });

    await test.step('Check for proper table headers', async () => {
      const tables = await page.locator('table').all();

      for (let i = 0; i < tables.length; i++) {
        const table = tables[i];
        const headers = table.locator('th');
        const headerCount = await headers.count();

        if (headerCount > 0) {
          console.log(`Table ${i} has ${headerCount} headers`);

          // Check if headers have proper scope
          const firstHeader = headers.first();
          const scope = await firstHeader.getAttribute('scope');

          if (scope) {
            console.log(`Table headers have proper scope: ${scope}`);
          }
        }
      }
    });
  });

  test('Mobile accessibility', async ({ page }) => {
    await test.step('Test touch targets size', async () => {
      await page.setViewportSize({ width: 375, height: 667 });

      const touchTargets = page.locator('button, a, input[type="checkbox"], input[type="radio"]');
      const count = await touchTargets.count();

      for (let i = 0; i < Math.min(count, 10); i++) {
        const target = touchTargets.nth(i);

        if (await target.isVisible()) {
          const box = await target.boundingBox();

          if (box) {
            // Minimum touch target size should be 44x44px
            const minSize = 44;
            const isLargeEnough = box.width >= minSize && box.height >= minSize;

            if (!isLargeEnough) {
              console.warn(`Touch target ${i} is too small: ${box.width}x${box.height}px`);
            }
          }
        }
      }
    });

    await test.step('Test zoom functionality', async () => {
      // Test 200% zoom
      await page.evaluate(() => {
        document.body.style.zoom = '2';
      });

      await page.waitForTimeout(1000);

      // Check if content is still accessible
      const mainContent = page.locator('main, [role="main"], body').first();
      await expect(mainContent).toBeVisible();

      // Reset zoom
      await page.evaluate(() => {
        document.body.style.zoom = '1';
      });
    });
  });
});
