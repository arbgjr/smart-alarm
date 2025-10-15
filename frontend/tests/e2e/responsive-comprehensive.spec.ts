import { test, expect, devices } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';

test.describe('Comprehensive Responsive Design', () => {
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('test@example.com', 'password123');
  });

  // Mobile Device Tests
  test.describe('Mobile Devices', () => {
    test('iPhone 12 - Portrait', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12'],
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check mobile-specific layout
      await expect(page.locator('[data-testid="mobile-menu-button"]')).toBeVisible();

      // Check that content is properly stacked
      const metricsGrid = page.locator('[data-testid="metrics-grid"]');
      await expect(metricsGrid).toHaveClass(/grid-cols-1/);

      // Test mobile navigation
      await page.locator('[data-testid="mobile-menu-button"]').click();
      await expect(page.locator('[data-testid="mobile-menu"]')).toBeVisible();

      // Test touch interactions
      const alarmCard = page.locator('[data-testid="alarm-card"]').first();
      if (await alarmCard.isVisible()) {
        await alarmCard.tap();
      }

      await context.close();
    });

    test('iPhone 12 - Landscape', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12 landscape'],
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check landscape layout adaptations
      await expect(page.locator('[data-testid="landscape-layout"]')).toBeVisible();

      // Check that horizontal space is utilized better
      const contentGrid = page.locator('[data-testid="main-content-grid"]');
      await expect(contentGrid).toHaveClass(/lg:grid-cols-3/);

      await context.close();
    });

    test('Pixel 5', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['Pixel 5'],
      });
      const page = await context.newPage();

      await page.goto('/alarms');

      // Check Android-specific behaviors
      await expect(page.locator('[data-testid="alarms-grid"]')).toHaveClass(/grid-cols-1/);

      // Test filter interactions on mobile
      await page.locator('[data-testid="search-input"]').fill('test');
      await expect(page.locator('text=Search: "test"')).toBeVisible();

      await context.close();
    });
  });

  // Tablet Device Tests
  test.describe('Tablet Devices', () => {
    test('iPad Pro', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPad Pro'],
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check tablet layout
      const metricsGrid = page.locator('[data-testid="metrics-grid"]');
      await expect(metricsGrid).toHaveClass(/md:grid-cols-2/);

      // Check that mobile menu is not visible
      await expect(page.locator('[data-testid="mobile-menu-button"]')).toBeHidden();

      // Test tablet-specific interactions
      await page.goto('/settings');

      // Settings should show sidebar navigation
      await expect(page.locator('[data-testid="settings-sidebar"]')).toBeVisible();

      await context.close();
    });

    test('iPad Pro - Landscape', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPad Pro landscape'],
      });
      const page = await context.newPage();

      await page.goto('/alarms');

      // Check landscape tablet layout
      const alarmsGrid = page.locator('[data-testid="alarms-grid"]');
      await expect(alarmsGrid).toHaveClass(/md:grid-cols-2/);

      // Check that more content is visible horizontally
      const actionButtons = page.locator('[data-testid="action-buttons"]');
      await expect(actionButtons).toBeVisible();

      await context.close();
    });
  });

  // Desktop Tests
  test.describe('Desktop Devices', () => {
    test('Desktop 1920x1080', async ({ browser }) => {
      const context = await browser.newContext({
        viewport: { width: 1920, height: 1080 }
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check full desktop layout
      const metricsGrid = page.locator('[data-testid="metrics-grid"]');
      await expect(metricsGrid).toHaveClass(/lg:grid-cols-4/);

      // Check that all navigation is visible
      await expect(page.locator('[data-testid="desktop-navigation"]')).toBeVisible();

      // Test multi-column layouts
      const mainGrid = page.locator('[data-testid="main-content-grid"]');
      await expect(mainGrid).toHaveClass(/lg:grid-cols-3/);

      await context.close();
    });

    test('Desktop 1366x768', async ({ browser }) => {
      const context = await browser.newContext({
        viewport: { width: 1366, height: 768 }
      });
      const page = await context.newPage();

      await page.goto('/settings');

      // Check medium desktop layout
      await expect(page.locator('[data-testid="settings-sidebar"]')).toBeVisible();

      // Check that content fits properly
      const settingsContent = page.locator('[data-testid="settings-content"]');
      await expect(settingsContent).toBeVisible();

      await context.close();
    });

    test('Ultrawide 2560x1080', async ({ browser }) => {
      const context = await browser.newContext({
        viewport: { width: 2560, height: 1080 }
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check ultrawide layout adaptations
      const container = page.locator('[data-testid="main-container"]');
      await expect(container).toHaveClass(/max-w-7xl/);

      // Content should be centered and not stretched too wide
      const contentWidth = await container.boundingBox();
      expect(contentWidth?.width).toBeLessThan(1400); // Max container width

      await context.close();
    });
  });

  // Cross-Device Navigation Tests
  test.describe('Cross-Device Navigation', () => {
    test('Navigation consistency across devices', async ({ browser }) => {
      const devices = [
        { name: 'Mobile', device: 'iPhone 12' },
        { name: 'Tablet', device: 'iPad Pro' },
        { name: 'Desktop', viewport: { width: 1920, height: 1080 } }
      ];

      for (const deviceConfig of devices) {
        const context = deviceConfig.device
          ? await browser.newContext({ ...devices[deviceConfig.device as keyof typeof devices] })
          : await browser.newContext({ viewport: deviceConfig.viewport });

        const page = await context.newPage();

        // Test navigation to all main pages
        await page.goto('/dashboard');
        await expect(page.locator('text=Smart Alarm')).toBeVisible();

        await page.goto('/alarms');
        await expect(page.locator('text=Alarm Management')).toBeVisible();

        await page.goto('/settings');
        await expect(page.locator('text=Settings')).toBeVisible();

        await context.close();
      }
    });
  });

  // Accessibility Tests
  test.describe('Responsive Accessibility', () => {
    test('Touch targets on mobile', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12'],
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check that touch targets are at least 44px
      const buttons = page.locator('button');
      const buttonCount = await buttons.count();

      for (let i = 0; i < Math.min(buttonCount, 10); i++) {
        const button = buttons.nth(i);
        if (await button.isVisible()) {
          const box = await button.boundingBox();
          if (box) {
            expect(box.height).toBeGreaterThanOrEqual(44);
            expect(box.width).toBeGreaterThanOrEqual(44);
          }
        }
      }

      await context.close();
    });

    test('Keyboard navigation on desktop', async ({ browser }) => {
      const context = await browser.newContext({
        viewport: { width: 1920, height: 1080 }
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Test tab navigation
      await page.keyboard.press('Tab');
      let focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();

      // Continue tabbing through interactive elements
      for (let i = 0; i < 5; i++) {
        await page.keyboard.press('Tab');
        focusedElement = page.locator(':focus');
        await expect(focusedElement).toBeVisible();
      }

      await context.close();
    });
  });

  // Performance Tests
  test.describe('Responsive Performance', () => {
    test('Layout shift on resize', async ({ page }) => {
      await page.goto('/dashboard');

      // Start with desktop size
      await page.setViewportSize({ width: 1920, height: 1080 });
      await page.waitForTimeout(500);

      // Resize to tablet
      await page.setViewportSize({ width: 768, height: 1024 });
      await page.waitForTimeout(500);

      // Resize to mobile
      await page.setViewportSize({ width: 375, height: 667 });
      await page.waitForTimeout(500);

      // Check that content is still visible and properly laid out
      await expect(page.locator('text=Smart Alarm')).toBeVisible();
      await expect(page.locator('[data-testid="metrics-grid"]')).toBeVisible();
    });

    test('Image loading on different densities', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12'],
        deviceScaleFactor: 3 // High DPI
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Check that images load properly on high DPI
      const images = page.locator('img');
      const imageCount = await images.count();

      for (let i = 0; i < imageCount; i++) {
        const image = images.nth(i);
        if (await image.isVisible()) {
          await expect(image).toHaveAttribute('src');
        }
      }

      await context.close();
    });
  });

  // Orientation Change Tests
  test.describe('Orientation Changes', () => {
    test('Smooth orientation transitions', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12'],
      });
      const page = await context.newPage();

      await page.goto('/dashboard');

      // Start in portrait
      await expect(page.locator('[data-testid="portrait-layout"]')).toBeVisible();

      // Rotate to landscape
      await page.setViewportSize({ width: 844, height: 390 });
      await page.waitForTimeout(500);

      // Check landscape layout
      await expect(page.locator('[data-testid="landscape-layout"]')).toBeVisible();

      // Rotate back to portrait
      await page.setViewportSize({ width: 390, height: 844 });
      await page.waitForTimeout(500);

      // Check portrait layout
      await expect(page.locator('[data-testid="portrait-layout"]')).toBeVisible();

      await context.close();
    });
  });

  // Content Adaptation Tests
  test.describe('Content Adaptation', () => {
    test('Text scaling and readability', async ({ browser }) => {
      const viewports = [
        { width: 375, height: 667, name: 'Mobile' },
        { width: 768, height: 1024, name: 'Tablet' },
        { width: 1920, height: 1080, name: 'Desktop' }
      ];

      for (const viewport of viewports) {
        const context = await browser.newContext({ viewport });
        const page = await context.newPage();

        await page.goto('/dashboard');

        // Check that text is readable at different sizes
        const headings = page.locator('h1, h2, h3');
        const headingCount = await headings.count();

        for (let i = 0; i < headingCount; i++) {
          const heading = headings.nth(i);
          if (await heading.isVisible()) {
            const fontSize = await heading.evaluate(el =>
              window.getComputedStyle(el).fontSize
            );
            const fontSizeNum = parseInt(fontSize);

            // Ensure minimum readable font size
            expect(fontSizeNum).toBeGreaterThanOrEqual(14);
          }
        }

        await context.close();
      }
    });

    test('Interactive element spacing', async ({ browser }) => {
      const context = await browser.newContext({
        ...devices['iPhone 12'],
      });
      const page = await context.newPage();

      await page.goto('/alarms');

      // Check spacing between interactive elements
      const buttons = page.locator('button:visible');
      const buttonCount = await buttons.count();

      if (buttonCount > 1) {
        for (let i = 0; i < buttonCount - 1; i++) {
          const button1 = buttons.nth(i);
          const button2 = buttons.nth(i + 1);

          const box1 = await button1.boundingBox();
          const box2 = await button2.boundingBox();

          if (box1 && box2) {
            // Check minimum spacing between buttons
            const distance = Math.abs(box2.y - (box1.y + box1.height));
            if (distance < 100) { // Only check if buttons are close vertically
              expect(distance).toBeGreaterThanOrEqual(8); // Minimum 8px spacing
            }
          }
        }
      }

      await context.close();
    });
  });
});
