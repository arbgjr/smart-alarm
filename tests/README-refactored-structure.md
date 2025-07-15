# 📋 Smart Alarm - Sistema de Testes Refatorado ✅ CONCLUÍDO

## 🎯 Visão Geral

O sistema de testes do Smart Alarm foi **REFATORADO COM SUCESSO** para uma estrutura modular, onde o script principal (`SmartAlarm-test.sh`) coordena a preparação do ambiente e chama scripts especializados para cada grupo de testes.

## ✅ Status da Refatoração CONCLUÍDO

- ✅ **Script principal refatorado**: `SmartAlarm-test.sh` funcional
- ✅ **Scripts especializados criados**: 5 scripts na pasta `scripts/`
- ✅ **Funções compartilhadas**: `test-common.sh` implementado
- ✅ **Backup preservado**: `SmartAlarm-test.sh.backup`
- ✅ **Testes validados**: Help e estrutura funcionando
- ✅ **Documentação atualizada**: README completo

## 🏗️ Estrutura de Arquivos

```
tests/
├── SmartAlarm-test.sh              # Script principal - coordenador
├── test-common.sh                  # Funções compartilhadas
├── run-holiday-tests.sh           # Script legacy (mantido para compatibilidade)
├── scripts/                       # Scripts especializados
│   ├── run-basic-tests.sh          # Testes básicos (sem containers)
│   ├── run-integration-tests.sh    # Testes de integração (com containers)
│   ├── run-coverage-tests.sh       # Análise de cobertura
│   ├── run-holiday-tests.sh        # Testes Holiday API (refatorado)
│   └── run-debug.sh                # Ferramentas de depuração
└── ...outros arquivos
```

## 🚀 Como Usar

### Script Principal (Recomendado)

```bash
# Testes básicos (rápidos, sem containers)
./tests/SmartAlarm-test.sh basic

# Testes de integração específicos
./tests/SmartAlarm-test.sh postgres
./tests/SmartAlarm-test.sh vault
./tests/SmartAlarm-test.sh minio
./tests/SmartAlarm-test.sh rabbitmq

# Testes especializados
./tests/SmartAlarm-test.sh holiday
./tests/SmartAlarm-test.sh coverage

# Modo de depuração
./tests/SmartAlarm-test.sh debug

# Com saída detalhada
./tests/SmartAlarm-test.sh postgres --verbose
```

### Scripts Especializados (Uso Direto)

```bash
# Executar diretamente (ambiente deve estar preparado)
./tests/scripts/run-basic-tests.sh security
./tests/scripts/run-integration-tests.sh postgres
./tests/scripts/run-coverage-tests.sh
./tests/scripts/run-holiday-tests.sh build
./tests/scripts/run-debug.sh interactive
```

## 📁 Descrição dos Scripts

### 1. `SmartAlarm-test.sh` (Principal)
- **Função**: Coordenador principal do sistema de testes
- **Responsabilidades**:
  - Preparar ambiente (limpar recursos, configurar rede Docker)
  - Detectar diretório raiz do projeto
  - Chamar scripts especializados apropriados
  - Mostrar instruções finais

### 2. `test-common.sh` (Funções Compartilhadas)
- **Função**: Biblioteca de funções comuns
- **Conteúdo**:
  - Funções de cores e mensagens
  - Utilitários Docker (IPs, conectividade)
  - Validações de ambiente
  - Funções de diagnóstico

### 3. `scripts/run-basic-tests.sh`
- **Função**: Testes básicos que não requerem containers
- **Tipos**:
  - `basic`: Todos os testes básicos
  - `owasp`: Testes OWASP Top 10
  - `security`: Testes de segurança
  - `all-security`: Todos os testes de segurança

### 4. `scripts/run-integration-tests.sh`
- **Função**: Testes de integração com containers Docker
- **Tipos**:
  - `postgres`: Testes PostgreSQL
  - `vault`: Testes HashiCorp Vault
  - `minio`: Testes MinIO
  - `rabbitmq`: Testes RabbitMQ
  - `jwt-fido2`: Testes autenticação
  - `essentials`: Testes essenciais marcados

### 5. `scripts/run-coverage-tests.sh`
- **Função**: Análise de cobertura de código
- **Recursos**:
  - Cobertura geral
  - Cobertura direcionada por categoria
  - Relatórios em XML/JSON

### 6. `scripts/run-holiday-tests.sh`
- **Função**: Testes especializados da Holiday API
- **Modos**:
  - `build`: Build + testes
  - `coverage`: Testes com cobertura
  - `unit`, `integration`, `api`: Por categoria
  - `handlers`, `validators`: Por componente

