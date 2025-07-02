# Alarm Service - Smart Alarm

O Alarm Service é responsável por todas as operações de alarmes, regras de negócio e notificações, sendo implementado exclusivamente em C#/.NET, seguindo Clean Architecture, SOLID e práticas modernas de testabilidade e segurança.

## Tecnologias e Padrões

- **C# (.NET 6+):** Toda a lógica de alarmes
- **Entity Framework Core:** Persistência de dados
- **Azure Functions:** Deploy serverless
- **FluentValidation:** Validação de entrada/saída
- **Logging estruturado:** Serilog e Application Insights
- **Testes automatizados:** xUnit, Moq

## Observações

- Não há dependências de Go, Python ou Node.js
- Todo o backend é testável, seguro e observável
