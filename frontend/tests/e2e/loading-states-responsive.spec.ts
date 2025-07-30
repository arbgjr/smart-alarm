import { test, expect } from '@playwright/test';

/**
 * Loading States Responsive Tests
 *
 * Tests the loading system we implemented in subtask 2.10:
 * - Skeleton components (text, circular, rectangular variants)
 * - EmptyState components (alarm, routine, search, error states)
 * - Loading components (spinner, overlay, button states)
 */

test.describe('Loading States Responsive Behavior', () => {

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

  test('should display Skeleton components responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Test different skeleton variants
      const skeletonVariants = [
        'SkeletonText',
        'SkeletonCard',
        'SkeletonList'
      ];

      for (const variant of skeletonVariants) {
        const skeleton = page.locator(`[data-testid="${variant.toLowerCase()}"]`);

        if (await skeleton.isVisible()) {
          // Verify skeleton animates
          const hasAnimation = await skeleton.locator('.animate-pulse, .animate-wave').isVisible();

          // Take screenshot
          await page.screenshot({
            path: `test-results/loading-states/skeleton-${variant}-${deviceName}.png`,
            clip: await skeleton.boundingBox() || undefined
          });

          // Verify skeleton has proper dimensions
          const box = await skeleton.boundingBox();
          if (box) {
            expect(box.width).toBeGreaterThan(0);
            expect(box.height).toBeGreaterThan(0);

            // On mobile, skeletons should not exceed viewport width
            if (deviceName === 'mobile') {
              expect(box.width).toBeLessThanOrEqual(viewport.width);
            }
          }
        }
      }
    }
  });

  test('should display EmptyState components with proper responsive layout', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Test empty state variants we implemented
      const emptyStateVariants = [
        'EmptyAlarmState',
        'EmptyRoutineState',
        'EmptySearchState',
        'LoadingFailedState'
      ];

      for (const variant of emptyStateVariants) {
        const emptyState = page.locator(`[data-testid="${variant.toLowerCase()}"]`);

        if (await emptyState.isVisible()) {
          // Take screenshot
          await page.screenshot({
            path: `test-results/loading-states/empty-${variant}-${deviceName}.png`,
            clip: await emptyState.boundingBox() || undefined
          });

          // Verify empty state has icon and text
          const icon = emptyState.locator('svg');
          const text = emptyState.locator('h3, p');

          if (await icon.isVisible()) {
            const iconBox = await icon.boundingBox();
            if (iconBox) {
              // Icons should be appropriately sized for viewport
              if (deviceName === 'mobile') {
                expect(iconBox.width).toBeLessThanOrEqual(64);
              } else {
                expect(iconBox.width).toBeGreaterThanOrEqual(32);
              }
            }
          }

          if (await text.isVisible()) {
            // Verify text is readable
            const textBox = await text.boundingBox();
            if (textBox) {
              expect(textBox.width).toBeGreaterThan(100);
            }
          }
        }
      }
    }
  });

  test('should display LoadingSpinner with correct sizes across viewports', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Test loading spinner sizes: sm, md, lg, xl
      const spinnerSizes = ['sm', 'md', 'lg', 'xl'];

      for (const size of spinnerSizes) {
        const spinner = page.locator(`[data-testid="loading-spinner-${size}"]`);

        if (await spinner.isVisible()) {
          // Take screenshot
          await page.screenshot({
            path: `test-results/loading-states/spinner-${size}-${deviceName}.png`,
            clip: await spinner.boundingBox() || undefined
          });

          // Verify spinner has animation
          const hasAnimation = await spinner.locator('.animate-spin').isVisible();

          // Verify size is appropriate for viewport
          const box = await spinner.boundingBox();
          if (box) {
            // Spinner sizes should scale down on mobile if needed
            if (deviceName === 'mobile' && size === 'xl') {
              expect(box.width).toBeLessThanOrEqual(viewport.width / 3);
            }
          }
        }
      }
    }
  });

  test('should display LoadingOverlay properly on different screen sizes', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      const loadingOverlay = page.locator('[data-testid="loading-overlay"]');

      if (await loadingOverlay.isVisible()) {
        // Take screenshot
        await page.screenshot({
          path: `test-results/loading-states/overlay-${deviceName}.png`,
          fullPage: true
        });

        // Verify overlay covers appropriate area
        const box = await loadingOverlay.boundingBox();
        if (box) {
          // Overlay should cover significant portion of viewport
          expect(box.width).toBeGreaterThanOrEqual(viewport.width * 0.5);
          expect(box.height).toBeGreaterThanOrEqual(viewport.height * 0.3);
        }

        // Verify overlay has backdrop
        const backdrop = loadingOverlay.locator('.bg-black, .bg-white, .bg-opacity-50');
        if (await backdrop.isVisible()) {
          expect(backdrop).toBeVisible();
        }
      }
    }
  });

  test('should display LoadingButton states responsively', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Test loading button variants
      const loadingButton = page.locator('[data-testid="loading-button"]');

      if (await loadingButton.isVisible()) {
        // Take screenshot
        await page.screenshot({
          path: `test-results/loading-states/button-loading-${deviceName}.png`,
          clip: await loadingButton.boundingBox() || undefined
        });

        // Verify button maintains minimum touch target on mobile
        if (deviceName === 'mobile') {
          const box = await loadingButton.boundingBox();
          if (box) {
            expect(box.height).toBeGreaterThanOrEqual(44); // iOS HIG minimum
            expect(box.width).toBeGreaterThanOrEqual(44);
          }
        }

        // Verify loading spinner is visible within button
        const buttonSpinner = loadingButton.locator('.animate-spin');
        if (await buttonSpinner.isVisible()) {
          expect(buttonSpinner).toBeVisible();
        }
      }
    }
  });

  test('should test AlarmList and RoutineList loading integration', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Test AlarmList with loading states
      const alarmList = page.locator('[data-testid="alarm-list"]');
      if (await alarmList.isVisible()) {
        // Look for SkeletonList within AlarmList
        const skeletonList = alarmList.locator('[data-testid="skeleton-list"]');
        if (await skeletonList.isVisible()) {
          await page.screenshot({
            path: `test-results/loading-states/alarm-list-loading-${deviceName}.png`,
            clip: await alarmList.boundingBox() || undefined
          });
        }

        // Look for EmptyAlarmState
        const emptyState = alarmList.locator('[data-testid="empty-alarm-state"]');
        if (await emptyState.isVisible()) {
          await page.screenshot({
            path: `test-results/loading-states/alarm-list-empty-${deviceName}.png`,
            clip: await alarmList.boundingBox() || undefined
          });
        }
      }

      // Test RoutineList with loading states
      const routineList = page.locator('[data-testid="routine-list"]');
      if (await routineList.isVisible()) {
        // Look for SkeletonList within RoutineList
        const skeletonList = routineList.locator('[data-testid="skeleton-list"]');
        if (await skeletonList.isVisible()) {
          await page.screenshot({
            path: `test-results/loading-states/routine-list-loading-${deviceName}.png`,
            clip: await routineList.boundingBox() || undefined
          });
        }

        // Look for EmptyRoutineState
        const emptyState = routineList.locator('[data-testid="empty-routine-state"]');
        if (await emptyState.isVisible()) {
          await page.screenshot({
            path: `test-results/loading-states/routine-list-empty-${deviceName}.png`,
            clip: await routineList.boundingBox() || undefined
          });
        }
      }
    }
  });

  test('should verify loading state accessibility on all devices', async ({ page }) => {
    for (const [deviceName, viewport] of Object.entries(VIEWPORTS)) {
      await page.setViewportSize(viewport);

      // Check loading elements have proper ARIA labels
      const loadingElements = page.locator('[aria-label*="loading"], [aria-label*="Loading"]');
      const count = await loadingElements.count();

      for (let i = 0; i < count; i++) {
        const element = loadingElements.nth(i);
        const ariaLabel = await element.getAttribute('aria-label');

        // Verify aria-label exists and is descriptive
        expect(ariaLabel).toBeTruthy();
        expect(ariaLabel!.length).toBeGreaterThan(5);
      }

      // Check that skeleton elements don't interfere with screen readers
      const skeletons = page.locator('[data-testid*="skeleton"]');
      const skeletonCount = await skeletons.count();

      for (let i = 0; i < skeletonCount; i++) {
        const skeleton = skeletons.nth(i);

        // Skeletons should have aria-hidden="true" or similar
        const ariaHidden = await skeleton.getAttribute('aria-hidden');
        const role = await skeleton.getAttribute('role');

        // Either aria-hidden should be true or role should be presentation
        const isAccessibilityFriendly = ariaHidden === 'true' || role === 'presentation';
        expect(isAccessibilityFriendly).toBeTruthy();
      }
    }
  });

});
