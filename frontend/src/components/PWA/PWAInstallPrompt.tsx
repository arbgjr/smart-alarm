import React, { useState, useEffect } from 'react';
import { Download, X, Smartphone, Monitor, Wifi, WifiOff } from 'lucide-react';
import { usePWA } from '../../utils/pwaManager';

interface PWAInstallPromptProps {
  onClose?: () => void;
}

export const PWAInstallPrompt: React.FC<PWAInstallPromptProps> = ({ onClose }) => {
  const {
    capabilities,
    installApp,
    offlineReady,
    needRefresh,
    updateServiceWorker,
    requestNotificationPermission,
    subscribeToPushNotifications
  } = usePWA();

  const [isInstalling, setIsInstalling] = useState(false);
  const [showPrompt, setShowPrompt] = useState(false);
  const [installStep, setInstallStep] = useState<'install' | 'permissions' | 'complete'>('install');

  useEffect(() => {
    // Show prompt if app is installable and not already installed
    if (capabilities.isInstallable && !capabilities.isInstalled) {
      const hasSeenPrompt = localStorage.getItem('pwa-install-prompt-seen');
      if (!hasSeenPrompt) {
        setShowPrompt(true);
      }
    }
  }, [capabilities.isInstallable, capabilities.isInstalled]);

  const handleInstall = async () => {
    setIsInstalling(true);

    try {
      const success = await installApp();
      if (success) {
        setInstallStep('permissions');
      } else {
        handleClose();
      }
    } catch (error) {
      console.error('Installation failed:', error);
      handleClose();
    } finally {
      setIsInstalling(false);
    }
  };

  const handlePermissions = async () => {
    try {
      const notificationGranted = await requestNotificationPermission();
      if (notificationGranted) {
        await subscribeToPushNotifications();
      }
      setInstallStep('complete');
    } catch (error) {
      console.error('Permission setup failed:', error);
      setInstallStep('complete');
    }
  };

  const handleClose = () => {
    setShowPrompt(false);
    localStorage.setItem('pwa-install-prompt-seen', 'true');
    onClose?.();
  };

  const handleUpdateApp = () => {
    updateServiceWorker();
  };

  if (!showPrompt && !needRefresh && !offlineReady) {
    return null;
  }

  return (
    <>
      {/* Update Available Notification */}
      {needRefresh && (
        <div className="fixed top-4 right-4 z-50 bg-blue-600 text-white rounded-lg shadow-lg p-4 max-w-sm">
          <div className="flex items-start space-x-3">
            <Download className="w-5 h-5 mt-0.5 flex-shrink-0" />
            <div className="flex-1">
              <h4 className="font-medium">Update Available</h4>
              <p className="text-sm text-blue-100 mt-1">
                A new version of Smart Alarm is ready to install.
              </p>
              <div className="flex space-x-2 mt-3">
                <button
                  onClick={handleUpdateApp}
                  className="bg-white text-blue-600 px-3 py-1 rounded text-sm font-medium hover:bg-blue-50 transition-colors"
                >
                  Update Now
                </button>
                <button
                  onClick={() => updateServiceWorker()}
                  className="text-blue-100 hover:text-white px-3 py-1 rounded text-sm transition-colors"
                >
                  Later
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Offline Ready Notification */}
      {offlineReady && (
        <div className="fixed bottom-4 right-4 z-50 bg-green-600 text-white rounded-lg shadow-lg p-4 max-w-sm">
          <div className="flex items-center space-x-3">
            <WifiOff className="w-5 h-5 flex-shrink-0" />
            <div>
              <h4 className="font-medium">Ready for Offline Use</h4>
              <p className="text-sm text-green-100 mt-1">
                Smart Alarm is now available offline!
              </p>
            </div>
            <button
              onClick={() => setShowPrompt(false)}
              className="text-green-100 hover:text-white transition-colors"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Install Prompt */}
      {showPrompt && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl max-w-md w-full">
            {installStep === 'install' && (
              <>
                <div className="p-6">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-lg font-semibold text-gray-900">
                      Install Smart Alarm
                    </h3>
                    <button
                      onClick={handleClose}
                      className="text-gray-400 hover:text-gray-600 transition-colors"
                    >
                      <X className="w-5 h-5" />
                    </button>
                  </div>

                  <div className="text-center mb-6">
                    <div className="w-16 h-16 bg-blue-100 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Smartphone className="w-8 h-8 text-blue-600" />
                    </div>
                    <p className="text-gray-600 mb-4">
                      Install Smart Alarm on your device for the best experience with:
                    </p>

                    <div className="space-y-2 text-left">
                      <div className="flex items-center space-x-3">
                        <Wifi className="w-4 h-4 text-green-600" />
                        <span className="text-sm text-gray-700">Offline access to your alarms</span>
                      </div>
                      <div className="flex items-center space-x-3">
                        <Monitor className="w-4 h-4 text-blue-600" />
                        <span className="text-sm text-gray-700">Native app experience</span>
                      </div>
                      <div className="flex items-center space-x-3">
                        <Download className="w-4 h-4 text-purple-600" />
                        <span className="text-sm text-gray-700">Faster loading and performance</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex space-x-3">
                    <button
                      onClick={handleClose}
                      className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors"
                    >
                      Not Now
                    </button>
                    <button
                      onClick={handleInstall}
                      disabled={isInstalling}
                      className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {isInstalling ? 'Installing...' : 'Install App'}
                    </button>
                  </div>
                </div>
              </>
            )}

            {installStep === 'permissions' && (
              <>
                <div className="p-6">
                  <div className="text-center mb-6">
                    <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Download className="w-8 h-8 text-green-600" />
                    </div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">
                      App Installed Successfully!
                    </h3>
                    <p className="text-gray-600 mb-4">
                      Enable notifications to receive alarm alerts even when the app is closed.
                    </p>
                  </div>

                  <div className="flex space-x-3">
                    <button
                      onClick={() => setInstallStep('complete')}
                      className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-md hover:bg-gray-50 transition-colors"
                    >
                      Skip
                    </button>
                    <button
                      onClick={handlePermissions}
                      className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                    >
                      Enable Notifications
                    </button>
                  </div>
                </div>
              </>
            )}

            {installStep === 'complete' && (
              <>
                <div className="p-6">
                  <div className="text-center">
                    <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Download className="w-8 h-8 text-green-600" />
                    </div>
                    <h3 className="text-lg font-semibold text-gray-900 mb-2">
                      You're All Set!
                    </h3>
                    <p className="text-gray-600 mb-6">
                      Smart Alarm is now installed and ready to use. You can find it on your home screen or app drawer.
                    </p>

                    <button
                      onClick={handleClose}
                      className="w-full px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
                    >
                      Get Started
                    </button>
                  </div>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </>
  );
};
