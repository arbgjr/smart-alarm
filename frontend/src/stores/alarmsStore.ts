import { create } from 'zustand';
import { persist, createJSONStorage } from 'zustand/middleware';
import { backgroundSync } from '@/utils/backgroundSync';
import { mlDataCollector } from '@/utils/mlDataCollector';
import type { AlarmDto } from '@/services/alarmService';

interface Alarm {
  id: string;
  name: string;
  triggerTime: string;
  time: string; // For compatibility
  isEnabled: boolean;
  isRecurring: boolean;
  recurrencePattern?: string;
  daysOfWeek: string[];
  description?: string;
  userId: string;
  createdAt: string;
  updatedAt: string;
}

interface AlarmFilters {
  enabled?: boolean;
  search?: string;
  dayOfWeek?: string;
}

interface AlarmFormData {
  name: string;
  time: string;
  isEnabled?: boolean;
  daysOfWeek?: string[];
  description?: string;
}

// Convert AlarmDto to Alarm interface
const convertAlarmDtoToAlarm = (dto: AlarmDto): Alarm => ({
  ...dto,
  time: dto.triggerTime,
  daysOfWeek: dto.recurrencePattern ? dto.recurrencePattern.split(',') : [],
});

interface AlarmsState {
  // State
  alarms: Alarm[];
  selectedAlarm: Alarm | null;
  filters: AlarmFilters;
  isLoading: boolean;
  error: string | null;
  lastSync: string | null;
  
  // Pagination
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;

  // Actions
  setAlarms: (alarms: Alarm[]) => void;
  addAlarm: (alarm: Alarm) => void;
  updateAlarm: (id: string, updates: Partial<Alarm>) => void;
  removeAlarm: (id: string) => void;
  setSelectedAlarm: (alarm: Alarm | null) => void;
  setFilters: (filters: AlarmFilters) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
  setPagination: (page: number, totalPages: number, totalCount: number) => void;
  setLastSync: (timestamp: string) => void;
  
  // CRUD Operations (with offline support)
  createAlarm: (data: AlarmFormData) => Promise<Alarm>;
  editAlarm: (id: string, data: Partial<AlarmFormData>) => Promise<Alarm>;
  deleteAlarm: (id: string) => Promise<void>;
  toggleAlarm: (id: string) => Promise<void>;
  
  // Bulk operations
  enableMultipleAlarms: (ids: string[]) => Promise<void>;
  disableMultipleAlarms: (ids: string[]) => Promise<void>;
  deleteMultipleAlarms: (ids: string[]) => Promise<void>;
  
  // Utility functions
  getAlarmById: (id: string) => Alarm | undefined;
  getActiveAlarms: () => Alarm[];
  getAlarmsForDay: (dayOfWeek: string) => Alarm[];
  getUpcomingAlarms: (hours?: number) => Alarm[];
  clearAll: () => void;
}

