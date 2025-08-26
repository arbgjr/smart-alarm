import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Clear any existing auth state
    await page.goto('/');
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  });

  test('should show login form when not authenticated', async ({ page }) => {
    await page.goto('/');
    
    // Should redirect to login or show login form
    await expect(page).toHaveURL(/\/(login|auth)/);
    
    // Check for login form elements
    await expect(page.locator('input[type="email"], input[name="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"], input[name="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")')).toBeVisible();
  });

  test('should login successfully with valid credentials', async ({ page }) => {
    await page.goto('/login');
    
    // Fill in login form
    await page.fill('input[type="email"], input[name="email"]', 'testuser@example.com');
    await page.fill('input[type="password"], input[name="password"]', 'TestPassword123!');
    
    // Submit form
    await page.click('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")');
    
    // Should redirect to dashboard after successful login
    await expect(page).toHaveURL(/\/(dashboard|alarms|home|$)/);
    
    // Should show user interface elements
    await expect(page.locator('text="Welcome"')).toBeVisible({ timeout: 10000 });
    
    // Check that auth state is stored
    const authState = await page.evaluate(() => {
      const stored = localStorage.getItem('smart-alarm-auth');
      return stored ? JSON.parse(stored) : null;
    });
    
    expect(authState).toBeTruthy();
    expect(authState?.state?.isAuthenticated).toBe(true);
    expect(authState?.state?.user?.email).toBe('testuser@example.com');
  });

  test('should show error with invalid credentials', async ({ page }) => {
    await page.goto('/login');
    
    // Fill in invalid credentials
    await page.fill('input[type="email"], input[name="email"]', 'invalid@example.com');
    await page.fill('input[type="password"], input[name="password"]', 'wrongpassword');
    
    // Submit form
    await page.click('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")');
    
    // Should show error message
    await expect(page.locator('text="Invalid credentials", text="Login failed", .error, .alert-error')).toBeVisible({ timeout: 10000 });
    
    // Should remain on login page
    await expect(page).toHaveURL(/\/login/);
  });

  test('should register new user successfully', async ({ page }) => {
    await page.goto('/register');
    
    // Fill in registration form
    const testEmail = `newuser-${Date.now()}@example.com`;
    await page.fill('input[name="name"], input[placeholder*="name"]', 'New Test User');
    await page.fill('input[type="email"], input[name="email"]', testEmail);
    await page.fill('input[type="password"], input[name="password"]', 'NewUserPassword123!');
    await page.fill('input[name="confirmPassword"], input[name="password2"]', 'NewUserPassword123!');
    
    // Submit form
    await page.click('button[type="submit"], button:has-text("Sign Up"), button:has-text("Register")');
    
    // Should either redirect to login or dashboard
    await expect(page).toHaveURL(/\/(login|dashboard|alarms|verify-email)/);
    
    // If redirected to verify email, that's also valid
    const currentUrl = page.url();
    if (currentUrl.includes('verify')) {
      await expect(page.locator('text="verify", text="email", text="sent"')).toBeVisible();
    }
  });

  test('should logout successfully', async ({ page }) => {
    // First login
    await page.goto('/login');
    await page.fill('input[type="email"], input[name="email"]', 'testuser@example.com');
    await page.fill('input[type="password"], input[name="password"]', 'TestPassword123!');
    await page.click('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")');
    
    // Wait for successful login
    await expect(page).toHaveURL(/\/(dashboard|alarms|home|$)/);
    
    // Find and click logout button
    await page.click('button:has-text("Logout"), button:has-text("Sign Out"), [data-testid="logout"], .logout');
    
    // Should redirect to login page
    await expect(page).toHaveURL(/\/(login|auth|$)/);
    
    // Auth state should be cleared
    const authState = await page.evaluate(() => {
      const stored = localStorage.getItem('smart-alarm-auth');
      return stored ? JSON.parse(stored) : null;
    });
    
    expect(authState?.state?.isAuthenticated).toBeFalsy();
  });

  test('should persist auth state across page refreshes', async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.fill('input[type="email"], input[name="email"]', 'testuser@example.com');
    await page.fill('input[type="password"], input[name="password"]', 'TestPassword123!');
    await page.click('button[type="submit"], button:has-text("Sign In"), button:has-text("Login")');
    
    await expect(page).toHaveURL(/\/(dashboard|alarms|home|$)/);
    
    // Refresh the page
    await page.reload();
    
    // Should still be logged in
    await expect(page).toHaveURL(/\/(dashboard|alarms|home|$)/);
    
    // Auth state should persist
    const authState = await page.evaluate(() => {
      const stored = localStorage.getItem('smart-alarm-auth');
      return stored ? JSON.parse(stored) : null;
    });
    
    expect(authState?.state?.isAuthenticated).toBe(true);
  });

  test('should redirect to login when accessing protected routes', async ({ page }) => {
    // Try to access protected routes without authentication
    const protectedRoutes = ['/dashboard', '/alarms', '/settings', '/profile'];
    
    for (const route of protectedRoutes) {
      await page.goto(route);
      
      // Should redirect to login
      await expect(page).toHaveURL(/\/(login|auth)/);
    }
  });
});