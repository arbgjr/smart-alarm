import { test, expect } from '@playwright/test';

const UI_URL = 'http://localhost:5173';

test.describe('Alarm Management E2E', () => {
  const testAlarmName = `E2E Test Alarm ${new Date().getTime()}`;
  const updatedTestAlarmName = `${testAlarmName} - Updated`;

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

  test('should allow a user to create, view, edit, and delete an alarm', async ({ page }) => {
    // 1. Navegação para a página de Alarmes
    await page.goto(`${UI_URL}/alarms`);
    await expect(page.locator('h1')).toContainText('Meus Alarmes');

    // 2. Criação de um novo alarme
    await page.locator('button:has-text("Novo Alarme")').click();

    // Preenche o formulário no modal
    await expect(page.locator('h2')).toContainText('Novo Alarme');
    await page.locator('input[name="name"]').fill(testAlarmName);

    // Define um horário para o alarme (ex: amanhã às 08:30)
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(8, 30, 0, 0);
    const formattedDateTime = tomorrow.toISOString().slice(0, 16);
    await page.locator('input[name="triggerTime"]').fill(formattedDateTime);

    await page.locator('button:has-text("Salvar")').click();

    // 3. Verificação da criação
    await expect(page.locator('text=Alarme criado com sucesso!')).toBeVisible();
    await expect(page.locator(`text=${testAlarmName}`)).toBeVisible();

    // Encontra o item da lista que contém o nosso alarme de teste
    const alarmListItem = page.locator('li', { has: page.locator(`text=${testAlarmName}`) });

    // 4. Edição do alarme
    await alarmListItem.locator('button[aria-label*="Editar"]').click();

    // Altera os dados no formulário de edição
    await expect(page.locator('h2')).toContainText('Editar Alarme');
    await page.locator('input[name="name"]').fill(updatedTestAlarmName);
    await page.locator('button:has-text("Salvar")').click();

    // 5. Verificação da edição
    await expect(page.locator('text=Alarme atualizado com sucesso!')).toBeVisible();
    await expect(page.locator(`text=${testAlarmName}`)).not.toBeVisible();
    await expect(page.locator(`text=${updatedTestAlarmName}`)).toBeVisible();

    // 6. Exclusão do alarme
    // Reencontra o item da lista com o nome atualizado
    const updatedAlarmListItem = page.locator('li', { has: page.locator(`text=${updatedTestAlarmName}`) });

    // Lida com o diálogo de confirmação do navegador
    page.on('dialog', dialog => dialog.accept());

    await updatedAlarmListItem.locator('button[aria-label*="Excluir"]').click();

    // 7. Verificação da exclusão
    await expect(page.locator('text=Alarme excluído com sucesso!')).toBeVisible();
    await expect(page.locator(`text=${updatedTestAlarmName}`)).not.toBeVisible();
  });
});
