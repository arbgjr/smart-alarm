import React, { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { useAlarms } from '../../hooks/useAlarms';
import { AlarmList } from '../../components/AlarmList';
import { AlarmForm } from '../../components/molecules/AlarmForm';
import { ErrorBoundary } from '../../components/molecules/ErrorBoundary/ErrorBoundary';
import { AlarmDto } from '../../services/alarmService';

interface AlarmsPageProps {}

export const AlarmsPage: React.FC<AlarmsPageProps> = () => {
  const { user } = useAuth();
  const { data: alarms, isLoading } = useAlarms();

  // Form state management
  const [showAlarmForm, setShowAlarmForm] = useState(false);
  const [editingAlarm, setEditingAlarm] = useState<AlarmDto | null>(null);

  const handleCreateAlarm = () => {
    setEditingAlarm(null);
    setShowAlarmForm(true);
  };

  const handleFormSuccess = () => {
    setShowAlarmForm(false);
    setEditingAlarm(null);
  };

  const handleFormCancel = () => {
    setShowAlarmForm(false);
    setEditingAlarm(null);
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center space-x-4">
              <button
                onClick={() => window.history.back()}
                className="text-gray-600 hover:text-gray-900 transition-colors"
                aria-label="Go back"
              >
                <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                </svg>
              </button>
              <h1 className="text-2xl font-bold text-gray-900">My Alarms</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-700">
                Welcome, {user?.name || user?.email || 'User'}
              </span>
              <button
                type="button"
                className="bg-white p-2 rounded-full text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                aria-label="User menu"
              >
                <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center">
                  <span className="text-white text-sm font-medium">
                    {(user?.name?.[0] || user?.email?.[0] || 'U').toUpperCase()}
                  </span>
                </div>
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Page Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {/* Page Header with Action */}
          <div className="flex justify-between items-center mb-8">
            <div>
              <h2 className="text-xl font-semibold text-gray-900">All Alarms</h2>
              <p className="text-gray-600 mt-1">
                Manage your alarms and set up automated notifications
              </p>
            </div>
            <button
              onClick={handleCreateAlarm}
              className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
            >
              <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              Create Alarm
            </button>
          </div>

          {/* Alarms List */}
          <div className="bg-white shadow rounded-lg">
            <ErrorBoundary fallback={
              <div className="text-center py-8">
                <p className="text-gray-500">Unable to load alarms. Please try refreshing the page.</p>
              </div>
            }>
              <div className="p-6">
                <AlarmList
                  alarms={alarms?.data || []}
                  isLoading={isLoading}
                  showActions={true}
                />
              </div>
            </ErrorBoundary>
          </div>
        </div>
      </main>

      {/* Alarm Form Modal */}
      <AlarmForm
        alarm={editingAlarm || undefined}
        isOpen={showAlarmForm}
        onSuccess={handleFormSuccess}
        onCancel={handleFormCancel}
      />
    </div>
  );
};
