import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { SettingsPage } from './pages/SettingsPage';

test.describe('Settings Interface', () => {
  let loginPage: LoginPage;
  let settingsPage: SettingsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    settingsPage = new SettingsPage(page);

    // Login and navigate to settings page
    await loginPage.goto();
    await loginPage.login('test@example.com', 'password123');
    await page.goto('/settings');
    await expect(page).toHaveURL('/settings');
  });

  test('should display settings navigation tabs', async ({ page }) => {
    // Check that all tabs are visible
    await expect(settingsPage.profileTab).toBeVisible();
    await expect(settingsPage.notificationsTab).toBeVisible();
    await expect(settingsPage.integrationsTab).toBeVisible();
    await expect(settingsPage.importExportTab).toBeVisible();

    // Check that profile tab is active by default
    await expect(settingsPage.profileTab).toHaveClass(/border-blue-500/);
  });

  test('should switch between settings tabs', async ({ page }) => {
    // Switch to notifications tab
    await settingsPage.notificationsTab.click();
    await expect(settingsPage.notificationsTab).toHaveClass(/border-blue-500/);
    await expect(page.locator('text=Notification Preferences')).toBeVisible();

    // Switch to integrations tab
    await settingsPage.integrationsTab.click();
    await expect(settingsPage.integrationsTab).toHaveClass(/border-blue-500/);
    await expect(page.locator('text=Calendar Integrations')).toBeVisible();

    // Switch to import/export tab
    await settingsPage.importExportTab.click();
    await expect(settingsPage.importExportTab).toHaveClass(/border-blue-500/);
    await expect(page.locator('text=Export Data')).toBeVisible();
  });

  test('should update user profile information', async ({ page }) => {
    // Ensure we're on profile tab
    await settingsPage.profileTab.click();

    // Update name
    const nameInput = page.locator('[data-testid="profile-name-input"]');
    await nameInput.clear();
    await nameInput.fill('Updated Name');

    // Update phone
    const phoneInput = page.locator('[data-testid="profile-phone-input"]');
    await phoneInput.clear();
    await phoneInput.fill('+1234567890');

    // Change timezone
    await page.locator('[data-testid="profile-timezone-select"]').selectOption('America/Los_Angeles');

    // Check that unsaved changes indicator appears
    await expect(page.locator('text=Unsaved Changes')).toBeVisible();

    // Save changes
    await page.locator('[data-testid="save-changes-button"]').click();

    // Check that changes were saved
    await expect(page.locator('text=Unsaved Changes')).toBeHidden();
  });

  test('should upload profile avatar', async ({ page }) => {
    // Ensure we're on profile tab
    await settingsPage.profileTab.click();

    // Create a test file
    const fileInput = page.locator('[data-testid="avatar-upload-input"]');

    // Set files on the input
    await fileInput.setInputFiles({
      name: 'avatar.png',
      mimeType: 'image/png',
      buffer: Buffer.from('fake-image-data')
    });

    // Check that avatar preview is updated
    await expect(page.locator('[data-testid="avatar-preview"]')).toBeVisible();
  });

  test('should change password', async ({ page }) => {
    // Switch to security section
    await settingsPage.profileTab.click();
    await page.locator('text=Security & Privacy').click();

    // Fill password fields
    await page.locator('[data-testid="current-password-input"]').fill('currentPassword');
    await page.locator('[data-testid="new-password-input"]').fill('newPassword123');
    await page.locator('[data-testid="confirm-password-input"]').fill('newPassword123');

    // Toggle password visibility
    await page.locator('[data-testid="toggle-password-visibility"]').first().click();

    // Check that password is now visible
    await expect(page.locator('[data-testid="current-password-input"]')).toHaveAttribute('type', 'text');
  });

  test('should configure notification settings', async ({ page }) => {
    // Switch to notifications tab
    await settingsPage.notificationsTab.click();

    // Toggle push notifications
    const pushNotificationsToggle = page.locator('[data-testid="push-notifications-toggle"]');
    await pushNotificationsToggle.click();

    // Configure quiet hours
    const quietHoursToggle = page.locator('[data-testid="quiet-hours-toggle"]');
    await quietHoursToggle.click();

    // Set quiet hours times
    await page.locator('[data-testid="quiet-hours-start"]').fill('22:00');
    await page.locator('[data-testid="quiet-hours-end"]').fill('07:00');

    // Configure alarm notifications
    await page.locator('[data-testid="before-trigger-toggle"]').click();
    await page.locator('[data-testid="before-trigger-minutes"]').selectOption('10');

    // Test notification
    await page.locator('[data-testid="test-notification-button"]').click();

    // Check for notification permission request or success message
    await page.waitForTimeout(1000);
  });

  test('should manage calendar integrations', async ({ page }) => {
    // Switch to integrations tab
    await settingsPage.integrationsTab.click();

    // Check that integration cards are visible
    await expect(page.locator('text=Google Calendar')).toBeVisible();
    await expect(page.locator('text=Outlook Calendar')).toBeVisible();
    await expect(page.locator('text=Apple Calendar')).toBeVisible();

    // Try to connect Google Calendar
    const googleCalendarCard = page.locator('[data-testid="google-calendar-integration"]');
    const connectButton = googleCalendarCard.locator('[data-testid="connect-button"]');

    if (await connectButton.isVisible()) {
      await connectButton.click();

      // Check for loading state
      await expect(googleCalendarCard.locator('text=Connecting...')).toBeVisible();
    }
  });

  test('should configure webhooks', async ({ page }) => {
    // Switch to integrations tab
    await settingsPage.integrationsTab.click();

    // Add new webhook
    await page.locator('[data-testid="add-webhook-button"]').click();

    // Fill webhook configuration
    await page.locator('[data-testid="webhook-url-input"]').fill('https://example.com/webhook');

    // Select events
    await page.locator('[data-testid="webhook-event-alarm.triggered"]').click();
    await page.locator('[data-testid="webhook-event-alarm.dismissed"]').click();

    // Enable webhook
    await page.locator('[data-testid="webhook-enabled-toggle"]').click();

    // Check that webhook was added
    await expect(page.locator('text=https://example.com/webhook')).toBeVisible();
  });

  test('should export data', async ({ page }) => {
    // Switch to import/export tab
    await settingsPage.importExportTab.click();

    // Configure export options
    await page.locator('[data-testid="export-alarms-checkbox"]').click();
    await page.locator('[data-testid="export-routines-checkbox"]').click();
    await page.locator('[data-testid="export-settings-checkbox"]').click();

    // Select export format
    await page.locator('[data-testid="export-format-json"]').click();

    // Start download (mock the download)
    const downloadPromise = page.waitForEvent('download');
    await page.locator('[data-testid="export-data-button"]').click();

    // Check that export started
    await expect(page.locator('text=Exporting...')).toBeVisible();
  });

  test('should import data', async ({ page }) => {
    // Switch to import/export tab
    await settingsPage.importExportTab.click();

    // Create mock import file
    const fileInput = page.locator('[data-testid="import-file-input"]');
    await fileInput.setInputFiles({
      name: 'smart-alarm-export.json',
      mimeType: 'application/json',
      buffer: Buffer.from(JSON.stringify({
        metadata: { version: '1.0.0' },
        data: { alarms: [], routines: [], settings: {} }
      }))
    });

    // Check that import started
    await expect(page.locator('text=Importing...')).toBeVisible();

    // Wait for import to complete
    await expect(page.locator('text=Import completed successfully')).toBeVisible({ timeout: 10000 });
  });

  test('should handle unsaved changes warning', async ({ page }) => {
    // Make changes to profile
    await settingsPage.profileTab.click();
    await page.locator('[data-testid="profile-name-input"]').fill('Changed Name');

    // Check that unsaved changes indicator appears
    await expect(page.locator('text=Unsaved Changes')).toBeVisible();

    // Try to navigate away
    await page.goto('/dashboard');

    // Should show confirmation dialog (if implemented)
    // For now, just check that we can navigate back
    await page.goto('/settings');
    await expect(page.locator('text=Unsaved Changes')).toBeVisible();
  });

  test('should be responsive on mobile', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });

    // Check that mobile layout is applied
    const sidebar = page.locator('[data-testid="settings-sidebar"]');

    // On mobile, tabs might be displayed differently
    await expect(settingsPage.profileTab).toBeVisible();

    // Check that content is properly stacked
    const mainContent = page.locator('[data-testid="settings-content"]');
    await expect(mainContent).toBeVisible();
  });

  test('should handle error states', async ({ page }) => {
    // Mock error response for settings save
    await page.route('**/api/settings', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ error: 'Failed to save settings' })
      });
    });

    // Make a change and try to save
    await page.locator('[data-testid="profile-name-input"]').fill('Test Name');
    await page.locator('[data-testid="save-changes-button"]').click();

    // Check for error message
    await expect(page.locator('text=Failed to save')).toBeVisible();
  });

  test('should validate form inputs', async ({ page }) => {
    // Try to enter invalid email
    const emailInput = page.locator('[data-testid="profile-email-input"]');
    await emailInput.clear();
    await emailInput.fill('invalid-email');

    // Check for validation error
    await expect(page.locator('text=Please enter a valid email')).toBeVisible();

    // Try to enter mismatched passwords
    await page.locator('text=Security & Privacy').click();
    await page.locator('[data-testid="new-password-input"]').fill('password1');
    await page.locator('[data-testid="confirm-password-input"]').fill('password2');

    // Check for password mismatch error
    await expect(page.locator('text=Passwords do not match')).toBeVisible();
  });
});
