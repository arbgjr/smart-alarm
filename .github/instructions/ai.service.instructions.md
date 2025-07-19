---
applyTo: "services/ai-service/**"
---
# AI Service Instructions

## 1. Core Responsibility

The **AI Service** provides machine learning-driven behavioral analysis and intelligent scheduling recommendations. It analyzes alarm patterns, user routines, and external data to optimize alarm effectiveness and suggest personalized scheduling strategies.

## 2. Key Technologies

- **Framework**: .NET 8, ASP.NET Core
- **Machine Learning**: **ML.NET** for model training, inference, and behavioral analysis
- **Architecture**: Clean Architecture with Domain, Application, Infrastructure, and API layers
- **Observability**: OpenTelemetry tracing, Serilog structured logging, Prometheus metrics
- **Communication**: MediatR for internal command/query handling, RabbitMQ for inter-service messaging
- **Persistence**: Entity Framework Core with PostgreSQL (dev) and Oracle (prod)

## 3. Architectural Patterns

- **Clean Architecture**: Strict adherence to layer separation - Domain contains ML models and business logic, Application orchestrates workflows, Infrastructure handles ML.NET implementations
- **CQRS with MediatR**: Commands for training/updating models, Queries for predictions and analysis
- **Domain Services**: `IAlarmAnalysisService` for complex behavioral analysis logic
- **Repository Pattern**: `IPredictionRepository` for storing model metadata and historical predictions
- **Event-Driven**: Publishes `AlarmPatternAnalyzedEvent` and `ModelUpdatedEvent` to the message bus

## 4. Code Generation Rules

- **Model Schema**: Define input/output classes in the Domain layer (e.g., `AlarmPatternInput`, `BehavioralAnalysisOutput`)
- **ML Services**: Implement ML.NET logic in Infrastructure layer services (e.g., `AlarmPatternAnalysisService`)
- **Command Handlers**: Use Application layer handlers for training requests (e.g., `TrainBehavioralModelCommandHandler`)
- **Query Handlers**: Use Application layer handlers for predictions (e.g., `GetScheduleRecommendationQueryHandler`)
- **Dependency Registration**: Register ML.NET services with proper lifetime management in `Program.cs`
- **Observability**: Wrap all ML operations with activities, structured logging, and custom metrics
- **Validation**: Use FluentValidation for all input models and prediction requests
- **Error Handling**: Use custom exceptions for ML-specific errors (e.g., `ModelNotTrainedException`)

## 5. ML.NET Integration Patterns

```csharp
// Domain Layer - Model Definition
public class AlarmPatternInput
{
    public float HourOfDay { get; set; }
    public float DayOfWeek { get; set; }
    public float UserResponseTime { get; set; }
    public float SnoozeCount { get; set; }
}

// Application Layer - Command Handler
public class AnalyzeAlarmPatternsCommandHandler : IRequestHandler<AnalyzeAlarmPatternsCommand>
{
    private readonly IAlarmPatternAnalysisService _analysisService;
    private readonly IMessagingService _messagingService;
    
    public async Task Handle(AnalyzeAlarmPatternsCommand request, CancellationToken cancellationToken)
    {
        var analysis = await _analysisService.AnalyzePatternsAsync(request.UserId, request.TimeRange);
        await _messagingService.PublishAsync(new AlarmPatternAnalyzedEvent(analysis));
    }
}

// Infrastructure Layer - ML Service Registration
services.AddScoped<IAlarmPatternAnalysisService, AlarmPatternAnalysisService>();
services.AddPredictionEnginePool<AlarmPatternInput, AlarmPatternOutput>()
    .FromFile(modelPath => configuration["MLModels:AlarmPatternModelPath"]);
```

## 6. Quality Standards

- **Testing**: Unit tests for all ML logic, integration tests for model loading and prediction accuracy
- **Model Versioning**: Track model versions and performance metrics in the database
- **Performance**: Implement model caching and batch prediction capabilities for efficiency
- **Security**: Never log user data in ML operations, sanitize all inputs
- **Documentation**: Document model features, training procedures, and prediction interpretation
