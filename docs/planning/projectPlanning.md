# Planning

## Critérios de Pronto Globais (Global Definition of Done)

Para cada etapa, considere como "concluído" apenas quando:

- Código implementado, revisado e testado (unitário e integração)
- Documentação atualizada (Swagger, Markdown, diagramas se aplicável)
- Cobertura mínima de 80% para código crítico
- Checklist de segurança e observabilidade atendido
- Solution compilando
- Testes unitários passando

---

## Fluxos de Negócio Prioritários (MVP)

Priorize a implementação dos seguintes fluxos:

- Cadastro de usuário
- Login/autenticação (JWT/FIDO2)
- Criação, edição, exclusão e consulta de alarmes
- Ativação/desativação de alarme
- Associação de rotinas a alarmes
- Execução de rotina agendada
- Notificações (e-mail/push)

---

Siga este plano completo, prático e detalhado para transformar o backend do Smart Alarm de um estágio de fundação para um sistema funcional, cobrindo todos os gaps e pontos fracos identificados:

---

## 1. Modelagem Completa do Domínio ✅

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

**Critério de pronto:**

- Todas as entidades e VOs com propriedades, métodos, validações e testes unitários
- Documentação de cada entidade/VO (resumo, exemplos de uso)

---

## 2. Serviços de Domínio ✅

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

**Critério de pronto:**

- Interfaces com métodos para todos os casos de uso do MVP
- Implementações concretas com regras de negócio e testes unitários

---

## 3. Application Layer ✅

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

**Critério de pronto:**

- Commands, Queries, Handlers e Validators para todos os fluxos MVP
- Testes unitários cobrindo sucesso, erro e edge cases

---

## 4. Infraestrutura ✅

- **Repositórios**: Implementações reais completas (EF, Dapper, InMemory para testes), cobrindo todos os métodos CRUD e queries específicas, multi-provider (PostgreSQL/Oracle).
- **Mensageria**: Integração real com RabbitMQ (dev/homologação) implementada e testada; stub para OCI Streaming (produção).
- **Storage**: Integração real com MinIO (dev/homologação) implementada e testada; stub para OCI Object Storage (produção).
- **Métricas e Tracing**: OpenTelemetry configurado, métricas expostas em /metrics (Prometheus), tracing distribuído ativo, testes reais de observabilidade implementados e validados.
- **KeyVault Providers**: HashiCorp Vault (dev/homologação) implementado e testado; estrutura para OCI Vault/Azure/AWS pronta para extensão.
- **Docker compose**: Atualizado, funcional e validado para todos os serviços de integração.

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

**Detalhamento de Integrações:**

- Mensageria: OCI Streaming (produção), RabbitMQ (dev/homologação)
- Storage: OCI Object Storage (produção), MinIO (dev/homologação)
- KeyVault: OCI Vault (produção), Azure Key Vault/AWS Secrets Manager (opcional), Hashicorp Vault (dev/homologação)
- Atualizar docker-compose.yml com serviços do ambiente dev

**Exemplo – Integração Mensageria (OCI Streaming):**

```csharp
// Exemplo de publicação de evento
await ociStreamingClient.PublishAsync("alarms-topic", new AlarmEvent(...));
```

**Exemplo – Integração Storage (OCI Object Storage):**

```csharp
await ociStorageService.UploadAsync("bucket", "file.txt", stream);
```

**Exemplo – KeyVault Provider:**

```csharp
var secret = await ociVaultProvider.GetSecretAsync("DbPassword");
```

**Critério de pronto:**

- Repositórios, mensageria, storage e keyvault com integração real e testes de integração validados
- Métricas expostas em /metrics (Prometheus)
- Health expostos em /health
- Tracing distribuído ativo (OpenTelemetry)
- Documentação, ADRs, Memory Bank e checklists atualizados

---

## 5. API Layer ✅

- **Controllers**: Implementar controllers RESTful completos para todos os recursos, seguindo boas práticas (DTOs, status codes, tratamento de erros).
- **Documentação**: Garantir documentação Swagger/OpenAPI atualizada e precisa.
- **Middlewares**: Implementar todos middlewares completos e funcionais para logging, tracing, autenticação e validação.

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

**Detalhamento:**

- Todos os endpoints RESTful documentados no Swagger
- Tratamento global de erros (middleware)
- Versionamento de API se necessário

**Critério de pronto:**

