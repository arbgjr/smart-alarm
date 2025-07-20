---
title: KeyVault Service - Technical Documentation
component_path: src/SmartAlarm.KeyVault/Services/KeyVaultService.cs
version: 1.0
date_created: 2025-07-19
last_updated: 2025-07-19
owner: Smart Alarm Development Team
tags: [infrastructure, security, multi-cloud, secrets-management, service]
---

# KeyVault Service Documentation

The `KeyVaultService` is a multi-cloud secrets management service that provides unified access to various secret providers (Azure KeyVault, OCI Vault, HashiCorp Vault, AWS Secrets Manager, Google Secret Manager). It implements caching, resilience patterns, and provider fallback mechanisms for robust secret retrieval in distributed environments.

## 1. Component Overview

### Purpose/Responsibility
- OVR-001: **Primary Responsibility** - Provide unified, resilient access to secrets across multiple cloud providers with caching, retry logic, and failover capabilities
- OVR-002: **Scope** - Handles secret retrieval, caching, provider management, and resilience patterns. Does NOT handle secret creation, rotation policies, or direct provider configuration
- OVR-003: **System Context** - Infrastructure service consumed by application services, repositories, and external integrations requiring secure credential access

## 2. Architecture Section

- ARC-001: **Design Patterns Used**
  - **Strategy Pattern**: Multiple secret providers implementing `ISecretProvider` interface
  - **Facade Pattern**: Unified interface hiding complexity of multiple providers
  - **Cache-Aside Pattern**: Local caching with expiration and concurrent access control
  - **Circuit Breaker Pattern**: Resilience pipeline with retry, timeout, and fallback policies
  - **Factory Pattern**: Provider selection and instantiation based on configuration
  
- ARC-002: **Dependencies**
  - **Internal**: `ISecretProvider` implementations (Azure, OCI, HashiCorp, AWS, Google)
  - **Internal**: `KeyVaultOptions` configuration model
  - **External**: Microsoft.Extensions.Logging - Structured logging
  - **External**: Microsoft.Extensions.Options - Configuration binding
  - **External**: Polly - Resilience and retry policies
  - **External**: System.Collections.Concurrent - Thread-safe caching

- ARC-003: **Component Interactions**
  - **Provider Ecosystem**: Manages multiple `ISecretProvider` implementations
  - **Application Services**: Consumed by authentication, external integrations, and repositories
  - **Configuration System**: Uses IOptions pattern for settings management
  - **Observability**: Integrates with logging and potentially metrics/tracing

- ARC-004: **Resilience Architecture**
  - Configurable retry policies with exponential backoff
  - Provider fallback mechanism for high availability
  - Circuit breaker pattern to prevent cascade failures
  - Concurrent cache access control with SemaphoreSlim

### Component Structure and Dependencies Diagram

```mermaid
graph TD
    subgraph "KeyVault Service Architecture"
        A[KeyVaultService] --> B[ConcurrentDictionary Cache]
        A --> C[ResiliencePipeline]
        A --> D[SemaphoreSlim]
        A --> E[ISecretProvider Collection]
    end

    subgraph "Secret Providers"
        F[AzureKeyVaultProvider] --> E
        G[OciVaultProvider] --> E
        H[HashiCorpVaultProvider] --> E
        I[AwsSecretsProvider] --> E
        J[GoogleSecretProvider] --> E
    end

    subgraph "External Dependencies"
        K[Microsoft.Extensions.Logging]
        L[Microsoft.Extensions.Options]
        M[Polly ResiliencePipeline]
        N[System.Collections.Concurrent]
    end

    subgraph "Consuming Services"
        O[Authentication Service]
        P[External Integrations]
        Q[Repository Services]
        R[Background Jobs]
    end

    A --> K
    A --> L
    A --> M
    A --> N
    
    O --> A
    P --> A
    Q --> A
    R --> A

    classDiagram
        class KeyVaultService {
            -IEnumerable~ISecretProvider~ _providers
            -KeyVaultOptions _options
            -ILogger _logger
            -ConcurrentDictionary _cache
            -SemaphoreSlim _cacheSemaphore
            -ResiliencePipeline _resiliencePipeline
            +GetSecretAsync(string, CancellationToken): Task~string?~
            +SetSecretAsync(string, string, CancellationToken): Task~bool~
            +DeleteSecretAsync(string, CancellationToken): Task~bool~
            +InvalidateCache(string): void
            +ClearCache(): void
            -CreateResiliencePipeline(): ResiliencePipeline
            -IsCacheValid(DateTime): bool
            -TryGetFromCache(string): (string?, DateTime)
            -SetCache(string, string): void
        }
        
        class ISecretProvider {
            <<interface>>
            +Name: string
            +Priority: int
            +IsAvailable(): Task~bool~
            +GetSecretAsync(string, CancellationToken): Task~string?~
            +SetSecretAsync(string, string, CancellationToken): Task~bool~
            +DeleteSecretAsync(string, CancellationToken): Task~bool~
        }
        
        class KeyVaultOptions {
            +bool Enabled
            +TimeSpan CacheExpiration
            +int MaxRetryAttempts
            +TimeSpan RetryDelay
            +bool EnableFallback
            +string[] PreferredProviders
        }
        
        class ResiliencePipeline {
            <<external>>
            +ExecuteAsync~T~(Func~T~): Task~T~
        }

        KeyVaultService --> ISecretProvider
        KeyVaultService --> KeyVaultOptions
        KeyVaultService --> ResiliencePipeline
```

