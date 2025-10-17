import React, { useState, useEffect } from 'react';
import { Wifi, WifiOff, RefreshCw, AlertCircle } from 'lucide-react';
import { usePWA } from '../../utils/pwaManager';

interface OfflineIndicatorProps {
  className?: string;
  position?: 'top' | 'bottom';
}

export const OfflineIndicator: React.FC<OfflineIndicatorProps> = ({
  className = '',
  position = 'top'
}) => {
  const { capabilities } = usePWA();
  const [showIndicator, setShowIndicator] = useState(false);
  const [wasOffline, setWasOffline] = useState(false);
  const [pendingActions, setPendingActions] = useState<string[]>([]);

  useEffect(() => {
    // Show indicator when offline
    if (!capabilities.isOnline) {
      setShowIndicator(true);
      setWasOffline(true);
    } else if (wasOffline) {
      // Show briefly when coming back online
      setShowIndicator(true);
      const timer = setTimeout(() => {
        setShowIndicator(false);
        setWasOffline(false);
      }, 3000);
      return () => clearTimeout(timer);
    } else {
      setShowIndicator(false);
    }
  }, [capabilities.isOnline, wasOffline]);

  useEffect(() => {
    // Listen for pending actions (could be from background sync)
    const handlePendingAction = (event: Event) => {
      const customEvent = event as CustomEvent<string>;
      setPendingActions(prev => [...prev, customEvent.detail]);
    };

    const handleActionCompleted = (event: Event) => {
      const customEvent = event as CustomEvent<string>;
      setPendingActions(prev => prev.filter(a => a !== customEvent.detail));
    };

    // These would be connected to your background sync system
    window.addEventListener('pendingAction', handlePendingAction);
    window.addEventListener('actionCompleted', handleActionCompleted);

    return () => {
      window.removeEventListener('pendingAction', handlePendingAction);
      window.removeEventListener('actionCompleted', handleActionCompleted);
    };
  }, []);

  const handleRetry = () => {
    // Trigger retry of pending actions
    window.location.reload();
  };

  if (!showIndicator) {
    return null;
  }

  const positionClasses = position === 'top'
    ? 'top-0 left-0 right-0'
    : 'bottom-0 left-0 right-0';

  return (
    <div className={`fixed ${positionClasses} z-50 ${className}`}>
      <div className={`${
        capabilities.isOnline
          ? 'bg-green-600 text-white'
          : 'bg-yellow-600 text-white'
      } px-4 py-2 shadow-lg`}>
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center space-x-3">
            <div className="flex-shrink-0">
              {capabilities.isOnline ? (
                <Wifi className="w-5 h-5" />
              ) : (
                <WifiOff className="w-5 h-5" />
              )}
            </div>

            <div className="flex-1 min-w-0">
              {capabilities.isOnline ? (
                <div>
                  <p className="text-sm font-medium">
                    Back online!
                  </p>
                  {pendingActions.length > 0 && (
                    <p className="text-xs opacity-90">
                      Syncing {pendingActions.length} pending action{pendingActions.length !== 1 ? 's' : ''}...
                    </p>
                  )}
                </div>
              ) : (
                <div>
                  <p className="text-sm font-medium">
                    You're offline
                  </p>
                  <p className="text-xs opacity-90">
                    Some features may be limited. Changes will sync when you're back online.
                  </p>
                </div>
              )}
            </div>
          </div>

          <div className="flex items-center space-x-2">
            {!capabilities.isOnline && (
              <button
                onClick={handleRetry}
                className="inline-flex items-center px-2 py-1 text-xs font-medium bg-white bg-opacity-20 rounded hover:bg-opacity-30 transition-colors"
              >
                <RefreshCw className="w-3 h-3 mr-1" />
                Retry
              </button>
            )}

            {pendingActions.length > 0 && (
              <div className="flex items-center space-x-1">
                <div className="w-3 h-3 border border-white border-t-transparent rounded-full animate-spin"></div>
                <span className="text-xs font-medium">
                  {pendingActions.length}
                </span>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

interface OfflineActionsBannerProps {
  className?: string;
}

export const OfflineActionsBanner: React.FC<OfflineActionsBannerProps> = ({
  className = ''
}) => {
  const { capabilities } = usePWA();
  const [offlineActions, setOfflineActions] = useState<Array<{
    id: string;
    type: string;
    description: string;
    timestamp: string;
  }>>([]);

  useEffect(() => {
    // Load offline actions from localStorage or IndexedDB
    const loadOfflineActions = () => {
      try {
        const stored = localStorage.getItem('offline-actions');
        if (stored) {
          setOfflineActions(JSON.parse(stored));
        }
      } catch (error) {
        console.error('Failed to load offline actions:', error);
      }
    };

    loadOfflineActions();

    // Listen for new offline actions
    const handleOfflineAction = (event: CustomEvent) => {
      const action = event.detail;
      setOfflineActions(prev => [...prev, action]);

      // Store in localStorage
      try {
        const updated = [...offlineActions, action];
        localStorage.setItem('offline-actions', JSON.stringify(updated));
      } catch (error) {
        console.error('Failed to store offline action:', error);
      }
    };

    window.addEventListener('offlineAction', handleOfflineAction as EventListener);

    return () => {
      window.removeEventListener('offlineAction', handleOfflineAction as EventListener);
    };
  }, [offlineActions]);

  const clearOfflineActions = () => {
    setOfflineActions([]);
    localStorage.removeItem('offline-actions');
  };

  if (capabilities.isOnline || offlineActions.length === 0) {
    return null;
  }

  return (
    <div className={`bg-blue-50 border border-blue-200 rounded-lg p-4 ${className}`}>
      <div className="flex items-start space-x-3">
        <AlertCircle className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" />

        <div className="flex-1 min-w-0">
          <h3 className="text-sm font-medium text-blue-900">
            Offline Actions Pending
          </h3>
          <p className="text-sm text-blue-700 mt-1">
            You have {offlineActions.length} action{offlineActions.length !== 1 ? 's' : ''} that will be synced when you're back online.
          </p>

          <div className="mt-3 space-y-2">
            {offlineActions.slice(0, 3).map((action) => (
              <div key={action.id} className="flex items-center justify-between text-xs">
                <span className="text-blue-800">{action.description}</span>
                <span className="text-blue-600">
                  {new Date(action.timestamp).toLocaleTimeString()}
                </span>
              </div>
            ))}

            {offlineActions.length > 3 && (
              <p className="text-xs text-blue-600">
                And {offlineActions.length - 3} more...
              </p>
            )}
          </div>

          <div className="mt-3 flex items-center space-x-3">
            <button
              onClick={clearOfflineActions}
              className="text-xs text-blue-600 hover:text-blue-800 font-medium"
            >
              Clear All
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
