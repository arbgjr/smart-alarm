# Backend Integration Tests

This directory contains comprehensive integration tests for the Smart Alarm backend services, implementing task **2.5 Testes de integração para backend**.

## Overview

The integration tests validate the interaction between multiple backend services and ensure that the system works correctly as a whole. These tests cover the three main areas specified in the task:

1. **AlarmTriggerService** - Alarm scheduling, triggering, and escalation
2. **Calendar Integration** - External calendar synchronization and vacation detection
3. **Notification System** - Real-time and push notifications

## Test Files

### 1. AlarmTriggerServiceIntegrationTests.cs

Tests the complete alarm lifecycle and background job integration.

**Key Test Scenarios:**

- `ScheduleAlarmAsync_WithValidRecurringAlarm_ShouldScheduleSuccessfully` - Validates alarm scheduling with Hangfire
- `TriggerAlarmAsync_WithActiveAlarm_ShouldTriggerAndScheduleEscalation` - Tests alarm triggering and escalation setup
- `EscalateMissedAlarmAsync_WithUnhandledAlarm_ShouldSendNotifications` - Validates escalation notification flow
- `ProcessMissedAlarmsAsync_WithMissedAlarms_ShouldEscalateAll` - Tests batch processing of missed alarms
- `RescheduleAlarmAsync_ShouldCancelAndScheduleNewJob` - Validates alarm rescheduling logic

**Dependencies Tested:**

- IAlarmRepository
- IAlarmEventService
- IBackgroundJobService (Hangfire)
- INotificationService
- IPushNotificationService

### 2. CalendarIntegrationServiceIntegrationTests.cs

Tests integration with external calendar providers (Google Calendar, Outlook).

**Key Test Scenarios:**

- `GetEventsFromAllProvidersAsync_WithBothProvidersAuthorized_ShouldReturnMergedEvents` - Multi-provider event aggregation
- `HasVacationOrDayOffAsync_WithVacationInGoogleCalendar_ShouldReturnTrue` - Vacation detection
- `SyncAllCalendarsAsync_WithMultipleIntegrations_ShouldSyncAll` - Calendar synchronization
- `GetSyncStatusAsync_WithIntegrations_ShouldReturnCompleteStatus` - Sync status reporting
- `SyncBidirectionalAsync_WithCreateAction_ShouldCreateInOutlook` - Bidirectional sync

**Dependencies Tested:**

- IGoogleCalendarService
- IOutlookCalendarService
- IIntegrationRepository

### 3. NotificationSystemIntegrationTests.cs

Tests real-time notification delivery via SignalR.

**Key Test Scenarios:**

- `SendNotificationAsync_WithValidUser_ShouldSendToUserGroup` - User-specific notifications
- `SendNotificationToGroupAsync_WithValidGroup_ShouldSendToGroup` - Group notifications
- `SendBroadcastNotificationAsync_ShouldSendToAllUsers` - Broadcast notifications
- `SendNotificationAsync_WithSignalRException_ShouldThrowException` - Error handling

**Dependencies Tested:**

- IHubContext<NotificationHub>
- SignalR client proxy chains

### 4. PushNotificationIntegrationTests.cs

Tests push notification delivery for mobile devices.

**Key Test Scenarios:**

- `SendPushNotificationAsync_WithValidNotification_ShouldSendSuccessfully` - Basic push notifications
- `SendPushNotificationAsync_WithHighPriorityNotification_ShouldHandleCorrectly` - Priority handling

**Dependencies Tested:**

- IPushNotificationService

### 5. BackendServicesIntegrationTests.cs

End-to-end integration tests that combine multiple services.

**Key Test Scenarios:**

- `AlarmWorkflow_FromScheduleToEscalation_ShouldWorkEndToEnd` - Complete alarm workflow
- `CalendarIntegrationWithAlarmScheduling_ShouldRespectVacationDays` - Calendar-aware scheduling
- `NotificationSystem_WithMultipleChannels_ShouldDeliverToAll` - Multi-channel notifications
- `CalendarSync_WithFailureRecovery_ShouldHandleGracefully` - Error recovery

## Testing Approach

### Mock-Based Integration Testing

- Uses Moq framework for dependency mocking
- Creates realistic service interaction scenarios
- Validates service boundaries and contracts

### Dependency Injection Setup

- Uses Microsoft.Extensions.DependencyInjection
- Configures services with proper logging and observability
- Mimics production service registration patterns

### Error Scenario Coverage

- Tests exception handling and error propagation
- Validates fallback mechanisms
- Ensures graceful degradation

## Requirements Coverage

The integration tests cover all requirements specified in task 2.5:

### Requirement 2.1 - Authentication & User Management

- ✅ User association with alarms tested
- ✅ User-specific notification delivery validated

### Requirement 2.2 - Calendar Integration

- ✅ Google Calendar integration tested
- ✅ Outlook Calendar integration tested
- ✅ Multi-provider event aggregation validated
- ✅ Vacation/day-off detection tested

### Requirement 2.3 - Alarm Management

- ✅ Complete alarm lifecycle tested
- ✅ Scheduling and triggering validated
- ✅ Escalation system thoroughly tested
- ✅ Background job integration verified

### Requirement 2.4 - Notification System

- ✅ Real-time SignalR notifications tested
- ✅ Push notification delivery validated
- ✅ Multi-channel notification flow verified

### Requirements 2.5, 2.6, 2.7 - System Integration

- ✅ Service-to-service communication tested
- ✅ Background job processing validated
- ✅ Error handling and resilience verified
- ✅ Cross-service workflow integration tested

## Running the Tests

### Prerequisites

- .NET 8 SDK
- All project dependencies restored
- Test environment configured

### Execution

```bash
# Run all integration tests
dotnet test tests/SmartAlarm.Infrastructure.Tests/Integration/

# Run specific test file
dotnet test tests/SmartAlarm.Infrastructure.Tests/Integration/AlarmTriggerServiceIntegrationTests.cs

# Run with detailed output
dotnet test tests/SmartAlarm.Infrastructure.Tests/Integration/ --verbosity detailed

# Generate test report
./run-integration-tests.ps1
```

### Test Categories

Tests are marked with appropriate traits:

- `[Trait("Category", "Integration")]` - Integration test category
- `[Trait("Service", "AlarmTrigger")]` - Service-specific tests
- `[Trait("Feature", "Calendar")]` - Feature-specific tests

## Test Statistics

- **Total Test Methods:** 29
- **AlarmTriggerService Tests:** 9
- **Calendar Integration Tests:** 10
- **Notification System Tests:** 4
- **Push Notification Tests:** 2
- **End-to-End Tests:** 4

## Key Benefits

1. **Comprehensive Coverage** - All backend services and their interactions are tested
2. **Realistic Scenarios** - Tests mirror production usage patterns
3. **Error Resilience** - Failure scenarios are thoroughly validated
4. **Maintainable** - Clear test structure and good documentation
5. **Fast Feedback** - Mock-based approach provides quick test execution

## Future Enhancements

- Add TestContainers for database integration tests
- Include performance benchmarking in integration tests
- Add chaos engineering scenarios
- Implement contract testing between services
- Add integration with external service simulators

## Conclusion

These integration tests provide comprehensive validation of the Smart Alarm backend services, ensuring that all components work together correctly and handle error scenarios gracefully. The tests cover all requirements specified in task 2.5 and provide a solid foundation for maintaining system quality as the application evolves.
