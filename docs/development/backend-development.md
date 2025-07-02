# Backend Development Guide - Multi-Language Service Architecture

Welcome to the backend development guide for the Smart Alarm system. This guide covers our sophisticated multi-language architecture where Go, Python, and Node.js services work together to create a reliable, intelligent, and secure alarm system for neurodivergent users.

## 🏗️ Architecture Philosophy: Specialized Services for Specialized Needs

Our backend architecture embodies a fundamental principle: different programming languages excel at different types of problems. Rather than forcing all functionality through a single technology stack, we've designed a system where each service uses the most appropriate technology for its specific responsibilities.

This approach creates some complexity in terms of deployment and inter-service communication, but the benefits far outweigh the costs. Each service can be optimized for its specific workload, scaled independently based on demand patterns, and developed by teams with expertise in the relevant technology stack.

The architecture also provides natural boundaries for different types of functionality, making it easier to maintain security boundaries, implement compliance requirements, and isolate failures to prevent cascading system outages.

## 🚀 Go Service: High-Performance Alarm Operations

The Go service handles the core alarm operations that form the backbone of user interactions. This service must be lightning-fast because delays in alarm operations directly impact user experience, particularly for neurodivergent users who might abandon slow interfaces or lose focus during lengthy operations.

### Service Architecture and Responsibilities

The Go service implements a clean architecture pattern with distinct layers for handling HTTP requests, business logic, and data persistence. This separation ensures that the service remains maintainable as complexity grows while providing clear boundaries for testing and debugging.

```go
// internal/handlers/alarms.go
// HTTP handlers optimized for speed and reliability

package handlers

import (
    "context"
    "encoding/json"
    "net/http"
    "time"
    
    "github.com/arbgjr/smart-alarm/internal/models"
    "github.com/arbgjr/smart-alarm/internal/services"
    "github.com/arbgjr/smart-alarm/pkg/validator"
    "github.com/gorilla/mux"
)

type AlarmHandler struct {
    alarmService *services.AlarmService
    validator    *validator.Validator
}

// CreateAlarm handles alarm creation with neurodivergent-specific validation
func (h *AlarmHandler) CreateAlarm(w http.ResponseWriter, r *http.Request) {
    ctx, cancel := context.WithTimeout(r.Context(), 30*time.Second)
    defer cancel()
    
    var req models.CreateAlarmRequest
    if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
        http.Error(w, "Invalid request format", http.StatusBadRequest)
        return
    }
    
    // Validate request with neurodivergent-specific rules
    if err := h.validator.ValidateAlarmRequest(req); err != nil {
        // Return detailed validation errors that help users fix issues
        validationResponse := models.ValidationErrorResponse{
            Message: "Please check the alarm details",
            Errors:  err.Details,
            Suggestions: h.generateSuggestions(err),
        }
        w.Header().Set("Content-Type", "application/json")
        w.WriteHeader(http.StatusBadRequest)
        json.NewEncoder(w).Encode(validationResponse)
        return
    }
    
    // Check for conflicts that might confuse neurodivergent users
    conflicts, err := h.alarmService.CheckConflicts(ctx, req.UserID, req.DateTime)
    if err != nil {
        http.Error(w, "Unable to check for conflicts", http.StatusInternalServerError)
        return
    }
    
    if len(conflicts) > 0 {
        conflictResponse := models.ConflictResponse{
            Message: "This alarm might conflict with your existing schedule",
            Conflicts: conflicts,
            Alternatives: h.alarmService.SuggestAlternatives(ctx, req),
        }
        w.Header().Set("Content-Type", "application/json")
        w.WriteHeader(http.StatusConflict)
        json.NewEncoder(w).Encode(conflictResponse)
        return
    }
    
    // Create the alarm with optimistic concurrency control
    alarm, err := h.alarmService.CreateAlarm(ctx, &req)
    if err != nil {
        // Log detailed error for debugging but return user-friendly message
        h.logger.Error("Failed to create alarm", "error", err, "userID", req.UserID)
        http.Error(w, "Unable to create alarm. Please try again.", http.StatusInternalServerError)
        return
    }
    
    // Return success response with created alarm details
    response := models.AlarmResponse{
        Alarm: alarm,
        Message: "Alarm created successfully",
        NextSteps: h.generateNextSteps(alarm),
    }
    
    w.Header().Set("Content-Type", "application/json")
    w.WriteHeader(http.StatusCreated)
    json.NewEncoder(w).Encode(response)
}

// generateSuggestions provides helpful suggestions for fixing validation errors
func (h *AlarmHandler) generateSuggestions(validationError *validator.ValidationError) []string {
    suggestions := make([]string, 0)
    
    for _, detail := range validationError.Details {
        switch detail.Field {
        case "title":
            if detail.Code == "required" {
                suggestions = append(suggestions, "Try a descriptive title like 'Take morning medication' or 'Leave for appointment'")
            } else if detail.Code == "too_long" {
                suggestions = append(suggestions, "Keep titles under 100 characters for clarity")
            }
        case "datetime":
            if detail.Code == "past_time" {
                suggestions = append(suggestions, "Choose a time in the future - did you mean tomorrow?")
            } else if detail.Code == "too_far_future" {
                suggestions = append(suggestions, "Alarms more than a year away might be better as calendar events")
            }
        }
    }
    
    return suggestions
}
```

### Data Models Optimized for Neurodivergent Users

The data models reflect the specific needs of neurodivergent users, including extensive accessibility options, conflict detection capabilities, and flexible recurrence patterns that accommodate irregular schedules.

```go
// internal/models/alarm.go
// Data models that capture the complexity of neurodivergent alarm needs

package models

import (
    "time"
    "database/sql/driver"
    "encoding/json"
)

// Alarm represents the core alarm entity with neurodivergent-specific features
type Alarm struct {
    ID          string    `json:"id" db:"id"`
    UserID      string    `json:"userId" db:"user_id"`
    Title       string    `json:"title" db:"title"`
    Description *string   `json:"description,omitempty" db:"description"`
    DateTime    time.Time `json:"datetime" db:"datetime"`
    Timezone    string    `json:"timezone" db:"timezone"`
    
    // Accessibility settings for neurodivergent users
    Accessibility AccessibilitySettings `json:"accessibility" db:"accessibility"`
    
    // Categorization helps with pattern recognition and AI analysis
    Category    AlarmCategory `json:"category" db:"category"`
    Priority    AlarmPriority `json:"priority" db:"priority"`
    Tags        []string      `json:"tags" db:"tags"`
    
    // Recurrence with flexible patterns for irregular schedules
    Recurrence  *RecurrencePattern `json:"recurrence,omitempty" db:"recurrence"`
    
    // State management with detailed tracking
    IsEnabled     bool       `json:"isEnabled" db:"is_enabled"`
    IsCompleted   bool       `json:"isCompleted" db:"is_completed"`
    CompletedAt   *time.Time `json:"completedAt,omitempty" db:"completed_at"`
    LastTriggered *time.Time `json:"lastTriggered,omitempty" db:"last_triggered"`
    
    // Optimistic concurrency control
    Version      int64     `json:"version" db:"version"`
    LastModified time.Time `json:"lastModified" db:"last_modified"`
    
    // Audit trail
    CreatedAt time.Time `json:"createdAt" db:"created_at"`
    CreatedBy string    `json:"createdBy" db:"created_by"`
}

// AccessibilitySettings captures neurodivergent-specific customizations
type AccessibilitySettings struct {
    // Visual accessibility options
    VisualCues          []VisualCue     `json:"visualCues"`
    HighContrast        bool            `json:"highContrast"`
    ReducedMotion       bool            `json:"reducedMotion"`
    
    // Audio accessibility options
    AudioOptions        AudioOptions    `json:"audioOptions"`
    CustomSoundURL      *string         `json:"customSoundUrl,omitempty"`
    
    // Interaction accessibility
    RequireConfirmation bool            `json:"requireConfirmation"`
    SnoozeOptions       []int           `json:"snoozeOptions"` // minutes
    VibrationPattern    []int           `json:"vibrationPattern,omitempty"`
    
    // Cognitive accessibility helpers
    ShowTimeRemaining   bool            `json:"showTimeRemaining"`
    ProvideContext      bool            `json:"provideContext"`
    AllowEasyReschedule bool            `json:"allowEasyReschedule"`
}

// AudioOptions provides granular control over audio notifications
type AudioOptions struct {
    Enabled    bool    `json:"enabled"`
    Volume     float64 `json:"volume"`     // 0.0 to 1.0
    SoundType  string  `json:"soundType"`  // gentle, standard, urgent, nature, custom
    Escalating bool    `json:"escalating"` // gradually increase volume if not responded
}

// RecurrencePattern supports complex scheduling needs
type RecurrencePattern struct {
    Type        RecurrenceType `json:"type"`
    Interval    int            `json:"interval"`
    DaysOfWeek  []time.Weekday `json:"daysOfWeek,omitempty"`
    DaysOfMonth []int          `json:"daysOfMonth,omitempty"`
    EndDate     *time.Time     `json:"endDate,omitempty"`
    
    // Advanced patterns for neurodivergent needs
    SkipHolidays    bool     `json:"skipHolidays"`
    AdaptToRoutine  bool     `json:"adaptToRoutine"`
    FlexibleTiming  bool     `json:"flexibleTiming"` // Allow AI to adjust timing slightly
}

// Enums for type safety and validation
type AlarmCategory string
const (
    CategoryMedication  AlarmCategory = "medication"
    CategoryAppointment AlarmCategory = "appointment"
    CategoryTask        AlarmCategory = "task"
    CategoryBreak       AlarmCategory = "break"
    CategoryTransition  AlarmCategory = "transition"
    CategoryExercise    AlarmCategory = "exercise"
    CategoryMeal        AlarmCategory = "meal"
    CategorySelfCare    AlarmCategory = "selfcare"
    CategoryCustom      AlarmCategory = "custom"
)

type AlarmPriority string
const (
    PriorityLow      AlarmPriority = "low"
    PriorityMedium   AlarmPriority = "medium"
    PriorityHigh     AlarmPriority = "high"
    PriorityCritical AlarmPriority = "critical"
)

// Custom JSON marshaling for database storage
func (as AccessibilitySettings) Value() (driver.Value, error) {
    return json.Marshal(as)
}

func (as *AccessibilitySettings) Scan(value interface{}) error {
    if value == nil {
        return nil
    }
    
    bytes, ok := value.([]byte)
    if !ok {
        return fmt.Errorf("cannot scan %T into AccessibilitySettings", value)
    }
    
    return json.Unmarshal(bytes, as)
}
```