## 3. Interface Documentation

### Public Methods

| Method | Purpose | Parameters | Return Type | Usage Notes |
|--------|---------|------------|-------------|-------------|
| GetSecretAsync() | Retrieve secret value | secretKey: string, cancellationToken: CancellationToken | Task&lt;string?&gt; | Returns null if not found, uses cache and resilience |
| SetSecretAsync() | Store secret value | secretKey: string, secretValue: string, cancellationToken: CancellationToken | Task&lt;bool&gt; | Returns success status, tries all providers |
| DeleteSecretAsync() | Remove secret | secretKey: string, cancellationToken: CancellationToken | Task&lt;bool&gt; | Returns success status, removes from cache |
| InvalidateCache() | Remove specific cache entry | secretKey: string | void | Thread-safe cache invalidation |
| ClearCache() | Remove all cache entries | None | void | Clears entire cache, thread-safe |

### Configuration Properties (KeyVaultOptions)

| Property | Purpose | Default Value | Type | Usage Notes |
|----------|---------|---------------|------|-------------|
| Enabled | Service activation flag | true | bool | Disables all operations if false |
| CacheExpiration | Cache entry TTL | 15 minutes | TimeSpan | Balances performance vs freshness |
| MaxRetryAttempts | Retry policy limit | 3 | int | Exponential backoff attempts |
| RetryDelay | Base retry delay | 1 second | TimeSpan | Initial delay before exponential backoff |
| EnableFallback | Provider fallback flag | true | bool | Enables trying multiple providers |
| PreferredProviders | Provider priority order | [] | string[] | Ordered list of provider names |

## 4. Implementation Details

- IMP-001: **Main Implementation Classes**
  - `KeyVaultService`: Primary orchestrator with caching and resilience
  - `ConcurrentDictionary<string, (string, DateTime)>`: Thread-safe cache with expiration
  - `SemaphoreSlim`: Concurrent access control for cache operations
  - `ResiliencePipeline`: Polly-based retry and timeout policies

- IMP-002: **Configuration and Initialization**
  - Constructor injection of providers, options, and logger
  - Automatic resilience pipeline creation with configurable policies
  - Service registration through DependencyInjection extension methods
  - Options pattern integration with IConfiguration

- IMP-003: **Key Algorithms and Business Logic**
  - **Cache-First Strategy**: Check cache before provider calls
  - **Provider Selection**: Priority-based provider ordering with fallback
  - **Expiration Management**: Time-based cache invalidation
  - **Resilience Patterns**: Retry with exponential backoff, timeout, circuit breaker
  - **Concurrent Access Control**: SemaphoreSlim prevents cache corruption

- IMP-004: **Performance Characteristics**
  - **Cache Hit Performance**: Sub-millisecond response for cached secrets
  - **Cache Miss Performance**: Network-dependent, typically 100-500ms per provider
  - **Memory Usage**: Linear growth with number of cached secrets
  - **Concurrency**: Thread-safe operations with controlled concurrent access

## 5. Usage Examples

### Basic Usage

```csharp
// Dependency injection registration
services.AddKeyVault(configuration);

// Service usage
public class SomeService
{
    private readonly IKeyVaultService _keyVault;
    
    public SomeService(IKeyVaultService keyVault)
    {
        _keyVault = keyVault;
    }
    
    public async Task<string> GetDatabaseConnectionString()
    {
        return await _keyVault.GetSecretAsync("database-connection-string");
    }
}
```

### Advanced Usage