### 7. `scripts/run-debug.sh`
- **Função**: Ferramentas de depuração e diagnóstico
- **Recursos**:
  - Container interativo
  - Testes de conectividade
  - Verificação de logs
  - Diagnóstico básico

## 🔧 Preparação do Ambiente

### Responsabilidades do Script Principal

1. **Limpeza de Recursos**
   - Remove containers de teste anteriores
   - Limpa redes Docker órfãs
   - Remove rede de teste anterior

2. **Configuração de Rede**
   - Cria rede compartilhada `smartalarm-test-net`
   - Conecta containers de serviço à rede
   - Gera mapeamentos de host
   - Aguarda serviços ficarem disponíveis

3. **Detecção de Ambiente**
   - Localiza diretório raiz do projeto
   - Detecta prefixo dos containers
   - Verifica Docker e Docker Compose

### Variáveis Exportadas

Os scripts especializados recebem estas variáveis:

```bash
PROJECT_ROOT         # Diretório raiz do projeto
CONTAINER_PREFIX     # Prefixo dos containers (smart-alarm ou smartalarm)
```

## 🐳 Integração Docker

### Rede Compartilhada
- **Nome**: `smartalarm-test-net`
- **Propósito**: Conectividade entre containers de teste e serviços
- **Serviços**: postgres, vault, minio, rabbitmq, prometheus, loki, jaeger, grafana

### Mapeamentos de Host
Containers de teste recebem mapeamentos automáticos:
```bash
--add-host postgres:IP_DO_CONTAINER
--add-host vault:IP_DO_CONTAINER
--add-host minio:IP_DO_CONTAINER
--add-host rabbitmq:IP_DO_CONTAINER
```

### Variáveis de Ambiente
```bash
POSTGRES_HOST=postgres
VAULT_HOST=vault
MINIO_HOST=minio
RABBITMQ_HOST=rabbitmq
ASPNETCORE_ENVIRONMENT=Testing
# + credenciais específicas
```

## 📊 Vantagens da Refatoração

### ✅ Benefícios

1. **Modularidade**: Cada script tem responsabilidade específica
2. **Reutilização**: Funções comuns centralizadas
3. **Manutenibilidade**: Código mais organizado e legível
4. **Flexibilidade**: Scripts podem ser executados independentemente
5. **Escalabilidade**: Fácil adicionar novos grupos de testes
6. **Debugging**: Script dedicado para diagnóstico

### 🔄 Compatibilidade

- **Mantida**: Todas as opções do script original funcionam
- **Melhorada**: Interface mais clara e informativa
- **Expandida**: Novos recursos de depuração e diagnóstico

## 🛠️ Desenvolvimento

### Adicionando Novos Grupos de Testes

1. Criar script em `tests/scripts/run-NOVO-GRUPO.sh`
2. Importar `test-common.sh`
3. Implementar lógica específica
4. Adicionar case no script principal
5. Atualizar help e documentação

### Exemplo de Novo Script

```bash
#!/bin/bash
# Importar funções comuns
source "$(dirname "$0")/../test-common.sh"

run_novo_grupo_tests() {
    local tipo="$1"
    local verbose_mode="$2"
    
    print_message "${BLUE}" "=== Executando Novo Grupo ==="
    # ... implementação específica
}

main() {
    detect_project_root
    run_novo_grupo_tests "$1" "$2"
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
```

## 📚 Migração

### Para Usuários Existentes

O script principal mantém compatibilidade total:

```bash
# Antes (ainda funciona)
./tests/SmartAlarm-test.sh postgres

# Novo (mesma funcionalidade, melhor estruturado)
./tests/SmartAlarm-test.sh postgres
```

### Para Desenvolvedores

- Scripts especializados podem ser executados diretamente
- Funções comuns disponíveis via `test-common.sh`
- Ambiente deve estar preparado pelo script principal

## 🔍 Troubleshooting

### Problemas Comuns

1. **Script não encontrado**: Verificar estrutura de pastas
2. **Permissão negada**: Executar `chmod +x` nos scripts
3. **Rede não configurada**: Executar script principal primeiro
4. **Containers não encontrados**: Verificar `docker-compose up -d`

### Debug

```bash
# Modo de depuração completo
./tests/SmartAlarm-test.sh debug

# Verificar conectividade específica
./tests/scripts/run-debug.sh connectivity

# Container interativo
./tests/scripts/run-debug.sh interactive
```
