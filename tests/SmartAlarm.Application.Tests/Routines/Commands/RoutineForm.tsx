import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useRoutines } from '@/hooks/useRoutines';
import { RoutineDto } from '@/services/routineService';

const routineSchema = z.object({
  name: z.string().min(1, 'O nome é obrigatório.').max(100),
  description: z.string().max(500).optional(),
  alarmIds: z.array(z.string().uuid()).optional(),
});

type RoutineFormData = z.infer<typeof routineSchema>;

interface RoutineFormProps {
  routineToEdit?: RoutineDto | null;
  onClose: () => void;
}

export function RoutineForm({ routineToEdit, onClose }: RoutineFormProps) {
  const { createRoutine, updateRoutine } = useRoutines();
  const isEditMode = !!routineToEdit;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<RoutineFormData>({
    resolver: zodResolver(routineSchema),
    defaultValues: {
      name: '',
      description: '',
      alarmIds: [],
    },
  });

  useEffect(() => {
    // Pré-preenche o formulário se estiver em modo de edição
    if (isEditMode) {
      reset({
        name: routineToEdit.name,
        description: routineToEdit.description,
        alarmIds: routineToEdit.alarmIds.map(String), // Garante que são strings
      });
    }
  }, [routineToEdit, isEditMode, reset]);

  const onSubmit = async (data: RoutineFormData) => {
    try {
      if (isEditMode) {
        // Chama a mutação de atualização
        await updateRoutine.mutateAsync({
          id: routineToEdit.id,
          ...data,
        });
      } else {
        // Chama a mutação de criação
        await createRoutine.mutateAsync(data);
      }
      onClose(); // Fecha o modal em caso de sucesso
    } catch (error) {
      console.error('Falha ao salvar a rotina:', error);
      // Aqui você pode exibir uma notificação de erro para o usuário
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <h2 className="text-2xl font-bold">{isEditMode ? 'Editar Rotina' : 'Nova Rotina'}</h2>
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700">Nome</label>
        <input
          id="name"
          {...register('name')}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
        />
        {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>}
      </div>
      <div>
        <label htmlFor="description" className="block text-sm font-medium text-gray-700">Descrição</label>
        <textarea
          id="description"
          {...register('description')}
          rows={3}
          className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
        />
      </div>
      {/* Aqui viria a lógica para selecionar os alarmes (AlarmIds) */}
      <div className="flex justify-end space-x-2">
        <button type="button" onClick={onClose} className="rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50">
          Cancelar
        </button>
        <button type="submit" disabled={isSubmitting} className="inline-flex justify-center rounded-md border border-transparent bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 disabled:opacity-50">
          {isSubmitting ? 'Salvando...' : 'Salvar'}
        </button>
      </div>
    </form>
  );
}