### Business Logic Layer with Neurodivergent Considerations

The service layer implements complex business logic that considers the unique needs of neurodivergent users, including conflict detection, routine analysis, and intelligent scheduling suggestions.

```go
// internal/services/alarm_service.go
// Business logic optimized for neurodivergent user patterns

package services

import (
    "context"
    "fmt"
    "time"
    
    "github.com/arbgjr/smart-alarm/internal/models"
    "github.com/arbgjr/smart-alarm/internal/repository"
)

type AlarmService struct {
    alarmRepo       repository.AlarmRepository
    userRepo        repository.UserRepository
    conflictChecker *ConflictChecker
    routineAnalyzer *RoutineAnalyzer
}

// CreateAlarm implements neurodivergent-aware alarm creation
func (s *AlarmService) CreateAlarm(ctx context.Context, req *models.CreateAlarmRequest) (*models.Alarm, error) {
    // Validate and enrich alarm data
    alarm := &models.Alarm{
        ID:          generateAlarmID(),
        UserID:      req.UserID,
        Title:       req.Title,
        Description: req.Description,
        DateTime:    req.DateTime,
        Timezone:    req.Timezone,
        Category:    req.Category,
        Priority:    s.determinePriority(req),
        Accessibility: s.enrichAccessibilitySettings(req.Accessibility, req.Category),
        Recurrence:  req.Recurrence,
        IsEnabled:   true,
        Version:     1,
        CreatedAt:   time.Now(),
        CreatedBy:   req.UserID,
    }
    
    // Apply neurodivergent-specific optimizations
    if err := s.optimizeForUser(ctx, alarm); err != nil {
        return nil, fmt.Errorf("failed to optimize alarm: %w", err)
    }
    
    // Store alarm with transaction safety
    tx, err := s.alarmRepo.BeginTransaction(ctx)
    if err != nil {
        return nil, fmt.Errorf("failed to begin transaction: %w", err)
    }
    defer tx.Rollback()
    
    createdAlarm, err := s.alarmRepo.Create(ctx, tx, alarm)
    if err != nil {
        return nil, fmt.Errorf("failed to create alarm: %w", err)
    }
    
    // Schedule notification delivery
    if err := s.scheduleNotifications(ctx, tx, createdAlarm); err != nil {
        return nil, fmt.Errorf("failed to schedule notifications: %w", err)
    }
    
    // Update user routine analysis
    if err := s.routineAnalyzer.UpdateUserRoutine(ctx, tx, req.UserID, createdAlarm); err != nil {
        // Log error but don't fail alarm creation
        s.logger.Warn("Failed to update routine analysis", "error", err)
    }
    
    if err := tx.Commit(); err != nil {
        return nil, fmt.Errorf("failed to commit transaction: %w", err)
    }
    
    return createdAlarm, nil
}

// CheckConflicts identifies potential scheduling conflicts that might stress neurodivergent users
func (s *AlarmService) CheckConflicts(ctx context.Context, userID string, datetime time.Time) ([]models.Conflict, error) {
    // Get user's existing alarms in a time window around the proposed time
    timeWindow := 2 * time.Hour // Configurable based on user preferences
    startTime := datetime.Add(-timeWindow)
    endTime := datetime.Add(timeWindow)
    
    existingAlarms, err := s.alarmRepo.GetAlarmsByTimeRange(ctx, userID, startTime, endTime)
    if err != nil {
        return nil, fmt.Errorf("failed to get existing alarms: %w", err)
    }
    
    conflicts := make([]models.Conflict, 0)
    
    for _, existingAlarm := range existingAlarms {
        conflict := s.analyzeConflict(datetime, existingAlarm)
        if conflict != nil {
            conflicts = append(conflicts, *conflict)
        }
    }

    return conflicts, nil

    for _, existingAlarm := range existingAlarms {
        conflict := s.analyzeConflict(datetime, existingAlarm)
        if conflict != nil {
            conflicts = append(conflicts, *conflict)
        }
    }
    
    return conflicts, nil
}

// analyzeConflict determines if two alarms might create cognitive stress for neurodivergent users
func (s *AlarmService) analyzeConflict(newDateTime time.Time, existingAlarm *models.Alarm) *models.Conflict {
    timeDiff := newDateTime.Sub(existingAlarm.DateTime).Abs()
    
    // Different conflict windows based on alarm categories
    conflictWindow := s.getConflictWindow(existingAlarm.Category)
    
    if timeDiff < conflictWindow {
        severity := s.calculateConflictSeverity(newDateTime, existingAlarm, timeDiff)
        
        return &models.Conflict{
            ExistingAlarmID: existingAlarm.ID,
            ExistingTitle:   existingAlarm.Title,
            TimeDifference:  timeDiff,
            Severity:        severity,
            Reason:         s.generateConflictReason(existingAlarm, timeDiff),
            Suggestions:    s.generateConflictSuggestions(newDateTime, existingAlarm),
        }
    }
    
    return nil
}

// getConflictWindow returns appropriate time window based on alarm category
func (s *AlarmService) getConflictWindow(category models.AlarmCategory) time.Duration {
    switch category {
    case models.CategoryMedication:
        return 45 * time.Minute // Medications need careful spacing
    case models.CategoryAppointment:
        return 30 * time.Minute // Appointments need travel time
    case models.CategoryTransition:
        return 20 * time.Minute // Transitions need processing time
    case models.CategoryBreak:
        return 15 * time.Minute // Breaks are flexible but shouldn't overlap
    default:
        return 25 * time.Minute // General safety margin
    }
}

// optimizeForUser applies AI-driven optimizations based on user patterns
func (s *AlarmService) optimizeForUser(ctx context.Context, alarm *models.Alarm) error {
    // Get user's historical patterns
    userPatterns, err := s.routineAnalyzer.GetUserPatterns(ctx, alarm.UserID)
    if err != nil {
        // If we can't get patterns, use safe defaults
        s.applyDefaultOptimizations(alarm)
        return nil
    }
    
    // Optimize timing based on user's attention patterns
    if userPatterns.OptimalTimes != nil {
        optimizedTime := s.findOptimalTime(alarm.DateTime, userPatterns.OptimalTimes)
        if !optimizedTime.IsZero() && optimizedTime != alarm.DateTime {
            // Store original time and suggest optimization
            alarm.OriginalDateTime = &alarm.DateTime
            alarm.DateTime = optimizedTime
            alarm.OptimizationApplied = true
            alarm.OptimizationReason = "Adjusted to your peak attention time"
        }
    }
    
    // Optimize accessibility settings based on user preferences
    if userPatterns.AccessibilityPreferences != nil {
        s.mergeAccessibilitySettings(&alarm.Accessibility, userPatterns.AccessibilityPreferences)
    }
    
    // Optimize notification timing for category-specific patterns
    if alarm.Category == models.CategoryMedication {
        s.optimizeForMedication(alarm, userPatterns)
    } else if alarm.Category == models.CategoryTransition {
        s.optimizeForTransition(alarm, userPatterns)
    }
    
    return nil
}
```

