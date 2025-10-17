// Intelligent Alarm Timing Optimization
import { mlDataCollector } from './mlDataCollector';
// import type { AlarmDto } from '@/services/alarmService';

export interface OptimalAlarmWindow {
  originalTime: string;
  optimizedTime: string;
  confidenceScore: number;
  reasoning: string;
  sleepCyclePhase: 'light' | 'deep' | 'rem' | 'transition';
  adjustmentMinutes: number;
}

export interface SleepCyclePhase {
  phase: 'light' | 'deep' | 'rem' | 'transition';
  startTime: string;
  endTime: string;
  probability: number;
}

export interface UserSleepProfile {
  averageSleepCycles: number; // Typically 4-6 cycles per night
  cycleLength: number; // Average 90 minutes, varies by individual
  lightSleepProportion: number; // Percentage of sleep in light phase
  deepSleepProportion: number; // Percentage of sleep in deep phase
  remSleepProportion: number; // Percentage of sleep in REM phase
  chronotype: 'morning' | 'evening' | 'intermediate'; // Natural preference
  sleepLatency: number; // Time to fall asleep in minutes
}

class AlarmOptimizer {
  private static instance: AlarmOptimizer;
  // private readonly SLEEP_CYCLE_LENGTH = 90; // minutes
  private readonly LIGHT_SLEEP_WINDOW = 15; // minutes before/after optimal time

  private constructor() {}

  public static getInstance(): AlarmOptimizer {
    if (!AlarmOptimizer.instance) {
      AlarmOptimizer.instance = new AlarmOptimizer();
    }
    return AlarmOptimizer.instance;
  }

  /**
   * Calculate optimal alarm time based on user's sleep patterns
   */
  public calculateOptimalAlarmTime(
    desiredWakeTime: string,
    estimatedBedtime?: string
  ): OptimalAlarmWindow {
    const userProfile = this.buildUserSleepProfile();
    const sleepDuration = this.calculateSleepDuration(estimatedBedtime, desiredWakeTime);

    if (!userProfile || sleepDuration < 4) {
      // Not enough data or too short sleep - return original time
      return {
        originalTime: desiredWakeTime,
        optimizedTime: desiredWakeTime,
        confidenceScore: 0.1,
        reasoning: 'Insufficient sleep data for optimization',
        sleepCyclePhase: 'transition',
        adjustmentMinutes: 0
      };
    }

    const cyclePhases = this.predictSleepCycles(estimatedBedtime || this.estimateBedtime(desiredWakeTime), sleepDuration, userProfile);
    const optimalWindow = this.findOptimalWakeWindow(desiredWakeTime, cyclePhases);

    return optimalWindow;
  }

  /**
   * Build user sleep profile from collected ML data
   */
  private buildUserSleepProfile(): UserSleepProfile | null {
    const analytics = mlDataCollector.getLocalAnalytics();
    if (!analytics) return null;

    // Analyze user's historical data to build sleep profile
    const chronotype = this.determineChronotype(analytics);

    return {
      averageSleepCycles: Math.round(analytics.avgSleepDuration / 1.5), // 90min cycles
      cycleLength: this.calculatePersonalCycleLength(analytics),
      lightSleepProportion: 0.50, // Estimated from research
      deepSleepProportion: 0.25,
      remSleepProportion: 0.25,
      chronotype,
      sleepLatency: this.estimateSleepLatency(analytics)
    };
  }

  /**
   * Determine user's chronotype based on sleep patterns
   */
  private determineChronotype(analytics: any): 'morning' | 'evening' | 'intermediate' {
    const avgBedtimeHour = this.timeToMinutes(analytics.avgBedtime) / 60;
    const avgWakeHour = this.timeToMinutes(analytics.avgWakeupTime) / 60;

    if (avgBedtimeHour <= 22 && avgWakeHour <= 6) {
      return 'morning';
    } else if (avgBedtimeHour >= 24 && avgWakeHour >= 8) {
      return 'evening';
    } else {
      return 'intermediate';
    }
  }

