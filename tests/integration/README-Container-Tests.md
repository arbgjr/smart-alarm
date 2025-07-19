# 🧪 Guia de Execução de Testes - Smart Alarm

## 📋 Tipos de Testes Disponíveis

### **1. Testes Básicos (SEM containers)** ✅
- ✅ **BasicOwaspSecurityTests.cs** - 10 testes de segurança OWASP  
- ✅ **BasicSecurityComponentsTests.cs** - 10 testes unitários com mocks
- ✅ **Taxa de sucesso: 100%** (20/20 testes passando)

### **2. Testes de Integração (COM containers)** 🐳
- 🔄 **BasicJwtFido2Tests.cs** - Testa endpoints reais da API
- ❗ **Requer containers:** PostgreSQL, Vault, RabbitMQ, MinIO

---

## 🚀 Como Executar

### **Opção 1: Apenas Testes Básicos (Recomendado)**
```powershell
# Executa testes que NÃO precisam de containers
.\tests\integration\run-basic-tests.ps1
```

### **Opção 2: Testes Completos com Containers**
```powershell
# 1. Inicia containers necessários
.\tests\integration\start-integration-tests.ps1

# 2. Executa todos os testes (incluindo integração)
dotnet test tests/integration/ --logger "console;verbosity=detailed"

# 3. Para os containers após os testes
docker-compose down
```

### **Opção 3: Comando Manual Direto**
```powershell
# Apenas testes específicos
dotnet test tests/integration/BasicOwaspSecurityTests.cs --logger "console;verbosity=detailed"
dotnet test tests/integration/BasicSecurityComponentsTests.cs --logger "console;verbosity=detailed"
```

---

## 🐳 Containers Necessários (apenas para integração)

| Container | Porta | Função | Status |
|-----------|-------|--------|---------|
| **vault** | 8200 | Gerenciamento de secrets | ✅ Configurado |
| **postgres** | 5432 | Banco de dados principal | ✅ Configurado |
| **rabbitmq** | 5672/15672 | Sistema de mensageria | ✅ Configurado |
| **minio** | 9000/9001 | Armazenamento de objetos | ✅ Configurado |

---

## 📊 Cobertura de Testes

### **Implementado ✅**
- ✅ **OWASP Top 10**: SQL Injection, XSS, Broken Access Control, etc.
- ✅ **Segurança Criptográfica**: BCrypt, JWT, GUID validation
- ✅ **Validação de Inputs**: SQL injection prevention, XSS protection
- ✅ **Autenticação**: JWT token structure, password hashing
- ✅ **Autorização**: Access control, secure headers

### **Análise de Falhas** 🔍
```
BasicJwtFido2Tests.cs (Integração):
❌ Falha: IJwtTokenService não registrado no DI
✅ Solução: Usar containers OU criar mocks adicionais
```

---

## 🎯 Recomendações

### **Para Desenvolvimento Rápido** ⚡
Use apenas testes básicos - são **rápidos, confiáveis e cobrem aspectos críticos**.

### **Para CI/CD Pipeline** 🔄  
Configure containers no pipeline para testes completos.

### **Para Validação Local** 🏠
Use **run-basic-tests.ps1** para validação rápida antes de commits.

---

## 🔧 Troubleshooting

### **"Docker não está rodando"**
```bash
# Windows
Inicie o Docker Desktop

# Verificar
docker info
```

### **"Containers não iniciam"**
```bash
# Limpar ambiente
docker-compose down -v
docker system prune -f

# Reiniciar
docker-compose up -d vault postgres rabbitmq minio
```

### **"Testes de integração falham"**
```bash
# Verificar se aplicação tem todas as dependências
dotnet build

# Verificar containers
docker-compose ps

# Executar apenas testes básicos
.\tests\integration\run-basic-tests.ps1
```

---

## 📈 Status Atual

| Categoria | Status | Detalhes |
|-----------|--------|----------|
| **Testes Unitários** | ✅ 100% | 10/10 componentes de segurança |
| **Testes OWASP** | ✅ 100% | 10/10 validações de segurança |
| **Testes Integração** | 🔄 Parcial | Precisa de IJwtTokenService |
| **Infraestrutura** | ✅ Pronta | Containers configurados |
| **Automação** | ✅ Pronta | Scripts PS1 disponíveis |
