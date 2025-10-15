// Web Push Notifications for Smart Alarm
export interface PushSubscriptionData {
  endpoint: string;
  keys: {
    p256dh: string;
    auth: string;
  };
  userId: string;
  deviceId: string;
  createdAt: string;
}

export interface NotificationPayload {
  title: string;
  body: string;
  icon?: string;
  badge?: string;
  image?: string;
  tag?: string;
  data?: {
    alarmId?: string;
    actionType?: 'alarm_trigger' | 'reminder' | 'optimization' | 'sync';
    url?: string;
    [key: string]: any;
  };
  actions?: NotificationAction[];
  requireInteraction?: boolean;
  silent?: boolean;
  timestamp?: string;
}

export interface NotificationAction {
  action: string;
  title: string;
  icon?: string;
}

export interface PushNotificationPermission {
  state: 'default' | 'granted' | 'denied';
  canPrompt: boolean;
  isSupported: boolean;
}

class PushNotificationManager {
  private static instance: PushNotificationManager;
  private swRegistration: ServiceWorkerRegistration | null = null;
  private subscription: PushSubscription | null = null;
  private readonly VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY || 'BEl62iUYgUivxIkv69yViEuiBIa40HI2BN9VUjNGDiJyNidEA8p3C9UjPqJNJaYP_tLgtGmq5wKpFRx9jY2OKPg';

  private constructor() {
    this.initializeServiceWorker();
  }

  public static getInstance(): PushNotificationManager {
    if (!PushNotificationManager.instance) {
      PushNotificationManager.instance = new PushNotificationManager();
    }
    return PushNotificationManager.instance;
  }

  /**
   * Initialize service worker registration
   */
  private async initializeServiceWorker(): Promise<void> {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      console.warn('Push notifications not supported');
      return;
    }

