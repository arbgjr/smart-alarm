import { test, expect } from '@playwright/test';
import { AlarmPage } from '../page-objects/AlarmPage';
import { LoginPage } from '../page-objects/LoginPage';
import { generateTestAlarm } from '../helpers/test-data';

test.describe('Alarm Management', () => {
  let alarmPage: AlarmPage;
  let loginPage: LoginPage;

  test.beforeEach(async ({ page }) => {
    alarmPage = new AlarmPage(page);
    loginPage = new LoginPage(page);
    
    // Login with test user
    await loginPage.goto();
    await loginPage.loginWithTestUser();
    
    // Navigate to alarms page
    await alarmPage.goto();
  });

  test('should create a new alarm successfully', async () => {
    const testAlarm = generateTestAlarm();
    
    await test.step('Open create alarm modal', async () => {
      await alarmPage.clickCreateAlarmButton();
      await expect(alarmPage.createAlarmModal).toBeVisible();
    });

    await test.step('Fill alarm details', async () => {
      await alarmPage.fillAlarmName(testAlarm.name);
      await alarmPage.setAlarmTime(testAlarm.time);
      await alarmPage.selectDaysOfWeek(testAlarm.daysOfWeek);
    });

    await test.step('Save alarm', async () => {
      await alarmPage.saveAlarm();
      await expect(alarmPage.createAlarmModal).not.toBeVisible();
    });

    await test.step('Verify alarm appears in list', async () => {
      await expect(alarmPage.getAlarmByName(testAlarm.name)).toBeVisible();
      
      const alarmCard = alarmPage.getAlarmByName(testAlarm.name);
      await expect(alarmCard.locator('.alarm-time')).toContainText(testAlarm.time);
      await expect(alarmCard.locator('.alarm-enabled')).toBeChecked();
    });
  });

  test('should edit an existing alarm', async () => {
    // First create an alarm
    const originalAlarm = generateTestAlarm();
    await alarmPage.createAlarmViaAPI(originalAlarm);
    await alarmPage.reload();

    const updatedAlarm = generateTestAlarm({ name: 'Updated Alarm' });

    await test.step('Open edit alarm modal', async () => {
      await alarmPage.clickEditAlarm(originalAlarm.name);
      await expect(alarmPage.editAlarmModal).toBeVisible();
    });

    await test.step('Update alarm details', async () => {
      await alarmPage.clearAlarmName();
      await alarmPage.fillAlarmName(updatedAlarm.name);
      await alarmPage.setAlarmTime(updatedAlarm.time);
    });

    await test.step('Save changes', async () => {
      await alarmPage.saveAlarm();
      await expect(alarmPage.editAlarmModal).not.toBeVisible();
    });

    await test.step('Verify alarm was updated', async () => {
      await expect(alarmPage.getAlarmByName(updatedAlarm.name)).toBeVisible();
      await expect(alarmPage.getAlarmByName(originalAlarm.name)).not.toBeVisible();
      
      const updatedAlarmCard = alarmPage.getAlarmByName(updatedAlarm.name);
      await expect(updatedAlarmCard.locator('.alarm-time')).toContainText(updatedAlarm.time);
    });
  });

  test('should delete an alarm', async () => {
    // Create an alarm to delete
    const testAlarm = generateTestAlarm();
    await alarmPage.createAlarmViaAPI(testAlarm);
    await alarmPage.reload();

    await test.step('Verify alarm exists', async () => {
      await expect(alarmPage.getAlarmByName(testAlarm.name)).toBeVisible();
    });

    await test.step('Delete alarm', async () => {
      await alarmPage.deleteAlarm(testAlarm.name);
    });

    await test.step('Confirm deletion', async () => {
      await expect(alarmPage.confirmDeleteModal).toBeVisible();
      await alarmPage.confirmDelete();
    });

    await test.step('Verify alarm was deleted', async () => {
      await expect(alarmPage.getAlarmByName(testAlarm.name)).not.toBeVisible();
    });
  });

  test('should toggle alarm on/off', async () => {
    const testAlarm = generateTestAlarm();
    await alarmPage.createAlarmViaAPI(testAlarm);
    await alarmPage.reload();

    const alarmCard = alarmPage.getAlarmByName(testAlarm.name);
    const toggleButton = alarmCard.locator('.alarm-toggle');

    await test.step('Verify alarm is initially enabled', async () => {
      await expect(toggleButton).toBeChecked();
      await expect(alarmCard).toHaveClass(/enabled/);
    });

    await test.step('Disable alarm', async () => {
      await toggleButton.click();
      await expect(toggleButton).not.toBeChecked();
      await expect(alarmCard).toHaveClass(/disabled/);
    });

    await test.step('Re-enable alarm', async () => {
      await toggleButton.click();
      await expect(toggleButton).toBeChecked();
      await expect(alarmCard).toHaveClass(/enabled/);
    });
  });

  test('should handle alarm validation errors', async () => {
    await test.step('Open create alarm modal', async () => {
      await alarmPage.clickCreateAlarmButton();
    });

    await test.step('Try to save empty alarm', async () => {
      await alarmPage.saveAlarm();
      
      // Should show validation errors
      await expect(alarmPage.nameValidationError).toBeVisible();
      await expect(alarmPage.timeValidationError).toBeVisible();
    });

    await test.step('Fill invalid time', async () => {
      await alarmPage.fillAlarmName('Test Alarm');
      await alarmPage.setAlarmTime('25:70'); // Invalid time
      await alarmPage.saveAlarm();
      
      await expect(alarmPage.timeValidationError).toContainText('Invalid time format');
    });

    await test.step('Fix errors and save successfully', async () => {
      await alarmPage.setAlarmTime('07:30');
      await alarmPage.selectDaysOfWeek(['Monday', 'Tuesday']);
      await alarmPage.saveAlarm();
      
      await expect(alarmPage.createAlarmModal).not.toBeVisible();
      await expect(alarmPage.getAlarmByName('Test Alarm')).toBeVisible();
    });
  });

  test('should filter alarms by status', async () => {
    // Create test alarms
    const enabledAlarm = generateTestAlarm({ name: 'Enabled Alarm' });
    const disabledAlarm = generateTestAlarm({ name: 'Disabled Alarm', enabled: false });
    
    await alarmPage.createAlarmViaAPI(enabledAlarm);
    await alarmPage.createAlarmViaAPI(disabledAlarm);
    await alarmPage.reload();

    await test.step('Show all alarms by default', async () => {
      await expect(alarmPage.getAlarmByName(enabledAlarm.name)).toBeVisible();
      await expect(alarmPage.getAlarmByName(disabledAlarm.name)).toBeVisible();
    });

    await test.step('Filter by enabled alarms only', async () => {
      await alarmPage.filterByStatus('enabled');
      
      await expect(alarmPage.getAlarmByName(enabledAlarm.name)).toBeVisible();
      await expect(alarmPage.getAlarmByName(disabledAlarm.name)).not.toBeVisible();
    });

    await test.step('Filter by disabled alarms only', async () => {
      await alarmPage.filterByStatus('disabled');
      
      await expect(alarmPage.getAlarmByName(enabledAlarm.name)).not.toBeVisible();
      await expect(alarmPage.getAlarmByName(disabledAlarm.name)).toBeVisible();
    });

    await test.step('Show all alarms again', async () => {
      await alarmPage.filterByStatus('all');
      
      await expect(alarmPage.getAlarmByName(enabledAlarm.name)).toBeVisible();
      await expect(alarmPage.getAlarmByName(disabledAlarm.name)).toBeVisible();
    });
  });

  test('should search alarms by name', async () => {
    // Create test alarms with different names
    await alarmPage.createAlarmViaAPI(generateTestAlarm({ name: 'Morning Workout' }));
    await alarmPage.createAlarmViaAPI(generateTestAlarm({ name: 'Evening Meditation' }));
    await alarmPage.createAlarmViaAPI(generateTestAlarm({ name: 'Work Meeting' }));
    await alarmPage.reload();

    await test.step('Search for specific alarm', async () => {
      await alarmPage.searchAlarms('Morning');
      
      await expect(alarmPage.getAlarmByName('Morning Workout')).toBeVisible();
      await expect(alarmPage.getAlarmByName('Evening Meditation')).not.toBeVisible();
      await expect(alarmPage.getAlarmByName('Work Meeting')).not.toBeVisible();
    });

    await test.step('Search for partial match', async () => {
      await alarmPage.searchAlarms('ing');
      
      await expect(alarmPage.getAlarmByName('Morning Workout')).toBeVisible();
      await expect(alarmPage.getAlarmByName('Evening Meditation')).not.toBeVisible();
      await expect(alarmPage.getAlarmByName('Work Meeting')).toBeVisible();
    });

    await test.step('Clear search', async () => {
      await alarmPage.clearSearch();
      
      await expect(alarmPage.getAlarmByName('Morning Workout')).toBeVisible();
      await expect(alarmPage.getAlarmByName('Evening Meditation')).toBeVisible();
      await expect(alarmPage.getAlarmByName('Work Meeting')).toBeVisible();
    });
  });

  test('should handle bulk operations', async () => {
    // Create multiple test alarms
    const alarms = [
      generateTestAlarm({ name: 'Alarm 1' }),
      generateTestAlarm({ name: 'Alarm 2' }),
      generateTestAlarm({ name: 'Alarm 3' })
    ];

    for (const alarm of alarms) {
      await alarmPage.createAlarmViaAPI(alarm);
    }
    await alarmPage.reload();

    await test.step('Select multiple alarms', async () => {
      await alarmPage.selectAlarmCheckbox('Alarm 1');
      await alarmPage.selectAlarmCheckbox('Alarm 2');
      
      await expect(alarmPage.bulkActionsBar).toBeVisible();
      await expect(alarmPage.selectedCount).toContainText('2 selected');
    });

    await test.step('Bulk disable alarms', async () => {
      await alarmPage.clickBulkDisable();
      
      const alarm1Toggle = alarmPage.getAlarmByName('Alarm 1').locator('.alarm-toggle');
      const alarm2Toggle = alarmPage.getAlarmByName('Alarm 2').locator('.alarm-toggle');
      const alarm3Toggle = alarmPage.getAlarmByName('Alarm 3').locator('.alarm-toggle');
      
      await expect(alarm1Toggle).not.toBeChecked();
      await expect(alarm2Toggle).not.toBeChecked();
      await expect(alarm3Toggle).toBeChecked(); // Not selected, should remain enabled
    });

    await test.step('Bulk delete alarms', async () => {
      await alarmPage.selectAllAlarms();
      await alarmPage.clickBulkDelete();
      await alarmPage.confirmBulkDelete();
      
      await expect(alarmPage.getAlarmByName('Alarm 1')).not.toBeVisible();
      await expect(alarmPage.getAlarmByName('Alarm 2')).not.toBeVisible();
      await expect(alarmPage.getAlarmByName('Alarm 3')).not.toBeVisible();
    });
  });

  test.describe('Accessibility', () => {
    test('should be keyboard navigable', async ({ page }) => {
      const testAlarm = generateTestAlarm();
      await alarmPage.createAlarmViaAPI(testAlarm);
      await alarmPage.reload();

      await test.step('Navigate with keyboard', async () => {
        // Tab to create button
        await page.keyboard.press('Tab');
        await expect(alarmPage.createAlarmButton).toBeFocused();
        
        // Tab to first alarm
        await page.keyboard.press('Tab');
        const firstAlarm = page.locator('.alarm-card').first();
        await expect(firstAlarm).toBeFocused();
        
        // Use arrow keys to navigate between alarms
        await page.keyboard.press('ArrowDown');
        // Should focus next alarm if exists
      });

      await test.step('Activate elements with Enter/Space', async () => {
        await alarmPage.createAlarmButton.focus();
        await page.keyboard.press('Enter');
        await expect(alarmPage.createAlarmModal).toBeVisible();
        
        await page.keyboard.press('Escape');
        await expect(alarmPage.createAlarmModal).not.toBeVisible();
      });
    });

    test('should have proper ARIA labels', async () => {
      const testAlarm = generateTestAlarm();
      await alarmPage.createAlarmViaAPI(testAlarm);
      await alarmPage.reload();

      const alarmCard = alarmPage.getAlarmByName(testAlarm.name);
      
      await expect(alarmCard).toHaveAttribute('role', 'article');
      await expect(alarmCard).toHaveAttribute('aria-label');
      
      const toggle = alarmCard.locator('.alarm-toggle');
      await expect(toggle).toHaveAttribute('aria-label');
      await expect(toggle).toHaveAttribute('role', 'switch');
    });
  });
});