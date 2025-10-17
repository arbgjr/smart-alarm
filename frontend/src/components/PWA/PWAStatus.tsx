import React, { useState } from 'react';
import { Wifi, WifiOff, Smartphone, Bell, BellOff, Download, RefreshCw } from 'lucide-react';
import { usePWA } from '../../utils/pwaManager';

interface PWAStatusProps {
  className?: string;
  showDetails?: boolean;
}

export const PWAStatus: React.FC<PWAStatusProps> = ({
  className = '',
  showDetails = false
}) => {
  const {
    capabilities,
    installApp,
    requestNotificationPermission,
    subscribeToPushNotifications,
    needRefresh,
    updateServiceWorker
  } = usePWA();

  const [isUpdating, setIsUpdating] = useState(false);

  const handleInstall = async () => {
    try {
      await installApp();
    } catch (error) {
      console.error('Installation failed:', error);
    }
  };

  const handleEnableNotifications = async () => {
    try {
      const granted = await requestNotificationPermission();
      if (granted) {
        await subscribeToPushNotifications();
      }
    } catch (error) {
      console.error('Failed to enable notifications:', error);
    }
  };

  const handleUpdate = async () => {
    setIsUpdating(true);
    try {
      updateServiceWorker();
    } catch (error) {
      console.error('Update failed:', error);
    } finally {
      setIsUpdating(false);
    }
  };

  if (!showDetails) {
    // Compact status indicator
    return (
      <div className={`flex items-center space-x-2 ${className}`}>
        {/* Online/Offline Status */}
        <div className="flex items-center space-x-1">
          {capabilities.isOnline ? (
            <div title="Online">
              <Wifi className="w-4 h-4 text-green-600" />
            </div>
          ) : (
            <div title="Offline">
              <WifiOff className="w-4 h-4 text-red-600" />
            </div>
          )}
        </div>

        {/* PWA Status */}
        {capabilities.isInstalled && (
          <div title="Installed as PWA">
            <Smartphone className="w-4 h-4 text-blue-600" />
          </div>
        )}

        {/* Notification Status */}
        {capabilities.hasNotificationPermission ? (
          <div title="Notifications enabled">
            <Bell className="w-4 h-4 text-green-600" />
          </div>
        ) : (
          <div title="Notifications disabled">
            <BellOff className="w-4 h-4 text-gray-400" />
          </div>
        )}

        {/* Update Available */}
        {needRefresh && (
          <button
            onClick={handleUpdate}
            disabled={isUpdating}
            className="text-blue-600 hover:text-blue-800 transition-colors"
            title="Update available"
          >
            <RefreshCw className={`w-4 h-4 ${isUpdating ? 'animate-spin' : ''}`} />
          </button>
        )}
      </div>
    );
  }

  // Detailed status panel
  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
      <h4 className="text-sm font-medium text-gray-900 mb-3">App Status</h4>

      <div className="space-y-3">
        {/* Connection Status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            {capabilities.isOnline ? (
              <Wifi className="w-4 h-4 text-green-600" />
            ) : (
              <WifiOff className="w-4 h-4 text-red-600" />
            )}
            <span className="text-sm text-gray-700">Connection</span>
          </div>
          <span className={`text-xs font-medium ${
            capabilities.isOnline ? 'text-green-600' : 'text-red-600'
          }`}>
            {capabilities.isOnline ? 'Online' : 'Offline'}
          </span>
        </div>

        {/* Installation Status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <Smartphone className="w-4 h-4 text-blue-600" />
            <span className="text-sm text-gray-700">Installation</span>
          </div>
          <div className="flex items-center space-x-2">
            {capabilities.isInstalled ? (
              <span className="text-xs font-medium text-green-600">Installed</span>
            ) : capabilities.isInstallable ? (
              <button
                onClick={handleInstall}
                className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded hover:bg-blue-200 transition-colors"
              >
                Install
              </button>
            ) : (
              <span className="text-xs text-gray-500">Not Available</span>
            )}
          </div>
        </div>

        {/* Notification Status */}
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            {capabilities.hasNotificationPermission ? (
              <Bell className="w-4 h-4 text-green-600" />
            ) : (
              <BellOff className="w-4 h-4 text-gray-400" />
            )}
            <span className="text-sm text-gray-700">Notifications</span>
          </div>
          <div className="flex items-center space-x-2">
            {capabilities.hasNotificationPermission ? (
              <span className="text-xs font-medium text-green-600">Enabled</span>
            ) : (
              <button
                onClick={handleEnableNotifications}
                className="text-xs bg-yellow-100 text-yellow-700 px-2 py-1 rounded hover:bg-yellow-200 transition-colors"
              >
                Enable
              </button>
            )}
          </div>
        </div>

        {/* Update Status */}
        {needRefresh && (
          <div className="flex items-center justify-between pt-2 border-t border-gray-200">
            <div className="flex items-center space-x-2">
              <Download className="w-4 h-4 text-blue-600" />
              <span className="text-sm text-gray-700">Update Available</span>
            </div>
            <button
              onClick={handleUpdate}
              disabled={isUpdating}
              className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded hover:bg-blue-200 disabled:opacity-50 transition-colors"
            >
              {isUpdating ? 'Updating...' : 'Update'}
            </button>
          </div>
        )}

        {/* Capabilities Summary */}
        <div className="pt-2 border-t border-gray-200">
          <div className="grid grid-cols-2 gap-2 text-xs">
            <div className="flex items-center space-x-1">
              <div className={`w-2 h-2 rounded-full ${
                capabilities.supportsPushNotifications ? 'bg-green-500' : 'bg-gray-300'
              }`} />
              <span className="text-gray-600">Push Support</span>
            </div>
            <div className="flex items-center space-x-1">
              <div className={`w-2 h-2 rounded-full ${
                capabilities.supportsBackgroundSync ? 'bg-green-500' : 'bg-gray-300'
              }`} />
              <span className="text-gray-600">Background Sync</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
