# Multi-Language Backend Development Instructions - Go + Python + Node.js

## Hybrid Backend Architecture Philosophy

Your updated architecture represents a sophisticated approach to backend development that recognizes a fundamental truth: different programming languages excel at different tasks, and forcing everything through a single technology often creates unnecessary compromises. By implementing a **microservices specialization pattern**, you're optimizing each component of your system for maximum efficiency and maintainability.

This approach is particularly important for a neurodivergent-focused application where performance and reliability directly impact user wellbeing. When someone depends on your system for critical medication reminders, every millisecond of latency and every percentage point of reliability matters. The hybrid architecture allows you to achieve the performance characteristics needed for each specific function while maintaining overall system coherence.

Think of this architecture like a well-organized medical team - you have specialists (Go for high-performance operations, Python for complex analysis, Node.js for integrations) working together under a coordinated care plan, rather than asking one generalist to handle everything from surgery to therapy to administration.

## Go Service: High-Performance CRUD Operations

Go serves as your system's backbone, handling the rapid-fire alarm operations that form the core of user interactions. The choice of Go for this layer provides the low-latency, high-throughput performance that neurodivergent users need for reliable alarm management.

### Project Structure for Go Service

```
alarm-service/
├── cmd/
│   └── server/
│       └── main.go                 # Application entry point
├── internal/
│   ├── handlers/                   # HTTP request handlers
│   │   ├── alarms.go              # Alarm CRUD operations
│   │   ├── users.go               # User management
│   │   └── health.go              # Health check endpoints
│   ├── services/                   # Business logic layer
│   │   ├── alarm_service.go       # Core alarm business logic
│   │   ├── notification_service.go # Notification scheduling
│   │   └── sync_service.go        # Cross-device synchronization
│   ├── repository/                 # Data access layer
│   │   ├── alarm_repository.go    # Alarm data operations
│   │   └── user_repository.go     # User data operations
│   ├── models/                     # Data structures
│   │   ├── alarm.go               # Alarm entity definitions
│   │   └── user.go                # User entity definitions
│   └── middleware/                 # Cross-cutting concerns
│       ├── auth.go                # Authentication middleware
│       ├── logging.go             # Request logging
│       └── cors.go                # CORS handling
├── pkg/                           # Shared packages
│   ├── database/                  # Database connection utilities
│   ├── encryption/                # Data encryption utilities
│   └── validation/                # Input validation helpers
├── configs/                       # Configuration files
├── scripts/                       # Build and deployment scripts
└── tests/                        # Test suites
```

### Core Go Implementation

```go
// internal/models/alarm.go
package models

import (
    "time"
    "encoding/json"
)

// Alarm represents the core alarm entity optimized for fast operations
type Alarm struct {
    ID          string                 `json:"id" db:"id"`
    UserID      string                 `json:"userId" db:"user_id"`
    Title       string                 `json:"title" db:"title"`
    Description *string                `json:"description,omitempty" db:"description"`
    DateTime    time.Time              `json:"datetime" db:"datetime"`
    Timezone    string                 `json:"timezone" db:"timezone"`
    
    // Recurrence pattern stored as JSON for flexibility
    Recurrence  *json.RawMessage       `json:"recurrence,omitempty" db:"recurrence"`
    
    // Neurodivergent-specific settings
    Accessibility AccessibilitySettings `json:"accessibility" db:"accessibility"`
    
    // Categorization and priority
    Priority    Priority               `json:"priority" db:"priority"`
    Category    Category               `json:"category" db:"category"`
    Tags        []string               `json:"tags" db:"tags"`
    
    // State management
    IsEnabled   bool                   `json:"isEnabled" db:"is_enabled"`
    IsCompleted bool                   `json:"isCompleted" db:"is_completed"`
    CompletedAt *time.Time             `json:"completedAt,omitempty" db:"completed_at"`
    
    // Optimistic concurrency control
    Version     int64                  `json:"version" db:"version"`
    LastModified time.Time             `json:"lastModified" db:"last_modified"`
    LastModifiedBy string              `json:"lastModifiedBy" db:"last_modified_by"`
    
    // Audit fields
    CreatedAt   time.Time              `json:"createdAt" db:"created_at"`
    CreatedBy   string                 `json:"createdBy" db:"created_by"`
}

// AccessibilitySettings encapsulates neurodivergent-specific customizations
type AccessibilitySettings struct {
    VisualCues          []VisualCue     `json:"visualCues"`
    AudioOptions        AudioOptions    `json:"audioOptions"`
    VibrationPattern    []int           `json:"vibrationPattern,omitempty"`
    RequireConfirmation bool            `json:"requireConfirmation"`
    SnoozeOptions       []int           `json:"snoozeOptions"`
}

type AudioOptions struct {
    Enabled       bool    `json:"enabled"`
    Volume        float64 `json:"volume"`
    SoundType     string  `json:"soundType"`
    CustomSoundURL *string `json:"customSoundUrl,omitempty"`
}

type Priority string
const (
    PriorityLow      Priority = "low"
    PriorityMedium   Priority = "medium"
    PriorityHigh     Priority = "high"
    PriorityCritical Priority = "critical"
)

type Category string
const (
    CategoryMedication  Category = "medication"
    CategoryAppointment Category = "appointment"
    CategoryTask        Category = "task"
    CategoryExercise    Category = "exercise"
    CategoryMeal        Category = "meal"
    CategoryBreak       Category = "break"
    CategoryCustom      Category = "custom"
)
```

### High-Performance Alarm Service Implementation

