import React, { useState, useEffect } from 'react';
import { AlarmDto, CreateAlarmRequest, UpdateAlarmRequest } from '../../../services/alarmService';
import { useCreateAlarm, useUpdateAlarm } from '../../../hooks/useAlarms';
import { LoadingSpinner } from '../Loading';

interface AlarmFormProps {
  alarm?: AlarmDto;
  onSuccess?: () => void;
  onCancel?: () => void;
  isOpen?: boolean;
}

export const AlarmForm: React.FC<AlarmFormProps> = ({
  alarm,
  onSuccess,
  onCancel,
  isOpen = true
}) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    triggerTime: '',
    isEnabled: true,
    isRecurring: false,
    recurrencePattern: ''
  });

  const createAlarmMutation = useCreateAlarm();
  const updateAlarmMutation = useUpdateAlarm();

  const isEditing = !!alarm;
  const isLoading = createAlarmMutation.isLoading || updateAlarmMutation.isLoading;

  useEffect(() => {
    if (alarm) {
      setFormData({
        name: alarm.name,
        description: alarm.description || '',
        triggerTime: alarm.triggerTime ? new Date(alarm.triggerTime).toISOString().slice(0, 16) : '',
        isEnabled: alarm.isEnabled,
        isRecurring: alarm.isRecurring,
        recurrencePattern: alarm.recurrencePattern || ''
      });
    }
  }, [alarm]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: type === 'checkbox' ? (e.target as HTMLInputElement).checked : value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.name.trim() || !formData.triggerTime) {
      return;
    }

    try {
      if (isEditing && alarm) {
        const updateRequest: UpdateAlarmRequest = {
          name: formData.name,
          description: formData.description,
          triggerTime: formData.triggerTime,
          isEnabled: formData.isEnabled,
          isRecurring: formData.isRecurring,
          recurrencePattern: formData.recurrencePattern || undefined
        };
        await updateAlarmMutation.mutateAsync({ id: alarm.id, alarm: updateRequest });
      } else {
        const createRequest: CreateAlarmRequest = {
          name: formData.name,
          description: formData.description,
          triggerTime: formData.triggerTime,
          isRecurring: formData.isRecurring,
          recurrencePattern: formData.recurrencePattern || undefined
        };
        await createAlarmMutation.mutateAsync(createRequest);
      }

      // Reset form if creating new alarm
      if (!isEditing) {
        setFormData({
          name: '',
          description: '',
          triggerTime: '',
          isEnabled: true,
          isRecurring: false,
          recurrencePattern: ''
        });
      }

      onSuccess?.();
    } catch (error) {
      console.error('Error saving alarm:', error);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md max-h-[90vh] overflow-y-auto">
        <div className="p-6">
          <div className="flex justify-between items-center mb-6">
            <h2 className="text-xl font-semibold text-gray-900">
              {isEditing ? 'Edit Alarm' : 'Create New Alarm'}
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
            {/* Alarm Name */}
            <div>
              <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                Alarm Name *
              </label>
              <input
                type="text"
                id="name"
                name="name"
                value={formData.name}
                onChange={handleInputChange}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="Enter alarm name"
              />
            </div>

            {/* Description */}
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

            {/* Trigger Time */}
            <div>
              <label htmlFor="triggerTime" className="block text-sm font-medium text-gray-700 mb-1">
                Trigger Time *
              </label>
              <input
                type="datetime-local"
                id="triggerTime"
                name="triggerTime"
                value={formData.triggerTime}
                onChange={handleInputChange}
                required
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            {/* Recurring */}
            <div className="flex items-center">
              <input
                type="checkbox"
                id="isRecurring"
                name="isRecurring"
                checked={formData.isRecurring}
                onChange={handleInputChange}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
              <label htmlFor="isRecurring" className="ml-2 block text-sm text-gray-700">
                Recurring alarm
              </label>
            </div>

            {/* Recurrence Pattern - only shown if recurring is enabled */}
            {formData.isRecurring && (
              <div>
                <label htmlFor="recurrencePattern" className="block text-sm font-medium text-gray-700 mb-1">
                  Recurrence Pattern
                </label>
                <select
                  id="recurrencePattern"
                  name="recurrencePattern"
                  value={formData.recurrencePattern}
                  onChange={handleInputChange}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                >
                  <option value="">Select pattern</option>
                  <option value="daily">Daily</option>
                  <option value="weekly">Weekly</option>
                  <option value="monthly">Monthly</option>
                  <option value="weekdays">Weekdays (Mon-Fri)</option>
                  <option value="weekends">Weekends (Sat-Sun)</option>
                </select>
              </div>
            )}

            {/* Enable/Disable */}
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
                Enable alarm immediately
              </label>
            </div>

            {/* Form Actions */}
            <div className="flex gap-3 pt-4">
              <button
                type="button"
                onClick={onCancel}
                className="flex-1 px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-gray-500 transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isLoading || !formData.name.trim() || !formData.triggerTime}
                className="flex-1 px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors flex items-center justify-center"
              >
                {isLoading ? (
                  <LoadingSpinner size="sm" />
                ) : (
                  isEditing ? 'Update Alarm' : 'Create Alarm'
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};
