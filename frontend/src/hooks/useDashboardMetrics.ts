import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useSignalRConnection, useRealTimeAlarmEvents } from '../utils/signalRConnection';
// import { alarmService } from '../services/alarmService';

interface DashboardMetrics {
  activeAlarms: number;
  todaysAlarms: number;
  activeRoutines: number;
  totalTriggered: number;
  totalDismissed: number;
  totalSnoozed: number;
  successRate: number;
  avgResponseTime: number;
}

interface AlarmChartData {
  date: string;
  triggered: number;
  dismissed: number;
  snoozed: number;
}

interface ActivityItem {
  id: string;
  type: 'alarm_triggered' | 'alarm_dismissed' | 'alarm_snoozed' | 'alarm_created' | 'alarm_updated' | 'alarm_deleted';
  title: string;
  description: string;
  timestamp: string;
  alarmId?: string;
}

export function useDashboardMetrics() {
  const [metrics, setMetrics] = useState<DashboardMetrics>({
    activeAlarms: 0,
    todaysAlarms: 0,
    activeRoutines: 0,
    totalTriggered: 0,
    totalDismissed: 0,
    totalSnoozed: 0,
    successRate: 0,
    avgResponseTime: 0
  });

  const [chartData, setChartData] = useState<AlarmChartData[]>([]);
  const [recentActivity, setRecentActivity] = useState<ActivityItem[]>([]);

  const { connect } = useSignalRConnection();
  const { subscribeToAlarmEvents } = useRealTimeAlarmEvents();

  // Fetch dashboard metrics
  const { data: dashboardData, isLoading: isLoadingMetrics, refetch: refetchMetrics } = useQuery({
    queryKey: ['dashboard-metrics'],
    queryFn: async () => {
      try {
        // Fetch metrics from API
        const response = await fetch('/api/dashboard/metrics', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`,
            'Content-Type': 'application/json'
          }
        });

        if (!response.ok) {
          throw new Error('Failed to fetch dashboard metrics');
        }

        return await response.json();
      } catch (error) {
        console.error('Error fetching dashboard metrics:', error);
        // Return mock data for development
        return {
          metrics: {
            activeAlarms: 5,
            todaysAlarms: 3,
            activeRoutines: 2,
            totalTriggered: 45,
            totalDismissed: 38,
            totalSnoozed: 7,
            successRate: 84.4,
            avgResponseTime: 2.3
          },
          chartData: generateMockChartData(),
          recentActivity: generateMockActivity()
        };
      }
    },
    refetchInterval: 30000, // Refetch every 30 seconds
    staleTime: 10000 // Consider data stale after 10 seconds
  });

  // Update metrics when data is fetched
  useEffect(() => {
    if (dashboardData) {
      setMetrics(prevMetrics => dashboardData.metrics || prevMetrics);
      setChartData(dashboardData.chartData || []);
      setRecentActivity(dashboardData.recentActivity || []);
    }
  }, [dashboardData]);

  // Set up real-time event listeners
  useEffect(() => {
    // Connect to SignalR
    connect();

    // Subscribe to alarm events for real-time updates
    const unsubscribe = subscribeToAlarmEvents({
      onAlarmTriggered: (event) => {
        setMetrics(prev => ({
          ...prev,
          totalTriggered: prev.totalTriggered + 1
        }));

        addActivity({
          id: `triggered-${event.alarmId}-${Date.now()}`,
          type: 'alarm_triggered',
          title: 'Alarm Triggered',
          description: `Alarm was triggered and is ringing`,
          timestamp: event.timestamp,
          alarmId: event.alarmId
        });

        // Refetch metrics to get updated data
        refetchMetrics();
      },

      onAlarmDismissed: (event) => {
        setMetrics(prev => ({
          ...prev,
          totalDismissed: prev.totalDismissed + 1
        }));

        addActivity({
          id: `dismissed-${event.alarmId}-${Date.now()}`,
          type: 'alarm_dismissed',
          title: 'Alarm Dismissed',
          description: `Alarm was dismissed ${event.data?.dismissalMethod ? `via ${event.data.dismissalMethod}` : ''}`,
          timestamp: event.timestamp,
          alarmId: event.alarmId
        });

        refetchMetrics();
      },

      onAlarmSnoozed: (event) => {
        setMetrics(prev => ({
          ...prev,
          totalSnoozed: prev.totalSnoozed + 1
        }));

        addActivity({
          id: `snoozed-${event.alarmId}-${Date.now()}`,
          type: 'alarm_snoozed',
          title: 'Alarm Snoozed',
          description: `Alarm was snoozed ${event.data?.snoozeCount ? `(${event.data.snoozeCount} times)` : ''}`,
          timestamp: event.timestamp,
          alarmId: event.alarmId
        });

        refetchMetrics();
      },

      onAlarmUpdated: (event) => {
        addActivity({
          id: `updated-${event.alarmId}-${Date.now()}`,
          type: 'alarm_updated',
          title: 'Alarm Updated',
          description: 'Alarm settings were modified',
          timestamp: event.timestamp,
          alarmId: event.alarmId
        });

        refetchMetrics();
      }
    });

    return unsubscribe;
  }, [connect, subscribeToAlarmEvents, refetchMetrics]);

  const addActivity = (activity: ActivityItem) => {
    setRecentActivity(prev => [activity, ...prev.slice(0, 49)]); // Keep last 50 activities
  };

  const refreshMetrics = () => {
    refetchMetrics();
  };

  return {
    metrics,
    chartData,
    recentActivity,
    isLoading: isLoadingMetrics,
    refreshMetrics
  };
}

// Mock data generators for development
function generateMockChartData(): AlarmChartData[] {
  const data: AlarmChartData[] = [];
  const today = new Date();

  for (let i = 6; i >= 0; i--) {
    const date = new Date(today);
    date.setDate(date.getDate() - i);

    data.push({
      date: date.toISOString(),
      triggered: Math.floor(Math.random() * 10) + 1,
      dismissed: Math.floor(Math.random() * 8) + 1,
      snoozed: Math.floor(Math.random() * 3)
    });
  }

  return data;
}

function generateMockActivity(): ActivityItem[] {
  const activities: ActivityItem[] = [];
  const types: ActivityItem['type'][] = [
    'alarm_triggered', 'alarm_dismissed', 'alarm_snoozed',
    'alarm_created', 'alarm_updated', 'alarm_deleted'
  ];

  for (let i = 0; i < 10; i++) {
    const type = types[Math.floor(Math.random() * types.length)];
    const timestamp = new Date(Date.now() - Math.random() * 24 * 60 * 60 * 1000).toISOString();

    activities.push({
      id: `activity-${i}`,
      type,
      title: getActivityTitle(type),
      description: getActivityDescription(type),
      timestamp,
      alarmId: `alarm-${Math.floor(Math.random() * 100)}`
    });
  }

  return activities.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());
}

function getActivityTitle(type: ActivityItem['type']): string {
  switch (type) {
    case 'alarm_triggered':
      return 'Alarm Triggered';
    case 'alarm_dismissed':
      return 'Alarm Dismissed';
    case 'alarm_snoozed':
      return 'Alarm Snoozed';
    case 'alarm_created':
      return 'Alarm Created';
    case 'alarm_updated':
      return 'Alarm Updated';
    case 'alarm_deleted':
      return 'Alarm Deleted';
    default:
      return 'Unknown Activity';
  }
}

function getActivityDescription(type: ActivityItem['type']): string {
  switch (type) {
    case 'alarm_triggered':
      return 'Morning alarm was triggered and is ringing';
    case 'alarm_dismissed':
      return 'Alarm was dismissed by user';
    case 'alarm_snoozed':
      return 'Alarm was snoozed for 5 minutes';
    case 'alarm_created':
      return 'New alarm was created';
    case 'alarm_updated':
      return 'Alarm settings were modified';
    case 'alarm_deleted':
      return 'Alarm was permanently deleted';
    default:
      return 'Unknown activity occurred';
  }
}
