# Integration Service - Smart Alarm

O Integration Service é responsável por todas as integrações externas do Smart Alarm, sendo implementado exclusivamente em C#/.NET, seguindo Clean Architecture e práticas modernas de segurança e testabilidade.

## Tecnologias e Padrões

- **C# (.NET 6+):** Toda a lógica de integração
- **HttpClientFactory & Polly:** Consumo resiliente de APIs externas
- **OAuth2/OpenID Connect:** Autenticação e autorização seguras
- **Logging estruturado:** Serilog e Application Insights
- **Testes automatizados:** xUnit, Moq

## Observações

- Não há dependências de Go, Python ou Node.js
- Todas as integrações são auditáveis, seguras e testáveis
