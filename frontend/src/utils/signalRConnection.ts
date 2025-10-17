// SignalR Real-time Communication Hub
import * as signalR from '@microsoft/signalr';
// import { useAuthStore } from '@/stores/authStore';

export interface AlarmEvent {
  alarmId: string;
  userId: string;
  type: 'triggered' | 'dismissed' | 'snoozed' | 'created' | 'updated' | 'deleted' | 'enabled' | 'disabled';
  timestamp: string;
  data?: {
    snoozeCount?: number;
    dismissalMethod?: 'button' | 'voice' | 'gesture' | 'timeout';
    responseTime?: number;
    deviceId?: string;
    location?: string;
  };
}

export interface SyncEvent {
  userId: string;
  type: 'alarm_sync' | 'settings_sync' | 'ml_data_sync' | 'user_presence';
  timestamp: string;
  deviceId: string;
  data?: any;
}

export interface NotificationEvent {
  userId: string;
  type: 'alarm_reminder' | 'optimization_suggestion' | 'sync_complete' | 'system_update';
  title: string;
  message: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  timestamp: string;
  actionable?: boolean;
  actionUrl?: string;
}

export interface ConnectionStatus {
  isConnected: boolean;
  connectionId?: string;
  lastConnected?: string;
  reconnectAttempts: number;
  deviceId: string;
}

class SignalRConnectionManager {
  private static instance: SignalRConnectionManager;
  private connection: signalR.HubConnection | null = null;
  private connectionStatus: ConnectionStatus;
  private reconnectTimer: NodeJS.Timeout | null = null;
  private eventHandlers: Map<string, Function[]> = new Map();
  private readonly MAX_RECONNECT_ATTEMPTS = 5;
  private readonly RECONNECT_INTERVAL = 5000; // 5 seconds

  private constructor() {
    this.connectionStatus = {
      isConnected: false,
      reconnectAttempts: 0,
      deviceId: this.generateDeviceId()
    };
  }

  public static getInstance(): SignalRConnectionManager {
    if (!SignalRConnectionManager.instance) {
      SignalRConnectionManager.instance = new SignalRConnectionManager();
    }
    return SignalRConnectionManager.instance;
  }

