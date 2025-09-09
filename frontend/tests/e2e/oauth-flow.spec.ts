import { test, expect, Page, BrowserContext } from '@playwright/test';

// Test configuration
const config = {
  frontend: {
    baseUrl: process.env.FRONTEND_BASE_URL || 'http://localhost:3000',
  },
  api: {
    baseUrl: process.env.API_BASE_URL || 'http://localhost:8080/api/v1',
  },
};

// OAuth2 Flow E2E Tests for Smart Alarm
test.describe('OAuth2 Authentication Flow', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    
    // Listen for console errors
    page.on('console', msg => {
      if (msg.type() === 'error') {
        console.error(`Console error: ${msg.text()}`);
      }
    });

    // Navigate to login page
    await page.goto(`${config.frontend.baseUrl}/login`);
    await page.waitForLoadState('networkidle');
  });

  test.afterEach(async () => {
    await context.close();
  });

  // Test 1: OAuth Login Buttons Are Present
  test('should display OAuth provider login buttons', async () => {
    // Check if OAuth login section exists
    const oauthSection = page.locator('[data-testid="oauth-providers"]');
    await expect(oauthSection).toBeVisible();

    // Check for individual provider buttons
    const providers = ['google', 'github', 'facebook', 'microsoft'];
    
    for (const provider of providers) {
      const providerButton = page.locator(`[data-testid="oauth-${provider}-button"]`);
      await expect(providerButton).toBeVisible();
      await expect(providerButton).toBeEnabled();
      
      // Check button text contains provider name
      await expect(providerButton).toContainText(new RegExp(provider, 'i'));
    }
  });

  // Test 2: OAuth Provider Button Clicks
  test('should initiate OAuth flow when clicking provider buttons', async () => {
    const providers = ['google', 'github', 'facebook', 'microsoft'];
    
    for (const provider of providers) {
      // Create a new page for each OAuth test to avoid state issues
      const testPage = await context.newPage();
      await testPage.goto(`${config.frontend.baseUrl}/login`);
      await testPage.waitForLoadState('networkidle');

      const providerButton = testPage.locator(`[data-testid="oauth-${provider}-button"]`);
      
      // Listen for navigation events
      const navigationPromise = testPage.waitForURL(url => 
        url.includes('/api/v1/auth/oauth') || 
        url.includes(provider.toLowerCase()) || 
        url.includes('oauth')
      );

      await providerButton.click();
      
      try {
        // Wait for either API call or redirect
        await Promise.race([
          navigationPromise,
          testPage.waitForResponse(response => 
            response.url().includes(`/api/v1/auth/oauth/${provider}/authorize`)
          )
        ]);
        
        // Verify we're either redirected or API was called
        const currentUrl = testPage.url();
        const isOAuthFlow = currentUrl.includes('oauth') || 
                           currentUrl.includes('authorize') ||
                           currentUrl.includes(provider.toLowerCase());
        
        expect(isOAuthFlow).toBeTruthy();
      } catch (error) {
        // If navigation doesn't happen, check if API call was made
        const apiCallMade = await testPage.evaluate(() => {
          return window.performance.getEntriesByType('resource')
            .some(entry => entry.name.includes('/api/v1/auth/oauth'));
        });
        
        expect(apiCallMade).toBeTruthy();
      }
      
      await testPage.close();
    }
  });

  // Test 3: OAuth Error Handling
  test('should handle OAuth errors gracefully', async () => {
    // Simulate OAuth error response by intercepting the API call
    await page.route('**/api/v1/auth/oauth/*/callback', async route => {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({
          success: false,
          message: 'access_denied: User denied access',
          provider: 'Google'
        })
      });
    });

    // Simulate returning from OAuth with error
    await page.goto(`${config.frontend.baseUrl}/auth/callback?error=access_denied&error_description=User%20denied%20access&state=test`);
    
    // Check for error message
    const errorMessage = page.locator('[data-testid="oauth-error-message"]');
    await expect(errorMessage).toBeVisible();
    await expect(errorMessage).toContainText(/denied|error|failed/i);
    
    // Check for retry option
    const retryButton = page.locator('[data-testid="oauth-retry-button"]');
    await expect(retryButton).toBeVisible();
  });

  // Test 4: OAuth Success Flow Simulation
  test('should handle successful OAuth callback', async () => {
    const mockUserData = {
      success: true,
      accessToken: 'mock-access-token',
      refreshToken: 'mock-refresh-token',
      user: {
        id: 'mock-user-id',
        name: 'Test User',
        email: 'test@example.com'
      },
      provider: 'Google',
      isNewUser: false
    };

    // Mock successful OAuth callback
    await page.route('**/api/v1/auth/oauth/*/callback', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockUserData)
      });
    });

    // Mock user profile endpoint
    await page.route('**/api/v1/users/me', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockUserData.user)
      });
    });

    // Simulate successful OAuth callback
    await page.goto(`${config.frontend.baseUrl}/auth/callback?code=mock-auth-code&state=test-state`);
    
    // Wait for redirect to dashboard or main app
    await page.waitForURL(url => 
      url.includes('/dashboard') || 
      url.includes('/alarms') || 
      url.includes('/app')
    );
    
    // Check if user is logged in (look for logout button or user menu)
    const userMenu = page.locator('[data-testid="user-menu"]');
    const logoutButton = page.locator('[data-testid="logout-button"]');
    
    const isLoggedIn = await Promise.race([
      userMenu.isVisible(),
      logoutButton.isVisible()
    ]);
    
    expect(isLoggedIn).toBeTruthy();
  });

  // Test 5: OAuth State Parameter Security
  test('should include and validate state parameter for security', async () => {
    let capturedState: string | null = null;

    // Intercept authorization URL request
    await page.route('**/api/v1/auth/oauth/*/authorize*', async route => {
      const url = new URL(route.request().url());
      capturedState = url.searchParams.get('state');
      
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          authorizationUrl: `https://accounts.google.com/oauth/authorize?state=${capturedState}&client_id=test`,
          state: capturedState,
          provider: 'Google'
        })
      });
    });

    const googleButton = page.locator('[data-testid="oauth-google-button"]');
    await googleButton.click();

    // Verify state parameter was included
    expect(capturedState).toBeTruthy();
    expect(capturedState).toMatch(/^[a-zA-Z0-9\-_]{16,}$/); // Should be a secure random string
  });

  // Test 6: OAuth Provider Account Linking
  test('should handle account linking for authenticated users', async () => {
    // First, simulate being logged in
    await page.addInitScript(() => {
      localStorage.setItem('auth-token', 'mock-jwt-token');
      localStorage.setItem('user', JSON.stringify({
        id: 'existing-user-id',
        name: 'Existing User',
        email: 'existing@example.com'
      }));
    });

    await page.goto(`${config.frontend.baseUrl}/settings/account`);
    await page.waitForLoadState('networkidle');

    // Look for account linking section
    const linkingSection = page.locator('[data-testid="oauth-account-linking"]');
    await expect(linkingSection).toBeVisible();

    // Test linking a provider account
    const linkGoogleButton = page.locator('[data-testid="link-google-account"]');
    if (await linkGoogleButton.isVisible()) {
      await linkGoogleButton.click();
      
      // Should redirect to OAuth authorization
      const isOAuthFlow = await page.waitForURL(url => 
        url.includes('oauth') || url.includes('authorize')
      ).catch(() => false);
      
      expect(isOAuthFlow).toBeTruthy();
    }
  });

  // Test 7: OAuth Provider Account Unlinking
  test('should handle account unlinking for authenticated users', async () => {
    // Simulate being logged in with linked account
    await page.addInitScript(() => {
      localStorage.setItem('auth-token', 'mock-jwt-token');
      localStorage.setItem('user', JSON.stringify({
        id: 'user-with-oauth',
        name: 'User With OAuth',
        email: 'oauth-user@example.com',
        externalProvider: 'Google',
        externalProviderId: 'google-123'
      }));
    });

    // Mock unlink API call
    await page.route('**/api/v1/auth/oauth/*/unlink', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true })
      });
    });

    await page.goto(`${config.frontend.baseUrl}/settings/account`);
    await page.waitForLoadState('networkidle');

    // Find and click unlink button
    const unlinkButton = page.locator('[data-testid="unlink-google-account"]');
    if (await unlinkButton.isVisible()) {
      await unlinkButton.click();
      
      // Confirm unlinking in modal/dialog
      const confirmButton = page.locator('[data-testid="confirm-unlink"]');
      if (await confirmButton.isVisible()) {
        await confirmButton.click();
      }
      
      // Verify success message
      const successMessage = page.locator('[data-testid="unlink-success"]');
      await expect(successMessage).toBeVisible();
    }
  });

  // Test 8: OAuth Responsive Design
  test('should display OAuth buttons correctly on mobile devices', async () => {
    // Test mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto(`${config.frontend.baseUrl}/login`);
    
    const oauthSection = page.locator('[data-testid="oauth-providers"]');
    await expect(oauthSection).toBeVisible();
    
    // OAuth buttons should stack vertically on mobile
    const googleButton = page.locator('[data-testid="oauth-google-button"]');
    const githubButton = page.locator('[data-testid="oauth-github-button"]');
    
    const googleBox = await googleButton.boundingBox();
    const githubBox = await githubButton.boundingBox();
    
    if (googleBox && githubBox) {
      // Buttons should be stacked (GitHub button below Google button)
      expect(githubBox.y).toBeGreaterThan(googleBox.y + googleBox.height);
    }
    
    // Test tablet viewport
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.waitForTimeout(500); // Allow layout to adjust
    
    // Buttons should still be visible and functional
    await expect(googleButton).toBeVisible();
    await expect(githubButton).toBeVisible();
  });

  // Test 9: OAuth Loading States
  test('should show loading states during OAuth flow', async () => {
    // Delay the authorization URL response
    await page.route('**/api/v1/auth/oauth/*/authorize*', async route => {
      await new Promise(resolve => setTimeout(resolve, 2000)); // 2 second delay
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          authorizationUrl: 'https://accounts.google.com/oauth/authorize?test',
          state: 'test-state',
          provider: 'Google'
        })
      });
    });

    const googleButton = page.locator('[data-testid="oauth-google-button"]');
    await googleButton.click();
    
    // Check for loading state
    const loadingIndicator = page.locator('[data-testid="oauth-loading"]');
    await expect(loadingIndicator).toBeVisible();
    
    // Button should be disabled during loading
    await expect(googleButton).toBeDisabled();
    
    // Wait for loading to complete
    await expect(loadingIndicator).toBeHidden();
    await expect(googleButton).toBeEnabled();
  });

  // Test 10: OAuth Accessibility
  test('should be accessible for screen readers', async () => {
    await page.goto(`${config.frontend.baseUrl}/login`);
    
    const providers = ['google', 'github', 'facebook', 'microsoft'];
    
    for (const provider of providers) {
      const button = page.locator(`[data-testid="oauth-${provider}-button"]`);
      
      // Check for proper ARIA labels
      const ariaLabel = await button.getAttribute('aria-label');
      expect(ariaLabel).toBeTruthy();
      expect(ariaLabel?.toLowerCase()).toContain(provider);
      
      // Check for proper roles
      const role = await button.getAttribute('role');
      expect(role).toBe('button');
      
      // Check for keyboard navigation
      await button.focus();
      await expect(button).toBeFocused();
    }
    
    // Test keyboard navigation between OAuth buttons
    await page.keyboard.press('Tab');
    await page.keyboard.press('Tab');
    
    // Should be able to activate with Enter key
    const focusedElement = page.locator(':focus');
    await page.keyboard.press('Enter');
    
    // Should trigger OAuth flow
    const hasNavigation = await page.waitForURL(url => 
      url.includes('oauth') || url.includes('authorize')
    ).catch(() => false);
    
    expect(hasNavigation).toBeTruthy();
  });
});

