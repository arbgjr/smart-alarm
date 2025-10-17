// Personalized Recommendations Engine
import { mlDataCollector } from './mlDataCollector';
// import { alarmOptimizer } from './alarmOptimizer';
import type { SleepPatternMetrics } from './mlDataCollector';

export interface PersonalizedRecommendation {
  id: string;
  category: 'sleep_schedule' | 'alarm_timing' | 'sleep_hygiene' | 'consistency' | 'environment' | 'health';
  priority: 'critical' | 'high' | 'medium' | 'low';
  title: string;
  description: string;
  actionableSteps: string[];
  expectedImpact: string;
  confidence: number;
  relevanceScore: number;
  personalizedInsight: string;
  timeframe: 'immediate' | 'short_term' | 'medium_term' | 'long_term'; // days to see results
  daysToResults: number;
  scientificBasis?: string;
  userSpecificData?: {
    currentPattern?: string;
    targetPattern?: string;
    improvement?: string;
  };
}

export interface RecommendationContext {
  userGoals: ('better_sleep' | 'easier_waking' | 'consistent_schedule' | 'energy_boost' | 'health_optimization')[];
  accessibilityNeeds: ('visual_impairment' | 'hearing_impairment' | 'motor_impairment' | 'cognitive_support' | 'neurodivergent')[];
  lifeStyle: {
    workSchedule: 'standard' | 'shift_work' | 'flexible' | 'irregular';
    stressLevel: 1 | 2 | 3 | 4 | 5;
    exerciseFrequency: 'daily' | 'regular' | 'occasional' | 'rarely' | 'never';
    caffeineHabits: 'none' | 'light' | 'moderate' | 'heavy';
  };
  chronotype: 'morning' | 'evening' | 'intermediate';
  age: number;
  gender?: 'male' | 'female' | 'other' | 'prefer_not_to_say';
}

class RecommendationsEngine {
  private static instance: RecommendationsEngine;
  private readonly RECOMMENDATION_TEMPLATES: Map<string, Partial<PersonalizedRecommendation>> = new Map();

  private constructor() {
    this.initializeRecommendationTemplates();
  }

  public static getInstance(): RecommendationsEngine {
    if (!RecommendationsEngine.instance) {
      RecommendationsEngine.instance = new RecommendationsEngine();
    }
    return RecommendationsEngine.instance;
  }

