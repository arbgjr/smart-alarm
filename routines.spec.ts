import { test, expect } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Routine Management E2E', () => {
  const testRoutineName = `E2E Test Routine ${new Date().getTime()}`;
  const updatedTestRoutineName = `${testRoutineName} - Updated`;

  // Antes de cada teste, faz login para obter um token de autenticação
  test.beforeEach(async ({ page }) => {
    await page.goto(`${UI_URL}/login`);

    // Preenche as credenciais de login
    await page.locator('input[name="email"]').fill('testuser@smartalarm.com');
    await page.locator('input[name="password"]').fill('Password123!');
    await page.locator('button[type="submit"]').click();

    // Espera pelo redirecionamento para o dashboard
    await expect(page).toHaveURL(`${UI_URL}/dashboard`);
  });

  test('should allow a user to create, view, edit, and delete a routine', async ({ page }) => {
    // 1. Navegação para a página de Rotinas
    await page.goto(`${UI_URL}/routines`);
    await expect(page.locator('h1')).toContainText('Minhas Rotinas');

    // 2. Criação de uma nova rotina
    await page.locator('button:has-text("Nova Rotina")').click();

    // Preenche o formulário no modal
    await expect(page.locator('h2')).toContainText('Nova Rotina');
    await page.locator('input[name="name"]').fill(testRoutineName);
    await page.locator('textarea[name="description"]').fill('This is a test routine created by Playwright.');
    await page.locator('button:has-text("Salvar")').click();

    // 3. Verificação da criação
    // Espera o toast de sucesso aparecer e desaparecer
    await expect(page.locator('text=Rotina criada com sucesso!')).toBeVisible();
    // Verifica se a nova rotina está na lista
    await expect(page.locator(`text=${testRoutineName}`)).toBeVisible();

    // Encontra o item da lista que contém a nossa rotina de teste
    const routineListItem = page.locator('li', { has: page.locator(`text=${testRoutineName}`) });

    // 4. Edição da rotina
    await routineListItem.locator('button[aria-label*="Editar"]').click();

    // Altera os dados no formulário de edição
    await expect(page.locator('h2')).toContainText('Editar Rotina');
    await page.locator('input[name="name"]').fill(updatedTestRoutineName);
    await page.locator('button:has-text("Salvar")').click();

    // 5. Verificação da edição
    await expect(page.locator('text=Rotina atualizada com sucesso!')).toBeVisible();
    await expect(page.locator(`text=${testRoutineName}`)).not.toBeVisible(); // O nome antigo não deve mais existir
    await expect(page.locator(`text=${updatedTestRoutineName}`)).toBeVisible(); // O novo nome deve estar visível

    // 6. Exclusão da rotina
    // Reencontra o item da lista com o nome atualizado
    const updatedRoutineListItem = page.locator('li', { has: page.locator(`text=${updatedTestRoutineName}`) });

    // Lida com o diálogo de confirmação do navegador
    page.on('dialog', dialog => dialog.accept());

    await updatedRoutineListItem.locator('button[aria-label*="Excluir"]').click();

    // 7. Verificação da exclusão
    await expect(page.locator('text=Rotina excluída com sucesso!')).toBeVisible();
    await expect(page.locator(`text=${updatedTestRoutineName}`)).not.toBeVisible();
  });
});