  private generateDeviceId(): string {
    const stored = localStorage.getItem('smart-alarm-device-id');
    if (stored) return stored;

    const deviceId = `device-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    localStorage.setItem('smart-alarm-device-id', deviceId);
    return deviceId;
  }

  /**
   * Initialize SignalR connection
   */
  public async initialize(): Promise<void> {
    if (this.connection) {
      await this.disconnect();
    }

    const authToken = this.getAuthToken();
    if (!authToken) {
      console.warn('No auth token available for SignalR connection');
      return;
    }

    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL || 'http://localhost:5000/smartalarmhub';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => authToken,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.ServerSentEvents,
        logger: import.meta.env.DEV ? signalR.LogLevel.Information : signalR.LogLevel.Warning
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff with jitter
          const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 30000);
          return delay + Math.random() * 1000;
        }
      })
      .build();

    this.setupConnectionEventHandlers();
    this.setupHubMethodHandlers();

    try {
      await this.connection.start();
      await this.registerDevice();
    } catch (error) {
      console.error('Failed to start SignalR connection:', error);
      this.scheduleReconnect();
    }
  }

  /**
   * Setup connection event handlers
   */
  private setupConnectionEventHandlers(): void {
    if (!this.connection) return;

    this.connection.onclose((error) => {
      console.warn('SignalR connection closed:', error);
      this.updateConnectionStatus(false);
      this.scheduleReconnect();
    });

    this.connection.onreconnecting((error) => {
      console.log('SignalR reconnecting...', error);
      this.updateConnectionStatus(false);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with ID:', connectionId);
      this.updateConnectionStatus(true, connectionId);
      this.registerDevice();
      this.connectionStatus.reconnectAttempts = 0;
    });
  }

  /**
   * Setup hub method handlers for incoming events
   */
  private setupHubMethodHandlers(): void {
    if (!this.connection) return;

    // Alarm events
    this.connection.on('AlarmTriggered', (event: AlarmEvent) => {
      console.log('Alarm triggered:', event);
      this.emitEvent('alarmTriggered', event);
      this.showNotification({
        userId: event.userId,
        type: 'alarm_reminder',
        title: 'Alarm Triggered!',
        message: `Your alarm is ringing. Time to wake up!`,
        priority: 'urgent',
        timestamp: event.timestamp,
        actionable: true
      });
    });

    this.connection.on('AlarmDismissed', (event: AlarmEvent) => {
      console.log('Alarm dismissed:', event);
      this.emitEvent('alarmDismissed', event);
    });

    this.connection.on('AlarmSnoozed', (event: AlarmEvent) => {
      console.log('Alarm snoozed:', event);
      this.emitEvent('alarmSnoozed', event);
    });

    this.connection.on('AlarmUpdated', (event: AlarmEvent) => {
      console.log('Alarm updated:', event);
      this.emitEvent('alarmUpdated', event);
    });

    // Sync events
    this.connection.on('SyncComplete', (event: SyncEvent) => {
      console.log('Sync complete:', event);
      this.emitEvent('syncComplete', event);
    });

    this.connection.on('DeviceSync', (event: SyncEvent) => {
      console.log('Device sync event:', event);
      this.emitEvent('deviceSync', event);
    });

    // Notification events
    this.connection.on('SystemNotification', (notification: NotificationEvent) => {
      console.log('System notification:', notification);
      this.emitEvent('systemNotification', notification);
      this.showNotification(notification);
    });

    // User presence events
    this.connection.on('UserPresenceUpdate', (data: { userId: string; isOnline: boolean; lastSeen: string }) => {
      console.log('User presence update:', data);
      this.emitEvent('userPresenceUpdate', data);
    });
  }

  /**
   * Register device with the hub
   */
  private async registerDevice(): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    try {
      const userId = this.getCurrentUserId();
      if (!userId) return;

      await this.connection.invoke('RegisterDevice', {
        userId,
        deviceId: this.connectionStatus.deviceId,
        deviceType: this.getDeviceType(),
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
      });

      console.log('Device registered with SignalR hub');
    } catch (error) {
      console.error('Failed to register device:', error);
    }
  }

  /**
   * Send alarm event to hub
   */
  public async sendAlarmEvent(event: Omit<AlarmEvent, 'userId' | 'timestamp'>): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      console.warn('SignalR not connected, queuing alarm event');
      // Could implement offline queue here
      return;
    }

    const userId = this.getCurrentUserId();
    if (!userId) return;

    const fullEvent: AlarmEvent = {
      ...event,
      userId,
      timestamp: new Date().toISOString()
    };

    try {
      await this.connection.invoke('SendAlarmEvent', fullEvent);
    } catch (error) {
      console.error('Failed to send alarm event:', error);
    }
  }

  /**
   * Send sync event to hub
   */
  public async sendSyncEvent(event: Omit<SyncEvent, 'userId' | 'timestamp' | 'deviceId'>): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    const userId = this.getCurrentUserId();
    if (!userId) return;

    const fullEvent: SyncEvent = {
      ...event,
      userId,
      timestamp: new Date().toISOString(),
      deviceId: this.connectionStatus.deviceId
    };

    try {
      await this.connection.invoke('SendSyncEvent', fullEvent);
    } catch (error) {
      console.error('Failed to send sync event:', error);
    }
  }

  /**
   * Request immediate sync from server
   */
  public async requestSync(syncType: 'alarms' | 'settings' | 'ml_data' | 'all' = 'all'): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    const userId = this.getCurrentUserId();
    if (!userId) return;

    try {
      await this.connection.invoke('RequestSync', {
        userId,
        syncType,
        deviceId: this.connectionStatus.deviceId,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      console.error('Failed to request sync:', error);
    }
  }

  /**
   * Update user presence status
   */
  public async updatePresence(isActive: boolean): Promise<void> {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return;
    }

    const userId = this.getCurrentUserId();
    if (!userId) return;

    try {
      await this.connection.invoke('UpdatePresence', {
        userId,
        isActive,
        deviceId: this.connectionStatus.deviceId,
        timestamp: new Date().toISOString()
      });
    } catch (error) {
      console.error('Failed to update presence:', error);
    }
  }

  /**
   * Disconnect from SignalR hub
   */
  public async disconnect(): Promise<void> {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }

    if (this.connection) {
      try {
        await this.connection.stop();
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      }
      this.connection = null;
    }

    this.updateConnectionStatus(false);
  }

  /**
   * Get current connection status
   */
  public getConnectionStatus(): ConnectionStatus {
    return { ...this.connectionStatus };
  }

  /**
   * Add event listener
   */
  public addEventListener(event: string, handler: Function): void {
    if (!this.eventHandlers.has(event)) {
      this.eventHandlers.set(event, []);
    }
    this.eventHandlers.get(event)!.push(handler);
  }

  /**
   * Remove event listener
   */
  public removeEventListener(event: string, handler: Function): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  /**
   * Emit event to registered listeners
   */
  private emitEvent(event: string, data: any): void {
    const handlers = this.eventHandlers.get(event);
    if (handlers) {
      handlers.forEach(handler => {
        try {
          handler(data);
        } catch (error) {
          console.error(`Error in event handler for ${event}:`, error);
        }
      });
    }
  }

  private updateConnectionStatus(isConnected: boolean, connectionId?: string): void {
    this.connectionStatus.isConnected = isConnected;
    this.connectionStatus.connectionId = connectionId;
    this.connectionStatus.lastConnected = isConnected ? new Date().toISOString() : this.connectionStatus.lastConnected;

    this.emitEvent('connectionStatusChanged', this.connectionStatus);
  }

  private scheduleReconnect(): void {
    if (this.connectionStatus.reconnectAttempts >= this.MAX_RECONNECT_ATTEMPTS) {
      console.warn('Max reconnection attempts reached');
      return;
    }

    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
    }

    const delay = this.RECONNECT_INTERVAL * Math.pow(2, this.connectionStatus.reconnectAttempts);

    this.reconnectTimer = setTimeout(async () => {
      this.connectionStatus.reconnectAttempts++;
      console.log(`SignalR reconnection attempt ${this.connectionStatus.reconnectAttempts}`);

      try {
        await this.initialize();
      } catch (error) {
        console.error('Reconnection failed:', error);
        this.scheduleReconnect();
      }
    }, delay);
  }

  private showNotification(notification: NotificationEvent): void {
    // Show browser notification if permission granted
    if ('Notification' in window && Notification.permission === 'granted') {
      try {
        new Notification(notification.title, {
          body: notification.message,
          icon: '/pwa-192x192.png',
          badge: '/pwa-192x192.png',
          tag: `smart-alarm-${notification.type}`,
          requireInteraction: notification.priority === 'urgent',
          silent: notification.priority === 'low'
        });
      } catch (error) {
        console.error('Failed to show notification:', error);
      }
    }

    // Also emit as event for in-app handling
    this.emitEvent('notification', notification);
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

  private getDeviceType(): string {
    const ua = navigator.userAgent;
    if (/Mobile|Android|iPhone|iPad/.test(ua)) return 'mobile';
    if (/Tablet|iPad/.test(ua)) return 'tablet';
    return 'desktop';
  }
}

// Create singleton instance
export const signalRManager = SignalRConnectionManager.getInstance();

// React hooks for components
export function useSignalRConnection() {
  return {
    connect: () => signalRManager.initialize(),
    disconnect: () => signalRManager.disconnect(),
    getStatus: () => signalRManager.getConnectionStatus(),
    sendAlarmEvent: (event: Omit<AlarmEvent, 'userId' | 'timestamp'>) =>
      signalRManager.sendAlarmEvent(event),
    sendSyncEvent: (event: Omit<SyncEvent, 'userId' | 'timestamp' | 'deviceId'>) =>
      signalRManager.sendSyncEvent(event),
    requestSync: (type?: 'alarms' | 'settings' | 'ml_data' | 'all') =>
      signalRManager.requestSync(type),
    updatePresence: (isActive: boolean) => signalRManager.updatePresence(isActive),
    addEventListener: (event: string, handler: Function) =>
      signalRManager.addEventListener(event, handler),
    removeEventListener: (event: string, handler: Function) =>
      signalRManager.removeEventListener(event, handler)
  };
}

export function useRealTimeAlarmEvents() {
  const { addEventListener, removeEventListener } = useSignalRConnection();

  const subscribeToAlarmEvents = (callbacks: {
    onAlarmTriggered?: (event: AlarmEvent) => void;
    onAlarmDismissed?: (event: AlarmEvent) => void;
    onAlarmSnoozed?: (event: AlarmEvent) => void;
    onAlarmUpdated?: (event: AlarmEvent) => void;
  }) => {
    if (callbacks.onAlarmTriggered) {
      addEventListener('alarmTriggered', callbacks.onAlarmTriggered);
    }
    if (callbacks.onAlarmDismissed) {
      addEventListener('alarmDismissed', callbacks.onAlarmDismissed);
    }
    if (callbacks.onAlarmSnoozed) {
      addEventListener('alarmSnoozed', callbacks.onAlarmSnoozed);
    }
    if (callbacks.onAlarmUpdated) {
      addEventListener('alarmUpdated', callbacks.onAlarmUpdated);
    }

    // Return cleanup function
    return () => {
      if (callbacks.onAlarmTriggered) {
        removeEventListener('alarmTriggered', callbacks.onAlarmTriggered);
      }
      if (callbacks.onAlarmDismissed) {
        removeEventListener('alarmDismissed', callbacks.onAlarmDismissed);
      }
      if (callbacks.onAlarmSnoozed) {
        removeEventListener('alarmSnoozed', callbacks.onAlarmSnoozed);
      }
      if (callbacks.onAlarmUpdated) {
        removeEventListener('alarmUpdated', callbacks.onAlarmUpdated);
      }
    };
  };

  return { subscribeToAlarmEvents };
}
