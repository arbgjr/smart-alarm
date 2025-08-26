import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { act } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useAlarmsIntegration, useCreateAlarmIntegration } from '../useAlarmsIntegration';
import { useAlarmsStore } from '@/stores/alarmsStore';
import { alarmService } from '@/services/alarmService';
import type { ReactNode } from 'react';

// Mock alarmService
vi.mock('@/services/alarmService', () => ({
  alarmService: {
    getAlarms: vi.fn(),
    createAlarm: vi.fn(),
    updateAlarm: vi.fn(),
    deleteAlarm: vi.fn(),
  },
}));

// Mock backgroundSync
vi.mock('@/utils/backgroundSync', () => ({
  backgroundSync: {
    addToSyncQueue: vi.fn(),
  },
}));

const mockAlarmDto = {
  id: '1',
  name: 'Test Alarm',
  triggerTime: '08:00',
  isEnabled: true,
  isRecurring: true,
  recurrencePattern: 'monday,friday',
  description: 'Test description',
  userId: 'user-1',
  createdAt: '2025-01-25T00:00:00.000Z',
  updatedAt: '2025-01-25T00:00:00.000Z',
};

const mockPaginatedResponse = {
  data: [mockAlarmDto],
  pageNumber: 1,
  pageSize: 10,
  totalPages: 1,
  totalElements: 1,
  hasNext: false,
  hasPrevious: false,
};

// Helper to create query client wrapper
const createWrapper = () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

describe('useAlarmsIntegration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAlarmsStore.getState().clearAll();
    
    // Mock successful API response
    vi.mocked(alarmService.getAlarms).mockResolvedValue(mockPaginatedResponse);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Data Fetching Integration', () => {
    it('should fetch alarms and sync with Zustand store', async () => {
      const { result } = renderHook(() => useAlarmsIntegration(), {
        wrapper: createWrapper(),
      });

      // Initially loading
      expect(result.current.isLoading).toBe(true);
      expect(result.current.alarms).toEqual([]);

      // Wait for data to load
      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      // Check that data is available from both React Query and Zustand
      expect(result.current.data).toBeDefined();
      expect(result.current.data?.data).toHaveLength(1);
      expect(result.current.alarms).toHaveLength(1);
      
      // Check converted alarm format
      const alarm = result.current.alarms[0];
      expect(alarm.name).toBe('Test Alarm');
      expect(alarm.time).toBe('08:00');
      expect(alarm.triggerTime).toBe('08:00');
      expect(alarm.daysOfWeek).toEqual(['monday', 'friday']);
    });

    it('should sync pagination data with store', async () => {
      const { result } = renderHook(() => useAlarmsIntegration(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isLoading).toBe(false);
      });

      // Check pagination was synced to Zustand
      const storeState = useAlarmsStore.getState();
      expect(storeState.currentPage).toBe(1);
      expect(storeState.totalPages).toBe(1);
      expect(storeState.totalCount).toBe(1);
    });

    it('should handle query parameters', async () => {
      const params = { pageNumber: 2, isEnabled: true };
      
      renderHook(() => useAlarmsIntegration(params), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(alarmService.getAlarms).toHaveBeenCalledWith(params);
      });
    });

    it('should handle API errors', async () => {
      const error = new Error('API Error');
      vi.mocked(alarmService.getAlarms).mockRejectedValue(error);

      const { result } = renderHook(() => useAlarmsIntegration(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isError).toBe(true);
      });

      // Check error was synced to Zustand
      const storeState = useAlarmsStore.getState();
      expect(storeState.error).toBe('API Error');
    });

    it('should clear error on successful fetch', async () => {
      // Set initial error
      act(() => {
        useAlarmsStore.getState().setError('Previous error');
      });

      const { result } = renderHook(() => useAlarmsIntegration(), {
        wrapper: createWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Check error was cleared
      const storeState = useAlarmsStore.getState();
      expect(storeState.error).toBeNull();
    });
  });

  describe('Loading State Synchronization', () => {
    it('should sync loading state with Zustand store', async () => {
      // Mock slow API response
      vi.mocked(alarmService.getAlarms).mockImplementation(
        () => new Promise(resolve => 
          setTimeout(() => resolve(mockPaginatedResponse), 100)
        )
      );

      renderHook(() => useAlarmsIntegration(), {
        wrapper: createWrapper(),
      });

      // Check initial loading state
      let storeState = useAlarmsStore.getState();
      expect(storeState.isLoading).toBe(true);

      await waitFor(() => {
        storeState = useAlarmsStore.getState();
        expect(storeState.isLoading).toBe(false);
      });
    });
  });

  describe('Cache Integration', () => {
    it('should use cached data when available', async () => {
      const wrapper = createWrapper();
      
      // First render - should fetch from API
      const { result: result1 } = renderHook(() => useAlarmsIntegration(), {
        wrapper,
      });

      await waitFor(() => {
        expect(result1.current.isSuccess).toBe(true);
      });

      expect(alarmService.getAlarms).toHaveBeenCalledTimes(1);

      // Second render with same params - should use cache
      const { result: result2 } = renderHook(() => useAlarmsIntegration(), {
        wrapper,
      });

      // Should immediately have data from cache
      expect(result2.current.data).toBeDefined();
      expect(result2.current.isLoading).toBe(false);
      
      // API should not be called again
      expect(alarmService.getAlarms).toHaveBeenCalledTimes(1);
    });
  });
});

