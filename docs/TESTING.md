# 🧪 Smart Alarm - Testing Documentation

## 📋 Overview

Este documento fornece informações completas sobre a estratégia de testes, configuração, execução e geração de relatórios para o Smart Alarm System.

## 🏗️ Arquitetura de Testes (TDD Compliance)

### Hierarquia de Testes

```
📁 tests/
├── 🔬 Unit Tests          # Testes unitários puros (Domain only)
├── 🔗 Integration Tests   # Testes com infraestrutura (DB, Redis, etc.)
├── 🌐 API Tests           # Controllers e endpoints HTTP
├── 🛡️ Security Tests      # Validações OWASP e JWT/FIDO2
└── 🎭 E2E Tests           # Frontend Playwright
```

### Regras TDD (Test-Driven Development)

| **Categoria** | **Dependências Permitidas** | **Localização** | **Marcação** |
|---------------|------------------------------|-----------------|--------------|
| **Unit** | ✅ Nenhuma (Domain puro) | `Domain/Entities`, `Domain/ValueObjects` | Nenhuma |
| **Integration** | ✅ PostgreSQL, Redis, MinIO, etc. | `Infrastructure/`, `Api/` | `[Trait("Category", "Integration")]` |
| **API** | ✅ HTTP, DI Container, Middleware | `Api/Controllers` | `[Trait("Category", "Integration")]` |
| **Security** | ✅ Certificates, JWT, Headers | `Security/`, `Auth/` | `[Trait("Category", "Security")]` |
| **E2E** | ✅ Browser, Full Environment | `frontend/tests/e2e/` | Playwright |

> **⚠️ Regra Fundamental**: Testes unitários NUNCA podem testar contra serviços externos (bancos de dados, APIs, file system). Isso vai contra os princípios do TDD.

## 🚀 Scripts de Automação

### Script Principal: `scripts/test-reports.sh`

```bash
# Testes unitários puros (123 testes passando 100%)
bash scripts/test-reports.sh unit

# Testes de integração com PostgreSQL ativo
bash scripts/test-reports.sh integration

# Testes de API/Controllers
bash scripts/test-reports.sh api

# Testes de segurança OWASP
bash scripts/test-reports.sh security

# Testes E2E (Playwright)
bash scripts/test-reports.sh e2e

# Todos os testes
bash scripts/test-reports.sh all

# Foco em cobertura (sem integration)
bash scripts/test-reports.sh coverage
```

### Funcionalidades do Script

- ✅ **Verificação automática de infraestrutura** (PostgreSQL, Redis)
- ✅ **Iniciação automática de containers** quando necessário
- ✅ **Múltiplos formatos de relatório** (HTML, TRX, Cobertura)
- ✅ **Instalação automática do ReportGenerator** para HTML
- ✅ **Output colorido** com status de cada categoria

## 📊 Relatórios Automáticos

### Estrutura de Saída

```
TestResults/
├── Unit/
│   ├── TestResults.html          # Relatório visual HTML
│   ├── TestResults.trx           # Formato Microsoft TRX
│   ├── coverage.cobertura.xml    # Cobertura XML
│   └── coverage-report/
│       └── index.html           # Relatório visual de cobertura
├── Integration/
│   └── [mesmo padrão]
├── API/
│   └── [mesmo padrão]
└── Security/
    └── [mesmo padrão]
```

### Configuração em `tests/coverlet.runsettings`

```xml
<Format>cobertura,lcov,json,html</Format>
<Exclude>[*.Tests]*,[*.Mocks]*,[*]*.Program,[*]*Exception</Exclude>
<Include>[SmartAlarm.*]*</Include>
<LoggerRunSettings>
  <!-- HTML + TRX + Console com verbosidade detalhada -->
</LoggerRunSettings>
```

## 🎯 Resultados Atuais (Evidenciados)

### ✅ Testes Unitários Puros (TDD Compliant)

```bash
bash scripts/test-reports.sh unit
```

**Resultado**: ✅ **123 testes passando (100%)**
- 🔬 **Domain.Entities**: 78 testes
- 🔬 **Domain.ValueObjects**: 45 testes  
- ⏱️ **Duração**: ~15 segundos
- 📊 **Cobertura**: Domain layer completa

### ⚠️ Testes de Integração (Requer Ambiente)

```bash
bash scripts/test-reports.sh integration
```

**Resultado**: ❌ **21 testes falhando (configuração)**
- **Causa**: Connection strings e ambiente não configurado
- **Solução**: Configurar variáveis de ambiente para PostgreSQL

### 🔧 Testes que Precisam de Recategorização

**32 testes atualmente falhando precisam ser movidos para categoria Integration:**

```csharp
// ❌ Atualmente sem categoria (falham)
SmartAlarm.Tests.Api.AlarmControllerTests.CreateAlarm_ShouldReturn201
SmartAlarm.Tests.Security.OwaspSecurityTests.OWASP_A01_Should_ValidateResourceOwnership
SmartAlarm.Tests.Integration.JwtFido2IntegrationTests.CombinedFlow_Should_AuthenticateWithFido2_ThenAccessWithJWT

// ✅ Devem ter categoria Integration
[Trait("Category", "Integration")]
public class AlarmControllerTests { ... }
```

## 🏗️ Pré-requisitos de Ambiente

