# Inteligência Artificial no Smart Alarm

Toda a lógica de IA e análise comportamental do Smart Alarm é implementada em C# utilizando ML.NET. O backend não depende de Python, Go ou Node.js para processamento de IA, garantindo segurança, performance e integração nativa com o restante da stack .NET.

## Tecnologias Utilizadas

- **ML.NET:** Treinamento, validação e inferência de modelos de machine learning em C#
- **Azure Functions:** Deploy serverless dos serviços de IA
- **Application Insights:** Monitoramento e logging dos modelos em produção

## Princípios

- Testabilidade e reprodutibilidade dos modelos
- Segurança e privacidade dos dados
- Observabilidade e monitoramento contínuo

## Observações

- Integrações com Python para IA só são permitidas via bibliotecas .NET, nunca expondo dados sensíveis fora do ambiente C#.