export const useAlarmsStore = create<AlarmsState>()(
  persist(
    (set, get) => ({
      // Initial state
      alarms: [],
      selectedAlarm: null,
      filters: {},
      isLoading: false,
      error: null,
      lastSync: null,
      currentPage: 1,
      totalPages: 1,
      totalCount: 0,
      pageSize: 10,

      // Basic setters
      setAlarms: (alarms) =>
        set((state) => ({
          ...state,
          alarms,
          lastSync: new Date().toISOString(),
        })),

      addAlarm: (alarm) =>
        set((state) => ({
          ...state,
          alarms: [alarm, ...state.alarms],
          totalCount: state.totalCount + 1,
        })),

      updateAlarm: (id, updates) =>
        set((state) => ({
          ...state,
          alarms: state.alarms.map((alarm) =>
            alarm.id === id ? { ...alarm, ...updates } : alarm
          ),
          selectedAlarm:
            state.selectedAlarm?.id === id
              ? { ...state.selectedAlarm, ...updates }
              : state.selectedAlarm,
        })),

      removeAlarm: (id) =>
        set((state) => ({
          ...state,
          alarms: state.alarms.filter((alarm) => alarm.id !== id),
          selectedAlarm:
            state.selectedAlarm?.id === id ? null : state.selectedAlarm,
          totalCount: Math.max(0, state.totalCount - 1),
        })),

      setSelectedAlarm: (alarm) =>
        set((state) => ({
          ...state,
          selectedAlarm: alarm,
        })),

      setFilters: (filters) =>
        set((state) => ({
          ...state,
          filters: { ...state.filters, ...filters },
          currentPage: 1, // Reset to first page when filters change
        })),

      setLoading: (loading) =>
        set((state) => ({
          ...state,
          isLoading: loading,
        })),

      setError: (error) =>
        set((state) => ({
          ...state,
          error,
          isLoading: false,
        })),

      setPagination: (currentPage, totalPages, totalCount) =>
        set((state) => ({
          ...state,
          currentPage,
          totalPages,
          totalCount,
        })),

      setLastSync: (timestamp) =>
        set((state) => ({
          ...state,
          lastSync: timestamp,
        })),

      // CRUD Operations with offline support
      createAlarm: async (data) => {
        const { setLoading, setError, addAlarm } = get();
        
        setLoading(true);
        setError(null);

        try {
          // Create optimistic alarm
          const optimisticAlarm: Alarm = {
            id: `temp-${Date.now()}`,
            name: data.name,
            triggerTime: data.time,
            time: data.time,
            isEnabled: data.isEnabled ?? true,
            isRecurring: data.daysOfWeek ? data.daysOfWeek.length > 0 : false,
            recurrencePattern: data.daysOfWeek?.join(','),
            daysOfWeek: data.daysOfWeek ?? [],
            description: data.description,
            userId: 'current-user', // Will be set by backend
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
          };

          // Add optimistically
          addAlarm(optimisticAlarm);

          // Track alarm creation for ML
          mlDataCollector.trackAlarmCreated(
            optimisticAlarm.id,
            data.time,
            {
              dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
              deviceType: /Mobile|Android|iPhone|iPad/.test(navigator.userAgent) ? 'mobile' : 'desktop',
            }
          );

          if (navigator.onLine) {
            // Try online creation
            const { alarmService } = await import('@/services/alarmService');
            const alarmData = {
              name: data.name,
              description: data.description,
              triggerTime: data.time,
              isRecurring: data.daysOfWeek ? data.daysOfWeek.length > 0 : false,
              recurrencePattern: data.daysOfWeek?.join(','),
            };
            const realAlarmDto = await alarmService.createAlarm(alarmData);
            const realAlarm = convertAlarmDtoToAlarm(realAlarmDto);
            
            // Replace optimistic alarm with real one
            get().updateAlarm(optimisticAlarm.id, realAlarm);
            
            setLoading(false);
            return realAlarm;
          } else {
            // Queue for background sync
            backgroundSync.addToSyncQueue('create', 'alarm', data);
            setLoading(false);
            return optimisticAlarm;
          }
        } catch (error: any) {
          console.error('Error creating alarm:', error);
          
          // Remove optimistic alarm on error
          get().removeAlarm(data.name); // fallback identification
          
          // Queue for background sync as fallback
          backgroundSync.addToSyncQueue('create', 'alarm', data);
          
          setError(error.message || 'Failed to create alarm');
          throw error;
        }
      },

      editAlarm: async (id, data) => {
        const { setLoading, setError, updateAlarm, getAlarmById } = get();
        
        const existingAlarm = getAlarmById(id);
        if (!existingAlarm) {
          throw new Error('Alarm not found');
        }

        setLoading(true);
        setError(null);

        // Store original alarm for rollback
        const originalAlarm = { ...existingAlarm };

        try {
          // Update optimistically
          updateAlarm(id, { ...data, updatedAt: new Date().toISOString() });

          if (navigator.onLine) {
            const { alarmService } = await import('@/services/alarmService');
            const updatedAlarmDto = await alarmService.updateAlarm(id, data);
            const updatedAlarm = convertAlarmDtoToAlarm(updatedAlarmDto);
            
            updateAlarm(id, updatedAlarm);
            setLoading(false);
            return updatedAlarm;
          } else {
            // Queue for background sync
            backgroundSync.addToSyncQueue('update', 'alarm', { id, ...data });
            setLoading(false);
            return { ...existingAlarm, ...data };
          }
        } catch (error: any) {
          console.error('Error updating alarm:', error);
          
          // Rollback optimistic update
          updateAlarm(id, originalAlarm);
          
          // Queue for background sync as fallback
          backgroundSync.addToSyncQueue('update', 'alarm', { id, ...data });
          
          setError(error.message || 'Failed to update alarm');
          throw error;
        }
      },

      deleteAlarm: async (id) => {
        const { setLoading, setError, removeAlarm, getAlarmById } = get();
        
        const alarm = getAlarmById(id);
        if (!alarm) return;

        setLoading(true);
        setError(null);

        try {
          // Remove optimistically
          removeAlarm(id);

          if (navigator.onLine) {
            const { alarmService } = await import('@/services/alarmService');
            await alarmService.deleteAlarm(id);
            setLoading(false);
          } else {
            // Queue for background sync
            backgroundSync.addToSyncQueue('delete', 'alarm', { id });
            setLoading(false);
          }
        } catch (error: any) {
          console.error('Error deleting alarm:', error);
          
          // Restore alarm on error
          get().addAlarm(alarm);
          
          // Queue for background sync as fallback
          backgroundSync.addToSyncQueue('delete', 'alarm', { id });
          
          setError(error.message || 'Failed to delete alarm');
          throw error;
        }
      },

      toggleAlarm: async (id) => {
        const alarm = get().getAlarmById(id);
        if (!alarm) return;

        await get().editAlarm(id, { isEnabled: !alarm.isEnabled });
      },

      // Bulk operations
      enableMultipleAlarms: async (ids) => {
        await Promise.allSettled(
          ids.map((id) => get().editAlarm(id, { isEnabled: true }))
        );
      },

      disableMultipleAlarms: async (ids) => {
        await Promise.allSettled(
          ids.map((id) => get().editAlarm(id, { isEnabled: false }))
        );
      },

      deleteMultipleAlarms: async (ids) => {
        await Promise.allSettled(
          ids.map((id) => get().deleteAlarm(id))
        );
      },

      // Utility functions
      getAlarmById: (id) => {
        return get().alarms.find((alarm) => alarm.id === id);
      },

      getActiveAlarms: () => {
        return get().alarms.filter((alarm) => alarm.isEnabled);
      },

      getAlarmsForDay: (dayOfWeek) => {
        return get().alarms.filter((alarm) =>
          alarm.isEnabled && alarm.daysOfWeek.includes(dayOfWeek)
        );
      },

      getUpcomingAlarms: (hours = 24) => {
        const now = new Date();
        const upcoming = new Date(now.getTime() + hours * 60 * 60 * 1000);
        
        return get().alarms.filter((alarm) => {
          if (!alarm.isEnabled) return false;
          
          // Simple time comparison (would need more sophisticated logic for recurring alarms)
          const [alarmHours, alarmMinutes] = alarm.time.split(':').map(Number);
          const alarmDate = new Date(now);
          alarmDate.setHours(alarmHours, alarmMinutes, 0, 0);
          
          return alarmDate >= now && alarmDate <= upcoming;
        });
      },

      clearAll: () =>
        set(() => ({
          alarms: [],
          selectedAlarm: null,
          filters: {},
          isLoading: false,
          error: null,
          currentPage: 1,
          totalPages: 1,
          totalCount: 0,
        })),
    }),
    {
      name: 'smart-alarm-alarms',
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({
        alarms: state.alarms,
        filters: state.filters,
        lastSync: state.lastSync,
        currentPage: state.currentPage,
        pageSize: state.pageSize,
      }),
    }
  )
);

