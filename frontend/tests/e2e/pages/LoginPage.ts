import { Page, Locator } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly registerButton: Locator;
  readonly forgotPasswordLink: Locator;
  readonly errorMessage: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.emailInput = page.locator('[data-testid="email-input"]');
    this.passwordInput = page.locator('[data-testid="password-input"]');
    this.loginButton = page.locator('[data-testid="login-button"]');
    this.registerButton = page.locator('[data-testid="register-button"]');
    this.forgotPasswordLink = page.locator('[data-testid="forgot-password-link"]');
    this.errorMessage = page.locator('[data-testid="error-message"]');
    this.loadingSpinner = page.locator('[data-testid="loading-spinner"]');
  }

  async goto() {
    await this.page.goto('/login');
  }

  async login(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();

    // Wait for navigation to dashboard
    await this.page.waitForURL('/dashboard', { timeout: 10000 });
  }

  async register(email: string, password: string, name?: string) {
    if (name) {
      await this.page.locator('[data-testid="name-input"]').fill(name);
    }
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.registerButton.click();
  }

  async forgotPassword(email: string) {
    await this.forgotPasswordLink.click();
    await this.page.locator('[data-testid="reset-email-input"]').fill(email);
    await this.page.locator('[data-testid="reset-password-button"]').click();
  }

  async isLoggedIn(): Promise<boolean> {
    try {
      await this.page.waitForURL('/dashboard', { timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  }

  async getErrorMessage(): Promise<string | null> {
    if (await this.errorMessage.isVisible()) {
      return await this.errorMessage.textContent();
    }
    return null;
  }

  async isLoading(): Promise<boolean> {
    return await this.loadingSpinner.isVisible();
  }
}
