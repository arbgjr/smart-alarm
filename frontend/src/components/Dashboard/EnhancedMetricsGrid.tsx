import React from 'react';
import { Clock, Calendar, Repeat, TrendingUp, Activity, Users, Zap, Target } from 'lucide-react';
import { MetricsCard } from './MetricsCard';

interface EnhancedMetricsGridProps {
  metrics: {
    activeAlarms: number;
    todaysAlarms: number;
    activeRoutines: number;
    totalTriggered: number;
    totalDismissed: number;
    totalSnoozed: number;
    successRate: number;
    avgResponseTime: number;
  };
  isLoading: boolean;
  className?: string;
}

export const EnhancedMetricsGrid: React.FC<EnhancedMetricsGridProps> = ({
  metrics,
  isLoading,
  className = ''
}) => {
  // Calculate derived metrics
  const totalAlarmActions = metrics.totalTriggered + metrics.totalDismissed + metrics.totalSnoozed;
  const dismissalRate = totalAlarmActions > 0 ? (metrics.totalDismissed / totalAlarmActions) * 100 : 0;
  const snoozeRate = totalAlarmActions > 0 ? (metrics.totalSnoozed / totalAlarmActions) * 100 : 0;
  const efficiency = metrics.avgResponseTime > 0 ? Math.max(0, 100 - (metrics.avgResponseTime * 10)) : 0;

  return (
    <div className={`grid gap-4 ${className}`}>
      {/* Primary Metrics Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricsCard
          title="Active Alarms"
          value={isLoading ? '...' : metrics.activeAlarms}
          icon={Clock}
          iconColor="bg-blue-500"
          isLoading={isLoading}
          change={{
            value: 12,
            type: 'increase',
            period: 'last week'
          }}
        />

        <MetricsCard
          title="Today's Alarms"
          value={isLoading ? '...' : metrics.todaysAlarms}
          icon={Calendar}
          iconColor="bg-green-500"
          isLoading={isLoading}
          change={{
            value: 5,
            type: 'increase',
            period: 'yesterday'
          }}
        />

        <MetricsCard
          title="Active Routines"
          value={isLoading ? '...' : metrics.activeRoutines}
          icon={Repeat}
          iconColor="bg-purple-500"
          isLoading={isLoading}
          change={{
            value: 0,
            type: 'neutral',
            period: 'last week'
          }}
        />

        <MetricsCard
          title="Success Rate"
          value={isLoading ? '...' : `${metrics.successRate.toFixed(1)}%`}
          icon={TrendingUp}
          iconColor="bg-orange-500"
          isLoading={isLoading}
          change={{
            value: 3.2,
            type: 'increase',
            period: 'last month'
          }}
        />
      </div>

      {/* Performance Metrics Row */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        <MetricsCard
          title="Total Triggered"
          value={isLoading ? '...' : metrics.totalTriggered}
          icon={Activity}
          iconColor="bg-blue-600"
          isLoading={isLoading}
          change={{
            value: 8.5,
            type: 'increase',
            period: 'last week'
          }}
        />

        <MetricsCard
          title="Avg Response Time"
          value={isLoading ? '...' : `${metrics.avgResponseTime}s`}
          icon={Zap}
          iconColor="bg-green-600"
          isLoading={isLoading}
          change={{
            value: 15,
            type: 'decrease',
            period: 'last week'
          }}
        />

        <MetricsCard
          title="Dismissal Rate"
          value={isLoading ? '...' : `${dismissalRate.toFixed(1)}%`}
          icon={Target}
          iconColor="bg-emerald-600"
          isLoading={isLoading}
          change={{
            value: 2.1,
            type: 'increase',
            period: 'last week'
          }}
        />

        <MetricsCard
          title="Efficiency Score"
          value={isLoading ? '...' : `${efficiency.toFixed(0)}%`}
          icon={Users}
          iconColor="bg-indigo-600"
          isLoading={isLoading}
          change={{
            value: 5.3,
            type: 'increase',
            period: 'last month'
          }}
        />
      </div>

      {/* Detailed Stats Row */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Dismissed</p>
              <p className="text-2xl font-bold text-green-600">
                {isLoading ? '...' : metrics.totalDismissed}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                {isLoading ? '...' : `${dismissalRate.toFixed(1)}% of all actions`}
              </p>
            </div>
            <div className="p-3 bg-green-100 rounded-lg">
              <Target className="w-6 h-6 text-green-600" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Snoozed</p>
              <p className="text-2xl font-bold text-yellow-600">
                {isLoading ? '...' : metrics.totalSnoozed}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                {isLoading ? '...' : `${snoozeRate.toFixed(1)}% of all actions`}
              </p>
            </div>
            <div className="p-3 bg-yellow-100 rounded-lg">
              <Clock className="w-6 h-6 text-yellow-600" />
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-600">Total Actions</p>
              <p className="text-2xl font-bold text-blue-600">
                {isLoading ? '...' : totalAlarmActions}
              </p>
              <p className="text-xs text-gray-500 mt-1">
                All alarm interactions
              </p>
            </div>
            <div className="p-3 bg-blue-100 rounded-lg">
              <Activity className="w-6 h-6 text-blue-600" />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
