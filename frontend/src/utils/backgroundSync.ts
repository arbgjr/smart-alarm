// Background sync utility for offline data synchronization
interface SyncData {
  id: string;
  action: 'create' | 'update' | 'delete';
  entity: 'alarm' | 'routine';
  data: any;
  timestamp: number;
}

const SYNC_QUEUE_KEY = 'smart-alarm-sync-queue';
const SYNC_TAG = 'smart-alarm-background-sync';

export class BackgroundSync {
  private static instance: BackgroundSync;
  private syncQueue: SyncData[] = [];

  private constructor() {
    this.loadSyncQueue();
  }

  public static getInstance(): BackgroundSync {
    if (!BackgroundSync.instance) {
      BackgroundSync.instance = new BackgroundSync();
    }
    return BackgroundSync.instance;
  }

  // Add operation to sync queue
  public addToSyncQueue(
    action: SyncData['action'],
    entity: SyncData['entity'],
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

    // Try to sync immediately if online
    if (navigator.onLine) {
      this.processQueue();
    } else {
      // Register background sync if supported
      this.registerBackgroundSync();
    }
  }

  // Load sync queue from localStorage
  private loadSyncQueue(): void {
    try {
      const stored = localStorage.getItem(SYNC_QUEUE_KEY);
      this.syncQueue = stored ? JSON.parse(stored) : [];
    } catch (error) {
      console.error('Failed to load sync queue:', error);
      this.syncQueue = [];
    }
  }

  // Save sync queue to localStorage
  private saveSyncQueue(): void {
    try {
      localStorage.setItem(SYNC_QUEUE_KEY, JSON.stringify(this.syncQueue));
    } catch (error) {
      console.error('Failed to save sync queue:', error);
    }
  }

  // Register background sync
  private async registerBackgroundSync(): Promise<void> {
    if ('serviceWorker' in navigator && 'serviceWorker' in navigator) {
      try {
        const registration = await navigator.serviceWorker.ready;
        // Background sync API might not be available in all browsers
        if ('sync' in registration) {
          await (registration as any).sync.register(SYNC_TAG);
          console.log('Background sync registered');
        }
      } catch (error) {
        console.error('Background sync registration failed:', error);
        // Fallback: try to sync when online
        this.setupOnlineListener();
      }
    } else {
      // Fallback for browsers without background sync
      this.setupOnlineListener();
    }
  }

  // Setup online event listener as fallback
  private setupOnlineListener(): void {
    const handleOnline = () => {
      console.log('Device came online, processing sync queue');
      this.processQueue();
    };

    window.addEventListener('online', handleOnline);
  }

  // Process sync queue
  public async processQueue(): Promise<void> {
    if (this.syncQueue.length === 0) {
      return;
    }

    const queueToProcess = [...this.syncQueue];
    console.log(`Processing ${queueToProcess.length} sync operations`);

    for (const syncData of queueToProcess) {
      try {
        await this.processSyncData(syncData);
        // Remove from queue on success
        this.syncQueue = this.syncQueue.filter(item => item.id !== syncData.id);
      } catch (error) {
        console.error(`Failed to sync ${syncData.id}:`, error);
        // Keep in queue for retry (could implement exponential backoff here)
      }
    }

    this.saveSyncQueue();
  }

  // Process individual sync data
  private async processSyncData(syncData: SyncData): Promise<void> {
    const { action, entity, data } = syncData;

    switch (entity) {
      case 'alarm': {
        // Import alarm service dynamically to avoid circular dependencies
        const { alarmService } = await import('@/services/alarmService');

        switch (action) {
          case 'create':
            await alarmService.createAlarm(data);
            break;
          case 'update':
            await alarmService.updateAlarm(data.id, data);
            break;
          case 'delete':
            await alarmService.deleteAlarm(data.id);
            break;
        }
        break;
      }

      case 'routine': {
        // Import routine service dynamically to avoid circular dependencies
        const { RoutineService } = await import('@/services');

        switch (action) {
          case 'create':
            await RoutineService.createRoutine(data);
            break;
          case 'update':
            await RoutineService.updateRoutine(data.id, data);
            break;
          case 'delete':
            await RoutineService.deleteRoutine(data.id);
            break;
        }
        break;
      }
    }

    console.log(`Successfully synced: ${syncData.id}`);
  }

  // Get pending sync operations count
  public getPendingCount(): number {
    return this.syncQueue.length;
  }

  // Get pending operations for UI display
  public getPendingOperations(): SyncData[] {
    return [...this.syncQueue];
  }

  // Clear all pending operations (use with caution)
  public clearQueue(): void {
    this.syncQueue = [];
    this.saveSyncQueue();
  }

  // Check if device is online
  public isOnline(): boolean {
    return navigator.onLine;
  }
}

// Create and export singleton instance
export const backgroundSync = BackgroundSync.getInstance();

// Helper function to handle offline operations
export const handleOfflineOperation = (
  action: SyncData['action'],
  entity: SyncData['entity'],
  data: any
): void => {
  backgroundSync.addToSyncQueue(action, entity, data);
};

// Initialize background sync event listener
if (typeof window !== 'undefined') {
  // Listen for sync events from service worker
  navigator.serviceWorker?.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'BACKGROUND_SYNC') {
      backgroundSync.processQueue().catch(console.error);
    }
  });
}