```go
// internal/services/alarm_service.go
package services

import (
    "context"
    "fmt"
    "time"
    "alarm-service/internal/models"
    "alarm-service/internal/repository"
    "alarm-service/pkg/database"
)

// AlarmService handles core alarm business logic with optimized performance
type AlarmService struct {
    alarmRepo    repository.AlarmRepository
    userRepo     repository.UserRepository
    dbPool       *database.Pool
    
    // Performance optimization: connection pooling for high-throughput scenarios
    readOnlyPool *database.Pool
}

// NewAlarmService creates a new alarm service with performance optimizations
func NewAlarmService(alarmRepo repository.AlarmRepository, userRepo repository.UserRepository, dbPool *database.Pool) *AlarmService {
    // Create separate read-only connection pool for query optimization
    readOnlyPool := database.NewReadOnlyPool(dbPool.Config())
    
    return &AlarmService{
        alarmRepo:    alarmRepo,
        userRepo:     userRepo,
        dbPool:       dbPool,
        readOnlyPool: readOnlyPool,
    }
}

// CreateAlarm handles alarm creation with optimistic concurrency control
func (s *AlarmService) CreateAlarm(ctx context.Context, alarm *models.Alarm) (*models.Alarm, error) {
    // Validate alarm data with neurodivergent-specific rules
    if err := s.validateAlarmForNeurodivergentUser(alarm); err != nil {
        return nil, fmt.Errorf("alarm validation failed: %w", err)
    }
    
    // Check for scheduling conflicts that might confuse neurodivergent users
    conflicts, err := s.checkForNeurodivergentConflicts(ctx, alarm)
    if err != nil {
        return nil, fmt.Errorf("conflict checking failed: %w", err)
    }
    
    if len(conflicts) > 0 {
        return nil, &ConflictError{
            Message: "Alarm timing may conflict with existing routines",
            Conflicts: conflicts,
            Suggestions: s.generateAlternativeTimings(ctx, alarm),
        }
    }
    
    // Set initial values for new alarm
    now := time.Now()
    alarm.Version = 1
    alarm.CreatedAt = now
    alarm.LastModified = now
    alarm.IsEnabled = true
    alarm.IsCompleted = false
    
    // Use database transaction for consistency
    tx, err := s.dbPool.BeginTx(ctx, nil)
    if err != nil {
        return nil, fmt.Errorf("failed to begin transaction: %w", err)
    }
    defer tx.Rollback()
    
    // Create alarm in database
    createdAlarm, err := s.alarmRepo.Create(ctx, tx, alarm)
    if err != nil {
        return nil, fmt.Errorf("failed to create alarm: %w", err)
    }
    
    // Schedule notification delivery (this could trigger external services)
    if err := s.scheduleNotificationDelivery(ctx, tx, createdAlarm); err != nil {
        return nil, fmt.Errorf("failed to schedule notifications: %w", err)
    }
    
    // Commit transaction
    if err := tx.Commit(); err != nil {
        return nil, fmt.Errorf("failed to commit transaction: %w", err)
    }
    
    return createdAlarm, nil
}

// GetUserAlarms retrieves alarms with performance optimizations for large datasets
func (s *AlarmService) GetUserAlarms(ctx context.Context, userID string, filters AlarmFilters) (*AlarmList, error) {
    // Use read-only connection pool for better performance on read operations
    ctx = database.WithReadOnlyPool(ctx, s.readOnlyPool)
    
    // Implement cursor-based pagination for consistent performance
    alarms, nextCursor, err := s.alarmRepo.GetByUserWithCursor(ctx, userID, filters)
    if err != nil {
        return nil, fmt.Errorf("failed to retrieve alarms: %w", err)
    }
    
    // Calculate upcoming alarms for neurodivergent planning assistance
    upcomingAlarms, err := s.calculateUpcomingAlarms(ctx, alarms, 24*time.Hour)
    if err != nil {
        return nil, fmt.Errorf("failed to calculate upcoming alarms: %w", err)
    }
    
    return &AlarmList{
        Alarms:         alarms,
        NextCursor:     nextCursor,
        UpcomingAlarms: upcomingAlarms,
        TotalCount:     len(alarms), // This could be optimized with a separate count query
    }, nil
}

// UpdateAlarm handles alarm updates with conflict resolution
func (s *AlarmService) UpdateAlarm(ctx context.Context, userID, alarmID string, updates *models.AlarmUpdate) (*models.Alarm, error) {
    // Start transaction for atomic operations
    tx, err := s.dbPool.BeginTx(ctx, nil)
    if err != nil {
        return nil, fmt.Errorf("failed to begin transaction: %w", err)
    }
    defer tx.Rollback()
    
    // Retrieve current alarm with row-level locking
    currentAlarm, err := s.alarmRepo.GetByIDForUpdate(ctx, tx, userID, alarmID)
    if err != nil {
        return nil, fmt.Errorf("failed to retrieve alarm for update: %w", err)
    }
    
    // Check version for optimistic concurrency control
    if updates.Version != 0 && updates.Version != currentAlarm.Version {
        // Return detailed conflict information for client-side resolution
        return nil, &VersionConflictError{
            ExpectedVersion: updates.Version,
            CurrentVersion:  currentAlarm.Version,
            CurrentAlarm:    currentAlarm,
            ConflictResolution: s.generateConflictResolutionOptions(currentAlarm, updates),
        }
    }
    
    // Apply updates with validation
    updatedAlarm := s.applyUpdatesToAlarm(currentAlarm, updates)
    updatedAlarm.Version++
    updatedAlarm.LastModified = time.Now()
    
    // Validate updated alarm
    if err := s.validateAlarmForNeurodivergentUser(updatedAlarm); err != nil {
        return nil, fmt.Errorf("updated alarm validation failed: %w", err)
    }
    
    // Save updated alarm
    savedAlarm, err := s.alarmRepo.Update(ctx, tx, updatedAlarm)
    if err != nil {
        return nil, fmt.Errorf("failed to update alarm: %w", err)
    }
    
    // Update notification scheduling if timing changed
    if s.hasTimingChanged(currentAlarm, updatedAlarm) {
        if err := s.rescheduleNotifications(ctx, tx, savedAlarm); err != nil {
            return nil, fmt.Errorf("failed to reschedule notifications: %w", err)
        }
    }
    
    // Commit transaction
    if err := tx.Commit(); err != nil {
        return nil, fmt.Errorf("failed to commit update: %w", err)
    }
    
    return savedAlarm, nil
}

// Performance optimization: Check for conflicts that specifically matter to neurodivergent users
func (s *AlarmService) checkForNeurodivergentConflicts(ctx context.Context, alarm *models.Alarm) ([]ConflictInfo, error) {
    var conflicts []ConflictInfo
    
    // Check for timing conflicts within sensitive windows (30 minutes for medication, 15 minutes for others)
    conflictWindow := 30 * time.Minute
    if alarm.Category != models.CategoryMedication {
        conflictWindow = 15 * time.Minute
    }
    
    nearbyAlarms, err := s.alarmRepo.GetAlarmsInTimeWindow(
        ctx, 
        alarm.UserID, 
        alarm.DateTime.Add(-conflictWindow), 
        alarm.DateTime.Add(conflictWindow),
    )
    if err != nil {
        return nil, err
    }
    
    for _, nearby := range nearbyAlarms {
        // Skip if it's the same alarm (for updates)
        if nearby.ID == alarm.ID {
            continue
        }
        
        // Calculate conflict severity based on categories and timing
        severity := s.calculateConflictSeverity(alarm, nearby)
        if severity > ConflictSeverityNone {
            conflicts = append(conflicts, ConflictInfo{
                ConflictingAlarm: nearby,
                Severity:         severity,
                TimeDifference:   alarm.DateTime.Sub(nearby.DateTime),
                Reason:          s.generateConflictReason(alarm, nearby),
            })
        }
    }
    
    return conflicts, nil
}

// Validate alarm data with neurodivergent-specific considerations
func (s *AlarmService) validateAlarmForNeurodivergentUser(alarm *models.Alarm) error {
    // Title validation: clear and not overwhelming
    if len(alarm.Title) == 0 {
        return fmt.Errorf("alarm title cannot be empty")
    }
    if len(alarm.Title) > 100 {
        return fmt.Errorf("alarm title too long (max 100 characters for clarity)")
    }
    
    // Time validation: not too far in the future (planning difficulties)
    if alarm.DateTime.After(time.Now().Add(365 * 24 * time.Hour)) {
        return fmt.Errorf("alarm cannot be scheduled more than 1 year in advance")
    }
    
    // Time validation: not in the past
    if alarm.DateTime.Before(time.Now().Add(-5 * time.Minute)) {
        return fmt.Errorf("alarm cannot be scheduled in the past")
    }
    
    // Accessibility validation: ensure required options are present
    if len(alarm.Accessibility.SnoozeOptions) == 0 {
        // Provide sensible defaults for neurodivergent users
        alarm.Accessibility.SnoozeOptions = []int{5, 10, 15}
    }
    
    // Validate snooze options are reasonable (not too short or too long)
    for _, snooze := range alarm.Accessibility.SnoozeOptions {
        if snooze < 1 || snooze > 60 {
            return fmt.Errorf("snooze options must be between 1 and 60 minutes")
        }
    }
    
    // Category-specific validation
    if err := s.validateCategorySpecificRules(alarm); err != nil {
        return err
    }
    
    return nil
}

// Category-specific validation for different alarm types
func (s *AlarmService) validateCategorySpecificRules(alarm *models.Alarm) error {
    switch alarm.Category {
    case models.CategoryMedication:
        // Medication alarms need higher reliability settings
        if !alarm.Accessibility.RequireConfirmation {
            return fmt.Errorf("medication alarms must require confirmation for safety")
        }
        if alarm.Priority == models.PriorityLow {
            return fmt.Errorf("medication alarms cannot have low priority")
        }
        
    case models.CategoryBreak:
        // Break alarms should have gentler notification settings
        if alarm.Accessibility.AudioOptions.Volume > 0.7 {
            return fmt.Errorf("break alarms should use gentler audio settings (max volume 0.7)")
        }
        
    case models.CategoryTask:
        // Task alarms benefit from specific timing considerations
        hour := alarm.DateTime.Hour()
        if hour < 6 || hour > 22 {
            return fmt.Errorf("task alarms should be scheduled during reasonable hours (6 AM - 10 PM)")
        }
    }
    
    return nil
}
```

