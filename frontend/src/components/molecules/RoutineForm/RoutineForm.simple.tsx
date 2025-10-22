import React, { useState, useEffect } from 'react';
import { RoutineDto, CreateRoutinePayload, UpdateRoutinePayload } from '../../../services/routineService';
import { useCreateRoutine, useUpdateRoutine } from '../../../hooks/useRoutines';
import { LoadingSpinner } from '../Loading';

interface RoutineFormProps {
  routine?: RoutineDto;
  onSuccess?: () => void;
  onCancel?: () => void;
  isOpen?: boolean;
}

export const RoutineForm: React.FC<RoutineFormProps> = ({
  routine,
  onSuccess,
  onCancel,
  isOpen = true
}) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    alarmIds: [] as string[],
  });

  const createRoutineMutation = useCreateRoutine();
  const updateRoutineMutation = useUpdateRoutine();

  const isEditing = !!routine;
  const isLoading = createRoutineMutation.isPending || updateRoutineMutation.isPending;

  useEffect(() => {
    if (routine) {
      setFormData({
        name: routine.name,
        description: routine.description || '',
        alarmIds: routine.alarmIds || [],
      });
    }
  }, [routine]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      return;
    }

    try {
      if (isEditing && routine) {
        const updatePayload: UpdateRoutinePayload = {
          id: routine.id,
          name: formData.name,
          description: formData.description || undefined,
          alarmIds: formData.alarmIds,
          isActive: true, // Backend requer este campo
        };
        await updateRoutineMutation.mutateAsync(updatePayload);
      } else {
        const createPayload: CreateRoutinePayload = {
          name: formData.name,
          description: formData.description || undefined,
          alarmIds: formData.alarmIds,
        };
        await createRoutineMutation.mutateAsync(createPayload);
      }

      // Reset form if creating new routine
      if (!isEditing) {
        setFormData({
          name: '',
          description: '',
          alarmIds: [],
        });
      }

      onSuccess?.();
    } catch (error) {
      console.error('Error saving routine:', error);
    }
  };

  if (!isOpen) return null;

  const submitButtonText = isEditing ? 'Update Routine' : 'Create Routine';

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-xl font-semibold text-gray-900">
              {isEditing ? 'Edit Routine' : 'Create New Routine'}
            </h2>
            <button
              onClick={onCancel}
              className="text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Close"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Routine Basic Info */}
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                Routine Name *
              </label>
              <input
                type="text"
                id="name"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Enter routine name"
              />
            </div>

            <div>
              <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                Description
              </label>
              <textarea
                id="description"
                name="description"
                value={formData.description}
                onChange={handleInputChange}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Enter description (optional)"
              />
            </div>

            <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3">
              <p className="text-sm text-yellow-800">
                <strong>Note:</strong> Routine steps and alarm associations will be available in a future update.
                For now, you can create basic routines with name and description.
              </p>
            </div>

            {/* Form Actions */}
            <div className="flex gap-3 pt-4 border-t">
              <button
                type="button"
                onClick={onCancel}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isLoading || !formData.name.trim()}
                className="flex-1 px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors flex items-center justify-center"
              >
                {isLoading ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  submitButtonText
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