// OAuth2 Integration with Existing Authentication
test.describe('OAuth2 Integration', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeEach(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
  });

  test.afterEach(async () => {
    await context.close();
  });

  test('should work alongside traditional email/password login', async () => {
    await page.goto(`${config.frontend.baseUrl}/login`);
    
    // Both email/password form and OAuth buttons should be present
    const emailInput = page.locator('[data-testid="email-input"]');
    const passwordInput = page.locator('[data-testid="password-input"]');
    const loginButton = page.locator('[data-testid="login-button"]');
    const oauthSection = page.locator('[data-testid="oauth-providers"]');
    
    await expect(emailInput).toBeVisible();
    await expect(passwordInput).toBeVisible();
    await expect(loginButton).toBeVisible();
    await expect(oauthSection).toBeVisible();
    
    // Should have clear separation between methods
    const separator = page.locator('[data-testid="login-separator"]');
    await expect(separator).toBeVisible();
  });

  test('should maintain session consistency between OAuth and regular auth', async () => {
    // Mock successful OAuth login
    await page.route('**/api/v1/auth/oauth/*/callback', async route => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          success: true,
          accessToken: 'oauth-access-token',
          refreshToken: 'oauth-refresh-token',
          user: {
            id: 'oauth-user-id',
            name: 'OAuth User',
            email: 'oauth@example.com'
          },
          provider: 'Google',
          isNewUser: false
        })
      });
    });

    // Login via OAuth
    await page.goto(`${config.frontend.baseUrl}/auth/callback?code=mock-code&state=test`);
    
    // Should be redirected to app
    await page.waitForURL(url => !url.includes('/login') && !url.includes('/callback'));
    
    // Verify authentication state is maintained across page refreshes
    await page.reload();
    
    // Should still be authenticated (not redirected to login)
    const currentUrl = page.url();
    expect(currentUrl).not.toContain('/login');
    
    // Should be able to access protected resources
    const userMenu = page.locator('[data-testid="user-menu"]');
    await expect(userMenu).toBeVisible();
  });
});