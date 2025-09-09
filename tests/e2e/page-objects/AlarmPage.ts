import { Page, Locator, expect } from '@playwright/test';

export class AlarmPage {
  readonly page: Page;
  
  // Main page elements
  readonly createAlarmButton: Locator;
  readonly searchInput: Locator;
  readonly filterDropdown: Locator;
  readonly alarmsList: Locator;
  readonly emptyState: Locator;
  
  // Modal elements
  readonly createAlarmModal: Locator;
  readonly editAlarmModal: Locator;
  readonly confirmDeleteModal: Locator;
  
  // Form elements
  readonly alarmNameInput: Locator;
  readonly alarmTimeInput: Locator;
  readonly daysOfWeekCheckboxes: Locator;
  readonly saveAlarmButton: Locator;
  readonly cancelButton: Locator;
  
  // Validation elements
  readonly nameValidationError: Locator;
  readonly timeValidationError: Locator;
  
  // Bulk operations
  readonly bulkActionsBar: Locator;
  readonly selectedCount: Locator;
  readonly bulkEnableButton: Locator;
  readonly bulkDisableButton: Locator;
  readonly bulkDeleteButton: Locator;
  readonly selectAllCheckbox: Locator;

  constructor(page: Page) {
    this.page = page;
    
    // Main page elements
    this.createAlarmButton = page.getByRole('button', { name: /create alarm|novo alarme/i });
    this.searchInput = page.getByPlaceholder(/search alarms|pesquisar alarmes/i);
    this.filterDropdown = page.getByLabel(/filter by status|filtrar por status/i);
    this.alarmsList = page.getByTestId('alarms-list');
    this.emptyState = page.getByTestId('empty-state');
    
    // Modal elements
    this.createAlarmModal = page.getByRole('dialog', { name: /create alarm|criar alarme/i });
    this.editAlarmModal = page.getByRole('dialog', { name: /edit alarm|editar alarme/i });
    this.confirmDeleteModal = page.getByRole('dialog', { name: /confirm delete|confirmar exclusão/i });
    
    // Form elements
    this.alarmNameInput = page.getByLabel(/alarm name|nome do alarme/i);
    this.alarmTimeInput = page.getByLabel(/alarm time|horário do alarme/i);
    this.daysOfWeekCheckboxes = page.getByRole('group', { name: /days of week|dias da semana/i });
    this.saveAlarmButton = page.getByRole('button', { name: /save|salvar/i });
    this.cancelButton = page.getByRole('button', { name: /cancel|cancelar/i });
    
    // Validation elements
    this.nameValidationError = page.getByTestId('name-validation-error');
    this.timeValidationError = page.getByTestId('time-validation-error');
    
    // Bulk operations
    this.bulkActionsBar = page.getByTestId('bulk-actions-bar');
    this.selectedCount = page.getByTestId('selected-count');
    this.bulkEnableButton = page.getByRole('button', { name: /enable selected|ativar selecionados/i });
    this.bulkDisableButton = page.getByRole('button', { name: /disable selected|desativar selecionados/i });
    this.bulkDeleteButton = page.getByRole('button', { name: /delete selected|excluir selecionados/i });
    this.selectAllCheckbox = page.getByLabel(/select all|selecionar todos/i);
  }

  async goto() {
    await this.page.goto('/alarms');
    await this.page.waitForLoadState('networkidle');
  }

  async reload() {
    await this.page.reload();
    await this.page.waitForLoadState('networkidle');
  }

  // Create alarm actions
  async clickCreateAlarmButton() {
    await this.createAlarmButton.click();
    await expect(this.createAlarmModal).toBeVisible();
  }

  async fillAlarmName(name: string) {
    await this.alarmNameInput.fill(name);
  }

  async clearAlarmName() {
    await this.alarmNameInput.clear();
  }

  async setAlarmTime(time: string) {
    // Handle different time input formats
    const timeInput = this.alarmTimeInput;
    
    if (await timeInput.getAttribute('type') === 'time') {
      await timeInput.fill(time);
    } else {
      // Custom time picker
      await timeInput.click();
      await this.page.getByText(time).click();
    }
  }

  async selectDaysOfWeek(days: string[]) {
    for (const day of days) {
      const dayCheckbox = this.daysOfWeekCheckboxes.getByLabel(new RegExp(day, 'i'));
      await dayCheckbox.check();
    }
  }

  async saveAlarm() {
    await this.saveAlarmButton.click();
  }

  async cancelAlarm() {
    await this.cancelButton.click();
  }

  // Alarm management actions
  async clickEditAlarm(alarmName: string) {
    const alarmCard = this.getAlarmByName(alarmName);
    const editButton = alarmCard.getByRole('button', { name: /edit|editar/i });
    await editButton.click();
  }