## Python Service: AI Processing and Behavioral Analysis

The Python service handles the sophisticated AI analysis that makes your alarm system truly intelligent for neurodivergent users. Python's rich ecosystem of machine learning libraries makes it the ideal choice for processing complex behavioral patterns and generating personalized recommendations.

### Project Structure for Python Service

```
ai-service/
├── app/
│   ├── __init__.py
│   ├── main.py                     # FastAPI application entry point
│   ├── api/                        # API route definitions
│   │   ├── __init__.py
│   │   ├── analysis.py            # Pattern analysis endpoints
│   │   ├── recommendations.py     # AI recommendation endpoints
│   │   └── health.py              # Health check endpoints
│   ├── services/                   # Business logic layer
│   │   ├── __init__.py
│   │   ├── pattern_analyzer.py    # Behavioral pattern analysis
│   │   ├── recommendation_engine.py # AI recommendation generation
│   │   └── neurodivergent_analyzer.py # Specialized ND analysis
│   ├── models/                     # ML models and data structures
│   │   ├── __init__.py
│   │   ├── attention_patterns.py  # ADHD attention modeling
│   │   ├── routine_analysis.py    # Autism routine pattern modeling
│   │   └── time_management.py     # Executive function modeling
│   ├── ml/                         # Machine learning utilities
│   │   ├── __init__.py
│   │   ├── feature_extraction.py  # Feature engineering
│   │   ├── model_training.py      # Model training utilities
│   │   └── privacy_preserving.py  # Differential privacy implementation
│   └── utils/                      # Utility functions
│       ├── __init__.py
│       ├── data_processing.py     # Data preprocessing utilities
│       └── encryption.py          # Data encryption utilities
├── requirements.txt                # Python dependencies
├── Dockerfile                     # Container configuration
└── tests/                         # Test suites
```

### Core Python Implementation

