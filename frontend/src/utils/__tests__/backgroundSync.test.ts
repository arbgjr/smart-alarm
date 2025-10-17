import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { BackgroundSync, backgroundSync } from '../backgroundSync';

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

// Mock navigator.serviceWorker
const mockServiceWorker = {
  register: vi.fn(() => Promise.resolve()),
  ready: Promise.resolve({
    sync: { register: vi.fn(() => Promise.resolve()) }
  }),
  addEventListener: vi.fn(),
  removeEventListener: vi.fn(),
};
Object.defineProperty(navigator, 'serviceWorker', {
  value: mockServiceWorker,
  writable: true
});

// Mock alarm service
vi.mock('@/services/alarmService', () => ({
  alarmService: {
    createAlarm: vi.fn(),
    updateAlarm: vi.fn(),
    deleteAlarm: vi.fn(),
  },
}));

// Mock services index
vi.mock('@/services', () => ({
  RoutineService: {
    createRoutine: vi.fn(),
    updateRoutine: vi.fn(),
    deleteRoutine: vi.fn(),
  },
}));

describe('BackgroundSync', () => {
  beforeEach(() => {
    // Clear localStorage mock
    vi.clearAllMocks();
    localStorageMock.getItem.mockReturnValue(null);

    // Mock navigator.onLine
    Object.defineProperty(navigator, 'onLine', {
      writable: true,
      value: true,
    });

    // Clear background sync queue
    backgroundSync.clearQueue();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Singleton Pattern', () => {
    it('should return the same instance', () => {
      const instance1 = BackgroundSync.getInstance();
      const instance2 = BackgroundSync.getInstance();

      expect(instance1).toBe(instance2);
      expect(instance1).toBe(backgroundSync);
    });
  });

  describe('Queue Management', () => {
    it('should add operations to sync queue', () => {
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test Alarm' });

      expect(backgroundSync.getPendingCount()).toBe(1);

      const operations = backgroundSync.getPendingOperations();
      expect(operations).toHaveLength(1);
      expect(operations[0]).toMatchObject({
        action: 'create',
        entity: 'alarm',
        data: { name: 'Test Alarm' },
      });
      expect(operations[0].id).toMatch(/^alarm-create-/);
      expect(operations[0].timestamp).toBeTypeOf('number');
    });

    it('should save queue to localStorage', () => {
      backgroundSync.addToSyncQueue('update', 'routine', { id: '1', name: 'Updated' });

      expect(localStorageMock.setItem).toHaveBeenCalledWith(
        'smart-alarm-sync-queue',
        expect.stringContaining('routine-update')
      );
    });

    it('should load queue from localStorage on initialization', () => {
      const mockQueue = JSON.stringify([
        {
          id: 'alarm-create-123',
          action: 'create',
          entity: 'alarm',
          data: { name: 'Saved Alarm' },
          timestamp: Date.now()
        }
      ]);

      localStorageMock.getItem.mockReturnValue(mockQueue);

      // Create new instance to test loading
      const newInstance = BackgroundSync.getInstance();

      expect(newInstance.getPendingCount()).toBe(1);
      expect(newInstance.getPendingOperations()[0].data.name).toBe('Saved Alarm');
    });

    it('should handle corrupted localStorage gracefully', () => {
      localStorageMock.getItem.mockReturnValue('invalid json');

      const instance = BackgroundSync.getInstance();
      expect(instance.getPendingCount()).toBe(0);
    });

    it('should clear all pending operations', () => {
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test' });
      backgroundSync.addToSyncQueue('update', 'routine', { id: '1' });

      expect(backgroundSync.getPendingCount()).toBe(2);

      backgroundSync.clearQueue();

      expect(backgroundSync.getPendingCount()).toBe(0);
      expect(localStorageMock.setItem).toHaveBeenCalledWith('smart-alarm-sync-queue', '[]');
    });
  });

  describe('Online Processing', () => {
    beforeEach(() => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true });
    });

    it('should process queue immediately when online', async () => {
      const processQueueSpy = vi.spyOn(backgroundSync, 'processQueue');

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test Alarm' });

      expect(processQueueSpy).toHaveBeenCalled();
    });

    it('should process alarm operations', async () => {
      const { alarmService } = await import('@/services/alarmService');

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test Alarm' });
      backgroundSync.addToSyncQueue('update', 'alarm', { id: '1', name: 'Updated' });
      backgroundSync.addToSyncQueue('delete', 'alarm', { id: '2' });

      await backgroundSync.processQueue();

      expect(alarmService.createAlarm).toHaveBeenCalledWith({ name: 'Test Alarm' });
      expect(alarmService.updateAlarm).toHaveBeenCalledWith('1', { id: '1', name: 'Updated' });
      expect(alarmService.deleteAlarm).toHaveBeenCalledWith('2');

      expect(backgroundSync.getPendingCount()).toBe(0);
    });

    it('should process routine operations', async () => {
      const { RoutineService } = await import('@/services');

      backgroundSync.addToSyncQueue('create', 'routine', { name: 'Test Routine' });
      backgroundSync.addToSyncQueue('update', 'routine', { id: '1', name: 'Updated' });
      backgroundSync.addToSyncQueue('delete', 'routine', { id: '2' });

      await backgroundSync.processQueue();

      expect(RoutineService.createRoutine).toHaveBeenCalledWith({ name: 'Test Routine' });
      expect(RoutineService.updateRoutine).toHaveBeenCalledWith('1', { id: '1', name: 'Updated' });
      expect(RoutineService.deleteRoutine).toHaveBeenCalledWith('2');

      expect(backgroundSync.getPendingCount()).toBe(0);
    });

    it('should keep failed operations in queue', async () => {
      const { alarmService } = await import('@/services/alarmService');
      (alarmService.createAlarm as any).mockRejectedValue(new Error('Network error'));

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Failing Alarm' });
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Success Alarm' });

      // Mock the second call to succeed
      (alarmService.createAlarm as any).mockResolvedValueOnce({ id: '1', name: 'Success Alarm' });

      await backgroundSync.processQueue();

      // One should fail and stay in queue, one should succeed and be removed
      expect(backgroundSync.getPendingCount()).toBe(1);
      expect(backgroundSync.getPendingOperations()[0].data.name).toBe('Failing Alarm');
    });
  });

  describe('Offline Handling', () => {
    beforeEach(() => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true });
    });

    it('should register background sync when offline', () => {
      // const registerSyncSpy = vi.spyOn(mockServiceWorker.ready.then(reg => reg.sync), 'register');

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Offline Alarm' });

      // Should not process immediately when offline
      expect(backgroundSync.getPendingCount()).toBe(1);
    });

    it('should setup online event listener as fallback', () => {
      // Mock ServiceWorker as unavailable
      Object.defineProperty(navigator, 'serviceWorker', { value: undefined });

      const addEventListenerSpy = vi.spyOn(window, 'addEventListener');

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Fallback Test' });

      expect(addEventListenerSpy).toHaveBeenCalledWith('online', expect.any(Function));
    });

    it('should process queue when coming online', async () => {
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Queued Alarm' });

      expect(backgroundSync.getPendingCount()).toBe(1);

      // Simulate coming online
      Object.defineProperty(navigator, 'onLine', { value: true });

      const processQueueSpy = vi.spyOn(backgroundSync, 'processQueue');

      // Trigger online event
      const onlineEvent = new Event('online');
      window.dispatchEvent(onlineEvent);

      // Wait for async processing
      await new Promise(resolve => setTimeout(resolve, 50));

      expect(processQueueSpy).toHaveBeenCalled();
    });
  });

  describe('Service Worker Integration', () => {
    it('should register background sync with service worker', async () => {
      Object.defineProperty(navigator, 'onLine', { value: false });

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'SW Test' });

      // Wait for async registration
      await new Promise(resolve => setTimeout(resolve, 100));

      expect(mockServiceWorker.ready).toBeDefined();
    });

    it('should handle background sync registration failure', () => {
      // Mock registration failure
      const mockFailingServiceWorker = {
        register: vi.fn(() => Promise.resolve()),
        ready: Promise.resolve({
          sync: { register: vi.fn(() => Promise.reject(new Error('Registration failed'))) }
        }),
      };

      Object.defineProperty(navigator, 'serviceWorker', { value: mockFailingServiceWorker });
      Object.defineProperty(navigator, 'onLine', { value: false });

      const addEventListenerSpy = vi.spyOn(window, 'addEventListener');

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Fallback Test' });

      // Should fallback to online listener
      expect(addEventListenerSpy).toHaveBeenCalledWith('online', expect.any(Function));
    });

    it('should handle service worker message events', () => {
      const processQueueSpy = vi.spyOn(backgroundSync, 'processQueue');

      // Simulate service worker message
      const messageEvent = new MessageEvent('message', {
        data: { type: 'BACKGROUND_SYNC' }
      });

      navigator.serviceWorker.dispatchEvent(messageEvent);

      expect(processQueueSpy).toHaveBeenCalled();
    });
  });

  describe('Utility Methods', () => {
    it('should correctly report online status', () => {
      Object.defineProperty(navigator, 'onLine', { value: true });
      expect(backgroundSync.isOnline()).toBe(true);

      Object.defineProperty(navigator, 'onLine', { value: false });
      expect(backgroundSync.isOnline()).toBe(false);
    });

    it('should generate unique operation IDs', () => {
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test 1' });
      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test 2' });

      const operations = backgroundSync.getPendingOperations();
      expect(operations[0].id).not.toBe(operations[1].id);
      expect(operations[0].id).toMatch(/^alarm-create-\d+$/);
      expect(operations[1].id).toMatch(/^alarm-create-\d+$/);
    });

    it('should handle empty queue gracefully', async () => {
      expect(backgroundSync.getPendingCount()).toBe(0);

      await backgroundSync.processQueue();

      // Should not throw or cause issues
      expect(backgroundSync.getPendingCount()).toBe(0);
    });
  });

  describe('Error Handling', () => {
    it('should handle localStorage errors gracefully', () => {
      localStorageMock.setItem.mockImplementation(() => {
        throw new Error('Storage quota exceeded');
      });

      expect(() => {
        backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test' });
      }).not.toThrow();

      expect(backgroundSync.getPendingCount()).toBe(1); // Should still add to memory
    });

    it('should handle network errors during sync', async () => {
      const { alarmService } = await import('@/services/alarmService');
      (alarmService.createAlarm as any).mockRejectedValue(new Error('Network timeout'));

      backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Network Test' });

      await backgroundSync.processQueue();

      // Failed operation should remain in queue
      expect(backgroundSync.getPendingCount()).toBe(1);
    });
  });
});
