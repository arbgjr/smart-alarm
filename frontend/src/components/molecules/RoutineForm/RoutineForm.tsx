import React, { useState, useEffect } from 'react';
import { RoutineDto, CreateRoutineRequest, UpdateRoutineRequest, CreateRoutineStepRequest } from '../../../services/routineService';
import { useCreateRoutine, useUpdateRoutine } from '../../../hooks/useRoutines';
import { LoadingSpinner } from '../Loading';

interface RoutineFormProps {
  routine?: RoutineDto;
  onSuccess?: () => void;
  onCancel?: () => void;
  isOpen?: boolean;
}

interface RoutineStepFormData {
  id?: string;
  name: string;
  description: string;
  stepType: string;
  configuration: Record<string, any>;
  order: number;
  isEnabled: boolean;
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
    isEnabled: true,
  });

  const [steps, setSteps] = useState<RoutineStepFormData[]>([]);

  const createRoutineMutation = useCreateRoutine();
  const updateRoutineMutation = useUpdateRoutine();

  const isEditing = !!routine;
  const isLoading = createRoutineMutation.isLoading || updateRoutineMutation.isLoading;

  // Available step types
  const stepTypes = [
    { value: 'notification', label: 'Send Notification' },
    { value: 'email', label: 'Send Email' },
    { value: 'webhook', label: 'Call Webhook' },
    { value: 'delay', label: 'Add Delay' },
    { value: 'condition', label: 'Check Condition' }
  ];

  useEffect(() => {
    if (routine) {
      setFormData({
        name: routine.name,
        description: routine.description || '',
        isEnabled: routine.isEnabled,
      });

      setSteps(
        routine.steps.map(step => ({
          id: step.id,
          name: step.name,
          description: step.description || '',
          stepType: step.stepType,
          configuration: step.configuration,
          order: step.order,
          isEnabled: step.isEnabled
        }))
      );
    }
  }, [routine]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? (e.target as HTMLInputElement).checked : value
    }));
  };

  const handleAddStep = () => {
    const newStep: RoutineStepFormData = {
      name: '',
      description: '',
      stepType: 'notification',
      configuration: {},
      order: steps.length + 1,
      isEnabled: true
    };
    setSteps(prev => [...prev, newStep]);
  };

  const handleRemoveStep = (index: number) => {
    setSteps(prev => prev.filter((_, i) => i !== index));
  };

  const handleStepChange = (index: number, field: keyof RoutineStepFormData, value: any) => {
    setSteps(prev =>
      prev.map((step, i) =>
        i === index ? { ...step, [field]: value } : step
      )
    );
  };

  const handleStepConfigurationChange = (index: number, key: string, value: any) => {
    setSteps(prev =>
      prev.map((step, i) =>
        i === index
          ? { ...step, configuration: { ...step.configuration, [key]: value } }
          : step
      )
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim()) {
      return;
    }

    try {
      if (isEditing && routine) {
        const updateRequest: UpdateRoutineRequest = {
          name: formData.name,
          description: formData.description,
          isEnabled: formData.isEnabled,
        };
        await updateRoutineMutation.mutateAsync({ id: routine.id, data: updateRequest });
      } else {
        const createRequest: CreateRoutineRequest = {
          name: formData.name,
          description: formData.description,
          isEnabled: formData.isEnabled,
          steps: steps.map((step, index): CreateRoutineStepRequest => ({
            name: step.name,
            description: step.description || undefined,
            stepType: step.stepType,
            configuration: step.configuration,
            order: index + 1,
            isEnabled: step.isEnabled
          }))
        };
        await createRoutineMutation.mutateAsync(createRequest);
      }

      // Reset form if creating new routine
      if (!isEditing) {
        setFormData({
          name: '',
          description: '',
          isEnabled: true,
        });
        setSteps([]);
      }

      onSuccess?.();
    } catch (error) {
      console.error('Error saving routine:', error);
    }
  };

  const renderStepConfiguration = (step: RoutineStepFormData, index: number) => {
    switch (step.stepType) {
      case 'notification':
        return (
          <div className="grid grid-cols-1 gap-2">
            <input
              type="text"
              placeholder="Notification message"
              value={step.configuration.message || ''}
              onChange={(e) => handleStepConfigurationChange(index, 'message', e.target.value)}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            />
          </div>
        );
      case 'email':
        return (
          <div className="grid grid-cols-1 gap-2">
            <input
              type="email"
              placeholder="Recipient email"
              value={step.configuration.to || ''}
              onChange={(e) => handleStepConfigurationChange(index, 'to', e.target.value)}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            />
            <input
              type="text"
              placeholder="Email subject"
              value={step.configuration.subject || ''}
              onChange={(e) => handleStepConfigurationChange(index, 'subject', e.target.value)}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            />
          </div>
        );
      case 'webhook':
        return (
          <div className="grid grid-cols-1 gap-2">
            <input
              type="url"
              placeholder="Webhook URL"
              value={step.configuration.url || ''}
              onChange={(e) => handleStepConfigurationChange(index, 'url', e.target.value)}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            />
          </div>
        );
      case 'delay':
        return (
          <div className="grid grid-cols-2 gap-2">
            <input
              type="number"
              placeholder="Duration"
              value={step.configuration.duration || ''}
              onChange={(e) => handleStepConfigurationChange(index, 'duration', parseInt(e.target.value))}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            />
            <select
              value={step.configuration.unit || 'seconds'}
              onChange={(e) => handleStepConfigurationChange(index, 'unit', e.target.value)}
              className="px-2 py-1 text-sm border border-gray-300 rounded"
            >
              <option value="seconds">Seconds</option>
              <option value="minutes">Minutes</option>
              <option value="hours">Hours</option>
            </select>
          </div>
        );
      default:
        return null;
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
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

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Routine Basic Info */}
            <div className="space-y-4">
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

              <div className="flex items-center">
                <input
                  type="checkbox"
                  id="isEnabled"
                  name="isEnabled"
                  checked={formData.isEnabled}
                  onChange={handleInputChange}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
                <label htmlFor="isEnabled" className="ml-2 block text-sm text-gray-700">
                  Enable routine immediately
                </label>
              </div>
            </div>

            {/* Routine Steps */}
            <div>
              <div className="flex justify-between items-center mb-4">
                <h3 className="text-lg font-medium text-gray-900">Routine Steps</h3>
                <button
                  type="button"
                  onClick={handleAddStep}
                  className="px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                >
                  Add Step
                </button>
              </div>

              <div className="space-y-4">
                {steps.map((step, index) => (
                  <div key={index} className="border border-gray-200 rounded-md p-4">
                    <div className="flex justify-between items-center mb-3">
                      <span className="text-sm font-medium text-gray-600">Step {index + 1}</span>
                      <button
                        type="button"
                        onClick={() => handleRemoveStep(index)}
                        className="text-red-600 hover:text-red-800 text-sm"
                      >
                        Remove
                      </button>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                      <input
                        type="text"
                        placeholder="Step name"
                        value={step.name}
                        onChange={(e) => handleStepChange(index, 'name', e.target.value)}
                        className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />

                      <select
                        value={step.stepType}
                        onChange={(e) => handleStepChange(index, 'stepType', e.target.value)}
                        className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      >
                        {stepTypes.map(type => (
                          <option key={type.value} value={type.value}>
                            {type.label}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="mt-3">
                      <textarea
                        placeholder="Step description (optional)"
                        value={step.description}
                        onChange={(e) => handleStepChange(index, 'description', e.target.value)}
                        rows={2}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                      />
                    </div>

                    <div className="mt-3">
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Configuration
                      </label>
                      {renderStepConfiguration(step, index)}
                    </div>

                    <div className="mt-3 flex items-center">
                      <input
                        type="checkbox"
                        checked={step.isEnabled}
                        onChange={(e) => handleStepChange(index, 'isEnabled', e.target.checked)}
                        className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                      />
                      <label className="ml-2 block text-sm text-gray-700">
                        Enable this step
                      </label>
                    </div>
                  </div>
                ))}

                {steps.length === 0 && (
                  <div className="text-center py-8 text-gray-500">
                    No steps added yet. Click "Add Step" to create your first routine step.
                  </div>
                )}
              </div>
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
                  isEditing ? 'Update Routine' : 'Create Routine'
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
