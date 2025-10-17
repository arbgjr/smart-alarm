import React, { useState, useEffect } from 'react';
import { Bell, X, CheckCircle, AlertTriangle, Info, Clock, ExternalLink } from 'lucide-react';
import { useSignalRConnection } from '../../utils/signalRConnection';

interface NotificationEvent {
  userId: string;
  type: 'alarm_reminder' | 'optimization_suggestion' | 'sync_complete' | 'system_update';
  title: string;
  message: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  timestamp: string;
  actionable?: boolean;
  actionUrl?: string;
}

interface RealTimeNotificationsProps {
  className?: string;
  maxNotifications?: number;
}

export const RealTimeNotifications: React.FC<RealTimeNotificationsProps> = ({
  className = '',
  maxNotifications = 5
}) => {
  const [notifications, setNotifications] = useState<NotificationEvent[]>([]);
  const [isExpanded, setIsExpanded] = useState(false);
  const { addEventListener, removeEventListener } = useSignalRConnection();

  useEffect(() => {
    // Listen for real-time notifications
    const handleNotification = (notification: NotificationEvent) => {
      setNotifications(prev => {
        const newNotifications = [notification, ...prev];
        return newNotifications.slice(0, maxNotifications * 2); // Keep more in memory
      });

      // Auto-expand for urgent notifications
      if (notification.priority === 'urgent') {
        setIsExpanded(true);
      }
    };

    addEventListener('notification', handleNotification);
    addEventListener('systemNotification', handleNotification);

    // Cleanup
    return () => {
      removeEventListener('notification', handleNotification);
      removeEventListener('systemNotification', handleNotification);
    };
  }, [addEventListener, removeEventListener, maxNotifications]);

  const dismissNotification = (timestamp: string) => {
    setNotifications(prev => prev.filter(n => n.timestamp !== timestamp));
  };

  const dismissAll = () => {
    setNotifications([]);
    setIsExpanded(false);
  };

  const getNotificationIcon = (type: NotificationEvent['type'], priority: NotificationEvent['priority']) => {
    if (priority === 'urgent') {
      return <AlertTriangle className="w-5 h-5 text-red-600" />;
    }

    switch (type) {
      case 'alarm_reminder':
        return <Clock className="w-5 h-5 text-blue-600" />;
      case 'optimization_suggestion':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'sync_complete':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'system_update':
        return <Info className="w-5 h-5 text-purple-600" />;
      default:
        return <Bell className="w-5 h-5 text-gray-600" />;
    }
  };

  const getNotificationColor = (priority: NotificationEvent['priority']) => {
    switch (priority) {
      case 'urgent':
        return 'border-red-200 bg-red-50';
      case 'high':
        return 'border-orange-200 bg-orange-50';
      case 'medium':
        return 'border-blue-200 bg-blue-50';
      case 'low':
        return 'border-gray-200 bg-gray-50';
      default:
        return 'border-gray-200 bg-white';
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

  const visibleNotifications = isExpanded
    ? notifications
    : notifications.slice(0, maxNotifications);

  const unreadCount = notifications.length;

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-gray-200">
        <div className="flex items-center space-x-2">
          <Bell className="w-5 h-5 text-gray-600" />
          <h3 className="text-lg font-medium text-gray-900">Live Notifications</h3>
          {unreadCount > 0 && (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
              {unreadCount}
            </span>
          )}
        </div>

        <div className="flex items-center space-x-2">
          {notifications.length > maxNotifications && (
            <button
              onClick={() => setIsExpanded(!isExpanded)}
              className="text-sm text-blue-600 hover:text-blue-500 font-medium"
            >
              {isExpanded ? 'Show Less' : `Show All (${notifications.length})`}
            </button>
          )}

          {notifications.length > 0 && (
            <button
              onClick={dismissAll}
              className="text-sm text-gray-600 hover:text-gray-500 font-medium"
            >
              Clear All
            </button>
          )}
        </div>
      </div>

      {/* Notifications List */}
      <div className="max-h-96 overflow-y-auto">
        {visibleNotifications.length === 0 ? (
          <div className="flex items-center justify-center h-32 text-gray-500">
            <div className="text-center">
              <Bell className="w-8 h-8 mx-auto mb-2 text-gray-300" />
              <p>No new notifications</p>
              <p className="text-sm mt-1">You're all caught up!</p>
            </div>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {visibleNotifications.map((notification, index) => (
              <div
                key={`${notification.timestamp}-${index}`}
                className={`p-4 border-l-4 ${getNotificationColor(notification.priority)} hover:bg-gray-50 transition-colors`}
              >
                <div className="flex items-start space-x-3">
                  <div className="flex-shrink-0 mt-0.5">
                    {getNotificationIcon(notification.type, notification.priority)}
                  </div>

                  <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <p className="text-sm font-medium text-gray-900">
                          {notification.title}
                        </p>
                        <p className="text-sm text-gray-600 mt-1">
                          {notification.message}
                        </p>

                        <div className="flex items-center mt-2 space-x-4">
                          <span className="text-xs text-gray-500">
                            {formatTimestamp(notification.timestamp)}
                          </span>

                          {notification.priority === 'urgent' && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
                              Urgent
                            </span>
                          )}

                          {notification.priority === 'high' && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-800">
                              High Priority
                            </span>
                          )}
                        </div>

                        {/* Action Button */}
                        {notification.actionable && notification.actionUrl && (
                          <div className="mt-3">
                            <a
                              href={notification.actionUrl}
                              className="inline-flex items-center px-3 py-1 text-xs font-medium text-blue-600 bg-blue-100 rounded-md hover:bg-blue-200 transition-colors"
                            >
                              Take Action
                              <ExternalLink className="w-3 h-3 ml-1" />
                            </a>
                          </div>
                        )}
                      </div>

                      <button
                        onClick={() => dismissNotification(notification.timestamp)}
                        className="flex-shrink-0 ml-2 p-1 text-gray-400 hover:text-gray-600 transition-colors"
                        aria-label="Dismiss notification"
                      >
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Footer */}
      {notifications.length > 0 && (
        <div className="px-4 py-3 bg-gray-50 border-t border-gray-200 text-center">
          <p className="text-xs text-gray-600">
            Showing {visibleNotifications.length} of {notifications.length} notifications
          </p>
        </div>
      )}
    </div>
  );
};
