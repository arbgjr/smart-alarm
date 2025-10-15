import { test, expect } from '@playwright/test';

test.describe('ML Insights and Sleep Analytics', () => {
  test.beforeEach(async ({ page }) => {
    // Set up authenticated state with ML data collection enabled
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
      localStorage.setItem('ml-data-collection-consent', 'true');
      
      // Set up some mock ML data
      const mockMLData = [
        {
          userId: 'test-user-id',
          timestamp: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString(), // Yesterday
          eventType: 'sleep_pattern',
          data: {
            bedtime: '22:30',
            wakeupTime: '07:00',
            sleepDuration: 8.5,
            sleepQuality: 4,
            dayOfWeek: 'monday'
          },
          metadata: {
            appVersion: '1.0.0',
            platform: 'desktop',
            sessionId: 'test-session',
            privacyConsent: true
          }
        },
        {
          userId: 'test-user-id',
          timestamp: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(), // 2 days ago
          eventType: 'sleep_pattern',
          data: {
            bedtime: '23:00',
            wakeupTime: '07:15',
            sleepDuration: 8.25,
            sleepQuality: 3,
            dayOfWeek: 'sunday'
          },
          metadata: {
            appVersion: '1.0.0',
            platform: 'desktop',
            sessionId: 'test-session',
            privacyConsent: true
          }
        }
      ];
      
      localStorage.setItem('smart-alarm-ml-data', JSON.stringify(mockMLData));
    });
    
    await page.reload();
  });

  test('should display smart insights page', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show insights page
    await expect(page.locator('h1, h2')).toContainText(/insights|analytics|sleep/i);
    
    // Should show ML insights section
    await expect(page.locator('text="Smart Sleep Insights", text="Sleep Analytics"')).toBeVisible();
  });

  test('should enable ML data collection', async ({ page }) => {
    // First disable ML collection
    await page.evaluate(() => {
      localStorage.setItem('ml-data-collection-consent', 'false');
    });
    
    await page.goto('/insights');
    
    // Should show enable ML collection prompt
    await expect(page.locator('button:has-text("Enable"), button:has-text("Smart Insights")')).toBeVisible();
    
    // Click to enable
    await page.click('button:has-text("Enable"), button:has-text("Smart Insights")');
    
    // Should show ML collection is now enabled
    await expect(page.locator('text="enabled", text="tracking", text="analytics"')).toBeVisible();
    
    // Verify in localStorage
    const consent = await page.evaluate(() => localStorage.getItem('ml-data-collection-consent'));
    expect(consent).toBe('true');
  });

  test('should display sleep pattern analytics', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show sleep pattern summary
    await expect(page.locator('text="Average Bedtime", text="Average Wake Time", text="Sleep Duration"')).toBeVisible();
    
    // Should show sleep consistency score
    await expect(page.locator('text="Sleep Consistency", text="Consistency Score"')).toBeVisible();
    
    // Should show numerical values
    await expect(page.locator('text="22:", text="23:", text="7:", text="8."')).toBeVisible(); // Time patterns
  });

  test('should show personalized recommendations', async ({ page }) => {
    await page.goto('/insights');
    
    // Should have recommendations section
    await expect(page.locator('text="Recommendations", text="Personalized"')).toBeVisible();
    
    // Should show recommendation cards
    const recommendations = page.locator('[data-testid="recommendation"], .recommendation-card');
    const recommendationCount = await recommendations.count();
    
    if (recommendationCount > 0) {
      // Should show recommendation details
      await expect(recommendations.first()).toContainText(/sleep|alarm|consistency|timing/i);
      
      // Should show confidence score
      await expect(recommendations.first()).toContainText(/confidence|%/i);
      
      // Should have apply button for actionable recommendations
      const applyButton = recommendations.first().locator('button:has-text("Apply")');
      if (await applyButton.isVisible()) {
        await applyButton.click();
        // Should trigger some action (like opening create alarm modal)
      }
    }
  });

  test('should display optimal alarm window', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show optimal alarm window if confidence is high enough
    const optimalWindow = page.locator('text="Optimal", text="Window", text="Wake"');
    
    if (await optimalWindow.isVisible()) {
      // Should show time range
      await expect(page.locator('text="06:", text="07:", text="08:"')).toBeVisible();
      
      // Should show confidence level
      await expect(page.locator('text="confidence", text="%"')).toBeVisible();
    }
  });

  test('should show intelligent alarm optimization', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show smart alarm optimization section
    await expect(page.locator('text="Intelligent", text="Optimization", text="Smart"')).toBeVisible();
    
    // Should show how it works explanation
    await expect(page.locator('text="light sleep", text="cycles", text="phases"')).toBeVisible();
    
    // Should show optimization features
    await expect(page.locator('text="detection", text="mapping", text="adaptation"')).toBeVisible();
  });

  test('should allow data export', async ({ page }) => {
    await page.goto('/insights');
    
    // Find export button
    const exportButton = page.locator('button:has-text("Export"), button:has-text("Download")');
    
    if (await exportButton.isVisible()) {
      // Set up download handling
      const downloadPromise = page.waitForEvent('download');
      
      await exportButton.click();
      
      // Wait for download
      const download = await downloadPromise;
      
      // Verify download file name
      expect(download.suggestedFilename()).toContain('smart-alarm-data');
      expect(download.suggestedFilename()).toContain('.json');
    }
  });

  test('should allow data deletion', async ({ page }) => {
    await page.goto('/insights');
    
    // Find delete button
    const deleteButton = page.locator('button:has-text("Delete"), button:has-text("Clear")');
    
    if (await deleteButton.isVisible()) {
      await deleteButton.click();
      
      // Should show confirmation dialog
      await expect(page.locator('text="sure", text="delete", text="cannot be undone"')).toBeVisible();
      
      // Confirm deletion
      await page.click('button:has-text("Delete"), button:has-text("Confirm")');
      
      // Should show success message
      await expect(page.locator('text="deleted", text="cleared", .success')).toBeVisible();
      
      // Data should be removed from localStorage
      const mlData = await page.evaluate(() => localStorage.getItem('smart-alarm-ml-data'));
      expect(mlData).toBe(null);
    }
  });

  test('should disable ML collection', async ({ page }) => {
    await page.goto('/insights');
    
    // Find disable button
    const disableButton = page.locator('button:has-text("Disable")');
    
    if (await disableButton.isVisible()) {
      await disableButton.click();
      
      // Should clear data and disable collection
      await expect(page.locator('text="disabled", text="Enable Smart Insights"')).toBeVisible();
      
      // Verify in localStorage
      const consent = await page.evaluate(() => localStorage.getItem('ml-data-collection-consent'));
      expect(consent).toBe('false');
    }
  });

  test('should show privacy controls', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show privacy section
    await expect(page.locator('text="Privacy", text="Data Controls"')).toBeVisible();
    
    // Should show privacy features
    await expect(page.locator('text="locally", text="control", text="consent"')).toBeVisible();
    
    // Should have privacy settings button
    const privacyButton = page.locator('button:has-text("Privacy Settings")');
    if (await privacyButton.isVisible()) {
      await privacyButton.click();
      // Should open privacy settings (implementation dependent)
    }
  });

  test('should sync ML data', async ({ page }) => {
    await page.goto('/insights');
    
    // Check if there are pending data points
    const syncButton = page.locator('button:has-text("Sync"), text="pending"');
    
    if (await syncButton.isVisible()) {
      await syncButton.click();
      
      // Should show sync in progress
      await expect(page.locator('text="syncing", text="uploading"')).toBeVisible({ timeout: 5000 });
    }
  });

  test('should handle insufficient data gracefully', async ({ page }) => {
    // Clear ML data to simulate insufficient data
    await page.evaluate(() => {
      localStorage.setItem('smart-alarm-ml-data', '[]');
    });
    
    await page.goto('/insights');
    
    // Should show message about needing more data
    await expect(page.locator('text="more data", text="at least", text="week", text="history"')).toBeVisible();
    
    // Should show onboarding information
    await expect(page.locator('text="track", text="patterns", text="insights"')).toBeVisible();
  });

  test('should show sleep cycle analysis', async ({ page }) => {
    await page.goto('/insights');
    
    // Should show sleep cycle information
    await expect(page.locator('text="Sleep Cycle", text="Cycles/Night"')).toBeVisible();
    
    // Should show cycle-related metrics
    await expect(page.locator('text="90 minutes", text="Cycle Length"')).toBeVisible();
    
    // Should show sleep efficiency
    await expect(page.locator('text="Sleep Efficiency", text="Excellent", text="Good", text="Fair"')).toBeVisible();
  });
});