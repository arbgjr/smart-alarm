# Smart Alarm – Copilot Instructions (AI Agents)

## Visão Geral e Arquitetura

Smart Alarm é uma plataforma modular para gestão inteligente de alarmes, rotinas e integrações, baseada 100% em C# (.NET 8+), Clean Architecture e OCI Functions (serverless). Todos os serviços seguem separação clara: Domain, Application, Infrastructure, Api. Veja `/services/` e `/src/` para exemplos.

**Padrões obrigatórios:**
- Clean Architecture, SOLID, DTOs validados (FluentValidation)
- Logging estruturado (Serilog), tracing (OpenTelemetry, Application Insights)
- Integração externa desacoplada via HttpClientFactory, Polly, OAuth2/OpenID Connect
- Nunca expor segredos em código/logs

**Referências rápidas:**
- Padrões: [`docs/architecture/systemPatterns.md`](../docs/architecture/systemPatterns.md)
- Observabilidade: [`src/SmartAlarm.Observability/README.md`](../src/SmartAlarm.Observability/README.md)
- ADRs: [`docs/architecture/`](../docs/architecture/)
- Exemplos de testes: [`tests/SmartAlarm.Tests/`](../tests/SmartAlarm.Tests/)

## Contexto e Memória

O projeto utiliza o **Memory Bank** (`memory-bank/`) para registrar contexto, decisões e progresso. Sempre consulte e mantenha alinhamento com os arquivos de contexto antes de propor mudanças arquiteturais ou padrões. Veja instruções em [`memory-bank/memory-bank.instructions.md`](../memory-bank/memory-bank.instructions.md) se disponível.

## Fluxo de Desenvolvimento

**Build e dependências:**
- `dotnet restore` para dependências
- `dotnet build` para build local

**Testes:**
- `dotnet test --logger "console;verbosity=detailed"` (sempre usar este logger)
- Cobertura: `dotnet test --collect:"XPlat Code Coverage" --settings tests/coverlet.runsettings`
- Testes de integração exigem infraestrutura local: `docker compose up -d --build` (RabbitMQ, Vault, MinIO, PostgreSQL)
- Scripts úteis: `tests/run-auth-tests.ps1`, `tests/SmartAlarm-test.sh`
- AAA obrigatório em todos os testes (ver exemplos em `tests/SmartAlarm.Tests/Domain/Entities/`)

**Debug:**
- Use mocks (`MockStorageService`, `MockKeyVaultProvider`, etc.) para testes isolados
- Para troubleshooting de integração, veja [`docs/development/testes-integracao.md`](../docs/development/testes-integracao.md)

## Convenções Específicas

- PascalCase para classes, métodos públicos, arquivos
- camelCase para variáveis, métodos privados
- UPPER_SNAKE_CASE para constantes globais
- Componentes React organizados por atomicidade (frontend)
- Commits: siga [`.github/instructions/commit-message.instructions.md`](./instructions/commit-message.instructions.md)

## Segurança e Variáveis Sensíveis

- Nunca exponha segredos, tokens ou credenciais em código, logs ou bundles
- Variáveis sensíveis devem ser lidas de configuração segura ou KeyVault (ver exemplos em `src/SmartAlarm.Infrastructure/KeyVault/`)

## Pull Requests e Revisão

- Siga o template de PR e checklist em [`.github/instructions/pull-request.instructions.md`](./instructions/pull-request.instructions.md)
- Sempre descreva claramente o objetivo, mudanças técnicas e pendências
- Confirme que todos os testes passam e que não há segredos/credenciais expostos

## Integração e Comunicação

- Serviços se comunicam via REST (ver APIs em `/services/*/Api/`)
- Repositórios desacoplados: multi-provider (PostgreSQL dev/test, Oracle prod) – veja `src/SmartAlarm.Infrastructure/README.md`
- Mensageria: RabbitMQ (dev), stub para OCI Streaming (prod)
- Storage: MinIO (dev), stub para OCI Object Storage (prod)
- KeyVault: HashiCorp Vault (dev), estrutura extensível para OCI/Azure/AWS

## Exemplos Frontend (se aplicável)

- Componentes React seguem Atomic Design (veja exemplos em `docs/frontend/` ou `src/SmartAlarm.Frontend/` se existir)
- Testes de componentes: utilize Testing Library, cubra interações, acessibilidade e estados visuais

## Exemplos Essenciais

**Handler com validação e logging:**
```csharp
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed: {@Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }
        var alarm = new Alarm { /* ... */ };
        await _alarmRepository.AddAsync(alarm);
        _logger.LogInformation("Alarm created: {AlarmId}", alarm.Id);
        return new AlarmResponse(alarm);
    }
}
```

**Teste AAA com xUnit:**
```csharp
[Fact]
public async Task Should_ThrowValidationException_When_CommandIsInvalid()
{
    // Arrange
    var handler = new CreateAlarmHandler(...);
    var invalidCommand = new CreateAlarmCommand { /* ... */ };
    // Act & Assert
    await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(invalidCommand, CancellationToken.None));
}
```

---
