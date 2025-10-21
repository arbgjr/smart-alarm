import { TrashIcon, PowerIcon, XMarkIcon, CheckCircleIcon, NoSymbolIcon } from '@heroicons/react/24/outline';

interface BulkActionBarProps {
  selectedCount: number;
  onClearSelection: () => void;
  onDelete: () => void;
  onEnable: () => void;
  onDisable: () => void;
}

export function BulkActionBar({
  selectedCount,
  onClearSelection,
  onDelete,
  onEnable,
  onDisable,
}: BulkActionBarProps) {
  if (selectedCount === 0) {
    return null;
  }

  return (
    <div className="fixed bottom-4 left-1/2 -translate-x-1/2 w-auto bg-gray-800 text-white rounded-lg shadow-lg p-2 flex items-center space-x-4 z-50 animate-fade-in-up">
      <span className="font-semibold px-2">{selectedCount} selecionada(s)</span>
      <div className="h-6 w-px bg-gray-600" />
      <div className="flex items-center space-x-2">
        <button onClick={onEnable} title="Ativar Selecionadas" className="p-2 rounded-full hover:bg-gray-700">
          <CheckCircleIcon className="h-5 w-5 text-green-400" />
        </button>
        <button onClick={onDisable} title="Desativar Selecionadas" className="p-2 rounded-full hover:bg-gray-700">
          <NoSymbolIcon className="h-5 w-5 text-yellow-400" />
        </button>
        <button onClick={onDelete} title="Excluir Selecionadas" className="p-2 rounded-full hover:bg-gray-700">
          <TrashIcon className="h-5 w-5 text-red-400" />
        </button>
      </div>
      <div className="h-6 w-px bg-gray-600" />
      <button
        onClick={onClearSelection}
        title="Limpar seleção"
        className="p-2 rounded-full hover:bg-gray-700"
      >
        <XMarkIcon className="h-6 w-6" />
      </button>
    </div>
  );
}