## 🐍 Python Service: AI-Powered Behavioral Analysis

The Python service implements sophisticated machine learning algorithms that analyze user behavior patterns to provide personalized recommendations and adaptive timing suggestions. This service processes sensitive behavioral data using privacy-preserving techniques.

### AI Architecture for Neurodivergent Pattern Recognition

The AI service uses specialized models trained to recognize patterns common in neurodivergent populations, while ensuring that individual privacy is maintained through local processing and differential privacy techniques.

```python
# app/services/neurodivergent_analyzer.py
# Specialized AI analysis for neurodivergent behavioral patterns

import numpy as np
import pandas as pd
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
from datetime import datetime, timedelta
import logging
from typing import Dict, List, Optional, Tuple

from app.models.attention_patterns import AttentionPatternModel
from app.models.routine_analysis import RoutineAnalysisModel
from app.utils.privacy_preserving import DifferentialPrivacyEngine

logger = logging.getLogger(__name__)

class NeurodivergentBehaviorAnalyzer:
    """
    Analyzes behavioral patterns specific to neurodivergent users using
    privacy-preserving machine learning techniques.
    
    This service identifies patterns in alarm usage, response times, and
    interaction behaviors to provide personalized recommendations while
    protecting individual privacy through differential privacy.
    """
    
    def __init__(self):
        self.attention_model = AttentionPatternModel()
        self.routine_model = RoutineAnalysisModel()
        self.privacy_engine = DifferentialPrivacyEngine(epsilon=1.0)
        self.scaler = StandardScaler()
        
        # Load pre-trained models for neurodivergent pattern recognition
        self.adhd_attention_model = self._load_adhd_model()
        self.autism_routine_model = self._load_autism_model()
        self.executive_function_model = self._load_executive_function_model()
    
    async def analyze_user_patterns(self, user_id: str, alarm_history: List[Dict]) -> Dict:
        """
        Performs comprehensive analysis of user behavioral patterns.
        
        Args:
            user_id: Unique user identifier
            alarm_history: Historical alarm data for analysis
            
        Returns:
            Dictionary containing analysis results and personalized recommendations
        """
        try:
            logger.info(f"Starting behavioral analysis for user {user_id}")
            
            # Convert alarm history to structured format
            df = self._prepare_analysis_data(alarm_history)
            
            if df.empty:
                return self._generate_baseline_recommendations(user_id)
            
            # Extract features for different types of analysis
            temporal_features = self._extract_temporal_features(df)
            attention_features = self._extract_attention_features(df)
            routine_features = self._extract_routine_features(df)
            
            # Analyze different aspects of neurodivergent behavior
            attention_analysis = await self._analyze_attention_patterns(
                df, attention_features
            )
            routine_analysis = await self._analyze_routine_adherence(
                df, routine_features
            )
            executive_analysis = await self._analyze_executive_function(
                df, temporal_features
            )
            
            # Generate personalized recommendations
            recommendations = await self._generate_recommendations(
                attention_analysis, routine_analysis, executive_analysis
            )
            
            # Apply differential privacy to protect user data
            anonymized_insights = self.privacy_engine.anonymize_insights({
                'attention_patterns': attention_analysis,
                'routine_patterns': routine_analysis,
                'executive_function': executive_analysis
            })
            
            return {
                'user_id': user_id,
                'analysis_timestamp': datetime.utcnow().isoformat(),
                'patterns': anonymized_insights,
                'recommendations': recommendations,
                'confidence_scores': self._calculate_confidence_scores(df),
                'privacy_budget_used': self.privacy_engine.get_budget_usage(user_id)
            }
            
        except Exception as e:
            logger.error(f"Analysis failed for user {user_id}: {str(e)}")
            raise
    
    def _extract_attention_features(self, df: pd.DataFrame) -> np.ndarray:
        """
        Extracts features relevant to attention pattern analysis.
        
        Features include response time variability, time-of-day effects,
        distraction indicators, and hyperfocus detection markers.
        """
        features = []
        
        # Response time variability (key ADHD indicator)
        response_times = df['response_delay'].dropna()
        if len(response_times) > 1:
            features.extend([
                response_times.mean(),
                response_times.std(),
                response_times.var(),
                np.percentile(response_times, 95) - np.percentile(response_times, 5)
            ])
        else:
            features.extend([0, 0, 0, 0])
        
        # Time-of-day attention patterns
        hourly_performance = df.groupby('hour')['response_delay'].agg(['mean', 'count'])
        peak_hours = hourly_performance['mean'].nsmallest(3).index.tolist()
        worst_hours = hourly_performance['mean'].nlargest(3).index.tolist()
        
        features.extend([
            len(peak_hours),
            len(worst_hours),
            hourly_performance['mean'].max() - hourly_performance['mean'].min()
        ])
        
        # Hyperfocus detection features
        consecutive_good_responses = self._detect_consecutive_patterns(df, 'fast_response')
        features.extend([
            consecutive_good_responses['max_streak'],
            consecutive_good_responses['avg_streak'],
            consecutive_good_responses['frequency']
        ])
        
        # Distraction susceptibility indicators
        weekend_vs_weekday = self._compare_weekend_weekday_performance(df)
        features.append(weekend_vs_weekday['difference'])
        
        return np.array(features)
    
    def _analyze_attention_patterns(self, df: pd.DataFrame, features: np.ndarray) -> Dict:
        """
        Analyzes attention patterns using specialized ADHD detection models.
        
        Identifies hyperfocus periods, attention variability, optimal timing,
        and distraction susceptibility patterns.
        """
        analysis = {}
        
        # Attention variability analysis
        response_times = df['response_delay'].dropna()
        if len(response_times) > 5:
            variability_score = response_times.std() / (response_times.mean() + 1e-6)
            
            if variability_score > 1.5:
                consistency_rating = 'highly_variable'
            elif variability_score > 0.8:
                consistency_rating = 'moderately_variable'
            elif variability_score > 0.4:
                consistency_rating = 'somewhat_consistent'
            else:
                consistency_rating = 'very_consistent'
            
            analysis['attention_variability'] = {
                'variability_score': float(variability_score),
                'consistency_rating': consistency_rating,
                'likely_adhd_pattern': variability_score > 1.0
            }
        
        # Optimal attention periods identification
        hourly_stats = df.groupby('hour').agg({
            'response_delay': ['mean', 'count'],
            'completion_rate': 'mean'
        }).round(2)
        
        # Calculate performance score for each hour
        performance_scores = (
            hourly_stats[('completion_rate', 'mean')] * 0.6 - 
            (hourly_stats[('response_delay', 'mean')] / 60) * 0.4
        )
        
        optimal_hours = performance_scores.nlargest(3)
        analysis['optimal_attention_hours'] = [
            {
                'hour': int(hour),
                'performance_score': float(score),
                'sample_size': int(hourly_stats.loc[hour, ('response_delay', 'count')])
            }
            for hour, score in optimal_hours.items()
        ]
        
        # Hyperfocus detection
        hyperfocus_periods = self._detect_hyperfocus_periods(df)
        analysis['hyperfocus_indicators'] = {
            'detected_periods': len(hyperfocus_periods),
            'average_duration_minutes': np.mean([p['duration'] for p in hyperfocus_periods]) if hyperfocus_periods else 0,
            'frequency_per_week': len(hyperfocus_periods) / max(1, len(df) / 7),
            'periods': hyperfocus_periods[:5]  # Return top 5 for privacy
        }
        
        return analysis
    
    def _analyze_routine_adherence(self, df: pd.DataFrame, features: np.ndarray) -> Dict:
        """
        Analyzes routine patterns particularly relevant for autism spectrum users.
        
        Examines schedule consistency, resistance to change, and preference
        for predictable timing patterns.
        """
        analysis = {}
        
        # Routine consistency measurement
        if len(df) > 14:  # Need at least 2 weeks of data
            daily_patterns = df.groupby(['day_of_week', 'hour']).size()
            consistency_score = self._calculate_routine_consistency(daily_patterns)
            
            analysis['routine_consistency'] = {
                'consistency_score': float(consistency_score),
                'preferred_days': self._identify_preferred_days(df),
                'preferred_times': self._identify_preferred_times(df),
                'change_resistance': self._measure_change_resistance(df)
            }
        
        # Pattern stability over time
        if len(df) > 30:  # Need sufficient data for temporal analysis
            stability_analysis = self._analyze_pattern_stability(df)
            analysis['pattern_stability'] = stability_analysis
        
        return analysis
    
    def _detect_hyperfocus_periods(self, df: pd.DataFrame) -> List[Dict]:
        """
        Detects periods of sustained high performance that may indicate hyperfocus.
        
        Hyperfocus is characterized by extended periods of consistent, fast responses
        with high completion rates.
        """
        if len(df) < 10:
            return []
        
        # Calculate rolling performance metrics
        df_sorted = df.sort_values('scheduled_time')
        window_size = min(5, len(df) // 3)
        
        df_sorted['rolling_response_time'] = df_sorted['response_delay'].rolling(
            window=window_size, min_periods=3
        ).mean()
        
        df_sorted['rolling_completion_rate'] = df_sorted['is_completed'].rolling(
            window=window_size, min_periods=3
        ).mean()
        
        # Combine metrics for hyperfocus score
        response_time_threshold = df_sorted['response_delay'].quantile(0.3)  # Fast responses
        completion_threshold = 0.8  # High completion rate
        
        df_sorted['hyperfocus_score'] = (
            (df_sorted['rolling_response_time'] < response_time_threshold).astype(int) * 0.4 +
            (df_sorted['rolling_completion_rate'] > completion_threshold).astype(int) * 0.6
        )
        
        # Identify periods of sustained high performance
        hyperfocus_periods = []
        current_period = None
        
        for idx, row in df_sorted.iterrows():
            if row['hyperfocus_score'] > 0.7:  # High performance threshold
                if current_period is None:
                    current_period = {
                        'start_time': row['scheduled_time'],
                        'peak_score': row['hyperfocus_score'],
                        'alarm_count': 1
                    }
                else:
                    current_period['peak_score'] = max(
                        current_period['peak_score'], 
                        row['hyperfocus_score']
                    )
                    current_period['alarm_count'] += 1
                    current_period['end_time'] = row['scheduled_time']
            else:
                if current_period is not None:
                    # End current period if it lasted long enough
                    if current_period.get('end_time'):
                        duration = (current_period['end_time'] - current_period['start_time']).total_seconds() / 3600
                        if duration >= 1.0:  # At least 1 hour
                            current_period['duration'] = duration
                            hyperfocus_periods.append(current_period)
                    current_period = None
        
        return hyperfocus_periods
    
    async def _generate_recommendations(self, attention_analysis: Dict, 
                                       routine_analysis: Dict, 
                                       executive_analysis: Dict) -> List[Dict]:
        """
        Generates personalized recommendations based on all analysis results.
        
        Recommendations are tailored to specific neurodivergent patterns and
        prioritized based on potential impact and user safety.
        """
        recommendations = []
        
        # Attention-based recommendations
        if attention_analysis.get('attention_variability', {}).get('likely_adhd_pattern'):
            recommendations.append({
                'type': 'attention_management',
                'priority': 'high',
                'title': 'Optimize for Variable Attention',
                'description': 'Your attention varies throughout the day. These settings can help.',
                'specific_actions': [
                    'Enable backup alarms 5-10 minutes after important reminders',
                    'Use more prominent notifications during low-attention periods',
                    'Consider shorter, more frequent check-ins during complex tasks'
                ],
                'confidence': 0.85,
                'evidence': f"Attention variability score: {attention_analysis['attention_variability']['variability_score']:.2f}"
            })
        
        # Optimal timing recommendations
        optimal_hours = attention_analysis.get('optimal_attention_hours', [])
        if optimal_hours:
            best_hour = optimal_hours[0]['hour']
            recommendations.append({
                'type': 'timing_optimization',
                'priority': 'medium',
                'title': f'Schedule Important Tasks Around {best_hour}:00',
                'description': f'Your data shows peak performance around {best_hour}:00',
                'specific_actions': [
                    f'Move medication reminders to {best_hour}:00 when possible',
                    'Schedule complex or important alarms during this window',
                    'Use this time for tasks requiring sustained attention'
                ],
                'confidence': 0.78,
                'evidence': f"Performance score: {optimal_hours[0]['performance_score']:.2f}"
            })
        
        # Routine-based recommendations
        routine_consistency = routine_analysis.get('routine_consistency', {})
        if routine_consistency.get('consistency_score', 0) > 0.7:
            recommendations.append({
                'type': 'routine_optimization',
                'priority': 'medium',
                'title': 'Leverage Your Strong Routine Preferences',
                'description': 'You show consistent routine adherence - we can build on this strength.',
                'specific_actions': [
                    'Link new alarms to existing routine anchor points',
                    'Maintain consistent timing to support routine stability',
                    'Use routine-based triggers rather than just time-based ones'
                ],
                'confidence': 0.82,
                'evidence': f"Routine consistency: {routine_consistency.get('consistency_score', 0):.2f}"
            })
        
        # Hyperfocus management recommendations
        hyperfocus_data = attention_analysis.get('hyperfocus_indicators', {})
        if hyperfocus_data.get('detected_periods', 0) > 0:
            recommendations.append({
                'type': 'hyperfocus_management',
                'priority': 'medium',
                'title': 'Manage Hyperfocus Periods Effectively',
                'description': 'You show hyperfocus patterns - this can be both helpful and risky.',
                'specific_actions': [
                    'Set gentle break reminders during hyperfocus-prone times',
                    'Use more persistent notifications during deep focus periods',
                    'Consider "hyperfocus mode" with specialized notification settings'
                ],
                'confidence': 0.73,
                'evidence': f"Hyperfocus frequency: {hyperfocus_data.get('frequency_per_week', 0):.1f} times/week"
            })
        
        return sorted(recommendations, key=lambda x: x['confidence'], reverse=True)
```

