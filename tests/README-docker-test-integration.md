# 🧪 Docker Integration Tests - Smart Alarm

## 📋 Visão Geral

O script `docker-test.sh` é o ponto central para execução de **todos os testes de integração** do Smart Alarm. Ele combina as funcionalidades originais de rede e containers com os novos testes de segurança e autenticação.

## 🚀 Como Usar

### **Sintaxe**
```bash
./tests/docker-test.sh [opção] [-v|--verbose]
```

### **Opções Principais**

#### **🧪 Testes Básicos (SEM containers)**
```bash
./tests/docker-test.sh basic       # Todos os testes básicos
./tests/docker-test.sh owasp       # Testes OWASP Top 10  
./tests/docker-test.sh security    # Testes de componentes
./tests/docker-test.sh all-security# Todos os de segurança
```

#### **🐳 Testes de Integração (COM containers)**
```bash
./tests/docker-test.sh postgres    # Testes PostgreSQL
./tests/docker-test.sh vault       # Testes HashiCorp Vault
./tests/docker-test.sh minio       # Testes MinIO
./tests/docker-test.sh rabbitmq    # Testes RabbitMQ
./tests/docker-test.sh jwt-fido2   # Testes autenticação
./tests/docker-test.sh essentials  # Testes essenciais
```

#### **📊 Análise e Depuração**
```bash
./tests/docker-test.sh coverage    # Análise de cobertura
./tests/docker-test.sh debug       # Modo interativo
./tests/docker-test.sh help        # Mostrar ajuda
```

## 🔧 Funcionalidades Integradas

### **Do PowerShell (start-integration-tests.ps1)**
- ✅ **Verificação de conectividade HTTP** para Vault, RabbitMQ, MinIO
- ✅ **Testes básicos sem containers** (OWASP + Security)
- ✅ **Análise de cobertura** com coverlet
- ✅ **Validação de saúde** dos serviços

### **Do Bash Original (docker-test.sh)**
- ✅ **Resolução de problemas de rede** entre containers
- ✅ **Configuração automática** de rede compartilhada
- ✅ **Detecção automática** de prefixos de container
- ✅ **Mapeamento dinâmico** de hosts
- ✅ **Execução em container isolado** com SDK .NET

## 📊 Tipos de Teste

### **Testes Básicos (100% funcionais)** ✅
| Arquivo | Descrição | Status |
|---------|-----------|---------|
| `BasicOwaspSecurityTests.cs` | OWASP Top 10 validation | ✅ 10/10 testes passando |
| `BasicSecurityComponentsTests.cs` | Componentes unitários | ✅ 10/10 testes passando |

### **Testes de Integração** 🔄
| Arquivo | Descrição | Requisitos |
|---------|-----------|------------|
| `BasicJwtFido2Tests.cs` | Autenticação JWT/FIDO2 | 🐳 Vault + PostgreSQL |

## 🐳 Containers Necessários

| Container | Porta | Função | Auto-Start |
|-----------|-------|--------|------------|
| **vault** | 8200 | Secrets management | ✅ Sim |
| **postgres** | 5432 | Banco principal | ✅ Sim |
| **rabbitmq** | 5672/15672 | Messaging | ✅ Sim |
| **minio** | 9000/9001 | Object storage | ✅ Sim |

## 🔍 Verificações Automáticas

O script executa as seguintes verificações:

### **1. Ambiente**
- ✅ Docker está rodando
- ✅ Docker Compose disponível  
- ✅ WSL detection (se aplicável)

### **2. Containers**
- ✅ Auto-start se não estiver rodando
- ✅ Health checks para cada serviço
- ✅ Conectividade HTTP/TCP

### **3. Rede**
- ✅ Criação de rede compartilhada
- ✅ Mapeamento dinâmico de IPs
- ✅ Resolução DNS entre containers

## 📈 Exemplos de Uso

### **Desenvolvimento Rápido** ⚡
```bash
# Validação rápida (sem containers)
./tests/docker-test.sh basic

# Resultado esperado: 20/20 testes passando em ~30s
```

### **Teste Específico** 🎯
```bash
# Testar apenas PostgreSQL
./tests/docker-test.sh postgres -v

# Inicia containers automaticamente se necessário
```

### **Análise Completa** 📊
```bash
# Análise de cobertura
./tests/docker-test.sh coverage

# Gera relatórios em coverage-report/
```

### **Depuração** 🔧
```bash
# Modo interativo
./tests/docker-test.sh debug

# Acesso bash no container para diagnóstico
```

## 🛠️ Troubleshooting

### **"Docker não está rodando"**
```bash
# Windows
# Inicie Docker Desktop

# Verificar
docker info
```

### **"Containers não iniciam"**
```bash
# Limpar completamente
docker-compose down --volumes --remove-orphans
docker system prune -f

# Reiniciar
./tests/docker-test.sh postgres
```

### **"Testes básicos falham"**
```bash
# Verificar build
dotnet build

# Executar apenas OWASP
./tests/docker-test.sh owasp
```

### **"Problemas de rede"**
```bash
# Diagnóstico completo
./tests/docker-test.sh debug

# Verificar containers
docker-compose ps
```

## 🎯 Status do Projeto

| Categoria | Status | Detalhes |
|-----------|--------|----------|
| **Testes Básicos** | ✅ 100% | 20/20 testes funcionais |
| **Infraestrutura** | ✅ Pronta | Auto-start + health checks |
| **Integração** | 🔄 Parcial | JWT/FIDO2 precisa de serviços |
| **Cobertura** | ✅ Configurada | Coverlet + ReportGenerator |
| **Documentação** | ✅ Completa | Scripts + README |

## 🔗 Arquivos Relacionados

- `tests/docker-test.sh` - **Script principal** (este arquivo)
- `tests/integration/BasicOwaspSecurityTests.cs` - Testes OWASP
- `tests/integration/BasicSecurityComponentsTests.cs` - Testes unitários
- `tests/integration/BasicJwtFido2Tests.cs` - Testes integração
- `tests/integration/coverlet.runsettings` - Configuração cobertura
- `docker-compose.yml` - Infraestrutura containers

## 💡 Recomendações

### **Para Desenvolvimento Diário** 📅
```bash
./tests/docker-test.sh basic
```
- Rápido (sem containers)
- Cobre aspectos críticos
- Validação antes de commits

### **Para CI/CD Pipeline** 🔄
```bash
./tests/docker-test.sh coverage
```
- Análise completa
- Relatórios detalhados
- Validação de qualidade

### **Para Debugging** 🐛
```bash
./tests/docker-test.sh debug
```
- Acesso interativo
- Diagnóstico de rede
- Investigação de falhas

---

## 🎉 Resumo

O `docker-test.sh` agora é o **script unificado** que:
- ✅ Mantém toda funcionalidade original de rede
- ✅ Adiciona testes básicos do PowerShell  
- ✅ Suporta análise de cobertura
- ✅ Fornece depuração interativa
- ✅ Auto-start de containers quando necessário

**Use `./tests/docker-test.sh help` para ver todas as opções!** 🚀