```python
# app/services/neurodivergent_analyzer.py
from typing import List, Dict, Any, Optional
import pandas as pd
import numpy as np
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
from datetime import datetime, timedelta
import logging

from app.models.attention_patterns import AttentionPatternModel
from app.models.routine_analysis import RoutineAnalysisModel
from app.utils.data_processing import DataProcessor

logger = logging.getLogger(__name__)

class NeurodivergentAnalyzer:
    """
    Specialized analyzer for neurodivergent behavioral patterns.
    
    This service processes user alarm and interaction data to identify
    patterns specific to ADHD, autism, and other neurodivergent conditions,
    providing personalized insights and recommendations.
    """
    
    def __init__(self):
        self.attention_model = AttentionPatternModel()
        self.routine_model = RoutineAnalysisModel()
        self.data_processor = DataProcessor()
        self.scaler = StandardScaler()
        
        # Initialize models for different neurodivergent profiles
        self._initialize_neurodivergent_models()
    
    def _initialize_neurodivergent_models(self):
        """Initialize specialized models for different neurodivergent patterns."""
        # ADHD-specific pattern recognition
        self.adhd_patterns = {
            'hyperfocus_detection': self._create_hyperfocus_model(),
            'attention_switching': self._create_attention_switching_model(),
            'time_blindness': self._create_time_blindness_model()
        }
        
        # Autism-specific pattern recognition
        self.autism_patterns = {
            'routine_importance': self._create_routine_importance_model(),
            'sensory_preferences': self._create_sensory_preference_model(),
            'transition_difficulty': self._create_transition_model()
        }
        
        # Executive function pattern recognition
        self.executive_function_patterns = {
            'planning_difficulty': self._create_planning_model(),
            'task_initiation': self._create_task_initiation_model(),
            'working_memory': self._create_working_memory_model()
        }
    
    async def analyze_user_patterns(self, user_id: str, alarm_history: List[Dict]) -> Dict[str, Any]:
        """
        Analyze comprehensive user patterns for neurodivergent characteristics.
        
        Args:
            user_id: Unique identifier for the user
            alarm_history: Historical alarm data for analysis
            
        Returns:
            Dictionary containing pattern analysis results and recommendations
        """
        try:
            logger.info(f"Starting pattern analysis for user {user_id}")
            
            # Convert alarm history to structured format for analysis
            df = self._prepare_alarm_data(alarm_history)
            
            if df.empty:
                return self._generate_baseline_recommendations()
            
            # Extract temporal features that matter for neurodivergent users
            temporal_features = self._extract_temporal_features(df)
            
            # Analyze attention patterns (especially important for ADHD)
            attention_analysis = await self._analyze_attention_patterns(df, temporal_features)
            
            # Analyze routine adherence (especially important for autism)
            routine_analysis = await self._analyze_routine_patterns(df, temporal_features)
            
            # Analyze executive function indicators
            executive_analysis = await self._analyze_executive_function(df, temporal_features)
            
            # Generate personalized recommendations
            recommendations = await self._generate_personalized_recommendations(
                attention_analysis, routine_analysis, executive_analysis
            )
            
            # Calculate confidence scores for all analyses
            confidence_scores = self._calculate_confidence_scores(
                attention_analysis, routine_analysis, executive_analysis
            )
            
            return {
                'user_id': user_id,
                'analysis_timestamp': datetime.utcnow().isoformat(),
                'attention_patterns': attention_analysis,
                'routine_patterns': routine_analysis,
                'executive_function': executive_analysis,
                'recommendations': recommendations,
                'confidence_scores': confidence_scores,
                'privacy_note': 'All analysis performed locally with differential privacy'
            }
            
        except Exception as e:
            logger.error(f"Pattern analysis failed for user {user_id}: {str(e)}")
            raise
    
    def _prepare_alarm_data(self, alarm_history: List[Dict]) -> pd.DataFrame:
        """Convert raw alarm history to structured DataFrame for analysis."""
        if not alarm_history:
            return pd.DataFrame()
        
        df = pd.DataFrame(alarm_history)
        
        # Convert datetime strings to datetime objects
        df['scheduled_time'] = pd.to_datetime(df['datetime'])
        df['completed_time'] = pd.to_datetime(df.get('completed_at', None))
        df['responded_time'] = pd.to_datetime(df.get('responded_at', None))
        
        # Calculate response metrics
        df['response_delay'] = (df['responded_time'] - df['scheduled_time']).dt.total_seconds() / 60
        df['completion_delay'] = (df['completed_time'] - df['scheduled_time']).dt.total_seconds() / 60
        
        # Extract time-based features
        df['hour'] = df['scheduled_time'].dt.hour
        df['day_of_week'] = df['scheduled_time'].dt.dayofweek
        df['is_weekend'] = df['day_of_week'].isin([5, 6])
        
        # Add derived features for neurodivergent analysis
        df['time_of_day_category'] = df['hour'].apply(self._categorize_time_of_day)
        df['is_routine_time'] = df.apply(self._identify_routine_times, axis=1)
        
        return df
    
    async def _analyze_attention_patterns(self, df: pd.DataFrame, features: Dict) -> Dict[str, Any]:
        """
        Analyze attention patterns that may indicate ADHD characteristics.
        
        This analysis looks for:
        - Variable response times indicating attention fluctuations
        - Hyperfocus periods where user is highly consistent
        - Time-of-day effects on attention
        - Task-switching difficulties
        """
        attention_analysis = {
            'attention_variability': self._calculate_attention_variability(df),
            'optimal_attention_hours': self._identify_optimal_attention_periods(df),
            'hyperfocus_indicators': self._detect_hyperfocus_periods(df),
            'attention_decline_patterns': self._analyze_attention_decline(df),
            'distraction_susceptibility': self._analyze_distraction_patterns(df)
        }
        
        # Use ML model to classify attention patterns
        if len(df) > 10:  # Need sufficient data for ML analysis
            attention_features = self._extract_attention_features(df)
            attention_analysis['predicted_adhd_likelihood'] = self.adhd_patterns['hyperfocus_detection'].predict_proba([attention_features])[0][1]
        
        return attention_analysis
    
    async def _analyze_routine_patterns(self, df: pd.DataFrame, features: Dict) -> Dict[str, Any]:
        """
        Analyze routine adherence patterns that may indicate autism characteristics.
        
        This analysis looks for:
        - Consistency in daily routines
        - Resistance to schedule changes
        - Preference for predictable timing
        - Stress indicators when routines are disrupted
        """
        routine_analysis = {
            'routine_consistency': self._calculate_routine_consistency(df),
            'preferred_time_patterns': self._identify_preferred_times(df),
            'change_resistance': self._analyze_change_resistance(df),
            'routine_disruption_impact': self._analyze_disruption_impact(df),
            'predictability_preferences': self._analyze_predictability_needs(df)
        }
        
        # Detect strong routine preferences that may indicate autism spectrum traits
        if len(df) > 20:
            routine_features = self._extract_routine_features(df)
            routine_analysis['routine_importance_score'] = self.autism_patterns['routine_importance'].predict([routine_features])[0]
        
        return routine_analysis
    
    async def _analyze_executive_function(self, df: pd.DataFrame, features: Dict) -> Dict[str, Any]:
        """
        Analyze executive function patterns across multiple neurodivergent conditions.
        
        This analysis looks for:
        - Planning and organization difficulties
        - Task initiation challenges
        - Working memory limitations
        - Time management issues
        """
        executive_analysis = {
            'planning_effectiveness': self._assess_planning_patterns(df),
            'task_initiation_delay': self._analyze_task_initiation(df),
            'time_estimation_accuracy': self._analyze_time_estimation(df),
            'working_memory_indicators': self._assess_working_memory(df),
            'cognitive_load_tolerance': self._analyze_cognitive_load(df)
        }
        
        return executive_analysis
    
    def _calculate_attention_variability(self, df: pd.DataFrame) -> Dict[str, float]:
        """Calculate metrics indicating attention variability (ADHD indicator)."""
        if 'response_delay' not in df.columns or df['response_delay'].isna().all():
            return {'variability_score': 0.0, 'consistency_rating': 'insufficient_data'}
        
        # Remove outliers for more accurate analysis
        response_delays = df['response_delay'].dropna()
        Q1 = response_delays.quantile(0.25)
        Q3 = response_delays.quantile(0.75)
        IQR = Q3 - Q1
        filtered_delays = response_delays[(response_delays >= Q1 - 1.5*IQR) & (response_delays <= Q3 + 1.5*IQR)]
        
        variability = filtered_delays.std() / (filtered_delays.mean() + 1e-6)  # Coefficient of variation
        
        # Classify variability level
        if variability < 0.3:
            consistency_rating = 'very_consistent'
        elif variability < 0.6:
            consistency_rating = 'moderately_consistent'
        elif variability < 1.0:
            consistency_rating = 'variable'
        else:
            consistency_rating = 'highly_variable'
        
        return {
            'consistency_rating': consistency_rating,
            'mean_response_time': float(filtered_delays.mean()),
            'response_time_range': float(filtered_delays.max() - filtered_delays.min())
        }
    
    def _identify_optimal_attention_periods(self, df: pd.DataFrame) -> List[Dict[str, Any]]:
        """Identify time periods when user shows optimal attention and response."""
        if len(df) < 5:
            return []
        
        # Group by hour and calculate performance metrics
        hourly_performance = df.groupby('hour').agg({
            'response_delay': ['mean', 'std', 'count'],
            'is_completed': 'mean'
        }).round(2)
        
        # Flatten column names
        hourly_performance.columns = ['_'.join(col).strip() for col in hourly_performance.columns]
        
        # Identify optimal hours (low response delay, high completion rate)
        hourly_performance['performance_score'] = (
            hourly_performance['is_completed_mean'] * 0.6 - 
            (hourly_performance['response_delay_mean'] / 60) * 0.4  # Normalize to 0-1 scale
        )
        
        # Get top performing hours
        optimal_hours = hourly_performance.nlargest(3, 'performance_score')
        
        return [
            {
                'hour': int(hour),
                'performance_score': float(row['performance_score']),
                'avg_response_time': float(row['response_delay_mean']),
                'completion_rate': float(row['is_completed_mean']),
                'sample_size': int(row['response_delay_count'])
            }
            for hour, row in optimal_hours.iterrows()
        ]
    
    def _detect_hyperfocus_periods(self, df: pd.DataFrame) -> Dict[str, Any]:
        """Detect periods of hyperfocus (extended periods of high performance)."""
        if len(df) < 10:
            return {'hyperfocus_detected': False, 'periods': []}
        
        # Sort by time and calculate rolling performance
        df_sorted = df.sort_values('scheduled_time')
        df_sorted['rolling_performance'] = df_sorted['is_completed'].rolling(window=5, min_periods=3).mean()
        df_sorted['rolling_response_consistency'] = 1 / (df_sorted['response_delay'].rolling(window=5, min_periods=3).std() + 1)
        
        # Combine metrics for hyperfocus detection
        df_sorted['hyperfocus_score'] = (
            df_sorted['rolling_performance'] * 0.6 + 
            df_sorted['rolling_response_consistency'] * 0.4
        )
        
        # Identify hyperfocus periods (high score for extended time)
        hyperfocus_threshold = df_sorted['hyperfocus_score'].quantile(0.8)
        hyperfocus_periods = []
        
        current_period = None
        for idx, row in df_sorted.iterrows():
            if row['hyperfocus_score'] > hyperfocus_threshold:
                if current_period is None:
                    current_period = {
                        'start': row['scheduled_time'],
                        'end': row['scheduled_time'],
                        'peak_score': row['hyperfocus_score']
                    }
                else:
                    current_period['end'] = row['scheduled_time']
                    current_period['peak_score'] = max(current_period['peak_score'], row['hyperfocus_score'])
            else:
                if current_period is not None:
                    # Only count periods longer than 2 hours
                    duration = (current_period['end'] - current_period['start']).total_seconds() / 3600
                    if duration >= 2:
                        current_period['duration_hours'] = duration
                        hyperfocus_periods.append(current_period)
                    current_period = None
        
        return {
            'hyperfocus_detected': len(hyperfocus_periods) > 0,
            'periods': hyperfocus_periods,
            'frequency_per_week': len(hyperfocus_periods) / max(1, len(df) / 7)
        }
    
    async def _generate_personalized_recommendations(self, attention_analysis: Dict, routine_analysis: Dict, executive_analysis: Dict) -> List[Dict[str, Any]]:
        """Generate personalized recommendations based on all analysis results."""
        recommendations = []
        
        # Attention-based recommendations
        if attention_analysis.get('attention_variability', {}).get('consistency_rating') == 'highly_variable':
            recommendations.append({
                'type': 'attention_management',
                'priority': 'high',
                'title': 'Implement Variable Attention Strategy',
                'description': 'Your attention varies significantly throughout the day. Consider shorter, more frequent alarms during low-attention periods.',
                'specific_actions': [
                    'Set backup alarms 5-10 minutes after important reminders',
                    'Use more prominent notification styles during identified low-attention hours',
                    'Consider breaking complex tasks into smaller, time-boxed segments'
                ],
                'supporting_evidence': f"Attention variability score: {attention_analysis['attention_variability']['variability_score']:.2f}"
            })
        
        # Optimal timing recommendations
        optimal_hours = attention_analysis.get('optimal_attention_hours', [])
        if optimal_hours:
            best_hour = optimal_hours[0]['hour']
            recommendations.append({
                'type': 'optimal_timing',
                'priority': 'medium',
                'title': f'Schedule Important Tasks Around {best_hour}:00',
                'description': f'Your performance data shows peak attention around {best_hour}:00.',
                'specific_actions': [
                    f'Move non-urgent alarms to {best_hour}:00 when possible',
                    'Schedule complex or important tasks during this time window',
                    'Use this time for medication reminders that require focus'
                ],
                'supporting_evidence': f"Performance score: {optimal_hours[0]['performance_score']:.2f}"
            })
        
        # Routine-based recommendations
        routine_consistency = routine_analysis.get('routine_consistency', {})
        if routine_consistency.get('consistency_score', 0) > 0.8:
            recommendations.append({
                'type': 'routine_optimization',
                'priority': 'medium',
                'title': 'Leverage Your Strong Routine Preferences',
                'description': 'You show strong adherence to routines. This can be leveraged for better alarm effectiveness.',
                'specific_actions': [
                    'Link new alarms to existing strong routine patterns',
                    'Use routine-based triggers rather than just time-based ones',
                    'Maintain consistent alarm timing to support routine stability'
                ],
                'supporting_evidence': f"Routine consistency: {routine_consistency.get('consistency_score', 0):.2f}"
            })
        
        # Executive function recommendations
        planning_effectiveness = executive_analysis.get('planning_effectiveness', {})
        if planning_effectiveness.get('advance_planning_success', 0) < 0.6:
            recommendations.append({
                'type': 'executive_support',
                'priority': 'high',
                'title': 'Implement Planning Support Features',
                'description': 'Data suggests planning challenges. Consider using more structured alarm setups.',
                'specific_actions': [
                    'Use template-based alarm creation for routine tasks',
                    'Set up weekly planning alarms to review upcoming schedules',
                    'Enable step-by-step guidance for complex multi-part tasks'
                ],
                'supporting_evidence': f"Planning success rate: {planning_effectiveness.get('advance_planning_success', 0):.2f}"
            })
        
        # Hyperfocus considerations
        hyperfocus_data = attention_analysis.get('hyperfocus_indicators', {})
        if hyperfocus_data.get('hyperfocus_detected', False):
            recommendations.append({
                'type': 'hyperfocus_management',
                'priority': 'medium',
                'title': 'Manage Hyperfocus Periods',
                'description': 'You show patterns of hyperfocus. This can be both beneficial and risky for alarm response.',
                'specific_actions': [
                    'Set break reminders during identified hyperfocus-prone times',
                    'Use more assertive notification styles during hyperfocus periods',
                    'Consider "hyperfocus mode" that increases alarm persistence'
                ],
                'supporting_evidence': f"Hyperfocus frequency: {hyperfocus_data.get('frequency_per_week', 0):.1f} times per week"
            })
        
        return recommendations
```