## 🔗 Node.js Integration Service: Orchestrating Complex Workflows

The Node.js service acts as the conductor of your system orchestra, coordinating between specialized services and managing external integrations. This service handles the complex workflows that make the alarm system feel seamless and intelligent to users.

### Service Orchestration Patterns

The integration service implements sophisticated orchestration patterns that handle failures gracefully, provide comprehensive error recovery, and maintain performance even when external services are slow or unavailable.

```typescript
// src/services/orchestration-service.ts
// Coordinates complex workflows across multiple services and external APIs

import { CircuitBreaker } from '../utils/circuit-breaker';
import { RetryConfig, withRetry } from '../utils/retry';
import { Logger } from '../utils/logger';

// Type definitions to improve type safety
interface CircuitBreakerOptions {
    name: string;
    failureThreshold: number;
    resetTimeout: number;
    timeout?: number;
    fallbackFn?: (error: Error, args: any[]) => Promise<any>;
}

interface AlarmCreationData {
    title: string;
    dateTime: string;
    timezone: string;
    category: string;
    priority?: string;
    description?: string;
    accessibility?: AccessibilitySettings;
    recurrence?: RecurrencePattern;
}

interface AccessibilitySettings {
    allowEasyReschedule?: boolean;
    visualOptions?: Record<string, any>;
    audioOptions?: AudioOptions;
}

interface AudioOptions {
    enabled: boolean;
    escalating?: boolean;
}

interface RecurrencePattern {
    type: string;
    flexibleTiming?: boolean;
}

interface Alarm {
    id: string;
    userId: string;
    title: string;
    dateTime: string;
    [key: string]: any;
}

interface AIRecommendations {
    suggestions: Array<{
        type: string;
        title: string;
        description?: string;
        confidence: number;
    }>;
    isDefaultFallback?: boolean;
}

interface EnhancedAlarmResult {
    alarm: Alarm;
    aiRecommendations: AIRecommendations | null;
    appliedEnhancements: string[];
    workflowId: string;
    processingTime: number;
    fallbackMode?: boolean;
    fallbackReason?: string;
}

interface WorkflowResult {
    success: boolean;
    workflowType: string;
    workflowId: string;
    [key: string]: any;
}

export class WorkflowOrchestrationService {
    private goServiceUrl: string;
    private pythonServiceUrl: string;
    private circuitBreakers: Map<string, CircuitBreaker>;
    private logger: Logger;

    constructor() {
        this.goServiceUrl = process.env.GO_SERVICE_URL || 'http://alarm-service:8080';
        this.pythonServiceUrl = process.env.PYTHON_SERVICE_URL || 'http://ai-service:5000';
        this.logger = new Logger('WorkflowOrchestration');
        
        // Initialize circuit breakers for each service
        this.circuitBreakers = new Map([
            ['go-service', new CircuitBreaker({
                name: 'go-alarm-service',
                failureThreshold: 5,
                resetTimeout: 30000,
                timeout: 5000,
                fallbackFn: this.alarmServiceFallback.bind(this)
            })],
            ['python-service', new CircuitBreaker({
                name: 'python-ai-service',
                failureThreshold: 3,
                resetTimeout: 60000,
                timeout: 30000,
                fallbackFn: this.aiServiceFallback.bind(this)
            })]
        ]);
    }

    /**
     * Creates an alarm with AI enhancement and comprehensive error handling
     */
    async createEnhancedAlarm(userId: string, alarmData: AlarmCreationData): Promise<EnhancedAlarmResult> {
        const workflowId = this.generateWorkflowId();
        this.logger.info('Starting enhanced alarm creation', { workflowId, userId });

        try {
            // Phase 1: Get AI recommendations (non-blocking)
            const aiRecommendationsPromise = this.getAIRecommendationsWithFallback(
                userId, 
                alarmData, 
                workflowId
            );

            // Phase 2: Create basic alarm (critical path)
            const basicAlarm = await this.createBasicAlarm(userId, alarmData, workflowId);

            // Phase 3: Wait for AI recommendations and apply if available
            const aiRecommendations = await aiRecommendationsPromise;
            const enhancedAlarm = await this.applyAIEnhancementsIfAvailable(
                basicAlarm, 
                aiRecommendations, 
                workflowId
            );

            // Phase 4: Set up notifications and integrations (async)
            this.setupAsyncIntegrations(enhancedAlarm, workflowId);

            return {
                alarm: enhancedAlarm,
                aiRecommendations,
                appliedEnhancements: this.getAppliedEnhancements(basicAlarm, enhancedAlarm),
                workflowId,
                processingTime: Date.now() - parseInt(workflowId.split('-')[1])
            };

        } catch (error) {
            this.logger.error('Enhanced alarm creation failed', { 
                workflowId, 
                userId, 
                error: error.message 
            });
            
            // Attempt fallback to basic alarm creation
            try {
                const fallbackAlarm = await this.createBasicAlarm(userId, alarmData, workflowId);
                return {
                    alarm: fallbackAlarm,
                    aiRecommendations: null,
                    appliedEnhancements: [],
                    workflowId,
                    fallbackMode: true,
                    fallbackReason: error.message
                };
            } catch (fallbackError) {
                throw new Error(`Both enhanced and fallback alarm creation failed: ${fallbackError.message}`);
            }
        }
    }

    /**
     * Gets AI recommendations with comprehensive error handling and fallbacks
     */
    private async getAIRecommendationsWithFallback(
        userId: string, 
        alarmData: AlarmCreationData, 
        workflowId: string
    ): Promise<AIRecommendations | null> {
        const circuitBreaker = this.circuitBreakers.get('python-service');
        
        if (!circuitBreaker) {
            throw new Error('Circuit breaker for python service not found');
        }
        
        try {
            return await circuitBreaker.execute(async () => {
                const response = await withRetry(
                    () => fetch(`${this.pythonServiceUrl}/api/recommendations/analyze`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-Workflow-ID': workflowId,
                            'X-User-ID': userId
                        },
                        body: JSON.stringify({
                            alarmData,
                            context: {
                                timestamp: new Date().toISOString(),
                                timezone: alarmData.timezone
                            }
                        })
                    }),
                    { 
                        maxRetries: 2, 
                        baseDelay: 1000,
                        maxDelay: 5000
                    }
                );

                if (!response.ok) {
                    throw new Error(`AI service responded with ${response.status}`);
                }

                return await response.json();
            });

        } catch (error) {
            this.logger.warn('AI recommendations unavailable, using fallback', { 
                workflowId, 
                userId, 
                error: error.message 
            });
            
            // Return basic rule-based recommendations as fallback
            return this.generateFallbackRecommendations(alarmData);
        }
    }

    /**
     * Creates a basic alarm without AI enhancements
     */
    private async createBasicAlarm(
        userId: string, 
        alarmData: AlarmCreationData, 
        workflowId: string
    ): Promise<Alarm> {
        const circuitBreaker = this.circuitBreakers.get('go-service');
        
        if (!circuitBreaker) {
            throw new Error('Circuit breaker for Go service not found');
        }
        
        return await circuitBreaker.execute(async () => {
            const response = await withRetry(
                () => fetch(`${this.goServiceUrl}/api/alarms`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-Workflow-ID': workflowId,
                        'X-User-ID': userId
                    },
                    body: JSON.stringify(alarmData)
                }),
                { 
                    maxRetries: 3, 
                    baseDelay: 200,
                    maxDelay: 2000
                }
            );

            if (!response.ok) {
                throw new Error(`Alarm service responded with ${response.status}`);
            }

            return await response.json();
        }, [userId, alarmData]);
    }

    /**
     * Applies AI recommendations to enhance the basic alarm
     */
    private async applyAIEnhancementsIfAvailable(
        basicAlarm: Alarm, 
        recommendations: AIRecommendations | null, 
        workflowId: string
    ): Promise<Alarm> {
        if (!recommendations || recommendations.isDefaultFallback) {
            return basicAlarm;
        }

        try {
            const circuitBreaker = this.circuitBreakers.get('go-service');
            
            if (!circuitBreaker) {
                throw new Error('Circuit breaker for Go service not found');
            }
            
            return await circuitBreaker.execute(async () => {
                const enhancementResponse = await withRetry(
                    () => fetch(`${this.goServiceUrl}/api/alarms/${basicAlarm.id}/enhance`, {
                        method: 'PUT',
                        headers: {
                            'Content-Type': 'application/json',
                            'X-Workflow-ID': workflowId
                        },
                        body: JSON.stringify({
                            recommendations,
                            baseAlarm: basicAlarm
                        })
                    }),
                    { 
                        maxRetries: 2, 
                        baseDelay: 500
                    }
                );

                if (!enhancementResponse.ok) {
                    // If enhancement fails, still return the basic alarm
                    this.logger.warn('Failed to apply enhancements, using basic alarm', { 
                        workflowId, 
                        alarmId: basicAlarm.id 
                    });
                    return basicAlarm;
                }

                return await enhancementResponse.json();
            });
        } catch (error) {
            // If enhancement process fails, log but return basic alarm
            this.logger.warn('Enhancement process failed, using basic alarm', { 
                workflowId, 
                alarmId: basicAlarm.id,
                error: error.message 
            });
            return basicAlarm;
        }
    }

    /**
     * Processes complex neurodivergent-specific workflows
     */
    async processNeurodivergentWorkflow(
        userId: string, 
        workflowType: string, 
        data: any
    ): Promise<WorkflowResult> {
        const workflowId = this.generateWorkflowId();
        
        this.logger.info('Processing neurodivergent workflow', { 
            workflowId, 
            userId, 
            workflowType 
        });

        switch (workflowType) {
            case 'daily_routine_optimization':
                return await this.optimizeDailyRoutine(userId, data, workflowId);
            
            case 'medication_schedule_analysis':
                return await this.analyzeMedicationSchedule(userId, data, workflowId);
            
            case 'attention_pattern_adjustment':
                return await this.adjustForAttentionPatterns(userId, data, workflowId);
            
            case 'hyperfocus_management_setup':
                return await this.setupHyperfocusManagement(userId, data, workflowId);
            
            default:
                throw new Error(`Unknown workflow type: ${workflowType}`);
        }
    }

    /**
     * Optimizes daily routine based on AI analysis and current alarm patterns
     */
    private async optimizeDailyRoutine(
        userId: string, 
        routineData: any, 
        workflowId: string
    ): Promise<WorkflowResult> {
        try {
            // Step 1: Get current alarm configuration
            const currentAlarms = await this.executeWithCircuitBreaker(
                'go-service',
                () => fetch(`${this.goServiceUrl}/api/users/${userId}/alarms`)
                    .then(res => {
                        if (!res.ok) throw new Error(`Failed to get alarms: ${res.status}`);
                        return res.json();
                    })
            );

            // Step 2: Analyze current routine patterns
            const routineAnalysis = await this.executeWithCircuitBreaker(
                'python-service',
                () => fetch(`${this.pythonServiceUrl}/api/analysis/routine`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        userId,
                        alarms: currentAlarms,
                        preferences: routineData
                    })
                })
                .then(res => {
                    if (!res.ok) throw new Error(`Routine analysis failed: ${res.status}`);
                    return res.json();
                })
            );

            // Step 3: Generate optimization recommendations
            const optimizations = await this.executeWithCircuitBreaker(
                'python-service',
                () => fetch(`${this.pythonServiceUrl}/api/recommendations/routine-optimization`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        userId,
                        currentRoutine: currentAlarms,
                        analysis: routineAnalysis
                    })
                })
                .then(res => {
                    if (!res.ok) throw new Error(`Optimization generation failed: ${res.status}`);
                    return res.json();
                })
            );

            // Step 4: Apply approved optimizations
            const appliedOptimizations = [];
            const approvedOptimizations = optimizations.recommendations.filter(
                rec => rec.autoApprove || routineData.approvedRecommendations?.includes(rec.id)
            );

            for (const optimization of approvedOptimizations) {
                try {
                    const updatedAlarm = await this.executeWithCircuitBreaker(
                        'go-service',
                        () => fetch(`${this.goServiceUrl}/api/alarms/${optimization.alarmId}`, {
                            method: 'PUT',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify(optimization.changes)
                        })
                        .then(res => {
                            if (!res.ok) throw new Error(`Optimization application failed: ${res.status}`);
                            return res.json();
                        })
                    );
                    
                    appliedOptimizations.push({
                        ...optimization,
                        updatedAlarm
                    });
                } catch (error) {
                    this.logger.error('Failed to apply optimization', { 
                        workflowId, 
                        optimizationId: optimization.id, 
                        error: error.message 
                    });
                }
            }

            return {
                success: true,
                workflowType: 'daily_routine_optimization',
                workflowId,
                analysis: routineAnalysis,
                appliedOptimizations,
                pendingRecommendations: optimizations.recommendations.filter(
                    rec => !rec.autoApprove && !routineData.approvedRecommendations?.includes(rec.id)
                ),
                metrics: {
                    totalRecommendations: optimizations.recommendations.length,
                    appliedCount: appliedOptimizations.length,
                    processingTime: Date.now() - parseInt(workflowId.split('-')[1])
                }
            };

        } catch (error) {
            this.logger.error('Daily routine optimization failed', { 
                workflowId, 
                userId, 
                error: error.message 
            });
            
            return {
                success: false,
                workflowType: 'daily_routine_optimization',
                workflowId,
                error: error.message,
                fallbackSuggestions: this.generateFallbackRoutineSuggestions(routineData)
            };
        }
    }

    /**
     * Analyzes medication schedule for a user
     */
    private async analyzeMedicationSchedule(
        userId: string, 
        medicationData: any, 
        workflowId: string
    ): Promise<WorkflowResult> {
        // Implementation would follow similar pattern to optimizeDailyRoutine
        // with medication-specific logic
        throw new Error('Method not fully implemented');
    }

    /**
     * Adjusts alarms based on attention pattern analysis
     */
    private async adjustForAttentionPatterns(
        userId: string, 
        attentionData: any, 
        workflowId: string
    ): Promise<WorkflowResult> {
        // Implementation would follow similar pattern to optimizeDailyRoutine
        // with attention-pattern specific logic
        throw new Error('Method not fully implemented');
    }

    /**
     * Sets up hyperfocus management alarms and notifications
     */
    private async setupHyperfocusManagement(
        userId: string, 
        hyperfocusData: any, 
        workflowId: string
    ): Promise<WorkflowResult> {
        // Implementation would follow similar pattern to optimizeDailyRoutine
        // with hyperfocus-specific logic
        throw new Error('Method not fully implemented');
    }

    /**
     * Executes operations with circuit breaker protection
     */
    private async executeWithCircuitBreaker<T>(
        serviceName: string, 
        operation: () => Promise<T>
    ): Promise<T> {
        const circuitBreaker = this.circuitBreakers.get(serviceName);
        if (!circuitBreaker) {
            throw new Error(`No circuit breaker configured for service: ${serviceName}`);
        }

        return await circuitBreaker.execute(operation);
    }

    /**
     * Sets up asynchronous integrations that don't block alarm creation
     */
    private async setupAsyncIntegrations(alarm: Alarm, workflowId: string): Promise<void> {
        // These operations run in the background and don't affect alarm creation success
        const integrationTasks = [
            this.scheduleNotificationDelivery(alarm, workflowId),
            this.updateExternalCalendars(alarm.userId, alarm, workflowId),
            this.updateUserAnalytics(alarm.userId, alarm, workflowId)
        ];
        
        // Fire and forget - don't await results
        Promise.allSettled(integrationTasks)
            .then(results => {
                const failedCount = results.filter(r => r.status === 'rejected').length;
                if (failedCount > 0) {
                    this.logger.warn(`${failedCount} async integrations failed`, { workflowId, alarmId: alarm.id });
                }
            })
            .catch(err => {
                this.logger.error('Error in async integrations', { workflowId, error: err.message });
            });
    }

    /**
     * Schedules notification delivery for an alarm
     */
    private async scheduleNotificationDelivery(alarm: Alarm, workflowId: string): Promise<void> {
        try {
            await this.executeWithCircuitBreaker(
                'go-service',
                () => fetch(`${this.goServiceUrl}/api/notifications/schedule`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'X-Workflow-ID': workflowId },
                    body: JSON.stringify({ alarm })
                })
                .then(res => {
                    if (!res.ok) throw new Error(`Notification scheduling failed: ${res.status}`);
                    return res.json();
                })
            );
        } catch (error) {
            this.logger.warn('Failed to schedule notifications', { 
                workflowId, 
                alarmId: alarm.id, 
                error: error.message 
            });
        }
    }

    /**
     * Updates external calendar integrations with alarm information
     */
    private async updateExternalCalendars(userId: string, alarm: Alarm, workflowId: string): Promise<void> {
        try {
            await this.executeWithCircuitBreaker(
                'go-service',
                () => fetch(`${this.goServiceUrl}/api/integrations/calendar`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json', 
                        'X-User-ID': userId,
                        'X-Workflow-ID': workflowId 
                    },
                    body: JSON.stringify({ alarm })
                })
                .then(res => {
                    if (!res.ok) throw new Error(`Calendar update failed: ${res.status}`);
                    return res.json();
                })
            );
        } catch (error) {
            this.logger.warn('Failed to update external calendars', { 
                workflowId, 
                alarmId: alarm.id, 
                error: error.message 
            });
        }
    }

    /**
     * Updates user analytics with new alarm data
     */
    private async updateUserAnalytics(userId: string, alarm: Alarm, workflowId: string): Promise<void> {
        try {
            await this.executeWithCircuitBreaker(
                'python-service',
                () => fetch(`${this.pythonServiceUrl}/api/analytics/update`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json', 
                        'X-User-ID': userId,
                        'X-Workflow-ID': workflowId 
                    },
                    body: JSON.stringify({
                        alarmId: alarm.id,
                        alarmType: alarm.category,
                        eventType: 'creation'
                    })
                })
                .then(res => {
                    if (!res.ok) throw new Error(`Analytics update failed: ${res.status}`);
                    return res.json();
                })
            );
        } catch (error) {
            this.logger.warn('Failed to update user analytics', { 
                workflowId, 
                alarmId: alarm.id, 
                userId,
                error: error.message 
            });
        }
    }

    /**
     * Generates a unique workflow ID for tracing
     */
    private generateWorkflowId(): string {
        return `wf-${Date.now()}-${Math.random().toString(36).substring(2, 10)}`;
    }

    /**
     * Calculates differences between basic and enhanced alarms
     */
    private getAppliedEnhancements(basicAlarm: Alarm, enhancedAlarm: Alarm): string[] {
        const enhancements: string[] = [];
        
        // Compare properties between basic and enhanced alarms
        Object.keys(enhancedAlarm).forEach(key => {
            if (key !== 'id' && 
                key !== 'createdAt' && 
                JSON.stringify(basicAlarm[key]) !== JSON.stringify(enhancedAlarm[key])) {
                enhancements.push(`enhanced_${key}`);
            }
        });
        
        return enhancements;
    }

    /**
     * Generates fallback recommendations when AI service is unavailable
     */
    private generateFallbackRecommendations(alarmData: AlarmCreationData): AIRecommendations {
        const baseRecommendations = {
            suggestions: [
                {
                    type: 'default',
                    title: 'Standard notification settings',
                    description: 'Using default settings while AI recommendations are unavailable',
                    confidence: 0.8
                }
            ],
            isDefaultFallback: true
        };
        
        // Add category-specific recommendations
        if (alarmData.category === 'medication') {
            baseRecommendations.suggestions.push({
                type: 'medication_reminder',
                title: 'Medication standard reminder',
                description: 'Standard notification pattern for medication reminders',
                confidence: 0.9
            });
        }
        
        return baseRecommendations;
    }

    /**
     * Generates fallback routine suggestions
     */
    private generateFallbackRoutineSuggestions(routineData: any): any[] {
        return [
            {
                id: 'fallback-1',
                title: 'Maintain consistent wake-up times',
                description: 'Consistency helps establish stable routines',
                confidence: 0.85
            },
            {
                id: 'fallback-2',
                title: 'Schedule breaks between activities',
                description: 'Regular breaks help maintain focus and reduce overwhelm',
                confidence: 0.8
            }
        ];
    }

    /**
     * Fallback handler for alarm service failures
     */
    private async alarmServiceFallback(error: Error, args: any[]): Promise<any> {
        this.logger.warn('Using alarm service fallback', { error: error.message });
        
        // Implement minimal local alarm creation as fallback
        const [userId, alarmData] = args;
        
        return {
            id: `local-${Date.now()}`,
            userId,
            title: alarmData.title,
            dateTime: alarmData.dateTime,
            timezone: alarmData.timezone,
            category: alarmData.category,
            isLocalFallback: true,
            syncRequired: true,
            createdAt: new Date().toISOString()
        };
    }

    /**
     * Fallback handler for AI service failures
     */
    private async aiServiceFallback(error: Error, args: any[]): Promise<any> {
        this.logger.warn('Using AI service fallback', { error: error.message });
        
        // Return safe default recommendations
        return {
            suggestions: [
                {
                    type: 'default',
                    title: 'Standard settings applied',
                    description: 'Using default settings while personalization is unavailable',
                    confidence: 0.7
                }
            ],
            isDefaultFallback: true
        };
    }
}

## 📦 API Contract and Service Communication

The services communicate through well-defined APIs that enforce strict contracts. This approach ensures reliable communication while allowing each service to evolve independently.

### API Contract Design Principles

The system follows these key principles for API contracts:

1. **Language-Agnostic Contracts**: All API contracts are defined in OpenAPI (Swagger) and Protocol Buffers to ensure language independence.
2. **Versioned Endpoints**: APIs are versioned to allow non-breaking evolution.
3. **Comprehensive Documentation**: Each API includes detailed documentation of parameters, responses, error codes, and examples.
4. **Strong Validation**: Request and response validation is enforced at API boundaries.

```yaml
# api/specs/alarm-service-v1.yaml
# OpenAPI specification for the Go Alarm Service

