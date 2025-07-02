# PWA & Notifications Implementation Instructions

## Critical Notification Reliability Philosophy

For neurodivergent users, missing a medication alarm or important appointment reminder isn't just inconvenient—it can have serious health consequences or create significant anxiety. Your notification system must be designed with multiple redundancy layers, graceful degradation, and absolute reliability as the primary goal. This means implementing multiple notification strategies and always having fallbacks ready when primary systems fail.

The notification architecture should assume that users will encounter various browser limitations, device restrictions, and network connectivity issues. Your system needs to work reliably across iOS Safari (which has significant PWA limitations), Android Chrome (which is most permissive), and various desktop browsers with different notification policies.

## Service Worker Architecture

Create a robust Service Worker that handles offline functionality, background sync, and notification delivery with multiple fallback mechanisms:

```typescript
// public/sw.js (or src/serviceWorker.ts if using TypeScript compilation)
const CACHE_NAME = 'neurodivergent-alarms-v1';
const STATIC_CACHE_URLS = [
  '/',
  '/static/js/bundle.js',
  '/static/css/main.css',
  '/manifest.json',
  '/icons/icon-192.png',
  '/icons/icon-512.png',
  '/sounds/gentle-chime.mp3',
  '/sounds/standard-beep.mp3',
  '/sounds/urgent-alarm.mp3'
];

// Install event - cache critical resources
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => cache.addAll(STATIC_CACHE_URLS))
      .then(() => self.skipWaiting())
  );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches.keys()
      .then((cacheNames) => {
        return Promise.all(
          cacheNames
            .filter((cacheName) => cacheName !== CACHE_NAME)
            .map((cacheName) => caches.delete(cacheName))
        );
      })
      .then(() => self.clients.claim())
  );
});

// Fetch event - serve from cache with network fallback
self.addEventListener('fetch', (event) => {
  // For API requests, use network-first strategy
  if (event.request.url.includes('/api/')) {
    event.respondWith(
      fetch(event.request)
        .then((response) => {
          // Cache successful API responses for offline use
          if (response.status === 200) {
            const responseClone = response.clone();
            caches.open(CACHE_NAME)
              .then((cache) => cache.put(event.request, responseClone));
          }
          return response;
        })
        .catch(() => {
          // Return cached version if available
          return caches.match(event.request);
        })
    );
  } else {
    // For static resources, use cache-first strategy
    event.respondWith(
      caches.match(event.request)
        .then((response) => response || fetch(event.request))
    );
  }
});

// Background sync for offline alarm creation/updates
self.addEventListener('sync', (event) => {
  if (event.tag === 'background-sync-alarms') {
    event.waitUntil(syncAlarms());
  }
});

// Push notification handling
self.addEventListener('push', (event) => {
  const options = {
    body: 'You have an alarm notification',
    icon: '/icons/icon-192.png',
    badge: '/icons/badge-72.png',
    vibrate: [200, 100, 200],
    data: {
      dateOfArrival: Date.now(),
      primaryKey: 1
    },
    actions: [
      {
        action: 'explore',
        title: 'View Alarm',
        icon: '/icons/checkmark.png'
      },
      {
        action: 'close',
        title: 'Snooze 5min',
        icon: '/icons/snooze.png'
      }
    ]
  };

  if (event.data) {
    const payload = event.data.json();
    options.body = payload.body || options.body;
    options.data = { ...options.data, ...payload.data };
    
    // Customize notification based on alarm priority
    if (payload.priority === 'critical') {
      options.requireInteraction = true;
      options.vibrate = [300, 100, 300, 100, 300];
    }
  }

  event.waitUntil(
    self.registration.showNotification('Alarm Reminder', options)
  );
});

// Handle notification click events
self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  if (event.action === 'explore') {
    // Open the app to the specific alarm
    event.waitUntil(
      clients.openWindow(`/alarm/${event.notification.data.alarmId}`)
    );
  } else if (event.action === 'close') {
    // Handle snooze functionality
    event.waitUntil(
      fetch('/api/alarms/snooze', {
        method: 'POST',
        body: JSON.stringify({
          alarmId: event.notification.data.alarmId,
          snoozeMinutes: 5
        }),
        headers: {
          'Content-Type': 'application/json'
        }
      })
    );
  } else {
    // Default action - open the app
    event.waitUntil(
      clients.openWindow('/')
    );
  }
});

// Local alarm notification scheduling
let scheduledAlarms = new Map();

self.addEventListener('message', (event) => {
  if (event.data && event.data.type === 'SCHEDULE_LOCAL_ALARM') {
    const { alarm, notificationTime } = event.data;
    
    // Calculate delay until notification should fire
    const delay = new Date(notificationTime).getTime() - Date.now();
    
    if (delay > 0) {
      const timeoutId = setTimeout(() => {
        self.registration.showNotification(alarm.title, {
          body: alarm.description || 'Alarm notification',
          icon: '/icons/icon-192.png',
          badge: '/icons/badge-72.png',
          tag: `local-alarm-${alarm.id}`,
          requireInteraction: alarm.accessibility.requireConfirmation,
          vibrate: alarm.accessibility.vibrationPattern || [200, 100, 200],
          data: {
            alarmId: alarm.id,
            isLocal: true,
            priority: alarm.priority
          },
          actions: [
            {
              action: 'complete',
              title: 'Mark Complete',
              icon: '/icons/checkmark.png'
            },
            {
              action: 'snooze',
              title: `Snooze ${alarm.accessibility.snoozeOptions[0]}min`,
              icon: '/icons/snooze.png'
            }
          ]
        });
        
        scheduledAlarms.delete(alarm.id);
      }, delay);
      
      scheduledAlarms.set(alarm.id, timeoutId);
    }
  } else if (event.data && event.data.type === 'CANCEL_LOCAL_ALARM') {
    const timeoutId = scheduledAlarms.get(event.data.alarmId);
    if (timeoutId) {
      clearTimeout(timeoutId);
      scheduledAlarms.delete(event.data.alarmId);
    }
  }
});

// Sync function for background sync
async function syncAlarms() {
  try {
    // Get pending changes from IndexedDB
    const db = await openDB('neurodivergent-alarms', 1);
    const pendingChanges = await db.getAll('pendingSync');
    
    // Send changes to server
    for (const change of pendingChanges) {
      const response = await fetch('/api/alarms/sync', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${change.authToken}`
        },
        body: JSON.stringify(change.data)
      });
      
      if (response.ok) {
        // Remove successfully synced change
        await db.delete('pendingSync', change.id);
      }
    }
  } catch (error) {
    console.error('Background sync failed:', error);
    // Sync will be retried automatically by the browser
  }
}
```

## PWA Manifest Configuration

Create a comprehensive PWA manifest that provides an app-like experience while clearly indicating the application's purpose for neurodivergent users:

```json
// public/manifest.json
{
  "name": "Neurodivergent Alarms - Smart Reminders",
  "short_name": "NeuroAlarms",
  "description": "Accessible alarm and reminder system designed specifically for neurodivergent users with ADHD, autism, and other cognitive differences",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#0066cc",
  "orientation": "portrait-primary",
  
  "icons": [
    {
      "src": "/icons/icon-72.png",
      "sizes": "72x72",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icons/icon-96.png",
      "sizes": "96x96",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icons/icon-128.png",
      "sizes": "128x128",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icons/icon-144.png",
      "sizes": "144x144",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icons/icon-152.png",
      "sizes": "152x152",
      "type": "image/png",
      "purpose": "maskable"
    },
    {
      "src": "/icons/icon-192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "/icons/icon-384.png",
      "sizes": "384x384",
      "type": "image/png",
      "purpose": "any"
    },
    {
      "src": "/icons/icon-512.png",
      "sizes": "512x512",
      "type": "image/png",
      "purpose": "any"
    }
  ],
  
  "screenshots": [
    {
      "src": "/screenshots/calendar-view.png",
      "sizes": "1280x720",
      "type": "image/png",
      "platform": "wide",
      "label": "Calendar view showing weekly alarms"
    },
    {
      "src": "/screenshots/alarm-creation.png",
      "sizes": "640x1136",
      "type": "image/png",
      "platform": "narrow",
      "label": "Simple alarm creation interface"
    }
  ],
  
  "categories": ["health", "productivity", "accessibility", "medical"],
  
  "shortcuts": [
    {
      "name": "Quick Alarm",
      "short_name": "New Alarm",
      "description": "Create a new alarm quickly",
      "url": "/quick-alarm",
      "icons": [
        {
          "src": "/icons/shortcut-alarm.png",
          "sizes": "96x96"
        }
      ]
    },
    {
      "name": "Today's Schedule",
      "short_name": "Today",
      "description": "View today's alarms and reminders",
      "url": "/today",
      "icons": [
        {
          "src": "/icons/shortcut-today.png",
          "sizes": "96x96"
        }
      ]
    },
    {
      "name": "Medication Reminders",
      "short_name": "Meds",
      "description": "Quick access to medication alarms",
      "url": "/category/medication",
      "icons": [
        {
          "src": "/icons/shortcut-medication.png",
          "sizes": "96x96"
        }
      ]
    }
  ],
  
  "related_applications": [],
  "prefer_related_applications": false,
  
  "protocol_handlers": [
    {
      "protocol": "web+neuroalarm",
      "url": "/alarm?data=%s"
    }
  ]
}
```

## Notification Permission and Setup

Implement a user-friendly notification permission flow that explains the importance of notifications for neurodivergent users while respecting their autonomy:

```typescript
// src/hooks/useNotifications.ts
import { useState, useEffect, useCallback } from 'react';