  /**
   * Calculate personal sleep cycle length from historical data
   */
  private calculatePersonalCycleLength(analytics: any): number {
    // Most people have 90-minute cycles, but it can vary from 70-120 minutes
    // Use sleep consistency and duration patterns to estimate
    const consistencyFactor = analytics.sleepConsistency;

    if (consistencyFactor > 0.8) {
      // Highly consistent sleepers often have stable 90-minute cycles
      return 90;
    } else if (consistencyFactor > 0.6) {
      // Moderately consistent - slight variation
      return Math.round(85 + (Math.random() * 10)); // 85-95 minutes
    } else {
      // Less consistent - more variation possible
      return Math.round(80 + (Math.random() * 20)); // 80-100 minutes
    }
  }

  /**
   * Estimate sleep latency (time to fall asleep)
   */
  private estimateSleepLatency(analytics: any): number {
    // Estimate based on sleep consistency
    // More consistent sleepers typically have shorter sleep latency
    const consistencyFactor = analytics.sleepConsistency;

    if (consistencyFactor > 0.8) {
      return 10; // 10 minutes for good sleepers
    } else if (consistencyFactor > 0.6) {
      return 20; // 20 minutes for average sleepers
    } else {
      return 30; // 30 minutes for poor sleepers
    }
  }

  /**
   * Predict sleep cycles for the night
   */
  private predictSleepCycles(bedtime: string, sleepDuration: number, profile: UserSleepProfile): SleepCyclePhase[] {
    const phases: SleepCyclePhase[] = [];
    const bedtimeMinutes = this.timeToMinutes(bedtime);
    const sleepStartMinutes = bedtimeMinutes + profile.sleepLatency;
    const totalSleepMinutes = sleepDuration * 60;
    const numberOfCycles = Math.floor(totalSleepMinutes / profile.cycleLength);

    for (let cycle = 0; cycle < numberOfCycles; cycle++) {
      const cycleStartMinutes = sleepStartMinutes + (cycle * profile.cycleLength);

      // Each cycle has different phases
      const phases_in_cycle = [
        {
          phase: 'light' as const,
          duration: profile.cycleLength * 0.5, // 50% light sleep
          probability: 0.9
        },
        {
          phase: 'deep' as const,
          duration: profile.cycleLength * 0.25, // 25% deep sleep
          probability: cycle < 3 ? 0.8 : 0.4 // Deep sleep more likely in first half
        },
        {
          phase: 'rem' as const,
          duration: profile.cycleLength * 0.25, // 25% REM sleep
          probability: cycle > 1 ? 0.8 : 0.4 // REM more likely in second half
        }
      ];

      let phaseStartMinutes = cycleStartMinutes;
      for (const phaseInfo of phases_in_cycle) {
        phases.push({
          phase: phaseInfo.phase,
          startTime: this.minutesToTime(phaseStartMinutes),
          endTime: this.minutesToTime(phaseStartMinutes + phaseInfo.duration),
          probability: phaseInfo.probability
        });
        phaseStartMinutes += phaseInfo.duration;
      }
    }

    return phases;
  }

