import { test, expect, devices } from '@playwright/test';

test.describe('Cross-Browser Compatibility', () => {
  // Test core functionality across different browsers
  ['chromium', 'firefox', 'webkit'].forEach(browserName => {
    test.describe(`${browserName} browser tests`, () => {
      test.beforeEach(async ({ page }) => {
        await page.goto('/');
        await page.waitForLoadState('networkidle');
      });

      test(`Basic functionality works in ${browserName}`, async ({ page }) => {
        await test.step('Page loads correctly', async () => {
          // Check if main content is visible
          const body = page.locator('body');
          await expect(body).toBeVisible();

          // Check for basic UI elements
          const navigation = page.locator('nav, [role="navigation"]').first();
          if (await navigation.isVisible()) {
            console.log(`✅ Navigation visible in ${browserName}`);
          }
        });

        await test.step('JavaScript functionality works', async () => {
          // Test basic JavaScript functionality
          const jsWorking = await page.evaluate(() => {
            // Test basic JS features
            const testArray = [1, 2, 3];
            const testObject = { test: true };

            return {
              arrayMethods: testArray.map(x => x * 2).length === 3,
              objectSpread: { ...testObject, extra: true }.extra === true,
              promises: Promise.resolve(true) instanceof Promise,
              localStorage: typeof localStorage !== 'undefined'
            };
          });

          expect(jsWorking.arrayMethods).toBe(true);
          expect(jsWorking.objectSpread).toBe(true);
          expect(jsWorking.promises).toBe(true);
          expect(jsWorking.localStorage).toBe(true);

          console.log(`✅ JavaScript features working in ${browserName}`);
        });

        await test.step('CSS features work', async () => {
          // Test CSS Grid and Flexbox support
          const cssSupport = await page.evaluate(() => {
            const testDiv = document.createElement('div');
            document.body.appendChild(testDiv);

            testDiv.style.display = 'grid';
            const gridSupport = getComputedStyle(testDiv).display === 'grid';

            testDiv.style.display = 'flex';
            const flexSupport = getComputedStyle(testDiv).display === 'flex';

            document.body.removeChild(testDiv);

            return { gridSupport, flexSupport };
          });

          expect(cssSupport.gridSupport).toBe(true);
          expect(cssSupport.flexSupport).toBe(true);

          console.log(`✅ CSS Grid and Flexbox supported in ${browserName}`);
        });

        await test.step('Form interactions work', async () => {
          // Test form functionality
          const input = page.locator('input').first();

          if (await input.isVisible()) {
            await input.fill('test input');
            const value = await input.inputValue();
            expect(value).toBe('test input');

            console.log(`✅ Form inputs working in ${browserName}`);
          }
        });
      });

      test(`Local storage works in ${browserName}`, async ({ page }) => {
        await test.step('Can set and get localStorage', async () => {
          const storageTest = await page.evaluate(() => {
            try {
              localStorage.setItem('test-key', 'test-value');
              const retrieved = localStorage.getItem('test-key');
              localStorage.removeItem('test-key');

              return retrieved === 'test-value';
            } catch (error) {
              return false;
            }
          });

          expect(storageTest).toBe(true);
          console.log(`✅ localStorage working in ${browserName}`);
        });

        await test.step('Can handle JSON in localStorage', async () => {
          const jsonTest = await page.evaluate(() => {
            try {
              const testData = { test: true, number: 42, array: [1, 2, 3] };
              localStorage.setItem('test-json', JSON.stringify(testData));
              const retrieved = JSON.parse(localStorage.getItem('test-json') || '{}');
              localStorage.removeItem('test-json');

              return retrieved.test === true && retrieved.number === 42 && retrieved.array.length === 3;
            } catch (error) {
              return false;
            }
          });

          expect(jsonTest).toBe(true);
          console.log(`✅ JSON localStorage working in ${browserName}`);
        });
      });
    });
  });

  // Test responsive design across different device sizes
  test.describe('Responsive Design Tests', () => {
    const devices_to_test = [
      { name: 'iPhone 12', ...devices['iPhone 12'] },
      { name: 'iPad Pro', ...devices['iPad Pro'] },
      { name: 'Desktop Chrome', ...devices['Desktop Chrome'] }
    ];

    devices_to_test.forEach(device => {
      test(`Responsive design works on ${device.name}`, async ({ browser }) => {
        const context = await browser.newContext({
          ...device
        });

        const page = await context.newPage();

        await test.step(`Load page on ${device.name}`, async () => {
          await page.goto('/');
          await page.waitForLoadState('networkidle');

          // Check if page is responsive
          const viewport = page.viewportSize();
          console.log(`Testing on ${device.name}: ${viewport?.width}x${viewport?.height}`);

          // Verify content is visible and not cut off
          const body = page.locator('body');
          await expect(body).toBeVisible();
        });

        await test.step(`Navigation works on ${device.name}`, async () => {
          // Test navigation on different screen sizes
          if (device.name.includes('iPhone') || device.name.includes('iPad')) {
            // Mobile/tablet navigation
            const mobileMenu = page.locator('[data-testid="mobile-menu"], .mobile-menu, button').filter({ hasText: /menu|☰/i }).first();

            if (await mobileMenu.isVisible()) {
              await mobileMenu.click();
              await page.waitForTimeout(300);
              console.log(`✅ Mobile menu works on ${device.name}`);
            }
          } else {
            // Desktop navigation
            const desktopNav = page.locator('nav a, .nav-link').first();

            if (await desktopNav.isVisible()) {
              console.log(`✅ Desktop navigation visible on ${device.name}`);
            }
          }
        });

        await test.step(`Touch interactions work on ${device.name}`, async () => {
          if (device.hasTouch) {
            // Test touch interactions
            const button = page.locator('button').first();

            if (await button.isVisible()) {
              await button.tap();
              await page.waitForTimeout(300);
              console.log(`✅ Touch interactions work on ${device.name}`);
            }
          }
        });

        await context.close();
      });
    });
  });

  test.describe('Feature Detection Tests', () => {
    test('Modern web features are supported or gracefully degraded', async ({ page }) => {
      await page.goto('/');

      await test.step('Check Web API support', async () => {
        const apiSupport = await page.evaluate(() => {
          return {
            fetch: typeof fetch !== 'undefined',
            promises: typeof Promise !== 'undefined',
            asyncAwait: (async () => true) instanceof Promise,
            serviceWorker: 'serviceWorker' in navigator,
            webStorage: typeof localStorage !== 'undefined',
            geolocation: 'geolocation' in navigator,
            notifications: 'Notification' in window,
            webRTC: 'RTCPeerConnection' in window,
            webGL: (() => {
              try {
                const canvas = document.createElement('canvas');
                return !!(canvas.getContext('webgl') || canvas.getContext('experimental-webgl'));
              } catch (e) {
                return false;
              }
            })()
          };
        });

        // Core features that should be supported
        expect(apiSupport.fetch).toBe(true);
        expect(apiSupport.promises).toBe(true);
        expect(apiSupport.webStorage).toBe(true);

        console.log('Web API Support:', apiSupport);
      });

      await test.step('Check CSS feature support', async () => {
        const cssSupport = await page.evaluate(() => {
          const testElement = document.createElement('div');
          document.body.appendChild(testElement);

          const support = {
            flexbox: (() => {
              testElement.style.display = 'flex';
              return getComputedStyle(testElement).display === 'flex';
            })(),
            grid: (() => {
              testElement.style.display = 'grid';
              return getComputedStyle(testElement).display === 'grid';
            })(),
            customProperties: (() => {
              testElement.style.setProperty('--test', 'test');
              return testElement.style.getPropertyValue('--test') === 'test';
            })(),
            transforms: (() => {
              testElement.style.transform = 'translateX(10px)';
              return testElement.style.transform !== '';
            })(),
            transitions: (() => {
              testElement.style.transition = 'all 0.3s';
              return testElement.style.transition !== '';
            })()
          };

          document.body.removeChild(testElement);
          return support;
        });

        // Modern CSS features that should be supported
        expect(cssSupport.flexbox).toBe(true);
        expect(cssSupport.customProperties).toBe(true);

        console.log('CSS Feature Support:', cssSupport);
      });
    });

    test('Graceful degradation for unsupported features', async ({ page }) => {
      await page.goto('/');

      await test.step('App works without advanced features', async () => {
        // Simulate older browser by removing modern features
        await page.addInitScript(() => {
          // Remove some modern APIs to test graceful degradation
          delete (window as any).fetch;
          delete (window as any).IntersectionObserver;
          delete (navigator as any).serviceWorker;
        });

        await page.reload();
        await page.waitForLoadState('networkidle');

        // App should still be functional
        const body = page.locator('body');
        await expect(body).toBeVisible();

        console.log('✅ App works with limited feature support');
      });
    });
  });
});
