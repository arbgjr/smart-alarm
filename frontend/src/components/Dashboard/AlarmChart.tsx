import React, { useMemo } from 'react';

interface AlarmChartData {
  date: string;
  triggered: number;
  dismissed: number;
  snoozed: number;
}

interface AlarmChartProps {
  data: AlarmChartData[];
  isLoading?: boolean;
  className?: string;
}

export const AlarmChart: React.FC<AlarmChartProps> = ({
  data,
  isLoading = false,
  className = ''
}) => {
  const chartData = useMemo(() => {
    if (!data || data.length === 0) return [];

    const maxValue = Math.max(
      ...data.flatMap(d => [d.triggered, d.dismissed, d.snoozed])
    );

    return data.map(item => ({
      ...item,
      triggeredHeight: maxValue > 0 ? (item.triggered / maxValue) * 100 : 0,
      dismissedHeight: maxValue > 0 ? (item.dismissed / maxValue) * 100 : 0,
      snoozedHeight: maxValue > 0 ? (item.snoozed / maxValue) * 100 : 0,
    }));
  }, [data]);

  if (isLoading) {
    return (
      <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
        <div className="animate-pulse">
          <div className="h-4 bg-gray-200 rounded w-1/3 mb-4"></div>
          <div className="h-48 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Alarm Activity</h3>
        <div className="flex items-center justify-center h-48 text-gray-500">
          <div className="text-center">
            <svg className="w-12 h-12 mx-auto mb-2 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
            </svg>
            <p>No alarm activity data available</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-medium text-gray-900">Alarm Activity</h3>
        <div className="flex items-center space-x-4 text-sm">
          <div className="flex items-center">
            <div className="w-3 h-3 bg-blue-500 rounded-full mr-2"></div>
            <span className="text-gray-600">Triggered</span>
          </div>
          <div className="flex items-center">
            <div className="w-3 h-3 bg-green-500 rounded-full mr-2"></div>
            <span className="text-gray-600">Dismissed</span>
          </div>
          <div className="flex items-center">
            <div className="w-3 h-3 bg-yellow-500 rounded-full mr-2"></div>
            <span className="text-gray-600">Snoozed</span>
          </div>
        </div>
      </div>

      <div className="relative h-48">
        <div className="flex items-end justify-between h-full space-x-2">
          {chartData.map((item, index) => (
            <div key={index} className="flex-1 flex flex-col items-center">
              <div className="relative w-full max-w-12 h-full flex items-end justify-center space-x-1">
                {/* Triggered bar */}
                <div
                  className="bg-blue-500 rounded-t-sm min-h-[2px] w-2 transition-all duration-300 hover:bg-blue-600"
                  style={{ height: `${item.triggeredHeight}%` }}
                  title={`Triggered: ${item.triggered}`}
                ></div>
                {/* Dismissed bar */}
                <div
                  className="bg-green-500 rounded-t-sm min-h-[2px] w-2 transition-all duration-300 hover:bg-green-600"
                  style={{ height: `${item.dismissedHeight}%` }}
                  title={`Dismissed: ${item.dismissed}`}
                ></div>
                {/* Snoozed bar */}
                <div
                  className="bg-yellow-500 rounded-t-sm min-h-[2px] w-2 transition-all duration-300 hover:bg-yellow-600"
                  style={{ height: `${item.snoozedHeight}%` }}
                  title={`Snoozed: ${item.snoozed}`}
                ></div>
              </div>
              <div className="mt-2 text-xs text-gray-500 text-center">
                {new Date(item.date).toLocaleDateString('en-US', {
                  month: 'short',
                  day: 'numeric'
                })}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Summary stats */}
      <div className="mt-4 pt-4 border-t border-gray-200">
        <div className="grid grid-cols-3 gap-4 text-center">
          <div>
            <p className="text-2xl font-bold text-blue-600">
              {data.reduce((sum, item) => sum + item.triggered, 0)}
            </p>
            <p className="text-sm text-gray-600">Total Triggered</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-green-600">
              {data.reduce((sum, item) => sum + item.dismissed, 0)}
            </p>
            <p className="text-sm text-gray-600">Total Dismissed</p>
          </div>
          <div>
            <p className="text-2xl font-bold text-yellow-600">
              {data.reduce((sum, item) => sum + item.snoozed, 0)}
            </p>
            <p className="text-sm text-gray-600">Total Snoozed</p>
          </div>
        </div>
      </div>
    </div>
  );
};