openapi: 3.0.3
info:
    title: Smart Alarm API - Core Service
    description: Core alarm functionality optimized for neurodivergent users
    version: 1.0.0
    
servers:
    - url: /api/v1
    description: Base API path
    
paths:
    /alarms:
    post:
        summary: Create a new alarm
        description: |
        Creates a new alarm with neurodivergent-specific optimizations.
        This endpoint applies user-specific behavioral patterns and accessibility settings.
        operationId: createAlarm
        tags:
        - Alarms
        security:
        - BearerAuth: []
        requestBody:
        required: true
        content:
            application/json:
            schema:
                $ref: '#/components/schemas/CreateAlarmRequest'
        responses:
        '201':
            description: Alarm created successfully
            content:
            application/json:
                schema:
                $ref: '#/components/schemas/AlarmResponse'
        '400':
            description: Invalid request parameters
            content:
            application/json:
                schema:
                $ref: '#/components/schemas/ValidationError'
        '409':
            description: Conflicts detected with existing alarms
            content:
            application/json:
                schema:
                $ref: '#/components/schemas/ConflictResponse'
    
    /alarms/conflicts:
    post:
        summary: Check for potential scheduling conflicts
        description: |
        Analyzes potential conflicts with existing alarms based on neurodivergent-specific
        timing considerations and cognitive load factors.
        operationId: checkConflicts
        tags:
        - Alarms
        security:
        - BearerAuth: []
        requestBody:
        required: true
        content:
            application/json:
            schema:
                $ref: '#/components/schemas/ConflictCheckRequest'
        responses:
        '200':
            description: Conflict analysis results
            content:
            application/json:
                schema:
                $ref: '#/components/schemas/ConflictResponse'

