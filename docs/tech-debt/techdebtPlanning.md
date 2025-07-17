## Análise Completa - Implementações Necessárias para Produção

Com base na análise do código, aqui estão os arquivos específicos e alterações necessárias para deixar o projeto ready to production:

## 🎯 **FASE 1: CRÍTICA - Segurança e Autenticação**

### **1.1 JWT Real no Integration Service**
**Arquivo:** Program.cs (linha ~45)

**Problema Atual:**
```csharp
// TODO: Configurar JWT adequadamente
options.RequireHttpsMetadata = false;
options.SaveToken = true;
```

**Implementação Necessária:**
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"];
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
        
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
    });
```

### **1.2 Corrigir QueryHandlers - Busca Real do Banco**
**Arquivo:** `src/SmartAlarm.Application/Queries/QueryHandlers.cs` (linha ~96)

**Problema Atual:**
```csharp
Name = "Test User", // TODO: Buscar do banco
Email = "user@example.com", // TODO: Buscar do banco
```

**Implementação Necessária:**
```csharp
public class ValidateTokenHandler : IRequestHandler<ValidateTokenQuery, UserDto?>
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ValidateTokenHandler> _logger;

    public async Task<UserDto?> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var claims = await _jwtTokenService.ValidateTokenAndGetClaimsAsync(request.Token);
            if (claims == null) return null;

            var userIdClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId)) 
                return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || !user.IsActive) return null;

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name.Value,
                Email = user.Email.Address,
                IsActive = user.IsActive,
                EmailVerified = user.EmailVerified,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToArray() ?? Array.Empty<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return null;
        }
    }
}
```

## 🚀 **FASE 2: FUNCIONALIDADES - MVP Completo**

### **2.1 Implementar OCI Object Storage Real**
**Arquivo:** OciObjectStorageService.cs

**Problema Atual:** Contém múltiplos `TODO: Implementar integração real com OCI SDK`

**Implementação Necessária:**
```csharp
public class OciObjectStorageService : IStorageService
{
    private readonly ObjectStorageClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OciObjectStorageService> _logger;

    public async Task UploadAsync(string path, Stream content)
    {
        try
        {
            var putObjectRequest = new PutObjectRequest
            {
                NamespaceName = _configuration["OCI:ObjectStorage:Namespace"],
                BucketName = _configuration["OCI:ObjectStorage:BucketName"],
                ObjectName = path,
                PutObjectBody = content
            };

            await _client.PutObject(putObjectRequest);
            _logger.LogInformation("Successfully uploaded {Path} to OCI Object Storage", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload {Path} to OCI Object Storage", path);
            throw;
        }
    }

    public async Task<Stream?> DownloadAsync(string path)
    {
        try
        {
            var getObjectRequest = new GetObjectRequest
            {
                NamespaceName = _configuration["OCI:ObjectStorage:Namespace"],
                BucketName = _configuration["OCI:ObjectStorage:BucketName"],
                ObjectName = path
            };

            var response = await _client.GetObject(getObjectRequest);
            return response.InputStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download {Path} from OCI Object Storage", path);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string path)
    {
        try
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                NamespaceName = _configuration["OCI:ObjectStorage:Namespace"],
                BucketName = _configuration["OCI:ObjectStorage:BucketName"],
                ObjectName = path
            };

            await _client.DeleteObject(deleteObjectRequest);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete {Path} from OCI Object Storage", path);
            return false;
        }
    }
}
```

### **2.2 Implementar OCI Streaming Real**
**Arquivo:** OciStreamingMessagingService.cs

**Problema Atual:** `TODO: Implementar integração real com OCI SDK`

**Implementação Necessária:**
```csharp
public async Task PublishEventAsync(string topic, string message)
{
    try
    {
        _logger.LogInformation("Publishing event to OCI Streaming topic {Topic}: {Message}", topic, message);
        
        var streamClient = new StreamClient(GetAuthenticationDetailsProvider());
        streamClient.SetEndpoint(_endpoint);
        
        var putMessagesRequest = new PutMessagesRequest
        {
            StreamId = _streamOcid,
            PutMessagesDetails = new PutMessagesDetails
            {
                Messages = new List<PutMessagesDetailsEntry>
                {
                    new PutMessagesDetailsEntry
                    {
                        Key = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{topic}:{_partitionKey}")),
                        Value = Convert.ToBase64String(Encoding.UTF8.GetBytes(message))
                    }
                }
            }
        };
        
        var response = await streamClient.PutMessages(putMessagesRequest);
        _logger.LogInformation("Successfully published event to OCI Streaming topic {Topic}", topic);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to publish event to OCI Streaming topic {Topic}", topic);
        throw new InvalidOperationException($"Erro ao publicar evento no tópico {topic}", ex);
    }
}
```

### **2.3 Implementar OCI Vault Real**
**Arquivo:** OciVaultProvider.cs

**Problema Atual:** `TODO: Implement OCI Vault secret retrieval`

**Implementação Necessária:**
```csharp
public async Task<string?> GetSecretAsync(string secretKey, CancellationToken cancellationToken = default)
{
    try
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Secret key cannot be null or empty");
            return null;
        }

        var vaultClient = new VaultsClient(GetAuthenticationDetailsProvider());
        var secretsClient = new SecretsClient(GetAuthenticationDetailsProvider());
        
        // List secrets to find the one we want
        var listSecretsRequest = new ListSecretsRequest
        {
            CompartmentId = _options.CompartmentId,
            VaultId = _options.VaultId,
            Name = secretKey
        };

