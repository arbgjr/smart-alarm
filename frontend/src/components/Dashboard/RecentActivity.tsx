import React from 'react';
import { Clock, CheckCircle, Pause, Plus, Edit, Trash2 } from 'lucide-react';

interface ActivityItem {
  id: string;
  type: 'alarm_triggered' | 'alarm_dismissed' | 'alarm_snoozed' | 'alarm_created' | 'alarm_updated' | 'alarm_deleted';
  title: string;
  description: string;
  timestamp: string;
  alarmId?: string;
}

interface RecentActivityProps {
  activities: ActivityItem[];
  isLoading?: boolean;
  maxItems?: number;
  className?: string;
}

export const RecentActivity: React.FC<RecentActivityProps> = ({
  activities,
  isLoading = false,
  maxItems = 10,
  className = ''
}) => {
  const getActivityIcon = (type: ActivityItem['type']) => {
    switch (type) {
      case 'alarm_triggered':
        return <Clock className="w-4 h-4 text-blue-600" />;
      case 'alarm_dismissed':
        return <CheckCircle className="w-4 h-4 text-green-600" />;
      case 'alarm_snoozed':
        return <Pause className="w-4 h-4 text-yellow-600" />;
      case 'alarm_created':
        return <Plus className="w-4 h-4 text-purple-600" />;
      case 'alarm_updated':
        return <Edit className="w-4 h-4 text-orange-600" />;
      case 'alarm_deleted':
        return <Trash2 className="w-4 h-4 text-red-600" />;
      default:
        return <Clock className="w-4 h-4 text-gray-600" />;
    }
  };

  const getActivityColor = (type: ActivityItem['type']) => {
    switch (type) {
      case 'alarm_triggered':
        return 'bg-blue-50 border-blue-200';
      case 'alarm_dismissed':
        return 'bg-green-50 border-green-200';
      case 'alarm_snoozed':
        return 'bg-yellow-50 border-yellow-200';
      case 'alarm_created':
        return 'bg-purple-50 border-purple-200';
      case 'alarm_updated':
        return 'bg-orange-50 border-orange-200';
      case 'alarm_deleted':
        return 'bg-red-50 border-red-200';
      default:
        return 'bg-gray-50 border-gray-200';
    }
  };

  const formatTimestamp = (timestamp: string) => {
    const date = new Date(timestamp);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));

    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}h ago`;
    return date.toLocaleDateString();
  };

  if (isLoading) {
    return (
      <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 rounded w-1/3 mb-4"></div>
          <div className="space-y-3">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="flex items-center space-x-3">
                <div className="w-8 h-8 bg-gray-200 rounded-full"></div>
                <div className="flex-1">
                  <div className="h-3 bg-gray-200 rounded w-3/4 mb-1"></div>
                  <div className="h-2 bg-gray-200 rounded w-1/2"></div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  const displayActivities = activities.slice(0, maxItems);

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-gray-900">Recent Activity</h3>
        {activities.length > maxItems && (
          <button className="text-sm text-blue-600 hover:text-blue-500 font-medium">
            View all ({activities.length})
          </button>
        )}
      </div>

      {displayActivities.length === 0 ? (
        <div className="flex items-center justify-center h-32 text-gray-500">
          <div className="text-center">
            <Clock className="w-8 h-8 mx-auto mb-2 text-gray-300" />
            <p>No recent activity</p>
          </div>
        </div>
      ) : (
        <div className="space-y-3">
          {displayActivities.map((activity) => (
            <div
              key={activity.id}
              className={`flex items-start space-x-3 p-3 rounded-lg border ${getActivityColor(activity.type)} transition-colors hover:shadow-sm`}
            >
              <div className="flex-shrink-0 mt-0.5">
                {getActivityIcon(activity.type)}
              </div>
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900 truncate">
                  {activity.title}
                </p>
                <p className="text-sm text-gray-600 mt-1">
                  {activity.description}
                </p>
                <p className="text-xs text-gray-500 mt-1">
                  {formatTimestamp(activity.timestamp)}
                </p>
              </div>
            </div>
          ))}
        </div>
      )}

      {displayActivities.length > 0 && (
        <div className="mt-4 pt-4 border-t border-gray-200">
          <div className="flex items-center justify-between text-sm text-gray-600">
            <span>Showing {displayActivities.length} of {activities.length} activities</span>
            <button className="text-blue-600 hover:text-blue-500 font-medium">
              Refresh
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