## Node.js Service: Third-Party Integrations

The Node.js service handles external integrations and serves as the orchestration layer that coordinates between your Go and Python services. Node.js excels at handling multiple concurrent API calls and has the richest ecosystem for third-party integrations.

### Project Structure for Node.js Service

```typescript
// integration-service/
// ├── src/
// │   ├── app.ts                      # Express application setup
// │   ├── server.ts                   # Server entry point
// │   ├── controllers/                # Request handlers
// │   │   ├── integrations.ts         # Integration management
// │   │   ├── notifications.ts        # Notification orchestration
// │   │   └── calendar.ts             # Calendar integration
// │   ├── services/                   # Business logic
// │   │   ├── fcm-service.ts          # Firebase Cloud Messaging
// │   │   ├── calendar-service.ts     # Calendar API integration
// │   │   ├── orchestration-service.ts # Service coordination
// │   │   └── webhook-service.ts      # Webhook handling
// │   ├── middleware/                 # Express middleware
// │   │   ├── auth.ts                 # Authentication
// │   │   ├── validation.ts           # Request validation
// │   │   └── rate-limiting.ts        # Rate limiting
// │   ├── utils/                      # Utilities
// │   │   ├── http-client.ts          # HTTP client utilities
// │   │   ├── retry.ts                # Retry logic
// │   │   └── circuit-breaker.ts      # Circuit breaker pattern
// │   └── types/                      # TypeScript definitions
// │       ├── integrations.ts         # Integration types
// │       └── notifications.ts        # Notification types
// ├── package.json
// ├── tsconfig.json
// └── Dockerfile

// src/services/orchestration-service.ts
import axios, { AxiosError } from 'axios';
import { CircuitBreaker } from '../utils/circuit-breaker';
import { RetryConfig, withRetry } from '../utils/retry';

/**
 * OrchestrationService coordinates between Go, Python, and external services
 * This service handles the complex logic of calling multiple services and
 * aggregating their responses for neurodivergent user workflows.
 */
export class OrchestrationService {
    private goServiceUrl: string;
    private pythonServiceUrl: string;
    private goCircuitBreaker: CircuitBreaker;
    private pythonCircuitBreaker: CircuitBreaker;

    constructor() {
        this.goServiceUrl = process.env.GO_SERVICE_URL || 'http://alarm-service:8080';
        this.pythonServiceUrl = process.env.PYTHON_SERVICE_URL || 'http://ai-service:8000';
        
        // Circuit breakers prevent cascading failures
        this.goCircuitBreaker = new CircuitBreaker({
            name: 'go-service',
            failureThreshold: 5,
            resetTimeout: 30000, // 30 seconds
            timeout: 5000 // 5 seconds
        });
        
        this.pythonCircuitBreaker = new CircuitBreaker({
            name: 'python-service',
            failureThreshold: 3,
            resetTimeout: 60000, // 60 seconds
            timeout: 30000 // 30 seconds for AI processing
        });
    }

    /**
     * Create an alarm with AI-enhanced suggestions
     * This orchestrates calls to both Go (for creation) and Python (for AI analysis)
     */
    async createAlarmWithAIEnhancement(userId: string, alarmData: AlarmCreationData): Promise<EnhancedAlarmResult> {
        try {
            // Step 1: Get AI recommendations before creating the alarm
            const aiRecommendations = await this.getAIRecommendations(userId, alarmData);
            
            // Step 2: Apply AI recommendations to alarm data
            const enhancedAlarmData = this.applyAIRecommendations(alarmData, aiRecommendations);
            
            // Step 3: Create the alarm using Go service
            const createdAlarm = await this.createAlarmViaGoService(userId, enhancedAlarmData);
            
            // Step 4: Schedule notifications and external integrations
            await Promise.allSettled([
                this.scheduleNotifications(createdAlarm),
                this.updateExternalCalendars(userId, createdAlarm),
                this.logUserInteraction(userId, 'alarm_created_with_ai', createdAlarm.id)
            ]);
            
            return {
                alarm: createdAlarm,
                aiRecommendations,
                appliedEnhancements: this.getAppliedEnhancements(alarmData, enhancedAlarmData),
                nextSuggestions: aiRecommendations.futureRecommendations
            };
            
        } catch (error) {
            console.error('Enhanced alarm creation failed:', error);
            
            // Fallback: create basic alarm without AI enhancement
            try {
                const basicAlarm = await this.createAlarmViaGoService(userId, alarmData);
                return {
                    alarm: basicAlarm,
                    aiRecommendations: null,
                    appliedEnhancements: [],
                    nextSuggestions: [],
                    fallbackMode: true,
                    fallbackReason: error.message
                };
            } catch (fallbackError) {
                throw new Error(`Both enhanced and fallback alarm creation failed: ${fallbackError.message}`);
            }
        }
    }

    /**
     * Get AI recommendations for alarm optimization
     */
    private async getAIRecommendations(userId: string, alarmData: AlarmCreationData): Promise<AIRecommendations | null> {
        try {
            return await this.pythonCircuitBreaker.execute(async () => {
                const response = await withRetry(
                    () => axios.post(`${this.pythonServiceUrl}/api/recommendations/suggest`, {
                        userId,
                        alarmData,
                        context: {
                            timestamp: new Date().toISOString(),
                            timezone: alarmData.timezone
                        }
                    }),
                    { maxRetries: 2, baseDelay: 1000 }
                );
                
                return response.data;
            });
        } catch (error) {
            console.warn(`AI recommendations unavailable for user ${userId}:`, error.message);
            return null;
        }
    }

    /**
     * Create alarm via Go service with proper error handling
     */
    private async createAlarmViaGoService(userId: string, alarmData: AlarmCreationData): Promise<Alarm> {
        return await this.goCircuitBreaker.execute(async () => {
            const response = await withRetry(
                () => axios.post(`${this.goServiceUrl}/api/alarms`, {
                    ...alarmData,
                    userId
                }, {
                    headers: {
                        'Content-Type': 'application/json',
                        'X-User-ID': userId,
                        'X-Request-ID': this.generateRequestId()
                    }
                }),
                { maxRetries: 3, baseDelay: 500 }
            );
            
            return response.data;
        });
    }

    /**
     * Schedule notifications through multiple channels
     */
    private async scheduleNotifications(alarm: Alarm): Promise<void> {
        const notificationTasks = [
            this.scheduleFCMNotification(alarm),
            this.scheduleEmailNotification(alarm),
            this.scheduleWebPushNotification(alarm)
        ];

        // Execute all notification scheduling in parallel
        const results = await Promise.allSettled(notificationTasks);
        
        // Log any failures but don't fail the overall operation
        results.forEach((result, index) => {
            if (result.status === 'rejected') {
                const notificationType = ['FCM', 'Email', 'WebPush'][index];
                console.error(`${notificationType} notification scheduling failed for alarm ${alarm.id}:`, result.reason);
            }
        });
    }

    /**
     * Update external calendar systems
     */
    private async updateExternalCalendars(userId: string, alarm: Alarm): Promise<void> {
        try {
            // Get user's connected calendar accounts
            const calendarConnections = await this.getUserCalendarConnections(userId);
            
            if (calendarConnections.length === 0) {
                return; // No calendars to update
            }

            // Create calendar events in parallel
            const calendarTasks = calendarConnections.map(connection =>
                this.createCalendarEvent(connection, alarm)
            );

            await Promise.allSettled(calendarTasks);
            
        } catch (error) {
            console.error(`Calendar integration failed for alarm ${alarm.id}:`, error);
            // Don't throw - calendar integration is optional
        }
    }

    /**
     * Handle complex multi-service user workflows
     */
    async processNeurodivergentWorkflow(userId: string, workflowType: string, data: any): Promise<WorkflowResult> {
        switch (workflowType) {
            case 'daily_routine_optimization':
                return await this.optimizeDailyRoutine(userId, data);
            
            case 'medication_schedule_analysis':
                return await this.analyzeMedicationSchedule(userId, data);
            
            case 'attention_pattern_adjustment':
                return await this.adjustForAttentionPatterns(userId, data);
            
            default:
                throw new Error(`Unknown workflow type: ${workflowType}`);
        }
    }

    /**
     * Optimize daily routine based on AI analysis and current alarms
     */
    private async optimizeDailyRoutine(userId: string, routineData: any): Promise<WorkflowResult> {
        try {
            // Step 1: Get current alarms from Go service
            const currentAlarms = await this.goCircuitBreaker.execute(() =>
                axios.get(`${this.goServiceUrl}/api/users/${userId}/alarms`)
            );

            // Step 2: Get AI analysis of current routine
            const routineAnalysis = await this.pythonCircuitBreaker.execute(() =>
                axios.post(`${this.pythonServiceUrl}/api/analysis/routine`, {
                    userId,
                    alarms: currentAlarms.data,
                    userPreferences: routineData
                })
            );

            // Step 3: Generate optimization suggestions
            const optimizations = await this.pythonCircuitBreaker.execute(() =>
                axios.post(`${this.pythonServiceUrl}/api/recommendations/routine-optimization`, {
                    userId,
                    currentRoutine: currentAlarms.data,
                    analysis: routineAnalysis.data
                })
            );

            // Step 4: Apply approved optimizations
            const approvedOptimizations = optimizations.data.recommendations.filter(
                (rec: any) => rec.autoApprove || routineData.approvedRecommendations?.includes(rec.id)
            );

            const updatedAlarms = [];
            for (const optimization of approvedOptimizations) {
                try {
                    const updatedAlarm = await this.goCircuitBreaker.execute(() =>
                        axios.put(`${this.goServiceUrl}/api/alarms/${optimization.alarmId}`, optimization.changes)
                    );
                    updatedAlarms.push(updatedAlarm.data);
                } catch (error) {
                    console.error(`Failed to apply optimization ${optimization.id}:`, error);
                }
            }

            return {
                success: true,
                workflowType: 'daily_routine_optimization',
                analysis: routineAnalysis.data,
                appliedOptimizations: approvedOptimizations,
                updatedAlarms,
                pendingRecommendations: optimizations.data.recommendations.filter(
                    (rec: any) => !rec.autoApprove && !routineData.approvedRecommendations?.includes(rec.id)
                )
            };

        } catch (error) {
            return {
                success: false,
                workflowType: 'daily_routine_optimization',
                error: error.message,
                fallbackSuggestions: this.generateFallbackRoutineSuggestions()
            };
        }
    }

    /**
     * Firebase Cloud Messaging for reliable push notifications
     */
    private async scheduleFCMNotification(alarm: Alarm): Promise<void> {
        // Implementation would use Firebase Admin SDK
        // This is a placeholder showing the integration pattern
        try {
            const fcmPayload = {
                notification: {
                    title: alarm.title,
                    body: alarm.description || 'Alarm notification',
                    icon: '/icons/alarm-icon.png'
                },
                data: {
                    alarmId: alarm.id,
                    category: alarm.category,
                    priority: alarm.priority
                },
                android: {
                    priority: alarm.priority === 'critical' ? 'high' : 'normal',
                    notification: {
                        channelId: 'alarms',
                        sound: alarm.accessibility.audioOptions.soundType
                    }
                },
                apns: {
                    payload: {
                        aps: {
                            sound: alarm.accessibility.audioOptions.soundType,
                            badge: 1,
                            'interruption-level': alarm.priority === 'critical' ? 'critical' : 'active'
                        }
                    }
                }
            };

            // Schedule notification for alarm time
            console.log(`FCM notification scheduled for alarm ${alarm.id}`, fcmPayload);
            
        } catch (error) {
            console.error(`FCM scheduling failed for alarm ${alarm.id}:`, error);
            throw error;
        }
    }

    /**
     * Generate unique request ID for tracing
     */
    private generateRequestId(): string {
        return `req_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    }

    /**
     * Apply AI recommendations to alarm data
     */
    private applyAIRecommendations(originalData: AlarmCreationData, recommendations: AIRecommendations | null): AlarmCreationData {
        if (!recommendations) {
            return originalData;
        }

        const enhanced = { ...originalData };

        // Apply timing optimizations
        if (recommendations.optimalTiming) {
            enhanced.datetime = recommendations.optimalTiming;
        }

        // Apply accessibility enhancements
        if (recommendations.accessibilityEnhancements) {
            enhanced.accessibility = {
                ...enhanced.accessibility,
                ...recommendations.accessibilityEnhancements
            };
        }

        // Apply priority adjustments
        if (recommendations.priorityAdjustment) {
            enhanced.priority = recommendations.priorityAdjustment;
        }

        return enhanced;
    }
}