  /**
   * Find the optimal wake window within the desired time range
   */
  private findOptimalWakeWindow(desiredWakeTime: string, sleepPhases: SleepCyclePhase[]): OptimalAlarmWindow {
    const desiredMinutes = this.timeToMinutes(desiredWakeTime);
    const windowStart = desiredMinutes - this.LIGHT_SLEEP_WINDOW;
    const windowEnd = desiredMinutes + this.LIGHT_SLEEP_WINDOW;

    // Find light sleep phases within the window
    const optimalPhases = sleepPhases.filter(phase => {
      if (phase.phase !== 'light') return false;

      const phaseStartMinutes = this.timeToMinutes(phase.startTime);
      const phaseEndMinutes = this.timeToMinutes(phase.endTime);

      return (phaseStartMinutes >= windowStart && phaseStartMinutes <= windowEnd) ||
             (phaseEndMinutes >= windowStart && phaseEndMinutes <= windowEnd) ||
             (phaseStartMinutes <= windowStart && phaseEndMinutes >= windowEnd);
    });

    if (optimalPhases.length === 0) {
      // No light sleep phases in window - find closest transition
      const nearestTransition = this.findNearestTransition(desiredMinutes, sleepPhases);

      return {
        originalTime: desiredWakeTime,
        optimizedTime: nearestTransition.time,
        confidenceScore: nearestTransition.confidence,
        reasoning: nearestTransition.reasoning,
        sleepCyclePhase: 'transition',
        adjustmentMinutes: nearestTransition.adjustmentMinutes
      };
    }

    // Find the best light sleep phase
    const bestPhase = optimalPhases.reduce((best, current) =>
      current.probability > best.probability ? current : best
    );

    const phaseMiddleMinutes = Math.round(
      (this.timeToMinutes(bestPhase.startTime) + this.timeToMinutes(bestPhase.endTime)) / 2
    );

    const adjustmentMinutes = phaseMiddleMinutes - desiredMinutes;
    const optimizedTime = this.minutesToTime(phaseMiddleMinutes);

    return {
      originalTime: desiredWakeTime,
      optimizedTime,
      confidenceScore: bestPhase.probability * 0.85, // Reduce confidence slightly
      reasoning: `Optimized to wake during light sleep phase (${Math.abs(adjustmentMinutes)} minutes ${adjustmentMinutes >= 0 ? 'later' : 'earlier'})`,
      sleepCyclePhase: 'light',
      adjustmentMinutes
    };
  }

  /**
   * Find nearest sleep phase transition when no light sleep is available
   */
  private findNearestTransition(desiredMinutes: number, sleepPhases: SleepCyclePhase[]): {
    time: string;
    confidence: number;
    reasoning: string;
    adjustmentMinutes: number;
  } {
    let nearestDistance = Infinity;
    let nearestTime = desiredMinutes;

    for (let i = 0; i < sleepPhases.length - 1; i++) {
      const currentPhaseEnd = this.timeToMinutes(sleepPhases[i].endTime);
      const distance = Math.abs(currentPhaseEnd - desiredMinutes);

      if (distance < nearestDistance) {
        nearestDistance = distance;
        nearestTime = currentPhaseEnd;
      }
    }

    const adjustmentMinutes = nearestTime - desiredMinutes;

    return {
      time: this.minutesToTime(nearestTime),
      confidence: Math.max(0.3, 0.7 - (nearestDistance / 30)), // Lower confidence for distant transitions
      reasoning: `Adjusted to nearest sleep phase transition (${Math.abs(adjustmentMinutes)} minutes ${adjustmentMinutes >= 0 ? 'later' : 'earlier'})`,
      adjustmentMinutes
    };
  }

  /**
   * Calculate sleep duration between bedtime and wake time
   */
  private calculateSleepDuration(bedtime: string | undefined, wakeTime: string): number {
    if (!bedtime) {
      return 8; // Default assumption
    }

    const bedMinutes = this.timeToMinutes(bedtime);
    const wakeMinutes = this.timeToMinutes(wakeTime);

    // Handle overnight sleep (bedtime after midnight)
    let durationMinutes = wakeMinutes - bedMinutes;
    if (durationMinutes <= 0) {
      durationMinutes += 24 * 60; // Add 24 hours
    }

    return durationMinutes / 60; // Convert to hours
  }

  /**
   * Estimate bedtime based on wake time and average sleep duration
   */
  private estimateBedtime(wakeTime: string): string {
    const analytics = mlDataCollector.getLocalAnalytics();
    const avgSleepDuration = analytics?.avgSleepDuration || 8;

    const wakeMinutes = this.timeToMinutes(wakeTime);
    const estimatedBedtimeMinutes = wakeMinutes - (avgSleepDuration * 60);

    // Handle negative times (previous day)
    const bedtimeMinutes = estimatedBedtimeMinutes < 0
      ? estimatedBedtimeMinutes + (24 * 60)
      : estimatedBedtimeMinutes;

    return this.minutesToTime(bedtimeMinutes);
  }

  /**
   * Convert time string to minutes since midnight
   */
  private timeToMinutes(time: string): number {
    const [hours, minutes] = time.split(':').map(Number);
    return hours * 60 + minutes;
  }

