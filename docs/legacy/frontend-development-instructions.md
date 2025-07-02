# Frontend Development Instructions - Accessibility-First React Components

## Component Architecture Philosophy

When building components for neurodivergent users, you need to think differently about interaction patterns and cognitive load. Every component should reduce mental effort rather than requiring users to learn complex interfaces. This means favoring predictable behaviors, clear visual hierarchies, and forgiving interaction patterns that work well even when attention or executive function is compromised.

## Core Component Patterns

### Alarm Form Component

Create forms that break complex alarm creation into digestible steps. Neurodivergent users often benefit from progressive disclosure rather than overwhelming single-page forms:

```typescript
// src/components/forms/AlarmForm.tsx
interface AlarmFormProps {
  initialData?: Partial<Alarm>;
  onSubmit: (alarm: Alarm) => void;
  onCancel: () => void;
}

export const AlarmForm: React.FC<AlarmFormProps> = ({
  initialData,
  onSubmit,
  onCancel
}) => {
  // Use react-hook-form for validation with immediate feedback
  // Implement step-by-step wizard for complex configurations
  // Provide clear save/cancel actions that are hard to miss
  // Include preview of alarm before final submission
};
```

The form should guide users through alarm creation without overwhelming them with choices. Start with essential information (time, title) and progressively reveal advanced options only when requested. Each step should provide clear feedback about what information is required and why it matters.

### Calendar Display Component

The calendar component needs to balance information density with clarity. Neurodivergent users often struggle with cluttered interfaces but also need comprehensive views of their schedules:

```typescript
// src/components/calendar/CalendarView.tsx
interface CalendarViewProps {
  alarms: Alarm[];
  view: 'month' | 'week' | 'day';
  onAlarmClick: (alarm: Alarm) => void;
  onDateClick: (date: Date) => void;
  accessibilitySettings: AccessibilitySettings;
}

export const CalendarView: React.FC<CalendarViewProps> = ({
  alarms,
  view,
  onAlarmClick,
  onDateClick,
  accessibilitySettings
}) => {
  // Implement React Big Calendar with accessibility enhancements
  // Color coding that works for colorblind users
  // Clear focus indicators for keyboard navigation
  // Reduced motion animations when requested
  // Screen reader friendly alarm descriptions
};
```

The calendar should provide multiple ways to navigate and view information. Some users work better with list views, others with traditional calendar grids. The interface should remember user preferences and default to their preferred view mode.

### Notification Management Component

Notifications require special consideration because neurodivergent users may have complex relationships with interruptions. Some need frequent reminders, others find them overwhelming:

```typescript
// src/components/notifications/NotificationSettings.tsx
interface NotificationSettingsProps {
  settings: NotificationSettings;
  onChange: (settings: NotificationSettings) => void;
  testNotification: () => void;
}

export const NotificationSettings: React.FC<NotificationSettingsProps> = ({
  settings,
  onChange,
  testNotification
}) => {
  // Granular control over notification types and timing
  // Test functionality to preview notifications
  // Quiet hours and context-aware settings
  // Multiple notification channels (visual, audio, vibration)
};
```

## Accessibility Implementation Guidelines

### Keyboard Navigation

Every interactive element must be accessible via keyboard. This is particularly important for users with motor differences or those who find mouse interaction challenging:

```typescript
// Example of comprehensive keyboard support
const handleKeyDown = (event: KeyboardEvent) => {
  switch (event.key) {
    case 'Enter':
    case ' ':
      // Activate the current element
      event.preventDefault();
      handleActivation();
      break;
    case 'Escape':
      // Cancel current operation or close modal
      handleCancel();
      break;
    case 'ArrowUp':
    case 'ArrowDown':
      // Navigate between items
      event.preventDefault();
      handleNavigation(event.key);
      break;
  }
};
```

### Screen Reader Support

Implement ARIA labels and descriptions that provide context beyond what visual users see. This helps users who rely on screen readers understand the purpose and state of interface elements:

```typescript
// Example of rich ARIA implementation
<button
  aria-label={`Edit alarm for ${alarm.title} at ${formatTime(alarm.datetime)}`}
  aria-describedby={`alarm-${alarm.id}-description`}
  aria-pressed={isEditing}
  onClick={handleEdit}
>
  <PencilIcon aria-hidden="true" />
  <span className="sr-only">Edit alarm</span>
</button>

<div 
  id={`alarm-${alarm.id}-description`}
  className="sr-only"
>
  {alarm.description || 'No additional description provided'}
</div>
```

