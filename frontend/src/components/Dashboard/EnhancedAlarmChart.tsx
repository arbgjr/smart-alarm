import React, { useMemo, useState } from 'react';
import { BarChart3, TrendingUp, Calendar, Clock } from 'lucide-react';

interface AlarmChartData {
  date: string;
  triggered: number;
  dismissed: number;
  snoozed: number;
}

interface EnhancedAlarmChartProps {
  data: AlarmChartData[];
  isLoading?: boolean;
  className?: string;
}

type ChartView = 'bar' | 'line' | 'area';
type TimeRange = '7d' | '30d' | '90d';

export const EnhancedAlarmChart: React.FC<EnhancedAlarmChartProps> = ({
  data,
  isLoading = false,
  className = ''
}) => {
  const [chartView, setChartView] = useState<ChartView>('bar');
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');

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
      total: item.triggered + item.dismissed + item.snoozed,
      successRate: item.triggered > 0 ? (item.dismissed / item.triggered) * 100 : 0
    }));
  }, [data]);

  const summaryStats = useMemo(() => {
    if (!data || data.length === 0) return null;

    const totals = data.reduce(
      (acc, item) => ({
        triggered: acc.triggered + item.triggered,
        dismissed: acc.dismissed + item.dismissed,
        snoozed: acc.snoozed + item.snoozed
      }),
      { triggered: 0, dismissed: 0, snoozed: 0 }
    );

    const avgPerDay = {
      triggered: totals.triggered / data.length,
      dismissed: totals.dismissed / data.length,
      snoozed: totals.snoozed / data.length
    };

    const successRate = totals.triggered > 0 ? (totals.dismissed / totals.triggered) * 100 : 0;
    const snoozeRate = totals.triggered > 0 ? (totals.snoozed / totals.triggered) * 100 : 0;

    return {
      totals,
      avgPerDay,
      successRate,
      snoozeRate
    };
  }, [data]);

  if (isLoading) {
    return (
      <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
        <div className="animate-pulse">
          <div className="flex items-center justify-between mb-4">
            <div className="h-6 bg-gray-200 rounded w-1/3"></div>
            <div className="flex space-x-2">
              <div className="h-8 w-16 bg-gray-200 rounded"></div>
              <div className="h-8 w-16 bg-gray-200 rounded"></div>
            </div>
          </div>
          <div className="h-64 bg-gray-200 rounded mb-4"></div>
          <div className="grid grid-cols-3 gap-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-16 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (!data || data.length === 0) {
    return (
      <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
        <h3 className="text-lg font-medium text-gray-900 mb-4">Alarm Activity Trends</h3>
        <div className="flex items-center justify-center h-64 text-gray-500">
          <div className="text-center">
            <BarChart3 className="w-12 h-12 mx-auto mb-2 text-gray-300" />
            <p>No alarm activity data available</p>
            <p className="text-sm mt-1">Start using alarms to see trends here</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-6 ${className}`}>
      {/* Header with Controls */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h3 className="text-lg font-medium text-gray-900">Alarm Activity Trends</h3>
          <p className="text-sm text-gray-600 mt-1">
            Track your alarm patterns and success rates over time
          </p>
        </div>

        <div className="flex items-center space-x-2">
          {/* Chart View Toggle */}
          <div className="flex bg-gray-100 rounded-lg p-1">
            {(['bar', 'line', 'area'] as ChartView[]).map((view) => (
              <button
                key={view}
                onClick={() => setChartView(view)}
                className={`px-3 py-1 text-xs font-medium rounded-md transition-colors ${
                  chartView === view
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                {view.charAt(0).toUpperCase() + view.slice(1)}
              </button>
            ))}
          </div>

          {/* Time Range Toggle */}
          <div className="flex bg-gray-100 rounded-lg p-1">
            {(['7d', '30d', '90d'] as TimeRange[]).map((range) => (
              <button
                key={range}
                onClick={() => setTimeRange(range)}
                className={`px-3 py-1 text-xs font-medium rounded-md transition-colors ${
                  timeRange === range
                    ? 'bg-white text-gray-900 shadow-sm'
                    : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                {range}
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Legend */}
      <div className="flex items-center justify-center space-x-6 mb-6">
        <div className="flex items-center">
          <div className="w-3 h-3 bg-blue-500 rounded-full mr-2"></div>
          <span className="text-sm text-gray-600">Triggered</span>
        </div>
        <div className="flex items-center">
          <div className="w-3 h-3 bg-green-500 rounded-full mr-2"></div>
          <span className="text-sm text-gray-600">Dismissed</span>
        </div>
        <div className="flex items-center">
          <div className="w-3 h-3 bg-yellow-500 rounded-full mr-2"></div>
          <span className="text-sm text-gray-600">Snoozed</span>
        </div>
      </div>

      {/* Chart Area */}
      <div className="relative h-64 mb-6">
        {chartView === 'bar' && (
          <div className="flex items-end justify-between h-full space-x-2">
            {chartData.map((item, index) => (
              <div key={index} className="flex-1 flex flex-col items-center group">
                <div className="relative w-full max-w-16 h-full flex items-end justify-center space-x-1">
                  {/* Triggered bar */}
                  <div
                    className="bg-blue-500 rounded-t-sm min-h-[2px] w-3 transition-all duration-300 hover:bg-blue-600 group-hover:shadow-lg"
                    style={{ height: `${item.triggeredHeight}%` }}
                    title={`Triggered: ${item.triggered}`}
                  ></div>
                  {/* Dismissed bar */}
                  <div
                    className="bg-green-500 rounded-t-sm min-h-[2px] w-3 transition-all duration-300 hover:bg-green-600 group-hover:shadow-lg"
                    style={{ height: `${item.dismissedHeight}%` }}
                    title={`Dismissed: ${item.dismissed}`}
                  ></div>
                  {/* Snoozed bar */}
                  <div
                    className="bg-yellow-500 rounded-t-sm min-h-[2px] w-3 transition-all duration-300 hover:bg-yellow-600 group-hover:shadow-lg"
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

                {/* Hover tooltip */}
                <div className="absolute bottom-full mb-2 left-1/2 transform -translate-x-1/2 bg-gray-900 text-white text-xs rounded-lg px-3 py-2 opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none z-10">
                  <div className="text-center">
                    <div className="font-medium">{new Date(item.date).toLocaleDateString()}</div>
                    <div className="mt-1 space-y-1">
                      <div>Triggered: {item.triggered}</div>
                      <div>Dismissed: {item.dismissed}</div>
                      <div>Snoozed: {item.snoozed}</div>
                      <div>Success: {item.successRate.toFixed(1)}%</div>
                    </div>
                  </div>
                  <div className="absolute top-full left-1/2 transform -translate-x-1/2 border-4 border-transparent border-t-gray-900"></div>
                </div>
              </div>
            ))}
          </div>
        )}

        {chartView === 'line' && (
          <div className="relative h-full">
            <svg className="w-full h-full" viewBox="0 0 400 200">
              {/* Grid lines */}
              {[0, 25, 50, 75, 100].map((y) => (
                <line
                  key={y}
                  x1="0"
                  y1={200 - (y * 2)}
                  x2="400"
                  y2={200 - (y * 2)}
                  stroke="#f3f4f6"
                  strokeWidth="1"
                />
              ))}

              {/* Lines */}
              {chartData.length > 1 && (
                <>
                  {/* Triggered line */}
                  <polyline
                    fill="none"
                    stroke="#3b82f6"
                    strokeWidth="2"
                    points={chartData.map((item, index) =>
                      `${(index / (chartData.length - 1)) * 400},${200 - (item.triggeredHeight * 2)}`
                    ).join(' ')}
                  />

                  {/* Dismissed line */}
                  <polyline
                    fill="none"
                    stroke="#10b981"
                    strokeWidth="2"
                    points={chartData.map((item, index) =>
                      `${(index / (chartData.length - 1)) * 400},${200 - (item.dismissedHeight * 2)}`
                    ).join(' ')}
                  />

                  {/* Snoozed line */}
                  <polyline
                    fill="none"
                    stroke="#f59e0b"
                    strokeWidth="2"
                    points={chartData.map((item, index) =>
                      `${(index / (chartData.length - 1)) * 400},${200 - (item.snoozedHeight * 2)}`
                    ).join(' ')}
                  />
                </>
              )}

              {/* Data points */}
              {chartData.map((item, index) => (
                <g key={index}>
                  <circle
                    cx={(index / (chartData.length - 1)) * 400}
                    cy={200 - (item.triggeredHeight * 2)}
                    r="4"
                    fill="#3b82f6"
                    className="hover:r-6 transition-all"
                  />
                  <circle
                    cx={(index / (chartData.length - 1)) * 400}
                    cy={200 - (item.dismissedHeight * 2)}
                    r="4"
                    fill="#10b981"
                    className="hover:r-6 transition-all"
                  />
                  <circle
                    cx={(index / (chartData.length - 1)) * 400}
                    cy={200 - (item.snoozedHeight * 2)}
                    r="4"
                    fill="#f59e0b"
                    className="hover:r-6 transition-all"
                  />
                </g>
              ))}
            </svg>
          </div>
        )}
      </div>

      {/* Summary Statistics */}
      {summaryStats && (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 pt-4 border-t border-gray-200">
          <div className="text-center">
            <div className="flex items-center justify-center mb-2">
              <TrendingUp className="w-4 h-4 text-blue-600 mr-1" />
              <span className="text-sm font-medium text-gray-600">Total Triggered</span>
            </div>
            <p className="text-2xl font-bold text-blue-600">{summaryStats.totals.triggered}</p>
            <p className="text-xs text-gray-500">
              {summaryStats.avgPerDay.triggered.toFixed(1)} avg/day
            </p>
          </div>

          <div className="text-center">
            <div className="flex items-center justify-center mb-2">
              <Clock className="w-4 h-4 text-green-600 mr-1" />
              <span className="text-sm font-medium text-gray-600">Success Rate</span>
            </div>
            <p className="text-2xl font-bold text-green-600">
              {summaryStats.successRate.toFixed(1)}%
            </p>
            <p className="text-xs text-gray-500">
              {summaryStats.totals.dismissed} dismissed
            </p>
          </div>

          <div className="text-center">
            <div className="flex items-center justify-center mb-2">
              <Calendar className="w-4 h-4 text-yellow-600 mr-1" />
              <span className="text-sm font-medium text-gray-600">Snooze Rate</span>
            </div>
            <p className="text-2xl font-bold text-yellow-600">
              {summaryStats.snoozeRate.toFixed(1)}%
            </p>
            <p className="text-xs text-gray-500">
              {summaryStats.totals.snoozed} snoozed
            </p>
          </div>

          <div className="text-center">
            <div className="flex items-center justify-center mb-2">
              <BarChart3 className="w-4 h-4 text-purple-600 mr-1" />
              <span className="text-sm font-medium text-gray-600">Daily Average</span>
            </div>
            <p className="text-2xl font-bold text-purple-600">
              {(summaryStats.avgPerDay.triggered + summaryStats.avgPerDay.dismissed + summaryStats.avgPerDay.snoozed).toFixed(1)}
            </p>
            <p className="text-xs text-gray-500">
              total actions/day
            </p>
          </div>
        </div>
      )}
    </div>
  );
};