    try {
      // Get service worker registration from PWA plugin
      this.swRegistration = await navigator.serviceWorker.ready;
      console.log('Service Worker ready for push notifications');
      
      // Check for existing subscription
      this.subscription = await this.swRegistration.pushManager.getSubscription();
      if (this.subscription) {
        console.log('Existing push subscription found');
      }
    } catch (error) {
      console.error('Failed to initialize service worker for push:', error);
    }
  }

  /**
   * Check push notification support and permission status
   */
  public getPermissionStatus(): PushNotificationPermission {
    const isSupported = 'Notification' in window && 'serviceWorker' in navigator && 'PushManager' in window;
    
    if (!isSupported) {
      return {
        state: 'denied',
        canPrompt: false,
        isSupported: false
      };
    }

    const permission = Notification.permission;
    return {
      state: permission,
      canPrompt: permission === 'default',
      isSupported: true
    };
  }

  /**
   * Request notification permission from user
   */
  public async requestPermission(): Promise<NotificationPermission> {
    if (!('Notification' in window)) {
      throw new Error('Notifications not supported');
    }

    if (Notification.permission === 'granted') {
      return 'granted';
    }

    if (Notification.permission === 'denied') {
      throw new Error('Notification permission denied');
    }

    // Request permission
    const permission = await Notification.requestPermission();
    
    if (permission === 'granted') {
      console.log('Notification permission granted');
      // Automatically subscribe to push notifications
      await this.subscribeToPush();
    }

    return permission;
  }

  /**
   * Subscribe to push notifications
   */
  public async subscribeToPush(): Promise<PushSubscriptionData | null> {
    if (!this.swRegistration) {
      await this.initializeServiceWorker();
      if (!this.swRegistration) {
        throw new Error('Service Worker not available');
      }
    }

    if (Notification.permission !== 'granted') {
      throw new Error('Notification permission not granted');
    }

    try {
      // Check if already subscribed
      const existingSubscription = await this.swRegistration.pushManager.getSubscription();
      if (existingSubscription) {
        this.subscription = existingSubscription;
        return this.createSubscriptionData(existingSubscription);
      }

      // Create new subscription
      const subscription = await this.swRegistration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: this.urlBase64ToUint8Array(this.VAPID_PUBLIC_KEY)
      });

      this.subscription = subscription;
      const subscriptionData = this.createSubscriptionData(subscription);

      // Send subscription to backend
      await this.sendSubscriptionToBackend(subscriptionData);

      console.log('Successfully subscribed to push notifications');
      return subscriptionData;

    } catch (error) {
      console.error('Failed to subscribe to push notifications:', error);
      throw error;
    }
  }

  /**
   * Unsubscribe from push notifications
   */
  public async unsubscribe(): Promise<void> {
    if (!this.subscription) {
      const existingSubscription = await this.swRegistration?.pushManager.getSubscription();
      if (!existingSubscription) {
        return; // Already unsubscribed
      }
      this.subscription = existingSubscription;
    }

    try {
      // Unsubscribe from browser
      await this.subscription.unsubscribe();
      
      // Remove subscription from backend
      await this.removeSubscriptionFromBackend();
      
      this.subscription = null;
      console.log('Successfully unsubscribed from push notifications');
      
    } catch (error) {
      console.error('Failed to unsubscribe from push notifications:', error);
      throw error;
    }
  }

  /**
   * Show local notification (fallback when push is not available)
   */
  public async showLocalNotification(payload: NotificationPayload): Promise<void> {
    if (!('Notification' in window)) {
      console.warn('Local notifications not supported');
      return;
    }

    if (Notification.permission !== 'granted') {
      console.warn('Notification permission not granted');
      return;
    }

    try {
      const notification = new Notification(payload.title, {
        body: payload.body,
        icon: payload.icon || '/pwa-192x192.png',
        badge: payload.badge || '/pwa-192x192.png',
        image: payload.image,
        tag: payload.tag || 'smart-alarm-notification',
        data: payload.data,
        requireInteraction: payload.requireInteraction || false,
        silent: payload.silent || false,
        timestamp: payload.timestamp ? new Date(payload.timestamp).getTime() : Date.now()
      });

      // Handle notification click
      notification.onclick = (event) => {
        event.preventDefault();
        notification.close();
        
        // Handle navigation
        if (payload.data?.url) {
          if ('clients' in navigator.serviceWorker) {
            // Try to focus existing window
            navigator.serviceWorker.controller?.postMessage({
              type: 'NOTIFICATION_CLICK',
              url: payload.data.url,
              data: payload.data
            });
          } else {
            window.open(payload.data.url, '_blank');
          }
        }
      };

      // Auto-close after delay for non-critical notifications
      if (!payload.requireInteraction && payload.data?.actionType !== 'alarm_trigger') {
        setTimeout(() => {
          notification.close();
        }, 5000);
      }

    } catch (error) {
      console.error('Failed to show local notification:', error);
    }
  }

  /**
   * Schedule alarm notification
   */
  public async scheduleAlarmNotification(
    alarmId: string,
    triggerTime: string,
    alarmName: string
  ): Promise<void> {
    const now = new Date();
    const triggerDate = new Date(`${now.toDateString()} ${triggerTime}`);
    
    // If trigger time is past, schedule for next day
    if (triggerDate < now) {
      triggerDate.setDate(triggerDate.getDate() + 1);
    }

    const payload: NotificationPayload = {
      title: 'Alarm: ' + alarmName,
      body: `Your alarm is going off! Time: ${triggerTime}`,
      icon: '/pwa-192x192.png',
      badge: '/pwa-192x192.png',
      tag: `alarm-${alarmId}`,
      requireInteraction: true,
      data: {
        alarmId,
        actionType: 'alarm_trigger',
        url: '/?action=dismiss-alarm&id=' + alarmId
      },
      actions: [
        {
          action: 'dismiss',
          title: 'Dismiss',
          icon: '/icons/dismiss.png'
        },
        {
          action: 'snooze',
          title: 'Snooze 5 min',
          icon: '/icons/snooze.png'
        }
      ]
    };

    // For immediate testing, show local notification
    if (import.meta.env.DEV) {
      console.log('Development mode: showing immediate notification for testing');
      await this.showLocalNotification(payload);
      return;
    }

    // In production, this would integrate with the backend scheduling system
    try {
      await this.sendScheduleRequest(alarmId, triggerDate.toISOString(), payload);
    } catch (error) {
      console.error('Failed to schedule alarm notification:', error);
      // Fallback to local notification system
      this.scheduleLocalNotification(triggerDate, payload);
    }
  }

  /**
   * Cancel scheduled alarm notification
   */
  public async cancelAlarmNotification(alarmId: string): Promise<void> {
    try {
      await this.sendCancelRequest(alarmId);
    } catch (error) {
      console.error('Failed to cancel alarm notification:', error);
    }

    // Also clear any local scheduled notifications
    this.cancelLocalNotification(`alarm-${alarmId}`);
  }

  /**
   * Schedule optimization reminder
   */
  public async scheduleOptimizationReminder(message: string, delayMinutes: number = 30): Promise<void> {
    const payload: NotificationPayload = {
      title: 'Smart Alarm Optimization',
      body: message,
      icon: '/pwa-192x192.png',
      tag: 'optimization-reminder',
      data: {
        actionType: 'optimization',
        url: '/?tab=insights'
      }
    };

    const scheduleTime = new Date(Date.now() + delayMinutes * 60 * 1000);
    this.scheduleLocalNotification(scheduleTime, payload);
  }

  /**
   * Schedule local notification using setTimeout (basic fallback)
   */
  private scheduleLocalNotification(scheduleTime: Date, payload: NotificationPayload): void {
    const delay = scheduleTime.getTime() - Date.now();
    
    if (delay > 0) {
      setTimeout(() => {
        this.showLocalNotification(payload);
      }, delay);
    } else {
      // Time has passed, show immediately
      this.showLocalNotification(payload);
    }
  }

  /**
   * Cancel local notification by tag
   */
  private cancelLocalNotification(tag: string): void {
    // This is a limitation of the current approach - we can't cancel setTimeout-based notifications
    // In a production app, you'd use a more sophisticated scheduling system
    console.log('Canceling local notification:', tag);
  }

  /**
   * Convert VAPID key from base64 to Uint8Array
   */
  private urlBase64ToUint8Array(base64String: string): Uint8Array {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
      .replace(/\-/g, '+')
      .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
      outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
  }

  /**
   * Create subscription data object
   */
  private createSubscriptionData(subscription: PushSubscription): PushSubscriptionData {
    const p256dhKey = subscription.getKey('p256dh');
    const authKey = subscription.getKey('auth');

    return {
      endpoint: subscription.endpoint,
      keys: {
        p256dh: p256dhKey ? btoa(String.fromCharCode(...new Uint8Array(p256dhKey))) : '',
        auth: authKey ? btoa(String.fromCharCode(...new Uint8Array(authKey))) : ''
      },
      userId: this.getCurrentUserId() || 'anonymous',
      deviceId: this.getDeviceId(),
      createdAt: new Date().toISOString()
    };
  }

  /**
   * Send subscription to backend
   */
  private async sendSubscriptionToBackend(subscriptionData: PushSubscriptionData): Promise<void> {
    try {
      const response = await fetch('/api/notifications/subscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        },
        body: JSON.stringify(subscriptionData)
      });

      if (!response.ok) {
        throw new Error('Failed to save subscription to backend');
      }

      console.log('Push subscription saved to backend');
    } catch (error) {
      console.error('Failed to send subscription to backend:', error);
      // Don't throw - continue with local functionality
    }
  }

  /**
   * Remove subscription from backend
   */
  private async removeSubscriptionFromBackend(): Promise<void> {
    try {
      const response = await fetch('/api/notifications/unsubscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        },
        body: JSON.stringify({
          userId: this.getCurrentUserId(),
          deviceId: this.getDeviceId()
        })
      });

      if (!response.ok) {
        throw new Error('Failed to remove subscription from backend');
      }

      console.log('Push subscription removed from backend');
    } catch (error) {
      console.error('Failed to remove subscription from backend:', error);
    }
  }

  /**
   * Send schedule request to backend
   */
  private async sendScheduleRequest(alarmId: string, scheduleTime: string, payload: NotificationPayload): Promise<void> {
    const response = await fetch('/api/notifications/schedule', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify({
        alarmId,
        scheduleTime,
        payload,
        userId: this.getCurrentUserId()
      })
    });

    if (!response.ok) {
      throw new Error('Failed to schedule notification');
    }
  }

  /**
   * Send cancel request to backend
   */
  private async sendCancelRequest(alarmId: string): Promise<void> {
    const response = await fetch(`/api/notifications/cancel/${alarmId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`
      }
    });

    if (!response.ok) {
      throw new Error('Failed to cancel notification');
    }
  }

  private getAuthToken(): string | null {
    try {
      const authData = JSON.parse(localStorage.getItem('smart-alarm-auth') || '{}');
      return authData?.state?.token || null;
    } catch {
      return null;
    }
  }

  private getCurrentUserId(): string | null {
    try {
      const authData = JSON.parse(localStorage.getItem('smart-alarm-auth') || '{}');
      return authData?.state?.user?.id || null;
    } catch {
      return null;
    }
  }

  private getDeviceId(): string {
    const stored = localStorage.getItem('smart-alarm-device-id');
    if (stored) return stored;
    
    const deviceId = `device-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    localStorage.setItem('smart-alarm-device-id', deviceId);
    return deviceId;
  }

  /**
   * Get current subscription status
   */
  public async getSubscriptionStatus(): Promise<{
    isSubscribed: boolean;
    subscription: PushSubscriptionData | null;
    permission: NotificationPermission;
  }> {
    if (!this.swRegistration) {
      await this.initializeServiceWorker();
    }

    const subscription = await this.swRegistration?.pushManager.getSubscription();
    
    return {
      isSubscribed: !!subscription,
      subscription: subscription ? this.createSubscriptionData(subscription) : null,
      permission: Notification.permission
    };
  }
}

// Create singleton instance
export const pushNotificationManager = PushNotificationManager.getInstance();

// React hooks for components
export function usePushNotifications() {
  return {
    getPermissionStatus: () => pushNotificationManager.getPermissionStatus(),
    requestPermission: () => pushNotificationManager.requestPermission(),
    subscribe: () => pushNotificationManager.subscribeToPush(),
    unsubscribe: () => pushNotificationManager.unsubscribe(),
    getSubscriptionStatus: () => pushNotificationManager.getSubscriptionStatus(),
    showNotification: (payload: NotificationPayload) => pushNotificationManager.showLocalNotification(payload),
    scheduleAlarm: (alarmId: string, time: string, name: string) => 
      pushNotificationManager.scheduleAlarmNotification(alarmId, time, name),
    cancelAlarm: (alarmId: string) => pushNotificationManager.cancelAlarmNotification(alarmId),
    scheduleReminder: (message: string, delayMinutes?: number) => 
      pushNotificationManager.scheduleOptimizationReminder(message, delayMinutes)
  };
}