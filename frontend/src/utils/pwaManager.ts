// PWA Management Utilities
// import { useRegisterSW } from 'virtual:pwa-register/react';

export interface PWAInstallPrompt {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

export interface PWACapabilities {
  isInstallable: boolean;
  isInstalled: boolean;
  isOnline: boolean;
  hasNotificationPermission: boolean;
  supportsPushNotifications: boolean;
  supportsBackgroundSync: boolean;
}

class PWAManager {
  private static instance: PWAManager;
  private installPrompt: PWAInstallPrompt | null = null;
  private capabilities: PWACapabilities = {
    isInstallable: false,
    isInstalled: false,
    isOnline: navigator.onLine,
    hasNotificationPermission: false,
    supportsPushNotifications: false,
    supportsBackgroundSync: false
  };
  private eventListeners: Map<string, Function[]> = new Map();

  private constructor() {
    this.initializeCapabilities();
    this.setupEventListeners();
  }

  public static getInstance(): PWAManager {
    if (!PWAManager.instance) {
      PWAManager.instance = new PWAManager();
    }
    return PWAManager.instance;
  }

  private initializeCapabilities() {
    // Check if app is installed
    this.capabilities.isInstalled = window.matchMedia('(display-mode: standalone)').matches ||
      (window.navigator as any).standalone === true;

    // Check notification permission
    if ('Notification' in window) {
      this.capabilities.hasNotificationPermission = Notification.permission === 'granted';
    }

    // Check push notification support
    this.capabilities.supportsPushNotifications = 'serviceWorker' in navigator && 'PushManager' in window;

    // Check background sync support
    this.capabilities.supportsBackgroundSync = 'serviceWorker' in navigator && 'sync' in window.ServiceWorkerRegistration.prototype;

    // Update online status
    this.capabilities.isOnline = navigator.onLine;
  }

  private setupEventListeners() {
    // Listen for install prompt
    window.addEventListener('beforeinstallprompt', (e) => {
      e.preventDefault();
      this.installPrompt = e as any;
      this.capabilities.isInstallable = true;
      this.emit('installable', true);
    });

    // Listen for app installed
    window.addEventListener('appinstalled', () => {
      this.capabilities.isInstalled = true;
      this.capabilities.isInstallable = false;
      this.installPrompt = null;
      this.emit('installed', true);
    });

    // Listen for online/offline events
    window.addEventListener('online', () => {
      this.capabilities.isOnline = true;
      this.emit('online', true);
    });

    window.addEventListener('offline', () => {
      this.capabilities.isOnline = false;
      this.emit('online', false);
    });

    // Listen for visibility changes (for background sync)
    document.addEventListener('visibilitychange', () => {
      if (!document.hidden && this.capabilities.isOnline) {
        this.emit('foreground', true);
      }
    });
  }

  public getCapabilities(): PWACapabilities {
    return { ...this.capabilities };
  }

  public async installApp(): Promise<boolean> {
    if (!this.installPrompt) {
      throw new Error('App installation not available');
    }

    try {
      await this.installPrompt.prompt();
      const choiceResult = await this.installPrompt.userChoice;

      if (choiceResult.outcome === 'accepted') {
        this.capabilities.isInstalled = true;
        this.capabilities.isInstallable = false;
        this.installPrompt = null;
        return true;
      }

      return false;
    } catch (error) {
      console.error('Failed to install app:', error);
      return false;
    }
  }

  public async requestNotificationPermission(): Promise<boolean> {
    if (!('Notification' in window)) {
      return false;
    }

    try {
      const permission = await Notification.requestPermission();
      this.capabilities.hasNotificationPermission = permission === 'granted';
      this.emit('notificationPermission', this.capabilities.hasNotificationPermission);
      return this.capabilities.hasNotificationPermission;
    } catch (error) {
      console.error('Failed to request notification permission:', error);
      return false;
    }
  }

  public async subscribeToPushNotifications(): Promise<PushSubscription | null> {
    if (!this.capabilities.supportsPushNotifications || !this.capabilities.hasNotificationPermission) {
      return null;
    }

    try {
      const registration = await navigator.serviceWorker.ready;

      // Check if already subscribed
      let subscription = await registration.pushManager.getSubscription();

      if (!subscription) {
        // Create new subscription
        const vapidPublicKey = import.meta.env.VITE_VAPID_PUBLIC_KEY;
        if (!vapidPublicKey) {
          console.warn('VAPID public key not configured');
          return null;
        }

        subscription = await registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: this.urlBase64ToUint8Array(vapidPublicKey) as BufferSource
        });
      }

      // Send subscription to server
      await this.sendSubscriptionToServer(subscription);

