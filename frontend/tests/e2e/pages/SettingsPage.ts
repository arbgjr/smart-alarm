import { Page, Locator } from '@playwright/test';

export class SettingsPage {
  readonly page: Page;

  // Navigation Tabs
  readonly profileTab: Locator;
  readonly notificationsTab: Locator;
  readonly integrationsTab: Locator;
  readonly importExportTab: Locator;

  // Profile Section
  readonly profileNameInput: Locator;
  readonly profileEmailInput: Locator;
  readonly profilePhoneInput: Locator;
  readonly timezoneSelect: Locator;
  readonly avatarUpload: Locator;

  // Security Section
  readonly currentPasswordInput: Locator;
  readonly newPasswordInput: Locator;
  readonly confirmPasswordInput: Locator;
  readonly twoFactorToggle: Locator;

  // Notifications Section
  readonly pushNotificationsToggle: Locator;
  readonly emailNotificationsToggle: Locator;
  readonly quietHoursToggle: Locator;
  readonly testNotificationButton: Locator;

  // Integrations Section
  readonly googleCalendarCard: Locator;
  readonly outlookCalendarCard: Locator;
  readonly addWebhookButton: Locator;

  // Import/Export Section
  readonly exportAlarmsCheckbox: Locator;
  readonly exportRoutinesCheckbox: Locator;
  readonly exportSettingsCheckbox: Locator;
  readonly exportFormatSelect: Locator;
  readonly exportButton: Locator;
  readonly importFileInput: Locator;

  // Common Elements
  readonly saveChangesButton: Locator;
  readonly unsavedChangesIndicator: Locator;

  constructor(page: Page) {
    this.page = page;

    // Navigation Tabs
    this.profileTab = page.locator('[data-testid="profile-tab"]');
    this.notificationsTab = page.locator('[data-testid="notifications-tab"]');
    this.integrationsTab = page.locator('[data-testid="integrations-tab"]');
    this.importExportTab = page.locator('[data-testid="import-export-tab"]');

    // Profile Section
    this.profileNameInput = page.locator('[data-testid="profile-name-input"]');
    this.profileEmailInput = page.locator('[data-testid="profile-email-input"]');
    this.profilePhoneInput = page.locator('[data-testid="profile-phone-input"]');
    this.timezoneSelect = page.locator('[data-testid="profile-timezone-select"]');
    this.avatarUpload = page.locator('[data-testid="avatar-upload-input"]');

    // Security Section
    this.currentPasswordInput = page.locator('[data-testid="current-password-input"]');
    this.newPasswordInput = page.locator('[data-testid="new-password-input"]');
    this.confirmPasswordInput = page.locator('[data-testid="confirm-password-input"]');
    this.twoFactorToggle = page.locator('[data-testid="two-factor-toggle"]');

    // Notifications Section
    this.pushNotificationsToggle = page.locator('[data-testid="push-notifications-toggle"]');
    this.emailNotificationsToggle = page.locator('[data-testid="email-notifications-toggle"]');
    this.quietHoursToggle = page.locator('[data-testid="quiet-hours-toggle"]');
    this.testNotificationButton = page.locator('[data-testid="test-notification-button"]');

    // Integrations Section
    this.googleCalendarCard = page.locator('[data-testid="google-calendar-integration"]');
    this.outlookCalendarCard = page.locator('[data-testid="outlook-calendar-integration"]');
    this.addWebhookButton = page.locator('[data-testid="add-webhook-button"]');

    // Import/Export Section
    this.exportAlarmsCheckbox = page.locator('[data-testid="export-alarms-checkbox"]');
    this.exportRoutinesCheckbox = page.locator('[data-testid="export-routines-checkbox"]');
    this.exportSettingsCheckbox = page.locator('[data-testid="export-settings-checkbox"]');
    this.exportFormatSelect = page.locator('[data-testid="export-format-select"]');
    this.exportButton = page.locator('[data-testid="export-data-button"]');
    this.importFileInput = page.locator('[data-testid="import-file-input"]');

    // Common Elements
    this.saveChangesButton = page.locator('[data-testid="save-changes-button"]');
    this.unsavedChangesIndicator = page.locator('[data-testid="unsaved-changes-indicator"]');
  }

