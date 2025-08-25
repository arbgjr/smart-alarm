# Progressive Web App (PWA) Implementation

This document outlines the comprehensive PWA implementation for the Smart Alarm system, covering service workers, caching strategies, background sync, and offline-first architecture.

## Overview

Smart Alarm is built as a modern Progressive Web App that provides:

- **Offline-First Experience**: Full functionality without internet connectivity
- **Cross-Platform Installation**: Native app experience on mobile and desktop
- **Background Synchronization**: Automatic data sync when connectivity returns
- **Smart Caching**: Intelligent resource management for optimal performance

## Service Worker Configuration

### Vite PWA Plugin Setup

The application uses the Vite PWA plugin with Workbox for service worker generation:

```typescript
// vite.config.ts
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: 'autoUpdate',
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg}'],
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/localhost:8080\/api\/.*$/,
            handler: 'NetworkFirst',
            options: {
              cacheName: 'api-cache-dev',
              networkTimeoutSeconds: 10,
              cacheableResponse: { statuses: [0, 200] }
            }
          },
          {
            urlPattern: /^https:\/\/api\.smartalarm\.com\/.*$/,
            handler: 'NetworkFirst', 
            options: {
              cacheName: 'api-cache-prod',
              networkTimeoutSeconds: 10,
              cacheableResponse: { statuses: [0, 200] }
            }
          }
        ]
      },
      includeAssets: ['favicon.ico', 'apple-touch-icon.png', 'masked-icon.svg'],
      manifest: {
        name: 'Smart Alarm',
        short_name: 'SmartAlarm',
        description: 'Intelligent alarm and routine management for neurodivergent users',
        theme_color: '#1f2937',
        background_color: '#ffffff',
        display: 'standalone',
        orientation: 'portrait',
        scope: '/',
        start_url: '/',
        categories: ['productivity', 'lifestyle', 'health'],
        icons: [
          {
            src: 'pwa-192x192.png',
            sizes: '192x192',
            type: 'image/png'
          },
          {
            src: 'pwa-512x512.png', 
            sizes: '512x512',
            type: 'image/png'
          }
        ]
      }
    })
  ]
});
```

### Service Worker Registration

The service worker is registered in the main application entry point:

```typescript
// src/main.tsx
import { registerSW } from 'virtual:pwa-register';

const updateSW = registerSW({
  onNeedRefresh() {
    // Show update notification to user
    if (confirm('New version available. Update now?')) {
      updateSW(true);
    }
  },
  onOfflineReady() {
    console.log('App ready to work offline');
    // Show offline-ready notification
  },
  onRegisterError(error) {
    console.error('Service worker registration failed:', error);
  }
});
```

## Caching Strategies

### Network-First for API Calls

API requests use Network-First strategy to prioritize fresh data:

```typescript
// Service Worker Configuration
{
  urlPattern: /\/api\/.*$/,
  handler: 'NetworkFirst',
  options: {
    cacheName: 'api-cache',
    networkTimeoutSeconds: 10,
    cacheableResponse: {
      statuses: [0, 200]
    },
    backgroundSync: {
      name: 'api-queue',
      options: {
        maxRetentionTime: 24 * 60 // 24 hours
      }
    }
  }
}
```

### Cache-First for Static Assets

Static resources use Cache-First for optimal performance:

```typescript
{
  urlPattern: /\.(?:js|css|html|png|jpg|jpeg|svg|ico)$/,
  handler: 'CacheFirst',
  options: {
    cacheName: 'static-resources',
    expiration: {
      maxEntries: 100,
      maxAgeSeconds: 30 * 24 * 60 * 60 // 30 days
    }
  }
}
```

## Background Sync Implementation

### Background Sync Utility

A comprehensive background sync system handles offline operations:

```typescript
// src/utils/backgroundSync.ts
export class BackgroundSync {
  private static instance: BackgroundSync;
  private syncQueue: SyncData[] = [];

  public addToSyncQueue(
    action: 'create' | 'update' | 'delete',
    entity: 'alarm' | 'routine',
    data: any
  ): void {
    const syncData: SyncData = {
      id: `${entity}-${action}-${Date.now()}`,
      action,
      entity,
      data,
      timestamp: Date.now()
    };

    this.syncQueue.push(syncData);
    this.saveSyncQueue();
    
    // Try immediate sync if online
    if (navigator.onLine) {
      this.processQueue();
    } else {
      this.registerBackgroundSync();
    }
  }

  private async registerBackgroundSync(): Promise<void> {
    if ('serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype) {
      try {
        const registration = await navigator.serviceWorker.ready;
        await registration.sync.register('smart-alarm-background-sync');
      } catch (error) {
        console.error('Background sync registration failed:', error);
        this.setupOnlineListener();
      }
    } else {
      this.setupOnlineListener();
    }
  }

  private setupOnlineListener(): void {
    window.addEventListener('online', () => {
      console.log('Device came online, processing sync queue');
      this.processQueue();
    });
  }

  public async processQueue(): Promise<void> {
    if (this.syncQueue.length === 0) return;

    for (const syncData of [...this.syncQueue]) {
      try {
        await this.processSyncData(syncData);
        this.syncQueue = this.syncQueue.filter(item => item.id !== syncData.id);
      } catch (error) {
        console.error(`Failed to sync ${syncData.id}:`, error);
      }
    }

    this.saveSyncQueue();
  }
}

export const backgroundSync = BackgroundSync.getInstance();
```

### Service Worker Integration

The service worker handles background sync events:

```typescript
// Service Worker (generated by Workbox)
self.addEventListener('sync', event => {
  if (event.tag === 'smart-alarm-background-sync') {
    event.waitUntil(
      // Notify main thread to process sync queue
      self.clients.matchAll().then(clients => {
        clients.forEach(client => {
          client.postMessage({
            type: 'BACKGROUND_SYNC'
          });
        });
      })
    );
  }
});
```

## Offline State Management

### Optimistic Updates

The Zustand stores implement optimistic updates for immediate user feedback:

```typescript
// Alarms Store - Optimistic Create
createAlarm: async (data: AlarmFormData) => {
  const { setLoading, addAlarm } = get();
  
  setLoading(true);
  
  // Create optimistic alarm immediately
  const optimisticAlarm: Alarm = {
    id: `temp-${Date.now()}`,
    ...data,
    userId: 'current-user',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  addAlarm(optimisticAlarm); // Immediate UI update

  try {
    if (navigator.onLine) {
      // Try server sync
      const realAlarm = await alarmService.createAlarm(data);
      get().updateAlarm(optimisticAlarm.id, realAlarm);
      return realAlarm;
    } else {
      // Queue for background sync
      backgroundSync.addToSyncQueue('create', 'alarm', data);
      return optimisticAlarm;
    }
  } catch (error) {
    // Rollback on failure
    get().removeAlarm(optimisticAlarm.id);
    backgroundSync.addToSyncQueue('create', 'alarm', data);
    throw error;
  } finally {
    setLoading(false);
  }
}
```

### Conflict Resolution

When coming back online, the system resolves conflicts between local and server state:

```typescript
// Conflict resolution strategy
private resolveConflict(local: Alarm, server: Alarm): Alarm {
  // Last-write-wins with user preference
  const localTime = new Date(local.updatedAt).getTime();
  const serverTime = new Date(server.updatedAt).getTime();
  
  if (localTime > serverTime) {
    // Local changes are newer
    return { ...server, ...local, id: server.id };
  } else {
    // Server changes are newer
    return server;
  }
}
```

## Installation & App Manifest

### Web App Manifest

The manifest.webmanifest enables installation across platforms:

```json
{
  "name": "Smart Alarm - Intelligent Routine Management",
  "short_name": "Smart Alarm",
  "description": "Accessible alarm and routine management for neurodivergent users",
  "theme_color": "#1f2937",
  "background_color": "#ffffff",
  "display": "standalone",
  "orientation": "portrait",
  "scope": "/",
  "start_url": "/",
  "categories": ["productivity", "lifestyle", "health", "accessibility"],
  "lang": "en-US",
  "dir": "ltr",
  "icons": [
    {
      "src": "/pwa-192x192.png",
      "sizes": "192x192",
      "type": "image/png",
      "purpose": "any maskable"
    },
    {
      "src": "/pwa-512x512.png",
      "sizes": "512x512", 
      "type": "image/png",
      "purpose": "any maskable"
    }
  ],
  "shortcuts": [
    {
      "name": "Create Alarm",
      "short_name": "New Alarm",
      "description": "Create a new alarm",
      "url": "/alarms/create",
      "icons": [
        {
          "src": "/alarm-shortcut-96x96.png",
          "sizes": "96x96"
        }
      ]
    }
  ]
}
```

### Installation Prompts

The application handles installation prompts gracefully:

```typescript
// src/hooks/usePWAInstall.ts
export function usePWAInstall() {
  const [deferredPrompt, setDeferredPrompt] = useState<any>(null);
  const [canInstall, setCanInstall] = useState(false);

  useEffect(() => {
    const handler = (e: Event) => {
      e.preventDefault();
      setDeferredPrompt(e);
      setCanInstall(true);
    };

    window.addEventListener('beforeinstallprompt', handler);
    
    return () => window.removeEventListener('beforeinstallprompt', handler);
  }, []);

  const installApp = async () => {
    if (!deferredPrompt) return false;

    deferredPrompt.prompt();
    const { outcome } = await deferredPrompt.userChoice;
    
    setDeferredPrompt(null);
    setCanInstall(false);
    
    return outcome === 'accepted';
  };

  return { canInstall, installApp };
}
```

## Performance Optimization

### Resource Precaching

Critical resources are precached during service worker installation:

```typescript
// Workbox precaching
precacheAndRoute([
  { url: '/index.html', revision: null },
  { url: '/manifest.webmanifest', revision: null },
  { url: '/offline.html', revision: null }
]);
```

### Runtime Caching

Dynamic content uses intelligent runtime caching:

```typescript
// API Response Caching
registerRoute(
  ({ request }) => request.destination === 'document',
  new NetworkFirst({
    cacheName: 'pages',
    plugins: [
      {
        cacheKeyWillBeUsed: async ({ request }) => {
          return `${request.url}?${new Date().getHours()}`; // Hourly cache invalidation
        }
      }
    ]
  })
);
```

## Testing PWA Features

### Service Worker Testing

```typescript
// src/__tests__/serviceWorker.test.ts
describe('Service Worker', () => {
  beforeEach(() => {
    // Mock service worker APIs
    Object.defineProperty(navigator, 'serviceWorker', {
      value: {
        register: jest.fn(() => Promise.resolve()),
        ready: Promise.resolve({
          sync: { register: jest.fn() }
        })
      }
    });
  });

  it('registers service worker successfully', async () => {
    const { registerSW } = await import('virtual:pwa-register');
    expect(registerSW).toBeDefined();
  });

  it('handles offline state correctly', () => {
    Object.defineProperty(navigator, 'onLine', { value: false });
    
    const sync = new BackgroundSync();
    sync.addToSyncQueue('create', 'alarm', { name: 'Test' });
    
    expect(sync.getPendingCount()).toBe(1);
  });
});
```

### Background Sync Testing

```typescript
// src/__tests__/backgroundSync.test.ts
describe('Background Sync', () => {
  let backgroundSync: BackgroundSync;

  beforeEach(() => {
    backgroundSync = BackgroundSync.getInstance();
    backgroundSync.clearQueue();
  });

  it('adds operations to sync queue when offline', () => {
    Object.defineProperty(navigator, 'onLine', { value: false });
    
    backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test Alarm' });
    
    expect(backgroundSync.getPendingCount()).toBe(1);
  });

  it('processes queue when coming online', async () => {
    backgroundSync.addToSyncQueue('create', 'alarm', { name: 'Test Alarm' });
    
    const processQueueSpy = jest.spyOn(backgroundSync, 'processQueue');
    
    // Simulate coming online
    Object.defineProperty(navigator, 'onLine', { value: true });
    window.dispatchEvent(new Event('online'));
    
    await new Promise(resolve => setTimeout(resolve, 100));
    
    expect(processQueueSpy).toHaveBeenCalled();
  });
});
```

## Browser Support

The PWA implementation supports:

- **Chrome/Edge 90+**: Full PWA features including background sync
- **Firefox 85+**: Service workers and caching (limited background sync)
- **Safari 14+**: Service workers and manifest (no background sync)
- **Mobile browsers**: Full installation and offline support

## Best Practices

1. **Always provide offline fallbacks** for critical functionality
2. **Use optimistic updates** for immediate user feedback
3. **Implement graceful degradation** for unsupported features
4. **Cache strategically** to balance performance and freshness
5. **Test offline scenarios** thoroughly during development
6. **Monitor service worker lifecycle** and update strategies
7. **Respect user preferences** for data usage and notifications

## Troubleshooting

### Common Issues

1. **Service Worker not updating**: Clear browser cache and check update logic
2. **Background sync not working**: Verify browser support and fallback to online listeners  
3. **Cache not clearing**: Implement proper cache versioning and cleanup
4. **Installation not showing**: Check manifest validation and HTTPS requirements

### Debugging Tools

- Chrome DevTools → Application → Service Workers
- Chrome DevTools → Application → Storage (for cache inspection)
- Lighthouse PWA audit for comprehensive analysis
- Workbox debugging logs in development mode

This comprehensive PWA implementation ensures the Smart Alarm system provides a reliable, offline-first experience that works across all platforms and network conditions.