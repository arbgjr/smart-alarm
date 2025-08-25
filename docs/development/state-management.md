# State Management Architecture - Zustand + React Query

This document outlines the modern state management architecture for Smart Alarm, combining Zustand for client state with React Query for server state synchronization.

## Overview

The Smart Alarm frontend uses a hybrid approach to state management:

- **Zustand**: Client state, UI preferences, authentication, and offline data
- **React Query**: Server state caching, synchronization, and background updates
- **Integration Hooks**: Seamless coordination between both systems

This architecture provides:

- **Offline-First Experience**: Local state with server synchronization
- **Optimistic Updates**: Immediate UI feedback with rollback capability
- **Persistent State**: User preferences and data survive browser restarts
- **Type Safety**: Full TypeScript support across all stores and hooks

## Store Architecture

### Store Organization

The application state is organized into three main Zustand stores:

```typescript
// Store Structure
├── authStore.ts      # Authentication, user data, JWT tokens
├── alarmsStore.ts    # Alarm data, CRUD operations, pagination
└── uiStore.ts        # Theme, language, accessibility, modals
```

### Auth Store (`authStore.ts`)

Manages authentication state with JWT token handling and role-based permissions:

```typescript
interface AuthState {
  // State
  user: User | null;
  token: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;

  // Actions
  setUser: (user: User) => void;
  setTokens: (token: string, refreshToken?: string) => void;
  login: (user: User, token: string, refreshToken?: string) => void;
  logout: () => void;
  
  // Utils
  hasRole: (role: string) => boolean;
  hasPermission: (permission: string) => boolean;
  getTokenExpiry: () => Date | null;
  isTokenExpired: () => boolean;
}
```

#### Key Features

- **Automatic Token Expiry**: Monitors JWT expiration and auto-logout
- **Role-Based Permissions**: Granular permission checking
- **Secure Token Storage**: Persistent storage with selective hydration
- **Token Refresh**: Automatic token renewal before expiration

#### Usage Examples

```typescript
// Authentication actions
const { login, logout, hasRole } = useAuthActions();
const isAuthenticated = useIsAuthenticated();
const currentUser = useCurrentUser();

// Role-based UI
if (hasRole('admin')) {
  return <AdminPanel />;
}

// Token expiry handling
useEffect(() => {
  if (isTokenExpired()) {
    logout();
  }
}, [isTokenExpired, logout]);
```

### Alarms Store (`alarmsStore.ts`)

Comprehensive alarm management with offline support and optimistic updates:

```typescript
interface AlarmsState {
  // State
  alarms: Alarm[];
  selectedAlarm: Alarm | null;
  filters: AlarmFilters;
  isLoading: boolean;
  error: string | null;
  
  // Pagination
  currentPage: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;

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
}
```

#### Optimistic Updates

All CRUD operations implement optimistic updates for immediate user feedback:

```typescript
createAlarm: async (data: AlarmFormData) => {
  const { setLoading, addAlarm } = get();
  
  setLoading(true);
  
  try {
    // 1. Create optimistic alarm immediately
    const optimisticAlarm: Alarm = {
      id: `temp-${Date.now()}`,
      ...data,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    addAlarm(optimisticAlarm); // Immediate UI update

    // 2. Try server sync
    if (navigator.onLine) {
      const realAlarm = await alarmService.createAlarm(data);
      get().updateAlarm(optimisticAlarm.id, realAlarm);
      return realAlarm;
    } else {
      // 3. Queue for background sync
      backgroundSync.addToSyncQueue('create', 'alarm', data);
      return optimisticAlarm;
    }
  } catch (error) {
    // 4. Rollback on failure
    get().removeAlarm(optimisticAlarm.id);
    backgroundSync.addToSyncQueue('create', 'alarm', data);
    throw error;
  } finally {
    setLoading(false);
  }
}
```

#### Usage Examples

```typescript
// Alarm operations
const { createAlarm, editAlarm, toggleAlarm } = useAlarmActions();
const alarms = useAlarms();
const { getActiveAlarms } = useAlarmUtils();

// Create new alarm
const handleCreateAlarm = async (data: AlarmFormData) => {
  try {
    await createAlarm(data);
    toast.success('Alarm created successfully!');
  } catch (error) {
    toast.error('Failed to create alarm');
  }
};

// Get filtered alarms
const activeAlarms = getActiveAlarms();
const todayAlarms = getAlarmsForDay('monday');
```

