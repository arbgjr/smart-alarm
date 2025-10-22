import { test, expect } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Authentication Flow E2E', () => {
  const testEmail = `testuser_${new Date().getTime()}@smartalarm.com`;
  const testPassword = 'Password123!';

  test('should handle the full authentication lifecycle', async ({ page }) => {
    // Capture console messages and errors
    const browserLogs: string[] = [];
    page.on('console', msg => {
      const text = `[${msg.type()}] ${msg.text()}`;
      browserLogs.push(text);
      console.log('Browser:', text);
    });
    page.on('pageerror', error => {
      const text = `[ERROR] ${error.message}`;
      browserLogs.push(text);
      console.error('Browser error:', error);
    });

    // Clear any pre-existing auth state from global setup
    await page.goto(UI_URL);
    await page.evaluate(() => {
      localStorage.clear(); // Clear ALL localStorage
    });
    // Reload to ensure React picks up the cleared state
    await page.reload();

    // 1. Tentar acessar uma página protegida sem estar logado
    await page.goto(`${UI_URL}/dashboard`);

    // Wait for any loading state to complete (with extended timeout)
    await page.waitForLoadState('networkidle', { timeout: 15000 });

    // Debug: Check localStorage state after navigation
    const authState = await page.evaluate(() => {
      return {
        zustandAuth: localStorage.getItem('smart-alarm-auth'),
        apiToken: localStorage.getItem('smart_alarm_access_token'),
        apiRefreshToken: localStorage.getItem('smart_alarm_refresh_token'),
      };
    });
    console.log('Auth state after navigation:', JSON.stringify(authState));

    // Debug: Check what's actually on the page
    const hasLoadingSpinner = await page.locator('[data-testid="loading-spinner"]').isVisible().catch(() => false);
    console.log('Has loading spinner:', hasLoadingSpinner);

    const pageContent = await page.locator('h1, h2').allTextContents();
    console.log('Page headings:', pageContent);

    // Debug: Print all browser console logs
    console.log('\n=== Browser Console Logs ===');
    browserLogs.forEach(log => console.log(log));
    console.log('============================\n');

    // Deve ser redirecionado para a página de login
    await expect(page).toHaveURL(`${UI_URL}/login`, { timeout: 15000 });
    await expect(page.locator('h3')).toContainText('Smart Alarm');

    // 2. Navegar para a página de registro e criar uma nova conta
    await page.locator('a:has-text("Cadastre-se")').click();
    await expect(page).toHaveURL(`${UI_URL}/register`);
    await expect(page.locator('h3')).toContainText('Smart Alarm');

    // Preencher o formulário de registro
    await page.locator('input[name="name"]').fill('Test User E2E');
    await page.locator('input[name="email"]').fill(testEmail);
    await page.locator('input[name="password"]').fill(testPassword);
    await page.locator('input[name="confirmPassword"]').fill(testPassword);
    await page.locator('button[type="submit"]').click();

    // Após o registro, deve ser redirecionado para a página de login
    await expect(page).toHaveURL(`${UI_URL}/login`);
    await expect(page.locator('text=Registro realizado com sucesso!')).toBeVisible();

    // 3. Fazer login com as novas credenciais
    await page.locator('input[name="email"]').fill(testEmail);
    await page.locator('input[name="password"]').fill(testPassword);
    await page.locator('button[type="submit"]').click();

    // Deve ser redirecionado para o dashboard
    await expect(page).toHaveURL(`${UI_URL}/dashboard`);
    await expect(page.locator('h1')).toContainText('Dashboard');

    // O nome do usuário deve estar visível em algum lugar (ex: no header)
    await expect(page.locator('header')).toContainText('Test User E2E');

    // 4. Fazer logout
    // O botão de logout pode estar em um menu dropdown
    const userMenuButton = page.locator('button[aria-label="User menu"]');
    if (await userMenuButton.isVisible()) {
      await userMenuButton.click();
    }
    await page.locator('button:has-text("Logout"), a:has-text("Logout")').click();

    // Deve ser redirecionado de volta para a página de login
    await expect(page).toHaveURL(`${UI_URL}/login`);
    await expect(page.locator('h2')).toContainText('Login');

    // 5. Tentar acessar a página protegida novamente após o logout
    await page.goto(`${UI_URL}/dashboard`);
    await expect(page).toHaveURL(`${UI_URL}/login`);
  });
});