components:
    schemas:
    CreateAlarmRequest:
        type: object
        required:
        - title
        - dateTime
        - timezone
        - category
        properties:
        title:
            type: string
            maxLength: 100
            example: "Take medication"
        description:
            type: string
            maxLength: 500
            example: "Take 10mg of medication with food"
        dateTime:
            type: string
            format: date-time
            example: "2025-06-15T08:30:00Z"
        timezone:
            type: string
            example: "America/New_York"
        category:
            type: string
            enum: [medication, appointment, task, break, transition, exercise, meal, selfcare, custom]
            example: "medication"
        priority:
            type: string
            enum: [low, medium, high, critical]
            example: "high"
        accessibility:
            $ref: '#/components/schemas/AccessibilitySettings'
        recurrence:
            $ref: '#/components/schemas/RecurrencePattern'
            
    # Other schema definitions omitted for brevity
```

## 🧪 Testing Strategies for Multi-Language Services

Testing a multi-language microservice architecture presents unique challenges. Our testing strategy addresses these challenges through a comprehensive approach.

### Unit Testing Each Service

Each service implements unit tests in its native language and testing framework:

- **Go Service**: Uses the standard `testing` package with testify for assertions
- **Python Service**: Uses pytest with pytest-mock for mocking
- **Node.js Service**: Uses Jest with TypeScript support

### Integration Testing Between Services

Integration tests verify that services communicate correctly across language boundaries:

```typescript
// tests/integration/alarm-workflow.test.ts
// Tests the end-to-end alarm creation workflow across services

