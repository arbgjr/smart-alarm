// Legacy React Query hooks (still available for migration)
export * from './useAlarms';
export * from './useAuth';
export * from './useRoutines';

// New integrated hooks (Zustand + React Query)
export * from './useAlarmsIntegration';
export * from './useAuthIntegration';

// Re-export store hooks for convenience
export {
  // Auth store hooks
  useAuthStore,
  useAuthActions,
  useAuthUtils,
  useIsAuthenticated,
  useCurrentUser,
  useAuthToken,
  useAuthLoading,
  useAuthError,
} from '@/stores/authStore';

export {
  // Alarms store hooks
  useAlarmsStore,
  useAlarms as useAlarmsState,
  useSelectedAlarm,
  useAlarmFilters,
  useAlarmLoading,
  useAlarmError,
  useAlarmPagination,
  useAlarmActions,
  useAlarmUtils,
} from '@/stores/alarmsStore';

export {
  // UI store hooks
  useUIStore,
  useTheme,
  useLanguage,
  useSidebarState,
  usePreferences,
  useNotificationSettings,
  useAccessibilitySettings,
  useUILoading,
  useModal,
  useModals,
  useUIActions,
  useSystemTheme,
} from '@/stores/uiStore';