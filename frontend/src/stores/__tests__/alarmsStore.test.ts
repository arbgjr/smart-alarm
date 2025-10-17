import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useAlarmsStore } from '../alarmsStore';
import { backgroundSync } from '@/utils/backgroundSync';
import type { AlarmFormData } from '../alarmsStore';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

// Mock backgroundSync
vi.mock('@/utils/backgroundSync', () => ({
  backgroundSync: {
    addToSyncQueue: vi.fn(),
  },
}));

// Mock alarmService
const mockAlarmService = {
  createAlarm: vi.fn(),
  updateAlarm: vi.fn(),
  deleteAlarm: vi.fn(),
};

vi.mock('@/services/alarmService', () => ({
  alarmService: mockAlarmService,
}));

const mockAlarm = {
  id: '1',
  name: 'Morning Alarm',
  triggerTime: '07:00',
  time: '07:00',
  isEnabled: true,
  isRecurring: true,
  recurrencePattern: 'monday,tuesday,wednesday,thursday,friday',
  daysOfWeek: ['monday', 'tuesday', 'wednesday', 'thursday', 'friday'],
  description: 'Wake up for work',
  userId: 'user-1',
  createdAt: '2025-01-25T00:00:00.000Z',
  updatedAt: '2025-01-25T00:00:00.000Z',
};

const mockAlarmFormData: AlarmFormData = {
  name: 'Test Alarm',
  time: '08:00',
  isEnabled: true,
  daysOfWeek: ['monday', 'friday'],
  description: 'Test alarm description',
};

