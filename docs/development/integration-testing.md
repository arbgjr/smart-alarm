# Padrões de Testes de Integração

Este documento descreve os padrões de implementação de testes de integração no projeto Smart Alarm, seguindo os princípios de Clean Architecture, SOLID, e as práticas recomendadas definidas no Memory Bank.

## Princípios Gerais

- Testes de integração devem validar a interação entre componentes do sistema ou com serviços externos
- Todos os testes devem ser categorizados adequadamente (usando o atributo `[Category]`)
- Cobertura mínima de 80% para código crítico
- Cada teste deve seguir o padrão AAA (Arrange, Act, Assert)
- Recursos externos devem ser isolados ou mockados quando apropriado
- Testes devem ser independentes e não devem depender de outros testes

## Organização dos Testes

Os testes de integração estão organizados nas seguintes pastas:

- `tests/SmartAlarm.Infrastructure.Tests/Integration/`: Testes de integração com serviços externos
- `tests/SmartAlarm.KeyVault.Tests/Integration/`: Testes de integração com KeyVault
- `tests/SmartAlarm.Api.Tests/Integration/`: Testes de integração de API
- `tests/integration/`: Testes de integração adicionais

## Categorização

Todos os testes de integração devem ser categorizados adequadamente:

```csharp
[Fact]
[Category("Integration")]
[Category("RabbitMQ")]
public async Task Should_PublishMessage_When_ValidInput()
{
    // Arrange
    
    // Act
    
    // Assert
}
```

Categorias comuns:
- `Integration`: Todos os testes de integração
- `RabbitMQ`: Testes específicos de integração com RabbitMQ
- `PostgreSQL`: Testes específicos de integração com PostgreSQL
- `MinIO`: Testes específicos de integração com MinIO
- `HashiCorpVault`: Testes específicos de integração com HashiCorp Vault
- `KeyVault`: Testes específicos de integração com KeyVault
- `Observability`: Testes específicos de integração com observabilidade

## Padrões por Tipo de Serviço

### 1. RabbitMQ

```csharp
[Fact]
[Category("Integration")]
[Category("RabbitMQ")]
public async Task Should_ConsumeMessage_When_Published()
{
    // Arrange
    var connectionFactory = new ConnectionFactory { HostName = "localhost", Port = 5672 };
    var message = new AlarmCreatedEvent { Id = Guid.NewGuid(), Name = "Test Alarm" };
    var messagingService = new RabbitMqMessagingService(connectionFactory, _logger);
    var receivedMessage = null;
    
    // Act
    await messagingService.PublishAsync("test-exchange", "test-routing-key", message);
    
    // Configurar consumidor para testar recebimento
    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.Received += (model, ea) => {
        var body = ea.Body.ToArray();
        var receivedMessage = Encoding.UTF8.GetString(body);
        // Processamento do receivedMessage
        return Task.CompletedTask;
    };
    
    // Aguardar processamento (com timeout)
    await Task.Delay(1000);
    
    // Assert
    Assert.NotNull(receivedMessage);
    Assert.Contains(message.Id.ToString(), receivedMessage);
}
```

### 2. PostgreSQL

```csharp
[Fact]
[Category("Integration")]
[Category("PostgreSQL")]
public async Task Should_SaveAndRetrieveEntity_When_UsingRepository()
{
    // Arrange
    var options = new DbContextOptionsBuilder<SmartAlarmDbContext>()
        .UseNpgsql("Host=localhost;Database=smartalarm;Username=smartalarm;Password=smartalarm123")
        .Options;
    
    using var context = new SmartAlarmDbContext(options);
    var repository = new AlarmRepository(context);
    var alarm = new Alarm("Test Alarm", DateTime.Now.AddDays(1));
    
    // Act
    await repository.AddAsync(alarm);
    await context.SaveChangesAsync();
    
    var retrievedAlarm = await repository.GetByIdAsync(alarm.Id);
    
    // Assert
    Assert.NotNull(retrievedAlarm);
    Assert.Equal(alarm.Name, retrievedAlarm.Name);
}
```

### 3. MinIO

```csharp
[Fact]
[Category("Integration")]
[Category("MinIO")]
public async Task Should_UploadAndDownloadFile_When_UsingStorageService()
{
    // Arrange
    var minioClient = new MinioClient()
        .WithEndpoint("localhost", 9000)
        .WithCredentials("minio", "minio123")
        .WithSSL(false)
        .Build();
    
    var storageService = new MinioStorageService(minioClient, _logger);
    var bucketName = "test-bucket";
    var fileName = "test-file.txt";
    var content = "Test content";
    
    // Create bucket if not exists
    var bucketExists = await minioClient.BucketExistsAsync(bucketName);
    if (!bucketExists)
    {
        await minioClient.MakeBucketAsync(bucketName);
    }
    
    // Act
    await storageService.UploadAsync(bucketName, fileName, 
        new MemoryStream(Encoding.UTF8.GetBytes(content)));
    
    var downloadedContent = await storageService.DownloadAsync(bucketName, fileName);
    
    // Assert
    Assert.NotNull(downloadedContent);
    using var reader = new StreamReader(downloadedContent);
    var downloadedText = await reader.ReadToEndAsync();
    Assert.Equal(content, downloadedText);
    
    // Cleanup
    await storageService.DeleteAsync(bucketName, fileName);
}
```

