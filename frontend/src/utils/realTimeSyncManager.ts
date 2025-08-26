// Real-time Alarm Status Synchronization Manager
import { signalRManager, type AlarmEvent, type SyncEvent } from './signalRConnection';
import { pushNotificationManager } from './pushNotifications';
import { mlDataCollector } from './mlDataCollector';
import { useAlarmsStore } from '@/stores/alarmsStore';
import { useAuthStore } from '@/stores/authStore';
import { backgroundSync } from './backgroundSync';

export interface AlarmSyncStatus {
  alarmId: string;
  lastSyncTime: string;
  syncState: 'synced' | 'pending' | 'conflict' | 'error';
  version: number;
  deviceLastModified: string;
}

export interface DeviceStatus {
  deviceId: string;
  isOnline: boolean;
  lastSeen: string;
  syncVersion: number;
}

export interface MultiDeviceState {
  devices: Map<string, DeviceStatus>;
  conflicts: AlarmConflict[];
  masterDevice?: string;
  lastFullSync: string;
}

export interface AlarmConflict {
  alarmId: string;
  conflictType: 'time_change' | 'enable_disable' | 'delete' | 'create';
  devices: {
    deviceId: string;
    version: number;
    timestamp: string;
    data: any;
  }[];
  resolvedBy?: string;
  resolvedAt?: string;
}

class RealTimeSyncManager {
  private static instance: RealTimeSyncManager;
  private syncStatus: Map<string, AlarmSyncStatus> = new Map();
  private multiDeviceState: MultiDeviceState;
  private isInitialized = false;
  private syncInProgress = false;
  private presenceTimer: NodeJS.Timeout | null = null;

  private constructor() {
    this.multiDeviceState = {
      devices: new Map(),
      conflicts: [],
      lastFullSync: new Date().toISOString()
    };
  }

  public static getInstance(): RealTimeSyncManager {
    if (!RealTimeSyncManager.instance) {
      RealTimeSyncManager.instance = new RealTimeSyncManager();
    }
    return RealTimeSyncManager.instance;
  }

  /**
   * Initialize real-time synchronization
   */
  public async initialize(): Promise<void> {
    if (this.isInitialized) return;

    try {
      // Initialize SignalR connection
      await signalRManager.initialize();
      
      // Setup event handlers
      this.setupSignalREventHandlers();
      this.setupPresenceTracking();
      this.setupConflictResolution();

      // Load existing sync status
      this.loadSyncStatus();

      // Perform initial sync
      await this.performFullSync();

      this.isInitialized = true;
      console.log('Real-time sync manager initialized');

    } catch (error) {
      console.error('Failed to initialize real-time sync manager:', error);
      throw error;
    }
  }

  /**
   * Setup SignalR event handlers
   */
  private setupSignalREventHandlers(): void {
    // Alarm events from other devices
    signalRManager.addEventListener('alarmUpdated', this.handleRemoteAlarmUpdate.bind(this));
    signalRManager.addEventListener('alarmTriggered', this.handleRemoteAlarmTrigger.bind(this));
    signalRManager.addEventListener('alarmDismissed', this.handleRemoteAlarmDismissal.bind(this));
    signalRManager.addEventListener('alarmSnoozed', this.handleRemoteAlarmSnooze.bind(this));

    // Sync events
    signalRManager.addEventListener('syncComplete', this.handleSyncComplete.bind(this));
    signalRManager.addEventListener('deviceSync', this.handleDeviceSync.bind(this));

    // Connection status changes
    signalRManager.addEventListener('connectionStatusChanged', this.handleConnectionChange.bind(this));
  }

  /**
   * Setup user presence tracking
   */
  private setupPresenceTracking(): void {
    // Update presence every 30 seconds
    this.presenceTimer = setInterval(async () => {
      try {
        await signalRManager.updatePresence(true);
      } catch (error) {
        console.debug('Failed to update presence:', error);
      }
    }, 30000);

    // Update presence on visibility change
    document.addEventListener('visibilitychange', () => {
      signalRManager.updatePresence(!document.hidden);
    });

    // Update presence on window focus/blur
    window.addEventListener('focus', () => signalRManager.updatePresence(true));
    window.addEventListener('blur', () => signalRManager.updatePresence(false));
  }

