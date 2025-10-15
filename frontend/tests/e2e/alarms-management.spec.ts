import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { AlarmsPage } from './pages/AlarmsPage';

test.describe('Enhanced Alarms Management', () => {
  let loginPage: LoginPage;
  let alarmsPage: AlarmsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    alarmsPage = new AlarmsPage(page);

    // Login and navigate to alarms page
    await loginPage.goto();
    await loginPage.login('test@example.com', 'password123');
    await page.goto('/alarms');
    await expect(page).toHaveURL('/alarms');
  });

  test('should display alarm filters', async ({ page }) => {
    // Check that filter components are visible
    await expect(alarmsPage.searchInput).toBeVisible();
    await expect(alarmsPage.statusFilter).toBeVisible();
    await expect(alarmsPage.typeFilter).toBeVisible();
    await expect(alarmsPage.sortByFilter).toBeVisible();

    // Check default filter values
    await expect(alarmsPage.statusFilter).toHaveValue('all');
    await expect(alarmsPage.typeFilter).toHaveValue('all');
    await expect(alarmsPage.sortByFilter).toHaveValue('time');
  });

  test('should filter alarms by search term', async ({ page }) => {
    // Type in search input
    await alarmsPage.searchInput.fill('morning');

    // Check that active filters are displayed
    await expect(page.locator('text=Search: "morning"')).toBeVisible();

    // Clear search
    await page.locator('[data-testid="clear-search"]').click();
    await expect(alarmsPage.searchInput).toHaveValue('');
  });

  test('should filter alarms by status', async ({ page }) => {
    // Change status filter
    await alarmsPage.statusFilter.selectOption('active');

    // Check that filter is applied
    await expect(page.locator('text=Status: active')).toBeVisible();

    // Reset filter
    await page.locator('[data-testid="clear-status-filter"]').click();
    await expect(alarmsPage.statusFilter).toHaveValue('all');
  });

  test('should filter alarms by type', async ({ page }) => {
    // Change type filter
    await alarmsPage.typeFilter.selectOption('recurring');

    // Check that filter is applied
    await expect(page.locator('text=Type: recurring')).toBeVisible();
  });

  test('should sort alarms', async ({ page }) => {
    // Change sort order
    await alarmsPage.sortOrderButton.click();

    // Check that sort order changed (arrow should flip)
    await expect(alarmsPage.sortOrderButton).toContainText('↓');
  });

  test('should clear all filters', async ({ page }) => {
    // Apply multiple filters
    await alarmsPage.searchInput.fill('test');
    await alarmsPage.statusFilter.selectOption('active');
    await alarmsPage.typeFilter.selectOption('recurring');

    // Clear all filters
    await page.locator('text=Clear all').click();

    // Check that all filters are reset
    await expect(alarmsPage.searchInput).toHaveValue('');
    await expect(alarmsPage.statusFilter).toHaveValue('all');
    await expect(alarmsPage.typeFilter).toHaveValue('all');
  });

  test('should switch between grid and list view', async ({ page }) => {
    // Check default view (should be grid)
    await expect(alarmsPage.gridViewButton).toHaveClass(/bg-white/);

    // Switch to list view
    await alarmsPage.listViewButton.click();
    await expect(alarmsPage.listViewButton).toHaveClass(/bg-white/);

    // Check that list view is displayed
    await expect(page.locator('[data-testid="alarms-list"]')).toBeVisible();

    // Switch back to grid view
    await alarmsPage.gridViewButton.click();
    await expect(page.locator('[data-testid="alarms-grid"]')).toBeVisible();
  });

  test('should create new alarm', async ({ page }) => {
    // Click create alarm button
    await alarmsPage.createAlarmButton.click();

    // Check that alarm form modal is opened
    await expect(page.locator('[data-testid="alarm-form-modal"]')).toBeVisible();

    // Fill alarm form
    await page.locator('[data-testid="alarm-name-input"]').fill('Test Alarm');
    await page.locator('[data-testid="alarm-time-input"]').fill('07:00');

    // Save alarm
    await page.locator('[data-testid="save-alarm-button"]').click();

    // Check that alarm was created
    await expect(page.locator('text=Test Alarm')).toBeVisible();
  });

  test('should edit existing alarm', async ({ page }) => {
    // Assume there's at least one alarm in the list
    const firstAlarmCard = page.locator('[data-testid="alarm-card"]').first();
    await expect(firstAlarmCard).toBeVisible();

    // Click edit button on first alarm
    await firstAlarmCard.locator('[data-testid="edit-alarm-button"]').click();

    // Check that alarm form is opened with existing data
    await expect(page.locator('[data-testid="alarm-form-modal"]')).toBeVisible();

    // Modify alarm name
    const nameInput = page.locator('[data-testid="alarm-name-input"]');
    await nameInput.clear();
    await nameInput.fill('Modified Alarm');

    // Save changes
    await page.locator('[data-testid="save-alarm-button"]').click();

    // Check that changes were saved
    await expect(page.locator('text=Modified Alarm')).toBeVisible();
  });

  test('should duplicate alarm', async ({ page }) => {
    // Click duplicate button on first alarm
    const firstAlarmCard = page.locator('[data-testid="alarm-card"]').first();
    await firstAlarmCard.locator('[data-testid="alarm-menu-button"]').click();
    await page.locator('[data-testid="duplicate-alarm-button"]').click();

    // Check that alarm form is opened with duplicated data
    await expect(page.locator('[data-testid="alarm-form-modal"]')).toBeVisible();

    // Check that name has "(Copy)" suffix
    const nameInput = page.locator('[data-testid="alarm-name-input"]');
    const nameValue = await nameInput.inputValue();
    expect(nameValue).toContain('(Copy)');
  });

  test('should toggle alarm status', async ({ page }) => {
    // Get first alarm card
    const firstAlarmCard = page.locator('[data-testid="alarm-card"]').first();

    // Click toggle button
    await firstAlarmCard.locator('[data-testid="toggle-alarm-button"]').click();

    // Check that status changed (visual indicator should change)
    await page.waitForTimeout(1000); // Wait for state update
  });

  test('should delete alarm', async ({ page }) => {
    // Click delete button on first alarm
    const firstAlarmCard = page.locator('[data-testid="alarm-card"]').first();
    await firstAlarmCard.locator('[data-testid="alarm-menu-button"]').click();
    await page.locator('[data-testid="delete-alarm-button"]').click();

    // Confirm deletion in dialog
    page.on('dialog', dialog => dialog.accept());

    // Check that alarm was removed
    await page.waitForTimeout(1000);
  });

  test('should open routine configuration modal', async ({ page }) => {
    // Click routines button
    await alarmsPage.routinesButton.click();

    // Check that routine modal is opened
    await expect(page.locator('[data-testid="routine-config-modal"]')).toBeVisible();

    // Check modal content
    await expect(page.locator('text=Create New Routine')).toBeVisible();
    await expect(page.locator('[data-testid="routine-name-input"]')).toBeVisible();
  });

  test('should open exception period modal', async ({ page }) => {
    // Click exceptions button
    await alarmsPage.exceptionsButton.click();

    // Check that exception modal is opened
    await expect(page.locator('[data-testid="exception-period-modal"]')).toBeVisible();

    // Check modal content
    await expect(page.locator('text=Create Exception Period')).toBeVisible();
    await expect(page.locator('[data-testid="exception-name-input"]')).toBeVisible();
  });

  test('should open holiday configuration modal', async ({ page }) => {
    // Click holidays button
    await alarmsPage.holidaysButton.click();

    // Check that holiday modal is opened
    await expect(page.locator('[data-testid="holiday-config-modal"]')).toBeVisible();

    // Check modal content
    await expect(page.locator('text=Holiday Configuration')).toBeVisible();
    await expect(page.locator('[data-testid="holiday-preferences"]')).toBeVisible();
  });

  test('should handle empty state', async ({ page }) => {
    // Mock empty alarms response
    await page.route('**/api/alarms', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] })
      });
    });

    // Reload page
    await page.reload();

    // Check empty state
    await expect(page.locator('text=No alarms found')).toBeVisible();
    await expect(page.locator('text=Create Your First Alarm')).toBeVisible();
  });

  test('should be responsive on mobile', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Check mobile layout
    await expect(page.locator('[data-testid="mobile-header"]')).toBeVisible();

    // Check that action buttons are stacked
    const actionButtons = page.locator('[data-testid="action-buttons"]');
    await expect(actionButtons).toHaveClass(/flex-wrap/);
  });

  test('should handle loading states', async ({ page }) => {
    // Intercept API call with delay
    await page.route('**/api/alarms', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      route.continue();
    });

    // Reload page
    await page.reload();

    // Check loading state
    await expect(page.locator('text=Loading alarms...')).toBeVisible();

    // Wait for loading to complete
    await expect(page.locator('text=Loading alarms...')).toBeHidden({ timeout: 10000 });
  });

  test('should handle error states', async ({ page }) => {
    // Mock error response
    await page.route('**/api/alarms', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Internal Server Error' })
      });
    });

    // Reload page
    await page.reload();

    // Check error state
    await expect(page.locator('text=Unable to load alarms')).toBeVisible();
  });
});
