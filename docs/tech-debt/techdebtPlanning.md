# Planejamento para resolver débitos técnicos existentes

Verificar mais detalhes em [activeContext.md](/memory-bank/activeContext.md)

## 1. Integrações Reais com SDKs (OCI, Azure, AWS)

**Pendências:**

- Storage (OciObjectStorageService.cs)
- Messaging (`OciStreamingMessagingService.cs`)
- KeyVault (OciVaultProvider.cs, AzureKeyVaultProvider.cs, AwsSecretsManagerProvider.cs)

**Ações:**

- Levantar requisitos e credenciais de cada provedor.
- Implementar métodos de integração real com os SDKs oficiais (OCI, Azure, AWS) para cada serviço.
- Garantir tratamento de erros, logging estruturado e testes de integração.
- Atualizar documentação técnica e exemplos de uso.

**Responsável:** Backend/Infra
**Prioridade:** Alta
**Estimativa:** 10 dias úteis

---

## 2. Refinar Handler de Rotinas

**Pendência:**

- TODO em ListRoutinesHandler.cs para buscar todas as rotinas quando não houver AlarmId.

**Ações:**

- Implementar lógica para retornar todas as rotinas do usuário quando AlarmId não for informado.
- Adicionar testes unitários cobrindo todos os cenários (com e sem AlarmId).
- Revisar documentação do endpoint.

**Responsável:** Backend
**Prioridade:** Média
**Estimativa:** 2 dias úteis

---

## 3. Autenticação Real

**Pendência:**

- Mock de autenticação em AuthController.cs.

**Ações:**

- Integrar autenticação real via JWT/FIDO2, removendo lógica hardcoded.
- Configurar provider de identidade (ex: IdentityServer, Azure AD, OCI IAM).
- Adicionar testes de autenticação (sucesso, falha, edge cases).
- Atualizar documentação de segurança e exemplos de login.

**Responsável:** Backend/Security
**Prioridade:** Alta
**Estimativa:** 4 dias úteis

---

## 4. Testes Automatizados e Cobertura

**Pendência:**

- Estrutura de testes existe, mas não há evidência clara de cobertura mínima de 80% nem exemplos AAA pattern.

**Ações:**

- Mapear áreas críticas sem cobertura.
- Implementar testes unitários e de integração seguindo AAA pattern.
- Configurar análise de cobertura no pipeline CI/CD.
- Garantir mínimo de 80% de cobertura para código crítico.

**Responsável:** Backend/QA
**Prioridade:** Alta
**Estimativa:** 5 dias úteis

---

## 5. Observabilidade Real em Produção

**Pendência:**

- MockTracingService e MockMetricsService usados em dev/teste; integração real só ocorre em produção.

**Ações:**

- Implementar e validar integração real com OpenTelemetry, Prometheus e Serilog nos ambientes de produção.
- Instrumentar handlers críticos conforme padrão.
- Adicionar dashboards e alertas.
- Documentar exemplos reais de instrumentação.

**Responsável:** Backend/DevOps
**Prioridade:** Média
**Estimativa:** 3 dias úteis

---

## 6. Revisar e Validar Migrations

**Pendência:**

- Migrations do EF Core presentes, mas não foi verificado se refletem 100% o modelo de domínio.

**Ações:**

- Validar se as migrations estão sincronizadas com o modelo de domínio atual.
- Corrigir eventuais divergências.
- Adicionar testes de integração para migrações.

**Responsável:** Backend/DBA
**Prioridade:** Média
**Estimativa:** 2 dias úteis

---

## 7. Atualizar Documentação

**Pendência:**

- Garantir que a documentação reflita o status real após as correções.

**Ações:**

- Atualizar READMEs, ADRs e Memory Bank após cada entrega.
- Incluir exemplos reais de uso, fluxos de autenticação, integração e observabilidade.

**Responsável:** Todos os times
**Prioridade:** Contínua

---

### **Ordem Recomendada de Execução**

1. Integrações reais (Storage, Messaging, KeyVault)
2. Autenticação real
3. Refinamento do handler de rotinas
4. Testes e cobertura
5. Observabilidade real
6. Revisão de migrations
7. Atualização da documentação