```csharp
// Configuration in appsettings.json
{
  "KeyVault": {
    "Enabled": true,
    "CacheExpiration": "00:15:00",
    "MaxRetryAttempts": 3,
    "RetryDelay": "00:00:01",
    "EnableFallback": true,
    "PreferredProviders": ["AzureKeyVault", "OciVault", "HashiCorpVault"]
  }
}

// Advanced service usage with error handling
public class AuthenticationService
{
    private readonly IKeyVaultService _keyVault;
    private readonly ILogger<AuthenticationService> _logger;
    
    public async Task<string?> GetJwtSecretAsync()
    {
        try
        {
            var secret = await _keyVault.GetSecretAsync("jwt-signing-key");
            if (secret == null)
            {
                _logger.LogWarning("JWT signing key not found in any provider");
                return null;
            }
            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve JWT signing key");
            throw;
        }
    }
    
    public async Task RotateApiKeyAsync(string newKey)
    {
        var success = await _keyVault.SetSecretAsync("api-key", newKey);
        if (success)
        {
            _keyVault.InvalidateCache("api-key"); // Ensure fresh read
            _logger.LogInformation("API key rotated successfully");
        }
    }
}
```

- USE-001: **Basic Usage**: Simple secret retrieval with dependency injection
- USE-002: **Advanced Configuration**: Multiple providers with custom settings
- USE-003: **Best Practices**: Error handling, cache management, structured logging

## 6. Quality Attributes

- QUA-001: **Security**
  - **Secret Protection**: No logging of secret values, memory-safe operations
  - **Provider Isolation**: Each provider maintains separate authentication
  - **Audit Trail**: Comprehensive logging of access attempts (without values)
  - **Encryption**: Relies on provider-native encryption for secret storage

- QUA-002: **Performance**
  - **Cache Performance**: Sub-millisecond response for cached secrets
  - **Network Optimization**: Concurrent provider queries with prioritization
  - **Memory Management**: Bounded cache with configurable expiration
  - **Scalability**: Horizontal scaling through stateless design

- QUA-003: **Reliability**
  - **High Availability**: Multi-provider fallback mechanism
  - **Fault Tolerance**: Circuit breaker pattern prevents cascade failures
  - **Retry Logic**: Exponential backoff with configurable limits
  - **Graceful Degradation**: Service continues with available providers

- QUA-004: **Maintainability**
  - **Provider Abstraction**: Clean interface for adding new secret providers
  - **Configuration-Driven**: Behavior modification without code changes
  - **Comprehensive Logging**: Detailed operational insights for troubleshooting
  - **Unit Testing**: Mockable interfaces and dependency injection

- QUA-005: **Extensibility**
  - **New Providers**: Implement `ISecretProvider` interface for new cloud providers
  - **Custom Policies**: Replace or extend resilience pipeline configuration
  - **Caching Strategies**: Configurable cache policies and invalidation strategies
  - **Observability**: Integration points for metrics, tracing, and monitoring

## 7. Reference Information

- REF-001: **Dependencies and Versions**
  - .NET 8.0+ framework
  - Microsoft.Extensions.Logging.Abstractions 8.0+
  - Microsoft.Extensions.Options 8.0+
  - Polly 8.0+ (resilience patterns)
  - Provider-specific SDKs (Azure, OCI, AWS, Google, HashiCorp)

- REF-002: **Configuration Reference**
  ```json
  {
    "KeyVault": {
      "Enabled": true,
      "CacheExpiration": "00:15:00",
      "MaxRetryAttempts": 3,
      "RetryDelay": "00:00:01",
      "EnableFallback": true,
      "PreferredProviders": ["AzureKeyVault", "OciVault"]
    }
  }
  ```

- REF-003: **Testing Guidelines**
  ```csharp
  // Unit test with mocked providers
  [Fact]
  public async Task GetSecretAsync_WithValidKey_ReturnsSecret()
  {
      // Arrange
      var mockProvider = new Mock<ISecretProvider>();
      mockProvider.Setup(p => p.GetSecretAsync("test-key", default))
                  .ReturnsAsync("test-value");
      
      var service = new KeyVaultService([mockProvider.Object], options, logger);
      
      // Act
      var result = await service.GetSecretAsync("test-key");
      
      // Assert
      Assert.Equal("test-value", result);
  }
  ```

- REF-004: **Common Issues and Troubleshooting**
  - **Issue**: "No providers available" error
    - **Solution**: Verify provider registration and configuration
  - **Issue**: Slow secret retrieval
    - **Solution**: Check network connectivity, adjust cache expiration
  - **Issue**: Authentication failures
    - **Solution**: Validate provider credentials and permissions
  - **Issue**: Memory consumption growth
    - **Solution**: Monitor cache size, adjust expiration settings

- REF-005: **Related Documentation**
  - [Multi-Cloud Architecture](../architecture/multi-cloud-strategy.md)
  - [Security Best Practices](../security/secrets-management.md)
  - [Provider Configuration Guide](../infrastructure/keyvault-providers.md)
  - [Observability Integration](../observability/logging-tracing.md)

- REF-006: **Change History and Migration Notes**
  - **v1.0**: Initial implementation with Azure, OCI, HashiCorp providers
  - **Future**: AWS and Google providers planned
  - **Migration**: No breaking changes expected, configuration additive