  /**
   * Setup conflict resolution system
   */
  private setupConflictResolution(): void {
    // Check for conflicts every 5 minutes
    setInterval(() => {
      this.resolveConflicts();
    }, 5 * 60 * 1000);
  }

  /**
   * Handle remote alarm updates
   */
  private async handleRemoteAlarmUpdate(event: AlarmEvent): Promise<void> {
    if (this.syncInProgress) return;

    try {
      const alarmsStore = useAlarmsStore.getState();
      const localAlarm = alarmsStore.getAlarmById(event.alarmId);

      if (!localAlarm) {
        // New alarm from another device
        await this.handleNewRemoteAlarm(event);
        return;
      }

      // Check for conflicts
      const syncStatus = this.syncStatus.get(event.alarmId);
      if (syncStatus && this.hasConflict(event, syncStatus)) {
        await this.handleAlarmConflict(event, localAlarm, syncStatus);
        return;
      }

      // Update local alarm
      await this.applyRemoteUpdate(event, localAlarm);

      // Update sync status
      this.updateSyncStatus(event.alarmId, {
        lastSyncTime: event.timestamp,
        syncState: 'synced',
        version: syncStatus ? syncStatus.version + 1 : 1,
        deviceLastModified: event.data?.deviceId || 'unknown'
      });

    } catch (error) {
      console.error('Failed to handle remote alarm update:', error);
    }
  }

  /**
   * Handle remote alarm trigger
   */
  private async handleRemoteAlarmTrigger(event: AlarmEvent): Promise<void> {
    try {
      // Track the alarm trigger in ML data
      mlDataCollector.trackAlarmDismissed(
        event.alarmId,
        event.data?.originalTime || '',
        event.timestamp,
        'button', // Default dismissal method
        0 // Unknown response time for remote triggers
      );

      // Show notification if this device didn't trigger the alarm
      const deviceId = this.getDeviceId();
      if (event.data?.deviceId !== deviceId) {
        await pushNotificationManager.showLocalNotification({
          title: 'Alarm Active on Another Device',
          body: 'Your alarm is ringing on another device',
          tag: `remote-alarm-${event.alarmId}`,
          data: {
            alarmId: event.alarmId,
            actionType: 'alarm_trigger'
          }
        });
      }

      // Update local alarm state if needed
      const alarmsStore = useAlarmsStore.getState();
      const alarm = alarmsStore.getAlarmById(event.alarmId);
      if (alarm) {
        // Mark alarm as triggered (you might want to add this state to your store)
        console.log(`Alarm ${event.alarmId} triggered on remote device`);
      }

    } catch (error) {
      console.error('Failed to handle remote alarm trigger:', error);
    }
  }

  /**
   * Handle remote alarm dismissal
   */
  private async handleRemoteAlarmDismissal(event: AlarmEvent): Promise<void> {
    try {
      // Cancel local notification if it exists
      await pushNotificationManager.cancelAlarmNotification(event.alarmId);

      // Track dismissal in ML data
      mlDataCollector.trackAlarmDismissed(
        event.alarmId,
        event.data?.originalTime || '',
        event.timestamp,
        event.data?.dismissalMethod || 'button',
        event.data?.responseTime || 0
      );

      // Show notification about dismissal on other devices
      const deviceId = this.getDeviceId();
      if (event.data?.deviceId !== deviceId) {
        await pushNotificationManager.showLocalNotification({
          title: 'Alarm Dismissed',
          body: 'Your alarm was dismissed on another device',
          tag: `alarm-dismissed-${event.alarmId}`,
          data: {
            alarmId: event.alarmId,
            actionType: 'alarm_trigger'
          }
        });
      }

    } catch (error) {
      console.error('Failed to handle remote alarm dismissal:', error);
    }
  }

