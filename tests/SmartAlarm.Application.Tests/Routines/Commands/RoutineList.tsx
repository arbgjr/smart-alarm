import { RoutineDto } from '@/services/routineService';
import { PencilIcon, TrashIcon, PlayIcon, PowerIcon } from '@heroicons/react/24/outline';

interface RoutineListProps {
  routines: RoutineDto[];
  selectedIds: string[];
  onEdit: (routine: RoutineDto) => void;
  onDelete: (id: string) => void;
  onToggleEnable: (id: string, isEnabled: boolean) => void;
  onSelect: (id: string) => void;
  // ... outras props
}

export function RoutineList({ routines, selectedIds, onEdit, onDelete, onToggleEnable, onSelect }: RoutineListProps) {
  if (!routines || routines.length === 0) {
    return <p className="text-center text-gray-500">Nenhuma rotina encontrada.</p>;
  }

  return (
    <ul className="divide-y divide-gray-200">
      {routines.map((routine) => (
        <li key={routine.id} className="flex items-center justify-between py-4 px-2 rounded-md hover:bg-gray-50">
          <div className="flex items-center space-x-4">
            <input
              type="checkbox"
              checked={selectedIds.includes(routine.id)}
              onChange={() => onSelect(routine.id)}
              className="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
          <div className="flex-1">
            <p className="text-lg font-semibold text-gray-900">{routine.name}</p>
            <p className="text-sm text-gray-600">{routine.description}</p>
            <div className="mt-2 flex items-center space-x-2">
              <span
                className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  routine.isEnabled
                    ? 'bg-green-100 text-green-800'
                    : 'bg-red-100 text-red-800'
                }`}
              >
                {routine.isEnabled ? 'Ativa' : 'Inativa'}
              </span>
              <span className="text-sm text-gray-500">
                {routine.alarmIds.length} alarme(s)
              </span>
            </div>
          </div>
          </div>
          <div className="flex items-center space-x-2">
            {/* Botão de Edição (NOVO) */}
            <button
              onClick={() => onEdit(routine)}
              className="p-2 text-gray-500 hover:text-blue-600 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
              aria-label={`Editar rotina ${routine.name}`}
            >
              <PencilIcon className="h-5 w-5" />
            </button>

            <button
              onClick={() => onToggleEnable(routine.id, !routine.isEnabled)}
              className={`p-2 text-gray-500 hover:text-green-600 focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-2`}
              aria-label={routine.isEnabled ? `Desativar rotina ${routine.name}` : `Ativar rotina ${routine.name}`}
            >
              <PowerIcon className="h-5 w-5" />
            </button>

            <button
              onClick={() => onDelete(routine.id)}
              className="p-2 text-gray-500 hover:text-red-600 focus:outline-none focus:ring-2 focus:ring-red-500 focus:ring-offset-2"
              aria-label={`Excluir rotina ${routine.name}`}
            >
              <TrashIcon className="h-5 w-5" />
            </button>
          </div>
        </li>
      ))}
    </ul>
  );
}
