import React, { useState, useEffect } from 'react';
import { Download, X, Smartphone, Monitor, Wifi, WifiOff } from 'lucide-react';
import { usePWA } from '../../utils/pwaManager';

interface InstallPromptProps {
  className?: string;
}

export const InstallPrompt: React.FC<InstallPromptProps> = ({ className = '' }) => {
  const {
    capabilities,
    installApp,
    needRefresh,
    offlineReady,
    updateServiceWorker,
    addEventListener,
    removeEventListener
  } = usePWA();

  const [showInstallPrompt, setShowInstallPrompt] = useState(false);
  const [showUpdatePrompt, setShowUpdatePrompt] = useState(false);
  const [isInstalling, setIsInstalling] = useState(false);
  const [installDismissed, setInstallDismissed] = useState(false);

  useEffect(() => {
    // Check if install prompt was previously dismissed
    const dismissed = localStorage.getItem('pwa-install-dismissed');
    if (dismissed) {
      const dismissedDate = new Date(dismissed);
      const daysSinceDismissed = (Date.now() - dismissedDate.getTime()) / (1000 * 60 * 60 * 24);

      // Show again after 7 days
      if (daysSinceDismissed < 7) {
        setInstallDismissed(true);
      }
    }

    // Listen for installable event
    const handleInstallable = (isInstallable: boolean) => {
      setShowInstallPrompt(isInstallable && !capabilities.isInstalled && !installDismissed);
    };

    addEventListener('installable', handleInstallable);

    // Set initial state
    if (capabilities.isInstallable && !capabilities.isInstalled && !installDismissed) {
      setShowInstallPrompt(true);
    }

    return () => {
      removeEventListener('installable', handleInstallable);
    };
  }, [capabilities, installDismissed, addEventListener, removeEventListener]);

  useEffect(() => {
    setShowUpdatePrompt(needRefresh);
  }, [needRefresh]);

  const handleInstall = async () => {
    setIsInstalling(true);
    try {
      const success = await installApp();
      if (success) {
        setShowInstallPrompt(false);
      }
    } catch (error) {
      console.error('Installation failed:', error);
    } finally {
      setIsInstalling(false);
    }
  };

  const handleDismissInstall = () => {
    setShowInstallPrompt(false);
    setInstallDismissed(true);
    localStorage.setItem('pwa-install-dismissed', new Date().toISOString());
  };

  const handleUpdate = () => {
    updateServiceWorker();
    setShowUpdatePrompt(false);
  };

  const getDeviceIcon = () => {
    if (capabilities.isInstallable) {
      return window.innerWidth < 768 ? Smartphone : Monitor;
    }
    return Download;
  };

  const DeviceIcon = getDeviceIcon();

  if (!showInstallPrompt && !showUpdatePrompt && !offlineReady) {
    return null;
  }

  return (
    <div className={`fixed bottom-4 right-4 z-50 ${className}`}>
      {/* Install Prompt */}
      {showInstallPrompt && (
        <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-4 mb-4 max-w-sm">
          <div className="flex items-start space-x-3">
            <div className="flex-shrink-0">
              <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                <DeviceIcon className="w-5 h-5 text-blue-600" />
              </div>
            </div>

            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-medium text-gray-900">
                Install Smart Alarm
              </h3>
              <p className="text-xs text-gray-600 mt-1">
                Install the app for a better experience with offline support and quick access.
              </p>

              <div className="flex items-center space-x-2 mt-3">
                <button
                  onClick={handleInstall}
                  disabled={isInstalling}
                  className="inline-flex items-center px-3 py-1 text-xs font-medium text-white bg-blue-600 rounded hover:bg-blue-700 disabled:opacity-50 transition-colors"
                >
                  {isInstalling ? (
                    <>
                      <div className="w-3 h-3 border border-white border-t-transparent rounded-full animate-spin mr-1"></div>
                      Installing...
                    </>
                  ) : (
                    <>
                      <Download className="w-3 h-3 mr-1" />
                      Install
                    </>
                  )}
                </button>

                <button
                  onClick={handleDismissInstall}
                  className="text-xs text-gray-500 hover:text-gray-700 transition-colors"
                >
                  Not now
                </button>
              </div>
            </div>

            <button
              onClick={handleDismissInstall}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Update Prompt */}
      {showUpdatePrompt && (
        <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-4 mb-4 max-w-sm">
          <div className="flex items-start space-x-3">
            <div className="flex-shrink-0">
              <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
                <Download className="w-5 h-5 text-green-600" />
              </div>
            </div>

            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-medium text-gray-900">
                Update Available
              </h3>
              <p className="text-xs text-gray-600 mt-1">
                A new version of Smart Alarm is available with improvements and bug fixes.
              </p>

              <div className="flex items-center space-x-2 mt-3">
                <button
                  onClick={handleUpdate}
                  className="inline-flex items-center px-3 py-1 text-xs font-medium text-white bg-green-600 rounded hover:bg-green-700 transition-colors"
                >
                  <Download className="w-3 h-3 mr-1" />
                  Update Now
                </button>

                <button
                  onClick={() => setShowUpdatePrompt(false)}
                  className="text-xs text-gray-500 hover:text-gray-700 transition-colors"
                >
                  Later
                </button>
              </div>
            </div>

            <button
              onClick={() => setShowUpdatePrompt(false)}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600 transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Offline Ready Notification */}
      {offlineReady && (
        <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-4 mb-4 max-w-sm">
          <div className="flex items-start space-x-3">
            <div className="flex-shrink-0">
              <div className="w-10 h-10 bg-purple-100 rounded-lg flex items-center justify-center">
                {capabilities.isOnline ? (
                  <Wifi className="w-5 h-5 text-purple-600" />
                ) : (
                  <WifiOff className="w-5 h-5 text-purple-600" />
                )}
              </div>
            </div>

            <div className="flex-1 min-w-0">
              <h3 className="text-sm font-medium text-gray-900">
                Offline Ready
              </h3>
              <p className="text-xs text-gray-600 mt-1">
                Smart Alarm is now available offline. You can use the app even without an internet connection.
              </p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
