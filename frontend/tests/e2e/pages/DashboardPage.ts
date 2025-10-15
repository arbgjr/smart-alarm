import { Page, Locator } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;

  // Metrics Cards
  readonly activeAlarmsCard: Locator;
  readonly todaysAlarmsCard: Locator;
  readonly activeRoutinesCard: Locator;
  readonly successRateCard: Locator;

  // Performance Metrics
  readonly totalTriggeredMetric: Locator;
  readonly avgResponseTimeMetric: Locator;
  readonly totalSnoozedMetric: Locator;

  // Charts and Analytics
  readonly alarmChart: Locator;
  readonly realTimeStatus: Locator;
  readonly recentActivity: Locator;

  // Quick Actions
  readonly refreshButton: Locator;
  readonly createAlarmButton: Locator;
  readonly createRoutineButton: Locator;
  readonly manageAlarmsButton: Locator;

  // Content Sections
  readonly alarmsSection: Locator;
  readonly routinesSection: Locator;

  // Mobile Navigation
  readonly mobileMenuButton: Locator;
  readonly mobileMenu: Locator;

  constructor(page: Page) {
    this.page = page;

    // Metrics Cards
    this.activeAlarmsCard = page.locator('[data-testid="active-alarms-card"]');
    this.todaysAlarmsCard = page.locator('[data-testid="todays-alarms-card"]');
    this.activeRoutinesCard = page.locator('[data-testid="active-routines-card"]');
    this.successRateCard = page.locator('[data-testid="success-rate-card"]');

    // Performance Metrics
    this.totalTriggeredMetric = page.locator('[data-testid="total-triggered-metric"]');
    this.avgResponseTimeMetric = page.locator('[data-testid="avg-response-time-metric"]');
    this.totalSnoozedMetric = page.locator('[data-testid="total-snoozed-metric"]');

    // Charts and Analytics
    this.alarmChart = page.locator('[data-testid="alarm-chart"]');
    this.realTimeStatus = page.locator('[data-testid="real-time-status"]');
    this.recentActivity = page.locator('[data-testid="recent-activity"]');

    // Quick Actions
    this.refreshButton = page.locator('[data-testid="refresh-button"]');
    this.createAlarmButton = page.locator('[data-testid="create-alarm-button"]');
    this.createRoutineButton = page.locator('[data-testid="create-routine-button"]');
    this.manageAlarmsButton = page.locator('[data-testid="manage-alarms-button"]');

    // Content Sections
    this.alarmsSection = page.locator('[data-testid="alarms-section"]');
    this.routinesSection = page.locator('[data-testid="routines-section"]');

    // Mobile Navigation
    this.mobileMenuButton = page.locator('[data-testid="mobile-menu-button"]');
    this.mobileMenu = page.locator('[data-testid="mobile-menu"]');
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async refreshMetrics() {
    await this.refreshButton.click();
  }

  async createAlarm() {
    await this.createAlarmButton.click();
  }

  async createRoutine() {
    await this.createRoutineButton.click();
  }

  async openMobileMenu() {
    await this.mobileMenuButton.click();
  }

  async navigateToAlarms() {
    await this.manageAlarmsButton.click();
  }
}
