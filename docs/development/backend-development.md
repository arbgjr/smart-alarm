# Backend Development Guide - Arquitetura Unificada em C#

Este guia cobre o desenvolvimento backend do Smart Alarm utilizando exclusivamente C# (.NET), seguindo Clean Architecture, princípios SOLID, testabilidade, segurança e integração nativa com Azure Functions.

## 🏗️ Filosofia de Arquitetura

A arquitetura backend é baseada em serviços especializados, todos implementados em C#/.NET, organizados como projetos independentes (AlarmService, AnalysisService, IntegrationService), preferencialmente serverless (Azure Functions). Todos os serviços seguem Clean Architecture, com separação clara de camadas (apresentação, aplicação, domínio, infraestrutura), facilitando testes, manutenção e evolução.

## 🚀 AlarmService: Operações CRUD de Alarmes

O AlarmService é responsável por todas as operações CRUD de alarmes, regras de negócio, notificações e validações específicas para neurodivergentes. Utiliza Entity Framework Core para persistência, FluentValidation para validação e logging estruturado (Serilog).

```csharp
// Application/Handlers/CreateAlarmHandler.cs
public class CreateAlarmHandler : IRequestHandler<CreateAlarmCommand, AlarmResponse>
{
    private readonly IAlarmRepository _alarmRepository;
    private readonly IValidator<CreateAlarmCommand> _validator;
    private readonly ILogger<CreateAlarmHandler> _logger;

    public CreateAlarmHandler(IAlarmRepository alarmRepository, IValidator<CreateAlarmCommand> validator, ILogger<CreateAlarmHandler> logger)
    {
        _alarmRepository = alarmRepository;
        _validator = validator;
        _logger = logger;
    }

    public async Task<AlarmResponse> Handle(CreateAlarmCommand request, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed: {@Errors}", validationResult.Errors);
            throw new ValidationException(validationResult.Errors);
        }

        var alarm = new Alarm
        {
            // ...atribuição dos campos do comando...
        };

        await _alarmRepository.AddAsync(alarm);
        _logger.LogInformation("Alarm created: {AlarmId}", alarm.Id);
        return new AlarmResponse(alarm);
    }
}
```

## 🤖 AnalysisService: IA e Análise Comportamental

Toda a lógica de IA e análise comportamental é implementada em C# usando ML.NET. Quando necessário, integrações com TensorFlow ou PyTorch podem ser feitas via bibliotecas .NET, mantendo sempre a lógica principal e dados sensíveis sob controle do backend C#.

- Modelos de recomendação, análise de padrões e sugestões contextuais são treinados e servidos via ML.NET.
- Testes unitários e de integração garantem a robustez dos modelos.

## 🔗 IntegrationService: Integrações Externas

Todas as integrações com APIs externas (calendários, notificações, feriados, etc.) são feitas via bibliotecas .NET, com autenticação OAuth2/OpenID Connect, logging e tratamento de erros padronizados (Polly, HttpClientFactory).

## 🛡️ Segurança, Testabilidade e Observabilidade

- Autenticação JWT/FIDO2, autorização baseada em claims e RBAC.
- Logging estruturado (Serilog), tracing distribuído (Application Insights), monitoramento e alertas.
- Testes automatizados (xUnit, Moq), cobertura mínima de 80% para código crítico.
- Documentação via Swagger/OpenAPI.

## 🧩 Padrões e Boas Práticas

- Clean Architecture e SOLID em todos os serviços.
- Validação rigorosa de entrada/saída.
- Tratamento de erros centralizado e respostas amigáveis.
- CI/CD automatizado (GitHub Actions/Azure DevOps), infraestrutura como código (Bicep/Terraform).

## Exemplo de Estrutura de Projeto

```
/AlarmService
  /Application
  /Domain
  /Infrastructure
  /Api
/AnalysisService
/IntegrationService
```

## Observações Finais

- Todo o backend é C#/.NET, sem dependências de Go, Python ou Node.js.
- Qualquer integração com Python para IA é encapsulada e nunca expõe dados sensíveis fora do ambiente .NET.
- O foco é sempre em testabilidade, segurança, acessibilidade e manutenção a longo prazo.