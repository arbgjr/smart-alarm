# AI Service - Smart Alarm

O AI Service é responsável por toda a lógica de inteligência artificial e análise comportamental do Smart Alarm, sendo implementado exclusivamente em C#/.NET, utilizando ML.NET e seguindo Clean Architecture, SOLID e práticas modernas de testabilidade e segurança.

## Tecnologias e Padrões

- **C# (.NET 6+):** Toda a lógica de IA
- **ML.NET:** Treinamento e inferência de modelos
- **Azure Functions:** Deploy serverless
- **Logging estruturado:** Serilog e Application Insights
- **Testes automatizados:** xUnit, Moq

## Observações

- Não há dependências de Go, Python ou Node.js
- Integrações com Python para IA só são permitidas via bibliotecas .NET, nunca expondo dados sensíveis fora do ambiente C#
- Todo o backend é testável, seguro e observável