  /**
   * Handle remote alarm snooze
   */
  private async handleRemoteAlarmSnooze(event: AlarmEvent): Promise<void> {
    try {
      // Track snooze in ML data
      mlDataCollector.trackAlarmSnoozed(
        event.alarmId,
        event.data?.snoozeCount || 1
      );

      // Show notification about snooze on other devices
      const deviceId = this.getDeviceId();
      if (event.data?.deviceId !== deviceId) {
        await pushNotificationManager.showLocalNotification({
          title: 'Alarm Snoozed',
          body: `Your alarm was snoozed for 5 minutes (${event.data?.snoozeCount || 1} time(s))`,
          tag: `alarm-snoozed-${event.alarmId}`,
          data: {
            alarmId: event.alarmId,
            actionType: 'alarm_trigger'
          }
        });
      }

    } catch (error) {
      console.error('Failed to handle remote alarm snooze:', error);
    }
  }

  /**
   * Handle sync completion events
   */
  private async handleSyncComplete(event: SyncEvent): Promise<void> {
    if (event.deviceId === this.getDeviceId()) {
      // This device completed sync
      console.log('Local sync completed');
      this.multiDeviceState.lastFullSync = event.timestamp;
    } else {
      // Another device completed sync
      console.log('Remote device sync completed:', event.deviceId);
      await this.requestPartialSync();
    }
  }

  /**
   * Handle device sync events
   */
  private async handleDeviceSync(event: SyncEvent): Promise<void> {
    const device = this.multiDeviceState.devices.get(event.deviceId);
    if (device) {
      device.lastSeen = event.timestamp;
      device.syncVersion++;
    } else {
      this.multiDeviceState.devices.set(event.deviceId, {
        deviceId: event.deviceId,
        isOnline: true,
        lastSeen: event.timestamp,
        syncVersion: 1
      });
    }

    // Check if we need to sync with this device
    if (this.needsSync(event)) {
      await this.requestPartialSync();
    }
  }

  /**
   * Handle connection status changes
   */
  private handleConnectionChange(status: any): void {
    console.log('SignalR connection status changed:', status);
    
    if (status.isConnected) {
      // Reconnected - request sync
      this.requestPartialSync();
    }
  }

  /**
   * Perform full synchronization
   */
  public async performFullSync(): Promise<void> {
    if (this.syncInProgress) return;

    this.syncInProgress = true;
    
    try {
      console.log('Starting full sync...');
      
      // Get local alarms
      const alarmsStore = useAlarmsStore.getState();
      const localAlarms = alarmsStore.alarms;

      // Request sync from SignalR hub
      await signalRManager.requestSync('all');

      // Sync with background sync queue
      await backgroundSync.processQueue();

      // Update sync status for all alarms
      localAlarms.forEach(alarm => {
        this.updateSyncStatus(alarm.id, {
          lastSyncTime: new Date().toISOString(),
          syncState: 'synced',
          version: (this.syncStatus.get(alarm.id)?.version || 0) + 1,
          deviceLastModified: this.getDeviceId()
        });
      });

      // Send sync completion event
      await signalRManager.sendSyncEvent({
        type: 'alarm_sync',
        data: {
          syncedAlarms: localAlarms.length,
          syncType: 'full'
        }
      });

      this.multiDeviceState.lastFullSync = new Date().toISOString();
      this.saveSyncStatus();

      console.log('Full sync completed');

    } catch (error) {
      console.error('Full sync failed:', error);
    } finally {
      this.syncInProgress = false;
    }
  }

  /**
   * Request partial sync
   */
  private async requestPartialSync(): Promise<void> {
    if (this.syncInProgress) return;

    try {
      await signalRManager.requestSync('alarms');
    } catch (error) {
      console.error('Failed to request partial sync:', error);
    }
  }

  /**
   * Send alarm update to other devices
   */
  public async sendAlarmUpdate(alarmId: string, updateType: AlarmEvent['type'], data?: any): Promise<void> {
    try {
      await signalRManager.sendAlarmEvent({
        alarmId,
        type: updateType,
        data: {
          ...data,
          deviceId: this.getDeviceId()
        }
      });

      // Update local sync status
      this.updateSyncStatus(alarmId, {
        lastSyncTime: new Date().toISOString(),
        syncState: 'synced',
        version: (this.syncStatus.get(alarmId)?.version || 0) + 1,
        deviceLastModified: this.getDeviceId()
      });

    } catch (error) {
      console.error('Failed to send alarm update:', error);
      
      // Mark as pending sync
      this.updateSyncStatus(alarmId, {
        lastSyncTime: new Date().toISOString(),
        syncState: 'pending',
        version: (this.syncStatus.get(alarmId)?.version || 0) + 1,
        deviceLastModified: this.getDeviceId()
      });
    }
  }