  private initializeRecommendationTemplates(): void {
    // Sleep Schedule Recommendations
    this.RECOMMENDATION_TEMPLATES.set('inconsistent_bedtime', {
      category: 'sleep_schedule',
      priority: 'high',
      title: 'Establish Consistent Bedtime',
      expectedImpact: 'Better sleep quality and easier mornings within 1-2 weeks',
      scientificBasis: 'Consistent sleep-wake cycles regulate circadian rhythms, improving sleep quality by up to 30%',
      timeframe: 'medium_term',
      daysToResults: 14
    });

    this.RECOMMENDATION_TEMPLATES.set('insufficient_sleep', {
      category: 'sleep_schedule',
      priority: 'critical',
      title: 'Increase Sleep Duration',
      expectedImpact: 'Improved cognitive function, mood, and physical health',
      scientificBasis: 'Adults need 7-9 hours of sleep. Chronic sleep deprivation affects memory consolidation and immune function',
      timeframe: 'short_term',
      daysToResults: 7
    });

    // Alarm Timing Recommendations
    this.RECOMMENDATION_TEMPLATES.set('alarm_optimization', {
      category: 'alarm_timing',
      priority: 'medium',
      title: 'Optimize Alarm Timing',
      expectedImpact: 'Easier waking and improved morning alertness',
      scientificBasis: 'Waking during light sleep phases reduces sleep inertia and improves morning cognitive performance',
      timeframe: 'immediate',
      daysToResults: 1
    });

    // Sleep Hygiene Recommendations
    this.RECOMMENDATION_TEMPLATES.set('evening_routine', {
      category: 'sleep_hygiene',
      priority: 'medium',
      title: 'Establish Evening Wind-Down Routine',
      expectedImpact: 'Faster sleep onset and better sleep quality',
      scientificBasis: 'Pre-sleep routines signal the brain to prepare for sleep, reducing sleep latency by 20-30%',
      timeframe: 'short_term',
      daysToResults: 10
    });

    this.RECOMMENDATION_TEMPLATES.set('screen_time', {
      category: 'sleep_hygiene',
      priority: 'medium',
      title: 'Reduce Evening Screen Exposure',
      expectedImpact: 'Improved melatonin production and sleep onset',
      scientificBasis: 'Blue light exposure within 2 hours of bedtime suppresses melatonin production by up to 50%',
      timeframe: 'immediate',
      daysToResults: 3
    });

    // Environment Recommendations
    this.RECOMMENDATION_TEMPLATES.set('sleep_environment', {
      category: 'environment',
      priority: 'medium',
      title: 'Optimize Sleep Environment',
      expectedImpact: 'Deeper sleep and fewer nighttime awakenings',
      scientificBasis: 'Cool (65-68°F), dark, and quiet environments promote deeper sleep stages',
      timeframe: 'immediate',
      daysToResults: 1
    });

    // Health-focused Recommendations
    this.RECOMMENDATION_TEMPLATES.set('exercise_timing', {
      category: 'health',
      priority: 'low',
      title: 'Optimize Exercise Timing',
      expectedImpact: 'Better sleep quality and daytime energy',
      scientificBasis: 'Regular exercise improves sleep quality, but intense exercise within 4 hours of bedtime may delay sleep onset',
      timeframe: 'medium_term',
      daysToResults: 21
    });
  }

  /**
   * Generate personalized recommendations based on user data
   */
  public generateRecommendations(): PersonalizedRecommendation[] {
    const analytics = mlDataCollector.getLocalAnalytics();
    if (!analytics) {
      return this.getOnboardingRecommendations();
    }

    const recommendations: PersonalizedRecommendation[] = [];

    // Analyze sleep patterns and generate recommendations
    recommendations.push(...this.analyzeSleepConsistency(analytics));
    recommendations.push(...this.analyzeSleepDuration(analytics));
    recommendations.push(...this.analyzeAlarmTiming(analytics));
    recommendations.push(...this.analyzeLifestyleFactors(analytics));

    // Sort by priority and relevance
    return recommendations
      .sort((a, b) => {
        const priorityOrder = { critical: 4, high: 3, medium: 2, low: 1 };
        return priorityOrder[b.priority] - priorityOrder[a.priority] || b.relevanceScore - a.relevanceScore;
      })
      .slice(0, 8); // Limit to top 8 recommendations
  }

  private analyzeSleepConsistency(analytics: SleepPatternMetrics): PersonalizedRecommendation[] {
    const recommendations: PersonalizedRecommendation[] = [];

    if (analytics.sleepConsistency < 0.7) {
      const template = this.RECOMMENDATION_TEMPLATES.get('inconsistent_bedtime')!;
      const variationMinutes = Math.round((1 - analytics.sleepConsistency) * 60);

      recommendations.push({
        id: 'sleep-consistency-' + Date.now(),
        ...template,
        priority: analytics.sleepConsistency < 0.5 ? 'critical' : 'high',
        description: `Your bedtime varies by approximately ${variationMinutes} minutes nightly. Consistent sleep schedules synchronize your circadian rhythm.`,
        actionableSteps: [
          `Set a target bedtime of ${this.suggestOptimalBedtime(analytics)}`,
          'Use a pre-sleep reminder 1 hour before bedtime',
          'Avoid caffeine after 2 PM to improve sleep onset',
          'Create a 30-minute wind-down routine'
        ],
        confidence: 0.9,
        relevanceScore: Math.min(0.95, (1 - analytics.sleepConsistency) * 1.2),
        personalizedInsight: `Based on your current pattern of sleeping ${analytics.avgSleepDuration.toFixed(1)} hours, maintaining consistent timing could improve your sleep quality by up to ${Math.round((1 - analytics.sleepConsistency) * 30)}%.`,
        userSpecificData: {
          currentPattern: `Irregular bedtime (±${variationMinutes} minutes)`,
          targetPattern: `Consistent ${this.suggestOptimalBedtime(analytics)} bedtime`,
          improvement: `${Math.round((1 - analytics.sleepConsistency) * 30)}% better sleep quality`
        }
      } as PersonalizedRecommendation);
    }

    return recommendations;
  }