### Visual Design Considerations

Colors should never be the only way to convey information. Use combinations of color, shape, text, and icons to ensure information is accessible to colorblind users and those with visual processing differences:

```css
/* Example of multi-modal information design */
.alarm-priority-high {
  @apply border-l-4 border-red-500 bg-red-50;
}

.alarm-priority-high::before {
  content: "⚠️";
  @apply mr-2;
}

.alarm-priority-high .priority-text {
  @apply font-semibold text-red-800;
}
```

## State Management Patterns

### Custom Hooks for Alarm Management

Create hooks that encapsulate complex alarm logic while providing simple interfaces for components:

```typescript
// src/hooks/useAlarms.ts
export const useAlarms = () => {
  const [alarms, setAlarms] = useState<Alarm[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const createAlarm = useCallback(async (alarmData: CreateAlarmData) => {
    // Implement optimistic updates for responsive UI
    // Handle offline scenarios gracefully
    // Provide clear error messages for validation failures
    // Auto-save drafts to prevent data loss
  }, []);

  const updateAlarm = useCallback(async (id: string, updates: Partial<Alarm>) => {
    // Similar patterns for update operations
    // Conflict resolution for concurrent edits
    // Undo functionality for accidental changes
  }, []);

  return {
    alarms,
    loading,
    error,
    createAlarm,
    updateAlarm,
    deleteAlarm,
    toggleAlarm
  };
};
```

### Offline State Management

Implement state synchronization that works seamlessly whether online or offline. Users should never lose data or functionality due to connectivity issues:

```typescript
// src/hooks/useOfflineSync.ts
export const useOfflineSync = () => {
  const [isOnline, setIsOnline] = useState(navigator.onLine);
  const [pendingSync, setPendingSync] = useState<SyncOperation[]>([]);

  useEffect(() => {
    // Monitor online/offline status
    // Queue operations when offline
    // Sync when connection restored
    // Resolve conflicts using CRDT principles
  }, []);

  return {
    isOnline,
    pendingSync,
    forcSync: () => {}, // Manual sync trigger
    conflictResolution: () => {} // Handle sync conflicts
  };
};
```

## Form Handling Best Practices

### Progressive Enhancement

Build forms that work at multiple levels of complexity. Start with basic functionality that works everywhere, then enhance with advanced features when supported:

```typescript
// Basic form that works without JavaScript
<form onSubmit={handleSubmit} noValidate>
  <fieldset>
    <legend>Alarm Details</legend>
    
    <label htmlFor="alarm-title">
      Alarm Title (required)
      <input
        id="alarm-title"
        type="text"
        required
        aria-describedby="title-help"
        {...register('title', { required: 'Please enter a title for your alarm' })}
      />
    </label>
    
    <div id="title-help" className="form-help">
      Choose a title that will help you quickly understand what this alarm is for
    </div>
    
    {errors.title && (
      <div role="alert" className="error-message">
        {errors.title.message}
      </div>
    )}
  </fieldset>
</form>
```

### Validation and Error Handling

Provide immediate, helpful feedback that guides users toward successful completion rather than just pointing out errors:

```typescript
const validationSchema = z.object({
  title: z.string()
    .min(1, 'Please enter a title for your alarm')
    .max(100, 'Titles should be under 100 characters for clarity'),
  
  datetime: z.date()
    .min(new Date(), 'Alarms must be set for future times')
    .refine(
      (date) => !isPastTime(date),
      'This time has already passed today. Did you mean tomorrow?'
    ),
  
  recurrence: z.object({
    pattern: z.enum(['none', 'daily', 'weekly', 'custom']),
    customDays: z.array(z.number()).optional()
  }).refine(
    (data) => {
      if (data.pattern === 'custom') {
        return data.customDays && data.customDays.length > 0;
      }
      return true;
    },
    'Please select which days for custom recurrence'
  )
});
```

## Performance Optimization

### Component Optimization

Use React's optimization features thoughtfully to prevent unnecessary re-renders while maintaining accessibility:

