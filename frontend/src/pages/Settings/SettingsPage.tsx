import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, User, Bell, Link as LinkIcon, Download, Save, RefreshCw } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { ErrorBoundary } from '../../components/molecules/ErrorBoundary/ErrorBoundary';
import { UserProfileSettings } from '../../components/Settings/UserProfileSettings';
import { NotificationSettings } from '../../components/Settings/NotificationSettings';
import { IntegrationSettings } from '../../components/Settings/IntegrationSettings';
import { ImportExportSettings } from '../../components/Settings/ImportExportSettings';

interface SettingsPageProps {}

type SettingsTab = 'profile' | 'notifications' | 'integrations' | 'import-export';

export const SettingsPage: React.FC<SettingsPageProps> = () => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<SettingsTab>('profile');
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);

  const tabs = [
    {
      id: 'profile' as SettingsTab,
      name: 'Profile',
      icon: User,
      description: 'Personal information and account settings'
    },
    {
      id: 'notifications' as SettingsTab,
      name: 'Notifications',
      icon: Bell,
      description: 'Notification preferences and delivery settings'
    },
    {
      id: 'integrations' as SettingsTab,
      name: 'Integrations',
      icon: LinkIcon,
      description: 'Connect external services and calendars'
    },
    {
      id: 'import-export' as SettingsTab,
      name: 'Import/Export',
      icon: Download,
      description: 'Backup and restore your alarm data'
    }
  ];

  const handleSaveAll = async () => {
    try {
      // TODO: Implement save all settings
      console.log('Saving all settings...');
      setHasUnsavedChanges(false);
    } catch (error) {
      console.error('Failed to save settings:', error);
    }
  };

  const renderTabContent = () => {
    switch (activeTab) {
      case 'profile':
        return (
          <UserProfileSettings
            user={user}
            onSettingsChange={() => setHasUnsavedChanges(true)}
          />
        );
      case 'notifications':
        return (
          <NotificationSettings
            onSettingsChange={() => setHasUnsavedChanges(true)}
          />
        );
      case 'integrations':
        return (
          <IntegrationSettings
            onSettingsChange={() => setHasUnsavedChanges(true)}
          />
        );
      case 'import-export':
        return (
          <ImportExportSettings
            onSettingsChange={() => setHasUnsavedChanges(true)}
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center space-x-4">
              <Link
                to="/dashboard"
                className="text-gray-600 hover:text-gray-900 transition-colors"
                aria-label="Back to dashboard"
              >
                <ArrowLeft className="w-6 h-6" />
              </Link>
              <h1 className="text-2xl font-bold text-gray-900">Settings</h1>
            </div>

            <div className="flex items-center space-x-4">
              {hasUnsavedChanges && (
                <button
                  onClick={handleSaveAll}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                >
                  <Save className="w-4 h-4 mr-2" />
                  Save Changes
                </button>
              )}

              <div className="flex items-center space-x-2">
                <span className="text-sm text-gray-700">
                  {user?.name || user?.email || 'User'}
                </span>
                <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
                  <span className="text-white text-sm font-medium">
                    {(user?.name?.[0] || user?.email?.[0] || 'U').toUpperCase()}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </header>

      {/* Page Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="lg:grid lg:grid-cols-12 lg:gap-x-8">
            {/* Sidebar Navigation */}
            <aside className="lg:col-span-3">
              <nav className="space-y-1">
                {tabs.map((tab) => {
                  const Icon = tab.icon;
                  return (
                    <button
                      key={tab.id}
                      onClick={() => setActiveTab(tab.id)}
                      className={`w-full group rounded-lg px-3 py-3 flex items-start text-left transition-colors ${
                        activeTab === tab.id
                          ? 'bg-blue-50 border-blue-200 text-blue-700'
                          : 'text-gray-900 hover:bg-gray-50'
                      }`}
                    >
                      <Icon className={`flex-shrink-0 -ml-1 mr-3 h-6 w-6 ${
                        activeTab === tab.id ? 'text-blue-500' : 'text-gray-400 group-hover:text-gray-500'
                      }`} />
                      <div className="flex-1">
                        <p className={`text-sm font-medium ${
                          activeTab === tab.id ? 'text-blue-900' : 'text-gray-900'
                        }`}>
                          {tab.name}
                        </p>
                        <p className={`text-xs mt-1 ${
                          activeTab === tab.id ? 'text-blue-700' : 'text-gray-500'
                        }`}>
                          {tab.description}
                        </p>
                      </div>
                    </button>
                  );
                })}
              </nav>

              {/* Unsaved Changes Indicator */}
              {hasUnsavedChanges && (
                <div className="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                  <div className="flex items-center">
                    <RefreshCw className="w-5 h-5 text-yellow-600 mr-2" />
                    <div>
                      <p className="text-sm font-medium text-yellow-800">
                        Unsaved Changes
                      </p>
                      <p className="text-xs text-yellow-700 mt-1">
                        Don't forget to save your changes before leaving.
                      </p>
                    </div>
                  </div>
                </div>
              )}
            </aside>

            {/* Main Content */}
            <div className="mt-6 lg:mt-0 lg:col-span-9">
              <ErrorBoundary fallback={
                <div className="bg-white shadow-sm rounded-lg border border-gray-200 p-6">
                  <div className="text-center py-8">
                    <p className="text-gray-500">Unable to load settings. Please try refreshing the page.</p>
                  </div>
                </div>
              }>
                {renderTabContent()}
              </ErrorBoundary>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};
