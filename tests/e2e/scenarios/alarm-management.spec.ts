import { test, expect } from '@playwright/test';

test.describe('Alarm Management', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to alarms page
    await page.goto('/alarms');

    // Wait for page to load
    await expect(page.locator('[data-testid="alarms-page"]')).toBeVisible();
  });

  test('should display alarms list', async ({ page }) => {
    // Check if alarms list is visible
    await expect(page.locator('[data-testid="alarms-list"]')).toBeVisible();

    // Check for create alarm button
    await expect(page.locator('[data-testid="create-alarm-button"]')).toBeVisible();
  });

  test('should create a new alarm', async ({ page }) => {
    // Click create alarm button
    await page.click('[data-testid="create-alarm-button"]');

    // Fill alarm form
    await page.fill('[data-testid="alarm-name-input"]', 'Test Alarm');
    await page.fill('[data-testid="alarm-time-input"]', '09:00');

    // Select days
    await page.check('[data-testid="day-monday"]');
    await page.check('[data-testid="day-tuesday"]');
    await page.check('[data-testid="day-wednesday"]');
    await page.check('[data-testid="day-thursday"]');
    await page.check('[data-testid="day-friday"]');

    // Save alarm
    await page.click('[data-testid="save-alarm-button"]');

    // Verify alarm was created
    await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
    await expect(page.locator('text=Test Alarm')).toBeVisible();
  });

  test('should edit an existing alarm', async ({ page }) => {
    // Assume there's at least one alarm
    const firstAlarm = page.locator('[data-testid="alarm-item"]').first();
    await expect(firstAlarm).toBeVisible();

    // Click edit button
    await firstAlarm.locator('[data-testid="edit-alarm-button"]').click();

    // Modify alarm name
    await page.fill('[data-testid="alarm-name-input"]', 'Updated Test Alarm');

    // Save changes
    await page.click('[data-testid="save-alarm-button"]');

    // Verify changes were saved
    await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
    await expect(page.locator('text=Updated Test Alarm')).toBeVisible();
  });

  test('should delete an alarm', async ({ page }) => {
    // Assume there's at least one alarm
    const firstAlarm = page.locator('[data-testid="alarm-item"]').first();
    await expect(firstAlarm).toBeVisible();

    // Click delete button
    await firstAlarm.locator('[data-testid="delete-alarm-button"]').click();

    // Confirm deletion
    await page.click('[data-testid="confirm-delete-button"]');

    // Verify alarm was deleted
    await expect(page.locator('[data-testid="success-message"]')).toBeVisible();
  });

  test('should toggle alarm on/off', async ({ page }) => {
    // Assume there's at least one alarm
    const firstAlarm = page.locator('[data-testid="alarm-item"]').first();
    await expect(firstAlarm).toBeVisible();

    // Get initial state
    const toggleButton = firstAlarm.locator('[data-testid="alarm-toggle"]');
    const initialState = await toggleButton.isChecked();

    // Toggle alarm
    await toggleButton.click();

    // Verify state changed
    const newState = await toggleButton.isChecked();
    expect(newState).toBe(!initialState);
  });

  test('should validate alarm form', async ({ page }) => {
    // Click create alarm button
    await page.click('[data-testid="create-alarm-button"]');

    // Try to save without filling required fields
    await page.click('[data-testid="save-alarm-button"]');

    // Verify validation errors
    await expect(page.locator('[data-testid="name-error"]')).toBeVisible();
    await expect(page.locator('[data-testid="time-error"]')).toBeVisible();
  });
});