### 4. HashiCorp Vault

```csharp
[Fact]
[Category("Integration")]
[Category("HashiCorpVault")]
public async Task Should_StoreAndRetrieveSecret_When_UsingVaultProvider()
{
    // Arrange
    var vaultSettings = new HashiCorpVaultSettings
    {
        ServerAddress = "http://localhost:8200",
        Token = "dev-token",
        MountPath = "secret",
        KvVersion = 2,
        SkipTlsVerification = true
    };
    
    var provider = new HashiCorpVaultProvider(vaultSettings, _logger);
    var secretPath = "test/integration";
    var secretKey = "testKey";
    var secretValue = "testValue";
    
    // Act
    await provider.StoreSecretAsync(secretPath, new Dictionary<string, string> { 
        { secretKey, secretValue } 
    });
    
    var retrievedSecret = await provider.GetSecretAsync(secretPath);
    
    // Assert
    Assert.NotNull(retrievedSecret);
    Assert.True(retrievedSecret.ContainsKey(secretKey));
    Assert.Equal(secretValue, retrievedSecret[secretKey]);
    
    // Cleanup
    await provider.DeleteSecretAsync(secretPath);
}
```

### 5. KeyVault (Abstração Multi-Provider)

```csharp
[Fact]
[Category("Integration")]
[Category("KeyVault")]
public async Task Should_UseCorrectProvider_When_ConfiguredForHashiCorpVault()
{
    // Arrange
    var configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string>
        {
            ["KeyVault:Provider"] = "HashiCorpVault",
            ["KeyVault:HashiCorpVault:ServerAddress"] = "http://localhost:8200",
            ["KeyVault:HashiCorpVault:Token"] = "dev-token",
            ["KeyVault:HashiCorpVault:MountPath"] = "secret",
            ["KeyVault:HashiCorpVault:KvVersion"] = "2",
            ["KeyVault:HashiCorpVault:SkipTlsVerification"] = "true"
        })
        .Build();
    
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddKeyVault(configuration);
    
    var serviceProvider = services.BuildServiceProvider();
    var keyVault = serviceProvider.GetRequiredService<IKeyVaultService>();
    
    var secretPath = "test/keyvault";
    var secretKey = "testKey";
    var secretValue = "testValue";
    
    // Act
    await keyVault.StoreSecretAsync(secretPath, new Dictionary<string, string> { 
        { secretKey, secretValue } 
    });
    
    var retrievedSecret = await keyVault.GetSecretAsync(secretPath);
    
    // Assert
    Assert.NotNull(retrievedSecret);
    Assert.True(retrievedSecret.ContainsKey(secretKey));
    Assert.Equal(secretValue, retrievedSecret[secretKey]);
    
    // Cleanup
    await keyVault.DeleteSecretAsync(secretPath);
}
```

### 6. Observabilidade

```csharp
[Fact]
[Category("Integration")]
[Category("Observability")]
public void Should_CollectMetrics_When_UsingPrometheusProvider()
{
    // Arrange
    var metricsBuilder = new MetricsBuilder()
        .Configuration.Configure(options => {
            options.Enabled = true;
            options.ListenAddress = "localhost";
            options.Port = 9090;
        })
        .OutputMetrics.AsPrometheusPlainText()
        .OutputMetrics.AsPrometheusProtobuf()
        .Build();
    
    var metricsService = new PrometheusMetricsService(metricsBuilder);
    var counter = metricsService.CreateCounter("test_counter", "Test counter");
    
    // Act
    counter.Increment();
    counter.Increment(5);
    
    // Assert - verificar se a métrica está disponível na endpoint do Prometheus
    using (var httpClient = new HttpClient())
    {
        var response = httpClient.GetAsync("http://localhost:9090/metrics").Result;
        var content = response.Content.ReadAsStringAsync().Result;
        
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("test_counter", content);
    }
}
```

## Execução dos Testes de Integração

Para executar os testes de integração, utilize os scripts fornecidos:

```bash
# Iniciar os serviços necessários
./start-dev-env.sh

# Executar todos os testes de integração
./test-integration.sh all

# Ou executar testes para um serviço específico
./test-integration.sh rabbitmq
./test-integration.sh postgres
./test-integration.sh minio
./test-integration.sh vault
./test-integration.sh keyvault
./test-integration.sh observability
```

## Práticas Recomendadas

1. **Isolamento**: Sempre deixe os testes independentes
2. **Limpeza**: Certifique-se de limpar os recursos após os testes
3. **Configuração Externa**: Use variáveis de ambiente ou configurações para apontar para serviços de teste
4. **Tolerância a Falhas**: Trate adequadamente falhas temporárias em serviços externos
5. **Timeout**: Defina timeouts adequados para operações que podem demorar
6. **Logs Detalhados**: Inclua logs detalhados para facilitar a depuração de falhas
7. **CI/CD**: Certifique-se de que os testes podem ser executados em pipelines de CI/CD

## Integração com o Memory Bank

Os padrões e o status dos testes de integração são documentados no Memory Bank:

- **systemPatterns.md**: Padrões e arquitetura geral dos testes
- **progress.md**: Status atual da implementação dos testes
- **activeContext.md**: Contexto atual e próximos passos

Para atualizar o Memory Bank após alterações nos padrões ou implementação de novos testes, use:

```
update memory bank
```
