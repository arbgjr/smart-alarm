import { test, expect } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Routine Search and Filter E2E', () => {
  const routine1Name = `Routine A ${new Date().getTime()}`;
  const routine2Name = `Routine B ${new Date().getTime()}`;
  const routine3Name = `Routine C with keyword ${new Date().getTime()}`;

  test.beforeEach(async ({ page }) => {
    // Login
    await page.goto(`${UI_URL}/login`);
    await page.locator('input[name="email"]').fill('testuser@smartalarm.com');
    await page.locator('input[name="password"]').fill('Password123!');
    await page.locator('button[type="submit"]').click();
    await expect(page).toHaveURL(`${UI_URL}/dashboard`);

    // Navigate to routines page
    await page.goto(`${UI_URL}/routines`);
    await expect(page.locator('h1')).toContainText('Minhas Rotinas');

    // Create some test routines
    await createRoutine(page, routine1Name, 'Description for routine A', true); // Active
    await createRoutine(page, routine2Name, 'Description for routine B', false); // Inactive
    await createRoutine(page, routine3Name, 'Another routine with keyword', true); // Active, with keyword

    // Ensure all routines are visible initially (after creation, before any filters)
    await page.locator('button:has-text("Todas")').click(); // Reset filter
    await page.locator('input[placeholder="Buscar por nome da rotina..."]').clear(); // Clear search
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
  });

  test.afterEach(async ({ page }) => {
    // Clean up created routines
    await page.goto(`${UI_URL}/routines`);
    await page.locator('button:has-text("Todas")').click(); // Ensure all are visible for deletion
    await page.locator('input[placeholder="Buscar por nome da rotina..."]').clear();
    await deleteRoutineIfVisible(page, routine1Name);
    await deleteRoutineIfVisible(page, routine2Name);
    await deleteRoutineIfVisible(page, routine3Name);
  });

  test('should filter routines by status', async ({ page }) => {
    // Test 'Ativas' filter
    await page.locator('button:has-text("Ativas")').click();
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).not.toBeVisible();

    // Test 'Inativas' filter
    await page.locator('button:has-text("Inativas")').click();
    await expect(page.locator(`text=${routine2Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine1Name}`)).not.toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).not.toBeVisible();

    // Test 'Todas' filter
    await page.locator('button:has-text("Todas")').click();
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
  });

  test('should search routines by name', async ({ page }) => {
    const searchInput = page.locator('input[placeholder="Buscar por nome da rotina..."]');

    // Search for routine A
    await searchInput.fill(routine1Name);
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).not.toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).not.toBeVisible();

    // Clear search
    await searchInput.clear();
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();

    // Search for a keyword present in one routine
    await searchInput.fill('keyword');
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine1Name}`)).not.toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).not.toBeVisible();
  });

  test('should combine search and filter', async ({ page }) => {
    const searchInput = page.locator('input[placeholder="Buscar por nome da rotina..."]');

    // Search for 'keyword' and filter 'Ativas'
    await searchInput.fill('keyword');
    await page.locator('button:has-text("Ativas")').click();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine1Name}`)).not.toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).not.toBeVisible();

    // Change filter to 'Inativas' - should find nothing
    await page.locator('button:has-text("Inativas")').click();
    await expect(page.locator(`text=${routine3Name}`)).not.toBeVisible();
    await expect(page.locator('text=Nenhuma rotina encontrada.')).toBeVisible();

    // Clear search and filter
    await searchInput.clear();
    await page.locator('button:has-text("Todas")').click();
    await expect(page.locator(`text=${routine1Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine2Name}`)).toBeVisible();
    await expect(page.locator(`text=${routine3Name}`)).toBeVisible();
  });
});

// Helper functions (reused from previous tests, or could be in a separate file)
// These helpers assume the UI elements and their interactions are consistent.
async function createRoutine(page: any, name: string, description: string, isEnabled: boolean) {
  await page.locator('button:has-text("Nova Rotina")').click();
  await expect(page.locator('h2')).toContainText('Nova Rotina');
  await page.locator('input[name="name"]').fill(name);
  await page.locator('textarea[name="description"]').fill(description);
  await page.locator('button:has-text("Salvar")').click();
  await expect(page.locator('text=Rotina criada com sucesso!')).toBeVisible();

  // If routine needs to be inactive, toggle it
  if (!isEnabled) {
    const routineListItem = page.locator('li', { has: page.locator(`text=${name}`) });
    await routineListItem.locator('button[aria-label*="Desativar"]').click();
    await expect(page.locator('text=Rotina desativada com sucesso!')).toBeVisible();
  }
}

async function deleteRoutineIfVisible(page: any, name: string) {
  const routineLocator = page.locator('li', { has: page.locator(`text=${name}`) });
  if (await routineLocator.isVisible()) {
    page.on('dialog', dialog => dialog.accept());
    await routineLocator.locator('button[aria-label*="Excluir"]').click();
    await expect(page.locator('text=Rotina excluída com sucesso!')).toBeVisible();
  }
}
