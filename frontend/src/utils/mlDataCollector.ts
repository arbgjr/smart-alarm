// ML Data Collection Pipeline for Smart Alarm
interface UserBehaviorData {
  userId: string;
  timestamp: string;
  eventType: 'alarm_created' | 'alarm_triggered' | 'alarm_dismissed' | 'alarm_snoozed' | 'sleep_pattern' | 'app_interaction';
  data: {
    // Alarm-related data
    alarmId?: string;
    originalTime?: string;
    actualTime?: string;
    snoozeCount?: number;
    dismissalMethod?: 'button' | 'voice' | 'gesture' | 'timeout';
    
    // Sleep pattern data
    bedtime?: string;
    wakeupTime?: string;
    sleepDuration?: number;
    sleepQuality?: 1 | 2 | 3 | 4 | 5; // User-reported quality
    
    // Contextual data
    dayOfWeek?: string;
    weather?: string;
    deviceType?: 'mobile' | 'desktop' | 'tablet';
    timezone?: string;
    
    // Behavioral patterns
    responseTime?: number; // Time to dismiss/snooze
    interactionPattern?: 'quick' | 'delayed' | 'multiple_snooze';
    preconditions?: {
      previousNightBedtime?: string;
      caffeineTaken?: boolean;
      exerciseToday?: boolean;
      stressLevel?: 1 | 2 | 3 | 4 | 5;
    };
  };
  metadata: {
    appVersion: string;
    platform: string;
    sessionId: string;
    privacyConsent: boolean;
  };
}

interface SleepPatternMetrics {
  avgBedtime: string;
  avgWakeupTime: string;
  avgSleepDuration: number;
  sleepConsistency: number; // 0-1 score
  optimalAlarmWindow: {
    start: string;
    end: string;
    confidence: number;
  };
}

class MLDataCollector {
  private static instance: MLDataCollector;
  private dataQueue: UserBehaviorData[] = [];
  private sessionId: string;
  private isEnabled: boolean = false;
  private readonly STORAGE_KEY = 'smart-alarm-ml-data';
  private readonly MAX_QUEUE_SIZE = 100;

  private constructor() {
    this.sessionId = this.generateSessionId();
    this.loadFromStorage();
    this.checkPrivacyConsent();
  }

  public static getInstance(): MLDataCollector {
    if (!MLDataCollector.instance) {
      MLDataCollector.instance = new MLDataCollector();
    }
    return MLDataCollector.instance;
  }