import axios from 'axios';
import { TestEnvironment } from '../utils/test-environment';
import { generateTestUser, generateTestAlarm } from '../utils/test-data-generators';

describe('Alarm Creation Workflow', () => {
    let environment: TestEnvironment;
    let testUser: any;
    
    beforeAll(async () => {
        // Start all services in test mode with in-memory databases
        environment = new TestEnvironment();
        await environment.startServices();
        
        // Create test user that will be used across tests
        testUser = await environment.createTestUser(generateTestUser());
    });
    
    afterAll(async () => {
        await environment.stopServices();
    });
    
    test('should create alarm and apply behavioral analysis', async () => {
        // Given: A test alarm for a medication reminder
        const testAlarm = generateTestAlarm({
            category: 'medication',
            title: 'Take morning medication',
            dateTime: new Date().toISOString()
        });
        
        // When: Creating the alarm through the integration service
        const response = await axios.post(`${environment.integrationServiceUrl}/api/workflows/alarm`, 
            testAlarm,
            {
                headers: {
                    'Authorization': `Bearer ${testUser.token}`,
                    'Content-Type': 'application/json'
                }
            }
        );
        
        // Then: The alarm should be created successfully
        expect(response.status).toBe(201);
        expect(response.data.alarm).toBeDefined();
        expect(response.data.alarm.id).toBeDefined();
        
        // And: The behavioral analysis should be applied
        expect(response.data.enhancementsApplied).toContain('behavioral_analysis');
        
        // And: The alarm should be retrievable from the Go service
        const alarmResponse = await axios.get(
            `${environment.goServiceUrl}/api/alarms/${response.data.alarm.id}`,
            {
                headers: { 'Authorization': `Bearer ${testUser.token}` }
            }
        );
        
        expect(alarmResponse.status).toBe(200);
        expect(alarmResponse.data.id).toBe(response.data.alarm.id);
    });
    
    test('should handle service degradation gracefully', async () => {
        // Given: The AI service is unavailable
        await environment.stopService('python-ai-service');
        
        // When: Creating an alarm that would normally use AI features
        const testAlarm = generateTestAlarm({
            category: 'transition',
            title: 'Prepare for meeting',
            dateTime: new Date(Date.now() + 3600000).toISOString() // 1 hour from now
        });
        
        const response = await axios.post(`${environment.integrationServiceUrl}/api/workflows/alarm`,
            testAlarm,
            {
                headers: {
                    'Authorization': `Bearer ${testUser.token}`,
                    'Content-Type': 'application/json'
                }
            }
        );
        
        // Then: The alarm should still be created successfully
        expect(response.status).toBe(201);
        expect(response.data.alarm).toBeDefined();
        
        // And: The response should indicate degraded service
        expect(response.data.degradedService).toBe(true);
        expect(response.data.enhancementsApplied).not.toContain('behavioral_analysis');
        
        // Restore the service for other tests
        await environment.startService('python-ai-service');
    });
});
```

## 🔒 Security Considerations in Multi-Language Architecture

Security is paramount in a system that handles sensitive user data. Our approach to security addresses the unique challenges of a multi-language architecture.

### Consistent Authentication and Authorization

Despite using different languages, all services implement consistent security practices:

1. **JWT-Based Authentication**: All services validate the same JWT token format
2. **Role-Based Authorization**: Uniform role definitions are enforced across services
3. **User Context Propagation**: User context is maintained consistently across service boundaries

### Language-Specific Security Optimizations

Each service implements security best practices specific to its language:

```go
// internal/middleware/auth.go
// Go-specific authentication middleware

