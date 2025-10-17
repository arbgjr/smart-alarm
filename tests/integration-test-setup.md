# Integration Test Environment Setup

## Prerequisites

To run integration tests that depend on external services, you need the following services running:

### Required Services

- **PostgreSQL** - Database for data persistence
- **Redis** - Caching and session storage
- **MinIO** - Object storage (S3-compatible)
- **RabbitMQ** - Message queue for background processing

### Setup Options

#### Option 1: Docker Compose (Recommended)

Use the existing docker-compose files to start all required services:

```bash
# Start all services
docker-compose -f docker-compose.full.yml up -d

# Or start specific services
docker-compose -f docker-compose.full.yml up -d postgres redis minio rabbitmq
```

#### Option 2: TestContainers (Automatic)

The integration tests can use TestContainers to automatically start and stop containers during test execution. This is configured in the test setup.

#### Option 3: Local Services

Install and run services locally:

- PostgreSQL on port 5432
- Redis on port 6379
- MinIO on port 9000
- RabbitMQ on port 5672

## Test Configuration

### Environment Variables

Set these environment variables for integration tests:

```bash
# Database
POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=smartalarm_test;Username=postgres;Password=postgres"

# Redis
REDIS_CONNECTION_STRING="localhost:6379"

# MinIO
MINIO_ENDPOINT="localhost:9000"
MINIO_ACCESS_KEY="minioadmin"
MINIO_SECRET_KEY="minioadmin"

# RabbitMQ
RABBITMQ_CONNECTION_STRING="amqp://guest:guest@localhost:5672/"
```

### Running Integration Tests

```bash
# Run all integration tests
dotnet test --filter "Category=Integration"

# Run specific integration test project
dotnet test tests/SmartAlarm.Infrastructure.Tests --filter "Category=Integration"

# Run with TestContainers (automatic service startup)
dotnet test tests/SmartAlarm.Infrastructure.Tests --filter "TestContainers"
```

## TestContainers Configuration

The following TestContainers are configured for automatic service management:

### PostgreSQL Container

```csharp
var postgresContainer = new PostgreSqlBuilder()
    .WithDatabase("smartalarm_test")
    .WithUsername("postgres")
    .WithPassword("postgres")
    .WithPortBinding(5432, true)
    .Build();
```

### Redis Container

```csharp
var redisContainer = new RedisBuilder()
    .WithPortBinding(6379, true)
    .Build();
```

### MinIO Container

```csharp
var minioContainer = new MinioBuilder()
    .WithUsername("minioadmin")
    .WithPassword("minioadmin")
    .WithPortBinding(9000, true)
    .Build();
```

### RabbitMQ Container

```csharp
var rabbitmqContainer = new RabbitMqBuilder()
    .WithUsername("guest")
    .WithPassword("guest")
    .WithPortBinding(5672, true)
    .Build();
```

## Troubleshooting

### Common Issues

1. **Port Conflicts**: Ensure ports 5432, 6379, 9000, and 5672 are available
2. **Docker Not Running**: Make sure Docker Desktop is running for TestContainers
3. **Memory Issues**: Ensure sufficient memory is available for containers
4. **Network Issues**: Check firewall settings for container communication

### Test Isolation

Each integration test should:

- Use a unique database name or schema
- Clean up test data after execution
- Use transactions that can be rolled back
- Avoid dependencies between tests

### Performance Considerations

- Use TestContainers for CI/CD pipelines
- Use local services for development
- Consider using in-memory databases for unit tests
- Implement test data seeding strategies

## CI/CD Integration

For continuous integration, ensure:

- Docker is available in the CI environment
- TestContainers can start containers
- Sufficient resources are allocated
- Tests run in parallel safely