  /**
   * Handle new alarm from remote device
   */
  private async handleNewRemoteAlarm(event: AlarmEvent): Promise<void> {
    try {
      // This would require more detailed alarm data in the event
      console.log('New remote alarm detected:', event.alarmId);
      
      // In a real implementation, you'd fetch the full alarm data
      // and add it to the local store
      await this.requestPartialSync();
      
    } catch (error) {
      console.error('Failed to handle new remote alarm:', error);
    }
  }

  /**
   * Apply remote update to local alarm
   */
  private async applyRemoteUpdate(event: AlarmEvent, localAlarm: any): Promise<void> {
    try {
      const alarmsStore = useAlarmsStore.getState();
      
      // Apply the update based on event type
      switch (event.type) {
        case 'updated':
          // Update alarm properties (would need more detailed data)
          console.log('Applying remote alarm update:', event.alarmId);
          break;
          
        case 'enabled':
          await alarmsStore.updateAlarm(event.alarmId, { isEnabled: true });
          break;
          
        case 'disabled':
          await alarmsStore.updateAlarm(event.alarmId, { isEnabled: false });
          break;
          
        case 'deleted':
          await alarmsStore.removeAlarm(event.alarmId);
          break;
      }
      
    } catch (error) {
      console.error('Failed to apply remote update:', error);
    }
  }

  /**
   * Check if there's a conflict
   */
  private hasConflict(event: AlarmEvent, syncStatus: AlarmSyncStatus): boolean {
    // Simple conflict detection - in production you'd have more sophisticated logic
    const eventTime = new Date(event.timestamp);
    const lastSyncTime = new Date(syncStatus.lastSyncTime);
    
    // If both devices modified the alarm within 30 seconds, it's potentially a conflict
    return Math.abs(eventTime.getTime() - lastSyncTime.getTime()) < 30000;
  }

  /**
   * Handle alarm conflict
   */
  private async handleAlarmConflict(event: AlarmEvent, localAlarm: any, syncStatus: AlarmSyncStatus): Promise<void> {
    const conflict: AlarmConflict = {
      alarmId: event.alarmId,
      conflictType: event.type as any,
      devices: [
        {
          deviceId: this.getDeviceId(),
          version: syncStatus.version,
          timestamp: syncStatus.lastSyncTime,
          data: localAlarm
        },
        {
          deviceId: event.data?.deviceId || 'unknown',
          version: syncStatus.version + 1,
          timestamp: event.timestamp,
          data: event.data
        }
      ]
    };

    this.multiDeviceState.conflicts.push(conflict);
    
    // Mark as conflict state
    this.updateSyncStatus(event.alarmId, {
      ...syncStatus,
      syncState: 'conflict'
    });

    console.warn('Alarm conflict detected:', conflict);
    
    // Auto-resolve using last-writer-wins for now
    await this.autoResolveConflict(conflict);
  }

  /**
   * Auto-resolve conflict using last-writer-wins
   */
  private async autoResolveConflict(conflict: AlarmConflict): Promise<void> {
    try {
      // Sort devices by timestamp (most recent first)
      const sortedDevices = conflict.devices.sort((a, b) => 
        new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()
      );

      const winner = sortedDevices[0];
      
      // Apply the winning change
      if (winner.deviceId !== this.getDeviceId()) {
        // Remote device won - apply their changes
        await this.applyRemoteUpdate({
          alarmId: conflict.alarmId,
          type: conflict.conflictType as any,
          timestamp: winner.timestamp,
          userId: '', // Would be filled in real implementation
          data: winner.data
        }, null);
      }

      // Mark conflict as resolved
      conflict.resolvedBy = winner.deviceId;
      conflict.resolvedAt = new Date().toISOString();

      // Update sync status
      this.updateSyncStatus(conflict.alarmId, {
        lastSyncTime: new Date().toISOString(),
        syncState: 'synced',
        version: Math.max(...conflict.devices.map(d => d.version)),
        deviceLastModified: winner.deviceId
      });

      console.log('Conflict auto-resolved for alarm:', conflict.alarmId);

    } catch (error) {
      console.error('Failed to resolve conflict:', error);
      
      // Mark as error state
      this.updateSyncStatus(conflict.alarmId, {
        lastSyncTime: new Date().toISOString(),
        syncState: 'error',
        version: 0,
        deviceLastModified: this.getDeviceId()
      });
    }
  }

