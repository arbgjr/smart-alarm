import React, { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { Clock, Plus, Settings, Calendar, AlertTriangle, ArrowLeft, Grid, List } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useAlarms } from '../../hooks/useAlarms';
import { AlarmList } from '../../components/AlarmList';
import { AlarmForm } from '../../components/molecules/AlarmForm';
import { ErrorBoundary } from '../../components/molecules/ErrorBoundary/ErrorBoundary';
import { AlarmFilters } from '../../components/Alarms/AlarmFilters';
import { AlarmCard } from '../../components/Alarms/AlarmCard';
import { RoutineConfigModal } from '../../components/Alarms/RoutineConfigModal';
import { ExceptionPeriodModal } from '../../components/Alarms/ExceptionPeriodModal';
import { HolidayConfigModal } from '../../components/Alarms/HolidayConfigModal';
import { AlarmDto } from '../../services/alarmService';

interface AlarmsPageProps {}

export const AlarmsPage: React.FC<AlarmsPageProps> = () => {
  const { user } = useAuth();
  const { data: alarms, isLoading } = useAlarms();

  // View state
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');

  // Filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'active' | 'inactive'>('all');
  const [typeFilter, setTypeFilter] = useState<'all' | 'one-time' | 'recurring'>('all');
  const [sortBy, setSortBy] = useState<'time' | 'name' | 'created' | 'status'>('time');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('asc');

  // Modal state management
  const [showAlarmForm, setShowAlarmForm] = useState(false);
  const [editingAlarm, setEditingAlarm] = useState<AlarmDto | null>(null);
  const [showRoutineModal, setShowRoutineModal] = useState(false);
  const [showExceptionModal, setShowExceptionModal] = useState(false);
  const [showHolidayModal, setShowHolidayModal] = useState(false);

  // Filtered and sorted alarms
  const filteredAlarms = useMemo(() => {
    if (!alarms?.data) return [];

    let filtered = alarms.data.filter((alarm: any) => {
      // Search filter
      if (searchTerm && !alarm.name.toLowerCase().includes(searchTerm.toLowerCase())) {
        return false;
      }

      // Status filter
      if (statusFilter !== 'all') {
        if (statusFilter === 'active' && !alarm.isActive) return false;
        if (statusFilter === 'inactive' && alarm.isActive) return false;
      }

      // Type filter
      if (typeFilter !== 'all') {
        if (typeFilter === 'one-time' && alarm.isRecurring) return false;
        if (typeFilter === 'recurring' && !alarm.isRecurring) return false;
      }

      return true;
    });

    // Sort
    filtered.sort((a: any, b: any) => {
      let aValue, bValue;

      switch (sortBy) {
        case 'name':
          aValue = a.name.toLowerCase();
          bValue = b.name.toLowerCase();
          break;
        case 'created':
          aValue = new Date(a.createdAt).getTime();
          bValue = new Date(b.createdAt).getTime();
          break;
        case 'status':
          aValue = a.isActive ? 1 : 0;
          bValue = b.isActive ? 1 : 0;
          break;
        case 'time':
        default:
          aValue = a.time;
          bValue = b.time;
          break;
      }

      if (sortOrder === 'desc') {
        return aValue < bValue ? 1 : -1;
      }
      return aValue > bValue ? 1 : -1;
    });

    return filtered;
  }, [alarms?.data, searchTerm, statusFilter, typeFilter, sortBy, sortOrder]);

  const handleCreateAlarm = () => {
    setEditingAlarm(null);
    setShowAlarmForm(true);
  };

  const handleEditAlarm = (alarm: any) => {
    setEditingAlarm(alarm);
    setShowAlarmForm(true);
  };

  const handleDuplicateAlarm = (alarm: any) => {
    const duplicated = { ...alarm, name: `${alarm.name} (Copy)`, id: undefined };
    setEditingAlarm(duplicated);
    setShowAlarmForm(true);
  };

  const handleDeleteAlarm = async (alarmId: string) => {
    if (window.confirm('Are you sure you want to delete this alarm?')) {
      try {
        // TODO: Implement delete API call
        console.log('Deleting alarm:', alarmId);
      } catch (error) {
        console.error('Failed to delete alarm:', error);
      }
    }
  };

  const handleToggleAlarm = async (alarmId: string, isActive: boolean) => {
    try {
      // TODO: Implement toggle API call
      console.log('Toggling alarm:', alarmId, isActive);
    } catch (error) {
      console.error('Failed to toggle alarm:', error);
    }
  };

  const handleFormSuccess = () => {
    setShowAlarmForm(false);
    setEditingAlarm(null);
  };

  const handleFormCancel = () => {
    setShowAlarmForm(false);
    setEditingAlarm(null);
  };

  const handleRoutineSave = (routine: any) => {
    console.log('Saving routine:', routine);
    setShowRoutineModal(false);
  };

  const handleExceptionSave = (exception: any) => {
    console.log('Saving exception:', exception);
    setShowExceptionModal(false);
  };

  const handleHolidaySave = (holidays: any[], preferences: any) => {
    console.log('Saving holidays:', holidays, preferences);
    setShowHolidayModal(false);
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
              <h1 className="text-2xl font-bold text-gray-900">Alarm Management</h1>
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
          {/* Page Header with Actions */}
          <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between mb-6">
            <div className="mb-4 lg:mb-0">
              <h2 className="text-xl font-semibold text-gray-900">
                All Alarms ({filteredAlarms.length})
              </h2>
              <p className="text-gray-600 mt-1">
                Manage your alarms, routines, and scheduling preferences
              </p>
            </div>

            <div className="flex flex-wrap items-center gap-3">
              {/* View Mode Toggle */}
              <div className="flex items-center bg-gray-100 rounded-lg p-1">
                <button
                  onClick={() => setViewMode('grid')}
                  className={`p-2 rounded-md transition-colors ${
                    viewMode === 'grid'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                  title="Grid view"
                >
                  <Grid className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setViewMode('list')}
                  className={`p-2 rounded-md transition-colors ${
                    viewMode === 'list'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                  title="List view"
                >
                  <List className="w-4 h-4" />
                </button>
              </div>

              {/* Action Buttons */}
              <button
                onClick={() => setShowRoutineModal(true)}
                className="inline-flex items-center px-3 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
              >
                <Calendar className="w-4 h-4 mr-2" />
                Routines
              </button>

              <button
                onClick={() => setShowExceptionModal(true)}
                className="inline-flex items-center px-3 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
              >
                <AlertTriangle className="w-4 h-4 mr-2" />
                Exceptions
              </button>

              <button
                onClick={() => setShowHolidayModal(true)}
                className="inline-flex items-center px-3 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
              >
                <Settings className="w-4 h-4 mr-2" />
                Holidays
              </button>

              <button
                onClick={handleCreateAlarm}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
              >
                <Plus className="w-4 h-4 mr-2" />
                Create Alarm
              </button>
            </div>
          </div>

          {/* Filters */}
          <AlarmFilters
            searchTerm={searchTerm}
            onSearchChange={setSearchTerm}
            statusFilter={statusFilter}
            onStatusFilterChange={setStatusFilter}
            typeFilter={typeFilter}
            onTypeFilterChange={setTypeFilter}
            sortBy={sortBy}
            onSortChange={setSortBy}
            sortOrder={sortOrder}
            onSortOrderChange={setSortOrder}
          />

          {/* Alarms Display */}
          <ErrorBoundary fallback={
            <div className="text-center py-8">
              <p className="text-gray-500">Unable to load alarms. Please try refreshing the page.</p>
            </div>
          }>
            {isLoading ? (
              <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                <span className="ml-3 text-gray-600">Loading alarms...</span>
              </div>
            ) : filteredAlarms.length === 0 ? (
              <div className="text-center py-12">
                <Clock className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No alarms found</h3>
                <p className="text-gray-600 mb-6">
                  {searchTerm || statusFilter !== 'all' || typeFilter !== 'all'
                    ? 'Try adjusting your filters or search terms.'
                    : 'Get started by creating your first alarm.'}
                </p>
                {(!searchTerm && statusFilter === 'all' && typeFilter === 'all') && (
                  <button
                    onClick={handleCreateAlarm}
                    className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                  >
                    <Plus className="w-4 h-4 mr-2" />
                    Create Your First Alarm
                  </button>
                )}
              </div>
            ) : (
              <div className={
                viewMode === 'grid'
                  ? 'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6'
                  : 'space-y-4'
              }>
                {viewMode === 'grid' ? (
                  filteredAlarms.map((alarm: any) => (
                    <AlarmCard
                      key={alarm.id}
                      alarm={alarm}
                      onEdit={handleEditAlarm}
                      onDelete={handleDeleteAlarm}
                      onToggle={handleToggleAlarm}
                      onDuplicate={handleDuplicateAlarm}
                    />
                  ))
                ) : (
                  <div className="bg-white shadow-sm rounded-lg border border-gray-200">
                    <div className="p-6">
                      <AlarmList
                        alarms={filteredAlarms}
                        isLoading={false}
                        showActions={true}
                      />
                    </div>
                  </div>
                )}
              </div>
            )}
          </ErrorBoundary>
        </div>
      </main>

      {/* Modals */}
      <AlarmForm
        alarm={editingAlarm || undefined}
        isOpen={showAlarmForm}
        onSuccess={handleFormSuccess}
        onCancel={handleFormCancel}
      />

      <RoutineConfigModal
        isOpen={showRoutineModal}
        onSave={handleRoutineSave}
        onCancel={() => setShowRoutineModal(false)}
      />

      <ExceptionPeriodModal
        isOpen={showExceptionModal}
        alarms={filteredAlarms}
        onSave={handleExceptionSave}
        onCancel={() => setShowExceptionModal(false)}
      />

      <HolidayConfigModal
        isOpen={showHolidayModal}
        holidays={[]}
        preferences={{ disableAlarms: true }}
        alarms={filteredAlarms}
        onSave={handleHolidaySave}
        onCancel={() => setShowHolidayModal(false)}
      />
    </div>
  );
};
