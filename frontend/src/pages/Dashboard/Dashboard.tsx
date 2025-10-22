import React, { useState } from "react";
import { Link } from "react-router-dom";
import { Clock, Menu, Settings, Repeat } from "lucide-react";
import { useViewport } from "../../utils/responsive";
import { useAuth } from "../../hooks/useAuth";
import { useActiveAlarms, useTodaysAlarms } from "../../hooks/useAlarms";
import { useActiveRoutines } from "../../hooks/useRoutines";
import { useDashboardMetrics } from "../../hooks/useDashboardMetrics";
import { AlarmList } from "../../components/AlarmList";
import { RoutineList } from "../../components/RoutineList";
import { AlarmForm } from "../../components/molecules/AlarmForm";
import { RoutineForm } from "../../components/molecules/RoutineForm";
import { ErrorBoundary } from "../../components/molecules/ErrorBoundary/ErrorBoundary";
import {
  RecentActivity,
  RealTimeStatus,
  EnhancedMetricsGrid,
  EnhancedAlarmChart,
  RealTimeNotifications,
  QuickActionsPanel,
} from "../../components/Dashboard";

interface DashboardProps {}

export const Dashboard: React.FC<DashboardProps> = () => {
  const { user, isLoading } = useAuth();
  const { data: activeAlarms, isLoading: isLoadingActiveAlarms } =
    useActiveAlarms();
  const { data: todaysAlarms, isLoading: isLoadingTodaysAlarms } =
    useTodaysAlarms();
  const { data: activeRoutines, isLoading: isLoadingActiveRoutines } =
    useActiveRoutines();
  const {
    metrics,
    chartData,
    recentActivity,
    isLoading: isLoadingMetrics,
    refreshMetrics,
  } = useDashboardMetrics();
  const viewport = useViewport();

  // Mobile navigation state
  const [showMobileMenu, setShowMobileMenu] = useState(false);

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
            <div className="flex items-center space-x-4">
              <h1
                className={`font-bold text-gray-900 ${
                  viewport.isMobile ? "text-xl" : "text-2xl"
                }`}
              >
                Smart Alarm
              </h1>
            </div>

            <div className="flex items-center space-x-2 sm:space-x-4">
              {/* Mobile menu button */}
              {viewport.isMobile && (
                <button
                  onClick={() => setShowMobileMenu(!showMobileMenu)}
                  className="p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-blue-500"
                  aria-label="Open menu"
                >
                  <Menu className="w-6 h-6" />
                </button>
              )}

              {/* Desktop navigation */}
              {!viewport.isMobile && (
                <>
                  <Link
                    to="/settings"
                    className="p-2 rounded-md text-gray-400 hover:text-gray-500 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-inset focus:ring-blue-500"
                    aria-label="Settings"
                  >
                    <Settings className="w-5 h-5" />
                  </Link>
                  <span className="text-sm text-gray-700 hidden sm:block">
                    Welcome, {user?.name || user?.email || "User"}
                  </span>
                </>
              )}

              <button
                type="button"
                className="bg-white p-2 rounded-full text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                aria-label="User menu"
              >
                <div
                  className={`bg-blue-600 rounded-full flex items-center justify-center ${
                    viewport.isMobile ? "w-8 h-8" : "w-10 h-10"
                  }`}
                >
                  <span className="text-white text-sm font-medium">
                    {(user?.name?.[0] || user?.email?.[0] || "U").toUpperCase()}
                  </span>
                </div>
              </button>
            </div>
          </div>
        </div>

        {/* Mobile menu */}
        {viewport.isMobile && showMobileMenu && (
          <div className="border-t border-gray-200 bg-white">
            <div className="px-4 py-3 space-y-3">
              <div className="text-sm text-gray-700">
                Welcome, {user?.name || user?.email || "User"}
              </div>
              <div className="flex flex-col space-y-2">
                <Link
                  to="/alarms"
                  className="flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md"
                  onClick={() => setShowMobileMenu(false)}
                >
                  <Clock className="w-4 h-4 mr-3" />
                  Alarms
                </Link>
                <Link
                  to="/routines"
                  className="flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md"
                  onClick={() => setShowMobileMenu(false)}
                >
                  <Repeat className="w-4 h-4 mr-3" />
                  Routines
                </Link>
                <Link
                  to="/settings"
                  className="flex items-center px-3 py-2 text-sm text-gray-700 hover:bg-gray-100 rounded-md"
                  onClick={() => setShowMobileMenu(false)}
                >
                  <Settings className="w-4 h-4 mr-3" />
                  Settings
                </Link>
              </div>
            </div>
          </div>
        )}
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

          {/* Enhanced Metrics Grid */}
          <EnhancedMetricsGrid
            metrics={{
              ...metrics,
              activeAlarms: activeAlarms?.length || 0,
              todaysAlarms: todaysAlarms?.length || 0,
              activeRoutines: activeRoutines?.routines?.length || 0,
            }}
            isLoading={
              isLoadingMetrics ||
              isLoadingActiveAlarms ||
              isLoadingTodaysAlarms ||
              isLoadingActiveRoutines
            }
            className="mb-6"
          />

          {/* Quick Actions Panel */}
          <QuickActionsPanel
            onCreateAlarm={handleCreateAlarm}
            onCreateRoutine={handleCreateRoutine}
            onRefreshData={refreshMetrics}
            onImportData={() => console.log("Import data")}
            onExportData={() => console.log("Export data")}
            className="mb-8"
          />
          {/* Charts and Analytics */}
          <div
            className={`grid gap-6 mb-6 ${
              viewport.isMobile ? "grid-cols-1" : "grid-cols-1 lg:grid-cols-3"
            }`}
          >
            {/* Enhanced Alarm Activity Chart */}
            <div className="lg:col-span-2">
              <ErrorBoundary
                fallback={
                  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                    <p className="text-center text-gray-500">
                      Unable to load chart data
                    </p>
                  </div>
                }
              >
                <EnhancedAlarmChart
                  data={chartData}
                  isLoading={isLoadingMetrics}
                />
              </ErrorBoundary>
            </div>

            {/* Real-time Status and Notifications */}
            <div className="space-y-6">
              <ErrorBoundary
                fallback={
                  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                    <p className="text-center text-gray-500">
                      Unable to load status
                    </p>
                  </div>
                }
              >
                <RealTimeStatus />
              </ErrorBoundary>

              <ErrorBoundary
                fallback={
                  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                    <p className="text-center text-gray-500">
                      Unable to load notifications
                    </p>
                  </div>
                }
              >
                <RealTimeNotifications maxNotifications={3} />
              </ErrorBoundary>
            </div>
          </div>

          {/* Main Content Grid */}
          <div
            className={`grid gap-6 ${
              viewport.isMobile ? "grid-cols-1" : "grid-cols-1 lg:grid-cols-3"
            }`}
          >
            {/* Recent Activity */}
            <div className="lg:col-span-1">
              <ErrorBoundary
                fallback={
                  <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
                    <p className="text-center text-gray-500">
                      Unable to load activity
                    </p>
                  </div>
                }
              >
                <RecentActivity
                  activities={recentActivity}
                  isLoading={isLoadingMetrics}
                  maxItems={8}
                />
              </ErrorBoundary>
            </div>

            {/* Alarms and Routines */}
            <div className="lg:col-span-2 grid grid-cols-1 lg:grid-cols-2 gap-8">
              {/* Alarms Section */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200">
                <div className="px-6 py-4 border-b border-gray-200">
                  <div className="flex items-center justify-between">
                    <h3 className="text-lg font-medium text-gray-900">
                      Recent Alarms
                    </h3>
                    <Link
                      to="/alarms"
                      className="text-sm text-blue-600 hover:text-blue-500 font-medium"
                    >
                      View all
                    </Link>
                  </div>
                </div>
                <div className="p-6">
                  <ErrorBoundary
                    fallback={
                      <div className="text-center py-8">
                        <p className="text-gray-500">
                          Unable to load alarms. Please try refreshing the page.
                        </p>
                      </div>
                    }
                  >
                    <AlarmList
                      alarms={activeAlarms || []}
                      isLoading={isLoadingActiveAlarms}
                      maxItems={5}
                    />
                  </ErrorBoundary>
                </div>
              </div>

              {/* Routines Section */}
              <div className="bg-white rounded-lg shadow-sm border border-gray-200">
                <div className="px-6 py-4 border-b border-gray-200">
                  <div className="flex items-center justify-between">
                    <h3 className="text-lg font-medium text-gray-900">
                      Recent Routines
                    </h3>
                    <Link
                      to="/routines"
                      className="text-sm text-blue-600 hover:text-blue-500 font-medium"
                    >
                      View all
                    </Link>
                  </div>
                </div>
                <div className="p-6">
                  <ErrorBoundary
                    fallback={
                      <div className="text-center py-8">
                        <p className="text-gray-500">
                          Unable to load routines. Please try refreshing the
                          page.
                        </p>
                      </div>
                    }
                  >
                    <RoutineList
                      routines={activeRoutines?.routines || []}
                      isLoading={isLoadingActiveRoutines}
                      maxItems={5}
                    />
                  </ErrorBoundary>
                </div>
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
