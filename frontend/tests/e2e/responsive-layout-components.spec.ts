import { test, expect, devices } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';

test.describe('Responsive Layout Components', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);

    // Mock authentication
    await page.addInitScript(() => {
      localStorage.setItem('smart-alarm-auth', JSON.stringify({
        state: {
          token: 'mock-jwt-token',
          user: {
            id: '1',
            email: 'test@example.com',
            name: 'Test User'
          }
        }
      }));
    });

    await page.goto('/dashboard');
  });

  test.describe('ResponsiveLayout Component', () => {
    test('should adapt container width based on viewport', async ({ page }) => {
      // Test different container sizes
      const containerSizes = [
        { viewport: { width: 375, height: 667 }, expectedClass: 'max-w-sm' },
        { viewport: { width: 768, height: 1024 }, expectedClass: 'max-w-md' },
        { viewport: { width: 1024, height: 768 }, expectedClass: 'max-w-lg' },
        { viewport: { width: 1920, height: 1080 }, expectedClass: 'max-w-7xl' }
      ];

      for (const config of containerSizes) {
        await page.setViewportSize(config.viewport);
        await page.waitForTimeout(500);

        // Check that responsive layout adapts
        const container = page.locator('[data-testid="responsive-container"]');
        if (await container.isVisible()) {
          await expect(container).toHaveClass(new RegExp(config.expectedClass));
        }
      }
    });

    test('should handle different padding configurations', async ({ page }) => {
      // Test padding variations
      await page.setViewportSize({ width: 1920, height: 1080 });

      // Check different padding classes are applied
      const paddedElements = page.locator('[data-testid*="padding-"]');
      const count = await paddedElements.count();

      for (let i = 0; i < count; i++) {
        const element = paddedElements.nth(i);
        await expect(element).toHaveClass(/px-/);
      }
    });
  });

  test.describe('ResponsiveGrid Component', () => {
    test('should display correct grid columns on different devices', async ({ page }) => {
      // Test mobile grid (1 column)
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      const mobileGrid = page.locator('[data-testid="responsive-grid"]');
      if (await mobileGrid.isVisible()) {
        await expect(mobileGrid).toHaveClass(/grid-cols-1/);
      }

      // Test tablet grid (2 columns)
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.waitForTimeout(500);

      if (await mobileGrid.isVisible()) {
        await expect(mobileGrid).toHaveClass(/md:grid-cols-2/);
      }

      // Test desktop grid (3+ columns)
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);

      if (await mobileGrid.isVisible()) {
        await expect(mobileGrid).toHaveClass(/lg:grid-cols-/);
      }
    });

    test('should handle different gap sizes', async ({ page }) => {
      // Check gap classes are applied
      const grids = page.locator('[data-testid*="grid-gap-"]');
      const count = await grids.count();

      for (let i = 0; i < count; i++) {
        const grid = grids.nth(i);
        await expect(grid).toHaveClass(/gap-/);
      }
    });
  });

  test.describe('ResponsiveStack Component', () => {
    test('should switch between vertical and horizontal layouts', async ({ page }) => {
      // Test responsive stack behavior
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      const stack = page.locator('[data-testid="responsive-stack"]');
      if (await stack.isVisible()) {
        // Should be vertical on mobile
        await expect(stack).toHaveClass(/flex-col/);
      }

      // Test desktop layout
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);

      if (await stack.isVisible()) {
        // Should be horizontal on desktop
        await expect(stack).toHaveClass(/md:flex-row/);
      }
    });

    test('should handle different alignment options', async ({ page }) => {
      // Check alignment classes
      const alignedStacks = page.locator('[data-testid*="stack-align-"]');
      const count = await alignedStacks.count();

      for (let i = 0; i < count; i++) {
        const stack = alignedStacks.nth(i);
        await expect(stack).toHaveClass(/items-/);
      }
    });
  });

  test.describe('ResponsiveCard Component', () => {
    test('should adapt padding and spacing on different devices', async ({ page }) => {
      // Test mobile card padding
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      const cards = page.locator('[data-testid="responsive-card"]');
      const cardCount = await cards.count();

      if (cardCount > 0) {
        const firstCard = cards.first();
        await expect(firstCard).toHaveClass(/p-/);
        await expect(firstCard).toHaveClass(/rounded-/);
      }

      // Test desktop card padding
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);

      if (cardCount > 0) {
        const firstCard = cards.first();
        await expect(firstCard).toHaveClass(/md:p-/);
      }
    });

    test('should handle hover states correctly', async ({ page }) => {
      await page.setViewportSize({ width: 1920, height: 1080 });

      const cards = page.locator('[data-testid="responsive-card"]');
      const cardCount = await cards.count();

      if (cardCount > 0) {
        const firstCard = cards.first();

        // Hover over card
        await firstCard.hover();

        // Should have hover shadow class
        await expect(firstCard).toHaveClass(/hover:shadow-/);
      }
    });
  });

  test.describe('ResponsiveText Component', () => {
    test('should scale text appropriately across devices', async ({ page }) => {
      const textVariants = [
        { testId: 'text-h1', expectedClass: 'text-2xl' },
        { testId: 'text-h2', expectedClass: 'text-xl' },
        { testId: 'text-body', expectedClass: 'text-sm' },
        { testId: 'text-caption', expectedClass: 'text-xs' }
      ];

      for (const variant of textVariants) {
        const element = page.locator(`[data-testid="${variant.testId}"]`);
        if (await element.isVisible()) {
          await expect(element).toHaveClass(new RegExp(variant.expectedClass));
        }
      }

      // Test responsive scaling
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);

      // Check that larger text classes are applied on desktop
      const headings = page.locator('[data-testid*="text-h"]');
      const headingCount = await headings.count();

      for (let i = 0; i < headingCount; i++) {
        const heading = headings.nth(i);
        await expect(heading).toHaveClass(/md:text-/);
      }
    });

    test('should handle different color variants', async ({ page }) => {
      const colorVariants = [
        { testId: 'text-primary', expectedClass: 'text-gray-900' },
        { testId: 'text-secondary', expectedClass: 'text-gray-700' },
        { testId: 'text-muted', expectedClass: 'text-gray-600' },
        { testId: 'text-error', expectedClass: 'text-red-600' }
      ];

      for (const variant of colorVariants) {
        const element = page.locator(`[data-testid="${variant.testId}"]`);
        if (await element.isVisible()) {
          await expect(element).toHaveClass(new RegExp(variant.expectedClass));
        }
      }
    });
  });

  test.describe('Cross-Device Consistency', () => {
    test('should maintain layout integrity across device changes', async ({ page }) => {
      const viewports = [
        { width: 375, height: 667, name: 'Mobile' },
        { width: 768, height: 1024, name: 'Tablet' },
        { width: 1024, height: 768, name: 'Tablet Landscape' },
        { width: 1366, height: 768, name: 'Desktop Small' },
        { width: 1920, height: 1080, name: 'Desktop Large' }
      ];

      for (const viewport of viewports) {
        await page.setViewportSize(viewport);
        await page.waitForTimeout(500);

        // Check that main content is always visible
        await expect(page.locator('text=Smart Alarm')).toBeVisible();

        // Check that responsive components are present
        const responsiveElements = page.locator('[data-testid*="responsive-"]');
        const count = await responsiveElements.count();
        expect(count).toBeGreaterThan(0);

        // Check that no horizontal scrolling is needed
        const bodyWidth = await page.evaluate(() => document.body.scrollWidth);
        const viewportWidth = viewport.width;
        expect(bodyWidth).toBeLessThanOrEqual(viewportWidth + 20); // Allow small margin
      }
    });

    test('should handle orientation changes smoothly', async ({ page }) => {
      // Start in portrait
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      // Check portrait layout
      await expect(page.locator('[data-testid="responsive-layout"]')).toBeVisible();

      // Change to landscape
      await page.setViewportSize({ width: 667, height: 375 });
      await page.waitForTimeout(500);

      // Check landscape layout
      await expect(page.locator('[data-testid="responsive-layout"]')).toBeVisible();

      // Verify content is still accessible
      await expect(page.locator('text=Smart Alarm')).toBeVisible();
    });
  });

  test.describe('Performance and Accessibility', () => {
    test('should maintain performance during layout changes', async ({ page }) => {
      // Measure layout performance
      await page.evaluate(() => {
        performance.mark('layout-start');
      });

      // Rapidly change viewport sizes
      const sizes = [
        { width: 375, height: 667 },
        { width: 768, height: 1024 },
        { width: 1920, height: 1080 },
        { width: 375, height: 667 }
      ];

      for (const size of sizes) {
        await page.setViewportSize(size);
        await page.waitForTimeout(100);
      }

      await page.evaluate(() => {
        performance.mark('layout-end');
        performance.measure('layout-changes', 'layout-start', 'layout-end');
      });

      // Check that layout changes completed reasonably quickly
      const measure = await page.evaluate(() => {
        const measures = performance.getEntriesByName('layout-changes');
        return measures[0]?.duration || 0;
      });

      expect(measure).toBeLessThan(5000); // Should complete within 5 seconds
    });

    test('should maintain accessibility across different layouts', async ({ page }) => {
      const viewports = [
        { width: 375, height: 667 },
        { width: 768, height: 1024 },
        { width: 1920, height: 1080 }
      ];

      for (const viewport of viewports) {
        await page.setViewportSize(viewport);
        await page.waitForTimeout(500);

        // Check that interactive elements are accessible
        const buttons = page.locator('button:visible');
        const buttonCount = await buttons.count();

        for (let i = 0; i < Math.min(buttonCount, 5); i++) {
          const button = buttons.nth(i);

          // Check that button has accessible size
          const box = await button.boundingBox();
          if (box) {
            expect(box.height).toBeGreaterThanOrEqual(44); // Minimum touch target
            expect(box.width).toBeGreaterThanOrEqual(44);
          }

          // Check that button is keyboard accessible
          await button.focus();
          await expect(button).toBeFocused();
        }
      }
    });
  });
});
