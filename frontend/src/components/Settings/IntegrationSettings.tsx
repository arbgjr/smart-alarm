import React, { useState } from 'react';
import { Calendar, Smartphone, Wifi, Link, Unlink, CheckCircle, XCircle, AlertCircle, RefreshCw } from 'lucide-react';

interface IntegrationSettingsProps {
  onSettingsChange: () => void;
}

interface Integration {
  id: string;
  name: string;
  type: 'calendar' | 'notification' | 'smart_device' | 'webhook';
  status: 'connected' | 'disconnected' | 'error' | 'pending';
  description: string;
  icon: React.ComponentType<any>;
  lastSync?: string;
  settings?: Record<string, any>;
}

interface WebhookConfig {
  url: string;
  events: string[];
  headers: Record<string, string>;
  enabled: boolean;
}

export const IntegrationSettings: React.FC<IntegrationSettingsProps> = ({
  onSettingsChange
}) => {
  const [integrations, setIntegrations] = useState<Integration[]>([
    {
      id: 'google-calendar',
      name: 'Google Calendar',
      type: 'calendar',
      status: 'disconnected',
      description: 'Sync alarms with your Google Calendar events',
      icon: Calendar,
      settings: {
        syncDirection: 'bidirectional',
        calendarId: 'primary'
      }
    },
    {
      id: 'outlook-calendar',
      name: 'Outlook Calendar',
      type: 'calendar',
      status: 'disconnected',
      description: 'Sync alarms with your Outlook Calendar events',
      icon: Calendar,
      settings: {
        syncDirection: 'bidirectional',
        calendarId: 'primary'
      }
    },
    {
      id: 'apple-calendar',
      name: 'Apple Calendar',
      type: 'calendar',
      status: 'disconnected',
      description: 'Sync alarms with your Apple Calendar events',
      icon: Calendar,
      settings: {
        syncDirection: 'bidirectional'
      }
    },
    {
      id: 'push-notifications',
      name: 'Push Notifications',
      type: 'notification',
      status: 'connected',
      description: 'Browser push notifications for alarms',
      icon: Smartphone,
      lastSync: new Date().toISOString()
    }
  ]);

  const [webhooks, setWebhooks] = useState<WebhookConfig[]>([
    {
      url: '',
      events: ['alarm.triggered', 'alarm.dismissed'],
      headers: {},
      enabled: false
    }
  ]);

  // const [showWebhookForm] = useState(false);

  const handleConnect = async (integrationId: string) => {
    setIntegrations(prev => prev.map(integration =>
      integration.id === integrationId
        ? { ...integration, status: 'pending' }
        : integration
    ));

    try {
      // Simulate connection process
      await new Promise(resolve => setTimeout(resolve, 2000));

      setIntegrations(prev => prev.map(integration =>
        integration.id === integrationId
          ? {
              ...integration,
              status: 'connected',
              lastSync: new Date().toISOString()
            }
          : integration
      ));

      onSettingsChange();
    } catch (error) {
      setIntegrations(prev => prev.map(integration =>
        integration.id === integrationId
          ? { ...integration, status: 'error' }
          : integration
      ));
    }
  };

  const handleDisconnect = async (integrationId: string) => {
    if (window.confirm('Are you sure you want to disconnect this integration?')) {
      setIntegrations(prev => prev.map(integration =>
        integration.id === integrationId
          ? { ...integration, status: 'disconnected', lastSync: undefined }
          : integration
      ));
      onSettingsChange();
    }
  };

  const handleSync = async (integrationId: string) => {
    setIntegrations(prev => prev.map(integration =>
      integration.id === integrationId
        ? { ...integration, status: 'pending' }
        : integration
    ));

    try {
      // Simulate sync process
      await new Promise(resolve => setTimeout(resolve, 1500));

      setIntegrations(prev => prev.map(integration =>
        integration.id === integrationId
          ? {
              ...integration,
              status: 'connected',
              lastSync: new Date().toISOString()
            }
          : integration
      ));
    } catch (error) {
      setIntegrations(prev => prev.map(integration =>
        integration.id === integrationId
          ? { ...integration, status: 'error' }
          : integration
      ));
    }
  };

  const addWebhook = () => {
    setWebhooks(prev => [...prev, {
      url: '',
      events: ['alarm.triggered'],
      headers: {},
      enabled: false
    }]);
    onSettingsChange();
  };

  const updateWebhook = (index: number, field: keyof WebhookConfig, value: any) => {
    setWebhooks(prev => prev.map((webhook, i) =>
      i === index ? { ...webhook, [field]: value } : webhook
    ));
    onSettingsChange();
  };

  const removeWebhook = (index: number) => {
    setWebhooks(prev => prev.filter((_, i) => i !== index));
    onSettingsChange();
  };

  const getStatusIcon = (status: Integration['status']) => {
    switch (status) {
      case 'connected':
        return <CheckCircle className="w-5 h-5 text-green-600" />;
      case 'disconnected':
        return <XCircle className="w-5 h-5 text-gray-400" />;
      case 'error':
        return <AlertCircle className="w-5 h-5 text-red-600" />;
      case 'pending':
        return <RefreshCw className="w-5 h-5 text-blue-600 animate-spin" />;
      default:
        return <XCircle className="w-5 h-5 text-gray-400" />;
    }
  };

  const getStatusText = (status: Integration['status']) => {
    switch (status) {
      case 'connected':
        return 'Connected';
      case 'disconnected':
        return 'Disconnected';
      case 'error':
        return 'Error';
      case 'pending':
        return 'Connecting...';
      default:
        return 'Unknown';
    }
  };

  const formatLastSync = (timestamp?: string) => {
    if (!timestamp) return 'Never';

    const date = new Date(timestamp);
    const now = new Date();
    const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));

    if (diffInMinutes < 1) return 'Just now';
    if (diffInMinutes < 60) return `${diffInMinutes}m ago`;
    if (diffInMinutes < 1440) return `${Math.floor(diffInMinutes / 60)}h ago`;
    return date.toLocaleDateString();
  };

  const availableEvents = [
    'alarm.triggered',
    'alarm.dismissed',
    'alarm.snoozed',
    'alarm.created',
    'alarm.updated',
    'alarm.deleted',
    'routine.created',
    'routine.updated',
    'sync.completed'
  ];

  return (
    <div className="space-y-6">
      {/* Calendar Integrations */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Calendar Integrations</h3>
          <p className="text-sm text-gray-600 mt-1">
            Connect your calendar services to sync alarms and events
          </p>
        </div>

        <div className="p-6">
          <div className="space-y-4">
            {integrations.filter(i => i.type === 'calendar').map((integration) => {
              const Icon = integration.icon;
              return (
                <div key={integration.id} className="flex items-center justify-between p-4 border border-gray-200 rounded-lg">
                  <div className="flex items-center space-x-4">
                    <Icon className="w-8 h-8 text-blue-600" />
                    <div>
                      <h4 className="text-sm font-medium text-gray-900">{integration.name}</h4>
                      <p className="text-xs text-gray-600">{integration.description}</p>
                      {integration.lastSync && (
                        <p className="text-xs text-gray-500 mt-1">
                          Last sync: {formatLastSync(integration.lastSync)}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center space-x-3">
                    <div className="flex items-center space-x-2">
                      {getStatusIcon(integration.status)}
                      <span className="text-sm text-gray-600">
                        {getStatusText(integration.status)}
                      </span>
                    </div>

                    <div className="flex space-x-2">
                      {integration.status === 'connected' && (
                        <button
                          onClick={() => handleSync(integration.id)}
                          className="px-3 py-1 text-xs bg-blue-100 text-blue-700 rounded hover:bg-blue-200 transition-colors"
                        >
                          Sync
                        </button>
                      )}

                      {integration.status === 'connected' ? (
                        <button
                          onClick={() => handleDisconnect(integration.id)}
                          className="px-3 py-1 text-xs bg-red-100 text-red-700 rounded hover:bg-red-200 transition-colors"
                        >
                          Disconnect
                        </button>
                      ) : (
                        <button
                          onClick={() => handleConnect(integration.id)}
                          disabled={integration.status === 'pending'}
                          className="px-3 py-1 text-xs bg-green-100 text-green-700 rounded hover:bg-green-200 transition-colors disabled:opacity-50"
                        >
                          Connect
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Notification Services */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <h3 className="text-lg font-medium text-gray-900">Notification Services</h3>
          <p className="text-sm text-gray-600 mt-1">
            Configure notification delivery methods
          </p>
        </div>

        <div className="p-6">
          <div className="space-y-4">
            {integrations.filter(i => i.type === 'notification').map((integration) => {
              const Icon = integration.icon;
              return (
                <div key={integration.id} className="flex items-center justify-between p-4 border border-gray-200 rounded-lg">
                  <div className="flex items-center space-x-4">
                    <Icon className="w-8 h-8 text-purple-600" />
                    <div>
                      <h4 className="text-sm font-medium text-gray-900">{integration.name}</h4>
                      <p className="text-xs text-gray-600">{integration.description}</p>
                      {integration.lastSync && (
                        <p className="text-xs text-gray-500 mt-1">
                          Last used: {formatLastSync(integration.lastSync)}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center space-x-3">
                    <div className="flex items-center space-x-2">
                      {getStatusIcon(integration.status)}
                      <span className="text-sm text-gray-600">
                        {getStatusText(integration.status)}
                      </span>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </div>

      {/* Webhooks */}
      <div className="bg-white shadow-sm rounded-lg border border-gray-200">
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-lg font-medium text-gray-900">Webhooks</h3>
              <p className="text-sm text-gray-600 mt-1">
                Send alarm events to external services via HTTP webhooks
              </p>
            </div>
            <button
              onClick={addWebhook}
              className="inline-flex items-center px-3 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 transition-colors"
            >
              <Link className="w-4 h-4 mr-2" />
              Add Webhook
            </button>
          </div>
        </div>

        <div className="p-6">
          {webhooks.length === 0 ? (
            <div className="text-center py-8 text-gray-500">
              <Wifi className="w-8 h-8 mx-auto mb-2 text-gray-300" />
              <p>No webhooks configured</p>
            </div>
          ) : (
            <div className="space-y-4">
              {webhooks.map((webhook, index) => (
                <div key={index} className="border border-gray-200 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-4">
                    <h4 className="text-sm font-medium text-gray-900">
                      Webhook {index + 1}
                    </h4>
                    <div className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        checked={webhook.enabled}
                        onChange={(e) => updateWebhook(index, 'enabled', e.target.checked)}
                        className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                      />
                      <span className="text-sm text-gray-600">Enabled</span>
                      <button
                        onClick={() => removeWebhook(index)}
                        className="text-red-600 hover:text-red-800 transition-colors"
                      >
                        <Unlink className="w-4 h-4" />
                      </button>
                    </div>
                  </div>

                  <div className="space-y-3">
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        Webhook URL
                      </label>
                      <input
                        type="url"
                        value={webhook.url}
                        onChange={(e) => updateWebhook(index, 'url', e.target.value)}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                        placeholder="https://your-service.com/webhook"
                      />
                    </div>

                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">
                        Events to Send
                      </label>
                      <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                        {availableEvents.map(event => (
                          <label key={event} className="flex items-center space-x-2">
                            <input
                              type="checkbox"
                              checked={webhook.events.includes(event)}
                              onChange={(e) => {
                                const newEvents = e.target.checked
                                  ? [...webhook.events, event]
                                  : webhook.events.filter(e => e !== event);
                                updateWebhook(index, 'events', newEvents);
                              }}
                              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                            />
                            <span className="text-xs text-gray-700">{event}</span>
                          </label>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