  async goto() {
    await this.page.goto('/settings');
  }

  async switchToProfileTab() {
    await this.profileTab.click();
  }

  async switchToNotificationsTab() {
    await this.notificationsTab.click();
  }

  async switchToIntegrationsTab() {
    await this.integrationsTab.click();
  }

  async switchToImportExportTab() {
    await this.importExportTab.click();
  }

  async updateProfile(data: {
    name?: string;
    email?: string;
    phone?: string;
    timezone?: string;
  }) {
    if (data.name) {
      await this.profileNameInput.clear();
      await this.profileNameInput.fill(data.name);
    }

    if (data.email) {
      await this.profileEmailInput.clear();
      await this.profileEmailInput.fill(data.email);
    }

    if (data.phone) {
      await this.profilePhoneInput.clear();
      await this.profilePhoneInput.fill(data.phone);
    }

    if (data.timezone) {
      await this.timezoneSelect.selectOption(data.timezone);
    }
  }

  async changePassword(currentPassword: string, newPassword: string) {
    await this.currentPasswordInput.fill(currentPassword);
    await this.newPasswordInput.fill(newPassword);
    await this.confirmPasswordInput.fill(newPassword);
  }

  async configureNotifications(options: {
    pushNotifications?: boolean;
    emailNotifications?: boolean;
    quietHours?: boolean;
  }) {
    if (options.pushNotifications !== undefined) {
      if (options.pushNotifications) {
        await this.pushNotificationsToggle.check();
      } else {
        await this.pushNotificationsToggle.uncheck();
      }
    }

    if (options.emailNotifications !== undefined) {
      if (options.emailNotifications) {
        await this.emailNotificationsToggle.check();
      } else {
        await this.emailNotificationsToggle.uncheck();
      }
    }

    if (options.quietHours !== undefined) {
      if (options.quietHours) {
        await this.quietHoursToggle.check();
      } else {
        await this.quietHoursToggle.uncheck();
      }
    }
  }

  async testNotification() {
    await this.testNotificationButton.click();
  }

  async connectGoogleCalendar() {
    const connectButton = this.googleCalendarCard.locator('[data-testid="connect-button"]');
    await connectButton.click();
  }

  async addWebhook(url: string, events: string[]) {
    await this.addWebhookButton.click();

    await this.page.locator('[data-testid="webhook-url-input"]').fill(url);

    for (const event of events) {
      await this.page.locator(`[data-testid="webhook-event-${event}"]`).check();
    }

    await this.page.locator('[data-testid="webhook-enabled-toggle"]').check();
  }

  async configureExport(options: {
    alarms?: boolean;
    routines?: boolean;
    settings?: boolean;
    format?: string;
  }) {
    if (options.alarms !== undefined) {
      if (options.alarms) {
        await this.exportAlarmsCheckbox.check();
      } else {
        await this.exportAlarmsCheckbox.uncheck();
      }
    }

    if (options.routines !== undefined) {
      if (options.routines) {
        await this.exportRoutinesCheckbox.check();
      } else {
        await this.exportRoutinesCheckbox.uncheck();
      }
    }

    if (options.settings !== undefined) {
      if (options.settings) {
        await this.exportSettingsCheckbox.check();
      } else {
        await this.exportSettingsCheckbox.uncheck();
      }
    }

    if (options.format) {
      await this.exportFormatSelect.selectOption(options.format);
    }
  }

  async exportData() {
    await this.exportButton.click();
  }

  async importData(filePath: string) {
    await this.importFileInput.setInputFiles(filePath);
  }

  async saveChanges() {
    await this.saveChangesButton.click();
  }
}
