import { test, expect } from '@playwright/test';

test.describe('Frontend Performance Tests', () => {
  test.beforeEach(async ({ page }) => {
    // Enable performance monitoring
    await page.addInitScript(() => {
      // Mark performance measurement start
      window.performance.mark('test-start');
    });
  });

  test('dashboard should load within performance budget', async ({ page }) => {
    const startTime = Date.now();

    // Navigate to dashboard
    await page.goto('/dashboard');

    // Wait for page to be fully loaded
    await page.waitForLoadState('networkidle');

    const loadTime = Date.now() - startTime;

    // Performance assertions
    expect(loadTime).toBeLessThan(3000); // Should load within 3 seconds

    // Check Core Web Vitals
    const webVitals = await page.evaluate(() => {
      return new Promise((resolve) => {
        const vitals = {};

        // Largest Contentful Paint (LCP)
        new PerformanceObserver((list) => {
          const entries = list.getEntries();
          const lastEntry = entries[entries.length - 1];
          vitals.lcp = lastEntry.startTime;
        }).observe({ entryTypes: ['largest-contentful-paint'] });

        // First Input Delay (FID) - simulated
        vitals.fid = 0; // Will be measured on actual user interaction

        // Cumulative Layout Shift (CLS)
        let clsValue = 0;
        new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            if (!entry.hadRecentInput) {
              clsValue += entry.value;
            }
          }
          vitals.cls = clsValue;
        }).observe({ entryTypes: ['layout-shift'] });

        // First Contentful Paint (FCP)
        new PerformanceObserver((list) => {
          const entries = list.getEntries();
          vitals.fcp = entries[0].startTime;
        }).observe({ entryTypes: ['paint'] });

        setTimeout(() => resolve(vitals), 2000);
      });
    });

    // Assert Core Web Vitals thresholds
    expect(webVitals.lcp).toBeLessThan(2500); // LCP should be < 2.5s
    expect(webVitals.cls).toBeLessThan(0.1);  // CLS should be < 0.1
    expect(webVitals.fcp).toBeLessThan(1800); // FCP should be < 1.8s
  });

  test('alarms page should handle large datasets efficiently', async ({ page }) => {
    // Mock a large dataset
    await page.route('**/api/alarms', route => {
      const largeDataset = Array.from({ length: 100 }, (_, i) => ({
        id: `alarm-${i}`,
        name: `Test Alarm ${i}`,
        time: '09:00',
        enabled: i % 2 === 0,
        daysOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday']
      }));

      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(largeDataset)
      });
    });

    const startTime = Date.now();

    await page.goto('/alarms');
    await page.waitForSelector('[data-testid="alarms-list"]');

    const renderTime = Date.now() - startTime;

    // Should render large dataset within reasonable time
    expect(renderTime).toBeLessThan(5000);

    // Check that all items are rendered (or virtualized properly)
    const visibleItems = await page.locator('[data-testid="alarm-item"]').count();
    expect(visibleItems).toBeGreaterThan(0);

    // Test scrolling performance
    const scrollStartTime = Date.now();
    await page.mouse.wheel(0, 1000);
    await page.waitForTimeout(100);
    const scrollTime = Date.now() - scrollStartTime;

    expect(scrollTime).toBeLessThan(500); // Scrolling should be smooth
  });

  test('form interactions should be responsive', async ({ page }) => {
    await page.goto('/alarms/create');

    // Measure form interaction responsiveness
    const interactions = [];

    // Test input responsiveness
    const nameInput = page.locator('[data-testid="alarm-name-input"]');

    const inputStartTime = Date.now();
    await nameInput.fill('Performance Test Alarm');
    const inputTime = Date.now() - inputStartTime;
    interactions.push({ action: 'input', time: inputTime });

    // Test dropdown responsiveness
    const timeInput = page.locator('[data-testid="alarm-time-input"]');

    const timeStartTime = Date.now();
    await timeInput.fill('09:30');
    const timeInputTime = Date.now() - timeStartTime;
    interactions.push({ action: 'time-input', time: timeInputTime });

    // Test checkbox interactions
    const mondayCheckbox = page.locator('[data-testid="day-monday"]');

    const checkboxStartTime = Date.now();
    await mondayCheckbox.check();
    const checkboxTime = Date.now() - checkboxStartTime;
    interactions.push({ action: 'checkbox', time: checkboxTime });

    // All interactions should be fast
    for (const interaction of interactions) {
      expect(interaction.time).toBeLessThan(200); // Should respond within 200ms
    }
  });

  test('navigation should be fast', async ({ page }) => {
    await page.goto('/dashboard');

    const navigationTimes = [];

    // Test navigation to different pages
    const pages = [
      { name: 'alarms', url: '/alarms' },
      { name: 'settings', url: '/settings' },
      { name: 'dashboard', url: '/dashboard' }
    ];

    for (const pageInfo of pages) {
      const startTime = Date.now();

      await page.goto(pageInfo.url);
      await page.waitForLoadState('networkidle');

      const navigationTime = Date.now() - startTime;
      navigationTimes.push({ page: pageInfo.name, time: navigationTime });
    }

    // All navigation should be fast
    for (const nav of navigationTimes) {
      expect(nav.time).toBeLessThan(2000); // Should navigate within 2 seconds
    }
  });

  test('memory usage should remain stable', async ({ page }) => {
    await page.goto('/dashboard');

    // Get initial memory usage
    const initialMemory = await page.evaluate(() => {
      return (performance as any).memory ? {
        usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
        totalJSHeapSize: (performance as any).memory.totalJSHeapSize
      } : null;
    });

    if (!initialMemory) {
      test.skip('Memory API not available in this browser');
      return;
    }

    // Perform memory-intensive operations
    for (let i = 0; i < 10; i++) {
      await page.goto('/alarms');
      await page.waitForLoadState('networkidle');
      await page.goto('/dashboard');
      await page.waitForLoadState('networkidle');
    }

    // Force garbage collection if available
    await page.evaluate(() => {
      if ((window as any).gc) {
        (window as any).gc();
      }
    });

    // Get final memory usage
    const finalMemory = await page.evaluate(() => {
      return {
        usedJSHeapSize: (performance as any).memory.usedJSHeapSize,
        totalJSHeapSize: (performance as any).memory.totalJSHeapSize
      };
    });

    // Memory usage shouldn't increase dramatically
    const memoryIncrease = finalMemory.usedJSHeapSize - initialMemory.usedJSHeapSize;
    const memoryIncreasePercent = (memoryIncrease / initialMemory.usedJSHeapSize) * 100;

    expect(memoryIncreasePercent).toBeLessThan(50); // Memory shouldn't increase by more than 50%
  });

  test('bundle size should be reasonable', async ({ page }) => {
    // Intercept network requests to measure bundle sizes
    const resourceSizes = new Map();

    page.on('response', async (response) => {
      const url = response.url();
      const contentLength = response.headers()['content-length'];

      if (url.includes('.js') || url.includes('.css')) {
        const size = contentLength ? parseInt(contentLength) : 0;
        resourceSizes.set(url, size);
      }
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Calculate total bundle size
    let totalJSSize = 0;
    let totalCSSSize = 0;

    for (const [url, size] of resourceSizes) {
      if (url.includes('.js')) {
        totalJSSize += size;
      } else if (url.includes('.css')) {
        totalCSSSize += size;
      }
    }

    // Assert reasonable bundle sizes
    expect(totalJSSize).toBeLessThan(1024 * 1024); // JS bundle should be < 1MB
    expect(totalCSSSize).toBeLessThan(200 * 1024);  // CSS bundle should be < 200KB
  });

  test('API response times should be acceptable', async ({ page }) => {
    const apiTimes = [];

    // Monitor API calls
    page.on('response', async (response) => {
      const url = response.url();

      if (url.includes('/api/')) {
        const timing = response.request().timing();
        apiTimes.push({
          url: url,
          responseTime: timing.responseEnd - timing.requestStart
        });
      }
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Navigate to trigger more API calls
    await page.goto('/alarms');
    await page.waitForLoadState('networkidle');

    // Check API response times
    for (const apiCall of apiTimes) {
      expect(apiCall.responseTime).toBeLessThan(2000); // API calls should be < 2s
    }
  });

  test('image loading should be optimized', async ({ page }) => {
    const imageMetrics = [];

    page.on('response', async (response) => {
      const url = response.url();
      const contentType = response.headers()['content-type'];

      if (contentType && contentType.startsWith('image/')) {
        const contentLength = response.headers()['content-length'];
        const size = contentLength ? parseInt(contentLength) : 0;

        imageMetrics.push({
          url: url,
          size: size,
          format: contentType
        });
      }
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('networkidle');

    // Check image optimization
    for (const image of imageMetrics) {
      // Images should be reasonably sized
      expect(image.size).toBeLessThan(500 * 1024); // < 500KB per image

      // Should use modern formats when possible
      const isOptimized = image.format.includes('webp') ||
                         image.format.includes('avif') ||
                         image.size < 100 * 1024; // Or be small enough

      expect(isOptimized).toBeTruthy();
    }
  });
});
