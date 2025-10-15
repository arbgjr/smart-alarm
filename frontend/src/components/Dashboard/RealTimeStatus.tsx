import React, { useState, useEffect } from 'react';
import { Wifi, WifiOff, Activity, Users, Server } from 'lucide-react';
import { useSignalRConnection } from '../../utils/signalRConnection';

interface SystemStatus {
  isOnline: boolean;
  signalRConnected: boolean;
  lastSync: string | null;
  activeUsers: number;
  systemHealth: 'healthy' | 'warning' | 'error';
  uptime: string;
}

interface RealTimeStatusProps {
  className?: string;
}

export const RealTimeStatus: React.FC<RealTimeStatusProps> = ({ className = '' }) => {
  const { getStatus, connect, addEventListener, removeEventListener } = useSignalRConnection();
  const [systemStatus, setSystemStatus] = useState<SystemStatus>({
    isOnline: navigator.onLine,
    signalRConnected: false,
    lastSync: null,
    activeUsers: 0,
    systemHealth: 'healthy',
    uptime: '0m'
  });

  useEffect(() => {
    // Initialize SignalR connection
    connect();

    // Update connection status
    const updateConnectionStatus = () => {
      const status = getStatus();
      setSystemStatus(prev => ({
        ...prev,
        signalRConnected: status.isConnected,
        lastSync: status.lastConnected
      }));
    };

    // Listen for connection status changes
    const handleConnectionChange = () => {
      updateConnectionStatus();
    };

    const handleSyncComplete = () => {
      setSystemStatus(prev => ({
        ...prev,
        lastSync: new Date().toISOString()
      }));
    };

    const handleUserPresenceUpdate = (data: { activeUsers?: number }) => {
      if (data.activeUsers !== undefined) {
        setSystemStatus(prev => ({
          ...prev,
          activeUsers: data.activeUsers || 0
        }));
      }
    };

    // Add event listeners
    addEventListener('connectionStatusChanged', handleConnectionChange);
    addEventListener('syncComplete', handleSyncComplete);
    addEventListener('userPresenceUpdate', handleUserPresenceUpdate);

    // Listen for online/offline events
    const handleOnline = () => {
      setSystemStatus(prev => ({ ...prev, isOnline: true }));
      connect(); // Reconnect SignalR when back online
    };

    const handleOffline = () => {
      setSystemStatus(prev => ({ ...prev, isOnline: false }));
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    // Initial status update
    updateConnectionStatus();

    // Cleanup
    return () => {
      removeEventListener('connectionStatusChanged', handleConnectionChange);
      removeEventListener('syncComplete', handleSyncComplete);
      removeEventListener('userPresenceUpdate', handleUserPresenceUpdate);
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [connect, getStatus, addEventListener, removeEventListener]);

  const getStatusColor = (status: SystemStatus['systemHealth']) => {
    switch (status) {
      case 'healthy':
        return 'text-green-600 bg-green-100';
      case 'warning':
        return 'text-yellow-600 bg-yellow-100';
      case 'error':
        return 'text-red-600 bg-red-100';
      default:
        return 'text-gray-600 bg-gray-100';
    }
  };

  const formatLastSync = (timestamp: string | null) => {
    if (!timestamp) return 'Never';

    const date = new Date(timestamp);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));

    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}h ago`;
    return date.toLocaleDateString();
  };

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 p-4 ${className}`}>
      <div className="flex items-center justify-between mb-3">
        <h4 className="text-sm font-medium text-gray-900">System Status</h4>
        <div className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getStatusColor(systemStatus.systemHealth)}`}>
          <Activity className="w-3 h-3 mr-1" />
          {systemStatus.systemHealth}
        </div>
      </div>

      <div className="space-y-3">
        {/* Connection Status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            {systemStatus.isOnline ? (
              <Wifi className="w-4 h-4 text-green-600" />
            ) : (
              <WifiOff className="w-4 h-4 text-red-600" />
            )}
            <span className="text-sm text-gray-700">Internet</span>
          </div>
          <span className={`text-xs font-medium ${systemStatus.isOnline ? 'text-green-600' : 'text-red-600'}`}>
            {systemStatus.isOnline ? 'Connected' : 'Offline'}
          </span>
        </div>

        {/* SignalR Status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Server className="w-4 h-4 text-blue-600" />
            <span className="text-sm text-gray-700">Real-time</span>
          </div>
          <span className={`text-xs font-medium ${systemStatus.signalRConnected ? 'text-green-600' : 'text-red-600'}`}>
            {systemStatus.signalRConnected ? 'Connected' : 'Disconnected'}
          </span>
        </div>

        {/* Active Users */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Users className="w-4 h-4 text-purple-600" />
            <span className="text-sm text-gray-700">Active Users</span>
          </div>
          <span className="text-xs font-medium text-gray-900">
            {systemStatus.activeUsers}
          </span>
        </div>

        {/* Last Sync */}
        <div className="flex items-center justify-between">
          <span className="text-sm text-gray-700">Last Sync</span>
          <span className="text-xs text-gray-600">
            {formatLastSync(systemStatus.lastSync)}
          </span>
        </div>
      </div>

      {/* Connection Actions */}
      {!systemStatus.signalRConnected && systemStatus.isOnline && (
        <div className="mt-3 pt-3 border-t border-gray-200">
          <button
            onClick={connect}
            className="w-full text-xs bg-blue-600 text-white px-3 py-1 rounded hover:bg-blue-700 transition-colors"
          >
            Reconnect
          </button>
        </div>
      )}
    </div>
  );
};
