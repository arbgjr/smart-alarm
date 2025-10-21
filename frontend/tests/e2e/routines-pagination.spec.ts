import { test, expect, Page } from '@playwright/test';

const UI_URL = 'http://localhost:5173';
const PAGE_SIZE = 10; // Deve ser o mesmo que o definido no frontend
const TOTAL_ROUTINES_TO_CREATE = 12; // Criar mais que uma página

test.describe('Routine Pagination E2E', () => {
  const routineNames: string[] = [];

  // Antes de todos os testes, cria os dados necessários
  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();

    // Login
    await page.goto(`${UI_URL}/login`);
    await page.locator('input[name="email"]').fill('testuser@smartalarm.com');
    await page.locator('input[name="password"]').fill('Password123!');
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(`${UI_URL}/dashboard`);

    // Navigate to routines page
    await page.goto(`${UI_URL}/routines`);
    await expect(page.locator('h1')).toContainText('Minhas Rotinas');

    // Create enough routines to trigger pagination
    for (let i = 1; i <= TOTAL_ROUTINES_TO_CREATE; i++) {
      const name = `Pagination Test Routine ${String(i).padStart(2, '0')}`;
      routineNames.push(name);
      await createRoutine(page, name);
    }

    await context.close();
  });

  // Depois de todos os testes, limpa os dados criados
  test.afterAll(async ({ browser }) => {
    const context = await browser.newContext();
    const page = await context.newPage();

    // Login
    await page.goto(`${UI_URL}/login`);
    await page.locator('input[name="email"]').fill('testuser@smartalarm.com');
    await page.locator('input[name="password"]').fill('Password123!');
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(`${UI_URL}/dashboard`);

    // Navigate to routines page and delete all test routines
    await page.goto(`${UI_URL}/routines`);
    for (const name of routineNames) {
      await deleteRoutineIfVisible(page, name);
    }

    await context.close();
  });

  test('should display pagination controls and navigate between pages', async ({ page }) => {
    await page.goto(`${UI_URL}/routines`);

    // 1. Verifica se a paginação está visível
    const pagination = page.locator('nav[aria-label="Pagination"]');
    await expect(pagination).toBeVisible();

    // 2. Verifica o conteúdo da primeira página
    await expect(page.locator(`text=${routineNames[0]}`)).toBeVisible();
    await expect(page.locator(`text=${routineNames[PAGE_SIZE - 1]}`)).toBeVisible();
    await expect(page.locator(`text=${routineNames[PAGE_SIZE]}`)).not.toBeVisible();
    await expect(page.locator('text=Mostrando 1 a 10 de 12 resultados')).toBeVisible();

    // 3. Navega para a próxima página
    await pagination.locator('button:has-text("Próximo")').click();

    // 4. Verifica o conteúdo da segunda página
    await expect(page.locator(`text=${routineNames[0]}`)).not.toBeVisible();
    await expect(page.locator(`text=${routineNames[PAGE_SIZE]}`)).toBeVisible();
    await expect(page.locator(`text=${routineNames[TOTAL_ROUTINES_TO_CREATE - 1]}`)).toBeVisible();
    await expect(page.locator('text=Mostrando 11 a 12 de 12 resultados')).toBeVisible();

    // 5. Navega para a página anterior
    await pagination.locator('button:has-text("Anterior")').click();

    // 6. Verifica se voltou para a primeira página
    await expect(page.locator(`text=${routineNames[0]}`)).toBeVisible();
    await expect(page.locator(`text=${routineNames[PAGE_SIZE]}`)).not.toBeVisible();
  });
});

// Helper functions
async function createRoutine(page: Page, name: string) {
  await page.locator('button:has-text("Nova Rotina")').click();
  await page.locator('input[name="name"]').fill(name);
  await page.locator('button:has-text("Salvar")').click();
  await expect(page.locator('text=Rotina criada com sucesso!')).toBeVisible();
}

async function deleteRoutineIfVisible(page: Page, name: string) {
  // This might need to handle pagination if the item is not on the first page
  const routineLocator = page.locator('li', { has: page.locator(`text=${name}`) });
  if (await routineLocator.isVisible()) {
    page.on('dialog', dialog => dialog.accept());
    await routineLocator.locator('button[aria-label*="Excluir"]').click();
    await expect(page.locator('text=Rotina excluída com sucesso!')).toBeVisible();
  }
}