// Selective hooks to prevent unnecessary re-renders
export const useAlarms = () => useAlarmsStore((state) => state.alarms);
export const useSelectedAlarm = () => useAlarmsStore((state) => state.selectedAlarm);
export const useAlarmFilters = () => useAlarmsStore((state) => state.filters);
export const useAlarmLoading = () => useAlarmsStore((state) => state.isLoading);
export const useAlarmError = () => useAlarmsStore((state) => state.error);
export const useAlarmPagination = () =>
  useAlarmsStore((state) => ({
    currentPage: state.currentPage,
    totalPages: state.totalPages,
    totalCount: state.totalCount,
    pageSize: state.pageSize,
  }));

// Action hooks
export const useAlarmActions = () =>
  useAlarmsStore((state) => ({
    createAlarm: state.createAlarm,
    editAlarm: state.editAlarm,
    deleteAlarm: state.deleteAlarm,
    toggleAlarm: state.toggleAlarm,
    setSelectedAlarm: state.setSelectedAlarm,
    setFilters: state.setFilters,
    enableMultipleAlarms: state.enableMultipleAlarms,
    disableMultipleAlarms: state.disableMultipleAlarms,
    deleteMultipleAlarms: state.deleteMultipleAlarms,
  }));

// Utility hooks
export const useAlarmUtils = () =>
  useAlarmsStore((state) => ({
    getAlarmById: state.getAlarmById,
    getActiveAlarms: state.getActiveAlarms,
    getAlarmsForDay: state.getAlarmsForDay,
    getUpcomingAlarms: state.getUpcomingAlarms,
  }));