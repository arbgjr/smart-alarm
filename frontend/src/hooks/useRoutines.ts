import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';

import { RoutineService, RoutineDto, CreateRoutineRequest, UpdateRoutineRequest, RoutineFilters } from '../services/routineService';

// Query Keys
export const routineKeys = {
  all: ['routines'] as const,
  lists: () => [...routineKeys.all, 'list'] as const,
  list: (filters: RoutineFilters) => [...routineKeys.lists(), filters] as const,
  details: () => [...routineKeys.all, 'detail'] as const,
  detail: (id: string) => [...routineKeys.details(), id] as const,
  active: () => [...routineKeys.all, 'active'] as const,
  steps: (routineId: string) => [...routineKeys.all, 'steps', routineId] as const,
};

/**
 * Hook to fetch routines with filtering and pagination
 */
export function useRoutines(filters: RoutineFilters = {}) {
  return useQuery({
    queryKey: routineKeys.list(filters),
    queryFn: () => RoutineService.getRoutines(filters),
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: 2,
  });
}

/**
 * Hook to fetch active routines for dashboard
 */
export function useActiveRoutines() {
  return useQuery({
    queryKey: routineKeys.active(),
    queryFn: () => RoutineService.getActiveRoutines(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    retry: 2,
  });
}

/**
 * Hook to fetch a specific routine by ID
 */
export function useRoutine(id: string) {
  return useQuery({
    queryKey: routineKeys.detail(id),
    queryFn: () => RoutineService.getRoutine(id),
    enabled: !!id,
  });
}

/**
 * Hook to fetch routine steps
 */
export function useRoutineSteps(routineId: string) {
  return useQuery({
    queryKey: routineKeys.steps(routineId),
    queryFn: () => RoutineService.getRoutineSteps(routineId),
    enabled: !!routineId,
  });
}

/**
 * Hook to create a new routine
 */
export function useCreateRoutine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateRoutineRequest) => RoutineService.createRoutine(data),
    onSuccess: (newRoutine) => {
      // Invalidate and refetch routines list
      queryClient.invalidateQueries({ queryKey: routineKeys.lists() });
      queryClient.invalidateQueries({ queryKey: routineKeys.active() });

      // Add the new routine to the cache
      queryClient.setQueryData(routineKeys.detail(newRoutine.id), newRoutine);

      console.log('Routine created successfully:', newRoutine.name);
    },
    onError: (error) => {
      console.error('Failed to create routine:', error);
    },
  });
}

/**
 * Hook to update an existing routine
 */
export function useUpdateRoutine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRoutineRequest }) =>
      RoutineService.updateRoutine(id, data),
    onSuccess: (updatedRoutine) => {
      // Update the specific routine in cache
      queryClient.setQueryData(routineKeys.detail(updatedRoutine.id), updatedRoutine);

      // Invalidate lists to ensure consistency
      queryClient.invalidateQueries({ queryKey: routineKeys.lists() });
      queryClient.invalidateQueries({ queryKey: routineKeys.active() });

      console.log('Routine updated successfully:', updatedRoutine.name);
    },
    onError: (error) => {
      console.error('Failed to update routine:', error);
    },
  });
}

/**
 * Hook to delete a routine
 */
export function useDeleteRoutine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => RoutineService.deleteRoutine(id),
    onSuccess: (_, deletedId) => {
      // Remove from cache
      queryClient.removeQueries({ queryKey: routineKeys.detail(deletedId) });

      // Invalidate lists
      queryClient.invalidateQueries({ queryKey: routineKeys.lists() });
      queryClient.invalidateQueries({ queryKey: routineKeys.active() });

      console.log('Routine deleted successfully');
    },
    onError: (error) => {
      console.error('Failed to delete routine:', error);
    },
  });
}

/**
 * Hook to enable a routine
 */
export function useEnableRoutine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => RoutineService.enableRoutine(id),
    onMutate: async (id) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: routineKeys.detail(id) });

      // Snapshot previous value
      const previousRoutine = queryClient.getQueryData<RoutineDto>(routineKeys.detail(id));

      // Optimistically update
      if (previousRoutine) {
        queryClient.setQueryData(routineKeys.detail(id), {
          ...previousRoutine,
          isEnabled: true,
        });
      }

      return { previousRoutine };
    },
    onError: (_error, id, context) => {
      // Rollback on error
      if (context?.previousRoutine) {
        queryClient.setQueryData(routineKeys.detail(id), context.previousRoutine);
      }
      console.error('Failed to enable routine');
    },
    onSuccess: (updatedRoutine) => {
      // Update cache with server response
      queryClient.setQueryData(routineKeys.detail(updatedRoutine.id), updatedRoutine);
      queryClient.invalidateQueries({ queryKey: routineKeys.lists() });
      queryClient.invalidateQueries({ queryKey: routineKeys.active() });

      console.log('Routine enabled successfully:', updatedRoutine.name);
    },
  });
}

/**
 * Hook to disable a routine
 */
export function useDisableRoutine() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => RoutineService.disableRoutine(id),
    onMutate: async (id) => {
      await queryClient.cancelQueries({ queryKey: routineKeys.detail(id) });

      const previousRoutine = queryClient.getQueryData<RoutineDto>(routineKeys.detail(id));

      if (previousRoutine) {
        queryClient.setQueryData(routineKeys.detail(id), {
          ...previousRoutine,
          isEnabled: false,
        });
      }

      return { previousRoutine };
    },
    onError: (_error, id, context) => {
      if (context?.previousRoutine) {
        queryClient.setQueryData(routineKeys.detail(id), context.previousRoutine);
      }
      console.error('Failed to disable routine');
    },
    onSuccess: (updatedRoutine) => {
      queryClient.setQueryData(routineKeys.detail(updatedRoutine.id), updatedRoutine);
      queryClient.invalidateQueries({ queryKey: routineKeys.lists() });
      queryClient.invalidateQueries({ queryKey: routineKeys.active() });

      console.log('Routine disabled successfully:', updatedRoutine.name);
    },
  });
}

/**
 * Hook to execute a routine manually
 */
export function useExecuteRoutine() {
  return useMutation({
    mutationFn: (id: string) => RoutineService.executeRoutine(id),
    onSuccess: () => {
      console.log('Routine executed successfully');
    },
    onError: (error) => {
      console.error('Failed to execute routine:', error);
    },
  });
}