  private analyzeSleepDuration(analytics: SleepPatternMetrics): PersonalizedRecommendation[] {
    const recommendations: PersonalizedRecommendation[] = [];
    const optimalRange = this.getOptimalSleepRange(30); // Default age

    if (analytics.avgSleepDuration < optimalRange.min || analytics.avgSleepDuration > optimalRange.max) {
      const template = this.RECOMMENDATION_TEMPLATES.get('insufficient_sleep')!;
      const isInsufficient = analytics.avgSleepDuration < optimalRange.min;

      recommendations.push({
        id: 'sleep-duration-' + Date.now(),
        ...template,
        title: isInsufficient ? 'Increase Sleep Duration' : 'Optimize Sleep Duration',
        priority: analytics.avgSleepDuration < 6 ? 'critical' : 'high',
        description: `You average ${analytics.avgSleepDuration.toFixed(1)} hours of sleep. ${isInsufficient ? `Adults typically need ${optimalRange.min}-${optimalRange.max} hours for optimal function.` : 'Consider if you feel rested or could optimize your schedule.'}`,
        actionableSteps: isInsufficient ? [
          `Gradually move bedtime earlier by 15 minutes each night`,
          `Target ${optimalRange.min} hours minimum sleep duration`,
          'Eliminate evening activities that delay bedtime',
          'Use smart alarm optimization to wake during light sleep'
        ] : [
          'Monitor how you feel with current sleep duration',
          'Consider if oversleeping indicates sleep debt or poor sleep quality',
          'Maintain consistent sleep schedule even on weekends',
          'Focus on sleep quality improvements'
        ],
        confidence: 0.85,
        relevanceScore: Math.abs(analytics.avgSleepDuration - optimalRange.optimal) / optimalRange.optimal,
        personalizedInsight: `Your current sleep duration is ${Math.abs(analytics.avgSleepDuration - optimalRange.optimal).toFixed(1)} hours ${analytics.avgSleepDuration < optimalRange.optimal ? 'below' : 'above'} the optimal ${optimalRange.optimal} hours for your age group.`,
        userSpecificData: {
          currentPattern: `${analytics.avgSleepDuration.toFixed(1)} hours average`,
          targetPattern: `${optimalRange.optimal} hours optimal`,
          improvement: isInsufficient ? 'Better cognitive function and mood' : 'More efficient sleep schedule'
        }
      } as PersonalizedRecommendation);
    }

    return recommendations;
  }