  /**
   * Resolve all pending conflicts
   */
  private async resolveConflicts(): Promise<void> {
    const unresolvedConflicts = this.multiDeviceState.conflicts.filter(c => !c.resolvedBy);
    
    for (const conflict of unresolvedConflicts) {
      await this.autoResolveConflict(conflict);
    }
  }

  /**
   * Check if sync is needed based on event
   */
  private needsSync(event: SyncEvent): boolean {
    // Simple heuristic - sync if the event is recent and from a different device
    const eventTime = new Date(event.timestamp);
    const now = new Date();
    const timeDiff = now.getTime() - eventTime.getTime();
    
    return timeDiff < 60000 && event.deviceId !== this.getDeviceId();
  }

  /**
   * Update sync status for alarm
   */
  private updateSyncStatus(alarmId: string, status: Partial<AlarmSyncStatus>): void {
    const existing = this.syncStatus.get(alarmId);
    this.syncStatus.set(alarmId, {
      alarmId,
      lastSyncTime: existing?.lastSyncTime || new Date().toISOString(),
      syncState: existing?.syncState || 'synced',
      version: existing?.version || 1,
      deviceLastModified: existing?.deviceLastModified || this.getDeviceId(),
      ...status
    });
  }

  /**
   * Load sync status from storage
   */
  private loadSyncStatus(): void {
    try {
      const stored = localStorage.getItem('alarm-sync-status');
      if (stored) {
        const data = JSON.parse(stored);
        this.syncStatus = new Map(data.syncStatus || []);
        this.multiDeviceState = { ...this.multiDeviceState, ...data.multiDeviceState };
      }
    } catch (error) {
      console.error('Failed to load sync status:', error);
    }
  }

  /**
   * Save sync status to storage
   */
  private saveSyncStatus(): void {
    try {
      const data = {
        syncStatus: Array.from(this.syncStatus.entries()),
        multiDeviceState: {
          ...this.multiDeviceState,
          devices: Array.from(this.multiDeviceState.devices.entries())
        }
      };
      localStorage.setItem('alarm-sync-status', JSON.stringify(data));
    } catch (error) {
      console.error('Failed to save sync status:', error);
    }
  }

  /**
   * Get sync status for all alarms
   */
  public getSyncStatus(): Map<string, AlarmSyncStatus> {
    return new Map(this.syncStatus);
  }

  /**
   * Get multi-device state
   */
  public getMultiDeviceState(): MultiDeviceState {
    return { ...this.multiDeviceState };
  }

  /**
   * Cleanup resources
   */
  public async cleanup(): Promise<void> {
    if (this.presenceTimer) {
      clearInterval(this.presenceTimer);
      this.presenceTimer = null;
    }

    await signalRManager.disconnect();
    this.saveSyncStatus();
    this.isInitialized = false;
  }

  private getDeviceId(): string {
    const stored = localStorage.getItem('smart-alarm-device-id');
    if (stored) return stored;
    
    const deviceId = `device-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
    localStorage.setItem('smart-alarm-device-id', deviceId);
    return deviceId;
  }
}

// Create singleton instance
export const realTimeSyncManager = RealTimeSyncManager.getInstance();

// React hooks for components
export function useRealTimeSync() {
  return {
    initialize: () => realTimeSyncManager.initialize(),
    performFullSync: () => realTimeSyncManager.performFullSync(),
    sendAlarmUpdate: (alarmId: string, type: AlarmEvent['type'], data?: any) => 
      realTimeSyncManager.sendAlarmUpdate(alarmId, type, data),
    getSyncStatus: () => realTimeSyncManager.getSyncStatus(),
    getMultiDeviceState: () => realTimeSyncManager.getMultiDeviceState(),
    cleanup: () => realTimeSyncManager.cleanup()
  };
}