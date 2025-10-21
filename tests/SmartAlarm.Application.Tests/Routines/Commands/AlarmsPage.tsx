import { useState } from 'react';
import { AlarmList } from '@/components/AlarmList/AlarmList';
import { AlarmForm } from '@/components/AlarmForm/AlarmForm';
import { Modal } from '@/components/shared/Modal';
import { useAlarms } from '@/hooks/useAlarms'; // Supondo que este hook exista
import { AlarmDto } from '@/services/alarmService';

export function AlarmsPage() {
  const { alarms, deleteAlarm, enableAlarm, disableAlarm, isLoading, error } = useAlarms();
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [alarmToEdit, setAlarmToEdit] = useState<AlarmDto | null>(null);

  const handleOpenCreateForm = () => {
    setAlarmToEdit(null);
    setIsFormOpen(true);
  };

  const handleOpenEditForm = (alarm: AlarmDto) => {
    setAlarmToEdit(alarm);
    setIsFormOpen(true);
  };

  const handleCloseForm = () => {
    setIsFormOpen(false);
    setAlarmToEdit(null);
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Tem certeza que deseja excluir este alarme?')) {
      deleteAlarm.mutate(id);
    }
  };

  const handleToggleEnable = (id: string, isEnabled: boolean) => {
    isEnabled ? disableAlarm.mutate(id) : enableAlarm.mutate(id);
  };

  return (
    <div className="container mx-auto p-4">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-3xl font-bold">Meus Alarmes</h1>
        <button onClick={handleOpenCreateForm} className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700">
          Novo Alarme
        </button>
      </div>

      {isLoading && <p>Carregando alarmes...</p>}
      {error && <p className="text-red-500">Erro ao carregar alarmes: {error.message}</p>}
      {!isLoading && !error && <AlarmList alarms={alarms || []} onEdit={handleOpenEditForm} onDelete={handleDelete} onToggleEnable={handleToggleEnable} />}

      <Modal isOpen={isFormOpen} onClose={handleCloseForm}>
        <AlarmForm alarmToEdit={alarmToEdit} onClose={handleCloseForm} />
      </Modal>
    </div>
  );
}