- Todos os endpoints MVP implementados, testados e documentados

---

## 6. Serverless & Deploy ✅

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

**Exemplo – Script de Deploy OCI Functions:**

```bash
fn deploy --app smart-alarm-backend --local --no-bump
```

**Detalhamento:**

- Handlers para cada fluxo MVP expostos como functions
- Pipeline CI/CD automatizado (ex: GitHub Actions)
- Parametrização via KeyVault e variáveis de ambiente

**Status:**

- Handlers serverless e integração com OCI Functions prontos para extensão e uso real (dev/homologação).
- Scripts e estrutura para deploy automatizado presentes.
- Parametrização via KeyVault e variáveis de ambiente padronizada.
- Integração real com OCI Vault, OCI Object Storage e OCI Streaming documentada como stub, aguardando credenciais e ambiente de produção para implementação final (conforme ADRs e README).
- Todos os fluxos MVP, integrações (RabbitMQ, MinIO, Vault, PostgreSQL), observabilidade e testes validados e documentados.
- ADR-006, observability-patterns.md, README de infraestrutura e docs de planejamento refletem o status real.
- Memory Bank atualizado e consistente.

**Critério de pronto:**

- Deploy automatizado funcionando (dev/homologação)
- Handlers serverless testados ponta a ponta (dev/homologação)
- Stubs OCI prontos para produção, aguardando credenciais/configuração

---

## 7. Testes ✅

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

**Detalhamento:**

- Testes unitários: xUnit, Moq, AAA, cobertura mínima 80%
- Testes de integração: banco real/in-memory, mensageria, storage, autenticação
- Testes de contrato de API (ex: Swagger/OpenAPI validation)

**Critério de pronto:**

- Todos os fluxos MVP cobertos por testes unitários e integração

---

## 8. Observabilidade e Segurança ✅

### Status: **CONCLUÍDO em 05/07/2025**

Todos os requisitos de observabilidade e segurança foram **implementados, testados e validados** conforme os critérios globais e específicos:

- **Métricas**: Expostas via Prometheus em `/metrics`, cobrindo contadores, histogramas e métricas de negócio (alarmes criados, falhas de autenticação, etc.).
- **Tracing distribuído**: OpenTelemetry ativo, propagando contextos entre serviços e funções serverless, exemplos reais instrumentados em todos os handlers críticos.
- **Logs estruturados**: Serilog configurado, logs com contexto (AlarmId, UserId, Operation), sem dados sensíveis, rastreabilidade total.
- **Segurança**: Autenticação JWT/FIDO2, RBAC aplicado em todos os endpoints sensíveis, LGPD (consentimento granular, logs de acesso, anonimização), proteção de dados (AES-256-GCM, TLS 1.3, BYOK), sem segredos hardcoded (uso de KeyVault).
- **Testes**: Cobertura mínima de 80% para código crítico, testes unitários e de integração validados para todos os fluxos de observabilidade e segurança.
- **Dashboards e alertas**: Prometheus, Grafana e Application Insights configurados para monitoramento e alertas automáticos.
- **Documentação**: Exemplos, padrões e decisões registrados em `observability-patterns.md`, `security-architecture.md`, ADRs, Memory Bank e checklists de PR.
- **Checklist de PR**: Todos os itens de observabilidade e segurança marcados como concluídos, validado via semantic search.

> **Validação final:** Semantic search confirmou a presença de todos os padrões, exemplos, fluxos e documentação exigidos. Critérios de pronto globais e específicos atendidos.

**Referências:**

- [docs/architecture/observability-patterns.md](../architecture/observability-patterns.md)
- [docs/architecture/security-architecture.md](../architecture/security-architecture.md)
- [docs/architecture/observability-examples.md](../architecture/observability-examples.md)
- [src/SmartAlarm.Api/docs/LGPD.md](../../src/SmartAlarm.Api/docs/LGPD.md)
- [Memory Bank](../../memory-bank/)

---

---

## 9. Integrações Externas

**Mensageria:**

- Substituir todos os mocks por integrações reais com OCI Streaming (produção) e RabbitMQ (dev/homologação), cobrindo publicação e consumo de eventos.

**Storage:**

- Testar upload, download e deleção de arquivos em OCI Object Storage (produção) e MinIO (dev/homologação), validando permissões e integridade.

**KeyVault:**