  /**
   * Convert minutes since midnight to time string
   */
  private minutesToTime(minutes: number): string {
    // Handle overflow/underflow
    const normalizedMinutes = ((minutes % (24 * 60)) + (24 * 60)) % (24 * 60);

    const hours = Math.floor(normalizedMinutes / 60);
    const mins = normalizedMinutes % 60;

    return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
  }

  /**
   * Get smart alarm recommendation for creating a new alarm
   */
  public getSmartAlarmRecommendation(desiredTime: string): {
    recommendedTime: string;
    optimization: OptimalAlarmWindow;
    userMessage: string;
  } {
    const optimization = this.calculateOptimalAlarmTime(desiredTime);

    let userMessage = '';
    if (optimization.confidenceScore > 0.7) {
      if (Math.abs(optimization.adjustmentMinutes) <= 5) {
        userMessage = `Perfect timing! ${desiredTime} aligns well with your natural sleep cycle.`;
      } else {
        userMessage = `Suggested: ${optimization.optimizedTime} (${Math.abs(optimization.adjustmentMinutes)} min ${optimization.adjustmentMinutes > 0 ? 'later' : 'earlier'}) for easier waking during light sleep.`;
      }
    } else if (optimization.confidenceScore > 0.4) {
      userMessage = `Moderate optimization available. ${optimization.reasoning}`;
    } else {
      userMessage = `Using original time. Build more sleep history for better optimization.`;
    }

    return {
      recommendedTime: optimization.confidenceScore > 0.4 ? optimization.optimizedTime : desiredTime,
      optimization,
      userMessage
    };
  }

  /**
   * Analyze alarm effectiveness after dismissal
   */
  public analyzeAlarmEffectiveness(
    _alarmId: string,
    originalTime: string,
    actualDismissalTime: string,
    userRating?: 1 | 2 | 3 | 4 | 5
  ): {
    effectiveness: 'excellent' | 'good' | 'fair' | 'poor';
    learningInsights: string[];
    adjustmentRecommendations: string[];
  } {
    const timeDifference = Math.abs(
      this.timeToMinutes(actualDismissalTime) - this.timeToMinutes(originalTime)
    );

    let effectiveness: 'excellent' | 'good' | 'fair' | 'poor';
    const insights: string[] = [];
    const recommendations: string[] = [];

    // Analyze timing effectiveness
    if (timeDifference <= 2) {
      effectiveness = 'excellent';
      insights.push('Alarm dismissed promptly - excellent timing');
    } else if (timeDifference <= 10) {
      effectiveness = 'good';
      insights.push('Alarm dismissed reasonably quickly');
    } else if (timeDifference <= 30) {
      effectiveness = 'fair';
      insights.push('Some delay in dismissal - may indicate deep sleep');
      recommendations.push('Consider adjusting alarm time by 10-15 minutes earlier');
    } else {
      effectiveness = 'poor';
      insights.push('Significant delay in dismissal - likely caught in deep sleep');
      recommendations.push('Recommend sleep schedule analysis and adjustment');
    }

    // Factor in user rating if provided
    if (userRating) {
      if (userRating >= 4) {
        insights.push('User reported positive waking experience');
      } else if (userRating <= 2) {
        effectiveness = effectiveness === 'excellent' ? 'good' : 'fair';
        insights.push('User reported difficult waking experience');
        recommendations.push('Consider gradual alarm or different wake time');
      }
    }

    return {
      effectiveness,
      learningInsights: insights,
      adjustmentRecommendations: recommendations
    };
  }
}

// Create singleton instance
export const alarmOptimizer = AlarmOptimizer.getInstance();

// React hook for components
export function useAlarmOptimization() {
  return {
    calculateOptimalTime: (desiredTime: string, bedtime?: string) =>
      alarmOptimizer.calculateOptimalAlarmTime(desiredTime, bedtime),

    getSmartRecommendation: (desiredTime: string) =>
      alarmOptimizer.getSmartAlarmRecommendation(desiredTime),

    analyzeEffectiveness: (
      alarmId: string,
      originalTime: string,
      actualTime: string,
      rating?: 1 | 2 | 3 | 4 | 5
    ) => alarmOptimizer.analyzeAlarmEffectiveness(alarmId, originalTime, actualTime, rating)
  };
}
