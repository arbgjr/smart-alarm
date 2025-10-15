import React, { useState } from 'react';
import { Clock, Calendar, Repeat, Edit, Trash2, Power, PowerOff, MoreVertical, Copy } from 'lucide-react';

interface AlarmCardProps {
  alarm: {
    id: string;
    name: string;
    time: string;
    isActive: boolean;
    isRecurring: boolean;
    daysOfWeek?: string[];
    description?: string;
    createdAt: string;
    nextTrigger?: string;
  };
  onEdit: (alarm: any) => void;
  onDelete: (alarmId: string) => void;
  onToggle: (alarmId: string, isActive: boolean) => void;
  onDuplicate: (alarm: any) => void;
}

export const AlarmCard: React.FC<AlarmCardProps> = ({
  alarm,
  onEdit,
  onDelete,
  onToggle,
  onDuplicate
}) => {
  const [showMenu, setShowMenu] = useState(false);

  const formatTime = (time: string) => {
    try {
      const date = new Date(`2000-01-01T${time}`);
      return date.toLocaleTimeString('en-US', {
        hour: 'numeric',
        minute: '2-digit',
        hour12: true
      });
    } catch {
      return time;
    }
  };

  const formatDaysOfWeek = (days?: string[]) => {
    if (!days || days.length === 0) return 'One-time';
    if (days.length === 7) return 'Every day';

    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    return days.map(day => dayNames[parseInt(day)]).join(', ');
  };

  const formatNextTrigger = (nextTrigger?: string) => {
    if (!nextTrigger) return null;

    const date = new Date(nextTrigger);
    const now = new Date();
    const diffInHours = Math.floor((date.getTime() - now.getTime()) / (1000 * 60 * 60));

    if (diffInHours < 24) {
      return `in ${diffInHours}h`;
    } else {
      return date.toLocaleDateString();
    }
  };

  return (
    <div className={`bg-white rounded-lg border-2 transition-all duration-200 hover:shadow-md ${
      alarm.isActive ? 'border-green-200 bg-green-50/30' : 'border-gray-200'
    }`}>
      <div className="p-6">
        {/* Header */}
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <div className="flex items-center space-x-3">
              <h3 className="text-lg font-semibold text-gray-900 truncate">
                {alarm.name}
              </h3>
              <div className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                alarm.isActive
                  ? 'bg-green-100 text-green-800'
                  : 'bg-gray-100 text-gray-800'
              }`}>
                {alarm.isActive ? (
                  <>
                    <Power className="w-3 h-3 mr-1" />
                    Active
                  </>
                ) : (
                  <>
                    <PowerOff className="w-3 h-3 mr-1" />
                    Inactive
                  </>
                )}
              </div>
            </div>
            {alarm.description && (
              <p className="text-sm text-gray-600 mt-1 truncate">
                {alarm.description}
              </p>
            )}
          </div>

          {/* Actions Menu */}
          <div className="relative">
            <button
              onClick={() => setShowMenu(!showMenu)}
              className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
            >
              <MoreVertical className="w-5 h-5" />
            </button>

            {showMenu && (
              <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg border border-gray-200 z-10">
                <div className="py-1">
                  <button
                    onClick={() => {
                      onEdit(alarm);
                      setShowMenu(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                  >
                    <Edit className="w-4 h-4 mr-2" />
                    Edit
                  </button>
                  <button
                    onClick={() => {
                      onDuplicate(alarm);
                      setShowMenu(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                  >
                    <Copy className="w-4 h-4 mr-2" />
                    Duplicate
                  </button>
                  <button
                    onClick={() => {
                      onToggle(alarm.id, !alarm.isActive);
                      setShowMenu(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                  >
                    {alarm.isActive ? (
                      <>
                        <PowerOff className="w-4 h-4 mr-2" />
                        Disable
                      </>
                    ) : (
                      <>
                        <Power className="w-4 h-4 mr-2" />
                        Enable
                      </>
                    )}
                  </button>
                  <div className="border-t border-gray-100 my-1"></div>
                  <button
                    onClick={() => {
                      onDelete(alarm.id);
                      setShowMenu(false);
                    }}
                    className="flex items-center w-full px-4 py-2 text-sm text-red-600 hover:bg-red-50"
                  >
                    <Trash2 className="w-4 h-4 mr-2" />
                    Delete
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Time Display */}
        <div className="flex items-center space-x-4 mb-4">
          <div className="flex items-center space-x-2">
            <Clock className="w-5 h-5 text-blue-600" />
            <span className="text-2xl font-bold text-gray-900">
              {formatTime(alarm.time)}
            </span>
          </div>
        </div>

        {/* Schedule Info */}
        <div className="flex items-center justify-between text-sm text-gray-600 mb-4">
          <div className="flex items-center space-x-2">
            {alarm.isRecurring ? (
              <>
                <Repeat className="w-4 h-4" />
                <span>{formatDaysOfWeek(alarm.daysOfWeek)}</span>
              </>
            ) : (
              <>
                <Calendar className="w-4 h-4" />
                <span>One-time alarm</span>
              </>
            )}
          </div>

          {alarm.nextTrigger && alarm.isActive && (
            <div className="text-blue-600 font-medium">
              Next: {formatNextTrigger(alarm.nextTrigger)}
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="flex items-center justify-between pt-4 border-t border-gray-200">
          <div className="text-xs text-gray-500">
            Created {new Date(alarm.createdAt).toLocaleDateString()}
          </div>

          <div className="flex items-center space-x-2">
            <button
              onClick={() => onToggle(alarm.id, !alarm.isActive)}
              className={`inline-flex items-center px-3 py-1 rounded-md text-xs font-medium transition-colors ${
                alarm.isActive
                  ? 'bg-red-100 text-red-700 hover:bg-red-200'
                  : 'bg-green-100 text-green-700 hover:bg-green-200'
              }`}
            >
              {alarm.isActive ? (
                <>
                  <PowerOff className="w-3 h-3 mr-1" />
                  Disable
                </>
              ) : (
                <>
                  <Power className="w-3 h-3 mr-1" />
                  Enable
                </>
              )}
            </button>

            <button
              onClick={() => onEdit(alarm)}
              className="inline-flex items-center px-3 py-1 rounded-md text-xs font-medium bg-blue-100 text-blue-700 hover:bg-blue-200 transition-colors"
            >
              <Edit className="w-3 h-3 mr-1" />
              Edit
            </button>
          </div>
        </div>
      </div>

      {/* Click outside to close menu */}
      {showMenu && (
        <div
          className="fixed inset-0 z-0"
          onClick={() => setShowMenu(false)}
        />
      )}
    </div>
  );
};
