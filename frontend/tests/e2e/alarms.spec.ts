import { test, expect } from '@playwright/test';

test.describe('Alarm Management', () => {
  test.beforeEach(async ({ page }) => {
    // Set up authenticated state
    await page.goto('/');
    await page.evaluate(() => {
      const testAuthState = {
        state: {
          user: {
            id: 'test-user-id',
            email: 'testuser@example.com',
            name: 'Test User',
            role: 'user'
          },
          token: 'test-jwt-token',
          isAuthenticated: true,
          refreshToken: 'test-refresh-token'
        },
        version: 0
      };
      
      localStorage.setItem('smart-alarm-auth', JSON.stringify(testAuthState));
    });
    
    await page.reload();
  });

  test('should display alarm list', async ({ page }) => {
    await page.goto('/alarms');
    
    // Should show alarms page
    await expect(page.locator('h1, h2, [data-testid="page-title"]')).toContainText(/alarms|Alarms/i);
    
    // Should show alarm list or empty state
    const hasAlarms = await page.locator('[data-testid="alarm-item"], .alarm-card, .alarm-list-item').count() > 0;
    const hasEmptyState = await page.locator('text="No alarms", text="Create your first alarm", .empty-state').isVisible();
    
    expect(hasAlarms || hasEmptyState).toBe(true);
  });

  test('should create new alarm', async ({ page }) => {
    await page.goto('/alarms');
    
    // Click create alarm button
    await page.click('button:has-text("Create"), button:has-text("Add"), button:has-text("New Alarm"), [data-testid="create-alarm"]');
    
    // Should show create alarm form/modal
    await expect(page.locator('form, .modal, .dialog')).toBeVisible();
    
    // Fill in alarm details
    await page.fill('input[name="name"], input[placeholder*="name"]', 'Test E2E Alarm');
    await page.fill('input[type="time"], input[name="time"]', '08:30');
    
    // Select days (if available)
    const mondayCheckbox = page.locator('input[type="checkbox"][value="monday"], label:has-text("Monday") input');
    if (await mondayCheckbox.isVisible()) {
      await mondayCheckbox.check();
    }
    
    // Save the alarm
    await page.click('button[type="submit"], button:has-text("Save"), button:has-text("Create")');
    
    // Should show success message or return to alarm list
    await expect(page.locator('text="success", text="created", .success, .alert-success')).toBeVisible({ timeout: 10000 });
    
    // Should see the new alarm in the list
    await expect(page.locator('text="Test E2E Alarm"')).toBeVisible();
  });

  test('should edit existing alarm', async ({ page }) => {
    await page.goto('/alarms');
    
    // Find first alarm and click edit
    const firstAlarm = page.locator('[data-testid="alarm-item"], .alarm-card').first();
    await expect(firstAlarm).toBeVisible();
    
    // Click edit button (could be in various places)
    await firstAlarm.locator('button:has-text("Edit"), .edit-button, [data-testid="edit-alarm"]').click();
    
    // Should show edit form
    await expect(page.locator('form, .modal, .dialog')).toBeVisible();
    
    // Modify alarm name
    const nameInput = page.locator('input[name="name"], input[placeholder*="name"]');
    await nameInput.clear();
    await nameInput.fill('Updated E2E Alarm');
    
    // Save changes
    await page.click('button[type="submit"], button:has-text("Save"), button:has-text("Update")');
    
    // Should show success message
    await expect(page.locator('text="success", text="updated", .success, .alert-success')).toBeVisible({ timeout: 10000 });
    
    // Should see updated name
    await expect(page.locator('text="Updated E2E Alarm"')).toBeVisible();
  });

  test('should toggle alarm on/off', async ({ page }) => {
    await page.goto('/alarms');
    
    // Find first alarm
    const firstAlarm = page.locator('[data-testid="alarm-item"], .alarm-card').first();
    await expect(firstAlarm).toBeVisible();
    
    // Find toggle switch
    const toggle = firstAlarm.locator('input[type="checkbox"], .toggle, .switch');
    await expect(toggle).toBeVisible();
    
    // Get initial state
    const isInitiallyChecked = await toggle.isChecked();
    
    // Toggle the alarm
    await toggle.click();
    
    // Should change state
    const isAfterToggle = await toggle.isChecked();
    expect(isAfterToggle).toBe(!isInitiallyChecked);
    
    // Should show feedback (success message or visual change)
    // This depends on your implementation
  });

  test('should delete alarm', async ({ page }) => {
    await page.goto('/alarms');
    
    // Find first alarm
    const firstAlarm = page.locator('[data-testid="alarm-item"], .alarm-card').first();
    await expect(firstAlarm).toBeVisible();
    
    // Get alarm name for verification
    const alarmName = await firstAlarm.locator('h3, .alarm-name, [data-testid="alarm-name"]').textContent();
    
    // Click delete button
    await firstAlarm.locator('button:has-text("Delete"), .delete-button, [data-testid="delete-alarm"]').click();
    
    // Should show confirmation dialog
    await expect(page.locator('.modal, .dialog, .confirm')).toBeVisible();
    
    // Confirm deletion
    await page.click('button:has-text("Delete"), button:has-text("Confirm"), button:has-text("Yes")');
    
    // Should show success message
    await expect(page.locator('text="success", text="deleted", .success, .alert-success')).toBeVisible({ timeout: 10000 });
    
    // Alarm should no longer be in the list
    if (alarmName) {
      await expect(page.locator(`text="${alarmName}"`)).not.toBeVisible();
    }
  });

  test('should filter alarms', async ({ page }) => {
    await page.goto('/alarms');
    
    // Check if filter functionality exists
    const filterInput = page.locator('input[placeholder*="filter"], input[placeholder*="search"], [data-testid="alarm-filter"]');
    
    if (await filterInput.isVisible()) {
      // Type in filter
      await filterInput.fill('Morning');
      
      // Should filter results
      const alarmItems = page.locator('[data-testid="alarm-item"], .alarm-card');
      const count = await alarmItems.count();
      
      // Check that visible items contain the filter term
      for (let i = 0; i < count; i++) {
        const item = alarmItems.nth(i);
        await expect(item).toContainText(/morning/i);
      }
      
      // Clear filter
      await filterInput.clear();
    }
  });

  test('should show alarm details', async ({ page }) => {
    await page.goto('/alarms');
    
    // Find first alarm and click to view details
    const firstAlarm = page.locator('[data-testid="alarm-item"], .alarm-card').first();
    await expect(firstAlarm).toBeVisible();
    
    // Click on alarm (not on action buttons)
    await firstAlarm.locator('h3, .alarm-name, [data-testid="alarm-name"]').click();
    
    // Should show alarm details (either in modal or separate page)
    await expect(page.locator('.modal, .dialog, [data-testid="alarm-details"]')).toBeVisible();
    
    // Should show alarm information
    await expect(page.locator('text="Time:", text="Name:", text="Days:"')).toBeVisible();
  });

  test('should handle offline mode for alarms', async ({ page }) => {
    await page.goto('/alarms');
    
    // Simulate offline mode
    await page.context().setOffline(true);
    
    // Try to create an alarm
    await page.click('button:has-text("Create"), button:has-text("Add"), [data-testid="create-alarm"]');
    
    await page.fill('input[name="name"]', 'Offline Test Alarm');
    await page.fill('input[type="time"]', '09:15');
    await page.click('button[type="submit"], button:has-text("Save")');
    
    // Should show offline indicator or queue message
    await expect(page.locator('text="offline", text="queue", text="sync", .offline')).toBeVisible({ timeout: 10000 });
    
    // Go back online
    await page.context().setOffline(false);
    
    // Should sync and show the alarm
    await page.reload();
    await expect(page.locator('text="Offline Test Alarm"')).toBeVisible({ timeout: 15000 });
  });

  test('should validate alarm form inputs', async ({ page }) => {
    await page.goto('/alarms');
    
    // Click create alarm
    await page.click('button:has-text("Create"), button:has-text("Add"), [data-testid="create-alarm"]');
    
    // Try to submit without required fields
    await page.click('button[type="submit"], button:has-text("Save")');
    
    // Should show validation errors
    await expect(page.locator('.error, .invalid, text="required"')).toBeVisible();
    
    // Fill invalid time
    await page.fill('input[name="name"]', 'Test Alarm');
    await page.fill('input[type="time"]', ''); // Clear time
    
    await page.click('button[type="submit"]');
    
    // Should show time validation error
    await expect(page.locator('text="time", text="required", .error')).toBeVisible();
  });

  test('should show upcoming alarms', async ({ page }) => {
    await page.goto('/');
    
    // Should show upcoming alarms section
    await expect(page.locator('text="upcoming", text="next", text="today"')).toBeVisible();
    
    // If there are upcoming alarms, they should be displayed
    const upcomingSection = page.locator('[data-testid="upcoming-alarms"], .upcoming-alarms');
    if (await upcomingSection.isVisible()) {
      await expect(upcomingSection.locator('.alarm-item, .upcoming-alarm')).toBeVisible();
    }
  });
});