### Para Testes Unitários (Unit)
- ✅ Nenhum (executam standalone)

### Para Testes de Integração
```bash
# Iniciar infraestrutura completa
docker compose -f docker-compose.full.yml up -d

# Verificar status
docker compose -f docker-compose.full.yml ps
```

**Serviços Requeridos:**
- 🐘 **PostgreSQL**: `localhost:5432` (primary) + `localhost:5433` (replica)
- 🟥 **Redis**: `localhost:6379` (master) + `localhost:26379` (sentinel)
- 🐰 **RabbitMQ**: `localhost:5672` (AMQP) + `localhost:15672` (management)
- 💾 **MinIO**: `localhost:9000` (API) + `localhost:9001` (console)
- 🔒 **HashiCorp Vault**: `localhost:8200`

### Para Testes E2E
```bash
cd frontend
npm install
npm run test:e2e
```

## 🔍 Diagnóstico e Troubleshooting

### Verificar Status da Infraestrutura

```bash
# Status dos containers
docker compose -f docker-compose.full.yml ps

# Health checks
curl http://localhost:5432  # PostgreSQL
curl http://localhost:6379  # Redis
curl http://localhost:9000  # MinIO
curl http://localhost:8200  # Vault
```

### Logs de Debugging

```bash
# Ver logs dos serviços
docker compose -f docker-compose.full.yml logs postgres-primary
docker compose -f docker-compose.full.yml logs redis-master

# Testar conectividade
docker exec smartalarm-postgres-primary pg_isready
docker exec smartalarm-redis-master redis-cli ping
```

### Problemas Comuns

| **Problema** | **Causa** | **Solução** |
|--------------|-----------|-------------|
| "Connection refused" | PostgreSQL não iniciado | `docker compose -f docker-compose.full.yml up -d postgres-primary` |
| "Redis timeout" | Redis não acessível | Verificar `localhost:6379` e firewall |
| "Assembly not found" | Build desatualizado | `dotnet build SmartAlarm.sln --no-restore` |
| "Test discovery failed" | Namespace issues | Verificar using statements e referencias |

## 📈 Métricas e KPIs de Qualidade

### Targets de Qualidade

- 🎯 **Unit Tests**: ≥95% cobertura Domain layer
- 🎯 **Integration Tests**: ≥80% pass rate com infraestrutura
- 🎯 **API Tests**: ≥90% endpoints cobertos
- 🎯 **Security Tests**: 100% OWASP validations passing
- 🎯 **E2E Tests**: ≥85% critical user journeys

### Atual vs Target

| **Categoria** | **Atual** | **Target** | **Status** |
|---------------|-----------|------------|------------|
| Unit Tests | ✅ **100%** (123/123) | 95% | ✅ **SUPERADO** |
| Integration Tests | ⚠️ **0%** (0/21) | 80% | ❌ **REQUER SETUP** |
| API Tests | ⚠️ **TBD** | 90% | 🔄 **PENDENTE** |
| Security Tests | ⚠️ **TBD** | 100% | 🔄 **PENDENTE** |
| E2E Tests | ⚠️ **TBD** | 85% | 🔄 **PENDENTE** |

## 🔄 Workflow de CI/CD (Futuro)

### Pipeline Recomendado

```yaml
# .github/workflows/tests.yml
stages:
  - unit-tests:        # Sempre executar (rápido)
  - integration-tests: # Com containers
  - security-tests:    # Com certificados  
  - e2e-tests:        # Com ambiente completo
  - coverage-report:   # Consolidar métricas
```

### Comandos para CI

```bash
# Unit (sempre primeiro - rápido)
bash scripts/test-reports.sh unit

# Integration (com retry se falhar)
bash scripts/test-reports.sh integration || retry_with_setup

# Security (com certificates)
bash scripts/test-reports.sh security

# E2E (com timeout longo)
timeout 300 bash scripts/test-reports.sh e2e
```

## 📚 Referências e Links Úteis

### Documentação Relacionada
- 📋 [RELATORIO_TESTE_EVIDENCIAS.md](../RELATORIO_TESTE_EVIDENCIAS.md) - Evidências completas de execução
- 🏗️ [CLAUDE.md](../CLAUDE.md) - Regras TDD e comandos de desenvolvimento
- 🚀 [DEPLOYMENT.md](./DEPLOYMENT.md) - Setup de infraestrutura

### Scripts e Ferramentas
- 🔧 [`scripts/test-reports.sh`](../scripts/test-reports.sh) - Script principal de testes
- ⚙️ [`tests/coverlet.runsettings`](../tests/coverlet.runsettings) - Configuração de cobertura
- 🐳 [`docker-compose.full.yml`](../docker-compose.full.yml) - Infraestrutura completa

### Comandos de Referência Rápida

```bash
# Comandos essenciais (copy-paste ready)
bash scripts/test-reports.sh unit         # Unit tests (sempre funcionam)
bash scripts/test-reports.sh integration  # Integration (requer setup)
docker compose -f docker-compose.full.yml up -d  # Iniciar infraestrutura
docker compose -f docker-compose.full.yml ps     # Verificar status
```

---

**📋 Resumo**: Este documento serve como referência completa para execução, configuração e troubleshooting de todos os tipos de testes no Smart Alarm System, garantindo compliance com TDD e geração automática de relatórios detalhados.