  private analyzeAlarmTiming(analytics: SleepPatternMetrics): PersonalizedRecommendation[] {
    const recommendations: PersonalizedRecommendation[] = [];

    if (analytics.optimalAlarmWindow.confidence > 0.6) {
      const template = this.RECOMMENDATION_TEMPLATES.get('alarm_optimization')!;

      recommendations.push({
        id: 'alarm-timing-' + Date.now(),
        ...template,
        description: `Your optimal wake-up window is ${analytics.optimalAlarmWindow.start}-${analytics.optimalAlarmWindow.end}. Setting alarms within this range can reduce morning grogginess.`,
        actionableSteps: [
          'Use the smart alarm feature for new alarms',
          `Consider adjusting existing alarms to fall within ${analytics.optimalAlarmWindow.start}-${analytics.optimalAlarmWindow.end}`,
          'Allow 15-30 minutes flexibility in your morning schedule',
          'Track how you feel waking up at different times within the window'
        ],
        confidence: analytics.optimalAlarmWindow.confidence,
        relevanceScore: analytics.optimalAlarmWindow.confidence * 0.8,
        personalizedInsight: `Based on your sleep patterns, you have a ${Math.round(analytics.optimalAlarmWindow.confidence * 100)}% probability of waking up more refreshed within your optimal window.`,
        userSpecificData: {
          currentPattern: `Average wake time: ${analytics.avgWakeupTime}`,
          targetPattern: `Optimal window: ${analytics.optimalAlarmWindow.start}-${analytics.optimalAlarmWindow.end}`,
          improvement: 'Reduced morning grogginess and faster alertness'
        }
      } as PersonalizedRecommendation);
    }

    return recommendations;
  }

  private analyzeLifestyleFactors(analytics: SleepPatternMetrics): PersonalizedRecommendation[] {
    const recommendations: PersonalizedRecommendation[] = [];

    // Evening routine recommendation
    if (analytics.sleepConsistency < 0.8) {
      const template = this.RECOMMENDATION_TEMPLATES.get('evening_routine')!;

      recommendations.push({
        id: 'evening-routine-' + Date.now(),
        ...template,
        description: 'A consistent pre-sleep routine can improve your sleep onset and quality, especially given your current sleep pattern variability.',
        actionableSteps: [
          'Start wind-down routine 1 hour before target bedtime',
          'Include relaxing activities: reading, gentle stretching, meditation',
          'Dim lights and reduce screen exposure 30 minutes before bed',
          'Keep the routine consistent, even on weekends'
        ],
        confidence: 0.8,
        relevanceScore: 0.7,
        personalizedInsight: 'Your sleep consistency could improve by up to 25% with a regular evening routine, based on sleep research patterns.',
        userSpecificData: {
          currentPattern: 'Irregular pre-sleep habits',
          targetPattern: 'Consistent 30-60 minute wind-down routine',
          improvement: 'Faster sleep onset and better sleep quality'
        }
      } as PersonalizedRecommendation);
    }

    // Screen time recommendation
    const template = this.RECOMMENDATION_TEMPLATES.get('screen_time')!;
    recommendations.push({
      id: 'screen-time-' + Date.now(),
      ...template,
      description: 'Reducing screen exposure before bed can help your brain naturally prepare for sleep and improve melatonin production.',
      actionableSteps: [
        'Stop using phones/tablets 1 hour before bedtime',
        'Use blue light filters if screens are necessary',
        'Replace evening screen time with relaxing activities',
        'Charge devices outside the bedroom to reduce temptation'
      ],
      confidence: 0.75,
      relevanceScore: 0.65,
      personalizedInsight: 'Blue light reduction in the evening typically improves sleep onset time by 15-30 minutes for most users.',
      userSpecificData: {
        currentPattern: 'Regular evening screen use',
        targetPattern: 'Screen-free hour before bed',
        improvement: 'Better melatonin production and faster sleep onset'
      }
    } as PersonalizedRecommendation);

    return recommendations;
  }

  private getOnboardingRecommendations(): PersonalizedRecommendation[] {
    // Return general recommendations for new users
    return [
      {
        id: 'onboarding-tracking',
        category: 'sleep_schedule',
        priority: 'high',
        title: 'Start Sleep Tracking',
        description: 'Begin tracking your sleep patterns to unlock personalized recommendations and smart alarm optimization.',
        actionableSteps: [
          'Enable Smart Sleep Insights in the app',
          'Log your bedtime and wake-up time for at least a week',
          'Rate your sleep quality each morning',
          'Allow location access for automatic sleep detection'
        ],
        expectedImpact: 'Personalized insights and recommendations within 7 days',
        confidence: 1.0,
        relevanceScore: 1.0,
        personalizedInsight: 'Once we have 7 days of sleep data, we can provide personalized recommendations specific to your patterns.',
        timeframe: 'short_term',
        daysToResults: 7,
        userSpecificData: {
          currentPattern: 'No sleep data available',
          targetPattern: 'Active sleep tracking',
          improvement: 'Unlock personalized recommendations'
        }
      } as PersonalizedRecommendation
    ];
  }

