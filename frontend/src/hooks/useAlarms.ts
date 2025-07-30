import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  alarmService,
  type CreateAlarmRequest,
  type UpdateAlarmRequest,
  type AlarmQueryParams
} from '../services/alarmService';

// Simple toast notification replacement
const toast = {
  success: (message: string) => console.log(`✅ ${message}`),
  error: (message: string) => console.error(`❌ ${message}`),
};

// Query keys for React Query
export const alarmKeys = {
  all: ['alarms'] as const,
  lists: () => [...alarmKeys.all, 'list'] as const,
  list: (params?: AlarmQueryParams) => [...alarmKeys.lists(), params] as const,
  details: () => [...alarmKeys.all, 'detail'] as const,
  detail: (id: string) => [...alarmKeys.details(), id] as const,
  active: () => [...alarmKeys.all, 'active'] as const,
  today: () => [...alarmKeys.all, 'today'] as const,
};

// Hook to get paginated alarms
export function useAlarms(params?: AlarmQueryParams) {
  return useQuery({
    queryKey: alarmKeys.list(params),
    queryFn: () => alarmService.getAlarms(params),
    staleTime: 1000 * 60 * 5, // 5 minutes
    cacheTime: 1000 * 60 * 10, // 10 minutes
  });
}

// Hook to get a single alarm
export function useAlarm(id: string) {
  return useQuery({
    queryKey: alarmKeys.detail(id),
    queryFn: () => alarmService.getAlarm(id),
    enabled: !!id,
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

// Hook to get active alarms
export function useActiveAlarms() {
  return useQuery({
    queryKey: alarmKeys.active(),
    queryFn: () => alarmService.getActiveAlarms(),
    staleTime: 1000 * 60, // 1 minute
    cacheTime: 1000 * 60 * 5, // 5 minutes
  });
}

// Hook to get today's alarms
export function useTodaysAlarms() {
  return useQuery({
    queryKey: alarmKeys.today(),
    queryFn: () => alarmService.getTodaysAlarms(),
    staleTime: 1000 * 60, // 1 minute
    cacheTime: 1000 * 60 * 5, // 5 minutes
  });
}

// Hook to create an alarm
export function useCreateAlarm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (alarm: CreateAlarmRequest) => alarmService.createAlarm(alarm),
    onSuccess: (newAlarm) => {
      // Invalidate and refetch alarm lists
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      // Add the new alarm to the cache
      queryClient.setQueryData(alarmKeys.detail(newAlarm.id), newAlarm);

      toast.success('Alarm created successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to create alarm';
      toast.error(message);
    },
  });
}

// Hook to update an alarm
export function useUpdateAlarm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, alarm }: { id: string; alarm: UpdateAlarmRequest }) =>
      alarmService.updateAlarm(id, alarm),
    onSuccess: (updatedAlarm) => {
      // Update the alarm in the cache
      queryClient.setQueryData(alarmKeys.detail(updatedAlarm.id), updatedAlarm);

      // Invalidate lists to ensure they're up to date
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      toast.success('Alarm updated successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to update alarm';
      toast.error(message);
    },
  });
}

// Hook to delete an alarm
export function useDeleteAlarm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alarmService.deleteAlarm(id),
    onSuccess: (_, deletedId) => {
      // Remove the alarm from the cache
      queryClient.removeQueries({ queryKey: alarmKeys.detail(deletedId) });

      // Invalidate lists to remove the deleted alarm
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      toast.success('Alarm deleted successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to delete alarm';
      toast.error(message);
    },
  });
}

// Hook to enable an alarm
export function useEnableAlarm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alarmService.enableAlarm(id),
    onSuccess: (updatedAlarm) => {
      // Update the alarm in the cache
      queryClient.setQueryData(alarmKeys.detail(updatedAlarm.id), updatedAlarm);

      // Invalidate lists to update enabled state
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      toast.success('Alarm enabled successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to enable alarm';
      toast.error(message);
    },
  });
}

// Hook to disable an alarm
export function useDisableAlarm() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => alarmService.disableAlarm(id),
    onSuccess: (updatedAlarm) => {
      // Update the alarm in the cache
      queryClient.setQueryData(alarmKeys.detail(updatedAlarm.id), updatedAlarm);

      // Invalidate lists to update enabled state
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.active() });
      queryClient.invalidateQueries({ queryKey: alarmKeys.today() });

      toast.success('Alarm disabled successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to disable alarm';
      toast.error(message);
    },
  });
}

// Hook to trigger an alarm manually
export function useTriggerAlarm() {
  return useMutation({
    mutationFn: (id: string) => alarmService.triggerAlarm(id),
    onSuccess: () => {
      toast.success('Alarm triggered successfully!');
    },
    onError: (error: any) => {
      const message = error?.response?.data?.message || 'Failed to trigger alarm';
      toast.error(message);
    },
  });
}
