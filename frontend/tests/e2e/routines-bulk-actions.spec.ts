import { test, expect, Page } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Routine Bulk Actions E2E', () => {
  const routineNames: string[] = [];

  // Helper para criar rotinas
  async function createTestRoutines(page: Page, count: number) {
    for (let i = 1; i <= count; i++) {
      const name = `Bulk Action Test Routine ${i}`;
      routineNames.push(name);
      await page.locator('button:has-text("Nova Rotina")').click();
      await page.locator('input[name="name"]').fill(name);
      await page.locator('button:has-text("Salvar")').click();
      await expect(page.locator('text=Rotina criada com sucesso!')).toBeVisible();
    }
  }

  // Setup: Cria 3 rotinas antes de cada teste
  test.beforeEach(async ({ page }) => {
    await page.goto(`${UI_URL}/login`);
    await page.locator('input[name="email"]').fill('testuser@smartalarm.com');
    await page.locator('input[name="password"]').fill('Password123!');
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(`${UI__URL}/dashboard`);

    await page.goto(`${UI_URL}/routines`);
    await expect(page.locator('h1')).toContainText('Minhas Rotinas');

    // Limpa a lista de nomes para cada teste
    routineNames.length = 0;
    await createTestRoutines(page, 3);
  });

  // Cleanup: Deleta as rotinas criadas
  test.afterEach(async ({ page }) => {
    await page.goto(`${UI_URL}/routines`);
    for (const name of routineNames) {
      const routineLocator = page.locator('li', { has: page.locator(`text=${name}`) });
      if (await routineLocator.isVisible()) {
        page.on('dialog', dialog => dialog.accept());
        await routineLocator.locator('button[aria-label*="Excluir"]').click();
        await expect(page.locator('text=Rotina excluída com sucesso!')).toBeVisible();
      }
    }
  });

  test('should select multiple routines and show the bulk action bar', async ({ page }) => {
    const routine1 = page.locator('li', { hasText: routineNames[0] });
    const routine2 = page.locator('li', { hasText: routineNames[1] });
    const bulkActionBar = page.locator('.fixed.bottom-4');

    // Barra de ações não deve estar visível inicialmente
    await expect(bulkActionBar).not.toBeVisible();

    // Seleciona a primeira rotina
    await routine1.locator('input[type="checkbox"]').check();
    await expect(bulkActionBar).toBeVisible();
    await expect(bulkActionBar).toContainText('1 selecionada(s)');

    // Seleciona a segunda rotina
    await routine2.locator('input[type="checkbox"]').check();
    await expect(bulkActionBar).toContainText('2 selecionada(s)');

    // Desmarca a primeira rotina
    await routine1.locator('input[type="checkbox"]').uncheck();
    await expect(bulkActionBar).toContainText('1 selecionada(s)');

    // Limpa a seleção
    await bulkActionBar.locator('button[title="Limpar seleção"]').click();
    await expect(bulkActionBar).not.toBeVisible();
  });

  test('should delete selected routines using bulk action', async ({ page }) => {
    const routine1 = page.locator('li', { hasText: routineNames[0] });
    const routine2 = page.locator('li', { hasText: routineNames[1] });
    const bulkActionBar = page.locator('.fixed.bottom-4');

    // Seleciona duas rotinas
    await routine1.locator('input[type="checkbox"]').check();
    await routine2.locator('input[type="checkbox"]').check();

    // Lida com o diálogo de confirmação
    page.on('dialog', dialog => dialog.accept());

    // Clica no botão de deletar em lote
    await bulkActionBar.locator('button[title="Excluir Selecionadas"]').click();

    // Verifica a notificação de sucesso
    await expect(page.locator('text=2 rotinas foram atualizadas.')).toBeVisible();

    // Verifica se as rotinas foram removidas da lista
    await expect(routine1).not.toBeVisible();
    await expect(routine2).not.toBeVisible();
    await expect(page.locator('li', { hasText: routineNames[2] })).toBeVisible(); // A terceira deve permanecer

    // Verifica se a barra de ações desapareceu
    await expect(bulkActionBar).not.toBeVisible();
  });

  test('should enable/disable selected routines using bulk action', async ({ page }) => {
    // Este teste pode ser expandido para verificar a mudança de estado (ativar/desativar)
    // Por simplicidade, vamos apenas testar a ação de desativar
    const routine1 = page.locator('li', { hasText: routineNames[0] });
    const routine3 = page.locator('li', { hasText: routineNames[2] });
    const bulkActionBar = page.locator('.fixed.bottom-4');

    // Seleciona duas rotinas
    await routine1.locator('input[type="checkbox"]').check();
    await routine3.locator('input[type="checkbox"]').check();

    // Clica no botão de desativar em lote
    await bulkActionBar.locator('button[title="Desativar Selecionadas"]').click();

    // Verifica a notificação de sucesso
    await expect(page.locator('text=2 rotinas foram atualizadas.')).toBeVisible();

    // Verifica se as rotinas agora estão inativas (a UI deve refletir isso, ex: com um badge)
    await expect(routine1.locator('text=Inativa')).toBeVisible();
    await expect(routine3.locator('text=Inativa')).toBeVisible();

    // Verifica se a barra de ações desapareceu
    await expect(bulkActionBar).not.toBeVisible();
  });
});
