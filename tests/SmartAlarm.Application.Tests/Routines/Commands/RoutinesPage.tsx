import { useState } from 'react';
import { RoutineList } from '@/components/RoutineList/RoutineList';
import { RoutineForm } from '@/components/RoutineForm/RoutineForm';
import { Modal } from '@/components/shared/Modal'; // Componente de modal genérico
import { useRoutines } from '@/hooks/useRoutines';
import { RoutineDto } from '@/services/routineService';
import { SearchBar } from '@/components/shared/SearchBar';
import { FilterPanel } from '@/components/shared/FilterPanel';
import { useDebounce } from '@/hooks/useDebounce';
import { Pagination } from '@/components/shared/Pagination';
import { BulkActionBar } from '@/components/shared/BulkActionBar';

const filterOptions = [
  { value: 'all', label: 'Todas' },
  { value: 'active', label: 'Ativas' },
  { value: 'inactive', label: 'Inativas' },
];

export function RoutinesPage() {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedRoutineIds, setSelectedRoutineIds] = useState<string[]>([]);
  const [statusFilter, setStatusFilter] = useState('all');
  const debouncedSearchTerm = useDebounce(searchTerm, 300); // Debounce de 300ms

  // Passa os termos de busca e filtro para o hook
  // O hook agora retorna dados paginados
  // Supondo que o hook useRoutines tenha as mutações em lote
  const { data: paginatedRoutines, deleteRoutine, enableRoutine, disableRoutine,
          deleteMultipleRoutines, enableMultipleRoutines, disableMultipleRoutines,
          isLoading, error } = useRoutines({
    search: debouncedSearchTerm,
    status: statusFilter,
    page: currentPage,
    pageSize: 10,
  });
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [routineToEdit, setRoutineToEdit] = useState<RoutineDto | null>(null);

  const handleOpenCreateForm = () => {
    setRoutineToEdit(null); // Garante que não está em modo de edição
    setIsFormOpen(true);
  };

  const handleOpenEditForm = (routine: RoutineDto) => {
    setRoutineToEdit(routine); // Define a rotina a ser editada
    setIsFormOpen(true);
  };

  const handleCloseForm = () => {
    setIsFormOpen(false);
    setRoutineToEdit(null); // Limpa o estado de edição
  };

  const handleDelete = (id: string) => {
    if (window.confirm('Tem certeza que deseja excluir esta rotina?')) {
      deleteRoutine.mutate(id);
    }
  };

  const handleToggleEnable = (id: string, isEnabled: boolean) => {
    if (isEnabled) {
      disableRoutine.mutate(id);
    } else {
      enableRoutine.mutate(id);
    }
  };

  const handleSelectRoutine = (id: string) => {
    setSelectedRoutineIds((prev) =>
      prev.includes(id) ? prev.filter((routineId) => routineId !== id) : [...prev, id]
    );
  };

  const handleClearSelection = () => {
    setSelectedRoutineIds([]);
  };

  const handleDeleteSelected = () => {
    if (window.confirm(`Tem certeza que deseja excluir ${selectedRoutineIds.length} rotinas?`)) {
      // Supondo que a mutação aceite um array de IDs
      deleteMultipleRoutines.mutate(selectedRoutineIds, {
        onSuccess: () => handleClearSelection(),
      });
    }
  };

  const handleEnableSelected = () => {
    // Supondo que a mutação aceite um array de IDs
    enableMultipleRoutines.mutate(selectedRoutineIds, {
      onSuccess: () => handleClearSelection(),
    });
  };

  const handleDisableSelected = () => {
    // Supondo que a mutação aceite um array de IDs
    disableMultipleRoutines.mutate(selectedRoutineIds, {
      onSuccess: () => handleClearSelection(),
    });
  };

  return (
    <div className="container mx-auto p-4">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-3xl font-bold">Minhas Rotinas</h1>
        <button onClick={handleOpenCreateForm} className="rounded-md bg-indigo-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-indigo-700">
          Nova Rotina
        </button>
      </div>

      <div className="flex flex-col md:flex-row gap-4 mb-4">
        <div className="flex-grow">
          <SearchBar value={searchTerm} onChange={setSearchTerm} placeholder="Buscar por nome da rotina..." />
        </div>
        <div className="flex-shrink-0">
          <FilterPanel options={filterOptions} value={statusFilter} onChange={setStatusFilter} />
        </div>
      </div>

      {isLoading && <p>Carregando rotinas...</p>}
      {error && <p className="text-red-500">Erro ao carregar rotinas: {error.message}</p>}
      {!isLoading && !error && paginatedRoutines && (
        <div className="mb-16"> {/* Adiciona margem inferior para não sobrepor a BulkActionBar */}
          <RoutineList
            routines={paginatedRoutines.data || []}
            selectedIds={selectedRoutineIds}
            onEdit={handleOpenEditForm}
            onDelete={handleDelete}
            onToggleEnable={handleToggleEnable}
            onSelect={handleSelectRoutine}
          />
          <Pagination
            currentPage={paginatedRoutines.currentPage}
            totalPages={paginatedRoutines.totalPages}
            totalItems={paginatedRoutines.totalCount}
            itemsPerPage={10}
            onPageChange={setCurrentPage}
          />
        </>
      )}

      <Modal isOpen={isFormOpen} onClose={handleCloseForm}>
        <RoutineForm routineToEdit={routineToEdit} onClose={handleCloseForm} />
      </Modal>

      <BulkActionBar
        selectedCount={selectedRoutineIds.length}
        onClearSelection={handleClearSelection}
        onDelete={handleDeleteSelected}
        onEnable={handleEnableSelected}
        onDisable={handleDisableSelected}
      />
    </div>
  );
}
