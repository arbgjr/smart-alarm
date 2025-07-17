## 🎯 **FASE 1: CRÍTICA - Segurança e Autenticação** 
**Duração Estimada:** 3-4 dias | **Prioridade:** 🔴 **ALTA**

### **1.1 JWT Real no Integration Service**
**Arquivo:** Program.cs (linha ~45)

**Problema Atual:**
```csharp
// TODO: Configurar JWT adequadamente
options.RequireHttpsMetadata = false;
options.SaveToken = true;
```

**Implementação:**
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
**Arquivo:** QueryHandlers.cs (linha ~96)

**Problema Atual:**
```csharp
Name = "Test User", // TODO: Buscar do banco
Email = "user@example.com", // TODO: Buscar do banco
```

**Implementação:**
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
            var isValid = await _jwtTokenService.ValidateTokenAsync(request.Token);
            if (!isValid) return null;

            var userId = await _jwtTokenService.GetUserIdFromTokenAsync(request.Token);
            if (!userId.HasValue) return null;

            var user = await _userRepository.GetByIdAsync(userId.Value);
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

### **1.3 Melhorar AuthController - Remover Hardcoding**
**Arquivo:** AuthController.cs

**Ação:** Verificar se existe hardcoding de credenciais e substituir por validação real via handlers.

---

## 🚀 **FASE 2: FUNCIONALIDADES - MVP Completo**
**Duração Estimada:** 5-7 dias | **Prioridade:** 🟡 **ALTA**

### **2.1 Implementar OCI Object Storage Real**
**Arquivo:** OciObjectStorageService.cs

**Implementação:**
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

    // Implementar DownloadAsync e DeleteAsync similar
}
```

**Dependências:**
- Adicionar pacote `OCI.DotNetSDK.Objectstorage`
- Configurar autenticação OCI (instance principal ou config file)
- Variáveis de ambiente para namespace, bucket, region

### **2.2 Corrigir ListRoutinesHandler**
**Arquivo:** ListRoutinesHandler.cs (linha 55)

**Problema Atual:**
```csharp
routines = await _routineRepository.GetByAlarmIdAsync(System.Guid.Empty); // TODO: ajustar para buscar todas se necessário
```

**Implementação:**
```csharp
IEnumerable<Domain.Entities.Routine> routines;
if (request.AlarmId.HasValue)
{
    routines = await _routineRepository.GetByAlarmIdAsync(request.AlarmId.Value);
}
else
{
    // Buscar todas as rotinas se AlarmId não for especificado
    routines = await _routineRepository.GetAllAsync();
}

// Se UserId for especificado, filtrar por usuário através dos alarmes
if (request.UserId.HasValue)
{
    var userAlarms = await _alarmRepository.GetByUserIdAsync(request.UserId.Value);
    var userAlarmIds = userAlarms.Select(a => a.Id).ToHashSet();
    routines = routines.Where(r => userAlarmIds.Contains(r.AlarmId));
}
```

### **2.3 Implementar Integrações Reais - Integration Service**

#### **2.3.1 IntegrationsController - Comando Real**
**Arquivo:** IntegrationsController.cs

**Remover:** `mockIntegration` e implementar:
```csharp
[HttpPost]
public async Task<IActionResult> CreateIntegration([FromBody] CreateIntegrationRequest request)
{
    var command = new CreateIntegrationCommand(
        UserId: GetCurrentUserId(),
        Provider: request.Provider,
        Configuration: request.Configuration,
        EnableNotifications: request.EnableNotifications,
        Features: request.Features
    );

    var result = await _mediator.Send(command);
    return CreatedAtAction(nameof(GetIntegration), new { id = result.Id }, result);
}
```

#### **2.3.2 GetUserIntegrationsQueryHandler - Dados Reais**
**Arquivo:** GetUserIntegrationsQueryHandler.cs

**Substituir:** `mockIntegrations` por busca real:
```csharp
private async Task<List<UserIntegrationInfo>> GetUserIntegrationsFromStorage(
    Guid userId, 
    string? providerFilter, 
    bool includeInactive)
{
    var integrations = await _integrationRepository.GetByUserIdAsync(userId);
    
    return integrations
        .Where(i => includeInactive || i.IsActive)
        .Where(i => string.IsNullOrEmpty(providerFilter) || 
                   i.Provider.Equals(providerFilter, StringComparison.OrdinalIgnoreCase))
        .Select(MapToUserIntegrationInfo)
        .ToList();
}
```

---

## 🔧 **FASE 3: INTEGRAÇÕES EXTERNAS - Calendários e APIs**
**Duração Estimada:** 7-10 dias | **Prioridade:** 🟢 **MÉDIA**

### **3.1 Implementar Calendários Externos Reais**
**Arquivo:** SyncExternalCalendarCommandHandler.cs

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

---

## 📊 **Dependências e Riscos:**

- **OCI SDK**: Necessário credenciais e configuração adequada
- **APIs Externas**: Rate limits, autenticação OAuth2
- **Testes**: Cada implementação deve ter testes unitários e integração
- **Documentação**: Atualizar Swagger/OpenAPI para novas funcionalidades

---

## 🔄 **Próximos Passos Imediatos**

1. **Configurar ambiente OCI** para testes de Object Storage
2. **Obter credenciais Google/Microsoft** para testes de calendário
3. **Implementar FASE 1** completamente antes de avançar
4. **Executar suite de testes** após cada fase
5. **Atualizar documentação** conforme implementação

Este planejamento garante uma evolução **segura**, **incremental** e **testável** do sistema, priorizando funcionalidades críticas e mantendo a qualidade do código.