  private suggestOptimalBedtime(analytics: SleepPatternMetrics): string {
    // Calculate optimal bedtime based on average wake time and target sleep duration
    const wakeTimeMinutes = this.timeToMinutes(analytics.avgWakeupTime);
    const targetSleepHours = 8; // Optimal for most adults
    const bedtimeMinutes = wakeTimeMinutes - (targetSleepHours * 60);

    // Handle overnight calculation
    const adjustedBedtimeMinutes = bedtimeMinutes < 0 ? bedtimeMinutes + (24 * 60) : bedtimeMinutes;

    return this.minutesToTime(adjustedBedtimeMinutes);
  }

  private getOptimalSleepRange(age: number): { min: number; max: number; optimal: number } {
    if (age >= 18 && age <= 64) {
      return { min: 7, max: 9, optimal: 8 };
    } else if (age >= 65) {
      return { min: 7, max: 8, optimal: 7.5 };
    } else {
      return { min: 7, max: 9, optimal: 8 }; // Default adult range
    }
  }

  private timeToMinutes(time: string): number {
    const [hours, minutes] = time.split(':').map(Number);
    return hours * 60 + minutes;
  }

  private minutesToTime(minutes: number): string {
    const normalizedMinutes = ((minutes % (24 * 60)) + (24 * 60)) % (24 * 60);
    const hours = Math.floor(normalizedMinutes / 60);
    const mins = normalizedMinutes % 60;
    return `${hours.toString().padStart(2, '0')}:${mins.toString().padStart(2, '0')}`;
  }

  /**
   * Get recommendations filtered by category
   */
  public getRecommendationsByCategory(
    category: PersonalizedRecommendation['category']
  ): PersonalizedRecommendation[] {
    return this.generateRecommendations().filter(rec => rec.category === category);
  }

  /**
   * Get high-priority recommendations
   */
  public getCriticalRecommendations(): PersonalizedRecommendation[] {
    return this.generateRecommendations().filter(rec => rec.priority === 'critical' || rec.priority === 'high');
  }

  /**
   * Mark recommendation as implemented (for learning)
   */
  public markRecommendationImplemented(recommendationId: string, effectivenessRating?: 1 | 2 | 3 | 4 | 5): void {
    // Store implementation feedback for future recommendation improvements
    const implementationData = {
      recommendationId,
      implementedAt: new Date().toISOString(),
      effectivenessRating,
      userId: this.getCurrentUserId()
    };

    // Store in localStorage for now (could sync to backend later)
    const implementations = JSON.parse(localStorage.getItem('recommendation-implementations') || '[]');
    implementations.push(implementationData);
    localStorage.setItem('recommendation-implementations', JSON.stringify(implementations));
  }

  private getCurrentUserId(): string | null {
    try {
      const authData = JSON.parse(localStorage.getItem('smart-alarm-auth') || '{}');
      return authData?.state?.user?.id || null;
    } catch {
      return null;
    }
  }
}

// Create singleton instance
export const recommendationsEngine = RecommendationsEngine.getInstance();

// React hook for components
export function usePersonalizedRecommendations() {
  return {
    getRecommendations: () => recommendationsEngine.generateRecommendations(),
    getByCategory: (category: PersonalizedRecommendation['category']) =>
      recommendationsEngine.getRecommendationsByCategory(category),
    getCritical: () => recommendationsEngine.getCriticalRecommendations(),
    markImplemented: (id: string, rating?: 1 | 2 | 3 | 4 | 5) =>
      recommendationsEngine.markRecommendationImplemented(id, rating)
  };
}