// Type definitions for the orchestration service
interface AlarmCreationData {
    title: string;
    description?: string;
    datetime: string;
    timezone: string;
    category: string;
    priority: string;
    accessibility: {
        visualCues: string[];
        audioOptions: {
            enabled: boolean;
            volume: number;
            soundType: string;
        };
        requireConfirmation: boolean;
        snoozeOptions: number[];
    };
}

interface EnhancedAlarmResult {
    alarm: Alarm;
    aiRecommendations: AIRecommendations | null;
    appliedEnhancements: string[];
    nextSuggestions: any[];
    fallbackMode?: boolean;
    fallbackReason?: string;
}

interface AIRecommendations {
    optimalTiming?: string;
    accessibilityEnhancements?: any;
    priorityAdjustment?: string;
    futureRecommendations: any[];
}

interface WorkflowResult {
    success: boolean;
    workflowType: string;
    analysis?: any;
    appliedOptimizations?: any[];
    updatedAlarms?: any[];
    pendingRecommendations?: any[];
    error?: string;
    fallbackSuggestions?: any[];
}
```

## Service Communication and Orchestration

The three services need to communicate efficiently while maintaining resilience and performance:

```yaml
# docker-compose.yml for local development
version: '3.8'

services:
  # Go service - High-performance CRUD operations
  alarm-service:
    build: 
      context: ./alarm-service
      dockerfile: Dockerfile
    ports:
      - "8080:8080"
    environment:
      - DATABASE_URL=${DATABASE_URL}
      - JWT_SECRET=${JWT_SECRET}
      - ENCRYPTION_KEY=${MASTER_ENCRYPTION_KEY}
    depends_on:
      - database
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Python service - AI processing and analysis  
  ai-service:
    build:
      context: ./ai-service
      dockerfile: Dockerfile
    ports:
      - "8000:8000"
    environment:
      - MODEL_PATH=/app/models
      - PRIVACY_MODE=differential_privacy
      - LOG_LEVEL=INFO
    volumes:
      - ./models:/app/models:ro
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8000/health"]
      interval: 60s
      timeout: 30s
      retries: 2

  # Node.js service - Integrations and orchestration
  integration-service:
    build:
      context: ./integration-service
      dockerfile: Dockerfile
    ports:
      - "3000:3000"
    environment:
      - GO_SERVICE_URL=http://alarm-service:8080
      - PYTHON_SERVICE_URL=http://ai-service:8000
      - FIREBASE_PROJECT_ID=${FIREBASE_PROJECT_ID}
      - FIREBASE_SERVICE_ACCOUNT=${FIREBASE_SERVICE_ACCOUNT}
    depends_on:
      - alarm-service
      - ai-service
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:3000/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Oracle Autonomous Database (simulated with PostgreSQL for local dev)
  database:
    image: postgres:15
    environment:
      - POSTGRES_DB=neurodivergent_alarms
      - POSTGRES_USER=${DB_USER}
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./scripts/init.sql:/docker-entrypoint-initdb.d/init.sql

  # API Gateway for unified endpoint
  api-gateway:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - integration-service
      - alarm-service
      - ai-service

volumes:
  postgres_data:
```

This updated backend architecture properly implements your ADR's multi-language approach, optimizing each service for its specific responsibilities while maintaining the security, reliability, and neurodivergent-focused features that make your application unique. The Go service handles high-frequency alarm operations with minimal latency, Python processes complex AI analysis with rich ML libraries, and Node.js orchestrates integrations with its extensive ecosystem - exactly as specified in your updated architecture decision record.