### UI Store (`uiStore.ts`)

Comprehensive UI state management with theme, accessibility, and user preferences:

```typescript
interface UIState {
  // Theme and appearance
  theme: 'light' | 'dark' | 'system';
  language: 'en' | 'pt' | 'es' | 'fr';
  
  // Layout preferences
  sidebarCollapsed: boolean;
  showWelcomeMessage: boolean;
  
  // User preferences
  preferences: UIPreferences;
  notifications: NotificationSettings;
  accessibility: AccessibilitySettings;
  
  // UI state
  isLoading: boolean;
  modals: {
    createAlarm: boolean;
    editAlarm: boolean;
    settings: boolean;
    confirmDelete: boolean;
  };
  
  // Actions
  setTheme: (theme: Theme) => void;
  setLanguage: (language: Language) => void;
  openModal: (modal: keyof UIState['modals']) => void;
  closeModal: (modal: keyof UIState['modals']) => void;
  setPreferences: (preferences: Partial<UIPreferences>) => void;
  applySystemTheme: () => void;
}
```

#### Theme Management

Advanced theme system with system preference detection:

```typescript
setTheme: (theme) => {
  set((state) => ({ ...state, theme }));
  
  // Apply theme immediately
  if (typeof window !== 'undefined') {
    const root = document.documentElement;
    
    if (theme === 'system') {
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      root.classList.toggle('dark', prefersDark);
    } else {
      root.classList.toggle('dark', theme === 'dark');
    }
  }
}
```

#### Usage Examples

```typescript
// Theme management
const { theme, setTheme, effectiveTheme } = useSystemTheme();
const { preferences, setPreferences } = usePreferences();

// Modal management
const { isOpen, open, close } = useModal('createAlarm');

// Accessibility settings
const accessibility = useAccessibilitySettings();
const { setAccessibility } = useUIActions();
```

## React Query Integration

### Integration Hooks

Custom hooks that combine Zustand with React Query for optimal data synchronization:

```typescript
// useAlarmsIntegration.ts
export function useAlarmsIntegration(params?: AlarmQueryParams) {
  const alarms = useAlarmsStore((state) => state.alarms);
  const { setAlarms, setLoading, setError, setPagination } = useAlarmsStore();

  const query = useQuery({
    queryKey: alarmKeys.list(params),
    queryFn: async () => {
      const response = await alarmService.getAlarms(params);
      
      // Convert DTOs and sync with Zustand store
      const alarms = response.data.map(convertAlarmDtoToAlarm);
      setAlarms(alarms);
      setPagination(response.pageNumber, response.totalPages, response.totalElements);
      
      return response;
    },
    staleTime: 1000 * 60 * 5, // 5 minutes
    onError: (error: any) => setError(error.message),
    onSuccess: () => setError(null)
  });

  // Sync loading state
  useEffect(() => {
    setLoading(query.isLoading);
  }, [query.isLoading, setLoading]);

  return {
    ...query,
    data: query.data ? { ...query.data, data: alarms } : undefined,
    alarms, // Immediate access to Zustand data
  };
}
```

### Mutation Integration

Enhanced mutations that coordinate between React Query cache and Zustand stores:

```typescript
export function useCreateAlarmIntegration() {
  const queryClient = useQueryClient();
  const { createAlarm } = useAlarmsStore();

  return useMutation({
    mutationFn: createAlarm,
    onSuccess: (newAlarm) => {
      // React Query cache updates
      queryClient.invalidateQueries({ queryKey: alarmKeys.lists() });
      queryClient.setQueryData(alarmKeys.detail(newAlarm.id), newAlarm);
      
      // Zustand store is already updated by createAlarm action
    },
    onError: (error) => {
      // Error handling is managed by the store
      console.error('Alarm creation failed:', error);
    }
  });
}
```

### Offline Sync Integration

Coordination between offline changes and React Query cache:

