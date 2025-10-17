import { useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAlarmsStore } from '@/stores/alarmsStore';
import { alarmService, type AlarmQueryParams, type AlarmDto } from '@/services/alarmService';
import { alarmKeys } from './useAlarms';

// Convert AlarmDto to Alarm interface for store compatibility
const convertAlarmDtoToAlarm = (dto: AlarmDto) => ({
  ...dto,
  time: dto.triggerTime,
  daysOfWeek: dto.recurrencePattern ? dto.recurrencePattern.split(',') : [],
});

// Integration hook that combines Zustand and React Query for alarms
export function useAlarmsIntegration(params?: AlarmQueryParams) {
  const alarms = useAlarmsStore((state) => state.alarms);
  const { setAlarms, setLoading, setError, setPagination } = useAlarmsStore();

  const query = useQuery({
    queryKey: alarmKeys.list(params),
    queryFn: async () => {
      const response = await alarmService.getAlarms(params);

      // Convert DTOs to Alarm interface and sync with Zustand store
      const alarms = response.data.map(convertAlarmDtoToAlarm);
      setAlarms(alarms);
      setPagination(
        response.pageNumber,
        response.totalPages,
        response.totalElements
      );

      return response;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error: any) => {
      setError(error.message || 'Failed to fetch alarms');
    },
    onSuccess: () => {
      setError(null);
    }
  });

  // Sync loading state
  useEffect(() => {
    setLoading(query.isLoading);
  }, [query.isLoading, setLoading]);

  return {
    ...query,
    // Return Zustand data for immediate reactivity
    data: query.data ? { ...query.data, data: alarms } : undefined,
    alarms,
  };
}

// Enhanced mutation hooks that sync with both React Query and Zustand
export function useCreateAlarmIntegration() {
  const queryClient = useQueryClient();
  const { createAlarm } = useAlarmsStore();

  return useMutation({
    mutationFn: createAlarm,
    onSuccess: (newAlarm) => {
      // React Query cache updates
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });
      queryClient.setQueryData(alarmKeys.detail(newAlarm.id), newAlarm);

      // Zustand store is already updated by createAlarm action
    },
  });
}

export function useUpdateAlarmIntegration() {
  const queryClient = useQueryClient();
  const { editAlarm } = useAlarmsStore();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: any }) => editAlarm(id, data),
    onSuccess: (updatedAlarm) => {
      // React Query cache updates
      queryClient.setQueryData(alarmKeys.detail(updatedAlarm.id), updatedAlarm);
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      // Zustand store is already updated by editAlarm action
    },
  });
}

export function useDeleteAlarmIntegration() {
  const queryClient = useQueryClient();
  const { deleteAlarm } = useAlarmsStore();

  return useMutation({
    mutationFn: deleteAlarm,
    onSuccess: (_, deletedId) => {
      // React Query cache cleanup
      queryClient.removeQueries({ queryKey: alarmKeys.detail(deletedId) });
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      // Zustand store is already updated by deleteAlarm action
    },
  });
}

export function useToggleAlarmIntegration() {
  const queryClient = useQueryClient();
  const { toggleAlarm } = useAlarmsStore();

  return useMutation({
    mutationFn: toggleAlarm,
    onSuccess: () => {
      // Invalidate queries to reflect toggle state
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      // Zustand store is already updated by toggleAlarm action
    },
  });
}

// Bulk operations integration
export function useBulkAlarmsIntegration() {
  const queryClient = useQueryClient();
  const {
    enableMultipleAlarms,
    disableMultipleAlarms,
    deleteMultipleAlarms,
  } = useAlarmsStore();

  const invalidateAll = () => {
    queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
    queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
    queryClient.invalidateQueries({ queryKey: alarmKeys.today() });
  };

  return {
    enableMultiple: useMutation({
      mutationFn: enableMultipleAlarms,
      onSuccess: invalidateAll,
    }),
    disableMultiple: useMutation({
      mutationFn: disableMultipleAlarms,
      onSuccess: invalidateAll,
    }),
    deleteMultiple: useMutation({
      mutationFn: deleteMultipleAlarms,
      onSuccess: invalidateAll,
    }),
  };
}

// Hook to sync offline changes when going online
export function useOfflineSync() {
  const queryClient = useQueryClient();

  const syncOfflineChanges = () => {
    // Invalidate all queries to refetch from server
    queryClient.invalidateQueries({ queryKey: alarmKeys.all });
  };

  // Listen for online events
  useEffect(() => {
    const handleOnline = () => {
      syncOfflineChanges();
    };

    window.addEventListener('online', handleOnline);
    return () => window.removeEventListener('online', handleOnline);
  }, [syncOfflineChanges]);

  return { syncOfflineChanges };
}