interface NotificationState {
  permission: NotificationPermission;
  isSupported: boolean;
  registration: ServiceWorkerRegistration | null;
  fcmToken: string | null;
}

export const useNotifications = () => {
  const [notificationState, setNotificationState] = useState<NotificationState>({
    permission: 'default',
    isSupported: 'Notification' in window,
    registration: null,
    fcmToken: null
  });

  const [lastError, setLastError] = useState<string | null>(null);

  // Check current notification permission status
  useEffect(() => {
    if ('Notification' in window) {
      setNotificationState(prev => ({
        ...prev,
        permission: Notification.permission
      }));
    }
  }, []);

  // Register service worker and set up notifications
  const initializeNotifications = useCallback(async () => {
    try {
      setLastError(null);

      // Register service worker if not already registered
      if ('serviceWorker' in navigator) {
        const registration = await navigator.serviceWorker.register('/sw.js');
        
        setNotificationState(prev => ({
          ...prev,
          registration
        }));

        // Wait for service worker to be ready
        await navigator.serviceWorker.ready;
      }

      return true;
    } catch (error) {
      console.error('Failed to initialize notifications:', error);
      setLastError('Unable to set up notifications. Please try again.');
      return false;
    }
  }, []);

  // Request notification permission with proper explanation
  const requestNotificationPermission = useCallback(async (showRationale = true) => {
    try {
      setLastError(null);

      if (!('Notification' in window)) {
        throw new Error('Notifications are not supported in this browser');
      }

      if (Notification.permission === 'granted') {
        return { granted: true, showedPrompt: false };
      }

      if (Notification.permission === 'denied') {
        throw new Error('Notifications are blocked. Please enable them in your browser settings.');
      }

      // For neurodivergent users, explain WHY notifications are important
      if (showRationale && Notification.permission === 'default') {
        const userConsent = await showNotificationRationale();
        if (!userConsent) {
          return { granted: false, showedPrompt: false, userDeclined: true };
        }
      }

      // Request permission
      const permission = await Notification.requestPermission();
      
      setNotificationState(prev => ({
        ...prev,
        permission
      }));

      if (permission === 'granted') {
        // Initialize Firebase Cloud Messaging if available
        await initializeFCM();
        return { granted: true, showedPrompt: true };
      } else {
        throw new Error('Notification permission was denied');
      }

    } catch (error) {
      console.error('Notification permission request failed:', error);
      setLastError(error.message);
      return { granted: false, showedPrompt: true, error: error.message };
    }
  }, []);

  // Show a user-friendly explanation of why notifications are needed
  const showNotificationRationale = useCallback(async (): Promise<boolean> => {
    return new Promise((resolve) => {
      // This would trigger a modal or overlay explaining notifications
      // For now, we'll assume user consent
      const userWantsNotifications = window.confirm(
        'This app helps you manage important reminders like medication and appointments. ' +
        'Notifications ensure you never miss critical alarms, even when the app is closed. ' +
        'Would you like to enable notifications?'
      );
      resolve(userWantsNotifications);
    });
  }, []);

  // Initialize Firebase Cloud Messaging for push notifications
  const initializeFCM = useCallback(async () => {
    try {
      // Only initialize if Firebase is available and we have a service worker
      if (!notificationState.registration) {
        await initializeNotifications();
      }

      // Initialize FCM (this would require Firebase SDK)
      // const messaging = getMessaging();
      // const token = await getToken(messaging, {
      //   vapidKey: process.env.REACT_APP_VAPID_KEY,
      //   serviceWorkerRegistration: notificationState.registration
      // });

      // For now, simulate token generation
      const simulatedToken = 'fcm-token-' + Math.random().toString(36).substr(2, 9);
      
      setNotificationState(prev => ({
        ...prev,
        fcmToken: simulatedToken
      }));

      // Send token to backend for storage
      await registerFCMToken(simulatedToken);

    } catch (error) {
      console.error('FCM initialization failed:', error);
      // FCM failure shouldn't prevent local notifications
    }
  }, [notificationState.registration]);

  // Register FCM token with backend
  const registerFCMToken = useCallback(async (token: string) => {
    try {
      await fetch('/api/notifications/register-token', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('authToken')}`
        },
        body: JSON.stringify({
          token,
          platform: navigator.platform,
          userAgent: navigator.userAgent
        })
      });
    } catch (error) {
      console.error('Failed to register FCM token:', error);
    }
  }, []);

  // Schedule local notification using Service Worker
  const scheduleLocalNotification = useCallback(async (alarm: any, notificationTime: Date) => {
    if (!notificationState.registration) {
      throw new Error('Service Worker not registered');
    }

    // Send message to service worker to schedule the notification
    if (notificationState.registration.active) {
      notificationState.registration.active.postMessage({
        type: 'SCHEDULE_LOCAL_ALARM',
        alarm,
        notificationTime: notificationTime.toISOString()
      });
    }

    // Also store in IndexedDB for persistence across browser restarts
    await storeScheduledNotification(alarm, notificationTime);
  }, [notificationState.registration]);

  // Cancel a scheduled local notification
  const cancelLocalNotification = useCallback(async (alarmId: string) => {
    if (notificationState.registration?.active) {
      notificationState.registration.active.postMessage({
        type: 'CANCEL_LOCAL_ALARM',
        alarmId
      });
    }

    // Remove from IndexedDB
    await removeScheduledNotification(alarmId);
  }, [notificationState.registration]);

  // Test notification to verify setup
  const sendTestNotification = useCallback(async () => {
    try {
      if (Notification.permission !== 'granted') {
        throw new Error('Notification permission not granted');
      }

      // Send immediate local notification
      new Notification('Test Notification', {
        body: 'Notifications are working correctly!',
        icon: '/icons/icon-192.png',
        tag: 'test-notification'
      });

      return { success: true };
    } catch (error) {
      console.error('Test notification failed:', error);
      setLastError('Test notification failed: ' + error.message);
      return { success: false, error: error.message };
    }
  }, []);

  return {
    notificationState,
    lastError,
    initializeNotifications,
    requestNotificationPermission,
    scheduleLocalNotification,
    cancelLocalNotification,
    sendTestNotification,
    isReady: notificationState.permission === 'granted' && notificationState.registration !== null
  };
};

// Helper functions for IndexedDB storage
async function storeScheduledNotification(alarm: any, notificationTime: Date) {
  // Implementation would use Dexie.js to store scheduled notifications
  // This ensures they persist across browser sessions
}

async function removeScheduledNotification(alarmId: string) {
  // Implementation would remove from IndexedDB
}
```

## Platform-Specific Notification Handling

Handle the unique challenges and opportunities each platform presents for reliable notifications:

```typescript
// src/services/platformNotificationService.ts
export class PlatformNotificationService {
  private platform: string;
  private capabilities: NotificationCapabilities;

  constructor() {
    this.platform = this.detectPlatform();
    this.capabilities = this.assessCapabilities();
  }

  private detectPlatform(): string {
    const userAgent = navigator.userAgent.toLowerCase();
    
    if (/iphone|ipad|ipod/.test(userAgent)) {
      return 'ios';
    } else if (/android/.test(userAgent)) {
      return 'android';
    } else if (/macintosh/.test(userAgent)) {
      return 'macos';
    } else if (/windows/.test(userAgent)) {
      return 'windows';
    } else {
      return 'unknown';
    }
  }

  private assessCapabilities(): NotificationCapabilities {
    return {
      // iOS Safari PWA capabilities
      supportsServiceWorkerNotifications: 'serviceWorker' in navigator && this.platform !== 'ios',
      supportsWebPush: 'PushManager' in window,
      supportsPersistentNotifications: this.platform !== 'ios',
      supportsActionButtons: this.platform !== 'ios',
      supportsCustomSounds: this.platform === 'android',
      supportsVibration: 'vibrate' in navigator,
      requiresUserInteraction: this.platform === 'ios',
      
      // Wake lock capabilities for keeping alarms active
      supportsWakeLock: 'wakeLock' in navigator,
      supportsPageVisibilityAPI: 'visibilityState' in document,
      
      // Background processing limitations
      backgroundProcessingLimited: this.platform === 'ios',
      requiresAppInstallation: this.platform === 'ios', // For reliable notifications
      maxBackgroundTime: this.platform === 'ios' ? 30000 : 300000 // iOS: 30s, others: 5min
    };
  }

  // Create platform-optimized notification
  async createNotification(alarm: any, options: NotificationOptions = {}) {
    const baseOptions: NotificationOptions = {
      icon: '/icons/icon-192.png',
      badge: '/icons/badge-72.png',
      tag: `alarm-${alarm.id}`,
      timestamp: Date.now(),
      ...options
    };

    // iOS-specific optimizations
    if (this.platform === 'ios') {
      return this.createIOSOptimizedNotification(alarm, baseOptions);
    }

    // Android-specific optimizations
    if (this.platform === 'android') {
      return this.createAndroidOptimizedNotification(alarm, baseOptions);
    }

    // Desktop optimizations
    return this.createDesktopOptimizedNotification(alarm, baseOptions);
  }

  private async createIOSOptimizedNotification(alarm: any, options: NotificationOptions) {
    // iOS Safari PWA limitations require different approach
    const notificationOptions = {
      ...options,
      // iOS doesn't support action buttons reliably
      actions: undefined,
      // Require interaction for critical alarms
      requireInteraction: alarm.priority === 'critical',
      // Simple vibration pattern that works on iOS
      vibrate: alarm.accessibility.vibrationPattern || [200, 100, 200],
      // No custom sounds on iOS web
      silent: false
    };

    // For iOS, we also need to implement fallback mechanisms
    if (!this.capabilities.supportsServiceWorkerNotifications) {
      // Use interval-based checking as fallback
      return this.implementiOSFallback(alarm);
    }

    return new Notification(alarm.title, notificationOptions);
  }

  private async createAndroidOptimizedNotification(alarm: any, options: NotificationOptions) {
    const notificationOptions = {
      ...options,
      // Android supports rich notifications
      actions: [
        {
          action: 'complete',
          title: 'Mark Complete',
          icon: '/icons/checkmark.png'
        },
        {
          action: 'snooze',
          title: `Snooze ${alarm.accessibility.snoozeOptions[0]}min`,
          icon: '/icons/snooze.png'
        }
      ],
      // Custom vibration patterns work well on Android
      vibrate: alarm.accessibility.vibrationPattern || [300, 100, 300, 100, 300],
      // Require interaction for critical alarms
      requireInteraction: alarm.priority === 'critical',
      // Android supports custom notification sounds
      sound: this.getCustomSound(alarm.accessibility.audioOptions.soundType)
    };

    return new Notification(alarm.title, notificationOptions);
  }

  private async createDesktopOptimizedNotification(alarm: any, options: NotificationOptions) {
    const notificationOptions = {
      ...options,
      // Desktop supports all notification features
      actions: [
        {
          action: 'complete',
          title: 'Mark Complete',
          icon: '/icons/checkmark.png'
        },
        {
          action: 'snooze',
          title: `Snooze ${alarm.accessibility.snoozeOptions[0]}min`,
          icon: '/icons/snooze.png'
        },
        {
          action: 'view',
          title: 'View Details',
          icon: '/icons/view.png'
        }
      ],
      // Desktop can handle longer notification display
      requireInteraction: alarm.priority === 'critical' || alarm.priority === 'high',
      // Desktop supports custom sounds
      sound: this.getCustomSound(alarm.accessibility.audioOptions.soundType)
    };

    return new Notification(alarm.title, notificationOptions);
  }

  // iOS fallback mechanism for when Service Workers are limited
  private async implementiOSFallback(alarm: any) {
    // Store alarm in localStorage for interval checking
    const fallbackAlarms = JSON.parse(localStorage.getItem('fallbackAlarms') || '[]');
    fallbackAlarms.push({
      id: alarm.id,
      title: alarm.title,
      description: alarm.description,
      scheduledTime: alarm.datetime,
      priority: alarm.priority,
      shown: false
    });
    localStorage.setItem('fallbackAlarms', JSON.stringify(fallbackAlarms));

    // Set up interval checking (this works even when iOS throttles background processing)
    this.startFallbackChecker();
  }

  private startFallbackChecker() {
    // Check every 15 seconds for due alarms (iOS allows this frequency)
    setInterval(() => {
      const fallbackAlarms = JSON.parse(localStorage.getItem('fallbackAlarms') || '[]');
      const now = new Date().getTime();

      fallbackAlarms.forEach((alarm: any, index: number) => {
        const alarmTime = new Date(alarm.scheduledTime).getTime();
        
        if (!alarm.shown && alarmTime <= now) {
          // Show notification
          new Notification(alarm.title, {
            body: alarm.description || 'Alarm notification',
            icon: '/icons/icon-192.png',
            tag: `fallback-${alarm.id}`
          });

          // Mark as shown
          fallbackAlarms[index].shown = true;
          localStorage.setItem('fallbackAlarms', JSON.stringify(fallbackAlarms));
        }
      });

      // Clean up old alarms (older than 24 hours)
      const cleanedAlarms = fallbackAlarms.filter((alarm: any) => {
        const alarmTime = new Date(alarm.scheduledTime).getTime();
        return (now - alarmTime) < 24 * 60 * 60 * 1000;
      });
      
      if (cleanedAlarms.length !== fallbackAlarms.length) {
        localStorage.setItem('fallbackAlarms', JSON.stringify(cleanedAlarms));
      }
    }, 15000);
  }

  private getCustomSound(soundType: string): string {
    const soundMap = {
      'gentle': '/sounds/gentle-chime.mp3',
      'standard': '/sounds/standard-beep.mp3',
      'urgent': '/sounds/urgent-alarm.mp3',
      'custom': '/sounds/custom-user-sound.mp3'
    };

    return soundMap[soundType] || soundMap.standard;
  }

  // Keep the app active using Wake Lock API when available
  async requestWakeLock() {
    try {
      if ('wakeLock' in navigator) {
        const wakeLock = await navigator.wakeLock.request('screen');
        
        // Release wake lock when page becomes hidden
        document.addEventListener('visibilitychange', () => {
          if (document.visibilityState === 'hidden') {
            wakeLock.release();
          }
        });

        return wakeLock;
      }
    } catch (error) {
      console.error('Wake Lock API failed:', error);
    }
    return null;
  }
}

interface NotificationCapabilities {
  supportsServiceWorkerNotifications: boolean;
  supportsWebPush: boolean;
  supportsPersistentNotifications: boolean;
  supportsActionButtons: boolean;
  supportsCustomSounds: boolean;
  supportsVibration: boolean;
  requiresUserInteraction: boolean;
  supportsWakeLock: boolean;
  supportsPageVisibilityAPI: boolean;
  backgroundProcessingLimited: boolean;
  requiresAppInstallation: boolean;
  maxBackgroundTime: number;
}
```

## Notification Reliability Testing

Create comprehensive testing utilities to verify notification functionality across different scenarios:

```typescript
// src/utils/notificationTesting.ts
export class NotificationReliabilityTester {
  private testResults: TestResult[] = [];

  async runFullNotificationTest(): Promise<TestSuite> {
    console.log('Starting comprehensive notification reliability test...');
    
    const tests = [
      this.testBasicNotificationSupport(),
      this.testServiceWorkerRegistration(),
      this.testNotificationPermission(),
      this.testLocalNotificationScheduling(),
      this.testNotificationPersistence(),
      this.testPlatformSpecificFeatures(),
      this.testFailoverMechanisms()
    ];

    const results = await Promise.allSettled(tests);
    
    return {
      overallSuccess: results.every(r => r.status === 'fulfilled' && r.value.passed),
      results: results.map((r, i) => ({
        testName: tests[i].name,
        status: r.status,
        result: r.status === 'fulfilled' ? r.value : { passed: false, error: r.reason }
      })),
      recommendations: this.generateRecommendations(results),
      timestamp: new Date().toISOString()
    };
  }

  private async testBasicNotificationSupport(): Promise<TestResult> {
    try {
      const hasNotificationAPI = 'Notification' in window;
      const hasServiceWorker = 'serviceWorker' in navigator;
      const hasPushManager = 'PushManager' in window;

      return {
        passed: hasNotificationAPI,
        details: {
          notificationAPI: hasNotificationAPI,
          serviceWorker: hasServiceWorker,
          pushManager: hasPushManager
        },
        message: hasNotificationAPI 
          ? 'Basic notification support detected'
          : 'Browser does not support notifications'
      };
    } catch (error) {
      return {
        passed: false,
        error: error.message,
        message: 'Failed to check basic notification support'
      };
    }
  }

  private async testServiceWorkerRegistration(): Promise<TestResult> {
    try {
      if (!('serviceWorker' in navigator)) {
        return {
          passed: false,
          message: 'Service Worker not supported'
        };
      }

      const registration = await navigator.serviceWorker.register('/sw.js');
      await navigator.serviceWorker.ready;

      return {
        passed: true,
        details: {
          scope: registration.scope,
          updateViaCache: registration.updateViaCache
        },
        message: 'Service Worker registered successfully'
      };
    } catch (error) {
      return {
        passed: false,
        error: error.message,
        message: 'Service Worker registration failed'
      };
    }
  }

  private async testNotificationPermission(): Promise<TestResult> {
    try {
      const initialPermission = Notification.permission;
      
      if (initialPermission === 'denied') {
        return {
          passed: false,
          message: 'Notification permission is denied - user must enable in browser settings'
        };
      }

      if (initialPermission === 'default') {
        // In testing, we don't want to trigger the actual permission prompt
        return {
          passed: true,
          message: 'Notification permission not yet requested (would need user interaction)',
          details: { permission: initialPermission }
        };
      }

      return {
        passed: true,
        message: 'Notification permission already granted',
        details: { permission: initialPermission }
      };
    } catch (error) {
      return {
        passed: false,
        error: error.message,
        message: 'Failed to check notification permission'
      };
    }
  }

  private async testLocalNotificationScheduling(): Promise<TestResult> {
    try {
      // Test immediate notification
      if (Notification.permission === 'granted') {
        const testNotification = new Notification('Test Notification', {
          body: 'This is a test notification',
          icon: '/icons/icon-192.png',
          tag: 'reliability-test',
          requireInteraction: false
        });

        // Close the test notification after 2 seconds
        setTimeout(() => testNotification.close(), 2000);

        return {
          passed: true,
          message: 'Local notification created successfully',
          details: { notificationTag: 'reliability-test' }
        };
      } else {
        return {
          passed: false,
          message: 'Cannot test notifications without permission'
        };
      }
    } catch (error) {
      return {
        passed: false,
        error: error.message,
        message: 'Local notification test failed'
      };
    }
  }

  private generateRecommendations(results: PromiseSettledResult<TestResult>[]): string[] {
    const recommendations: string[] = [];
    
    // Analyze results and provide specific recommendations
    const hasServiceWorker = results[1]?.status === 'fulfilled' && results[1].value.passed;
    const hasPermission = results[2]?.status === 'fulfilled' && results[2].value.passed;
    
    if (!hasServiceWorker) {
      recommendations.push('Enable Service Worker support for reliable background notifications');
    }
    
    if (!hasPermission) {
      recommendations.push('Request notification permission with clear explanation of benefits');
    }
    
    // Platform-specific recommendations
    const platform = this.detectPlatform();
    if (platform === 'ios') {
      recommendations.push('For iOS users: Install as PWA for reliable notifications');
      recommendations.push('Implement fallback polling mechanism for iOS Safari');
    }
    
    if (platform === 'android') {
      recommendations.push('Consider implementing rich notification actions for Android');
    }
    
    return recommendations;
  }

  private detectPlatform(): string {
    const userAgent = navigator.userAgent.toLowerCase();
    if (/iphone|ipad|ipod/.test(userAgent)) return 'ios';
    if (/android/.test(userAgent)) return 'android';
    return 'desktop';
  }
}

interface TestResult {
  passed: boolean;
  message: string;
  details?: any;
  error?: string;
}

interface TestSuite {
  overallSuccess: boolean;
  results: any[];
  recommendations: string[];
  timestamp: string;
}
```

Remember that notification reliability is absolutely critical for your target users. A missed medication reminder could have serious health consequences, so always implement multiple fallback mechanisms and test thoroughly across all supported platforms. The complexity of ensuring cross-platform notification reliability is significant, but it's essential for building trust with neurodivergent users who depend on these systems for their daily wellbeing.
    