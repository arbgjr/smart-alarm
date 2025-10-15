import { Page, Locator } from '@playwright/test';

export class AlarmsPage {
  readonly page: Page;

  // Filters
  readonly searchInput: Locator;
  readonly statusFilter: Locator;
  readonly typeFilter: Locator;
  readonly sortByFilter: Locator;
  readonly sortOrderButton: Locator;

  // View Controls
  readonly gridViewButton: Locator;
  readonly listViewButton: Locator;

  // Action Buttons
  readonly createAlarmButton: Locator;
  readonly routinesButton: Locator;
  readonly exceptionsButton: Locator;
  readonly holidaysButton: Locator;

  // Content Areas
  readonly alarmsGrid: Locator;
  readonly alarmsList: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    this.page = page;

    // Filters
    this.searchInput = page.locator('[data-testid="search-input"]');
    this.statusFilter = page.locator('[data-testid="status-filter"]');
    this.typeFilter = page.locator('[data-testid="type-filter"]');
    this.sortByFilter = page.locator('[data-testid="sort-by-filter"]');
    this.sortOrderButton = page.locator('[data-testid="sort-order-button"]');

    // View Controls
    this.gridViewButton = page.locator('[data-testid="grid-view-button"]');
    this.listViewButton = page.locator('[data-testid="list-view-button"]');

    // Action Buttons
    this.createAlarmButton = page.locator('[data-testid="create-alarm-button"]');
    this.routinesButton = page.locator('[data-testid="routines-button"]');
    this.exceptionsButton = page.locator('[data-testid="exceptions-button"]');
    this.holidaysButton = page.locator('[data-testid="holidays-button"]');

    // Content Areas
    this.alarmsGrid = page.locator('[data-testid="alarms-grid"]');
    this.alarmsList = page.locator('[data-testid="alarms-list"]');
    this.emptyState = page.locator('[data-testid="empty-state"]');
  }

  async goto() {
    await this.page.goto('/alarms');
  }

  async searchAlarms(query: string) {
    await this.searchInput.fill(query);
  }

  async filterByStatus(status: string) {
    await this.statusFilter.selectOption(status);
  }

  async filterByType(type: string) {
    await this.typeFilter.selectOption(type);
  }

  async sortBy(field: string) {
    await this.sortByFilter.selectOption(field);
  }

  async toggleSortOrder() {
    await this.sortOrderButton.click();
  }

  async switchToGridView() {
    await this.gridViewButton.click();
  }

  async switchToListView() {
    await this.listViewButton.click();
  }

  async createAlarm() {
    await this.createAlarmButton.click();
  }

  async openRoutinesModal() {
    await this.routinesButton.click();
  }

  async openExceptionsModal() {
    await this.exceptionsButton.click();
  }

  async openHolidaysModal() {
    await this.holidaysButton.click();
  }

  async getAlarmCard(index: number = 0) {
    return this.page.locator('[data-testid="alarm-card"]').nth(index);
  }

  async editAlarm(index: number = 0) {
    const alarmCard = await this.getAlarmCard(index);
    await alarmCard.locator('[data-testid="edit-alarm-button"]').click();
  }

  async deleteAlarm(index: number = 0) {
    const alarmCard = await this.getAlarmCard(index);
    await alarmCard.locator('[data-testid="alarm-menu-button"]').click();
    await this.page.locator('[data-testid="delete-alarm-button"]').click();
  }

  async toggleAlarm(index: number = 0) {
    const alarmCard = await this.getAlarmCard(index);
    await alarmCard.locator('[data-testid="toggle-alarm-button"]').click();
  }

  async duplicateAlarm(index: number = 0) {
    const alarmCard = await this.getAlarmCard(index);
    await alarmCard.locator('[data-testid="alarm-menu-button"]').click();
    await this.page.locator('[data-testid="duplicate-alarm-button"]').click();
  }
}
