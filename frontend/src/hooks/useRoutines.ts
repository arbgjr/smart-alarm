import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  routineService,
  CreateRoutinePayload,
  UpdateRoutinePayload,
  BulkUpdateRoutinesPayload,
  PaginatedRoutinesResponse,
} from '@/services/routineService';
import { notificationService } from '@/services/notificationService';
import { RoutineDto } from '@/services/routineService';

interface UseRoutinesParams {
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export function useRoutines(params: UseRoutinesParams = {}) {
  const queryClient = useQueryClient();
  const queryKey = ['routines', params];

  const { data, isLoading, error } = useQuery<PaginatedRoutinesResponse>({
    queryKey,
    queryFn: () => routineService.getRoutines(params), // Assumindo que o serviço retorna a estrutura paginada
  });

  const invalidateRoutines = () => {
    queryClient.invalidateQueries({ queryKey: ['routines'] });
  };

  const createRoutine = useMutation({
    mutationFn: (payload: CreateRoutinePayload) => routineService.createRoutine(payload),
    onSuccess: () => {
      notificationService.success('Rotina criada com sucesso!');
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao criar rotina: ${err.message}`);
    },
  });

  const updateRoutine = useMutation({
    mutationFn: (payload: UpdateRoutinePayload) => routineService.updateRoutine(payload.id, payload),
    onSuccess: () => {
      notificationService.success('Rotina atualizada com sucesso!');
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao atualizar rotina: ${err.message}`);
    },
  });

  const deleteRoutine = useMutation({
    mutationFn: (id: string) => routineService.deleteRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina excluída com sucesso!');
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao excluir rotina: ${err.message}`);
    },
  });

  const enableRoutine = useMutation({
    mutationFn: (id: string) => routineService.enableRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina ativada com sucesso!');
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao ativar rotina: ${err.message}`);
    },
  });

  const disableRoutine = useMutation({
    mutationFn: (id: string) => routineService.disableRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina desativada com sucesso!');
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao desativar rotina: ${err.message}`);
    },
  });

  // Novas mutações para ações em lote
  const bulkUpdateMutation = useMutation({
    mutationFn: (payload: BulkUpdateRoutinesPayload) => routineService.bulkUpdateRoutines(payload),
    onSuccess: (data, variables) => {
      notificationService.success(`${variables.routineIds.length} rotinas foram atualizadas.`);
      invalidateRoutines();
    },
    onError: (err) => {
      notificationService.error(`Falha ao executar ação em lote: ${err.message}`);
    },
  });

  return {
    data: data, // Retorna o objeto paginado completo: { items, totalPages, ... }
    isLoading,
    error,
    createRoutine,
    updateRoutine,
    deleteRoutine,
    enableRoutine,
    disableRoutine,
    // Mutações em lote expostas para o componente
    deleteMultipleRoutines: { mutate: (ids: string[]) => bulkUpdateMutation.mutate({ routineIds: ids, action: 'Delete' }), ...bulkUpdateMutation },
    enableMultipleRoutines: { mutate: (ids: string[]) => bulkUpdateMutation.mutate({ routineIds: ids, action: 'Enable' }), ...bulkUpdateMutation },
    disableMultipleRoutines: { mutate: (ids: string[]) => bulkUpdateMutation.mutate({ routineIds: ids, action: 'Disable' }), ...bulkUpdateMutation },
  };
}
