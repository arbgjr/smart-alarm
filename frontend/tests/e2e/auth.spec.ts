import { test, expect } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Authentication Flow E2E', () => {
  const testEmail = `testuser_${new Date().getTime()}@smartalarm.com`;
  const testPassword = 'Password123!';

  test('should handle the full authentication lifecycle', async ({ page }) => {
    // 1. Tentar acessar uma página protegida sem estar logado
    await page.goto(`${UI_URL}/dashboard`);
    // Deve ser redirecionado para a página de login
    await expect(page).toHaveURL(`${UI_URL}/login`);
    await expect(page.locator('h2')).toContainText('Login');

    // 2. Navegar para a página de registro e criar uma nova conta
    await page.locator('a:has-text("Registrar nova conta")').click();
    await expect(page).toHaveURL(`${UI_URL}/register`);
    await expect(page.locator('h2')).toContainText('Criar Conta');

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
