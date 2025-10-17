import React, { useState, useEffect } from 'react';
import {
  Smartphone,
  Download,
  Wifi,
  WifiOff,
  Bell,
  BellOff,
  RefreshCw,
  CheckCircle,
  XCircle,
  AlertTriangle,
  Monitor,
  Tablet
} from 'lucide-react';
import { usePWA } from '../../utils/pwaManager';
import { useViewport } from '../../utils/responsive';

interface PWAStatusPanelProps {
  className?: string;
  showDetails?: boolean;
}

export const PWAStatusPanel: React.FC<PWAStatusPanelProps> = ({
  className = '',
  showDetails = true
}) => {
  const {
    capabilities,
    installApp,
    requestNotificationPermission,
    subscribeToPushNotifications,
    unsubscribeFromPushNotifications,
    needRefresh,
    updateServiceWorker
  } = usePWA();

  const viewport = useViewport();
  const [isInstalling, setIsInstalling] = useState(false);
  const [isEnablingNotifications, setIsEnablingNotifications] = useState(false);
  const [pushSubscription, setPushSubscription] = useState<PushSubscription | null>(null);

  useEffect(() => {
    // Check current push subscription
    const checkPushSubscription = async () => {
      if ('serviceWorker' in navigator && 'PushManager' in window) {
        try {
          const registration = await navigator.serviceWorker.ready;
          const subscription = await registration.pushManager.getSubscription();
          setPushSubscription(subscription);
        } catch (error) {
          console.error('Failed to check push subscription:', error);
        }
      }
    };

    checkPushSubscription();
  }, []);

  const handleInstall = async () => {
    setIsInstalling(true);
    try {
      await installApp();
    } catch (error) {
      console.error('Installation failed:', error);
    } finally {
      setIsInstalling(false);
    }
  };

  const handleEnableNotifications = async () => {
    setIsEnablingNotifications(true);
    try {
      const granted = await requestNotificationPermission();
      if (granted) {
        const subscription = await subscribeToPushNotifications();
        setPushSubscription(subscription);
      }
    } catch (error) {
      console.error('Failed to enable notifications:', error);
    } finally {
      setIsEnablingNotifications(false);
    }
  };

  const handleDisableNotifications = async () => {
    try {
      await unsubscribeFromPushNotifications();
      setPushSubscription(null);
    } catch (error) {
      console.error('Failed to disable notifications:', error);
    }
  };

  const handleUpdate = () => {
    updateServiceWorker();
  };

  const getDeviceIcon = () => {
    if (viewport.isMobile) return Smartphone;
    if (viewport.isTablet) return Tablet;
    return Monitor;
  };

  const DeviceIcon = getDeviceIcon();

  const statusItems = [
    {
      id: 'installation',
      label: 'App Installation',
      status: (capabilities.isInstalled ? 'success' : capabilities.isInstallable ? 'warning' : 'error') as 'success' | 'warning' | 'error',
      description: capabilities.isInstalled
        ? 'App is installed and ready to use'
        : capabilities.isInstallable
          ? 'App can be installed for better experience'
          : 'Installation not available on this device',
      action: capabilities.isInstallable && !capabilities.isInstalled ? {
        label: 'Install App',
        onClick: handleInstall,
        loading: isInstalling,
        icon: Download
      } : undefined
    },
    {
      id: 'offline',
      label: 'Offline Support',
      status: (capabilities.isOnline ? 'success' : 'warning') as 'success' | 'warning' | 'error',
      description: capabilities.isOnline
        ? 'Online - all features available'
        : 'Offline - limited functionality available',
      icon: capabilities.isOnline ? Wifi : WifiOff
    },
    {
      id: 'notifications',
      label: 'Push Notifications',
      status: (capabilities.hasNotificationPermission ? 'success' : capabilities.supportsPushNotifications ? 'warning' : 'error') as 'success' | 'warning' | 'error',
      description: capabilities.hasNotificationPermission
        ? 'Notifications enabled and working'
        : capabilities.supportsPushNotifications
          ? 'Notifications supported but not enabled'
          : 'Push notifications not supported',
      action: capabilities.supportsPushNotifications ? {
        label: capabilities.hasNotificationPermission ? 'Disable Notifications' : 'Enable Notifications',
        onClick: capabilities.hasNotificationPermission ? handleDisableNotifications : handleEnableNotifications,
        loading: isEnablingNotifications,
        icon: capabilities.hasNotificationPermission ? BellOff : Bell
      } : undefined
    },
    {
      id: 'sync',
      label: 'Background Sync',
      status: (capabilities.supportsBackgroundSync ? 'success' : 'error') as 'success' | 'warning' | 'error',
      description: capabilities.supportsBackgroundSync
        ? 'Background sync available for offline actions'
        : 'Background sync not supported',
      icon: RefreshCw
    }
  ];

  const getStatusIcon = (status: 'success' | 'warning' | 'error') => {
    switch (status) {
      case 'success':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'warning':
        return <AlertTriangle className="w-5 h-5 text-yellow-600" />;
      case 'error':
        return <XCircle className="w-5 h-5 text-red-600" />;
    }
  };

  const getStatusColor = (status: 'success' | 'warning' | 'error') => {
    switch (status) {
      case 'success':
        return 'border-green-200 bg-green-50';
      case 'warning':
        return 'border-yellow-200 bg-yellow-50';
      case 'error':
        return 'border-red-200 bg-red-50';
    }
  };

  return (
    <div className={`bg-white rounded-lg shadow-sm border border-gray-200 ${className}`}>
      <div className="px-6 py-4 border-b border-gray-200">
        <div className="flex items-center space-x-3">
          <DeviceIcon className="w-6 h-6 text-blue-600" />
          <div>
            <h3 className="text-lg font-medium text-gray-900">PWA Status</h3>
            <p className="text-sm text-gray-600">
              Progressive Web App capabilities and features
            </p>
          </div>
        </div>
      </div>

      <div className="p-6">
        {/* Update Available Banner */}
        {needRefresh && (
          <div className="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <Download className="w-5 h-5 text-blue-600" />
                <div>
                  <h4 className="text-sm font-medium text-blue-900">Update Available</h4>
                  <p className="text-xs text-blue-700">A new version is ready to install</p>
                </div>
              </div>
              <button
                onClick={handleUpdate}
                className="px-3 py-1 text-xs font-medium text-white bg-blue-600 rounded hover:bg-blue-700 transition-colors"
              >
                Update Now
              </button>
            </div>
          </div>
        )}

        {/* Status Items */}
        <div className="space-y-4">
          {statusItems.map((item) => (
            <div
              key={item.id}
              className={`p-4 border rounded-lg ${getStatusColor(item.status)}`}
            >
              <div className="flex items-start justify-between">
                <div className="flex items-start space-x-3">
                  <div className="flex-shrink-0 mt-0.5">
                    {item.icon ? (
                      <item.icon className="w-5 h-5 text-gray-600" />
                    ) : (
                      getStatusIcon(item.status)
                    )}
                  </div>

                  <div className="flex-1 min-w-0">
                    <h4 className="text-sm font-medium text-gray-900">
                      {item.label}
                    </h4>
                    <p className="text-xs text-gray-600 mt-1">
                      {item.description}
                    </p>
                  </div>
                </div>

                <div className="flex items-center space-x-2">
                  {getStatusIcon(item.status)}

                  {item.action && (
                    <button
                      onClick={item.action.onClick}
                      disabled={item.action.loading}
                      className="inline-flex items-center px-3 py-1 text-xs font-medium text-white bg-blue-600 rounded hover:bg-blue-700 disabled:opacity-50 transition-colors"
                    >
                      {item.action.loading ? (
                        <div className="w-3 h-3 border border-white border-t-transparent rounded-full animate-spin mr-1"></div>
                      ) : (
                        <item.action.icon className="w-3 h-3 mr-1" />
                      )}
                      {item.action.label}
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>

        {/* Additional Details */}
        {showDetails && (
          <div className="mt-6 pt-6 border-t border-gray-200">
            <h4 className="text-sm font-medium text-gray-900 mb-3">Device Information</h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs text-gray-600">
              <div>
                <span className="font-medium">Device Type:</span> {
                  viewport.isMobile ? 'Mobile' : viewport.isTablet ? 'Tablet' : 'Desktop'
                }
              </div>
              <div>
                <span className="font-medium">Screen Size:</span> {viewport.width} × {viewport.height}
              </div>
              <div>
                <span className="font-medium">Orientation:</span> {viewport.orientation}
              </div>
              <div>
                <span className="font-medium">Touch Support:</span> {viewport.isTouch ? 'Yes' : 'No'}
              </div>
              <div>
                <span className="font-medium">Pixel Ratio:</span> {viewport.pixelRatio}x
              </div>
              <div>
                <span className="font-medium">User Agent:</span> {navigator.userAgent.split(' ')[0]}
              </div>
            </div>
          </div>
        )}

        {/* Push Subscription Details */}
        {pushSubscription && showDetails && (
          <div className="mt-4 pt-4 border-t border-gray-200">
            <h4 className="text-sm font-medium text-gray-900 mb-2">Push Subscription</h4>
            <div className="text-xs text-gray-600">
              <div className="mb-1">
                <span className="font-medium">Endpoint:</span> {pushSubscription.endpoint.substring(0, 50)}...
              </div>
              <div>
                <span className="font-medium">Status:</span> Active
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
