# PENDÊNCIAS PARA HOMOLOGAÇÃO LOCAL 100%

**Status Atual:** API compila e roda, infraestrutura UP, migrations aplicadas ✅
**Para 100%:** Corrigir itens abaixo para ambiente completo de homologação

## 🚨 CRÍTICO - Para API funcionar completamente

### 1. DatabaseHealthCheck Configuration
- **Problema:** `Unable to resolve service for type 'System.String'` no DatabaseHealthCheck
- **Localização:** `src/SmartAlarm.Observability/HealthChecks/DatabaseHealthCheck.cs:49`
- **Solução:** Registrar connection string parameter no DI ou ajustar construtor
- **Impacto:** Health check `/health` falha, mas API funciona normalmente

## 🧪 TESTES - Para validação completa

### 2. Categorização Incorreta de Testes
- **Problema:** Testes "unit" estão usando PostgreSQL, MinIO, APIs externas
- **Arquivos:** `tests/SmartAlarm.Application.Tests/Handlers/AlarmHandlerIntegrationTests.cs`
- **Solução:** Recategorizar como `[Category("Integration")]` e usar mocks em unit tests
- **Regra:** Unit tests = apenas mocks/stubs, Integration tests = infraestrutura real

### 3. Conflitos de Interface IFileParser
- **Problema:** Duas interfaces IFileParser (Application vs Infrastructure)
- **Arquivos:** 
  - `src/SmartAlarm.Application/Services/IFileParser.cs`
  - `src/SmartAlarm.Infrastructure/Services/IFileParser.cs`
- **Solução:** Remover uma das interfaces ou resolver conflito de namespaces
- **Workaround:** Implementação temporária `TemporaryFileParser` em uso

### 4. Erros de Compilação em Testes
- **Problema:** Métodos override não encontrados, referências quebradas
- **Arquivos:** `tests/SmartAlarm.Application.Tests/`
- **Solução:** Corrigir assinaturas de métodos e referências

## 🌐 FRONTEND & E2E

### 5. Validação do Frontend PWA
- **Pendente:** Testar se React PWA compila e roda
- **Comando:** `cd frontend && npm run dev`
- **Validar:** Service worker, offline sync, push notifications

### 6. Pipeline Completa de Testes
- **Pendente:** Rodar todos os tipos de teste com sucesso
- **Comandos:**
  - `bash scripts/test-reports.sh unit` (deve passar 100%)
  - `bash scripts/test-reports.sh integration` (com infraestrutura UP)
  - `bash scripts/test-reports.sh api` (testes de controller)
  - `bash scripts/test-reports.sh all` (pipeline completa)

### 7. Ambiente E2E Completo
- **Pendente:** Validar stack completa funcionando
- **Inclui:** API + Frontend + Banco + Cache + Storage + Messaging
- **Teste:** Criar alarme via frontend → Persistir no banco → Processar jobs

## 📋 ROADMAP PARA 100%

### Fase 1: API Estável (30min)
1. Corrigir DatabaseHealthCheck DI
2. Validar endpoints básicos funcionando

### Fase 2: Testes Limpos (45min)  
1. Recategorizar testes unit vs integration
2. Corrigir erros de compilação
3. Pipeline de testes verde

### Fase 3: Stack Completa (30min)
1. Frontend PWA funcionando
2. E2E environment validado
3. Homologação local 100%

## 🎯 CRITÉRIO DE SUCESSO

**100% Funcionando quando:**
- ✅ API roda sem erros (incluindo health checks)
- ✅ Todos os testes passam (unit, integration, API)
- ✅ Frontend PWA carrega e funciona
- ✅ E2E: Criar alarme no frontend persiste no banco
- ✅ Infraestrutura completa estável

**Estado Atual:** ~80% completo - infraestrutura e core funcionando, faltam ajustes finais