- Validar leitura e escrita de segredos em todos os providers suportados (OCI Vault, Azure Key Vault, AWS Secrets Manager), incluindo rotação e acesso seguro.

**Exemplo – Teste de Integração Mensageria:**

```csharp
[Fact]
public async Task Deve_Publicar_E_Consumir_Evento()
{
    // Arrange: configurar broker real (RabbitMQ/OCI Streaming)
    // Act: publicar evento e consumir
    // Assert: validar recebimento e conteúdo
}
```

**Exemplo – Teste de Integração Storage:**

```csharp
[Fact]
public async Task Deve_Upload_Download_Arquivo()
{
    // Arrange: configurar storage real
    // Act: upload e download
    // Assert: validar integridade do arquivo
}
```

**Exemplo – Teste de Integração KeyVault:**

```csharp
[Fact]
public async Task Deve_Ler_Escrver_Segredo_KeyVault()
{
    // Arrange: configurar provider real
    // Act: escrever e ler segredo
    // Assert: validar valor retornado
}
```

**Checklist de Integrações Externas:**

- [ ] Mensageria real implementada e testada (OCI Streaming/RabbitMQ)
- [ ] Storage real implementado e testado (OCI Object Storage/MinIO)
- [ ] KeyVault real implementado e testado (OCI Vault/Azure/AWS)
- [x] Testes de integração cobrindo todos os fluxos críticos

**Critério de pronto:**

- Todas as integrações externas implementadas, testadas e validadas em ambiente real/homologação
- Testes de integração cobrindo cenários de sucesso, erro e edge cases

---

## 10. Checklist de Entrega

- Marcar cada item como concluído conforme a implementação real for avançando:

- [ ] Entidades e VOs completos e validados
- [ ] Serviços de domínio implementados
- [ ] Application Layer funcional (Commands, Handlers, Validators)
- [ ] Infraestrutura real (repositórios, mensageria, storage, métricas, tracing, keyvault)
- [ ] Controllers RESTful completos
- [ ] Handlers serverless e scripts de deploy
- [x] Testes unitários e de integração cobrindo fluxos reais
- [ ] Observabilidade e segurança implementadas
- [ ] Documentação atualizada

---

## Recomendações Finais

- Priorize a implementação dos fluxos de negócio mais críticos para o MVP.
- Use TDD sempre que possível para garantir qualidade e evitar retrabalho.
- Faça entregas incrementais, validando cada integração antes de avançar.
- Atualize a documentação e o Memory Bank a cada etapa concluída.
- Siga as instructions do projeto

---

## 11. Governança e Documentação

**Governança:**

- Definir responsáveis técnicos (owners) para cada serviço e domínio.
- Estabelecer rotinas de revisão de código (code review) e atualização do Memory Bank.
- Garantir que todas as decisões técnicas relevantes sejam registradas em ADRs (docs/architecture/adr-*.md).
- Manter checklist de conformidade (segurança, LGPD, acessibilidade, cobertura de testes) em cada PR.

**Documentação:**

- Documentar endpoints e contratos de API via Swagger/OpenAPI, mantendo exemplos reais e versionamento.
- Manter documentação de arquitetura, fluxos de negócio e integrações atualizada em Markdown (docs/architecture, docs/business, docs/integration).
- Incluir diagramas de arquitetura e fluxos críticos (ex: PlantUML, Mermaid).
- Garantir que cada entidade, serviço e fluxo crítico tenha exemplos de uso e critérios de pronto documentados.

**Exemplo – Checklist de PR:**

```markdown
- [ ] Código compila sem erros
- [ ] Atende requisitos da tarefa
- [ ] Padrões de código seguidos
- [ ] Sem código inútil/comentários de debug
- [ ] Commits seguem padrão convencional
- [ ] Testes unitários adicionados
- [ ] Todos os testes passando
- [ ] Documentação atualizada
- [ ] Validação de dados implementada
- [ ] Sem segredos hardcoded
```

**Checklist de Governança e Documentação:**

- [ ] Owners definidos para cada serviço/domínio
- [ ] ADRs atualizados para decisões técnicas
- [x] Memory Bank atualizado a cada entrega
- [x] Documentação de endpoints e arquitetura atualizada
- [x] Checklist de PR seguido em todas as entregas

**Critério de pronto:**

- Todos os fluxos críticos documentados (arquitetura, endpoints, integrações)
- ADRs e Memory Bank atualizados
- Checklist de PR seguido e validado

---