```typescript
// Memoize expensive calendar calculations
const calendarEvents = useMemo(() => {
  return alarms.map(alarm => ({
    id: alarm.id,
    title: alarm.title,
    start: alarm.datetime,
    end: new Date(alarm.datetime.getTime() + 30 * 60 * 1000), // 30 min default
    resource: alarm
  }));
}, [alarms]);

// Debounce search input to reduce API calls
const debouncedSearch = useMemo(
  () => debounce((searchTerm: string) => {
    // Perform search operation
  }, 300),
  []
);
```

### Lazy Loading and Code Splitting

Load components and functionality progressively to keep initial bundle size small:

```typescript
// Lazy load complex components
const CalendarView = lazy(() => import('./components/calendar/CalendarView'));
const SettingsPanel = lazy(() => import('./components/settings/SettingsPanel'));

// Use Suspense with meaningful loading states
<Suspense fallback={<CalendarSkeleton />}>
  <CalendarView {...calendarProps} />
</Suspense>
```

## Testing Considerations

### Accessibility Testing

Test components with actual assistive technologies, not just automated tools:

```typescript
// Example of accessibility-focused test
describe('AlarmForm', () => {
  it('should be navigable with keyboard only', async () => {
    render(<AlarmForm onSubmit={mockSubmit} onCancel={mockCancel} />);
    
    // Test tab order
    userEvent.tab();
    expect(screen.getByLabelText('Alarm Title')).toHaveFocus();
    
    userEvent.tab();
    expect(screen.getByLabelText('Date and Time')).toHaveFocus();
    
    // Test form submission with Enter key
    userEvent.type(screen.getByLabelText('Alarm Title'), 'Test Alarm');
    userEvent.keyboard('{Enter}');
    
    expect(mockSubmit).toHaveBeenCalled();
  });

  it('should provide clear error messages', async () => {
    render(<AlarmForm onSubmit={mockSubmit} onCancel={mockCancel} />);
    
    userEvent.click(screen.getByRole('button', { name: /save alarm/i }));
    
    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent('Please enter a title for your alarm');
    });
  });
});
```

## Integration with Backend Services

### Error Handling

Implement graceful degradation when backend services are unavailable:

```typescript
const useAlarmAPI = () => {
  const createAlarm = async (alarmData: CreateAlarmData) => {
    try {
      // Optimistic update for immediate feedback
      const tempAlarm = { ...alarmData, id: generateTempId() };
      updateLocalAlarms(prev => [...prev, tempAlarm]);

      // Attempt to sync with server
      const serverAlarm = await api.createAlarm(alarmData);
      
      // Replace temp alarm with server response
      updateLocalAlarms(prev => 
        prev.map(alarm => 
          alarm.id === tempAlarm.id ? serverAlarm : alarm
        )
      );
      
    } catch (error) {
      // Revert optimistic update
      updateLocalAlarms(prev => 
        prev.filter(alarm => alarm.id !== tempAlarm.id)
      );
      
      // Queue for retry when connection restored
      queueForSync({ type: 'create', data: alarmData });
      
      // Show user-friendly error message
      showNotification({
        type: 'warning',
        message: 'Alarm saved locally. Will sync when connection is restored.',
        action: { label: 'Retry now', handler: () => retrySync() }
      });
    }
  };
};
```

Remember that the frontend is the primary interface between your users and their critical alarm needs. Every interaction should feel supportive and reduce cognitive load rather than adding complexity to their daily routines. The goal is to make time management feel easier and more reliable, not to showcase complex technical capabilities.

## Integration with Multi-Service Backend

Configure the frontend to communicate efficiently with your multi-language backend architecture:

