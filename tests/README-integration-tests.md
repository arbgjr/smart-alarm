# Integration Tests Setup Guide

This guide explains how to set up and run integration tests for the Smart Alarm project.

## Quick Start

### Option 1: Automatic Setup with TestContainers (Recommended)

```bash
# Run integration tests with automatic container management
dotnet test --filter "Category=Integration"
```

### Option 2: Manual Service Setup

```bash
# Start services manually
.\tests\scripts\setup-integration-tests.ps1

# Run integration tests
dotnet test --filter "Category=Integration"

# Stop services when done
.\tests\scripts\cleanup-integration-tests.ps1
```

## Prerequisites

### For TestContainers (Automatic)

- Docker Desktop installed and running
- .NET 8 SDK
- Sufficient memory (4GB+ recommended)

### For Manual Setup

- Docker Desktop or individual services installed
- PostgreSQL, Redis, MinIO, RabbitMQ running

## Test Categories

### Unit Tests

```bash
# Fast tests that don't require external services
dotnet test --filter "Category!=Integration&Category!=TestContainers"
```

### Integration Tests

```bash
# Tests that require external services (manual setup)
dotnet test --filter "Category=Integration"
```

### TestContainer Tests

```bash
# Tests with automatic service management
dotnet test --filter "Category=TestContainers"
```

## Service Configuration

### Required Services and Ports

- **PostgreSQL**: 5432 (Database)
- **Redis**: 6379 (Caching)
- **MinIO**: 9000 (Object Storage)
- **RabbitMQ**: 5672 (Message Queue)

### Environment Variables

```bash
# Set these for manual service setup
export POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=smartalarm_test;Username=postgres;Password=postgres"
export REDIS_CONNECTION_STRING="localhost:6379"
export MINIO_ENDPOINT="localhost:9000"
export RABBITMQ_CONNECTION_STRING="amqp://guest:guest@localhost:5672/"
```

## Troubleshooting

### Common Issues

1. **Docker not running**

   ```bash
   # Check Docker status
   docker --version
   docker ps
   ```

2. **Port conflicts**

   ```bash
   # Check what's using the ports
   netstat -an | findstr "5432 6379 9000 5672"
   ```

3. **Memory issues**

   - Ensure Docker has at least 4GB memory allocated
   - Close other applications if needed

4. **Test failures**

   ```bash
   # Check service status
   .\tests\scripts\setup-integration-tests.ps1 -CheckStatus

   # View Docker logs
   docker-compose -f docker-compose.full.yml logs
   ```

### Performance Tips

- Use TestContainers for CI/CD pipelines
- Use manual setup for development (faster startup)
- Run tests in parallel when possible
- Use test data seeding for consistent test environments

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Integration Tests
on: [push, pull_request]

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: "8.0.x"

      - name: Run Integration Tests
        run: dotnet test --filter "Category=TestContainers" --logger trx --results-directory TestResults

      - name: Upload Test Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: test-results
          path: TestResults
```

### Azure DevOps Example

```yaml
- task: DotNetCoreCLI@2
  displayName: "Run Integration Tests"
  inputs:
    command: "test"
    arguments: '--filter "Category=TestContainers" --logger trx --results-directory $(Agent.TempDirectory)'
    publishTestResults: true
```

## Best Practices

### Test Isolation

- Each test should be independent
- Use transactions that can be rolled back
- Clean up test data after each test
- Use unique identifiers for test data

### Performance

- Minimize database operations
- Use in-memory databases for unit tests
- Batch operations when possible
- Consider test parallelization

### Maintainability

- Use base classes for common setup
- Create helper methods for test data
- Document test scenarios clearly
- Keep tests focused and simple

## Examples

See the following files for examples:

- `tests/TestContainers/IntegrationTestBase.cs` - Base class for TestContainer tests
- `tests/integration-test-setup.md` - Detailed setup instructions
- `tests/scripts/setup-integration-tests.ps1` - Setup automation script
