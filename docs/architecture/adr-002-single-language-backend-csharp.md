# ADR-002: Migração para Backend C# Único

**Status:** Aceito  
**Data:** 2025-07-02  
**Autores:** Equipe de Desenvolvimento  
**Decisores:** Product Owner, Tech Lead

## Contexto e Problema

Originalmente, nossa arquitetura previa múltiplas linguagens para o backend, mas a complexidade operacional, a dificuldade de manutenção e a necessidade de padronização motivaram a migração para uma stack única.

## Decisão

**Todo o backend será unificado em C# (.NET)**, eliminando qualquer dependência de Go, Python, Node.js ou outras linguagens para serviços core. Todos os serviços (CRUD de alarmes, IA/análise comportamental, integrações externas) serão implementados como projetos .NET independentes, preferencialmente serverless (Azure Functions), seguindo Clean Architecture e princípios SOLID.

## Justificativa

- **Padronização e Simplicidade:** Uma única linguagem reduz drasticamente a complexidade operacional, facilita onboarding, manutenção e evolução do sistema.
- **Performance e Produtividade:** .NET moderno (6+) oferece performance próxima a linguagens de baixo nível para operações CRUD, com excelente produtividade e ferramentas de desenvolvimento.
- **IA e Análise:** ML.NET cobre a maioria dos cenários de machine learning necessários. Integrações com TensorFlow/PyTorch podem ser feitas via bibliotecas .NET, e Python.NET só será usado em casos excepcionais, sempre encapsulado.
- **Integrações Externas:** O ecossistema .NET possui bibliotecas maduras para integrações com APIs de terceiros, notificações, calendários, etc.
- **Serverless e Cloud-Native:** Azure Functions suporta C# nativamente, com cold start competitivo e integração facilitada com serviços Azure.
- **Segurança e Testabilidade:** Clean Architecture, SOLID, testes automatizados, logging estruturado, autenticação JWT/FIDO2, documentação via Swagger/OpenAPI.

## Implicações

### Positivas

- Redução de custos operacionais e overhead de DevOps.
- Base de código uniforme, fácil de auditar e evoluir.
- Onboarding mais rápido e curva de aprendizado menor.
- Ferramentas de análise, profiling e monitoramento padronizadas.
- Performance adequada para todos os cenários do produto.

### Negativas

- Custo de migração inicial dos serviços legados.
- Eventual necessidade de integração com bibliotecas Python para IA muito específica (mitigado por Python.NET encapsulado).

### Arquitetura Técnica

- **AlarmService:** CRUD de alarmes, regras de negócio e notificações, tudo em C#.
- **AnalysisService:** IA/análise comportamental com ML.NET, interoperabilidade Python apenas se necessário.
- **IntegrationService:** Integrações externas (calendários, notificações, etc.) via bibliotecas .NET.
- Todos os serviços como projetos .NET independentes, preferencialmente Azure Functions.

## Plano de Implementação

1. Setup da infraestrutura C# e implementação do AlarmService.
2. Implementação do IntegrationService e migração das integrações externas.
3. Implementação do AnalysisService com ML.NET e interoperabilidade Python apenas se necessário.
4. Testes de integração, performance e validação completa.

Durante todo o processo, manter os serviços legados apenas até a validação completa dos novos serviços em C#.

## Critérios de Sucesso

- Latência equivalente ou melhor para 95% das operações CRUD.
- 99.9% de uptime para todos os serviços.
- Redução de 30% no tempo para implementar novos recursos.
- Redução de 50% no tempo gasto em manutenção de infraestrutura.

---

*Esta ADR será revisada após a implementação completa ou se novas informações significativas surgirem durante o processo de migração.*