        var secrets = await vaultClient.ListSecrets(listSecretsRequest);
        var secret = secrets.Items.FirstOrDefault();
        
        if (secret == null)
        {
            _logger.LogWarning("Secret '{SecretKey}' not found in OCI Vault", secretKey);
            return null;
        }

        var getSecretBundleRequest = new GetSecretBundleRequest
        {
            SecretId = secret.Id
        };

        var secretBundle = await secretsClient.GetSecretBundle(getSecretBundleRequest);
        var secretContent = secretBundle.SecretBundle.SecretBundleContent as Base64SecretBundleContentDetails;
        
        if (secretContent?.Content != null)
        {
            var secretValue = Encoding.UTF8.GetString(Convert.FromBase64String(secretContent.Content));
            _logger.LogDebug("Successfully retrieved secret '{SecretKey}' from OCI Vault", secretKey);
            return secretValue;
        }

        _logger.LogWarning("Secret content is null for '{SecretKey}'", secretKey);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to retrieve secret '{SecretKey}' from OCI Vault", secretKey);
        return null;
    }
}
```

### **2.4 Corrigir ListRoutinesHandler**
**Arquivo:** ListRoutinesHandler.cs (linha ~96)

**Status:** ✅ **JÁ IMPLEMENTADO** - A análise mostrou que este handler já possui implementação real com busca do banco

## 🔧 **FASE 3: INTEGRAÇÕES EXTERNAS**

### **3.1 Implementar Calendários Externos Reais**
**Arquivo:** `services/integration-service/Handlers/SyncExternalCalendarCommandHandler.cs`

**Substituir:** `mockEvents` por integrações reais:

#### **Google Calendar:**
```csharp
private async Task<List<ExternalCalendarEvent>> FetchGoogleCalendarEvents(
    string accessToken, 
    DateTime fromDate, 
    DateTime toDate, 
    CancellationToken cancellationToken)
{
    var credential = GoogleCredential.FromAccessToken(accessToken);
    var service = new CalendarService(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credential
    });

    var request = service.Events.List("primary");
    request.TimeMin = fromDate;
    request.TimeMax = toDate;
    request.SingleEvents = true;
    request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

    var events = await request.ExecuteAsync();
    
    return events.Items.Select(e => new ExternalCalendarEvent(
        e.Id,
        e.Summary ?? "Sem título",
        e.Start.DateTime ?? DateTime.Parse(e.Start.Date),
        e.End.DateTime ?? DateTime.Parse(e.End.Date),
        e.Location ?? "",
        e.Description ?? ""
    )).ToList();
}
```

#### **Microsoft Outlook:**
```csharp
private async Task<List<ExternalCalendarEvent>> FetchOutlookCalendarEvents(
    string accessToken, 
    DateTime fromDate, 
    DateTime toDate, 
    CancellationToken cancellationToken)
{
    var graphServiceClient = new GraphServiceClient(
        new DelegateAuthenticationProvider((requestMessage) =>
        {
            requestMessage.Headers.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return Task.FromResult(requestMessage);
        }));

    var events = await graphServiceClient.Me.Events
        .Request()
        .Filter($"start/dateTime ge '{fromDate:O}' and end/dateTime le '{toDate:O}'")
        .GetAsync();

    return events.Select(e => new ExternalCalendarEvent(
        e.Id,
        e.Subject ?? "Sem título",
        e.Start.DateTime ?? DateTime.Now,
        e.End.DateTime ?? DateTime.Now,
        e.Location?.DisplayName ?? "",
        e.Body?.Content ?? ""
    )).ToList();
}
```

### **3.2 Corrigir Integration Service Controllers**
**Arquivo:** IntegrationsController.cs

**Remover:** `mockIntegration` e implementar comandos reais via MediatR

## 📋 **Dependências Necessárias**

### **Pacotes NuGet a Adicionar:**
```xml
<PackageReference Include="OCI.DotNetSDK.Objectstorage" Version="65.49.0" />
<PackageReference Include="OCI.DotNetSDK.Streaming" Version="65.49.0" />
<PackageReference Include="OCI.DotNetSDK.Vault" Version="65.49.0" />
<PackageReference Include="Google.Apis.Calendar.v3" Version="1.68.0.3324" />
<PackageReference Include="Microsoft.Graph" Version="5.42.0" />
```

### **Configurações de Ambiente:**
```bash
# OCI Configuration
OCI_OBJECT_STORAGE_NAMESPACE=your_oci_namespace
OCI_OBJECT_STORAGE_BUCKET_NAME=smartalarm-storage
OCI_REGION=us-ashburn-1
OCI_STREAMING_STREAM_OCID=your_stream_ocid
OCI_STREAMING_ENDPOINT=your_streaming_endpoint
OCI_VAULT_ID=your_vault_id
OCI_COMPARTMENT_ID=your_compartment_id

# External APIs
GOOGLE_CLIENT_ID=your_google_client_id
GOOGLE_CLIENT_SECRET=your_google_client_secret
MICROSOFT_CLIENT_ID=your_microsoft_client_id
MICROSOFT_CLIENT_SECRET=your_microsoft_client_secret
MICROSOFT_TENANT_ID=your_microsoft_tenant_id
```

## 🚨 **Warnings Críticos a Corrigir**

### **Vulnerabilidades de Segurança:**
```bash
# Executar para correção automática
./fix-warnings.sh
```

**Atualizar:**
- Azure.Identity de 1.10.4 para 1.12.0+
- Oracle.ManagedDataAccess para Oracle.ManagedDataAccess.Core 3.21.120