```typescript
// src/services/apiService.ts
class BackendAPIService {
  private goServiceUrl: string;
  private pythonServiceUrl: string; 
  private integrationServiceUrl: string;
  private authToken: string | null = null;

  constructor() {
    // In production, these would route through API Gateway
    this.goServiceUrl = process.env.REACT_APP_GO_SERVICE_URL || '/api/go';
    this.pythonServiceUrl = process.env.REACT_APP_PYTHON_SERVICE_URL || '/api/ai';
    this.integrationServiceUrl = process.env.REACT_APP_INTEGRATION_SERVICE_URL || '/api/integrations';
  }

  // Direct communication with Go service for high-performance CRUD
  async createAlarm(alarmData: CreateAlarmData): Promise<Alarm> {
    try {
      const response = await fetch(`${this.goServiceUrl}/alarms`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authToken}`,
          'X-Service-Client': 'frontend'
        },
        body: JSON.stringify(alarmData)
      });

      if (!response.ok) {
        throw new Error(`Failed to create alarm: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Go service alarm creation failed:', error);
      throw error;
    }
  }

  // Communication with Python service for AI features
  async getAIRecommendations(userId: string, context: AIContext): Promise<AIRecommendations> {
    try {
      const response = await fetch(`${this.pythonServiceUrl}/recommendations`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authToken}`,
          'X-Privacy-Mode': 'differential-privacy'
        },
        body: JSON.stringify({ userId, context })
      });

      if (!response.ok) {
        throw new Error(`AI recommendations failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Python service AI recommendations failed:', error);
      // Return fallback recommendations
      return this.getFallbackRecommendations(context);
    }
  }

  // Communication through integration service for orchestrated operations
  async createEnhancedAlarm(alarmData: CreateAlarmData): Promise<EnhancedAlarmResult> {
    try {
      const response = await fetch(`${this.integrationServiceUrl}/alarms/enhanced`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authToken}`,
          'X-Request-Type': 'enhanced-creation'
        },
        body: JSON.stringify(alarmData)
      });

      if (!response.ok) {
        throw new Error(`Enhanced alarm creation failed: ${response.statusText}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Integration service enhanced creation failed:', error);
      // Fallback to direct Go service creation
      const basicAlarm = await this.createAlarm(alarmData);
      return {
        alarm: basicAlarm,
        aiRecommendations: null,
        appliedEnhancements: [],
        fallbackMode: true
      };
    }
  }

  private getFallbackRecommendations(context: AIContext): AIRecommendations {
    // Basic rule-based recommendations when AI service is unavailable
    return {
      timing: this.getBasicTimingRecommendations(context),
      accessibility: this.getBasicAccessibilityRecommendations(context),
      confidence: 0.3,
      source: 'fallback-rules'
    };
  }
}

export const backendAPI = new BackendAPIService();
```

## Optimized State Management for Multi-Service Architecture

Implement state management that efficiently handles data from multiple backend services:

```typescript
// src/hooks/useEnhancedAlarms.ts
import { useState, useEffect, useCallback } from 'react';
import { backendAPI } from '../services/apiService';

export const useEnhancedAlarms = () => {
  const [alarms, setAlarms] = useState<Alarm[]>([]);
  const [aiInsights, setAIInsights] = useState<AIInsights | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Optimized alarm creation that leverages all backend services
  const createEnhancedAlarm = useCallback(async (alarmData: CreateAlarmData) => {
    setLoading(true);
    setError(null);

    try {
      // Use integration service for AI-enhanced creation
      const result = await backendAPI.createEnhancedAlarm(alarmData);
      
      // Update local state with new alarm
      setAlarms(prev => [...prev, result.alarm]);
      
      // Update AI insights if available
      if (result.aiRecommendations) {
        setAIInsights(prev => ({
          ...prev,
          lastRecommendations: result.aiRecommendations,
          appliedEnhancements: result.appliedEnhancements
        }));
      }

      // Show user feedback about AI enhancements
      if (result.appliedEnhancements.length > 0) {
        showNotification({
          type: 'success',
          message: `Alarm created with ${result.appliedEnhancements.length} AI improvements`,
          details: result.appliedEnhancements
        });
      }

      return result;

    } catch (error) {
      setError(error.message);
      throw error;
    } finally {
      setLoading(false);
    }
  }, []);

  // Efficient bulk loading that uses Go service directly for performance
  const loadUserAlarms = useCallback(async (filters?: AlarmFilters) => {
    setLoading(true);
    setError(null);

    try {
      // Use Go service directly for fast alarm retrieval
      const alarmsData = await backendAPI.getUserAlarms(filters);
      setAlarms(alarmsData.alarms);

      // Async: Get AI insights for loaded alarms (non-blocking)
      if (alarmsData.alarms.length > 0) {
        backendAPI.getAIInsights(alarmsData.alarms.map(a => a.id))
          .then(insights => setAIInsights(insights))
          .catch(error => console.warn('AI insights loading failed:', error));
      }

    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  }, []);

  // Real-time updates with efficient service targeting
  const updateAlarm = useCallback(async (alarmId: string, updates: AlarmUpdate) => {
    try {
      // Optimistic update
      setAlarms(prev => prev.map(alarm => 
        alarm.id === alarmId 
          ? { ...alarm, ...updates, lastModified: new Date().toISOString() }
          : alarm
      ));

      // Update via Go service for speed
      const updatedAlarm = await backendAPI.updateAlarm(alarmId, updates);
      
      // Replace optimistic update with server response
      setAlarms(prev => prev.map(alarm => 
        alarm.id === alarmId ? updatedAlarm : alarm
      ));

      // If significant changes, get updated AI recommendations
      if (this.isSignificantChange(updates)) {
        const newRecommendations = await backendAPI.getAIRecommendations(
          updatedAlarm.userId, 
          { alarmId, changes: updates }
        );
        
        setAIInsights(prev => ({
          ...prev,
          lastRecommendations: newRecommendations
        }));
      }

    } catch (error) {
      // Revert optimistic update on failure
      await loadUserAlarms();
      setError(error.message);
      throw error;
    }
  }, [loadUserAlarms]);

  return {
    alarms,
    aiInsights,
    loading,
    error,
    createEnhancedAlarm,
    loadUserAlarms,
    updateAlarm,
    isReady: alarms.length >= 0 && !loading
  };
};

// Helper to determine if changes warrant new AI analysis
function isSignificantChange(updates: AlarmUpdate): boolean {
  return !!(
    updates.datetime || 
    updates.category || 
    updates.priority || 
    updates.accessibility
  );
}
```

## Service-Aware Error Handling

Implement error handling that provides appropriate feedback based on which backend service failed:

```typescript
// src/utils/errorHandling.ts
export class ServiceAwareErrorHandler {
  static handleBackendError(error: any, service: 'go' | 'python' | 'integration'): UserFriendlyError {
    const baseError = {
      timestamp: new Date().toISOString(),
      service,
      originalError: error.message
    };

    switch (service) {
      case 'go':
        return this.handleGoServiceError(error, baseError);
      case 'python': 
        return this.handlePythonServiceError(error, baseError);
      case 'integration':
        return this.handleIntegrationServiceError(error, baseError);
      default:
        return this.handleGenericError(error, baseError);
    }
  }

  private static handleGoServiceError(error: any, baseError: any): UserFriendlyError {
    // Go service handles critical alarm operations
    if (error.status === 409) {
      return {
        ...baseError,
        userMessage: 'This alarm conflicts with your existing schedule',
        severity: 'medium',
        actionable: true,
        suggestedActions: [
          'Choose a different time',
          'Review conflicting alarms',
          'Modify existing alarm instead'
        ],
        retryable: false
      };
    }

    if (error.status === 422) {
      return {
        ...baseError,
        userMessage: 'Please check your alarm details and try again',
        severity: 'low',
        actionable: true,
        suggestedActions: [
          'Verify the time is in the future',
          'Check that the title is not empty',
          'Ensure required fields are filled'
        ],
        retryable: true
      };
    }

    if (error.status >= 500) {
      return {
        ...baseError,
        userMessage: 'Alarm system temporarily unavailable. Your data is safe.',
        severity: 'high',
        actionable: true,
        suggestedActions: [
          'Try again in a few moments',
          'Check your internet connection',
          'Contact support if issue persists'
        ],
        retryable: true,
        retryDelay: 5000
      };
    }

    return this.handleGenericError(error, baseError);
  }

  private static handlePythonServiceError(error: any, baseError: any): UserFriendlyError {
    // Python service handles AI features - these are often optional
    return {
      ...baseError,
      userMessage: 'AI features temporarily unavailable. Basic functionality still works.',
      severity: 'low',
      actionable: false,
      suggestedActions: [
        'Continue using manual alarm creation',
        'AI recommendations will return when service recovers'
      ],
      retryable: true,
      retryDelay: 30000, // Longer delay for AI service
      degradedMode: true
    };
  }

  private static handleIntegrationServiceError(error: any, baseError: any): UserFriendlyError {
    // Integration service handles external features
    if (error.message.includes('notification')) {
      return {
        ...baseError,
        userMessage: 'Alarm created, but notifications may be delayed',
        severity: 'medium',
        actionable: true,
        suggestedActions: [
          'Check notification settings',
          'Verify internet connection',
          'Enable backup local notifications'
        ],
        retryable: true,
        partialSuccess: true
      };
    }

    if (error.message.includes('calendar')) {
      return {
        ...baseError,
        userMessage: 'Alarm created, but calendar sync failed',
        severity: 'low',
        actionable: true,
        suggestedActions: [
          'Check calendar integration settings',
          'Manually add to calendar if needed',
          'Retry sync later'
        ],
        retryable: true,
        partialSuccess: true
      };
    }

    return this.handleGenericError(error, baseError);
  }

  private static handleGenericError(error: any, baseError: any): UserFriendlyError {
    return {
      ...baseError,
      userMessage: 'Something went wrong. Please try again.',
      severity: 'medium',
      actionable: true,
      suggestedActions: [
        'Check your internet connection',
        'Try again in a moment',
        'Contact support if problem continues'
      ],
      retryable: true
    };
  }
}

interface UserFriendlyError {
  userMessage: string;
  severity: 'low' | 'medium' | 'high';
  actionable: boolean;
  suggestedActions: string[];
  retryable: boolean;
  retryDelay?: number;
  degradedMode?: boolean;
  partialSuccess?: boolean;
  service: string;
  timestamp: string;
  originalError: string;
}
```

## Performance Optimization for Multi-Service Communication

Implement intelligent caching and request optimization to minimize latency across your distributed architecture:

```typescript
// src/utils/serviceOptimization.ts
export class ServiceOptimizationManager {
  private cache: Map<string, CachedResponse> = new Map();
  private requestQueue: Map<string, Promise<any>> = new Map();
  private batchTimer: NodeJS.Timeout | null = null;
  private pendingBatchRequests: BatchRequest[] = [];

  /**
   * Intelligent request batching for efficient service communication
   */
  async batchRequest<T>(
    service: 'go' | 'python' | 'integration',
    operation: string,
    data: any,
    options: BatchOptions = {}
  ): Promise<T> {
    const requestKey = `${service}-${operation}`;
    const batchDelay = options.batchDelay || 100; // 100ms default batching window

    // If this exact request is already pending, return the existing promise
    if (this.requestQueue.has(requestKey)) {
      return this.requestQueue.get(requestKey)!;
    }

    // Add to batch queue
    const promise = new Promise<T>((resolve, reject) => {
      this.pendingBatchRequests.push({
        service,
        operation,
        data,
        resolve,
        reject,
        timestamp: Date.now()
      });

      // Set up batch processing if not already scheduled
      if (!this.batchTimer) {
        this.batchTimer = setTimeout(() => {
          this.processBatch();
        }, batchDelay);
      }
    });

    this.requestQueue.set(requestKey, promise);
    return promise;
  }

  private async processBatch(): Promise<void> {
    const batch = [...this.pendingBatchRequests];
    this.pendingBatchRequests = [];
    this.batchTimer = null;

    // Group requests by service for efficient batch processing
    const serviceGroups = this.groupByService(batch);

    // Process each service group
    await Promise.allSettled([
      this.processGoServiceBatch(serviceGroups.go || []),
      this.processPythonServiceBatch(serviceGroups.python || []),
      this.processIntegrationServiceBatch(serviceGroups.integration || [])
    ]);

    // Clear request queue for processed requests
    batch.forEach(request => {
      const requestKey = `${request.service}-${request.operation}`;
      this.requestQueue.delete(requestKey);
    });
  }

  /**
   * Smart caching with service-specific TTL strategies
   */
  async getCachedOrFetch<T>(
    cacheKey: string,
    fetchFunction: () => Promise<T>,
    ttl: number = 300000 // 5 minutes default
  ): Promise<T> {
    const cached = this.cache.get(cacheKey);
    
    if (cached && (Date.now() - cached.timestamp) < ttl) {
      return cached.data as T;
    }

    try {
      const data = await fetchFunction();
      
      this.cache.set(cacheKey, {
        data,
        timestamp: Date.now(),
        ttl
      });

      // Clean up expired cache entries periodically
      this.cleanupExpiredCache();
      
      return data;
    } catch (error) {
      // Return stale cache if available during service failure
      if (cached) {
        console.warn(`Service failed, returning stale cache for ${cacheKey}`);
        return cached.data as T;
      }
      throw error;
    }
  }

  /**
   * Service-specific optimization strategies
   */
  optimizeForService(service: 'go' | 'python' | 'integration'): ServiceOptimization {
    switch (service) {
      case 'go':
        return {
          batchDelay: 50, // Faster batching for CRUD operations
          cacheStrategy: 'aggressive', // Cache alarm data aggressively
          retryPolicy: {
            maxRetries: 3,
            baseDelay: 100,
            backoffMultiplier: 1.5
          },
          timeoutMs: 3000
        };

      case 'python':
        return {
          batchDelay: 500, // Longer batching for AI operations
          cacheStrategy: 'conservative', // AI results may be context-dependent
          retryPolicy: {
            maxRetries: 2,
            baseDelay: 1000,
            backoffMultiplier: 2
          },
          timeoutMs: 30000 // AI operations take longer
        };

      case 'integration':
        return {
          batchDelay: 200, // Medium batching for orchestration
          cacheStrategy: 'minimal', // Integration results often real-time
          retryPolicy: {
            maxRetries: 3,
            baseDelay: 500,
            backoffMultiplier: 2
          },
          timeoutMs: 10000
        };

      default:
        throw new Error(`Unknown service: ${service}`);
    }
  }

  /**
   * Circuit breaker pattern for service resilience
   */
  private circuitBreakers: Map<string, CircuitBreakerState> = new Map();

  async executeWithCircuitBreaker<T>(
    serviceKey: string,
    operation: () => Promise<T>,
    options: CircuitBreakerOptions = {}
  ): Promise<T> {
    const breaker = this.getOrCreateCircuitBreaker(serviceKey, options);
    
    if (breaker.state === 'open') {
      const timeSinceOpen = Date.now() - breaker.lastFailureTime;
      if (timeSinceOpen < breaker.resetTimeout) {
        throw new Error(`Circuit breaker open for ${serviceKey}`);
      } else {
        // Try to reset to half-open
        breaker.state = 'half-open';
      }
    }

    try {
      const result = await operation();
      
      // Success: reset failure count and close circuit if half-open
      if (breaker.state === 'half-open') {
        breaker.state = 'closed';
        breaker.failureCount = 0;
      }
      
      return result;
    } catch (error) {
      breaker.failureCount++;
      breaker.lastFailureTime = Date.now();
      
      // Open circuit if failure threshold exceeded
      if (breaker.failureCount >= breaker.failureThreshold) {
        breaker.state = 'open';
      }
      
      throw error;
    }
  }

  private getOrCreateCircuitBreaker(serviceKey: string, options: CircuitBreakerOptions): CircuitBreakerState {
    if (!this.circuitBreakers.has(serviceKey)) {
      this.circuitBreakers.set(serviceKey, {
        state: 'closed',
        failureCount: 0,
        lastFailureTime: 0,
        failureThreshold: options.failureThreshold || 5,
        resetTimeout: options.resetTimeout || 60000
      });
    }
    return this.circuitBreakers.get(serviceKey)!;
  }

  private cleanupExpiredCache(): void {
    const now = Date.now();
    for (const [key, cached] of this.cache.entries()) {
      if ((now - cached.timestamp) > cached.ttl) {
        this.cache.delete(key);
      }
    }
  }
}

// Type definitions for optimization
interface BatchRequest {
  service: 'go' | 'python' | 'integration';
  operation: string;
  data: any;
  resolve: (value: any) => void;
  reject: (error: any) => void;
  timestamp: number;
}

interface BatchOptions {
  batchDelay?: number;
  priority?: 'low' | 'medium' | 'high';
}

interface CachedResponse {
  data: any;
  timestamp: number;
  ttl: number;
}

interface ServiceOptimization {
  batchDelay: number;
  cacheStrategy: 'aggressive' | 'conservative' | 'minimal';
  retryPolicy: {
    maxRetries: number;
    baseDelay: number;
    backoffMultiplier: number;
  };
  timeoutMs: number;
}

interface CircuitBreakerOptions {
  failureThreshold?: number;
  resetTimeout?: number;
}

interface CircuitBreakerState {
  state: 'closed' | 'open' | 'half-open';
  failureCount: number;
  lastFailureTime: number;
  failureThreshold: number;
  resetTimeout: number;
}

export const serviceOptimization = new ServiceOptimizationManager();
```

This updated frontend architecture now properly integrates with your multi-language backend approach, providing efficient communication with each specialized service while maintaining the accessibility and neurodivergent-focused features that make your application unique. The frontend intelligently routes requests to the most appropriate backend service and handles the complexity of distributed system communication transparently for users.
