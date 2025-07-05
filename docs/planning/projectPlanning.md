# Planning

Siga este plano completo, prático e detalhado para transformar o backend do Smart Alarm de um estágio de fundação para um sistema funcional, cobrindo todos os gaps e pontos fracos identificados:

---

## 1. Modelagem Completa do Domínio

- **Revisar e completar entidades**: Definir todas as propriedades, métodos e regras de negócio das entidades principais (`Alarm`, `User`, `Schedule`, `Routine`, etc.), baseando-se nos requisitos do produto.
- **Value Objects**: Implementar lógica de validação e invariantes em `Email`, `Name`, `TimeConfiguration` e outros VOs, garantindo imutabilidade e consistência.
- **Regras de negócio**: Implementar métodos de domínio (ex: ativar/desativar alarme, validar horários, associar rotinas a usuários).

**Exemplo – Value Object:**

```csharp
public class Email
{
    public string Value { get; }
    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new ArgumentException("E-mail inválido.", nameof(value));
        Value = value;
    }
    // Override Equals/GetHashCode para imutabilidade
}
```

**Exemplo – Entidade:**

```csharp
public class Alarm
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    // Outras regras de negócio...
}
```

---

## 2. Serviços de Domínio

- **Interfaces**: Definir métodos claros e completos nas interfaces de serviço de domínio (`IAlarmDomainService`, `IUserDomainService`, etc.), refletindo os casos de uso reais.
- **Implementações**: Criar classes concretas para cada serviço, implementando as regras de negócio e orquestrando entidades e VOs.
- **Cobertura de cenários**: Garantir que todos os fluxos críticos do negócio estejam contemplados (criação, edição, exclusão, consulta, etc.).

**Exemplo – Interface:**

```csharp
public interface IAlarmDomainService
{
    Task<Alarm> CreateAlarmAsync(string name, TimeConfiguration timeConfig, CancellationToken ct);
    Task ActivateAlarmAsync(Guid alarmId, CancellationToken ct);
    // Outros métodos...
}
```

**Exemplo – Implementação:**

```csharp
public class AlarmDomainService : IAlarmDomainService
{
    // Injeção de dependências (repositórios, etc.)
    public async Task<Alarm> CreateAlarmAsync(string name, TimeConfiguration timeConfig, CancellationToken ct)
    {
        // Validação, criação e persistência
    }
    public async Task ActivateAlarmAsync(Guid alarmId, CancellationToken ct)
    {
        // Busca, validação e ativação
    }
}
```

---

## 3. Application Layer

- **Commands/Queries**: Definir e implementar todos os Commands e Queries necessários para os casos de uso do sistema.
- **Handlers**: Implementar Handlers com lógica de orquestração, validação (usando FluentValidation) e tratamento de erros.
- **Validators**: Criar validadores robustos para todos os DTOs de entrada, cobrindo regras de negócio e integridade de dados.

**Exemplo – Command/Handler:**

```csharp
public record CreateAlarmCommand(string Name, TimeConfiguration TimeConfig) : IRequest<AlarmDto>;

public class CreateAlarmCommandHandler : IRequestHandler<CreateAlarmCommand, AlarmDto>
{
    private readonly IAlarmDomainService _service;
    public async Task<AlarmDto> Handle(CreateAlarmCommand request, CancellationToken ct)
    {
        var alarm = await _service.CreateAlarmAsync(request.Name, request.TimeConfig, ct);
        return new AlarmDto(alarm.Id, alarm.Name, alarm.IsActive);
    }
}
```

**Exemplo – Validator:**

```csharp
public class CreateAlarmCommandValidator : AbstractValidator<CreateAlarmCommand>
{
    public CreateAlarmCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        // Outras regras...
    }
}
```

---

## 4. Infraestrutura

- **Repositórios**: Completar implementações reais dos repositórios (EF, Dapper, InMemory para testes), cobrindo todos os métodos CRUD e queries específicas.
- **Mensageria**: Implementar integração real com um broker (ex: RabbitMQ, OCI Streaming, etc.), substituindo mocks.
- **Storage**: Integrar com um serviço real de storage (OCI Object Storage, S3, etc.), implementando upload, download e delete.
- **Métricas e Tracing**: Integrar com Prometheus (via OpenTelemetry) e Application Insights, expondo métricas reais e spans de tracing.
- **KeyVault Providers**: Implementar providers reais para OCI Vault, Azure Key Vault, AWS Secrets Manager, etc., com testes de integração.