```typescript
export function useOfflineSync() {
  const queryClient = useQueryClient();

  const syncOfflineChanges = () => {
    // Invalidate all queries to refetch from server
    queryClient.invalidateQueries({ queryKey: alarmKeys.all });
  };

  // Listen for online events
  useEffect(() => {
    const handleOnline = () => syncOfflineChanges();
    window.addEventListener('online', handleOnline);
    return () => window.removeEventListener('online', handleOnline);
  }, []);

  return { syncOfflineChanges };
}
```

## Persistence Strategy

### Selective Persistence

Each store implements selective persistence to optimize storage and security:

```typescript
// Auth Store Persistence
persist(
  (set, get) => ({ /* store implementation */ }),
  {
    name: 'smart-alarm-auth',
    storage: createJSONStorage(() => localStorage),
    partialize: (state) => ({
      user: state.user,
      token: state.token,
      refreshToken: state.refreshToken,
      isAuthenticated: state.isAuthenticated,
    }),
    onRehydrateStorage: () => (state) => {
      if (state?.isTokenExpired()) {
        state.logout();
      }
    }
  }
)
```

### Storage Configuration

Different storage strategies for different data types:

```typescript
// Configuration for each store
const storageConfig = {
  authStore: {
    storage: localStorage, // Secure token storage
    partialize: ['user', 'token', 'isAuthenticated'], // Security-sensitive fields only
    encryption: false // Handled by browser security
  },
  alarmsStore: {
    storage: localStorage, // Fast access for alarms
    partialize: ['alarms', 'filters', 'currentPage'], // User data and preferences
    compression: true // Large datasets
  },
  uiStore: {
    storage: localStorage, // UI preferences
    partialize: ['theme', 'language', 'preferences'], // User customization
    sync: true // Cross-tab synchronization
  }
};
```

## Type Safety

### Store Interfaces

All stores are fully typed with comprehensive interfaces:

```typescript
// Type definitions
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

interface AlarmFormData {
  name: string;
  time: string;
  isEnabled?: boolean;
  daysOfWeek?: string[];
  description?: string;
}
```

### Hook Type Safety

Integration hooks maintain full type safety:

```typescript
// Typed hook return values
export function useAlarmsIntegration(params?: AlarmQueryParams): {
  data: PaginatedResponse<Alarm> | undefined;
  alarms: Alarm[];
  isLoading: boolean;
  error: Error | null;
  refetch: () => void;
} {
  // Implementation with full type checking
}
```

## Best Practices

### Store Organization

1. **Single Responsibility**: Each store handles one domain area
2. **Minimal State**: Store only what's necessary, derive everything else
3. **Immutable Updates**: Use Immer-style updates for complex state
4. **Action Grouping**: Group related actions together

### Performance Optimization

1. **Selective Subscriptions**: Use specific selectors to prevent unnecessary re-renders
2. **Computed Values**: Derive computed state rather than storing it
3. **Debounced Updates**: Debounce rapid state changes
4. **Lazy Loading**: Load store slices on demand

### Error Handling

1. **Consistent Error States**: Standardize error handling across stores
2. **Optimistic Rollback**: Always provide rollback for failed operations
3. **User Feedback**: Clear error messages with suggested actions
4. **Graceful Degradation**: Fallback to cached/offline data when possible

### Testing Strategy

1. **Store Unit Tests**: Test store logic in isolation
2. **Integration Tests**: Test store and React Query coordination
3. **Hook Tests**: Test custom hooks with realistic scenarios
4. **E2E Tests**: Test complete user workflows

## Migration Guide

### From Legacy React Query

If migrating from pure React Query hooks:

```typescript
// Before: Pure React Query
const { data: alarms, isLoading } = useQuery({
  queryKey: ['alarms'],
  queryFn: alarmService.getAlarms
});

// After: Integrated approach
const { alarms, isLoading } = useAlarmsIntegration();
// Now alarms are immediately available from Zustand store
// with React Query providing background synchronization
```

### Progressive Migration

1. **Start with one store**: Begin with UI store for immediate benefits
2. **Add integration gradually**: Migrate query hooks one at a time
3. **Maintain compatibility**: Keep existing hooks working during transition
4. **Update components incrementally**: Convert components to new hooks

This state management architecture provides a robust, type-safe, and performant foundation for the Smart Alarm application, ensuring excellent user experience both online and offline.