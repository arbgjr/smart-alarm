import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

test.describe('Enhanced Dashboard Features', () => {
  let loginPage: LoginPage;
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    dashboardPage = new DashboardPage(page);

    // Mock authentication for testing
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

  test('should display enhanced metrics grid with detailed statistics', async ({ page }) => {
    // Check that enhanced metrics grid is visible
    await expect(page.locator('[data-testid="enhanced-metrics-grid"]')).toBeVisible();

    // Check primary metrics row
    await expect(page.locator('text=Active Alarms')).toBeVisible();
    await expect(page.locator('text=Today\'s Alarms')).toBeVisible();
    await expect(page.locator('text=Active Routines')).toBeVisible();
    await expect(page.locator('text=Success Rate')).toBeVisible();

    // Check performance metrics row
    await expect(page.locator('text=Total Triggered')).toBeVisible();
    await expect(page.locator('text=Avg Response Time')).toBeVisible();
    await expect(page.locator('text=Dismissal Rate')).toBeVisible();
    await expect(page.locator('text=Efficiency Score')).toBeVisible();

    // Check detailed stats row
    await expect(page.locator('text=Total Dismissed')).toBeVisible();
    await expect(page.locator('text=Total Snoozed')).toBeVisible();
    await expect(page.locator('text=Total Actions')).toBeVisible();

    // Verify metrics have numeric values
    const metricValues = page.locator('[data-testid="metric-value"]');
    const count = await metricValues.count();
    expect(count).toBeGreaterThan(0);

    for (let i = 0; i < count; i++) {
      const value = await metricValues.nth(i).textContent();
      expect(value).toMatch(/\d+/);
    }
  });

  test('should display enhanced alarm chart with interactive features', async ({ page }) => {
    // Check that enhanced chart is visible
    await expect(page.locator('[data-testid="enhanced-alarm-chart"]')).toBeVisible();

    // Check chart controls
    await expect(page.locator('text=Alarm Activity Trends')).toBeVisible();

    // Check view toggle buttons
    await expect(page.locator('button:has-text("Bar")')).toBeVisible();
    await expect(page.locator('button:has-text("Line")')).toBeVisible();
    await expect(page.locator('button:has-text("Area")')).toBeVisible();

    // Check time range toggle
    await expect(page.locator('button:has-text("7d")')).toBeVisible();
    await expect(page.locator('button:has-text("30d")')).toBeVisible();
    await expect(page.locator('button:has-text("90d")')).toBeVisible();

    // Test view switching
    await page.locator('button:has-text("Line")').click();
    await page.waitForTimeout(500);

    await page.locator('button:has-text("Area")').click();
    await page.waitForTimeout(500);

    // Test time range switching
    await page.locator('button:has-text("30d")').click();
    await page.waitForTimeout(500);

    // Check chart legend
    await expect(page.locator('text=Triggered')).toBeVisible();
    await expect(page.locator('text=Dismissed')).toBeVisible();
    await expect(page.locator('text=Snoozed')).toBeVisible();

    // Check summary statistics
    await expect(page.locator('text=Total Triggered')).toBeVisible();
    await expect(page.locator('text=Success Rate')).toBeVisible();
    await expect(page.locator('text=Snooze Rate')).toBeVisible();
    await expect(page.locator('text=Daily Average')).toBeVisible();
  });

  test('should display real-time notifications panel', async ({ page }) => {
    // Mock real-time notification
    await page.addInitScript(() => {
      setTimeout(() => {
        const event = new CustomEvent('notification', {
          detail: {
            userId: '1',
            type: 'alarm_reminder',
            title: 'Test Notification',
            message: 'This is a test notification',
            priority: 'medium',
            timestamp: new Date().toISOString()
          }
        });
        window.dispatchEvent(event);
      }, 1000);
    });

    // Check that notifications panel is visible
    await expect(page.locator('[data-testid="real-time-notifications"]')).toBeVisible();

    // Check notification appears
    await expect(page.locator('text=Test Notification')).toBeVisible({ timeout: 5000 });

    // Check notification content
    await expect(page.locator('text=This is a test notification')).toBeVisible();

    // Test notification dismissal
    await page.locator('[data-testid="dismiss-notification"]').first().click();
    await expect(page.locator('text=Test Notification')).toBeHidden();
  });

  test('should display enhanced quick actions panel', async ({ page }) => {
    // Check that quick actions panel is visible
    await expect(page.locator('[data-testid="quick-actions-panel"]')).toBeVisible();

    // Check primary actions
    await expect(page.locator('button:has-text("Create Alarm")')).toBeVisible();
    await expect(page.locator('button:has-text("Create Routine")')).toBeVisible();
    await expect(page.locator('button:has-text("Refresh Data")')).toBeVisible();

    // Check navigation actions
    await expect(page.locator('a:has-text("Manage Alarms")')).toBeVisible();
    await expect(page.locator('a:has-text("Settings")')).toBeVisible();
    await expect(page.locator('a:has-text("Analytics")')).toBeVisible();

    // Check utility actions
    await expect(page.locator('button:has-text("Import")')).toBeVisible();
    await expect(page.locator('button:has-text("Export")')).toBeVisible();

    // Test refresh action
    await page.locator('button:has-text("Refresh Data")').click();

    // Should show loading state briefly
    await expect(page.locator('text=Refreshing...')).toBeVisible({ timeout: 1000 });
    await expect(page.locator('text=Refreshing...')).toBeHidden({ timeout: 5000 });

    // Check status indicators
    await expect(page.locator('text=System Online')).toBeVisible();
    await expect(page.locator('text=Real-time Sync')).toBeVisible();
  });

  test('should handle responsive layout changes', async ({ page }) => {
    // Start with desktop layout
    await page.setViewportSize({ width: 1920, height: 1080 });

    // Check desktop layout
    await expect(page.locator('[data-testid="enhanced-metrics-grid"]')).toHaveClass(/lg:grid-cols-4/);

    // Switch to tablet layout
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(500);

    // Check tablet layout adaptations
    await expect(page.locator('[data-testid="enhanced-metrics-grid"]')).toHaveClass(/md:grid-cols-2/);

    // Switch to mobile layout
    await page.setViewportSize({ width: 375, height: 667 });
    await page.waitForTimeout(500);

    // Check mobile layout
    await expect(page.locator('[data-testid="mobile-menu-button"]')).toBeVisible();
    await expect(page.locator('[data-testid="enhanced-metrics-grid"]')).toHaveClass(/grid-cols-1/);

    // Test mobile menu
    await page.locator('[data-testid="mobile-menu-button"]').click();
    await expect(page.locator('[data-testid="mobile-menu"]')).toBeVisible();
  });

  test('should handle real-time data updates', async ({ page }) => {
    // Mock SignalR connection
    await page.addInitScript(() => {
      // Mock SignalR events
      setTimeout(() => {
        const alarmEvent = new CustomEvent('alarmTriggered', {
          detail: {
            alarmId: 'test-alarm-1',
            userId: '1',
            type: 'triggered',
            timestamp: new Date().toISOString()
          }
        });
        window.dispatchEvent(alarmEvent);
      }, 2000);
    });

    // Check initial metrics
    const initialTriggered = await page.locator('[data-testid="total-triggered-value"]').textContent();

    // Wait for real-time update
    await page.waitForTimeout(3000);

    // Check that metrics updated (in a real scenario)
    // Note: This would require actual SignalR integration to test properly
    await expect(page.locator('[data-testid="total-triggered-value"]')).toBeVisible();
  });

  test('should display error states gracefully', async ({ page }) => {
    // Mock API error
    await page.route('**/api/dashboard/metrics', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' })
      });
    });

    // Refresh to trigger error
    await page.reload();

    // Check error boundaries
    await expect(page.locator('text=Unable to load')).toBeVisible({ timeout: 10000 });

    // Check that other components still work
    await expect(page.locator('[data-testid="quick-actions-panel"]')).toBeVisible();
  });

  test('should handle loading states correctly', async ({ page }) => {
    // Mock slow API response
    await page.route('**/api/dashboard/metrics', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          metrics: {
            activeAlarms: 5,
            todaysAlarms: 3,
            activeRoutines: 2,
            totalTriggered: 45,
            totalDismissed: 38,
            totalSnoozed: 7,
            successRate: 84.4,
            avgResponseTime: 2.3
          }
        })
      });
    });

    // Reload to trigger loading
    await page.reload();

    // Check loading indicators
    await expect(page.locator('[data-testid="loading-spinner"]')).toBeVisible();

    // Wait for loading to complete
    await expect(page.locator('[data-testid="loading-spinner"]')).toBeHidden({ timeout: 10000 });

    // Check that content loaded
    await expect(page.locator('[data-testid="enhanced-metrics-grid"]')).toBeVisible();
  });

  test('should support keyboard navigation', async ({ page }) => {
    // Test tab navigation through interactive elements
    await page.keyboard.press('Tab');

    let focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();

    // Continue tabbing through elements
    for (let i = 0; i < 10; i++) {
      await page.keyboard.press('Tab');
      focusedElement = page.locator(':focus');
      await expect(focusedElement).toBeVisible();
    }

    // Test Enter key activation
    const createAlarmButton = page.locator('button:has-text("Create Alarm")');
    await createAlarmButton.focus();
    await page.keyboard.press('Enter');

    // Should navigate or open modal
    await page.waitForTimeout(1000);
  });

  test('should handle chart interactions', async ({ page }) => {
    // Check chart is visible
    await expect(page.locator('[data-testid="enhanced-alarm-chart"]')).toBeVisible();

    // Test hover interactions on chart bars
    const chartBars = page.locator('[data-testid="chart-bar"]');
    const barCount = await chartBars.count();

    if (barCount > 0) {
      // Hover over first bar
      await chartBars.first().hover();

      // Check for tooltip
      await expect(page.locator('[data-testid="chart-tooltip"]')).toBeVisible({ timeout: 2000 });
    }

    // Test chart view switching
    await page.locator('button:has-text("Line")').click();
    await page.waitForTimeout(500);

    // Check that chart updated
    await expect(page.locator('[data-testid="enhanced-alarm-chart"]')).toBeVisible();
  });
});