  private generateSessionId(): string {
    return `session-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private checkPrivacyConsent(): void {
    const consent = localStorage.getItem('ml-data-collection-consent');
    this.isEnabled = consent === 'true';
  }

  public enableDataCollection(): void {
    this.isEnabled = true;
    localStorage.setItem('ml-data-collection-consent', 'true');
  }

  public disableDataCollection(): void {
    this.isEnabled = false;
    localStorage.setItem('ml-data-collection-consent', 'false');
    this.clearStoredData();
  }

  // Track alarm creation patterns
  public trackAlarmCreated(alarmId: string, time: string, contextData?: Partial<UserBehaviorData['data']>): void {
    if (!this.isEnabled) return;

    this.addDataPoint({
      eventType: 'alarm_created',
      data: {
        alarmId,
        originalTime: time,
        dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
        ...contextData,
      },
    });
  }

  // Track alarm dismissal behavior
  public trackAlarmDismissed(
    alarmId: string,
    originalTime: string,
    actualTime: string,
    method: UserBehaviorData['data']['dismissalMethod'],
    responseTime: number
  ): void {
    if (!this.isEnabled) return;

    const timeDiff = this.calculateTimeDifference(originalTime, actualTime);
    
    this.addDataPoint({
      eventType: 'alarm_dismissed',
      data: {
        alarmId,
        originalTime,
        actualTime,
        dismissalMethod: method,
        responseTime,
        dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
        interactionPattern: responseTime < 30000 ? 'quick' : 'delayed',
      },
    });
  }

  // Track snooze patterns
  public trackAlarmSnoozed(alarmId: string, snoozeCount: number): void {
    if (!this.isEnabled) return;

    this.addDataPoint({
      eventType: 'alarm_snoozed',
      data: {
        alarmId,
        snoozeCount,
        interactionPattern: snoozeCount > 2 ? 'multiple_snooze' : 'single_snooze' as any,
        dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
      },
    });
  }

  // Track sleep patterns
  public trackSleepPattern(
    bedtime: string,
    wakeupTime: string,
    quality: 1 | 2 | 3 | 4 | 5,
    preconditions?: UserBehaviorData['data']['preconditions']
  ): void {
    if (!this.isEnabled) return;

    const sleepDuration = this.calculateSleepDuration(bedtime, wakeupTime);

    this.addDataPoint({
      eventType: 'sleep_pattern',
      data: {
        bedtime,
        wakeupTime,
        sleepDuration,
        sleepQuality: quality,
        dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
        preconditions,
      },
    });
  }

  // Track general app interactions for behavior analysis
  public trackAppInteraction(interactionType: string, duration: number, context?: Record<string, any>): void {
    if (!this.isEnabled) return;

    this.addDataPoint({
      eventType: 'app_interaction',
      data: {
        responseTime: duration,
        dayOfWeek: new Date().toLocaleDateString('en', { weekday: 'long' }).toLowerCase(),
        ...context,
      },
    });
  }

  private addDataPoint(partialData: Partial<UserBehaviorData>): void {
    const userId = this.getCurrentUserId();
    if (!userId) return;

    const dataPoint: UserBehaviorData = {
      userId,
      timestamp: new Date().toISOString(),
      eventType: partialData.eventType!,
      data: partialData.data || {},
      metadata: {
        appVersion: import.meta.env.VITE_APP_VERSION || '1.0.0',
        platform: this.detectPlatform(),
        sessionId: this.sessionId,
        privacyConsent: this.isEnabled,
      },
    };

    this.dataQueue.push(dataPoint);
    
    // Limit queue size to prevent memory issues
    if (this.dataQueue.length > this.MAX_QUEUE_SIZE) {
      this.dataQueue.shift();
    }

    this.saveToStorage();
    this.attemptSync();
  }

  private calculateTimeDifference(originalTime: string, actualTime: string): number {
    const original = new Date(`1970-01-01T${originalTime}`);
    const actual = new Date(actualTime);
    return Math.abs(actual.getTime() - original.getTime());
  }

  private calculateSleepDuration(bedtime: string, wakeupTime: string): number {
    const bed = new Date(`1970-01-01T${bedtime}`);
    const wake = new Date(`1970-01-02T${wakeupTime}`); // Next day
    return Math.abs(wake.getTime() - bed.getTime()) / (1000 * 60 * 60); // Hours
  }

  private getCurrentUserId(): string | null {
    // Get user ID from auth store
    try {
      const authData = JSON.parse(localStorage.getItem('smart-alarm-auth') || '{}');
      return authData?.state?.user?.id || null;
    } catch {
      return null;
    }
  }

  private detectPlatform(): string {
    const ua = navigator.userAgent;
    if (/Mobile|Android|iPhone|iPad/.test(ua)) return 'mobile';
    if (/Tablet|iPad/.test(ua)) return 'tablet';
    return 'desktop';
  }

  private saveToStorage(): void {
    try {
      localStorage.setItem(this.STORAGE_KEY, JSON.stringify(this.dataQueue));
    } catch (error) {
      console.warn('Failed to save ML data to storage:', error);
    }
  }

  private loadFromStorage(): void {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (stored) {
        this.dataQueue = JSON.parse(stored);
      }
    } catch (error) {
      console.warn('Failed to load ML data from storage:', error);
      this.dataQueue = [];
    }
  }

  private clearStoredData(): void {
    this.dataQueue = [];
    localStorage.removeItem(this.STORAGE_KEY);
  }

  // Sync data with backend ML service
  private async attemptSync(): Promise<void> {
    if (this.dataQueue.length === 0 || !navigator.onLine) return;

    try {
      const response = await fetch('/api/ml/behavior-data', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`,
        },
        body: JSON.stringify({
          data: this.dataQueue,
          batchId: `batch-${Date.now()}`,
        }),
      });

      if (response.ok) {
        this.dataQueue = []; // Clear synced data
        this.saveToStorage();
      }
    } catch (error) {
      console.debug('ML data sync failed, will retry later:', error);
      // Data remains in queue for retry
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

  // Get analytics for user insights
  public getLocalAnalytics(): SleepPatternMetrics | null {
    if (!this.isEnabled) return null;

    const sleepData = this.dataQueue
      .filter(d => d.eventType === 'sleep_pattern')
      .slice(-30); // Last 30 entries

    if (sleepData.length < 7) return null; // Need at least a week of data

    const bedtimes = sleepData.map(d => d.data.bedtime!);
    const wakeupTimes = sleepData.map(d => d.data.wakeupTime!);
    const durations = sleepData.map(d => d.data.sleepDuration!);

    return {
      avgBedtime: this.calculateAverageTime(bedtimes),
      avgWakeupTime: this.calculateAverageTime(wakeupTimes),
      avgSleepDuration: durations.reduce((a, b) => a + b, 0) / durations.length,
      sleepConsistency: this.calculateConsistencyScore(bedtimes, wakeupTimes),
      optimalAlarmWindow: this.calculateOptimalWindow(wakeupTimes),
    };
  }

  private calculateAverageTime(times: string[]): string {
    const totalMinutes = times.reduce((sum, time) => {
      const [hours, minutes] = time.split(':').map(Number);
      return sum + (hours * 60) + minutes;
    }, 0);

    const avgMinutes = Math.round(totalMinutes / times.length);
    const hours = Math.floor(avgMinutes / 60);
    const minutes = avgMinutes % 60;

    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
  }

  private calculateConsistencyScore(bedtimes: string[], wakeupTimes: string[]): number {
    // Calculate standard deviation for consistency
    const bedtimeVariance = this.calculateTimeVariance(bedtimes);
    const wakeupVariance = this.calculateTimeVariance(wakeupTimes);
    
    // Convert to 0-1 score (lower variance = higher consistency)
    const maxVariance = 3600; // 1 hour in seconds
    const consistency = Math.max(0, 1 - (bedtimeVariance + wakeupVariance) / (2 * maxVariance));
    
    return Math.round(consistency * 100) / 100;
  }

  private calculateTimeVariance(times: string[]): number {
    const seconds = times.map(time => {
      const [hours, minutes] = time.split(':').map(Number);
      return hours * 3600 + minutes * 60;
    });

    const mean = seconds.reduce((a, b) => a + b) / seconds.length;
    const variance = seconds.reduce((sum, value) => sum + Math.pow(value - mean, 2), 0) / seconds.length;
    
    return Math.sqrt(variance);
  }

  private calculateOptimalWindow(wakeupTimes: string[]): SleepPatternMetrics['optimalAlarmWindow'] {
    const avgWakeup = this.calculateAverageTime(wakeupTimes);
    const variance = this.calculateTimeVariance(wakeupTimes);
    
    // Create 30-minute window around average time
    const [hours, minutes] = avgWakeup.split(':').map(Number);
    const avgMinutes = hours * 60 + minutes;
    
    const startMinutes = Math.max(0, avgMinutes - 15);
    const endMinutes = avgMinutes + 15;
    
    const startHours = Math.floor(startMinutes / 60);
    const startMins = startMinutes % 60;
    const endHours = Math.floor(endMinutes / 60);
    const endMins = endMinutes % 60;
    
    return {
      start: `${startHours.toString().padStart(2, '0')}:${startMins.toString().padStart(2, '0')}`,
      end: `${endHours.toString().padStart(2, '0')}:${endMins.toString().padStart(2, '0')}`,
      confidence: Math.max(0.5, 1 - variance / 3600), // Higher confidence with lower variance
    };
  }

  // Get pending data count for UI display
  public getPendingDataCount(): number {
    return this.dataQueue.length;
  }

  // Manual sync trigger
  public async forcSync(): Promise<boolean> {
    try {
      await this.attemptSync();
      return this.dataQueue.length === 0;
    } catch {
      return false;
    }
  }

  // Privacy compliance
  public exportUserData(): UserBehaviorData[] {
    return [...this.dataQueue];
  }

  public deleteUserData(): void {
    this.clearStoredData();
  }
}

// Create singleton instance
export const mlDataCollector = MLDataCollector.getInstance();

// Hook for React components
export function useMLDataCollection() {
  const isEnabled = mlDataCollector['isEnabled'];
  const pendingCount = mlDataCollector.getPendingDataCount();
  
  return {
    isEnabled,
    pendingCount,
    enableCollection: () => mlDataCollector.enableDataCollection(),
    disableCollection: () => mlDataCollector.disableDataCollection(),
    trackAlarmCreated: mlDataCollector.trackAlarmCreated.bind(mlDataCollector),
    trackAlarmDismissed: mlDataCollector.trackAlarmDismissed.bind(mlDataCollector),
    trackAlarmSnoozed: mlDataCollector.trackAlarmSnoozed.bind(mlDataCollector),
    trackSleepPattern: mlDataCollector.trackSleepPattern.bind(mlDataCollector),
    getLocalAnalytics: () => mlDataCollector.getLocalAnalytics(),
    exportData: () => mlDataCollector.exportUserData(),
    deleteData: () => mlDataCollector.deleteUserData(),
    forceSync: () => mlDataCollector.forcSync(),
  };
}