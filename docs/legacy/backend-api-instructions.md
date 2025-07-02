# Instruções Legadas de Backend - Atualização para C#/.NET

> Este documento foi atualizado para refletir a nova arquitetura unificada do backend em C#/.NET. Todas as instruções anteriores relacionadas a Go, Python, Node.js ou multi-linguagem foram removidas.

## Estrutura do Backend

- Todos os serviços backend são implementados em C# (.NET 6+), organizados em projetos independentes (AlarmService, AnalysisService, IntegrationService).
- Utilização de Clean Architecture, SOLID, testes automatizados e integração nativa com Azure Functions.

## Padrões de API

- APIs RESTful documentadas via Swagger/OpenAPI.
- Autenticação JWT/FIDO2, autorização baseada em claims e RBAC.
- Validação rigorosa de entrada/saída (FluentValidation).
- Logging estruturado (Serilog) e tracing (Application Insights).

## Integrações e IA

- Integrações externas (calendários, notificações, etc.) via HttpClientFactory, Polly e autenticação OAuth2/OpenID Connect.
- IA e análise comportamental implementadas com ML.NET. Integração com Python apenas via bibliotecas .NET, nunca expondo dados sensíveis fora do ambiente C#.

## Testes e Segurança

- Testes unitários e de integração (xUnit, Moq), cobertura mínima de 80% para código crítico.
- CI/CD automatizado (GitHub Actions/Azure DevOps), infraestrutura como código (Bicep/Terraform).
- Monitoramento e alertas via Application Insights.

## Observações

- Todo o backend é C#/.NET, sem dependências de Go, Python ou Node.js.
- Para dúvidas sobre migração de código legado, consulte a equipe de arquitetura.