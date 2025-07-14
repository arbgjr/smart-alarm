# Holiday API Integration Tests

Este diretório contém testes de integração abrangentes para a Holiday API do SmartAlarm, seguindo RFC 9110 HTTP Semantics.

## 📁 Estrutura de Arquivos

```
tests/
├── http/
│   ├── holidays.http              # Testes principais da Holiday API
│   └── holidays-advanced.http     # Testes avançados e edge cases
├── run-holiday-tests.sh           # Runner para testes HTTP (.http files)
├── run-holiday-dotnet-tests.sh    # Runner para testes dotnet específicos
├── SmartAlarm-test.sh             # Script principal (com suporte holiday)
└── README-holiday-tests.md        # Esta documentação
```

## 🚀 Execução Rápida

### Opção 1: Via Script Principal
```bash
# Executar todos os testes Holiday (HTTP + dotnet)
./tests/SmartAlarm-test.sh holiday

# Com saída detalhada
./tests/SmartAlarm-test.sh holiday -v
```

### Opção 2: Scripts Específicos
```bash
# Apenas testes HTTP (.http files)
./tests/run-holiday-tests.sh

# Apenas testes dotnet
./tests/run-holiday-dotnet-tests.sh

# Testes dotnet com cobertura
./tests/run-holiday-dotnet-tests.sh coverage
```

## 📋 Tipos de Teste

### 1. Testes HTTP (.http files)
Localização: `tests/http/holidays.http` e `tests/http/holidays-advanced.http`

**Cobertura:**
- ✅ Autenticação e autorização
- ✅ CRUD completo (Create, Read, Update, Delete)
- ✅ Validação de dados
- ✅ Códigos de status HTTP corretos
- ✅ Headers e Content-Type
- ✅ Tratamento de erros (400, 401, 403, 404)
- ✅ Testes de segurança (XSS, SQL Injection)
- ✅ CORS e OPTIONS
- ✅ Rate limiting
- ✅ Timezone handling
- ✅ Unicode e caracteres especiais
- ✅ Boundary values (min/max dates)

**Características RFC 9110:**
- Uso correto de métodos HTTP (GET, POST, PUT, DELETE, OPTIONS)
- Headers apropriados (Content-Type, Authorization, Accept)
- Status codes semânticos
- Content negotiation
- Idempotência respeitada

### 2. Testes dotnet (C#)
Localização: Projetos de teste existentes com filtro `FullyQualifiedName~Holiday`

**Cobertura:**
- ✅ Unit tests para Handlers
- ✅ Unit tests para Validators
- ✅ Integration tests para Controllers
- ✅ Repository tests
- ✅ DTO mapping tests

## 🔧 Pré-requisitos

### Para Testes HTTP
1. **SmartAlarm API rodando:**
   ```bash
   # Via Docker
   docker-compose up -d
   dotnet run --project src/SmartAlarm.Api
   
   # OU via dotnet direto
   dotnet run --project src/SmartAlarm.Api --urls https://localhost:5001
   ```

2. **Ferramentas opcionais:**
   - VS Code + REST Client extension (humao.rest-client)
   - curl (para testes básicos automáticos)

### Para Testes dotnet
1. **.NET SDK 8.0+**
2. **Dependências restauradas:**
   ```bash
   dotnet restore SmartAlarm.sln
   ```

## 📊 Execução Manual dos Arquivos .http

### Via VS Code (Recomendado)
1. Instalar extensão **REST Client** (humao.rest-client)
2. Abrir arquivo: `code tests/http/holidays.http`
3. Clicar em "Send Request" em cada endpoint
4. Verificar responses e status codes

### Via curl (Exemplo básico)
```bash
# Health check
curl -k https://localhost:5001/api/v1/health

# Get holidays (sem auth - deve retornar 401)
curl -k https://localhost:5001/api/v1/holidays

# Login e obter token
TOKEN=$(curl -k -X POST https://localhost:5001/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@smartalarm.com","password":"Admin123!"}' \
  | jq -r '.token')

# Get holidays com autenticação
curl -k -H "Authorization: Bearer $TOKEN" \
  https://localhost:5001/api/v1/holidays
```