describe('AlarmsStore', () => {
  beforeEach(() => {
    // Clear store state before each test
    useAlarmsStore.getState().clearAll();
    vi.clearAllMocks();

    // Mock navigator.onLine
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: true,
    });
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Initial State', () => {
    it('should have correct initial state', () => {
      const { result } = renderHook(() => useAlarmsStore());

      expect(result.current.alarms).toEqual([]);
      expect(result.current.selectedAlarm).toBeNull();
      expect(result.current.filters).toEqual({});
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(result.current.currentPage).toBe(1);
      expect(result.current.totalPages).toBe(1);
      expect(result.current.totalCount).toBe(0);
      expect(result.current.pageSize).toBe(10);
    });
  });

  describe('Basic State Management', () => {
    it('should set alarms correctly', () => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.setAlarms([mockAlarm]);
      });

      expect(result.current.alarms).toEqual([mockAlarm]);
      expect(result.current.lastSync).toBeTruthy();
    });

    it('should add alarm correctly', () => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      expect(result.current.alarms).toEqual([mockAlarm]);
      expect(result.current.totalCount).toBe(1);
    });

    it('should update alarm correctly', () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      // Update alarm
      act(() => {
        result.current.updateAlarm('1', { name: 'Updated Alarm' });
      });

      expect(result.current.alarms[0].name).toBe('Updated Alarm');
      expect(result.current.alarms[0].id).toBe('1');
    });

    it('should remove alarm correctly', () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
        result.current.setSelectedAlarm(mockAlarm);
      });

      expect(result.current.alarms).toHaveLength(1);
      expect(result.current.selectedAlarm).toEqual(mockAlarm);

      // Remove alarm
      act(() => {
        result.current.removeAlarm('1');
      });

      expect(result.current.alarms).toEqual([]);
      expect(result.current.selectedAlarm).toBeNull();
      expect(result.current.totalCount).toBe(0);
    });

    it('should set selected alarm', () => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.setSelectedAlarm(mockAlarm);
      });

      expect(result.current.selectedAlarm).toEqual(mockAlarm);
    });

    it('should set filters and reset page', () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Set current page to 3
      act(() => {
        result.current.setPagination(3, 5, 50);
      });

      expect(result.current.currentPage).toBe(3);

      // Set filters should reset to page 1
      act(() => {
        result.current.setFilters({ enabled: true });
      });

      expect(result.current.filters).toEqual({ enabled: true });
      expect(result.current.currentPage).toBe(1);
    });

    it('should set pagination correctly', () => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.setPagination(2, 5, 45);
      });

      expect(result.current.currentPage).toBe(2);
      expect(result.current.totalPages).toBe(5);
      expect(result.current.totalCount).toBe(45);
    });
  });

  describe('CRUD Operations - Online', () => {
    beforeEach(() => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true });
    });

    it('should create alarm when online', async () => {
      const mockCreatedAlarm = { ...mockAlarm, id: 'real-id' };
      mockAlarmService.createAlarm.mockResolvedValue(mockCreatedAlarm);

      const { result } = renderHook(() => useAlarmsStore());

      await act(async () => {
        await result.current.createAlarm(mockAlarmFormData);
      });

      expect(mockAlarmService.createAlarm).toHaveBeenCalledWith({
        name: mockAlarmFormData.name,
        description: mockAlarmFormData.description,
        triggerTime: mockAlarmFormData.time,
        isRecurring: true,
        recurrencePattern: 'monday,friday',
      });

      expect(result.current.alarms).toHaveLength(1);
      expect(result.current.alarms[0].name).toBe('Test Alarm');
      expect(result.current.isLoading).toBe(false);
    });

    it('should update alarm when online', async () => {
      const updatedAlarmDto = { ...mockAlarm, name: 'Updated Alarm' };
      mockAlarmService.updateAlarm.mockResolvedValue(updatedAlarmDto);

      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      await act(async () => {
        await result.current.editAlarm('1', { name: 'Updated Alarm' });
      });

      expect(mockAlarmService.updateAlarm).toHaveBeenCalledWith('1', { name: 'Updated Alarm' });
      expect(result.current.alarms[0].name).toBe('Updated Alarm');
      expect(result.current.isLoading).toBe(false);
    });

    it('should delete alarm when online', async () => {
      mockAlarmService.deleteAlarm.mockResolvedValue(undefined);

      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      expect(result.current.alarms).toHaveLength(1);

      await act(async () => {
        await result.current.deleteAlarm('1');
      });

      expect(mockAlarmService.deleteAlarm).toHaveBeenCalledWith('1');
      expect(result.current.alarms).toEqual([]);
      expect(result.current.isLoading).toBe(false);
    });

    it('should toggle alarm', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      // Mock editAlarm method
      const editAlarmSpy = vi.spyOn(result.current, 'editAlarm').mockResolvedValue(mockAlarm);

      await act(async () => {
        await result.current.toggleAlarm('1');
      });

      expect(editAlarmSpy).toHaveBeenCalledWith('1', { isEnabled: false });
    });
  });

  describe('CRUD Operations - Offline', () => {
    beforeEach(() => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true });
    });

    it('should create alarm offline with background sync', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      await act(async () => {
        await result.current.createAlarm(mockAlarmFormData);
      });

      expect(result.current.alarms).toHaveLength(1);
      expect(result.current.alarms[0].name).toBe('Test Alarm');
      expect(result.current.alarms[0].id).toMatch(/^temp-/);
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalledWith('create', 'alarm', mockAlarmFormData);
      expect(mockAlarmService.createAlarm).not.toHaveBeenCalled();
    });

    it('should update alarm offline with background sync', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      await act(async () => {
        await result.current.editAlarm('1', { name: 'Updated Offline' });
      });

      expect(result.current.alarms[0].name).toBe('Updated Offline');
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalledWith('update', 'alarm', { id: '1', name: 'Updated Offline' });
      expect(mockAlarmService.updateAlarm).not.toHaveBeenCalled();
    });

    it('should delete alarm offline with background sync', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      await act(async () => {
        await result.current.deleteAlarm('1');
      });

      expect(result.current.alarms).toEqual([]);
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalledWith('delete', 'alarm', { id: '1' });
      expect(mockAlarmService.deleteAlarm).not.toHaveBeenCalled();
    });
  });

  describe('Error Handling', () => {
    it('should handle create alarm errors with rollback', async () => {
      const error = new Error('Network error');
      mockAlarmService.createAlarm.mockRejectedValue(error);

      const { result } = renderHook(() => useAlarmsStore());

      let thrownError;
      await act(async () => {
        try {
          await result.current.createAlarm(mockAlarmFormData);
        } catch (e) {
          thrownError = e;
        }
      });

      expect(thrownError).toBe(error);
      expect(result.current.alarms).toEqual([]);
      expect(result.current.error).toBe('Network error');
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalled();
    });

    it('should handle update alarm errors with rollback', async () => {
      const error = new Error('Update failed');
      mockAlarmService.updateAlarm.mockRejectedValue(error);

      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      const originalName = result.current.alarms[0].name;

      await act(async () => {
        try {
          await result.current.editAlarm('1', { name: 'Failed Update' });
        } catch (e) {
          // Expected to fail
        }
      });

      // Should rollback to original state
      expect(result.current.alarms[0].name).toBe(originalName);
      expect(result.current.error).toBe('Update failed');
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalled();
    });

    it('should handle delete alarm errors with restore', async () => {
      const error = new Error('Delete failed');
      mockAlarmService.deleteAlarm.mockRejectedValue(error);

      const { result } = renderHook(() => useAlarmsStore());

      // Add initial alarm
      act(() => {
        result.current.addAlarm(mockAlarm);
      });

      await act(async () => {
        try {
          await result.current.deleteAlarm('1');
        } catch (e) {
          // Expected to fail
        }
      });

      // Should restore the alarm
      expect(result.current.alarms).toHaveLength(1);
      expect(result.current.error).toBe('Delete failed');
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalled();
    });
  });

  describe('Bulk Operations', () => {
    beforeEach(() => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.addAlarm(mockAlarm);
        result.current.addAlarm({ ...mockAlarm, id: '2', name: 'Alarm 2' });
        result.current.addAlarm({ ...mockAlarm, id: '3', name: 'Alarm 3' });
      });
    });

    it('should enable multiple alarms', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      const editAlarmSpy = vi.spyOn(result.current, 'editAlarm').mockResolvedValue(mockAlarm);

      await act(async () => {
        await result.current.enableMultipleAlarms(['1', '2']);
      });

      expect(editAlarmSpy).toHaveBeenCalledTimes(2);
      expect(editAlarmSpy).toHaveBeenCalledWith('1', { isEnabled: true });
      expect(editAlarmSpy).toHaveBeenCalledWith('2', { isEnabled: true });
    });

    it('should disable multiple alarms', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      const editAlarmSpy = vi.spyOn(result.current, 'editAlarm').mockResolvedValue(mockAlarm);

      await act(async () => {
        await result.current.disableMultipleAlarms(['1', '3']);
      });

      expect(editAlarmSpy).toHaveBeenCalledTimes(2);
      expect(editAlarmSpy).toHaveBeenCalledWith('1', { isEnabled: false });
      expect(editAlarmSpy).toHaveBeenCalledWith('3', { isEnabled: false });
    });

    it('should delete multiple alarms', async () => {
      const { result } = renderHook(() => useAlarmsStore());

      const deleteAlarmSpy = vi.spyOn(result.current, 'deleteAlarm').mockResolvedValue();

      await act(async () => {
        await result.current.deleteMultipleAlarms(['2', '3']);
      });

      expect(deleteAlarmSpy).toHaveBeenCalledTimes(2);
      expect(deleteAlarmSpy).toHaveBeenCalledWith('2');
      expect(deleteAlarmSpy).toHaveBeenCalledWith('3');
    });
  });

  describe('Utility Functions', () => {
    beforeEach(() => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.addAlarm(mockAlarm);
        result.current.addAlarm({
          ...mockAlarm,
          id: '2',
          name: 'Disabled Alarm',
          isEnabled: false
        });
        result.current.addAlarm({
          ...mockAlarm,
          id: '3',
          name: 'Weekend Alarm',
          daysOfWeek: ['saturday', 'sunday']
        });
      });
    });

    it('should get alarm by ID', () => {
      const { result } = renderHook(() => useAlarmsStore());

      const alarm = result.current.getAlarmById('1');
      expect(alarm).toEqual(mockAlarm);

      const nonExistent = result.current.getAlarmById('999');
      expect(nonExistent).toBeUndefined();
    });

    it('should get active alarms only', () => {
      const { result } = renderHook(() => useAlarmsStore());

      const activeAlarms = result.current.getActiveAlarms();
      expect(activeAlarms).toHaveLength(2);
      expect(activeAlarms.every(alarm => alarm.isEnabled)).toBe(true);
    });

    it('should get alarms for specific day', () => {
      const { result } = renderHook(() => useAlarmsStore());

      const mondayAlarms = result.current.getAlarmsForDay('monday');
      expect(mondayAlarms).toHaveLength(1);
      expect(mondayAlarms[0].id).toBe('1');

      const saturdayAlarms = result.current.getAlarmsForDay('saturday');
      expect(saturdayAlarms).toHaveLength(1);
      expect(saturdayAlarms[0].id).toBe('3');
    });

    it('should get upcoming alarms', () => {
      // Mock current time
      const mockNow = new Date('2025-01-25T06:00:00.000Z');
      vi.setSystemTime(mockNow);

      const { result } = renderHook(() => useAlarmsStore());

      const upcomingAlarms = result.current.getUpcomingAlarms(2); // Next 2 hours
      expect(upcomingAlarms).toHaveLength(2); // Both enabled alarms at 07:00
    });

    it('should clear all data', () => {
      const { result } = renderHook(() => useAlarmsStore());

      expect(result.current.alarms).toHaveLength(3);

      act(() => {
        result.current.clearAll();
      });

      expect(result.current.alarms).toEqual([]);
      expect(result.current.selectedAlarm).toBeNull();
      expect(result.current.filters).toEqual({});
      expect(result.current.currentPage).toBe(1);
      expect(result.current.totalPages).toBe(1);
      expect(result.current.totalCount).toBe(0);
    });
  });

  describe('Persistence', () => {
    it('should persist alarms state to localStorage', () => {
      const { result } = renderHook(() => useAlarmsStore());

      act(() => {
        result.current.addAlarm(mockAlarm);
        result.current.setFilters({ enabled: true });
      });

      expect(localStorageMock.setItem).toHaveBeenCalled();
      const setItemCalls = vi.mocked(localStorageMock.setItem).mock.calls;
      const persistCall = setItemCalls.find(call => call[0] === 'smart-alarm-alarms');

      expect(persistCall).toBeDefined();
      const persistedData = JSON.parse(persistCall![1]);
      expect(persistedData.state.alarms).toHaveLength(1);
      expect(persistedData.state.filters).toEqual({ enabled: true });
    });
  });
});
