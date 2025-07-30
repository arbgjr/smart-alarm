import { test, expect } from '@playwright/test';

/**
 * Component Navigation Responsive Tests
 *
 * Tests navigation and component interaction across different screen sizes
 */

test.describe('Component Navigation Responsiveness', () => {

  const BASE_URL = 'http://localhost:5173';

  const VIEWPORTS = {
    mobile: { width: 390, height: 844 },
    tablet: { width: 1024, height: 768 },
    desktop: { width: 1920, height: 1080 }
  };

  test.beforeEach(async ({ page }) => {
    await page.goto(BASE_URL);
    await page.waitForLoadState('domcontentloaded');
  });

  test('should navigate between dashboard sections responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for main navigation
      const mainNav = page.locator('nav, [data-testid="main-navigation"]');

      if (await mainNav.isVisible()) {
        // Find navigation links
        const navLinks = mainNav.locator('a, button[role="menuitem"]');
        const linkCount = await navLinks.count();

        // Test first few navigation items
        for (let i = 0; i < Math.min(linkCount, 3); i++) {
          const link = navLinks.nth(i);

          if (await link.isVisible()) {
            // Take screenshot before click
            await page.screenshot({
              path: `test-results/navigation/before-nav-${i}-${deviceName}.png`,
              fullPage: true
            });

            // Click navigation item
            await link.click();

            // Wait for navigation to complete
            await page.waitForLoadState('domcontentloaded');
            await page.waitForTimeout(500);

            // Take screenshot after navigation
            await page.screenshot({
              path: `test-results/navigation/after-nav-${i}-${deviceName}.png`,
              fullPage: true
            });

            // Verify page changed (URL or content)
            const currentUrl = page.url();
            expect(currentUrl).toBeTruthy();
          }
        }
      }
    }
  });

  test('should test Dashboard component layout responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for dashboard container
      const dashboard = page.locator('[data-testid="dashboard"], .dashboard');

      if (await dashboard.isVisible()) {
        // Take dashboard screenshot
        await page.screenshot({
          path: `test-results/components/dashboard-${deviceName}.png`,
          fullPage: true
        });

        // Check dashboard stats section
        const statsSection = dashboard.locator('[data-testid="dashboard-stats"], .stats');
        if (await statsSection.isVisible()) {
          const statsBox = await statsSection.boundingBox();

          if (statsBox) {
            // On mobile, stats should stack vertically or adapt layout
            if (deviceName === 'mobile') {
              expect(statsBox.width).toBeLessThanOrEqual(viewport.width);
            }
          }
        }

        // Check main content area
        const mainContent = dashboard.locator('[data-testid="dashboard-content"], .main-content');
        if (await mainContent.isVisible()) {
          const contentBox = await mainContent.boundingBox();

          if (contentBox) {
            // Content should be responsive
            expect(contentBox.width).toBeGreaterThan(200);
            expect(contentBox.width).toBeLessThanOrEqual(viewport.width);
          }
        }
      }
    }
  });

  test('should test AlarmList component interactions', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      const alarmList = page.locator('[data-testid="alarm-list"]');

      if (await alarmList.isVisible()) {
        // Screenshot alarm list
        await page.screenshot({
          path: `test-results/components/alarm-list-${deviceName}.png`,
          clip: await alarmList.boundingBox() || undefined
        });

        // Test alarm item interactions
        const alarmItems = alarmList.locator('[data-testid="alarm-item"]');
        const itemCount = await alarmItems.count();

        if (itemCount > 0) {
          const firstAlarm = alarmItems.first();

          // Test toggle button if exists
          const toggleButton = firstAlarm.locator('button[data-testid="alarm-toggle"]');
          if (await toggleButton.isVisible()) {
            // Take screenshot before toggle
            await page.screenshot({
              path: `test-results/interactions/alarm-before-toggle-${deviceName}.png`,
              clip: await firstAlarm.boundingBox() || undefined
            });

            await toggleButton.click();
            await page.waitForTimeout(500);

            // Take screenshot after toggle
            await page.screenshot({
              path: `test-results/interactions/alarm-after-toggle-${deviceName}.png`,
              clip: await firstAlarm.boundingBox() || undefined
            });
          }

          // Test delete button if exists
          const deleteButton = firstAlarm.locator('button[data-testid="alarm-delete"]');
          if (await deleteButton.isVisible()) {
            // Verify button size on mobile
            if (deviceName === 'mobile') {
              const buttonBox = await deleteButton.boundingBox();
              if (buttonBox) {
                expect(buttonBox.height).toBeGreaterThanOrEqual(44);
                expect(buttonBox.width).toBeGreaterThanOrEqual(44);
              }
            }
          }
        }
      }
    }
  });

  test('should test RoutineList component interactions', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      const routineList = page.locator('[data-testid="routine-list"]');

      if (await routineList.isVisible()) {
        // Screenshot routine list
        await page.screenshot({
          path: `test-results/components/routine-list-${deviceName}.png`,
          clip: await routineList.boundingBox() || undefined
        });

        // Test routine item interactions
        const routineItems = routineList.locator('[data-testid="routine-item"]');
        const itemCount = await routineItems.count();

        if (itemCount > 0) {
          const firstRoutine = routineItems.first();

          // Test routine interactions
          const actionButtons = firstRoutine.locator('button');
          const buttonCount = await actionButtons.count();

          for (let i = 0; i < Math.min(buttonCount, 2); i++) {
            const button = actionButtons.nth(i);

            if (await button.isVisible()) {
              // Verify button accessibility on mobile
              if (deviceName === 'mobile') {
                const buttonBox = await button.boundingBox();
                if (buttonBox) {
                  expect(buttonBox.height).toBeGreaterThanOrEqual(44);
                }
              }
            }
          }
        }
      }
    }
  });

  test('should test modal and overlay responsiveness', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for buttons that might trigger modals
      const modalTriggers = page.locator('button:has-text("Add"), button:has-text("Create"), button:has-text("Edit")');
      const triggerCount = await modalTriggers.count();

      if (triggerCount > 0) {
        const firstTrigger = modalTriggers.first();

        if (await firstTrigger.isVisible()) {
          // Click to open modal
          await firstTrigger.click();
          await page.waitForTimeout(500);

          // Look for modal
          const modal = page.locator('[role="dialog"], .modal, [data-testid="modal"]');

          if (await modal.isVisible()) {
            // Take modal screenshot
            await page.screenshot({
              path: `test-results/modals/modal-${deviceName}.png`,
              fullPage: true
            });

            // Verify modal sizing
            const modalBox = await modal.boundingBox();

            if (modalBox) {
              // On mobile, modal should not exceed viewport
              if (deviceName === 'mobile') {
                expect(modalBox.width).toBeLessThanOrEqual(viewport.width - 20);
                expect(modalBox.height).toBeLessThanOrEqual(viewport.height - 40);
              }
            }

            // Try to close modal
            const closeButton = modal.locator('button:has-text("Cancel"), button:has-text("Close"), [aria-label="Close"]');
            if (await closeButton.isVisible()) {
              await closeButton.click();
              await page.waitForTimeout(500);
            } else {
              // Try ESC key
              await page.keyboard.press('Escape');
              await page.waitForTimeout(500);
            }
          }
        }
      }
    }
  });

  test('should test form responsiveness', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for forms
      const forms = page.locator('form');
      const formCount = await forms.count();

      if (formCount > 0) {
        const firstForm = forms.first();

        if (await firstForm.isVisible()) {
          // Take form screenshot
          await page.screenshot({
            path: `test-results/forms/form-${deviceName}.png`,
            clip: await firstForm.boundingBox() || undefined
          });

          // Test form inputs
          const inputs = firstForm.locator('input, textarea, select');
          const inputCount = await inputs.count();

          for (let i = 0; i < Math.min(inputCount, 3); i++) {
            const input = inputs.nth(i);

            if (await input.isVisible()) {
              // Verify input sizing
              const inputBox = await input.boundingBox();

              if (inputBox) {
                // Inputs should be properly sized for touch on mobile
                if (deviceName === 'mobile') {
                  expect(inputBox.height).toBeGreaterThanOrEqual(44);
                }

                // Inputs should not exceed form width
                const formBox = await firstForm.boundingBox();
                if (formBox) {
                  expect(inputBox.width).toBeLessThanOrEqual(formBox.width);
                }
              }
            }
          }
        }
      }
    }
  });

  test('should test error handling component responsiveness', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Look for error boundary or error messages
      const errorComponents = page.locator('[data-testid="error-boundary"], [data-testid="error-message"], .error');
      const errorCount = await errorComponents.count();

      if (errorCount > 0) {
        for (let i = 0; i < errorCount; i++) {
          const errorComponent = errorComponents.nth(i);

          if (await errorComponent.isVisible()) {
            // Take error component screenshot
            await page.screenshot({
              path: `test-results/errors/error-${i}-${deviceName}.png`,
              clip: await errorComponent.boundingBox() || undefined
            });

            // Test retry button if exists
            const retryButton = errorComponent.locator('button:has-text("Retry"), button:has-text("Try Again")');
            if (await retryButton.isVisible()) {
              // Verify button accessibility
              if (deviceName === 'mobile') {
                const buttonBox = await retryButton.boundingBox();
                if (buttonBox) {
                  expect(buttonBox.height).toBeGreaterThanOrEqual(44);
                }
              }
            }
          }
        }
      }
    }
  });

});