## 🎯 Cenários de Teste Específicos

### Testes de Autenticação
- ❌ Acesso sem token (401)
- ❌ Token inválido (401) 
- ❌ Usuário sem role admin (403)
- ✅ Admin autenticado

### Testes CRUD
- ✅ CREATE: Criar holiday válido
- ✅ READ: Buscar por ID, listar todos, buscar por data
- ✅ UPDATE: Atualizar descrição
- ✅ DELETE: Remover holiday

### Testes de Validação
- ❌ Descrição vazia
- ❌ Data inválida
- ❌ Campos obrigatórios ausentes
- ❌ Formato JSON inválido

### Testes de Segurança
- 🛡️ XSS protection
- 🛡️ SQL Injection protection
- 🛡️ CORS headers
- 🛡️ Content-Type validation
- 🛡️ Request size limits

### Testes de Edge Cases
- 📅 Datas limite (min: 0001-01-01, max: 9999-12-31)
- 📅 Ano bissexto (29 de fevereiro)
- 🌍 Fusos horários diferentes
- 🔤 Unicode e caracteres especiais
- 📏 Descrições muito longas (255 chars)

## 📈 Análise de Cobertura

```bash
# Gerar relatório de cobertura para Holiday
./tests/run-holiday-dotnet-tests.sh coverage

# Localização do relatório
./tests/coverage-report/holiday-coverage/
```

## 🐛 Depuração

### Verificar logs da API
```bash
# Se usando Docker
docker-compose logs smartalarm-api

# Se rodando direto
# Verificar console do dotnet run
```

### Verificar conectividade
```bash
# Teste básico de conectividade
./tests/run-holiday-tests.sh

# Debug detalhado
./tests/SmartAlarm-test.sh debug
```

### Executar categoria específica
```bash
# Apenas testes unitários Holiday
./tests/run-holiday-dotnet-tests.sh unit

# Apenas testes de integração Holiday
./tests/run-holiday-dotnet-tests.sh integration

# Apenas testes de API/Controller
./tests/run-holiday-dotnet-tests.sh api
```

## 🔄 Integração Contínua

### GitHub Actions (exemplo)
```yaml
- name: Run Holiday Integration Tests
  run: |
    ./tests/SmartAlarm-test.sh holiday
```

### Docker Integration
```bash
# Em ambiente containerizado
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --exit-code-from tests holiday-tests
```

## 📝 Estrutura dos Arquivos .http

### holidays.http
- **Seção 1-5**: Setup e autenticação
- **Seção 6-10**: CRUD básico
- **Seção 11-15**: Tratamento de erros
- **Seção 16-20**: Testes de segurança
- **Cleanup**: Limpeza dos dados de teste

### holidays-advanced.http
- **Boundary testing**: Valores limite
- **Unicode testing**: Caracteres especiais
- **Performance testing**: Bulk operations
- **Concurrency testing**: Updates simultâneos
- **Protocol testing**: HTTP specifics

## 🆘 Troubleshooting

### API não responde
```bash
# Verificar se está rodando
curl -k https://localhost:5001/api/v1/health

# Verificar logs
docker-compose logs smartalarm-api
```

### Testes falhando
```bash
# Verificar build
dotnet build SmartAlarm.sln

# Executar com mais detalhes
./tests/SmartAlarm-test.sh holiday -v
```

### Problemas de certificado SSL
```bash
# Usar -k no curl para ignorar certificados
# OU configurar certificado de desenvolvimento
dotnet dev-certs https --trust
```

## 📚 Referências

- [RFC 9110 - HTTP Semantics](https://tools.ietf.org/html/rfc9110)
- [REST Client Extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)
- [ASP.NET Core Testing](https://docs.microsoft.com/en-us/aspnet/core/test/)
- [SmartAlarm API Documentation](../docs/api/)

---

**Nota**: Estes testes são executados automaticamente como parte da pipeline de CI/CD e devem ser executados localmente antes de fazer commits que afetam a Holiday API.
