import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { useActiveAlarms, useTodaysAlarms } from '../../hooks/useAlarms';
import { useActiveRoutines } from '../../hooks/useRoutines';
import { AlarmList } from '../../components/AlarmList';
import { RoutineList } from '../../components/RoutineList';
import { AlarmForm } from '../../components/molecules/AlarmForm';
import { RoutineForm } from '../../components/molecules/RoutineForm';
import { ErrorBoundary } from '../../components/molecules/ErrorBoundary/ErrorBoundary';

interface DashboardProps {}

export const Dashboard: React.FC<DashboardProps> = () => {
  const { user, isLoading } = useAuth();
  const { data: activeAlarms, isLoading: isLoadingActiveAlarms } = useActiveAlarms();
  const { data: todaysAlarms, isLoading: isLoadingTodaysAlarms } = useTodaysAlarms();
  const { data: activeRoutines, isLoading: isLoadingActiveRoutines } = useActiveRoutines();

  // Form state management
  const [showAlarmForm, setShowAlarmForm] = useState(false);
  const [showRoutineForm, setShowRoutineForm] = useState(false);
  const [editingAlarm, setEditingAlarm] = useState<any>(null);
  const [editingRoutine, setEditingRoutine] = useState<any>(null);

  const handleCreateAlarm = () => {
    setEditingAlarm(null);
    setShowAlarmForm(true);
  };

  const handleCreateRoutine = () => {
    setEditingRoutine(null);
    setShowRoutineForm(true);
  };

  const handleFormSuccess = () => {
    setShowAlarmForm(false);
    setShowRoutineForm(false);
    setEditingAlarm(null);
    setEditingRoutine(null);
  };

  const handleFormCancel = () => {
    setShowAlarmForm(false);
    setShowRoutineForm(false);
    setEditingAlarm(null);
    setEditingRoutine(null);
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Navigation Header */}
      <header className="bg-white shadow-sm border-b border-gray-200">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-4">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">Smart Alarm</h1>
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

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {/* Welcome Section */}
          <div className="mb-8">
            <h2 className="text-3xl font-bold text-gray-900 mb-2">Dashboard</h2>
            <p className="text-gray-600">
              Manage your alarms and routines efficiently
            </p>
          </div>

          {/* Quick Stats */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 bg-blue-100 rounded-md flex items-center justify-center">
                    <svg className="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                    </svg>
                  </div>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-500">Active Alarms</p>
                  <p className="text-2xl font-semibold text-gray-900">
                    {isLoadingActiveAlarms ? '...' : activeAlarms?.length || 0}
                  </p>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 bg-green-100 rounded-md flex items-center justify-center">
                    <svg className="w-5 h-5 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                    </svg>
                  </div>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-500">Active Routines</p>
                  <p className="text-2xl font-semibold text-gray-900">
                    {isLoadingActiveRoutines ? '...' : activeRoutines?.length || 0}
                  </p>
                </div>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center">
                <div className="flex-shrink-0">
                  <div className="w-8 h-8 bg-yellow-100 rounded-md flex items-center justify-center">
                    <svg className="w-5 h-5 text-yellow-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 17h5l-5 5v-5z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12l2 2 4-4" />
                    </svg>
                  </div>
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-500">Today's Tasks</p>
                  <p className="text-2xl font-semibold text-gray-900">
                    {isLoadingTodaysAlarms ? '...' : todaysAlarms?.length || 0}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="bg-white shadow rounded-lg mb-8">
            <div className="px-6 py-4">
              <h2 className="text-lg font-medium text-gray-900 mb-4">Quick Actions</h2>
              <div className="flex gap-4">
                <button
                  onClick={handleCreateAlarm}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                >
                  <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                  </svg>
                  Create Alarm
                </button>
                <button
                  onClick={handleCreateRoutine}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-green-600 hover:bg-green-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-green-500 transition-colors"
                >
                  <svg className="w-5 h-5 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                  </svg>
                  Create Routine
                </button>
              </div>
            </div>
          </div>
          {/* Main Content Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
            {/* Alarms Section */}
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium text-gray-900">Recent Alarms</h3>
                  <Link
                    to="/alarms"
                    className="text-sm text-blue-600 hover:text-blue-500 font-medium"
                  >
                    View all
                  </Link>
                </div>
              </div>
              <div className="p-6">
                <ErrorBoundary fallback={
                  <div className="text-center py-8">
                    <p className="text-gray-500">Unable to load alarms. Please try refreshing the page.</p>
                  </div>
                }>
                  <AlarmList
                    alarms={activeAlarms || []}
                    isLoading={isLoadingActiveAlarms}
                    maxItems={5}
                  />
                </ErrorBoundary>
              </div>
            </div>

            {/* Routines Section */}
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h3 className="text-lg font-medium text-gray-900">Recent Routines</h3>
                  <Link
                    to="/routines"
                    className="text-sm text-blue-600 hover:text-blue-500 font-medium"
                  >
                    View all
                  </Link>
                </div>
              </div>
              <div className="p-6">
                <ErrorBoundary fallback={
                  <div className="text-center py-8">
                    <p className="text-gray-500">Unable to load routines. Please try refreshing the page.</p>
                  </div>
                }>
                  <RoutineList
                    routines={activeRoutines || []}
                    isLoading={isLoadingActiveRoutines}
                    maxItems={5}
                  />
                </ErrorBoundary>
              </div>
            </div>
          </div>
        </div>
      </main>

      {/* Alarm Form Modal */}
      <AlarmForm
        alarm={editingAlarm}
        isOpen={showAlarmForm}
        onSuccess={handleFormSuccess}
        onCancel={handleFormCancel}
      />

      {/* Routine Form Modal */}
      <RoutineForm
        routine={editingRoutine}
        isOpen={showRoutineForm}
        onSuccess={handleFormSuccess}
        onCancel={handleFormCancel}
      />
    </div>
  );
};

export default Dashboard;