**Exemplo – Repositório EF:**

```csharp
public class EfAlarmRepository : IAlarmRepository
{
    private readonly SmartAlarmDbContext _context;
    public async Task AddAsync(Alarm alarm)
    {
        _context.Alarms.Add(alarm);
        await _context.SaveChangesAsync();
    }
    // Outros métodos...
}
```

**Exemplo – Métricas Prometheus:**

```csharp
// Startup.cs
services.AddOpenTelemetryMetrics(builder =>
    builder.AddAspNetCoreInstrumentation()
           .AddPrometheusExporter());
```

---

## 5. API Layer

- **Controllers**: Implementar controllers RESTful completos para todos os recursos, seguindo boas práticas (DTOs, status codes, tratamento de erros).
- **Documentação**: Garantir documentação Swagger/OpenAPI atualizada e precisa.
- **Middlewares**: Implementar middlewares para logging, tracing, autenticação e validação.

**Exemplo – Controller:**

```csharp
[ApiController]
[Route("api/alarms")]
public class AlarmsController : ControllerBase
{
    private readonly IMediator _mediator;
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAlarmCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    // Outros endpoints...
}
```

---

## 6. Serverless & Deploy

- **Function Handlers**: Implementar handlers compatíveis com OCI Functions para os principais fluxos (ex: criação de alarme, execução de rotina).
- **Scripts de Deploy**: Criar scripts e pipelines para build, publish e deploy automático no padrão serverless (OCI).
- **Configuração de ambiente**: Garantir parametrização via KeyVault e variáveis de ambiente.

**Exemplo – OCI Function Handler:**

```csharp
public class AlarmFunction
{
    public async Task<AlarmDto> HandleAsync(CreateAlarmCommand command)
    {
        // Lógica de criação de alarme
    }
}
```

---

## 7. Testes

- **Unitários**: Garantir cobertura mínima de 80% para código crítico, cobrindo entidades, serviços, handlers e repositórios.
- **Integração**: Criar testes de integração ponta a ponta para os principais fluxos de negócio, incluindo persistência, mensageria, storage e autenticação.
- **Mocks e Fakes**: Utilizar mocks apenas onde necessário, priorizando testes reais com infraestrutura local ou de homologação.

**Exemplo – Teste Unitário:**

```csharp
[Fact]
public void Should_Activate_Alarm()
{
    var alarm = new Alarm("Teste");
    alarm.Activate();
    alarm.IsActive.Should().BeTrue();
}
```

**Exemplo – Teste de Integração:**

```csharp
[Fact]
public async Task Should_Create_Alarm_And_Persist()
{
    // Arrange: setup contexto real, dependências
    // Act: criar alarme via handler/controller
    // Assert: verificar persistência e retorno
}
```

---

## 8. Observabilidade e Segurança

- **Métricas**: Expor métricas customizadas e de infraestrutura via Prometheus.
- **Tracing**: Implementar tracing distribuído real com OpenTelemetry.
- **Segurança**: Garantir autenticação JWT/FIDO2, RBAC e LGPD em todos os endpoints.

**Exemplo – Middleware de Tracing:**

```csharp
public class ObservabilityMiddleware
{
    // Implementação de tracing e logging estruturado
}
```

---

## 9. Integrações Externas

- **Mensageria**: Substituir mocks por integrações reais.
- **Storage**: Testar upload/download real.
- **KeyVault**: Validar leitura/escrita de segredos em todos os providers suportados.

---

## 10. Checklist de Entrega

- Marcar cada item como concluído conforme a implementação real for avançando:

- [ ] Entidades e VOs completos e validados
- [ ] Serviços de domínio implementados
- [ ] Application Layer funcional (Commands, Handlers, Validators)
- [ ] Infraestrutura real (repositórios, mensageria, storage, métricas, tracing, keyvault)
- [ ] Controllers RESTful completos
- [ ] Handlers serverless e scripts de deploy
- [ ] Testes unitários e de integração cobrindo fluxos reais
- [ ] Observabilidade e segurança implementadas
- [ ] Documentação atualizada

---

## Recomendações Finais

- Priorize a implementação dos fluxos de negócio mais críticos para o MVP.
- Use TDD sempre que possível para garantir qualidade e evitar retrabalho.
- Faça entregas incrementais, validando cada integração antes de avançar.
- Atualize a documentação e o Memory Bank a cada etapa concluída.
- Siga as instructions do projeto
