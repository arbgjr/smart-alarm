import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

test.describe('Enhanced Dashboard', () => {
  let loginPage: LoginPage;
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    dashboardPage = new DashboardPage(page);

    // Login before each test
    await loginPage.goto();
    await loginPage.login('test@example.com', 'password123');
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display enhanced metrics cards', async ({ page }) => {
    // Check that all metric cards are visible
    await expect(dashboardPage.activeAlarmsCard).toBeVisible();
    await expect(dashboardPage.todaysAlarmsCard).toBeVisible();
    await expect(dashboardPage.activeRoutinesCard).toBeVisible();
    await expect(dashboardPage.successRateCard).toBeVisible();

    // Check that metrics have values
    const activeAlarmsValue = await dashboardPage.activeAlarmsCard.locator('[data-testid="metric-value"]').textContent();
    expect(activeAlarmsValue).toMatch(/\d+/);

    // Check that change indicators are present
    await expect(dashboardPage.activeAlarmsCard.locator('[data-testid="metric-change"]')).toBeVisible();
  });

  test('should display performance metrics', async ({ page }) => {
    // Check performance metrics section
    await expect(dashboardPage.totalTriggeredMetric).toBeVisible();
    await expect(dashboardPage.avgResponseTimeMetric).toBeVisible();
    await expect(dashboardPage.totalSnoozedMetric).toBeVisible();

    // Verify metrics have numeric values
    const totalTriggered = await dashboardPage.totalTriggeredMetric.locator('[data-testid="metric-value"]').textContent();
    expect(totalTriggered).toMatch(/\d+/);
  });

  test('should display alarm activity chart', async ({ page }) => {
    // Check that chart is visible
    await expect(dashboardPage.alarmChart).toBeVisible();

    // Check chart legend
    await expect(dashboardPage.alarmChart.locator('text=Triggered')).toBeVisible();
    await expect(dashboardPage.alarmChart.locator('text=Dismissed')).toBeVisible();
    await expect(dashboardPage.alarmChart.locator('text=Snoozed')).toBeVisible();

    // Check chart data bars
    const chartBars = dashboardPage.alarmChart.locator('[data-testid="chart-bar"]');
    await expect(chartBars.first()).toBeVisible();
  });

  test('should display real-time status', async ({ page }) => {
    // Check real-time status component
    await expect(dashboardPage.realTimeStatus).toBeVisible();

    // Check connection status
    await expect(dashboardPage.realTimeStatus.locator('text=Internet')).toBeVisible();
    await expect(dashboardPage.realTimeStatus.locator('text=Real-time')).toBeVisible();

    // Check status indicators
    const connectionIcon = dashboardPage.realTimeStatus.locator('[data-testid="connection-icon"]');
    await expect(connectionIcon).toBeVisible();
  });

  test('should display recent activity', async ({ page }) => {
    // Check recent activity section
    await expect(dashboardPage.recentActivity).toBeVisible();

    // Check activity items (if any exist)
    const activityItems = dashboardPage.recentActivity.locator('[data-testid="activity-item"]');
    const count = await activityItems.count();

    if (count > 0) {
      // Check first activity item structure
      const firstItem = activityItems.first();
      await expect(firstItem.locator('[data-testid="activity-icon"]')).toBeVisible();
      await expect(firstItem.locator('[data-testid="activity-title"]')).toBeVisible();
      await expect(firstItem.locator('[data-testid="activity-timestamp"]')).toBeVisible();
    }
  });

  test('should refresh metrics when refresh button is clicked', async ({ page }) => {
    // Click refresh button
    await dashboardPage.refreshButton.click();

    // Check for loading state or updated timestamp
    await expect(page.locator('[data-testid="loading-indicator"]')).toBeVisible({ timeout: 1000 });
    await expect(page.locator('[data-testid="loading-indicator"]')).toBeHidden({ timeout: 5000 });
  });

  test('should navigate to alarms page from quick actions', async ({ page }) => {
    await dashboardPage.createAlarmButton.click();
    await expect(page).toHaveURL('/alarms?action=create');
  });

  test('should navigate to routines page from quick actions', async ({ page }) => {
    await dashboardPage.createRoutineButton.click();
    await expect(page).toHaveURL('/routines?action=create');
  });

  test('should navigate to alarms management', async ({ page }) => {
    await dashboardPage.manageAlarmsButton.click();
    await expect(page).toHaveURL('/alarms');
  });

  test('should display alarm and routine lists', async ({ page }) => {
    // Check alarms section
    await expect(dashboardPage.alarmsSection).toBeVisible();
    await expect(dashboardPage.alarmsSection.locator('text=Recent Alarms')).toBeVisible();

    // Check routines section
    await expect(dashboardPage.routinesSection).toBeVisible();
    await expect(dashboardPage.routinesSection.locator('text=Recent Routines')).toBeVisible();

    // Check "View all" links
    await expect(dashboardPage.alarmsSection.locator('text=View all')).toBeVisible();
    await expect(dashboardPage.routinesSection.locator('text=View all')).toBeVisible();
  });

  test('should handle error states gracefully', async ({ page }) => {
    // Simulate network error by intercepting API calls
    await page.route('**/api/dashboard/metrics', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' })
      });
    });

    // Refresh the page to trigger the error
    await page.reload();

    // Check that error boundaries are working
    const errorMessage = page.locator('text=Unable to load');
    await expect(errorMessage.first()).toBeVisible({ timeout: 10000 });
  });

  test('should be responsive on mobile devices', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Check that mobile layout is applied
    await expect(dashboardPage.mobileMenuButton).toBeVisible();

    // Open mobile menu
    await dashboardPage.mobileMenuButton.click();
    await expect(dashboardPage.mobileMenu).toBeVisible();

    // Check mobile menu items
    await expect(dashboardPage.mobileMenu.locator('text=Alarms')).toBeVisible();
    await expect(dashboardPage.mobileMenu.locator('text=Routines')).toBeVisible();
    await expect(dashboardPage.mobileMenu.locator('text=Settings')).toBeVisible();
  });

  test('should adapt layout for tablet devices', async ({ page }) => {
    // Set tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });

    // Check that tablet layout is applied
    const metricsGrid = page.locator('[data-testid="metrics-grid"]');
    await expect(metricsGrid).toHaveClass(/grid-cols-2/);
  });

  test('should work with keyboard navigation', async ({ page }) => {
    // Test keyboard navigation through quick action buttons
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');

    // Check that focus is visible
    const focusedElement = page.locator(':focus');
    await expect(focusedElement).toBeVisible();

    // Test Enter key activation
    await page.keyboard.press('Enter');

    // Should navigate or trigger action
    await page.waitForTimeout(1000);
  });

  test('should display loading states correctly', async ({ page }) => {
    // Intercept API calls to add delay
    await page.route('**/api/dashboard/metrics', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      route.continue();
    });

    // Reload page to trigger loading
    await page.reload();

    // Check for loading indicators
    const loadingSpinner = page.locator('[data-testid="loading-spinner"]');
    await expect(loadingSpinner).toBeVisible();

    // Wait for loading to complete
    await expect(loadingSpinner).toBeHidden({ timeout: 10000 });
  });
});
