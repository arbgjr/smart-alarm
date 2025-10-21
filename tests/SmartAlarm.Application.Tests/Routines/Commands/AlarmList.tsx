import { AlarmDto } from '@/services/alarmService';
import { PencilIcon, TrashIcon, BellSnoozeIcon } from '@heroicons/react/24/outline';

interface AlarmListProps {
  alarms: AlarmDto[];
  onEdit: (alarm: AlarmDto) => void;
  onDelete: (id: string) => void;
  onToggleEnable: (id: string, isEnabled: boolean) => void;
}

export function AlarmList({ alarms, onEdit, onDelete, onToggleEnable }: AlarmListProps) {
  if (!alarms || alarms.length === 0) {
    return <p className="text-center text-gray-500">Nenhum alarme encontrado.</p>;
  }

  return (
    <ul className="divide-y divide-gray-200">
      {alarms.map((alarm) => (
        <li key={alarm.id} className="flex items-center justify-between py-4">
          <div className="flex-1">
            <p className="text-lg font-semibold text-gray-900">{alarm.name}</p>
            <p className="text-sm text-gray-600">
              {new Date(alarm.triggerTime).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
            </p>
            <div className="mt-2">
              <span
                className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  alarm.isEnabled
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                }`}
              >
                {alarm.isEnabled ? 'Ativo' : 'Inativo'}
              </span>
            </div>
          </div>
          <div className="flex items-center space-x-2">
            {/* Botão de Edição */}
            <button
              onClick={() => onEdit(alarm)}
              className="p-2 text-gray-500 hover:text-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              aria-label={`Editar alarme ${alarm.name}`}
            >
              <PencilIcon className="h-5 w-5" />
            </button>

            <button
              onClick={() => onToggleEnable(alarm.id, !alarm.isEnabled)}
              className={`p-2 text-gray-500 hover:text-green-600 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2`}
              aria-label={alarm.isEnabled ? `Desativar alarme ${alarm.name}` : `Ativar alarme ${alarm.name}`}
            >
              <BellSnoozeIcon className="h-5 w-5" />
            </button>

            <button
              onClick={() => onDelete(alarm.id)}
              className="p-2 text-gray-500 hover:text-red-600 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2"
              aria-label={`Excluir alarme ${alarm.name}`}
            >
              <TrashIcon className="h-5 w-5" />
            </button>
          </div>
        </li>
      ))}
    </ul>
  );
}