package middleware

import (
    "context"
    "fmt"
    "net/http"
    "strings"

    "github.com/arbgjr/smart-alarm/internal/models"
    "github.com/golang-jwt/jwt/v4"
)

// AuthMiddleware provides JWT validation and user context injection
func AuthMiddleware(jwtSecret string, adminOnly bool) func(http.Handler) http.Handler {
    return func(next http.Handler) http.Handler {
        return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
            authHeader := r.Header.Get("Authorization")
            if authHeader == "" {
                http.Error(w, "Authorization header required", http.StatusUnauthorized)
                return
            }

            // Extract bearer token
            parts := strings.Split(authHeader, " ")
            if len(parts) != 2 || parts[0] != "Bearer" {
                http.Error(w, "Invalid authorization format", http.StatusUnauthorized)
                return
            }
            tokenString := parts[1]

            // Parse and validate JWT
            token, err := jwt.ParseWithClaims(tokenString, &models.UserClaims{}, func(token *jwt.Token) (interface{}, error) {
                // Validate signing algorithm
                if _, ok := token.Method.(*jwt.SigningMethodHMAC); !ok {
                    return nil, fmt.Errorf("unexpected signing method: %v", token.Header["alg"])
                }
                return []byte(jwtSecret), nil
            })

            if err != nil {
                http.Error(w, "Invalid token", http.StatusUnauthorized)
                return
            }

            claims, ok := token.Claims.(*models.UserClaims)
            if !ok || !token.Valid {
                http.Error(w, "Invalid token claims", http.StatusUnauthorized)
                return
            }

            // Enforce role requirements if needed
            if adminOnly && !claims.HasRole("admin") {
                http.Error(w, "Admin role required", http.StatusForbidden)
                return
            }

            // Add validated user to context and continue
            ctx := context.WithValue(r.Context(), models.ContextKeyUser, claims)
            next.ServeHTTP(w, r.WithContext(ctx))
        })
    }
}
```

## 🚀 Deployment and DevOps Considerations

Deploying a multi-language microservice architecture requires specialized DevOps practices to ensure consistent builds, deployments, and monitoring across different technology stacks.

### Containerization and Orchestration

Each service is containerized with language-specific optimizations:

```dockerfile
# services/alarm-service/Dockerfile
# Multi-stage build for Go service

# Build stage
FROM golang:1.20-alpine AS build

WORKDIR /app

# Copy and download dependencies
COPY go.mod go.sum ./
RUN go mod download

# Copy source code
COPY . .

# Build with optimizations for production
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o alarm-service ./cmd/api

# Final stage with minimal runtime
FROM alpine:3.17

# Add security patches and CA certificates
RUN apk --no-cache add ca-certificates tzdata && \
    update-ca-certificates

# Create non-root user
RUN adduser -D -H -h /app appuser
WORKDIR /app

# Copy binary from build stage
COPY --from=build /app/alarm-service .
COPY --from=build /app/config ./config

# Use non-root user
USER appuser

# Health check for Kubernetes readiness/liveness probes
HEALTHCHECK --interval=30s --timeout=3s \
    CMD wget -qO- http://localhost:8080/health || exit 1

# Expose port and specify entrypoint
EXPOSE 8080
ENTRYPOINT ["./alarm-service"]
```

### Monitoring and Observability

Unified monitoring across different languages is implemented through common observability standards:

1. **Structured Logging**: All services output JSON logs with consistent fields
2. **Distributed Tracing**: OpenTelemetry integration tracks requests across service boundaries
3. **Metrics Collection**: Prometheus endpoints expose language-specific and common metrics
4. **Health Checks**: Standardized health check endpoints follow the same format across services

## 🧠 Conclusion

Our multi-language backend architecture provides several key advantages for the Smart Alarm system:

1. **Optimized Performance**: Each service uses the language best suited for its specific workload
2. **Specialized Expertise**: Development teams can leverage deep language-specific expertise
3. **Independent Scaling**: Services can be scaled based on their unique resource requirements
4. **Resilient Design**: Circuit breakers and graceful degradation ensure reliability even when services fail
5. **Future-Proof Architecture**: New specialized services can be added in appropriate languages as needs evolve

This architecture does introduce complexity in terms of deployment, testing, and monitoring, but the benefits significantly outweigh these costs, particularly for a system with the specialized requirements needed to serve neurodivergent users effectively.

By embracing language diversity rather than enforcing homogeneity, we've built a system that leverages the best tools for each job while maintaining overall cohesion through well-