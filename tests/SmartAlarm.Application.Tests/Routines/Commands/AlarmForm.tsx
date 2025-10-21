import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { useAlarms } from '@/hooks/useAlarms'; // Supondo que este hook exista
import { AlarmDto } from '@/services/alarmService';

const alarmSchema = z.object({
  name: z.string().min(1, 'O nome é obrigatório.').max(100),
  triggerTime: z.string().refine((val) => !isNaN(Date.parse(val)), {
    message: 'Data e hora inválidas.',
  }),
});

type AlarmFormData = z.infer<typeof alarmSchema>;

interface AlarmFormProps {
  alarmToEdit?: AlarmDto | null;
  onClose: () => void;
}

export function AlarmForm({ alarmToEdit, onClose }: AlarmFormProps) {
  const { createAlarm, updateAlarm } = useAlarms();
  const isEditMode = !!alarmToEdit;

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<AlarmFormData>({
    resolver: zodResolver(alarmSchema),
  });

  useEffect(() => {
    if (isEditMode) {
      reset({
        name: alarmToEdit.name,
        // Formata a data para o input datetime-local
        triggerTime: new Date(alarmToEdit.triggerTime).toISOString().slice(0, 16),
      });
    } else {
      reset({ name: '', triggerTime: '' });
    }
  }, [alarmToEdit, isEditMode, reset]);

  const onSubmit = async (data: AlarmFormData) => {
    const payload = {
      ...data,
      triggerTime: new Date(data.triggerTime).toISOString(),
    };

    try {
      if (isEditMode) {
        await updateAlarm.mutateAsync({ id: alarmToEdit.id, ...payload });
      } else {
        await createAlarm.mutateAsync(payload);
      }
      onClose();
    } catch (error) {
      console.error('Falha ao salvar o alarme:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <h2 className="text-2xl font-bold">{isEditMode ? 'Editar Alarme' : 'Novo Alarme'}</h2>
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700">Nome</label>
        <input id="name" {...register('name')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm" />
        {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>}
      </div>
      <div>
        <label htmlFor="triggerTime" className="block text-sm font-medium text-gray-700">Horário</label>
        <input id="triggerTime" type="datetime-local" {...register('triggerTime')} className="mt-1 block w-full rounded-md border-gray-300 shadow-sm" />
        {errors.triggerTime && <p className="mt-1 text-sm text-red-600">{errors.triggerTime.message}</p>}
      </div>
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