      return subscription;
    } catch (error) {
      console.error('Failed to subscribe to push notifications:', error);
      return null;
    }
  }

  public async unsubscribeFromPushNotifications(): Promise<boolean> {
    try {
      const registration = await navigator.serviceWorker.ready;
      const subscription = await registration.pushManager.getSubscription();

      if (subscription) {
        await subscription.unsubscribe();
        await this.removeSubscriptionFromServer(subscription);
        return true;
      }

      return false;
    } catch (error) {
      console.error('Failed to unsubscribe from push notifications:', error);
      return false;
    }
  }

  public async scheduleBackgroundSync(tag: string, data?: any): Promise<boolean> {
    if (!this.capabilities.supportsBackgroundSync) {
      return false;
    }

    try {
      const registration = await navigator.serviceWorker.ready;

      // Store data for background sync if provided
      if (data) {
        await this.storeBackgroundSyncData(tag, data);
      }

      await (registration as any).sync.register(tag);
      return true;
    } catch (error) {
      console.error('Failed to schedule background sync:', error);
      return false;
    }
  }

  public showNotification(title: string, options?: NotificationOptions): Promise<void> {
    return new Promise((resolve, reject) => {
      if (!this.capabilities.hasNotificationPermission) {
        reject(new Error('Notification permission not granted'));
        return;
      }

      try {
        const notification = new Notification(title, {
          icon: '/pwa-192x192.png',
          badge: '/pwa-192x192.png',
          ...options
        });

        notification.onclick = () => {
          window.focus();
          notification.close();
        };

        resolve();
      } catch (error) {
        reject(error);
      }
    });
  }

  public addEventListener(event: string, callback: Function): void {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event)!.push(callback);
  }

  public removeEventListener(event: string, callback: Function): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  private emit(event: string, data: any): void {
    const listeners = this.eventListeners.get(event);
    if (listeners) {
      listeners.forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event listener for ${event}:`, error);
        }
      });
    }
  }

  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
      .replace(/-/g, '+')
      .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }

  private async sendSubscriptionToServer(subscription: PushSubscription): Promise<void> {
    try {
      const response = await fetch('/api/push/subscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          subscription: subscription.toJSON(),
          userAgent: navigator.userAgent,
          timestamp: new Date().toISOString()
        })
      });

      if (!response.ok) {
        throw new Error('Failed to send subscription to server');
      }
    } catch (error) {
      console.error('Error sending subscription to server:', error);
      throw error;
    }
  }

  private async removeSubscriptionFromServer(subscription: PushSubscription): Promise<void> {
    try {
      const response = await fetch('/api/push/unsubscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify({
          endpoint: subscription.endpoint
        })
      });

      if (!response.ok) {
        throw new Error('Failed to remove subscription from server');
      }
    } catch (error) {
      console.error('Error removing subscription from server:', error);
      throw error;
    }
  }

  private async storeBackgroundSyncData(tag: string, data: any): Promise<void> {
    try {
      const db = await this.openIndexedDB();
      const transaction = db.transaction(['backgroundSync'], 'readwrite');
      const store = transaction.objectStore('backgroundSync');

      await store.put({
        tag,
        data,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      console.error('Failed to store background sync data:', error);
    }
  }

  private async openIndexedDB(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
      const request = indexedDB.open('SmartAlarmPWA', 1);

      request.onerror = () => reject(request.error);
      request.onsuccess = () => resolve(request.result);

      request.onupgradeneeded = (event) => {
        const db = (event.target as IDBOpenDBRequest).result;

        if (!db.objectStoreNames.contains('backgroundSync')) {
          const store = db.createObjectStore('backgroundSync', { keyPath: 'tag' });
          store.createIndex('timestamp', 'timestamp', { unique: false });
        }
      };
    });
  }
}

// Create singleton instance
export const pwaManager = PWAManager.getInstance();

// React hook for PWA functionality
export function usePWA() {
  // const {
  //   needRefresh,
  //   offlineReady,
  //   updateServiceWorker
  // } = useRegisterSW({
  //   onRegistered(r: any) {
  //     console.log('SW Registered: ' + r);
  //   },
  //   onRegisterError(error: any) {
  //     console.log('SW registration error', error);
  //   },
  // });

  // Temporary fallback values
  const needRefresh = false;
  const offlineReady = false;
  const updateServiceWorker = () => {};

  return {
    // Service Worker state
    needRefresh,
    offlineReady,
    updateServiceWorker,

    // PWA capabilities
    capabilities: pwaManager.getCapabilities(),

    // Installation
    installApp: () => pwaManager.installApp(),

    // Notifications
    requestNotificationPermission: () => pwaManager.requestNotificationPermission(),
    subscribeToPushNotifications: () => pwaManager.subscribeToPushNotifications(),
    unsubscribeFromPushNotifications: () => pwaManager.unsubscribeFromPushNotifications(),
    showNotification: (title: string, options?: NotificationOptions) =>
      pwaManager.showNotification(title, options),

    // Background sync
    scheduleBackgroundSync: (tag: string, data?: any) =>
      pwaManager.scheduleBackgroundSync(tag, data),

    // Event listeners
    addEventListener: (event: string, callback: Function) =>
      pwaManager.addEventListener(event, callback),
    removeEventListener: (event: string, callback: Function) =>
      pwaManager.removeEventListener(event, callback)
  };
}
