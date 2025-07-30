import React from 'react';
import { AlarmDto } from '../../services/alarmService';
import { useEnableAlarm, useDisableAlarm, useDeleteAlarm } from '../../hooks/useAlarms';
import { SkeletonList } from '../atoms/Skeleton';
import { EmptyAlarmState } from '../molecules/EmptyState';

interface AlarmListProps {
  alarms: AlarmDto[];
  isLoading?: boolean;
  showActions?: boolean;
  maxItems?: number;
}

export const AlarmList: React.FC<AlarmListProps> = ({
  alarms,
  isLoading = false,
  showActions = true,
  maxItems
}) => {
  const enableAlarmMutation = useEnableAlarm();
  const disableAlarmMutation = useDisableAlarm();
  const deleteAlarmMutation = useDeleteAlarm();

  const displayAlarms = maxItems ? alarms.slice(0, maxItems) : alarms;

  const handleToggleAlarm = (alarm: AlarmDto) => {
    if (alarm.isEnabled) {
      disableAlarmMutation.mutate(alarm.id);
    } else {
      enableAlarmMutation.mutate(alarm.id);
    }
  };

  const handleDeleteAlarm = (id: string) => {
    if (window.confirm('Are you sure you want to delete this alarm?')) {
      deleteAlarmMutation.mutate(id);
    }
  };

  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleTimeString('en-US', {
      hour: '2-digit',
      minute: '2-digit',
      hour12: true
    });
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (date.toDateString() === today.toDateString()) {
      return 'Today';
    } else if (date.toDateString() === tomorrow.toDateString()) {
      return 'Tomorrow';
    } else {
      return date.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric'
      });
    }
  };

  if (isLoading) {
    return <SkeletonList items={3} className="space-y-3" />;
  }

  if (displayAlarms.length === 0) {
    return <EmptyAlarmState />;
  }

  return (
    <div className="space-y-2">
      {displayAlarms.map((alarm) => (
        <div
          key={alarm.id}
          className={`flex items-center justify-between p-4 rounded-lg border transition-colors ${
            alarm.isEnabled
              ? 'bg-white border-gray-200 hover:bg-gray-50'
              : 'bg-gray-50 border-gray-200'
          }`}
        >
          <div className="flex items-center space-x-3">
            {/* Enable/Disable Toggle */}
            <button
              onClick={() => handleToggleAlarm(alarm)}
              disabled={enableAlarmMutation.isPending || disableAlarmMutation.isPending}
              className={`w-5 h-5 rounded border-2 flex items-center justify-center transition-colors ${
                alarm.isEnabled
                  ? 'bg-blue-600 border-blue-600 text-white'
                  : 'border-gray-300 hover:border-gray-400'
              } ${
                (enableAlarmMutation.isPending || disableAlarmMutation.isPending)
                  ? 'opacity-50 cursor-not-allowed'
                  : 'cursor-pointer'
              }`}
            >
              {alarm.isEnabled && (
                <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              )}
            </button>

            {/* Alarm Info */}
            <div>
              <div className="flex items-center space-x-2">
                <h4 className={`text-sm font-medium ${alarm.isEnabled ? 'text-gray-900' : 'text-gray-500'}`}>
                  {alarm.name}
                </h4>
                {alarm.isRecurring && (
                  <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">
                    Recurring
                  </span>
                )}
              </div>
              <div className="flex items-center space-x-4 mt-1">
                <span className={`text-sm ${alarm.isEnabled ? 'text-gray-600' : 'text-gray-400'}`}>
                  {formatTime(alarm.triggerTime)}
                </span>
                <span className={`text-xs ${alarm.isEnabled ? 'text-gray-500' : 'text-gray-400'}`}>
                  {formatDate(alarm.triggerTime)}
                </span>
              </div>
              {alarm.description && (
                <p className={`text-xs mt-1 ${alarm.isEnabled ? 'text-gray-500' : 'text-gray-400'}`}>
                  {alarm.description}
                </p>
              )}
            </div>
          </div>

          {/* Actions */}
          {showActions && (
            <div className="flex items-center space-x-2">
              <button
                className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
                title="Edit alarm"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
              </button>
              <button
                onClick={() => handleDeleteAlarm(alarm.id)}
                disabled={deleteAlarmMutation.isPending}
                className={`p-1 text-gray-400 hover:text-red-600 transition-colors ${
                  deleteAlarmMutation.isPending ? 'opacity-50 cursor-not-allowed' : ''
                }`}
                title="Delete alarm"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                </svg>
              </button>
            </div>
          )}
        </div>
      ))}

      {maxItems && alarms.length > maxItems && (
        <div className="text-center py-2">
          <p className="text-sm text-gray-500">
            Showing {maxItems} of {alarms.length} alarms
          </p>
        </div>
      )}
    </div>
  );
};

export default AlarmList;
