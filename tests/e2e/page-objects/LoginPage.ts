import { Page, Locator, expect } from '@playwright/test';

export class LoginPage {
  readonly page: Page;

  // Form elements
  readonly emailInput: Locator;
  readonly passwordInput: Locator;
  readonly loginButton: Locator;
  readonly registerButton: Locator;
  readonly forgotPasswordLink: Locator;

  // OAuth buttons
  readonly googleLoginButton: Locator;
  readonly microsoftLoginButton: Locator;
  readonly fido2Button: Locator;

  // Error and success messages
  readonly errorMessage: Locator;
  readonly successMessage: Locator;

  // Loading states
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;

    // Form elements
    this.emailInput = page.getByLabel(/email|e-mail/i);
    this.passwordInput = page.getByLabel(/password|senha/i);
    this.loginButton = page.getByRole('button', { name: /login|entrar/i });
    this.registerButton = page.getByRole('button', { name: /register|cadastrar/i });
    this.forgotPasswordLink = page.getByRole('link', { name: /forgot password|esqueci a senha/i });

    // OAuth buttons
    this.googleLoginButton = page.getByRole('button', { name: /google/i });
    this.microsoftLoginButton = page.getByRole('button', { name: /microsoft/i });
    this.fido2Button = page.getByRole('button', { name: /fido2|biometric/i });

    // Messages
    this.errorMessage = page.getByTestId('error-message');
    this.successMessage = page.getByTestId('success-message');

    // Loading
    this.loadingSpinner = page.getByTestId('loading-spinner');
  }

  async goto() {
    await this.page.goto('/login');
    await this.page.waitForLoadState('networkidle');
  }

  async loginWithCredentials(email: string, password: string) {
    await this.emailInput.fill(email);
    await this.passwordInput.fill(password);
    await this.loginButton.click();

    // Wait for navigation or error
    await Promise.race([
      this.page.waitForURL('/dashboard'),
      this.errorMessage.waitFor({ state: 'visible' })
    ]);
  }

  async loginWithTestUser() {
    await this.loginWithCredentials('test@example.com', 'TestPassword123!');
  }

  async loginWithGoogle() {
    await this.googleLoginButton.click();
    // Handle OAuth flow in a separate context if needed
  }

  async loginWithMicrosoft() {
    await this.microsoftLoginButton.click();
    // Handle OAuth flow in a separate context if needed
  }

  async loginWithFido2() {
    await this.fido2Button.click();
    // Handle FIDO2 authentication flow
  }

  async isLoggedIn(): Promise<boolean> {
    try {
      await this.page.waitForURL('/dashboard', { timeout: 5000 });
      return true;
    } catch {
      return false;
    }
  }

  async logout() {
    // Navigate to logout or click logout button
    await this.page.goto('/logout');
    await this.page.waitForURL('/login');
  }

  async getErrorMessage(): Promise<string> {
    return await this.errorMessage.textContent() || '';
  }

  async waitForLogin() {
    await this.page.waitForURL('/dashboard');
  }
}
