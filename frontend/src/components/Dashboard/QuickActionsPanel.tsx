import React, { useState } from 'react';
import {
  Clock,
  Repeat,
  Calendar,
  Settings,
  Download,
  Upload,
  RefreshCw,
  BarChart3,
  Zap
} from 'lucide-react';
import { Link } from 'react-router-dom';

interface QuickActionsPanelProps {
  onCreateAlarm?: () => void;
  onCreateRoutine?: () => void;
  onRefreshData?: () => void;
  onImportData?: () => void;
  onExportData?: () => void;
  className?: string;
}

export const QuickActionsPanel: React.FC<QuickActionsPanelProps> = ({
  onCreateAlarm,
  onCreateRoutine,
  onRefreshData,
  onImportData,
  onExportData,
  className = ''
}) => {
  const [isRefreshing, setIsRefreshing] = useState(false);

  const handleRefresh = async () => {
    if (onRefreshData) {
      setIsRefreshing(true);
      try {
        await onRefreshData();
      } finally {
        setTimeout(() => setIsRefreshing(false), 1000); // Minimum visual feedback
      }
    }
  };

  const primaryActions = [
    {
      id: 'create-alarm',
      label: 'Create Alarm',
      icon: Clock,
      color: 'bg-blue-600 hover:bg-blue-700',
      onClick: onCreateAlarm,
      description: 'Set up a new alarm'
    },
    {
      id: 'create-routine',
      label: 'Create Routine',
      icon: Repeat,
      color: 'bg-green-600 hover:bg-green-700',
      onClick: onCreateRoutine,
      description: 'Create a recurring schedule'
    },
    {
      id: 'refresh',
      label: 'Refresh Data',
      icon: RefreshCw,
      color: 'bg-purple-600 hover:bg-purple-700',
      onClick: handleRefresh,
      description: 'Update dashboard metrics',
      loading: isRefreshing
    }
  ];

  const secondaryActions = [
    {
      id: 'manage-alarms',
      label: 'Manage Alarms',
      icon: Calendar,
      href: '/alarms',
      description: 'View and edit all alarms'
    },
    {
      id: 'settings',
      label: 'Settings',
      icon: Settings,
      href: '/settings',
      description: 'Configure preferences'
    },
    {
      id: 'analytics',
      label: 'Analytics',
      icon: BarChart3,
      href: '/analytics',
      description: 'View detailed reports'
    }
  ];

  const utilityActions = [
    {
      id: 'import',
      label: 'Import',
      icon: Upload,
      onClick: onImportData,
      description: 'Import alarm data'
    },
    {
      id: 'export',
      label: 'Export',
      icon: Download,
      onClick: onExportData,
      description: 'Export your data'
    }
  ];

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 ${className}`}>
      {/* Header */}
      <div className="px-6 py-4 border-b border-gray-200">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-medium text-gray-900">Quick Actions</h3>
          <div className="flex items-center space-x-2">
            <Zap className="w-4 h-4 text-yellow-500" />
            <span className="text-sm text-gray-600">Shortcuts</span>
          </div>
        </div>
      </div>

      <div className="p-6">
        {/* Primary Actions */}
        <div className="mb-6">
          <h4 className="text-sm font-medium text-gray-700 mb-3">Primary Actions</h4>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {primaryActions.map((action) => (
              <button
                key={action.id}
                onClick={action.onClick}
                disabled={action.loading}
                className={`
                  relative group p-4 rounded-lg text-white transition-all duration-200
                  ${action.color}
                  ${action.loading ? 'opacity-75 cursor-not-allowed' : 'hover:shadow-lg transform hover:-translate-y-0.5'}
                `}
                title={action.description}
              >
                <div className="flex flex-col items-center text-center">
                  <action.icon
                    className={`w-6 h-6 mb-2 ${action.loading ? 'animate-spin' : ''}`}
                  />
                  <span className="text-sm font-medium">{action.label}</span>
                </div>

                {/* Tooltip */}
                <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-3 py-1 bg-gray-900 text-white text-xs rounded-lg opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none whitespace-nowrap">
                  {action.description}
                  <div className="absolute top-full left-1/2 transform -translate-x-1/2 border-4 border-transparent border-t-gray-900"></div>
                </div>
              </button>
            ))}
          </div>
        </div>

        {/* Secondary Actions */}
        <div className="mb-6">
          <h4 className="text-sm font-medium text-gray-700 mb-3">Navigation</h4>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
            {secondaryActions.map((action) => (
              <Link
                key={action.id}
                to={action.href}
                className="group p-4 border border-gray-200 rounded-lg hover:border-gray-300 hover:shadow-md transition-all duration-200 transform hover:-translate-y-0.5"
                title={action.description}
              >
                <div className="flex flex-col items-center text-center">
                  <action.icon className="w-6 h-6 mb-2 text-gray-600 group-hover:text-gray-900" />
                  <span className="text-sm font-medium text-gray-700 group-hover:text-gray-900">
                    {action.label}
                  </span>
                </div>
              </Link>
            ))}
          </div>
        </div>

        {/* Utility Actions */}
        <div>
          <h4 className="text-sm font-medium text-gray-700 mb-3">Data Management</h4>
          <div className="flex flex-wrap gap-3">
            {utilityActions.map((action) => (
              <button
                key={action.id}
                onClick={action.onClick}
                className="group flex items-center px-4 py-2 border border-gray-200 rounded-lg text-sm font-medium text-gray-700 hover:border-gray-300 hover:text-gray-900 hover:shadow-sm transition-all duration-200"
                title={action.description}
              >
                <action.icon className="w-4 h-4 mr-2 text-gray-500 group-hover:text-gray-700" />
                {action.label}
              </button>
            ))}
          </div>
        </div>

        {/* Status Indicators */}
        <div className="mt-6 pt-4 border-t border-gray-200">
          <div className="flex items-center justify-between text-sm">
            <div className="flex items-center space-x-4">
              <div className="flex items-center">
                <div className="w-2 h-2 bg-green-500 rounded-full mr-2"></div>
                <span className="text-gray-600">System Online</span>
              </div>
              <div className="flex items-center">
                <div className="w-2 h-2 bg-blue-500 rounded-full mr-2"></div>
                <span className="text-gray-600">Real-time Sync</span>
              </div>
            </div>

            <div className="text-gray-500">
              Last updated: {new Date().toLocaleTimeString()}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
