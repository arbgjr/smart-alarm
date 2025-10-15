import React, { useState } from 'react';
import { Bell, Smartphone, Mail, Volume2, Vibrate, Clock, Calendar, AlertTriangle } from 'lucide-react';

interface NotificationSettingsProps {
  onSettingsChange: () => void;
}

interface NotificationPreferences {
  pushNotifications: boolean;
  emailNotifications: boolean;
  smsNotifications: boolean;
  soundEnabled: boolean;
  vibrationEnabled: boolean;
  quietHours: {
    enabled: boolean;
    startTime: string;
    endTime: string;
  };
  alarmNotifications: {
    beforeTrigger: boolean;
    beforeTriggerMinutes: number;
    onTrigger: boolean;
    onSnooze: boolean;
    onDismiss: boolean;
  };
  systemNotifications: {
    syncComplete: boolean;
    errorAlerts: boolean;
    updateNotifications: boolean;
    maintenanceAlerts: boolean;
  };
  notificationSound: string;
  volume: number;
}

export const NotificationSettings: React.FC<NotificationSettingsProps> = ({
  onSettingsChange
}) => {
  const [preferences, setPreferences] = useState<NotificationPreferences>({
    pushNotifications: true,
    emailNotifications: true,
    smsNotifications: false,
    soundEnabled: true,
    vibrationEnabled: true,
    quietHours: {
      enabled: false,
      startTime: '22:00',
      endTime: '07:00'
    },
    alarmNotifications: {
      beforeTrigger: true,
      beforeTriggerMinutes: 5,
      onTrigger: true,
      onSnooze: true,
      onDismiss: false
    },
    systemNotifications: {
      syncComplete: false,
      errorAlerts: true,
      updateNotifications: true,
      maintenanceAlerts: true
    },
    notificationSound: 'default',
    volume: 80
  });

  const handlePreferenceChange = (path: string, value: any) => {
    setPreferences(prev => {
      const keys = path.split('.');
      const newPrefs = { ...prev };
      let current: any = newPrefs;

      for (let i = 0; i < keys.length - 1; i++) {
        current[keys[i]] = { ...current[keys[i]] };
        current = current[keys[i]];
      }

      current[keys[keys.length - 1]] = value;
      return newPrefs;
    });
    onSettingsChange();
  };

  const testNotification = async () => {
    if ('Notification' in window) {
      if (Notification.permission === 'granted') {
        new Notification('Smart Alarm Test', {
          body: 'This is a test notification from Smart Alarm',
          icon: '/pwa-192x192.png'
        });
      } else if (Notification.permission !== 'denied') {
        const permission = await Notification.requestPermission();
        if (permission === 'granted') {
          new Notification('Smart Alarm Test', {
            body: 'This is a test notification from Smart Alarm',
            icon: '/pwa-192x192.png'
          });
        }
      }
    }
  };

  const notificationSounds = [
    { value: 'default', label: 'Default' },
    { value: 'gentle', label: 'Gentle Chime' },
    { value: 'classic', label: 'Classic Bell' },
    { value: 'modern', label: 'Modern Tone' },
    { value: 'nature', label: 'Nature Sounds' },
    { value: 'custom', label: 'Custom Sound' }
  ];

  return (
    <div className="space-y-6">
      {/* General Notification Settings */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">General Notifications</h3>
          <p className="text-sm text-gray-600 mt-1">
            Configure how you want to receive notifications
          </p>
        </div>

        <div className="p-6 space-y-6">
          {/* Notification Channels */}
          <div>
            <h4 className="text-md font-medium text-gray-900 mb-4">Notification Channels</h4>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <Smartphone className="w-5 h-5 text-blue-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Push Notifications</p>
                    <p className="text-xs text-gray-600">Receive notifications on this device</p>
                  </div>
                </div>
                <div className="flex items-center space-x-3">
                  <input
                    type="checkbox"
                    checked={preferences.pushNotifications}
                    onChange={(e) => handlePreferenceChange('pushNotifications', e.target.checked)}
                    className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                  />
                  <button
                    onClick={testNotification}
                    className="text-xs text-blue-600 hover:text-blue-800 underline"
                  >
                    Test
                  </button>
                </div>
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <Mail className="w-5 h-5 text-green-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Email Notifications</p>
                    <p className="text-xs text-gray-600">Receive notifications via email</p>
                  </div>
                </div>
                <input
                  type="checkbox"
                  checked={preferences.emailNotifications}
                  onChange={(e) => handlePreferenceChange('emailNotifications', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <Smartphone className="w-5 h-5 text-purple-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">SMS Notifications</p>
                    <p className="text-xs text-gray-600">Receive notifications via text message</p>
                  </div>
                </div>
                <input
                  type="checkbox"
                  checked={preferences.smsNotifications}
                  onChange={(e) => handlePreferenceChange('smsNotifications', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>
            </div>
          </div>

          {/* Sound & Vibration */}
          <div className="pt-6 border-t border-gray-200">
            <h4 className="text-md font-medium text-gray-900 mb-4">Sound & Vibration</h4>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <Volume2 className="w-5 h-5 text-orange-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Sound Enabled</p>
                    <p className="text-xs text-gray-600">Play sound for notifications</p>
                  </div>
                </div>
                <input
                  type="checkbox"
                  checked={preferences.soundEnabled}
                  onChange={(e) => handlePreferenceChange('soundEnabled', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <Vibrate className="w-5 h-5 text-red-600" />
                  <div>
                    <p className="text-sm font-medium text-gray-900">Vibration Enabled</p>
                    <p className="text-xs text-gray-600">Vibrate device for notifications</p>
                  </div>
                </div>
                <input
                  type="checkbox"
                  checked={preferences.vibrationEnabled}
                  onChange={(e) => handlePreferenceChange('vibrationEnabled', e.target.checked)}
                  className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                />
              </div>

              {preferences.soundEnabled && (
                <div className="ml-8 space-y-3">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Notification Sound
                    </label>
                    <select
                      value={preferences.notificationSound}
                      onChange={(e) => handlePreferenceChange('notificationSound', e.target.value)}
                      className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                    >
                      {notificationSounds.map(sound => (
                        <option key={sound.value} value={sound.value}>
                          {sound.label}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Volume: {preferences.volume}%
                    </label>
                    <input
                      type="range"
                      min="0"
                      max="100"
                      value={preferences.volume}
                      onChange={(e) => handlePreferenceChange('volume', parseInt(e.target.value))}
                      className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer"
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Quiet Hours */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Quiet Hours</h3>
          <p className="text-sm text-gray-600 mt-1">
            Automatically silence notifications during specific hours
          </p>
        </div>

        <div className="p-6">
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center space-x-3">
              <Clock className="w-5 h-5 text-indigo-600" />
              <div>
                <p className="text-sm font-medium text-gray-900">Enable Quiet Hours</p>
                <p className="text-xs text-gray-600">Silence non-critical notifications</p>
              </div>
            </div>
            <input
              type="checkbox"
              checked={preferences.quietHours.enabled}
              onChange={(e) => handlePreferenceChange('quietHours.enabled', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          {preferences.quietHours.enabled && (
            <div className="ml-8 grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Start Time
                </label>
                <input
                  type="time"
                  value={preferences.quietHours.startTime}
                  onChange={(e) => handlePreferenceChange('quietHours.startTime', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  End Time
                </label>
                <input
                  type="time"
                  value={preferences.quietHours.endTime}
                  onChange={(e) => handlePreferenceChange('quietHours.endTime', e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                />
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Alarm Notifications */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Alarm Notifications</h3>
          <p className="text-sm text-gray-600 mt-1">
            Configure notifications related to your alarms
          </p>
        </div>

        <div className="p-6 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">Before Alarm Triggers</p>
              <p className="text-xs text-gray-600">Notify before alarm goes off</p>
            </div>
            <div className="flex items-center space-x-3">
              <input
                type="checkbox"
                checked={preferences.alarmNotifications.beforeTrigger}
                onChange={(e) => handlePreferenceChange('alarmNotifications.beforeTrigger', e.target.checked)}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
              {preferences.alarmNotifications.beforeTrigger && (
                <select
                  value={preferences.alarmNotifications.beforeTriggerMinutes}
                  onChange={(e) => handlePreferenceChange('alarmNotifications.beforeTriggerMinutes', parseInt(e.target.value))}
                  className="text-sm border border-gray-300 rounded px-2 py-1"
                >
                  <option value={1}>1 min</option>
                  <option value={5}>5 min</option>
                  <option value={10}>10 min</option>
                  <option value={15}>15 min</option>
                  <option value={30}>30 min</option>
                </select>
              )}
            </div>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">When Alarm Triggers</p>
              <p className="text-xs text-gray-600">Notify when alarm starts ringing</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.alarmNotifications.onTrigger}
              onChange={(e) => handlePreferenceChange('alarmNotifications.onTrigger', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">When Alarm is Snoozed</p>
              <p className="text-xs text-gray-600">Notify when alarm is snoozed</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.alarmNotifications.onSnooze}
              onChange={(e) => handlePreferenceChange('alarmNotifications.onSnooze', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">When Alarm is Dismissed</p>
              <p className="text-xs text-gray-600">Notify when alarm is turned off</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.alarmNotifications.onDismiss}
              onChange={(e) => handlePreferenceChange('alarmNotifications.onDismiss', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>
        </div>
      </div>

      {/* System Notifications */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">System Notifications</h3>
          <p className="text-sm text-gray-600 mt-1">
            Configure system and maintenance notifications
          </p>
        </div>

        <div className="p-6 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">Sync Complete</p>
              <p className="text-xs text-gray-600">Notify when data sync is complete</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.systemNotifications.syncComplete}
              onChange={(e) => handlePreferenceChange('systemNotifications.syncComplete', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">Error Alerts</p>
              <p className="text-xs text-gray-600">Notify about system errors</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.systemNotifications.errorAlerts}
              onChange={(e) => handlePreferenceChange('systemNotifications.errorAlerts', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">Update Notifications</p>
              <p className="text-xs text-gray-600">Notify about app updates</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.systemNotifications.updateNotifications}
              onChange={(e) => handlePreferenceChange('systemNotifications.updateNotifications', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-gray-900">Maintenance Alerts</p>
              <p className="text-xs text-gray-600">Notify about scheduled maintenance</p>
            </div>
            <input
              type="checkbox"
              checked={preferences.systemNotifications.maintenanceAlerts}
              onChange={(e) => handlePreferenceChange('systemNotifications.maintenanceAlerts', e.target.checked)}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
          </div>
        </div>
      </div>
    </div>
  );
};
