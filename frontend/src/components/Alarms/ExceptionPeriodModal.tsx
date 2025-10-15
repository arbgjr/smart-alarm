import React, { useState, useEffect } from 'react';
import { X, Calendar, Clock, AlertTriangle } from 'lucide-react';

interface ExceptionPeriod {
  id?: string;
  name: string;
  description?: string;
  startDate: string;
  endDate: string;
  startTime?: string;
  endTime?: string;
  type: 'disable_all' | 'disable_specific' | 'modify_time';
  affectedAlarmIds?: string[];
  modifiedTime?: string;
  isActive: boolean;
}

interface ExceptionPeriodModalProps {
  isOpen: boolean;
  exception?: ExceptionPeriod;
  alarms: Array<{ id: string; name: string; time: string }>;
  onSave: (exception: ExceptionPeriod) => void;
  onCancel: () => void;
}

export const ExceptionPeriodModal: React.FC<ExceptionPeriodModalProps> = ({
  isOpen,
  exception,
  alarms,
  onSave,
  onCancel
}) => {
  const [formData, setFormData] = useState<ExceptionPeriod>({
    name: '',
    description: '',
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    startTime: '',
    endTime: '',
    type: 'disable_all',
    affectedAlarmIds: [],
    modifiedTime: '',
    isActive: true
  });

  useEffect(() => {
    if (exception) {
      setFormData(exception);
    } else {
      setFormData({
        name: '',
        description: '',
        startDate: new Date().toISOString().split('T')[0],
        endDate: new Date().toISOString().split('T')[0],
        startTime: '',
        endTime: '',
        type: 'disable_all',
        affectedAlarmIds: [],
        modifiedTime: '',
        isActive: true
      });
    }
  }, [exception, isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  const toggleAlarmSelection = (alarmId: string) => {
    setFormData(prev => ({
      ...prev,
      affectedAlarmIds: prev.affectedAlarmIds?.includes(alarmId)
        ? prev.affectedAlarmIds.filter(id => id !== alarmId)
        : [...(prev.affectedAlarmIds || []), alarmId]
    }));
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-6 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">
            {exception ? 'Edit Exception Period' : 'Create Exception Period'}
          </h2>
          <button
            onClick={onCancel}
            className="text-gray-400 hover:text-gray-600 transition-colors"
          >
            <X className="w-6 h-6" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-6">
          {/* Basic Info */}
          <div className="grid grid-cols-1 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Exception Name *
              </label>
              <input
                type="text"
                required
                value={formData.name}
                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                placeholder="e.g., Vacation, Holiday, Sick Leave"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Description
              </label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                rows={3}
                placeholder="Optional description of this exception period"
              />
            </div>
          </div>

          {/* Date Range */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Start Date *
              </label>
              <input
                type="date"
                required
                value={formData.startDate}
                onChange={(e) => setFormData(prev => ({ ...prev, startDate: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                End Date *
              </label>
              <input
                type="date"
                required
                value={formData.endDate}
                onChange={(e) => setFormData(prev => ({ ...prev, endDate: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>

          {/* Time Range (Optional) */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Start Time (Optional)
              </label>
              <input
                type="time"
                value={formData.startTime}
                onChange={(e) => setFormData(prev => ({ ...prev, startTime: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                End Time (Optional)
              </label>
              <input
                type="time"
                value={formData.endTime}
                onChange={(e) => setFormData(prev => ({ ...prev, endTime: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          </div>

          {/* Exception Type */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Exception Type
            </label>
            <div className="space-y-3">
              {[
                {
                  value: 'disable_all',
                  label: 'Disable All Alarms',
                  description: 'Turn off all alarms during this period',
                  icon: AlertTriangle,
                  color: 'text-red-600'
                },
                {
                  value: 'disable_specific',
                  label: 'Disable Specific Alarms',
                  description: 'Turn off only selected alarms',
                  icon: Clock,
                  color: 'text-yellow-600'
                },
                {
                  value: 'modify_time',
                  label: 'Modify Alarm Time',
                  description: 'Change alarm times during this period',
                  icon: Calendar,
                  color: 'text-blue-600'
                }
              ].map(({ value, label, description, icon: Icon, color }) => (
                <label key={value} className="flex items-start space-x-3 cursor-pointer">
                  <input
                    type="radio"
                    name="type"
                    value={value}
                    checked={formData.type === value}
                    onChange={(e) => setFormData(prev => ({ ...prev, type: e.target.value as any }))}
                    className="mt-1 h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300"
                  />
                  <div className="flex-1">
                    <div className="flex items-center space-x-2">
                      <Icon className={`w-4 h-4 ${color}`} />
                      <span className="text-sm font-medium text-gray-900">{label}</span>
                    </div>
                    <p className="text-sm text-gray-600 mt-1">{description}</p>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {/* Specific Alarms Selection */}
          {(formData.type === 'disable_specific' || formData.type === 'modify_time') && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Select Alarms
              </label>
              <div className="max-h-40 overflow-y-auto border border-gray-300 rounded-md p-3 space-y-2">
                {alarms.map((alarm) => (
                  <label key={alarm.id} className="flex items-center space-x-3 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={formData.affectedAlarmIds?.includes(alarm.id) || false}
                      onChange={() => toggleAlarmSelection(alarm.id)}
                      className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                    />
                    <div className="flex-1">
                      <span className="text-sm font-medium text-gray-900">{alarm.name}</span>
                      <span className="text-sm text-gray-600 ml-2">({alarm.time})</span>
                    </div>
                  </label>
                ))}

                {alarms.length === 0 && (
                  <p className="text-sm text-gray-500 text-center py-4">
                    No alarms available to select
                  </p>
                )}
              </div>
            </div>
          )}

          {/* Modified Time */}
          {formData.type === 'modify_time' && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                New Alarm Time
              </label>
              <input
                type="time"
                value={formData.modifiedTime}
                onChange={(e) => setFormData(prev => ({ ...prev, modifiedTime: e.target.value }))}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
          )}

          {/* Active Toggle */}
          <div className="flex items-center">
            <input
              type="checkbox"
              id="isActive"
              checked={formData.isActive}
              onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
              Activate this exception period immediately
            </label>
          </div>

          {/* Actions */}
          <div className="flex items-center justify-end space-x-3 pt-6 border-t border-gray-200">
            <button
              type="button"
              onClick={onCancel}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-md hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              {exception ? 'Update Exception' : 'Create Exception'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