describe('useCreateAlarmIntegration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAlarmsStore.getState().clearAll();
    
    vi.mocked(alarmService.createAlarm).mockResolvedValue(mockAlarmDto);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('Mutation Integration', () => {
    it('should create alarm and sync with both stores', async () => {
      const { result } = renderHook(() => useCreateAlarmIntegration(), {
        wrapper: createWrapper(),
      });

      const alarmData = {
        name: 'New Alarm',
        time: '09:00',
        isEnabled: true,
        daysOfWeek: ['monday'],
        description: 'New test alarm',
      };

      // Trigger mutation
      await act(async () => {
        await result.current.mutateAsync(alarmData);
      });

      expect(result.current.isSuccess).toBe(true);
      expect(result.current.data).toEqual(mockAlarmDto);
      
      // Check Zustand store was updated
      const storeState = useAlarmsStore.getState();
      expect(storeState.alarms).toHaveLength(1);
      expect(storeState.alarms[0].name).toBe('Test Alarm'); // From createAlarm response
    });

    it('should handle mutation errors', async () => {
      const error = new Error('Creation failed');
      vi.mocked(alarmService.createAlarm).mockRejectedValue(error);

      const { result } = renderHook(() => useCreateAlarmIntegration(), {
        wrapper: createWrapper(),
      });

      const alarmData = {
        name: 'Failing Alarm',
        time: '10:00',
      };

      // Trigger mutation
      let mutationError;
      await act(async () => {
        try {
          await result.current.mutateAsync(alarmData);
        } catch (e) {
          mutationError = e;
        }
      });

      expect(result.current.isError).toBe(true);
      expect(mutationError).toBe(error);
      
      // Store should handle error appropriately (via createAlarm action)
      const storeState = useAlarmsStore.getState();
      expect(storeState.alarms).toHaveLength(0); // Rollback occurred
    });

    it('should invalidate queries on successful creation', async () => {
      const queryClient = new QueryClient();
      const invalidateQueriesSpy = vi.spyOn(queryClient, 'invalidateQueries');
      
      const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>
          {children}
        </QueryClientProvider>
      );

      const { result } = renderHook(() => useCreateAlarmIntegration(), {
        wrapper,
      });

      const alarmData = {
        name: 'Cache Invalidation Test',
        time: '11:00',
      };

      await act(async () => {
        await result.current.mutateAsync(alarmData);
      });

      // Check that relevant queries were invalidated
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ 
        queryKey: expect.arrayContaining(['alarms', 'list']) 
      });
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ 
        queryKey: expect.arrayContaining(['alarms', 'active']) 
      });
      expect(invalidateQueriesSpy).toHaveBeenCalledWith({ 
        queryKey: expect.arrayContaining(['alarms', 'today']) 
      });
    });
  });

  describe('Optimistic Updates', () => {
    it('should show optimistic update immediately', async () => {
      // Mock slow API response
      vi.mocked(alarmService.createAlarm).mockImplementation(
        () => new Promise(resolve => 
          setTimeout(() => resolve(mockAlarmDto), 100)
        )
      );

      const { result } = renderHook(() => useCreateAlarmIntegration(), {
        wrapper: createWrapper(),
      });

      const alarmData = {
        name: 'Optimistic Alarm',
        time: '12:00',
      };

      // Trigger mutation without awaiting
      act(() => {
        result.current.mutate(alarmData);
      });

      // Check that optimistic update is immediately visible in store
      await waitFor(() => {
        const storeState = useAlarmsStore.getState();
        expect(storeState.alarms).toHaveLength(1);
        expect(storeState.alarms[0].name).toBe('Optimistic Alarm');
        expect(storeState.alarms[0].id).toMatch(/^temp-/); // Temporary ID
      });

      // Wait for API call to complete
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
      });

      // Check that real data replaced optimistic data
      const finalState = useAlarmsStore.getState();
      expect(finalState.alarms[0].id).toBe('1'); // Real ID from API
      expect(finalState.alarms[0].name).toBe('Test Alarm'); // Real name from API
    });
  });

  describe('Offline Handling', () => {
    beforeEach(() => {
      // Mock offline state
      Object.defineProperty(navigator, 'onLine', {
        writable: true,
        value: false,
      });
    });

    it('should queue operations when offline', async () => {
      const { backgroundSync } = await import('@/utils/backgroundSync');
      
      const { result } = renderHook(() => useCreateAlarmIntegration(), {
        wrapper: createWrapper(),
      });

      const alarmData = {
        name: 'Offline Alarm',
        time: '13:00',
      };

      await act(async () => {
        await result.current.mutateAsync(alarmData);
      });

      // Should not call API when offline
      expect(alarmService.createAlarm).not.toHaveBeenCalled();
      
      // Should queue for background sync
      expect(backgroundSync.addToSyncQueue).toHaveBeenCalledWith(
        'create',
        'alarm',
        alarmData
      );
    });
  });
});