  async deleteAlarm(alarmName: string) {
    const alarmCard = this.getAlarmByName(alarmName);
    const deleteButton = alarmCard.getByRole('button', { name: /delete|excluir/i });
    await deleteButton.click();
  }

  async confirmDelete() {
    const confirmButton = this.confirmDeleteModal.getByRole('button', { name: /confirm|confirmar/i });
    await confirmButton.click();
    await expect(this.confirmDeleteModal).not.toBeVisible();
  }

  async toggleAlarm(alarmName: string) {
    const alarmCard = this.getAlarmByName(alarmName);
    const toggle = alarmCard.locator('.alarm-toggle');
    await toggle.click();
  }

  // Search and filter actions
  async searchAlarms(query: string) {
    await this.searchInput.fill(query);
    await this.page.waitForTimeout(500); // Debounce
  }

  async clearSearch() {
    await this.searchInput.clear();
    await this.page.waitForTimeout(500);
  }

  async filterByStatus(status: 'all' | 'enabled' | 'disabled') {
    await this.filterDropdown.selectOption(status);
    await this.page.waitForTimeout(300);
  }

  // Bulk operations
  async selectAlarmCheckbox(alarmName: string) {
    const alarmCard = this.getAlarmByName(alarmName);
    const checkbox = alarmCard.locator('input[type="checkbox"]');
    await checkbox.check();
  }

  async selectAllAlarms() {
    await this.selectAllCheckbox.check();
  }

  async deselectAllAlarms() {
    await this.selectAllCheckbox.uncheck();
  }

  async clickBulkEnable() {
    await this.bulkEnableButton.click();
  }

  async clickBulkDisable() {
    await this.bulkDisableButton.click();
  }

  async clickBulkDelete() {
    await this.bulkDeleteButton.click();
  }

  async confirmBulkDelete() {
    const confirmButton = this.page.getByRole('button', { name: /confirm|confirmar/i });
    await confirmButton.click();
  }

  // Helper methods
  getAlarmByName(name: string): Locator {
    return this.page.getByTestId('alarm-card').filter({ hasText: name });
  }

  async getAlarmCount(): Promise<number> {
    return await this.page.getByTestId('alarm-card').count();
  }

  async getSelectedAlarmsCount(): Promise<number> {
    return await this.page.locator('input[type="checkbox"]:checked').count();
  }

  async waitForAlarmToAppear(alarmName: string, timeout: number = 5000) {
    await expect(this.getAlarmByName(alarmName)).toBeVisible({ timeout });
  }

  async waitForAlarmToDisappear(alarmName: string, timeout: number = 5000) {
    await expect(this.getAlarmByName(alarmName)).not.toBeVisible({ timeout });
  }

  // API helper methods
  async createAlarmViaAPI(alarmData: {
    name: string;
    time: string;
    enabled?: boolean;
    daysOfWeek?: string[];
  }) {
    const response = await this.page.request.post('/api/alarms', {
      data: {
        name: alarmData.name,
        time: alarmData.time,
        enabled: alarmData.enabled ?? true,
        daysOfWeek: alarmData.daysOfWeek ?? ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
      }
    });
    
    expect(response.status()).toBe(201);
    return await response.json();
  }

  async deleteAlarmViaAPI(alarmId: string) {
    const response = await this.page.request.delete(`/api/alarms/${alarmId}`);
    expect(response.status()).toBe(204);
  }

  async getAlarmsViaAPI() {
    const response = await this.page.request.get('/api/alarms');
    expect(response.status()).toBe(200);
    return await response.json();
  }

  // Visual testing helpers
  async takeScreenshot(name: string) {
    await this.page.screenshot({
      path: `test-results/screenshots/${name}.png`,
      fullPage: true
    });
  }

  async compareScreenshot(name: string) {
    await expect(this.page).toHaveScreenshot(`${name}.png`, {
      fullPage: true,
      threshold: 0.2
    });
  }

  // Accessibility helpers
  async checkKeyboardNavigation() {
    await this.page.keyboard.press('Tab');
    const focusedElement = await this.page.locator(':focus');
    return focusedElement;
  }

  async announceToScreenReader(message: string) {
    await this.page.evaluate((msg) => {
      const announcement = document.createElement('div');
      announcement.setAttribute('aria-live', 'polite');
      announcement.setAttribute('aria-atomic', 'true');
      announcement.className = 'sr-only';
      announcement.textContent = msg;
      document.body.appendChild(announcement);
      
      setTimeout(() => {
        document.body.removeChild(announcement);
      }, 1000);
    }, message);
  }

  // Performance helpers
  async measurePageLoadTime() {
    const startTime = Date.now();
    await this.goto();
    const endTime = Date.now();
    return endTime - startTime;
  }

  async measureInteractionTime(action: () => Promise<void>) {
    const startTime = Date.now();
    await action();
    const endTime = Date.now();
    return endTime - startTime;
  }
}