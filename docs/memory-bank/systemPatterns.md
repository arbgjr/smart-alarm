# Smart Alarm — System Patterns

## Arquitetura
- Clean Architecture: separação clara entre Domain, Application, Infrastructure, Api
- SOLID aplicado em todos os serviços
- Modularidade: cada serviço é um projeto .NET independente

## Decisões Técnicas
- .NET 8.0 como padrão
- Serverless (OCI Functions) para backend
- Validação com FluentValidation
- Logging estruturado (Serilog), tracing (Application Insights)
- Testes: xUnit, Moq, AAA pattern

## Padrões de Design
- DTOs para entrada/saída
- Repositórios para persistência
- Handlers para comandos/queries (CQRS)
- Externalização de integrações via HttpClientFactory e Polly