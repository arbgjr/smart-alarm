import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  routineService,
  CreateRoutinePayload,
  UpdateRoutinePayload,
  BulkUpdateRoutinesPayload,
  PaginatedRoutinesResponse,
} from '@/services/routineService';
import { notificationService } from '@/services/notificationService';

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
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao criar rotina: ${errorMessage}`);
    },
  });

  const updateRoutine = useMutation({
    mutationFn: (payload: UpdateRoutinePayload) => routineService.updateRoutine(payload.id, payload),
    onSuccess: () => {
      notificationService.success('Rotina atualizada com sucesso!');
      invalidateRoutines();
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao atualizar rotina: ${errorMessage}`);
    },
  });

  const deleteRoutine = useMutation({
    mutationFn: (id: string) => routineService.deleteRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina excluída com sucesso!');
      invalidateRoutines();
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao excluir rotina: ${errorMessage}`);
    },
  });

  const enableRoutine = useMutation({
    mutationFn: (id: string) => routineService.enableRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina ativada com sucesso!');
      invalidateRoutines();
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao ativar rotina: ${errorMessage}`);
    },
  });

  const disableRoutine = useMutation({
    mutationFn: (id: string) => routineService.disableRoutine(id),
    onSuccess: () => {
      notificationService.success('Rotina desativada com sucesso!');
      invalidateRoutines();
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao desativar rotina: ${errorMessage}`);
    },
  });

  // Novas mutações para ações em lote
  const bulkUpdateMutation = useMutation({
    mutationFn: (payload: BulkUpdateRoutinesPayload) => routineService.bulkUpdateRoutines(payload),
    onSuccess: (_data, variables) => {
      notificationService.success(`${variables.routineIds.length} rotinas foram atualizadas.`);
      invalidateRoutines();
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Falha ao executar ação em lote: ${errorMessage}`);
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
    deleteMultipleRoutines: {
      mutateAsync: (ids: string[]) => bulkUpdateMutation.mutateAsync({ routineIds: ids, action: 'Delete' }),
      isPending: bulkUpdateMutation.isPending,
      error: bulkUpdateMutation.error,
    },
    enableMultipleRoutines: {
      mutateAsync: (ids: string[]) => bulkUpdateMutation.mutateAsync({ routineIds: ids, action: 'Enable' }),
      isPending: bulkUpdateMutation.isPending,
      error: bulkUpdateMutation.error,
    },
    disableMultipleRoutines: {
      mutateAsync: (ids: string[]) => bulkUpdateMutation.mutateAsync({ routineIds: ids, action: 'Disable' }),
      isPending: bulkUpdateMutation.isPending,
      error: bulkUpdateMutation.error,
    },
  };
}

// Exports nomeados para resolver erros de hooks faltando
export const useCreateRoutine = (params: UseRoutinesParams = {}) => {
  const { createRoutine } = useRoutines(params);
  return createRoutine;
};

export const useUpdateRoutine = (params: UseRoutinesParams = {}) => {
  const { updateRoutine } = useRoutines(params);
  return updateRoutine;
};

export const useEnableRoutine = (params: UseRoutinesParams = {}) => {
  const { enableRoutine } = useRoutines(params);
  return enableRoutine;
};

export const useDisableRoutine = (params: UseRoutinesParams = {}) => {
  const { disableRoutine } = useRoutines(params);
  return disableRoutine;
};

export const useDeleteRoutine = (params: UseRoutinesParams = {}) => {
  const { deleteRoutine } = useRoutines(params);
  return deleteRoutine;
};

// Hook específico para rotinas ativas - filtrando apenas isEnabled: true
export const useActiveRoutines = (params: Omit<UseRoutinesParams, 'status'> = {}) => {
  return useRoutines({ ...params, status: 'active' });
};

// Para compatibilidade temporária - será removido quando componentes forem atualizados
export const useExecuteRoutine = () => {
  // Funcionalidade de execução de rotinas não implementada ainda no backend
  return useMutation({
    mutationFn: async (_id: string) => {
      throw new Error('Execução de rotinas não implementada ainda');
    },
    onError: (err: unknown) => {
      const errorMessage = err instanceof Error ? err.message : 'Erro desconhecido';
      notificationService.error(`Erro ao executar rotina: ${errorMessage}`);
    },
  });
};
