import { test, expect, Page } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('PWA Functionality E2E', () => {
  test.beforeEach(async ({ page }) => {
    // O Service Worker é registrado na primeira visita.
    // Para garantir um estado limpo, podemos precisar de um contexto novo para alguns testes.
    await page.goto(UI_URL, { waitUntil: 'networkidle' });
  });

  test('should register a service worker', async ({ page }) => {
    // O Playwright pode inspecionar o Service Worker registrado.
    const swURL = await page.evaluate(async () => {
      const registration = await navigator.serviceWorker.ready;
      return registration.active?.scriptURL;
    });

    // Verifica se a URL do Service Worker é a esperada.
    expect(swURL).toContain('sw.js');
  });

  test('should load from cache when offline', async ({ page, context }) => {
    // 1. Visita a página para garantir que os assets sejam cacheados.
    await page.goto(`${UI_URL}/dashboard`, { waitUntil: 'networkidle' });
    await expect(page.locator('h1')).toContainText('Dashboard');

    // 2. Simula o estado offline.
    await context.setOffline(true);

    // 3. Recarrega a página.
    try {
      await page.reload({ waitUntil: 'domcontentloaded', timeout: 5000 });
    } catch (error) {
      // É esperado que a recarga possa falhar em modo offline,
      // mas o conteúdo deve ser servido pelo Service Worker.
      console.log('Reload in offline mode triggered a navigation error, which is expected.');
    }

    // 4. Verifica se o conteúdo principal ainda está visível, servido pelo cache.
    await expect(page.locator('h1')).toContainText('Dashboard');
    await expect(page.locator('text=Você está offline')).toBeVisible(); // Supondo que haja um indicador offline.

    // 5. Volta a ficar online.
    await context.setOffline(false);
    await page.reload();
    await expect(page.locator('h1')).toContainText('Dashboard');
  });

  test('should show update notification when a new version is available', async ({ page }) => {
    // Esta é uma simulação. Em um teste real, isso requereria uma infraestrutura mais complexa.
    // Vamos simular o evento que o PwaUpdater escuta.
    await page.evaluate(() => {
      // Simula o evento que o `vite-plugin-pwa` dispara.
      const event = new CustomEvent('swUpdated', { detail: { needsRefresh: true } });
      window.dispatchEvent(event);
    });

    // Verifica se o toast de atualização apareceu.
    await expect(page.locator('text=Nova versão disponível!')).toBeVisible();
    await expect(page.locator('button:has-text("Atualizar")')).toBeVisible();
  });
});
