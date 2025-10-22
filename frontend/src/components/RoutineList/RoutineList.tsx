import React from 'react';
import { RoutineDto } from '../../services/routineService';
import { useDeleteRoutine } from '../../hooks/useRoutines';
import { SkeletonList } from '../atoms/Skeleton';
import { EmptyRoutineState } from '../molecules/EmptyState';

interface RoutineListProps {
  routines: RoutineDto[];
  isLoading?: boolean;
  showActions?: boolean;
  maxItems?: number;
}

export const RoutineList: React.FC<RoutineListProps> = ({
  routines,
  isLoading = false,
  showActions = true,
  maxItems
}) => {
  const deleteRoutineMutation = useDeleteRoutine();

  const displayRoutines = maxItems ? routines.slice(0, maxItems) : routines;

  const handleDeleteRoutine = (id: string) => {
    if (globalThis.confirm('Are you sure you want to delete this routine?')) {
      deleteRoutineMutation.mutate(id);
    }
  };

  if (isLoading) {
    return <SkeletonList items={3} />;
  }

  if (displayRoutines.length === 0) {
    return <EmptyRoutineState />;
  }

  return (
    <div className="space-y-2">
      {displayRoutines.map((routine) => (
        <div
          key={routine.id}
          className={`flex items-center justify-between p-4 rounded-lg border transition-colors ${
            routine.isActive
              ? 'bg-white border-gray-200 hover:bg-gray-50'
              : 'bg-gray-50 border-gray-200'
          }`}
        >
          <div className="flex items-center space-x-3">
            {/* Status Indicator */}
            <div
              className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                routine.isActive
                  ? 'bg-green-600 border-green-600 text-white'
                  : 'border-gray-300'
              }`}
            >
              {routine.isActive && (
                <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              )}
            </div>

            {/* Routine Info */}
            <div>
              <div className="flex items-center space-x-2">
                <h4 className={`text-sm font-medium ${routine.isActive ? 'text-gray-900' : 'text-gray-500'}`}>
                  {routine.name}
                </h4>
                <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                  Routine
                </span>
              </div>
              <div className="flex items-center space-x-4 mt-1">
                <span className={`text-sm ${routine.isActive ? 'text-gray-600' : 'text-gray-400'}`}>
                  Status: {routine.isActive ? 'Active' : 'Inactive'}
                </span>
                {routine.updatedAt && (
                  <span className={`text-xs ${routine.isActive ? 'text-gray-500' : 'text-gray-400'}`}>
                    Updated {new Date(routine.updatedAt).toLocaleDateString()}
                  </span>
                )}
              </div>
              {routine.description && (
                <p className={`text-xs mt-1 ${routine.isActive ? 'text-gray-500' : 'text-gray-400'}`}>
                  {routine.description}
                </p>
              )}
            </div>
          </div>

          {/* Actions */}
          {showActions && (
            <div className="flex items-center space-x-2">
              {/* Edit Button */}
              <button
                className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
                title="Edit routine"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>

              {/* Delete Button */}
              <button
                onClick={() => handleDeleteRoutine(routine.id)}
                disabled={deleteRoutineMutation.isPending}
                className={`p-1 text-gray-400 hover:text-red-600 transition-colors ${
                  deleteRoutineMutation.isPending ? 'opacity-50 cursor-not-allowed' : ''
                }`}
                title="Delete routine"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          )}
        </div>
      ))}

      {maxItems && routines.length > maxItems && (
        <div className="text-center py-2">
          <p className="text-sm text-gray-500">
            Showing {maxItems} of {routines.length} routines
          </p>
        </div>
      )}
    </div>
  );
};

export default RoutineList;
